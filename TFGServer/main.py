from flask import Flask, request, jsonify
import threading
import asyncio
import disnake
from disnake.ext import commands
import os
import config
from db_connection import Database  # Importa tu clase de conexión

app = Flask(__name__)

intents = disnake.Intents.all()
bot = commands.Bot(command_prefix="!!", intents=intents)

db = Database()

def cargar_cogs():
    print("🔄 Cargando cogs...")
    for filename in os.listdir("./cogs"):
        if filename.endswith(".py") and not filename.startswith("__"):
            try:
                bot.load_extension(f"cogs.{filename[:-3]}")
                print(f"✅ Cog '{filename}' cargado.")
            except Exception as e:
                print(f"❌ Error al cargar '{filename}': {e}")
    print("✅ Todos los cogs han sido cargados.")

@bot.event
async def on_ready():
    print(f'✅ Bot conectado como {bot.user.name}')
    cargar_cogs()

def run_bot():
    bot.run(config.Token)

threading.Thread(target=run_bot, daemon=True).start()

@app.route("/crear-servidor", methods=["POST"])
def crear_servidor_api():
    data = request.json
    nombre = data.get("nombre")
    insti_id = data.get("insti_id")

    if not nombre or not insti_id:
        return jsonify({"error": "Faltan datos"}), 400

    try:
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("CrearServidor")._crear_servidor(nombre, insti_id), bot.loop
        )
        invite_url = future.result(timeout=15)

        if invite_url:
            return jsonify({"status": "OK", "invite": invite_url}), 200
        else:
            return jsonify({"status": "Error", "message": "Servidor ya existente o error interno"}), 400

    except Exception as e:
        print(f"Error en crear-servidor: {e}")
        return jsonify({"error": f"Error al crear el servidor: {str(e)}"}), 500

# Aquí añades el endpoint para vincular el DiscordID al usuario
@app.route("/vincular-discordid", methods=["POST"])
def vincular_discordid_api():
    data = request.json
    email = data.get("email")
    discord_id = data.get("discord_id")

    if not email or not discord_id:
        return jsonify({"error": "Faltan email o discord_id"}), 400

    try:
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        # Ejecutamos la función asíncrona para actualizar DiscordID en la base de datos
        future = asyncio.run_coroutine_threadsafe(
            db.actualizar_discord_id(email, discord_id),
            loop
        )
        actualizado = future.result(timeout=10)

        if actualizado:
            return jsonify({"status": "OK"}), 200
        else:
            return jsonify({"error": "Usuario no encontrado para el email dado"}), 404

    except Exception as e:
        print(f"Error en vincular-discordid: {e}")
        return jsonify({"error": f"Error interno: {str(e)}"}), 500

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000) 