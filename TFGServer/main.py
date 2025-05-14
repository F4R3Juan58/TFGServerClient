import disnake
from disnake.ext import commands
import config
import os

bot = commands.Bot(command_prefix="!!", intents=disnake.Intents.all())

####    Inicio del Bot      ####

@bot.event
async def on_ready():
    print(f'Bot conectado como {bot.user.name}')
    load_extensions() # Llamar a load_extensions sin await

def load_extensions():
    print("Cargando extensiones")
    for filename in os.listdir("./cogs"):
        if filename.endswith(".py"):
            bot.load_extension(f"cogs.{filename[:-3]}")
    print("Extensiones cargadas.")


#### Token ####

bot.run(config.Token)
