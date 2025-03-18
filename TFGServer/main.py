import discord
from discord.ext import commands
import config
import os

bot = commands.Bot(command_prefix="!!", intents=discord.Intents.all())

####    Inicio del Bot      ####

@bot.event
async def on_ready():  # Asegúrate de que 'on_ready' sea una corutina.
    print(f'Bot conectado como {bot.user.name}')
    await load_extensions()  # Llamar a load_extensions sin await

async def load_extensions():
    print("Cargando extensiones")
    for filename in os.listdir("./cogs"):
        if filename.endswith(".py"):
            # Cargar la extensión sin usar await
            await bot.load_extension(f"cogs.{filename[:-3]}")
    print("Extensiones cargadas.")

#### Token ####

bot.run(config.Token)
