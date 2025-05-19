from disnake.ext import commands
import disnake
from db_connection import Database  # tu conexión

class CrearServidor(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    async def _crear_servidor(self, nombre_instituto: str, insti_id: int) -> str | None:
        # 1. Verificar en la base de datos si ya existe servidor para ese instituto
        if await self.db.check_server_exists(insti_id):
            print(f"⚠️ Ya existe un servidor para instituto ID {insti_id}")
            return None

        # 2. Crear el servidor en Discord
        for guild in self.bot.guilds:
            if guild.name.lower() == nombre_instituto.lower():
                print(f"⚠️ Ya existe un servidor con el nombre '{nombre_instituto}'")
                return None

        try:
            nuevo_guild = await self.bot.create_guild(name=nombre_instituto)
            print(f"✅ Servidor '{nombre_instituto}' creado.")

            # Borrar canales por defecto
            for channel in nuevo_guild.channels:
                await channel.delete()

            # Crear canales nuevos
            canal_general = await nuevo_guild.create_text_channel("📌・general")
            await nuevo_guild.create_text_channel("❓・dudas")
            print(f"📂 Canales creados en '{nombre_instituto}'.")

            # Crear invitación para canal general
            invite = await canal_general.create_invite(max_age=0, max_uses=0, unique=True)

            # Guardar en la base de datos el servidor creado
            await self.db.save_server(insti_id, nombre_instituto, nuevo_guild.id)

            return invite.url

        except disnake.HTTPException as e:
            print(f"❌ Error al crear servidor '{nombre_instituto}': {e}")
            return None

def setup(bot):
    bot.add_cog(CrearServidor(bot))