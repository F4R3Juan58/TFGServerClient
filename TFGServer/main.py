from flask import Flask, request, jsonify
import threading
import traceback  # arriba del archivo
import asyncio
import disnake
from disnake.ext import commands
import random
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
                await guild.delete()  # Eliminar el servidor
                print(f"Servidor {guild.name} eliminado correctamente.")
            except Exception as e:
                print(f"Error al eliminar el servidor {guild.name}: {e}")

        asyncio.run_coroutine_threadsafe(procesar_servidor(), bot.loop).result()

        return jsonify({"status": "OK", "message": "Servidor procesado y limpiado."}), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500

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
    print("Cursos raw: " + str(cursos_raw))

    if not insti_id or not cursos_raw:
        return jsonify({"error": "Faltan datos"}), 400

    # Convertir string a lista de strings separadas por coma
    cursos_lista = [curso.strip() for curso in cursos_raw.split(",") if curso.strip()]

    try:
        # 1. Obtener la información del servidor desde la base de datos
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()
        
        print(servidor)

        if not servidor:
            return jsonify({"error": "No existe servidor asociado al instituto."}), 404

        # 2. --- CORRECCIÓN CLAVE ---
        # Obtenemos el ID de Discord como un entero.
        discord_id = int(servidor['DiscordID'])
        
        # Ya NO necesitamos la línea `guild = bot.get_guild(discord_id)`. La eliminamos.
        # El Cog se encargará de obtener el 'guild' de forma segura usando fetch_guild.

        cog = bot.get_cog("AñadirCursosCogs")
        if not cog:
            return jsonify({"error": "No se encontró el cog AñadirCursosCogs."}), 500

        # 3. --- CORRECCIÓN CLAVE ---
        # Llamamos a la función del Cog pasándole el ID numérico (discord_id), no el objeto guild.
        asyncio.run_coroutine_threadsafe(
            cog.configurar_servidor_api(discord_id, cursos_lista),
            bot.loop
        ).result()

        return jsonify({"status": "OK", "message": "Servidor configurado correctamente."}), 200

    except Exception as e:
        # Imprime el traceback completo en la consola del servidor para un mejor debugging
        import traceback
        traceback.print_exc()
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
    print("Petición recibida en /asignar-tutor")

    data = request.get_json(force=True)
    print("Datos recibidos:", data)

    insti_id = data.get("InstiID")
    categoria = data.get("categoria")
    discord_id_profesor = data.get("discordId")

    if not all([insti_id, categoria, discord_id_profesor]):
        print("Faltan datos:", insti_id, categoria, discord_id_profesor)
        return jsonify({"error": "Faltan datos"}), 400

    try:
        print("Llamando a asignar_tutor_logica con:",
              "InstiID:", insti_id,
              "Categoria:", categoria,
              "DiscordID:", discord_id_profesor)

        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("AsignarTutorCogs").asignar_tutor_logica(
                insti_id=insti_id,
                categoria=categoria,
                discord_id_profesor=int(discord_id_profesor)
            ),
            bot.loop
        )
        result = future.result()
        print("Resultado de asignar_tutor_logica:", result)

        return jsonify({"status": "OK", "message": "Tutor asignado correctamente."}), 200

    except Exception as e:
        print("Error al asignar tutor:", str(e))
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
    from flask import request, jsonify
    import asyncio

    # Obtener los datos de la solicitud
    data = request.get_json(force=True)
    print(f"Datos recibidos: {data}")

    insti_id = data.get("InstiID")
    nombre_asignatura = data.get("AsignaturaName")
    discord_id_profesor = int(data.get("DiscordID", 0))  # opcional

    if not insti_id or not nombre_asignatura:
        print("Faltan datos: InstiID y Asignatura son requeridos")
        return jsonify({"error": "Faltan datos: InstiID y Asignatura son requeridos"}), 400

    try:
        # Buscar el servidor asociado al InstiID
        print(f"Buscando servidor con InstiID: {insti_id}")
        servidor = asyncio.run_coroutine_threadsafe(
            db.obtener_servidor_por_insti_id(insti_id),
            bot.loop
        ).result()

        if not servidor:
            print(f"Servidor con InstiID {insti_id} no encontrado.")
            return jsonify({"error": "Servidor no encontrado"}), 404

        discord_id_guild = int(servidor["DiscordID"])
        print(f"DiscordID encontrado: {discord_id_guild}")
        guild = bot.get_guild(discord_id_guild)

        if not guild:
            print(f"Guild de Discord con ID {discord_id_guild} no encontrado.")
            return jsonify({"error": "Guild de Discord no encontrado"}), 404

        profesor = guild.get_member(discord_id_profesor) if discord_id_profesor else None
        if not profesor:
            print("Profesor no encontrado en el servidor.")
            return jsonify({"error": "Profesor no encontrado en el servidor"}), 404

        # Buscar un rol del profesor que contenga la asignatura en su nombre
        rol_asignatura = None
        for rol in profesor.roles:
            if nombre_asignatura.lower() in rol.name.lower():
                rol_asignatura = rol
                break

        if not rol_asignatura:
            print(f"No se encontró un rol del profesor que contenga la asignatura '{nombre_asignatura}'")
            return jsonify({"error": f"No se encontró un rol del profesor que contenga la asignatura '{nombre_asignatura}'"}), 404

        # Buscar todos los miembros con ese rol, excepto el profesor
        miembros_con_rol = [
            member for member in guild.members
            if rol_asignatura in member.roles and member.id != discord_id_profesor
        ]

        alumnos = [member.name for member in miembros_con_rol]

        if alumnos:
            print(f"Alumnos encontrados: {alumnos}")
            return jsonify({"alumnos": alumnos}), 200
        else:
            print(f"No se encontraron alumnos con el rol '{rol_asignatura.name}'")
            return jsonify({"error": f"No se encontraron alumnos con el rol '{rol_asignatura.name}'"}), 404

    except Exception as e:
        print(f"Error al obtener los alumnos: {e}")
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
    
@app.route("/api/alumnos_conectados", methods=["POST"])
async def alumnos_conectados():
    data = request.get_json()
    profesor_id = int(data.get("profesor_discord_id"))

    for guild in bot.guilds:
        profesor = guild.get_member(profesor_id)
        if profesor and profesor.voice and profesor.voice.channel:
            canal = profesor.voice.channel
            alumnos = [
                {
                    "nombre": miembro.display_name,
                    "discord_id": str(miembro.id)
                }
                for miembro in canal.members if miembro.id != profesor_id
            ]
            return jsonify(alumnos), 200

    return jsonify([]), 200


@app.route("/api/expulsar_alumno_clase", methods=["POST"])
def expulsar_alumno_clase():
    data = request.get_json()
    alumno_id = int(data.get("alumno_id"))
    profesor_id = int(data.get("profesor_id"))

    async def expulsar():
        for guild in bot.guilds:
            profesor = guild.get_member(profesor_id)
            alumno = guild.get_member(alumno_id)

            if not profesor or not alumno:
                continue
            if not profesor.voice or not alumno.voice:
                continue
            canal = profesor.voice.channel
            if canal.id != alumno.voice.channel.id:
                continue

            try:
                # Expulsar
                await alumno.move_to(None, reason=f"Expulsado por el profesor {profesor.display_name}")
                print("✅ Alumno expulsado")

                # Denegar permiso de conexión en el canal para ese usuario
                overwrite = canal.overwrites_for(alumno)
                overwrite.connect = False
                await canal.set_permissions(alumno, overwrite=overwrite)
                print(f"🚫 Permiso de conexión revocado para {alumno.display_name}")
            except Exception as e:
                print(f"❗ Error al expulsar o cambiar permisos: {str(e)}")

    asyncio.run_coroutine_threadsafe(expulsar(), bot.loop)
    return jsonify({"mensaje": "Petición de expulsión enviada"}), 200

@app.route("/api/enviar_cuestionarios", methods=["POST"])
def enviar_cuestionarios():
    data = request.get_json()

    # Extraemos los datos enviados
    instiID = data.get("InstiID")
    profesorID = data.get("DiscordID")
    asignatura = data.get("Asignatura")
    cuestionarios = data.get("Cuestionarios", [])

    # Debug: Imprimir los datos recibidos
    print(f"Datos recibidos: InstiID={instiID}, ProfesorID={profesorID}, Asignatura={asignatura}, Cuestionarios={cuestionarios}")

    # Obtener servidor de Discord asociado al instituto
    servidor = asyncio.run_coroutine_threadsafe(
        db.obtener_servidor_por_insti_id(instiID),
        bot.loop
    ).result()

    if not servidor:
        print("Error: No existe servidor asociado al instituto.")  # Debug
        return jsonify({"error": "No existe servidor asociado al instituto."}), 404

    discord_id = int(servidor['DiscordID'])
    guild = bot.get_guild(discord_id)

    # Comprobamos si los datos esenciales están presentes
    if not instiID or not profesorID or not asignatura or not cuestionarios:
        print("Error: Faltan datos necesarios.")  # Debug
        return jsonify({"error": "Faltan datos necesarios"}), 400

    # Buscar el canal de Discord asociado con la asignatura
    try:
        print(f"Buscando canal para la asignatura: {asignatura}")  # Debug
        channel = obtener_canal_por_asignatura(guild, asignatura)
    except Exception as e:
        print(f"Error al obtener canal: {str(e)}")  # Debug
        return jsonify({"error": str(e)}), 404

    # Enviar cada cuestionario a Discord
    for cuestionario in cuestionarios:
        pregunta = cuestionario.get("Pregunta")
        respuestas = [
            cuestionario.get("Respuesta1"),
            cuestionario.get("Respuesta2"),
            cuestionario.get("Respuesta3"),
            cuestionario.get("Respuesta4")
        ]
        respuesta_correcta = cuestionario.get("Correcta")

        # Debug: Imprimir los detalles del cuestionario
        print(f"Enviando pregunta: {pregunta}, Respuestas: {respuestas}, Respuesta Correcta: {respuesta_correcta}")

        # Enviar a Discord
        bot.loop.create_task(enviar_pregunta_discord(pregunta, respuestas, respuesta_correcta, asignatura, channel))

    return jsonify({"message": "Cuestionarios enviados exitosamente"}), 200


async def enviar_pregunta_discord(pregunta, respuestas, respuesta_correcta, asignatura, channel):
    # Desordenar las respuestas
    random.shuffle(respuestas)

    # Crear botones para cada respuesta
    botones = [
        disnake.ui.Button(
            label=res,
            custom_id=f"respuesta_{i}_{'correcta' if res == respuesta_correcta else 'incorrecta'}",
            style=disnake.ButtonStyle.primary if res == respuesta_correcta else disnake.ButtonStyle.secondary,
            disabled=False  # Los botones son habilitados
        )
        for i, res in enumerate(respuestas)
    ]

    # Crear un embed para la pregunta
    embed = disnake.Embed(title=pregunta, color=disnake.Color.blue())
    for idx, respuesta in enumerate(respuestas):
        embed.add_field(name=f"Opción {idx + 1}", value=respuesta, inline=False)

    # Enviar la pregunta como un mensaje al canal de Discord
    try:
        print(f"Enviando pregunta al canal: {channel.name}")  # Debug
        await channel.send(embed=embed, components=[botones])  # Enviar el embed con botones
        print("Pregunta enviada a Discord exitosamente.")  # Debug
    except Exception as e:
        print(f"Error al enviar el mensaje a Discord: {e}")  # Debug


def obtener_canal_por_asignatura(guild, asignatura):
    # Buscar roles con el nombre de la asignatura
    roles = [role.name for role in guild.roles if '-' in role.name and asignatura.lower() in role.name.lower()]
    print(f"Roles encontrados: {roles}")  # Debug
    if roles:
        print(f"Roles que contienen '-' y coinciden con '{asignatura}': {', '.join(roles)}")
    else:
        raise Exception(f"No se encontraron roles con '-' y que contengan '{asignatura}'.")

    # Buscar categorías con el nombre de la asignatura
    categorias = [categoria for categoria in guild.categories if '-' in categoria.name and asignatura.lower() in categoria.name.lower()]
    print(f"Categorías encontradas: {categorias}")  # Debug
    if categorias:
        print(f"Categorías que contienen '-' y coinciden con '{asignatura}': {', '.join(categoria.name for categoria in categorias)}")
    else:
        raise Exception(f"No se encontraron categorías con '-' y que contengan '{asignatura}'.")

    # Buscar el canal "📝・exámenes" dentro de la categoría asociada a la asignatura
    for categoria in categorias:
        for canal in categoria.text_channels:
            if canal.name == "📝・exámenes":
                print(f"Canal encontrado: {canal.name}")  # Debug
                return canal

    raise Exception(f"No se encontró el canal '📝・exámenes' en la categoría de '{asignatura}'.")


# Evento para manejar la respuesta de los botones
@bot.event
async def on_button_click(interaction: disnake.MessageInteraction):
    # Aquí verificamos cuál fue el botón presionado
    boton_presionado = interaction.component.custom_id

    # Verificamos si la respuesta es la correcta
    if "correcta" in boton_presionado:
        await interaction.response.send_message("¡Respuesta correcta!", ephemeral=True)
    else:
        await interaction.response.send_message("Respuesta incorrecta. Intenta de nuevo.", ephemeral=True)

    # Desactivar todos los botones después de una respuesta
    new_buttons = [
        disnake.ui.Button(
            label=btn.label,
            custom_id=btn.custom_id,
            style=btn.style,
            disabled=True  # Desactivar el botón después de la respuesta
        )
        for btn in interaction.message.components[0]
    ]

    # Actualizar el mensaje con los botones desactivados
    await interaction.message.edit(components=[new_buttons])
    
@app.route("/crear-canal-tutoria", methods=["POST"])
def crear_canal_tutoria():
    data = request.get_json()
    alumno_id = data.get("alumno_id")
    insti_id = data.get("insti_id")
    profesor_id = data.get("profesor_id")

    if not all([alumno_id, insti_id, profesor_id]):
        return jsonify({"error": "Faltan datos: alumno_id, insti_id o profesor_id"}), 400

    try:
        # Llamar al comando del cog para crear el canal de voz
        futuro = asyncio.run_coroutine_threadsafe(
            bot.get_cog("CrearCanalVozCogs").crear_canal_voz(insti_id, profesor_id, alumno_id),
            bot.loop
        )
        futuro.result()  # Esperar a que se ejecute el comando
        return jsonify({"status": "OK", "message": "Canal de voz creado correctamente."}), 200
    except Exception as e:
        return jsonify({"error": f"Error al crear el canal de voz: {str(e)}"}), 500
    
@app.route("/crear-canal-texto", methods=["POST"])
def crear_canal_texto():
    data = request.get_json()
    alumno_id = data.get("alumno_id")
    insti_id = data.get("insti_id")
    profesor_id = data.get("profesor_id")

    if not all([alumno_id, insti_id, profesor_id]):
        return jsonify({"error": "Faltan datos: alumno_id, insti_id o profesor_id"}), 400

    try:
        # Llamar al comando del cog para crear el canal de texto
        futuro = asyncio.run_coroutine_threadsafe(
            bot.get_cog("CrearCanalTextoCogs").crear_canal_texto(insti_id, profesor_id, alumno_id),
            bot.loop
        )
        futuro.result()  # Esperar a que se ejecute el comando
        return jsonify({"status": "OK", "message": "Canal de texto creado correctamente."}), 200
    except Exception as e:
        return jsonify({"error": f"Error al crear el canal de texto: {str(e)}"}), 500
    
@app.route("/crear-canal-fct", methods=["POST"])
def crear_canal_fct():
    data = request.get_json()
    alumno_id = data.get("alumno_id")
    insti_id = data.get("insti_id")
    profesor_id = data.get("profesor_id")

    if not all([alumno_id, insti_id, profesor_id]):
        return jsonify({"error": "Faltan datos: alumno_id, insti_id o profesor_id"}), 400

    try:
        # Llamar al comando del cog para crear el canal FCT
        futuro = asyncio.run_coroutine_threadsafe(
            bot.get_cog("CrearCanalFCTCogs").crear_canal_fct(insti_id, profesor_id, alumno_id),
            bot.loop
        )
        futuro.result()  # Esperar a que se ejecute el comando
        return jsonify({"status": "OK", "message": "Canal FCT creado correctamente."}), 200
    except Exception as e:
        return jsonify({"error": f"Error al crear el canal FCT: {str(e)}"}), 500
    
@app.route("/crear-canal-tfg", methods=["POST"])
def crear_canal_tfg():
    data = request.get_json()
    alumno_id = data.get("alumno_id")
    insti_id = data.get("insti_id")
    profesor_id = data.get("profesor_id")

    if not all([alumno_id, insti_id, profesor_id]):
        return jsonify({"error": "Faltan datos: alumno_id, insti_id o profesor_id"}), 400

    try:
        # Llamar al comando del cog para crear el canal TFG
        futuro = asyncio.run_coroutine_threadsafe(
            bot.get_cog("CrearCanalTFGCogs").crear_canal_tfg(insti_id, profesor_id, alumno_id),
            bot.loop
        )
        futuro.result()  # Esperar a que se ejecute el comando
        return jsonify({"status": "OK", "message": "Canal TFG creado correctamente."}), 200
    except Exception as e:
        return jsonify({"error": f"Error al crear el canal TFG: {str(e)}"}), 500
    
@app.route("/obtener-tutorias-profesor", methods=["POST"])
def obtener_tutorias_profesor():
    data = request.get_json()
    insti_id = data.get("insti_id")
    profesor_id = data.get("profesor_id")

    if not all([insti_id, profesor_id]):
        return jsonify({"error": "Faltan datos: insti_id o profesor_id"}), 400

    try:
        # Llamar al comando del cog para obtener las tutorías
        futuro = asyncio.run_coroutine_threadsafe(
            bot.get_cog("RellenarTutoriasCogs").obtener_tutorias(insti_id, profesor_id),
            bot.loop
        )
        tutorias = futuro.result()  # Esperar a que se ejecute el comando
        return jsonify({"status": "OK", "tutorias": tutorias}), 200
    except Exception as e:
        return jsonify({"error": f"Error al obtener las tutorías: {str(e)}"}), 500
    
@app.route("/eliminar-tutoria", methods=["POST"])
def eliminar_tutoria():
    data = request.get_json()
    insti_id = data.get("insti_id")
    profesor_id = data.get("profesor_id")
    nombre_tutoria = data.get("nombre_tutoria")

    if not all([insti_id, profesor_id, nombre_tutoria]):
        return jsonify({"error": "Faltan datos: insti_id, profesor_id o nombre_tutoria"}), 400

    try:
        # Verificar que el cog 'EliminarTutoriaCogs' está cargado
        cog = bot.get_cog("EliminarTutoriaCogs")
        if cog is None:
            print("El cog 'EliminarTutoriaCogs' no está cargado.")
            return jsonify({"error": "El cog 'EliminarTutoriaCogs' no está cargado."}), 500

        # Llamar al comando del cog para eliminar la tutoría
        futuro = asyncio.run_coroutine_threadsafe(
            cog.eliminar_tutoria(None, insti_id, profesor_id, nombre_tutoria),  # Pasamos None como ctx
            bot.loop
        )
        futuro.result()  # Esperar a que se ejecute el comando
        return jsonify({"status": "OK", "message": f"Tutoría '{nombre_tutoria}' eliminada correctamente."}), 200
    except Exception as e:
        return jsonify({"error": f"Error al eliminar la tutoría: {str(e)}"}), 500
    
@app.route("/asignar-delegado", methods=["POST"])
def asignar_delegado():
    data = request.get_json(force=True)
    insti_id = data.get("InstiID")
    discord_id = data.get("DiscordID")

    if not insti_id or not discord_id:
        return jsonify({"error": "Faltan datos (InstiID o DiscordID)"}), 400

    try:
        future = asyncio.run_coroutine_threadsafe(
            bot.get_cog("AsignarDelegadoCogs").asignar_delegado_logica(
                int(insti_id), int(discord_id)
            ),
            bot.loop
        )
        future.result(timeout=15)
        return jsonify({"status": "OK", "message": "Delegado asignado correctamente"}), 200

    except Exception as e:
        print(f"❌ Error en asignar-delegado: {e}")
        return jsonify({"error": str(e)}), 500

if __name__ == "__main__":
    print("🚀 Iniciando servidor Flask...")
    app.run(host="0.0.0.0", port=5000)
    