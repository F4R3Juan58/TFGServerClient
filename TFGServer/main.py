from flask import Flask, request, jsonify
import threading
import traceback  # arriba del archivo
import asyncio
import disnake
from disnake.ext import commands
import os
import config
from werkzeug.utils import secure_filename
from db_connection import Database  # Clase para acceso a DB

app = Flask(__name__)
UPLOAD_FOLDER = 'uploads'
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER

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
    for guild in bot.guilds:
            print(f"Bot está en el servidor: {guild.name} (ID: {guild.id})")
    cargar_cogs()

def run_bot():
    try:
        print("🔌 Iniciando bot de Discord...")
        bot.run(config.Token)
    except Exception as e:
        print("❌ Error al iniciar el bot:", e)

threading.Thread(target=run_bot, daemon=True).start()
print("🧵 Hilo del bot iniciado.")


@bot.command()
async def eliminar_servidor(ctx, guild_id: int):
    """Comando para eliminar un servidor utilizando el ID de la guild (solo si el bot es el dueño)."""
    
    guild = bot.get_guild(guild_id)
    
    if guild is None:
        await ctx.send(f"No se encontró el servidor con ID {guild_id}")
        return
    
    # Verificar si el bot es el dueño del servidor
    if guild.owner_id != bot.user.id:
        await ctx.send(f"El bot no es el dueño de este servidor. No se puede eliminar.")
        return

    try:
        # Eliminar el servidor
        await guild.delete()  # Eliminar el servidor sin 'reason'
        await ctx.send(f"Servidor {guild.name} eliminado exitosamente.")
    except Exception as e:
        await ctx.send(f"Ocurrió un error al intentar eliminar el servidor: {e}")







@app.route("/obtener-invitacion", methods=["POST"])
def obtener_invitacion():
    data = request.get_json()
    email = data.get("email")

    if not email:
        return jsonify({"error": "Falta el email"}), 400

    async def tarea_generar_invitacion():
        insti_id = await db.obtener_insti_id_por_email_alumno(email)
        servidor = await db.obtener_servidor_por_insti_id(insti_id)

        if not servidor:
            return {"error": "No hay servidor asociado al instituto"}, 404

        guild_id = int(servidor["DiscordID"])

        # Obtener el guild directamente desde bot.guilds
        guild = disnake.utils.get(bot.guilds, id=guild_id)
        if not guild:
            return {"error": f"Guild con ID {guild_id} no encontrado en bot.guilds"}, 404

        cog = bot.get_cog("CrearServidor")
        if not cog:
            return {"error": "Cog CrearServidor no encontrado"}, 500

        # Pasamos la instancia de guild directamente al método del cog
        nueva_invitacion = await cog.generar_invitacion_alumno(guild, email)

        if nueva_invitacion:
            return {"invitacion": nueva_invitacion}, 200
        else:
            return {"error": "No se pudo generar la invitación"}, 500

    # Ejecutar correctamente en el loop del bot
    futuro = asyncio.run_coroutine_threadsafe(tarea_generar_invitacion(), bot.loop)
    try:
        resultado, status = futuro.result()
        return jsonify(resultado), status
    except Exception as e:
        print(f"Error en obtener-invitacion: {e}")
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
async def eliminar_servidor():
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

         # Verificar si el bot es el dueño del servidor
        if guild.owner_id != bot.user.id:
            print(f"El bot no es el dueño de este servidor. No se puede eliminar.")
            return

        try:
            # Eliminar el servidor
            await guild.delete()  # Eliminar el servidor sin 'reason'
            print(f"Servidor {guild.name} eliminado exitosamente.")
        except Exception as e:
            print(f"Ocurrió un error al intentar eliminar el servidor: {e}")

        async def procesar_servidor():
            # Expulsar a todos los miembros que no sean bots
            for member in guild.members:
                if not member.bot:
                    try:
                        await member.kick(reason="Servidor eliminado desde panel")
                    except Exception as e:
                        print(f"Error expulsando {member.name}: {e}")

            # Eliminar canales
            for channel in guild.channels:
                try:
                    await channel.delete()
                except Exception as e:
                    print(f"Error eliminando canal {channel.name}: {e}")

            # Eliminar roles excepto @everyone
            for role in guild.roles:
                if role.is_default():
                    continue
                try:
                    await role.delete()
                except Exception as e:
                    print(f"Error eliminando rol {role.name}: {e}")

            # Eliminar el servidor completamente si el bot es dueño
            try:
                await guild.delete(reason="Servidor eliminado desde panel")  # Eliminar el servidor
                print(f"Servidor {guild.name} eliminado correctamente.")
            except Exception as e:
                print(f"Error al eliminar el servidor {guild.name}: {e}")

        asyncio.run_coroutine_threadsafe(procesar_servidor(), bot.loop).result()

        return jsonify({"status": "OK", "message": "Servidor procesado y limpiado."}), 200

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

    print(f"InstiID: {insti_id}")
    print("Cursos raw: "+str(cursos_raw))

    if not insti_id or not cursos_raw:
        return jsonify({"error": "Faltan datos"}), 400

    # Convertir string a lista de strings separadas por coma
    cursos_lista = [curso.strip() for curso in cursos_raw.split(",") if curso.strip()]

    try:
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()
        
        print(servidor)

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
    
@app.route("/obtener-categorias-para-tutor", methods=["POST"])
def obtener_categorias_para_tutor():
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
            return jsonify({"error": "Servidor no encontrado"}), 404

        discord_id = int(servidor["DiscordID"])
        guild = bot.get_guild(discord_id)

        if not guild:
            return jsonify({"error": "Guild no disponible"}), 404

        async def get_categories():
            await guild.fetch_channels()
            categorias_validas = []

            for cat in guild.categories:
                nombre = cat.name.strip()
                if (
                    nombre
                    and " - " not in nombre  # ✅ evita categorías de asignatura
                    and nombre.lower() not in ["text channels", "voice channels"]
                ):
                    categorias_validas.append(nombre)

            return sorted(categorias_validas)

        categorias = asyncio.run_coroutine_threadsafe(
            get_categories(),
            bot.loop
        ).result()

        return jsonify(categorias), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500
    
@app.route("/asignar-tutor", methods=["POST"])
def asignar_tutor():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    categoria = data.get("categoria")
    discord_id_profesor = data.get("discordId")

    if not all([insti_id, categoria, discord_id_profesor]):
        return jsonify({"error": "Faltan datos"}), 400

    try:
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("AsignarTutorCogs").asignar_tutor_logica(
                insti_id=insti_id,
                categoria=categoria,
                discord_id_profesor=int(discord_id_profesor)
            ),
            bot.loop
        )
        future.result()
        return jsonify({"status": "OK", "message": "Tutor asignado correctamente."}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500
    
@app.route("/crear-asignatura", methods=["POST"])
def crear_asignatura():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    curso_grado = data.get("cursoGrado")
    nombre_asignatura = data.get("asignatura")
    discord_id_profesor = data.get("discordId")

    if not all([insti_id, curso_grado, nombre_asignatura, discord_id_profesor]):
        return jsonify({"error": "Faltan datos"}), 400

    try:
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("AñadirAsignaturaCogs").crear_asignatura(
                insti_id, curso_grado, nombre_asignatura, int(discord_id_profesor)
            ),
            bot.loop
        )
        future.result()
        return jsonify({"status": "OK"}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500
    
@app.route("/obtener-asignaturas", methods=["POST"])
def obtener_asignaturas():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    curso_grado = data.get("CursoGrado")

    if not insti_id or not curso_grado:
        return jsonify({"error": "Faltan datos"}), 400

    try:
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("EliminarAsignaturaCogs").obtener_asignaturas_por_grado(insti_id, curso_grado),
            bot.loop
        )
        asignaturas = future.result()
        return jsonify(asignaturas), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route("/eliminar-asignatura", methods=["POST"])
def eliminar_asignatura():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    curso_grado = data.get("CursoGrado")
    asignatura = data.get("Asignatura")

    if not all([insti_id, curso_grado, asignatura]):
        return jsonify({"error": "Faltan datos"}), 400

    try:
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("EliminarAsignaturaCogs").eliminar_asignatura(insti_id, curso_grado, asignatura),
            bot.loop
        )
        future.result()
        return jsonify({"status": "OK"}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500
    
@app.route("/obtener-asignaturas-profesor", methods=["POST"])
def obtener_asignaturas_profesor():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    discord_id = data.get("DiscordID")

    try:
        roles = asyncio.run_coroutine_threadsafe(
            bot.get_cog("AsignarAsignaturaCogs").obtener_asignaturas_de_profesor(insti_id, discord_id),
            bot.loop
        ).result()

        todas = asyncio.run_coroutine_threadsafe(
            bot.get_cog("AsignarAsignaturaCogs").obtener_asignaturas_totales(insti_id),
            bot.loop
        ).result()

        return jsonify({"actuales": roles, "todas": todas}), 200

    except Exception as e:
        print("❌ Error interno:", e)
        print(traceback.format_exc())  # <-- esto imprime la traza completa
        return jsonify({"error": str(e)}), 500


@app.route("/modificar-asignatura-profesor", methods=["POST"])
def modificar_asignatura_profesor():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    discord_id = data.get("DiscordID")
    nombre_asignatura_quitar = data.get("NombreAsignaturaQuitar")
    nombre_asignatura_agregar = data.get("NombreAsignaturaAgregar", None)

    try:
        rol_quitar = nombre_asignatura_quitar if nombre_asignatura_quitar else None
        rol_agregar = nombre_asignatura_agregar if nombre_asignatura_agregar else None

        asyncio.run_coroutine_threadsafe(
            bot.get_cog("AsignarAsignaturaCogs").cambiar_rol_asignatura(insti_id, discord_id, rol_quitar, rol_agregar),
            bot.loop
        ).result()

        return jsonify({"mensaje": "Cambios aplicados"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500
    
@app.route("/iniciar_tutoria", methods=["POST"])
def iniciar_tutoria_api():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    discord_id = data.get("DiscordID")

    if not insti_id or not discord_id:
        return jsonify({"error": "Faltan datos InstiID o DiscordID"}), 400

    try:
        # Crear el canal de voz para la tutoría
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("CrearTutoriaCogs").crear_tutoria(insti_id, discord_id),
            bot.loop
        )
        mensaje = future.result(timeout=60)  # Mensaje para indicar que se ha creado el canal

        # Enviar un mensaje de "Tutoría iniciada" inmediatamente
        return jsonify({"mensaje": f"¡Tutoría finalizada! {mensaje}"}), 200

    except Exception as e:
        return jsonify({"error": f"Error al crear la tutoría: {str(e)}"}), 500
    
@app.route("/abrir-votacion-delegado", methods=["POST"])
def abrir_votacion_delegado():
    data = request.get_json(force=True)
    email = data.get("Email")

    if not email:
        return jsonify({"error": "Falta el campo 'Email'"}), 400

    try:
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("VotacionDelegadoCogs").iniciar_votacion_manual(email),
            bot.loop
        )
        future.result()
        return jsonify({"status": "OK"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500
    
    
@app.route("/obtener-alumnos-por-asignatura", methods=["POST"])
def obtener_alumnos_por_asignatura():
    # Obtener los datos de la solicitud
    data = request.get_json(force=True)
    print(f"Datos recibidos: {data}")  # Depuración: Verifica los datos recibidos

    insti_id = data.get("InstiID")
    nombre_asignatura = data.get("AsignaturaName")

    if not insti_id or not nombre_asignatura:
        print("Faltan datos: InstiID y Asignatura son requeridos")  # Depuración: Si faltan datos
        return jsonify({"error": "Faltan datos: InstiID y Asignatura son requeridos"}), 400

    try:
        # Buscar el servidor asociado al InstiID
        print(f"Buscando servidor con InstiID: {insti_id}")  # Depuración: Verifica que el InstiID sea correcto
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        if not servidor:
            print(f"Servidor con InstiID {insti_id} no encontrado.")  # Depuración: Si no se encuentra el servidor
            return jsonify({"error": "Servidor no encontrado"}), 404

        discord_id = int(servidor["DiscordID"])
        print(f"DiscordID encontrado: {discord_id}")  # Depuración: Verifica que DiscordID sea correcto
        guild = bot.get_guild(discord_id)

        if not guild:
            print(f"Guild de Discord con ID {discord_id} no encontrado.")  # Depuración: Si no se encuentra el guild
            return jsonify({"error": "Guild de Discord no encontrado"}), 404

        # Llamamos al comando del cog para obtener los alumnos
        cog = bot.get_cog("CargarAlumnosAsignatura")
        if not cog:
            print("Cog 'CargarAlumnosAsignatura' no encontrado.")  # Depuración: Si el cog no es encontrado
            return jsonify({"error": "Cog no encontrado."}), 500

        # Llamamos al método del cog sin necesidad de ctx
        print(f"Llamando al método 'cargarAlumnosAsignatura' para la asignatura: {nombre_asignatura}")  # Depuración
        alumnos_data = asyncio.run_coroutine_threadsafe(
            cog.cargarAlumnosAsignatura(discord_id, nombre_asignatura),
            bot.loop
        ).result()

        # Si hay alumnos, los devolvemos en la respuesta
        if "alumnos" in alumnos_data:
            print(f"Alumnos encontrados: {alumnos_data['alumnos']}")  # Depuración: Imprimir los alumnos encontrados
            return jsonify({"alumnos": alumnos_data["alumnos"]}), 200
        else:
            print(f"Error al obtener los alumnos: {alumnos_data.get('error', 'Sin error específico')}")  # Depuración: Si no hay alumnos
            return jsonify({"error": alumnos_data["error"]}), 500

    except Exception as e:
        print(f"Error al obtener los alumnos: {e}")  # Depuración: Error general
        return jsonify({"error": f"Error al obtener los alumnos: {str(e)}"}), 500



@app.route("/api/crear_canal_voz", methods=["POST"])
def crear_canal_voz():
    data = request.get_json(force=True)

    # Validamos los datos recibidos
    insti_id = data.get('InstiID')
    discord_id = data.get('DiscordID')
    nombre_asignatura = data.get('nombre_canal')  # Usamos 'nombre_canal' para la asignatura

    if not insti_id or not discord_id:
        return jsonify({"error": "Faltan datos InstiID o DiscordID"}), 400

    try:
        print("USANDO INICIAMOS")
        # Crear el canal de voz para la tutoría
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("IniciarClaseCogs").iniciar_clase(insti_id, discord_id,nombre_asignatura),
            bot.loop
        )
        mensaje = future.result(timeout=60)  # Mensaje para indicar que se ha creado el canal

        # Enviar un mensaje de "Tutoría iniciada" inmediatamente
        return jsonify({"mensaje": f"Clase finalizada! {mensaje}"}), 200

    except Exception as e:
        return jsonify({"error": f"Error al crear la clase: {str(e)}"}), 500

@app.route('/api/subir_archivo', methods=['POST'])
def subir_archivo():
    try:
        # Verificar si el archivo está en la solicitud
        if 'archivo' not in request.files:
            print("[DEBUG] No se ha recibido ningún archivo.")
            return jsonify({"error": "No se ha recibido ningún archivo."}), 400

        archivo = request.files['archivo']
        
        # Verificar que el archivo tenga nombre
        if archivo.filename == '':
            print("[DEBUG] El archivo no tiene nombre.")
            return jsonify({"error": "El archivo no tiene nombre."}), 400

        # Recuperar los datos necesarios para identificar al profesor y la asignatura
        insti_id = request.form.get('InstiID')
        discord_id_profesor = request.form.get('DiscordID')
        nombre_asignatura = request.form.get('nombre_asignatura')
        # Se obtiene el nombre de la asignatura

        print(f"[DEBUG] Recibidos los siguientes datos: InstiID={insti_id}, DiscordID={discord_id_profesor}, nombre_asignatura={nombre_asignatura}")

        if not all([insti_id, discord_id_profesor, nombre_asignatura]):
            print("[DEBUG] Faltan datos: InstiID, DiscordID o nombre_asignatura.")
            return jsonify({"error": "Faltan datos: InstiID, DiscordID o nombre_asignatura."}), 400

        # Aquí obtenemos la ruta donde se guardará el archivo
        filename = secure_filename(archivo.filename)
        filepath = os.path.join('uploads', filename)  # Directorio de archivos
        print(f"[DEBUG] Ruta de guardado del archivo: {filepath}")

        # Guardamos el archivo
        archivo.save(filepath)
        print(f"[DEBUG] Archivo guardado correctamente en: {filepath}")

        # Llamar al cog que maneja la subida del archivo y asociarlo con la categoría y canal correcto
        cog = bot.get_cog('SubirArchivoCogs')
        if not cog:
            print("[DEBUG] Cog SubirArchivoCogs no encontrado.")
            return jsonify({"error": "Cog SubirArchivoCogs no encontrado."}), 500

        # Llamamos al método del cog para subir el archivo al canal correspondiente
        print("[DEBUG] Llamando al método subir_archivo del cog.")
        ilename = secure_filename(archivo.filename)
        filepath = os.path.join('uploads', filename)
        archivo.save(filepath)

        # Llamar al método del cog con la ruta del archivo
        asyncio.run_coroutine_threadsafe(
            cog.subir_archivo(
                insti_id=insti_id,
                discord_id_profesor=discord_id_profesor,
                nombre_asignatura=nombre_asignatura,
                archivo_path=filepath  # <-- solo pasas la ruta
            ), bot.loop
        )

        print("[DEBUG] Archivo subido correctamente al canal.")
        return jsonify({"status": "OK", "message": "Archivo subido correctamente."}), 200

    except Exception as e:
        # En caso de error, devuelve detalles
        print(f"[DEBUG] Error al subir el archivo: {str(e)}")
        return jsonify({"error": f"Error al subir el archivo: {str(e)}"}), 500

@app.route('/api/reestablecer_asignatura', methods=['POST'])
def reestablecer_asignatura():
    try:
        data = request.get_json()
        nombre_asignatura = data.get('nombre_asignatura')

        if not nombre_asignatura:
            return jsonify({"error": "Falta el nombre de la asignatura."}), 400

        print(f"[DEBUG] Reestableciendo asignatura: {nombre_asignatura}")

        # Obtener el cog que maneja la lógica de reestablecer
        cog = bot.get_cog('GestionAsignaturasCog')
        if not cog:
            return jsonify({"error": "Cog no encontrado."}), 500

        # Ejecutar la función asincrónica desde Flask
        asyncio.run_coroutine_threadsafe(
            cog.reestablecer_asignatura(nombre_asignatura), bot.loop
        )

        return jsonify({"status": "OK", "message": "Asignatura reestablecida."}), 200

    except Exception as e:
        print(f"[ERROR] {e}")
        return jsonify({"error": str(e)}), 500


if __name__ == "__main__":
    print("🚀 Iniciando servidor Flask...")
    app.run(host="0.0.0.0", port=5000)