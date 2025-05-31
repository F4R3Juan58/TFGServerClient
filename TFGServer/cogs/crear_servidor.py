from disnake.ext import commands
import disnake
from db_connection import Database

class CrearServidor(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()
        self.servers_admin_assigned = set()  # Guarda IDs de guilds donde ya asignamos admin

    async def _crear_servidor(self, nombre_instituto: str, insti_id: int, user_email: str) -> str | None:
        # Comprueba si ya existe servidor para ese instituto
        if await self.db.check_server_exists(insti_id):
            print(f"⚠️ Ya existe un servidor para instituto ID {insti_id}")
            return None

        # Comprueba si ya existe un guild con ese nombre
        for guild in self.bot.guilds:
            if guild.name.lower() == nombre_instituto.lower():
                print(f"⚠️ Ya existe un servidor con el nombre '{nombre_instituto}'")
                return None

        try:
            # Crear servidor
            nuevo_guild = await self.bot.create_guild(name=nombre_instituto)
            print(f"✅ Servidor '{nombre_instituto}' creado.")

            # Borrar canales por defecto
            for channel in nuevo_guild.channels:
                await channel.delete()

            # Crear canales nuevos
            canal_general = await nuevo_guild.create_text_channel("📌・general")
            print(f"📂 Canales creados en '{nombre_instituto}'.")

            # Crear rol admin con permisos de administrador
            admin_role = await nuevo_guild.create_role(name="admin", permissions=disnake.Permissions(administrator=True))
            profesor_role = await nuevo_guild.create_role(name="profesor")
            alumno_role = await nuevo_guild.create_role(name="alumno")
            print(f"🔑 Rol 'admin' creado en '{nombre_instituto}'.")

            # Guardamos el guild id en set para saber que aún no hemos asignado admin a nadie
            self.servers_admin_assigned.add(nuevo_guild.id)

            # Crear invitación para canal general
            invite = await canal_general.create_invite(max_age=0, max_uses=0, unique=True)

            # Guardar servidor en base de datos
            await self.db.save_server(insti_id, nombre_instituto, nuevo_guild.id)

            # Guardar invitación + email del creador en tabla conexiones
            await self.db.save_invitation(email=user_email, invite_code=invite.code)

            return invite.url

        except disnake.HTTPException as e:
            print(f"❌ Error al crear servidor '{nombre_instituto}': {e}")
            return None

    @commands.Cog.listener()
    async def on_member_join(self, member: disnake.Member):
        guild = member.guild
        # Comprobar si el servidor es uno que hemos creado y si aún no asignamos admin
        if guild.id in self.servers_admin_assigned:
            # Buscamos el rol admin
            admin_role = disnake.utils.get(guild.roles, name="admin")
            if admin_role is None:
                print(f"⚠️ No se encontró rol 'admin' en el servidor {guild.name}")
                return

            # Comprobar si ya hay alguien con ese rol (para evitar asignar admin a más de uno)
            admins = [m for m in guild.members if admin_role in m.roles]
            if len(admins) == 0:
                try:
                    await member.add_roles(admin_role)
                    print(f"✅ Se asignó el rol 'admin' a {member.name} en {guild.name}")
                    # Ya asignado, eliminar de la lista para no repetir
                    self.servers_admin_assigned.remove(guild.id)
                except Exception as e:
                    print(f"❌ Error asignando rol admin a {member.name}: {e}")

def setup(bot):
    bot.add_cog(CrearServidor(bot))