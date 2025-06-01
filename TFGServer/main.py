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
    for guild in bot.guilds:
        print(f"Conectado a: {guild.name} (ID: {guild.id})")
        
        
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

@app.route("/login-alumno", methods=["POST"])
def login_alumno():
    data = request.get_json(force=True)
    email = data.get("Email")

    print(email)

    if not email:
        return jsonify({"error": "Falta el email"}), 400

    try:
        # Paso 1: Obtener el insti_id por email
        insti_id = asyncio.run_coroutine_threadsafe(
            db.obtener_insti_id_por_email(email),
            bot.loop
        ).result()
        
        print(insti_id)

        if not insti_id:
            return jsonify({"error": "No se encontró el instituto para ese email"}), 404

        # Paso 2: Obtener los datos del servidor con ese insti_id
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        print(servidor)

        if not servidor:
            return jsonify({"error": "No hay servidor asociado al instituto"}), 404

        # Paso 3: Obtener el guild desde el bot
        discord_id = int(servidor['DiscordID'])
        guild = bot.get_guild(discord_id)

        if not guild:
            return jsonify({"error": "Servidor no encontrado"}), 404

        # Crear invitación en el primer canal disponible con permisos
        invite = None
        for channel in guild.text_channels:
            if channel.permissions_for(guild.me).create_instant_invite:
                invite = asyncio.run_coroutine_threadsafe(
                    channel.create_invite(max_age=86400, max_uses=1),
                    bot.loop
                ).result()
                break

        if not invite:
            return jsonify({"error": "No se pudo crear una invitación"}), 500

        # Guardar la invitación en la tabla conexiones
        asyncio.run_coroutine_threadsafe(
            db.save_invitation(email, invite.code),
            bot.loop
        ).result()

        return jsonify({"invite_url": invite.url}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500

    
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

@app.route("/eliminar-servidor", methods=["POST"])
def eliminar_servidor():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")

    if not insti_id:
        return jsonify({"error": "Falta InstiID"}), 400

    try:
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        if not servidor:
            return jsonify({"error": "No existe servidor asociado al instituto."}), 404

        discord_id = int(servidor['DiscordID'])
        guild = bot.get_guild(discord_id)

        async def procesar_servidor():
            try:
                await guild.delete()
                print(f"Servidor '{guild.name}' eliminado completamente.")
            except Exception as e:
                print(f"Error al eliminar el servidor: {e}")

        asyncio.run_coroutine_threadsafe(procesar_servidor(), bot.loop).result()

        # Aquí puedes eliminar también el registro de la base de datos si quieres
        db.eliminar_servidor(insti_id)

        return jsonify({"status": "OK", "message": "Servidor eliminado completamente."}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500



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


@app.route("/añadir-cursos", methods=["POST"])
def configurar_servidor_api():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    cursos_raw = data.get("cursosGrados")  # Aquí recibes string: "1º DAM, 2º ASIR"

    if not insti_id or not cursos_raw:
        return jsonify({"error": "Faltan datos"}), 400

    # Convertir string a lista de strings separadas por coma
    cursos_lista = [curso.strip() for curso in cursos_raw.split(",") if curso.strip()]

    try:
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        if not servidor:
            return jsonify({"error": "No existe servidor asociado al instituto."}), 404

        discord_id = int(servidor['DiscordID'])
        guild = bot.get_guild(discord_id)

        cog = bot.get_cog("AñadirCursosCogs")
        if not cog:
            return jsonify({"error": "No se encontró el cog AñadirCursosCogs."}), 500

        asyncio.run_coroutine_threadsafe(
            cog.configurar_servidor_api(guild, cursos_lista),
            bot.loop
        ).result()

        return jsonify({"status": "OK", "message": "Servidor configurado correctamente."}), 200

    except Exception as e:
        return jsonify({"error": f"Error al configurar el servidor: {str(e)}"}), 500

@app.route("/obtener-categorias", methods=["POST"])  # POST porque recibes insti_id en body JSON
def obtener_categorias():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")

    if not insti_id:
        return jsonify({"error": "Falta InstiID"}), 400

    try:
        # Obtener servidor asociado al instituto (sincronizado con bot.loop)
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        if not servidor:
            return jsonify({"error": "No existe servidor asociado al instituto."}), 404

        discord_id = int(servidor['DiscordID'])
        guild = bot.get_guild(discord_id)

        if guild is None:
            return jsonify({"error": "Guild no encontrado."}), 404

        # Función async para obtener categorías filtradas
        async def obtener_categorias_guild():
            categorias = []
            await guild.fetch_channels()  # Actualizar caché de canales
            for cat in guild.categories:
                if "-" not in cat.name:
                    categorias.append(cat.name)
            # Eliminar duplicados si los hay
            categorias_unicas = list(set(categorias))
            return categorias_unicas

        # Ejecutar función async desde sync
        categorias_filtradas = asyncio.run_coroutine_threadsafe(
            obtener_categorias_guild(),
            bot.loop
        ).result()

        return jsonify(categorias_filtradas)

    except Exception as e:
        return jsonify({"error": f"Error al obtener categorías: {str(e)}"}), 500
    
@app.route("/eliminar-categoria", methods=["POST"])
def eliminar_categoria():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    categoria = data.get("categoria")

    if not insti_id or not categoria:
        return jsonify({"error": "Faltan datos"}), 400

    try:
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        if not servidor:
            return jsonify({"error": "No existe servidor asociado al instituto."}), 404

        discord_id = int(servidor['DiscordID'])
        guild = bot.get_guild(discord_id)

        if not guild:
            return jsonify({"error": "Guild no encontrado."}), 404

        cog = bot.get_cog("EliminarCursosCogs")
        if not cog:
            return jsonify({"error": "Cog no encontrado."}), 500

        future = asyncio.run_coroutine_threadsafe(
            cog.eliminar_categoria_y_canales(guild, categoria),
            bot.loop
        )
        future.result()

        return jsonify({"status": "OK", "message": "Categoría eliminada correctamente."}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/expulsar-usuario", methods=["POST"])
def expulsar_usuario():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    discord_id_usuario = data.get("discordId")

    if not insti_id or not discord_id_usuario:
        return jsonify({"error": "Faltan datos: InstiID y discordId son requeridos"}), 400

    try:
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        discord_id = int(servidor['DiscordID'])
        guild = bot.get_guild(discord_id)

        async def kick_member():
            member = guild.get_member(int(discord_id_usuario))
            if member is None:
                return False, "Usuario no encontrado en el servidor."

            try:
                await member.kick(reason="Expulsado desde panel administrativo")
                return True, "Usuario expulsado correctamente."
            except Exception as e:
                return False, f"Error al expulsar usuario: {e}"

        success, message = asyncio.run_coroutine_threadsafe(kick_member(), bot.loop).result()

        if success:
            return jsonify({"status": "OK", "message": message})
        else:
            return jsonify({"error": message}), 500

    except Exception as e:
        return jsonify({"error": f"Error interno: {e}"}), 500



@bot.command()
@commands.is_owner()  # Solo el dueño del bot puede usar este comando
async def eliminar_servidor(ctx, guild_id: int):
    guild = bot.get_guild(guild_id)
    if not guild:
        await ctx.send(f"❌ No se encontró el servidor con ID {guild_id}")
        return

    try:
        await guild.delete()
        await ctx.send(f"✅ Servidor '{guild.name}' eliminado correctamente.")
    except Exception as e:
        await ctx.send(f"⚠️ Error al eliminar el servidor: {e}")

if __name__ == "__main__":
    print("🚀 Iniciando servidor Flask...")
    app.run(host="0.0.0.0", port=5000)