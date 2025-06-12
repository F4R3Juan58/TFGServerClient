from disnake.ext import commands
import disnake
import traceback
from db_connection import Database

class DiscordIDLinker(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()
        self.invite_uses_cache = {}  # {guild_id: {invite_code: uses}}
        self.deleted_invites_cache = {}
        
    async def cog_load(self):
        print("🔄 Inicializando pool de base de datos en DiscordIDLinker...")
        await self.db.init_pool()
        print("✅ Pool de base de datos inicializado.")

    async def cache_invites_for_guild(self, guild: disnake.Guild):
        try:
            invites = await guild.invites()
            self.invite_uses_cache[guild.id] = {invite.code: invite.uses for invite in invites}
            print(f"Cache de invitaciones cargada para guild {guild.name} ({guild.id})")
        except Exception as e:
            print(f"Error al cachear invitaciones para guild {guild.id}: {e}")
            traceback.print_exc()

    @commands.Cog.listener()
    async def on_ready(self):
        print("🔄 Cacheando invitaciones para todos los guilds en on_ready...")
        for guild in self.bot.guilds:
            await self.cache_invites_for_guild(guild)
        print("✅ Cache inicial de invitaciones completada.")

    @commands.Cog.listener()
    async def on_invite_create(self, invite: disnake.Invite):
        guild_cache = self.invite_uses_cache.setdefault(invite.guild.id, {})
        guild_cache[invite.code] = invite.uses
        print(f"Invitación creada y cacheada: {invite.code} en guild {invite.guild.id}")

    @commands.Cog.listener()
    async def on_invite_delete(self, invite: disnake.Invite):
        guild_cache = self.invite_uses_cache.get(invite.guild.id, {})
        if invite.code in guild_cache:
            guild_cache.pop(invite.code)
            print(f"📌 Invitación eliminada guardada temporalmente: {invite.code} en guild {invite.guild.id}")
            # Guardamos invitación eliminada para chequear después en on_member_join
            if invite.guild.id not in self.deleted_invites_cache:
                self.deleted_invites_cache[invite.guild.id] = {}
            self.deleted_invites_cache[invite.guild.id][invite.code] = invite   

    @commands.Cog.listener()
    async def on_member_join(self, member: disnake.Member):
        print("Nuevo usuario en el server")
        guild = member.guild
        try:
            print(f"[DEBUG] Miembro {member.name} ({member.id}) ha entrado al servidor {guild.name} ({guild.id})")

            current_invites = await guild.invites()
            print(f"[DEBUG] Invitaciones actuales en guild {guild.id}: {[inv.code for inv in current_invites]}")

            guild_cache = self.invite_uses_cache.setdefault(guild.id, {})
            print(f"[DEBUG] Cache de usos anteriores: {guild_cache}")

            used_invite = None
            for invite in current_invites:
                old_uses = guild_cache.get(invite.code, 0)
                print(f"[DEBUG] Invitación {invite.code}: usos actuales {invite.uses}, usos cache {old_uses}")
                if invite.uses > old_uses:
                    used_invite = invite
                    print(f"[DEBUG] Invitación usada detectada (activa): {invite.code}")
                    break

            # Si no detectamos invitación usada en las activas, buscamos en eliminadas
            if used_invite is None:
                deleted_cache = self.deleted_invites_cache.get(guild.id, {})
                print(f"[DEBUG] Invitaciones eliminadas en cache para guild {guild.id}: {list(deleted_cache.keys())}")

                if len(deleted_cache) == 1:
                    used_invite = list(deleted_cache.values())[0]
                    print(f"[DEBUG] Invitación usada eliminada detectada: {used_invite.code} por usuario {member.name} ({member.id})")
                else:
                    print(f"[DEBUG] No se pudo detectar la invitación usada para {member.name}. No hay una única invitación eliminada.")
                    return

            # Actualizar cache con los nuevos usos para invitaciones activas
            self.invite_uses_cache[guild.id] = {inv.code: inv.uses for inv in current_invites}
            print(f"[DEBUG] Cache actualizada: {self.invite_uses_cache[guild.id]}")

            # Eliminar invitaciones con usos >= 1 del servidor y de la cache
            codes_a_eliminar = [inv.code for inv in current_invites if inv.uses >= 1]
            for code in codes_a_eliminar:
                invite_to_delete = next((inv for inv in current_invites if inv.code == code), None)
                if invite_to_delete:
                    try:
                        await invite_to_delete.delete()
                        print(f"[DEBUG] Invitación {code} eliminada del servidor.")
                    except Exception as delete_error:
                        print(f"[ERROR] No se pudo eliminar invitación {code}: {delete_error}")

                    # Eliminar también de la cache interna
                    if code in self.invite_uses_cache[guild.id]:
                        del self.invite_uses_cache[guild.id][code]
                        print(f"[DEBUG] Invitación {code} eliminada de la cache interna.")

            print(f"[DEBUG] Invitación usada final: {used_invite.code} por usuario {member.name} ({member.id})")

            conexion = await self.db.get_connection_by_invite(used_invite.code)
            print(f"[DEBUG] Resultado consulta BD para invitación {used_invite.code}: {conexion}")

            if conexion and conexion.get('discordid') is None:
                email = conexion.get('email')
                print(f"[DEBUG] Email asociado: {email}")

                actualizado = await self.db.update_discordid_by_email_all(email, member.id)
                if actualizado:
                    print(f"[DEBUG] DiscordID de {member.name} vinculado en tablas con email {email}")
                else:
                    print(f"[DEBUG] No se encontró el email {email} en tablas para actualizar discordid")

                try:
                    await member.send("✅ Tu Discord ha sido vinculado correctamente con la invitación usada.")
                    print(f"[DEBUG] DM enviado a {member.name}")
                except Exception as dm_error:
                    print(f"[DEBUG] No se pudo enviar DM a {member.name}: {dm_error}")
            else:
                print(f"[DEBUG] Invitación {used_invite.code} no encontrada en BD o ya vinculada (discordid no es None).")

            # Limpiar invitación usada de cache de eliminadas para no procesarla otra vez
            if used_invite.code in self.deleted_invites_cache.get(guild.id, {}):
                del self.deleted_invites_cache[guild.id][used_invite.code]
                print(f"[DEBUG] Invitación {used_invite.code} removida de cache de eliminadas.")
            
            await self.asignar_roles_por_discordid(member)

        except Exception as e:
            print(f"[ERROR] Error en on_member_join: {e}")
            traceback.print_exc()

    async def asignar_roles_por_discordid(self, member: disnake.Member):
        discord_id = member.id

        # Buscar si es profesor
        profesor = await self.db.obtener_profesor_por_discordid(discord_id)
        if profesor:
            # Asignar rol Profesor
            rol_profesor = disnake.utils.get(member.guild.roles, name="profesor")
            if rol_profesor:
                await member.add_roles(rol_profesor)
                print(f"[DEBUG] Rol Profesor asignado a {member.name}")

            # Si es tutor, buscar el curso y asignar rol curso
            if profesor.get("IsTutor") == 1:
                tutor = disnake.utils.get(member.guild.roles, name="tutor")
                await member.add_roles(tutor)
                curso_id = profesor.get("CursoID")
                nombre_curso = await self.db.obtener_nombre_curso_por_id(curso_id)
                if nombre_curso:
                    rol_curso = disnake.utils.get(member.guild.roles, name=nombre_curso)
                    if rol_curso:
                        await member.add_roles(rol_curso)
                        print(f"[DEBUG] Rol curso '{nombre_curso}' asignado a {member.name}")
                    else:
                        print(f"[DEBUG] Rol curso '{nombre_curso}' no encontrado en servidor")
                else:
                    print(f"[DEBUG] No se encontró nombre de curso para CursoID {curso_id}")

            return  # Salimos, ya asignamos profesor y posible curso

        # Si no es profesor, buscamos si es alumno
        alumno = await self.db.obtener_alumno_por_discordid(discord_id)
        if alumno:
            rol_alumno = disnake.utils.get(member.guild.roles, name="alumno")
            if rol_alumno:
                await member.add_roles(rol_alumno)
                print(f"[DEBUG] Rol Alumno asignado a {member.name}")
            else:
                print(f"[DEBUG] Rol Alumno no encontrado en servidor")
            curso_id = alumno.get("CursoID")
            nombre_curso = await self.db.obtener_nombre_curso_por_id(curso_id)
            if nombre_curso:
                rol_curso = disnake.utils.get(member.guild.roles, name=nombre_curso)
                if rol_curso:
                    await member.add_roles(rol_curso)
                    print(f"[DEBUG] Rol curso '{nombre_curso}' asignado a {member.name}")
                else:
                    print(f"[DEBUG] Rol curso '{nombre_curso}' no encontrado en servidor")
            else:
                print(f"[DEBUG] No se encontró nombre de curso para CursoID {curso_id}")
            if alumno.get("IsDelegado") == 1:
                rol_delegado = disnake.utils.get(member.guild.roles, name="delegado")
                await member.add_roles(rol_delegado)

            return  # Salimos, ya asignamos profesor y posible curso

        # Si no es ni profesor ni alumno
        print(f"[DEBUG] Usuario {member.name} con ID {discord_id} no es ni Profesor ni Alumno")


def setup(bot):
    bot.add_cog(DiscordIDLinker(bot))