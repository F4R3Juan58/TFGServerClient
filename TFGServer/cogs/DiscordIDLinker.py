from disnake.ext import commands
import disnake
import asyncio
import aiohttp

class DiscordIDLinker(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    @commands.Cog.listener()
    async def on_member_join(self, member: disnake.Member):
        try:
            await member.send(
                "¡Bienvenido! Para vincular tu cuenta, responde a este mensaje con tu correo electrónico registrado."
            )

            def check(m):
                return m.author == member and "@" in m.content

            msg = await self.bot.wait_for('message', check=check, timeout=300)  # espera 5 minutos

            email = msg.content.strip()

            # Llamar backend para actualizar discordId con email
            async with aiohttp.ClientSession() as session:
                url = "http://localhost:5000/vincular-discordid"
                payload = {"email": email, "discord_id": member.id}
                async with session.post(url, json=payload) as resp:
                    if resp.status == 200:
                        await member.send("✅ Tu cuenta ha sido vinculada correctamente.")
                    else:
                        await member.send("❌ No se pudo vincular tu cuenta. Por favor contacta con soporte.")

        except asyncio.TimeoutError:
            await member.send("⏰ Tiempo agotado para enviar el correo. Por favor contacta con soporte para vincular tu cuenta.")
        except disnake.Forbidden:
            print(f"No puedo enviar DM a {member.name}")

def setup(bot):
    bot.add_cog(DiscordIDLinker(bot))
