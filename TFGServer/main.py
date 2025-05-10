import disnake
from disnake.ext import commands
import config
import hashlib
import base64
from cryptography.fernet import Fernet
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

password = config.Password
def generate_key_from_password(password: str) -> bytes:
    sha256 = hashlib.sha256(password.encode()).digest()
    return base64.urlsafe_b64encode(sha256)

def decrypt_token(encrypted_token: str, password: str) -> str:
    key = generate_key_from_password(password)
    fernet = Fernet(key)
    decrypted_token = fernet.decrypt(encrypted_token.encode())
    return decrypted_token.decode()

decrypted_token = decrypt_token(config.Bot, password)
bot.run(decrypted_token)
