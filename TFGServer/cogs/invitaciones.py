import disnake
from disnake.ext import commands

class Invitaciones(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    @commands.command(name="invitar")
    async def invitar(self, ctx, *, nombre_instituto: str):
        # Buscar el servidor por nombre
        guild = disnake.utils.get(self.bot.guilds, name=nombre_instituto)

        if not guild:
            await ctx.send(f"❌ No se encontró ningún servidor con el nombre: {nombre_instituto}")
            return

        # Buscar un canal de texto para crear la invitación
        canal = disnake.utils.get(guild.text_channels, name="general") or guild.text_channels[0]

        try:
            invite = await canal.create_invite(max_age=0, max_uses=1, unique=True)
            await ctx.author.send(f"📩 Aquí tienes tu invitación a **{guild.name}**:\n{invite.url}")
            await ctx.send("✅ Te envié la invitación por mensaje privado.")
        except disnake.Forbidden:
            await ctx.send("❌ No tengo permisos para enviarte mensajes privados.")
        except Exception as e:
            await ctx.send(f"⚠️ Ocurrió un error al crear la invitación: {str(e)}")

def setup(bot):
    bot.add_cog(Invitaciones(bot))
