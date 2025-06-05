from disnake.ext import commands
import disnake
import traceback
from db_connection import Database

class DiscordIDLinker(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()
        self.invite_uses_cache = {}  # {guild_id: {invite_code: uses}}

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
            print(f"Invitación eliminada y removida de cache: {invite.code} en guild {invite.guild.id}")

    @commands.Cog.listener()
    async def on_member_join(self, member: disnake.Member):
        guild = member.guild
        try:
            current_invites = await guild.invites()
            guild_cache = self.invite_uses_cache.setdefault(guild.id, {})

            used_invite = None
            print("Invitaciones actuales:")
            for invite in current_invites:
                print(f"{invite.code}: {invite.uses} (antes: {guild_cache.get(invite.code, 0)})")

                old_uses = guild_cache.get(invite.code, 0)
                if invite.uses > old_uses:
                    used_invite = invite
                    break

            # Actualizar cache con los nuevos usos
            self.invite_uses_cache[guild.id] = {inv.code: inv.uses for inv in current_invites}

            if used_invite is None:
                print(f"⚠️ No se pudo detectar la invitación usada por {member.name}")
                return

            print(f"Invitación usada detectada: {used_invite.code} por usuario {member.name} ({member.id})")

            conexion = await self.db.get_connection_by_invite(used_invite.code)
            print(f"Resultado consulta BD para invitación {used_invite.code}: {conexion}")

            if conexion and conexion.get('discordid') is None:
                email = conexion.get('email')
                actualizado = await self.db.update_discordid_by_email_all(email, member.id)
                if actualizado:
                    print(f"✅ DiscordID de {member.name} vinculado en tablas con email {email}")
                else:
                    print(f"⚠️ No se encontró el email {email} en Alumnos, Profesores ni Administradores para actualizar.")
                try:
                    await member.send("✅ Tu Discord ha sido vinculado correctamente con la invitación usada.")
                except Exception as dm_error:
                    print(f"⚠️ No se pudo enviar DM a {member.name}: {dm_error}")
            else:
                print(f"⚠️ Invitación {used_invite.code} no encontrada o ya vinculada.")

        except Exception as e:
            print(f"Error en on_member_join: {e}")
            traceback.print_exc()

def setup(bot):
    bot.add_cog(DiscordIDLinker(bot))