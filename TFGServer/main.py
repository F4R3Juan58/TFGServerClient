from flask import Flask, request, jsonify
import threading
import asyncio
import disnake
from disnake.ext import commands
import os
import config
from db_connection import Database  # Clase para acceso a DB

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
    for g in bot.guilds:
        print(f"🧠 {g.name} - {g.id}")
    cargar_cogs()

def run_bot():
    try:
        print("🔌 Iniciando bot de Discord...")
        bot.run(config.Token)
    except Exception as e:
        print("❌ Error al iniciar el bot:", e)

threading.Thread(target=run_bot, daemon=True).start()
print("🧵 Hilo del bot iniciado.")

@app.route("/crear-servidor", methods=["POST"])
def crear_servidor_api():
    data = request.json
    nombre = data.get("nombre")
    insti_id = data.get("insti_id")
    user_email = data.get("email")

    if not nombre or not insti_id or not user_email:
        return jsonify({"error": "Faltan datos"}), 400

    try:
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("CrearServidor")._crear_servidor(nombre, insti_id, user_email),
            bot.loop
        )
        invite_url = future.result(timeout=20)
        if invite_url:
            return jsonify({"status": "OK", "invite": invite_url}), 200
        else:
            return jsonify({"status": "Error", "message": "Servidor ya existente o error interno"}), 400

    except Exception as e:
        print(f"Error en crear-servidor: {e}")
        return jsonify({"error": f"Error al crear el servidor: {str(e)}"}), 500

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

@app.route("/configurar-servidor", methods=["POST"])
def configurar_servidor_api():
    data = request.get_json(force=True)
    email = data.get("email")
    cursos = data.get("cursos")  # Ahora es lista de dicts con 'grado' y 'curso'

    if not email or not cursos:
        return jsonify({"error": "Faltan datos"}), 400

    try:
        # Obtener InstiID
        insti_id = asyncio.run_coroutine_threadsafe(
            db.obtener_insti_id_por_email(email),
            bot.loop
        ).result()

        if not insti_id:
            return jsonify({"error": "No se encontró instituto asociado al usuario."}), 404

        # Obtener servidor
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        if not servidor:
            return jsonify({"error": "No existe servidor asociado al instituto."}), 404

        discord_id = int(servidor['DiscordID'])
        guild = bot.get_guild(discord_id)

        if not guild:
            # Bot no está en el servidor
            client_id = bot.user.id
            permisos = 268438528
            invite_link = f"https://discord.com/oauth2/authorize?client_id={client_id}&permissions={permisos}&scope=bot"

            return jsonify({
                "error": "El bot no está en el servidor.",
                "invite_url": invite_link
            }), 403

        cog = bot.get_cog("ConfigurarServidorCog")
        if not cog:
            return jsonify({"error": "No se encontró el cog ConfigurarServidorCog."}), 500

        asyncio.run_coroutine_threadsafe(
            cog.configurar_servidor_api(guild, cursos),
            bot.loop
        ).result()

        return jsonify({"status": "OK", "message": "Servidor configurado correctamente."}), 200

    except Exception as e:
        return jsonify({"error": f"Error al configurar el servidor: {str(e)}"}), 500

if __name__ == "__main__":
    print("🚀 Iniciando servidor Flask...")
    app.run(host="0.0.0.0", port=5000)