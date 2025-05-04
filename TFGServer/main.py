import disnake
from disnake.ext import commands
import config
import os

bot = commands.Bot(command_prefix="!!", intents=disnake.Intents.all())

####    Inicio del Bot      ####

@bot.event
async def on_ready():
    print(f'Bot conectado como {bot.user.name}')
    await load_extensions()

async def load_extensions():
    print("Cargando extensiones")
    for filename in os.listdir("./cogs"):
        if filename.endswith(".py"):
            try:
                await bot.load_extension(f"cogs.{filename[:-3]}")
            except Exception as e:
                print(f"No se pudo cargar {filename}: {e}")
    print("Extensiones cargadas.")

#### Token ####

bot.run(config.Token)
