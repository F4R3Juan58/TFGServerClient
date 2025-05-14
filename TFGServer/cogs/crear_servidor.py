from disnake.ext import commands
import disnake

class CrearServidor(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    @commands.command(name="crear_servidor")
    async def crear_servidor(self, ctx, *, nombre_instituto: str):
        """Simula la creación de un servidor con nombre del instituto y 2 canales por defecto"""

        # 1. Buscar si ya existe un servidor con ese nombre
        for guild in self.bot.guilds:
            if guild.name.lower() == nombre_instituto.lower():
                await ctx.send(f"❌ Ya existe un servidor llamado **{nombre_instituto}**.")
                return

        # 2. Crear el servidor (Guild)
        try:
            nuevo_guild = await self.bot.create_guild(name=nombre_instituto)
            await ctx.send(f"✅ Servidor **{nombre_instituto}** creado correctamente.")

            # 3. Obtener el canal por defecto y crear canales adicionales
            for channel in nuevo_guild.channels:
                await channel.delete()

            await nuevo_guild.create_text_channel("📌・general")
            await nuevo_guild.create_text_channel("❓・dudas")

            await ctx.send(f"📂 Canales creados en **{nombre_instituto}**.")

        except disnake.HTTPException as e:
            await ctx.send(f"❌ Error al crear el servidor: {str(e)}")

def setup(bot):
    bot.add_cog(CrearServidor(bot))