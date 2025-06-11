from disnake.ext import commands
import disnake
import unicodedata

def normalize_str(s: str) -> str:
    return unicodedata.normalize('NFKD', s).encode('ascii', 'ignore').decode('ascii').strip().lower()

class AñadirCursosCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    async def buscar_categoria_por_nombre(self, guild: disnake.Guild, nombre_rol: str):
        await guild.fetch_channels()  # Refrescar cache
        nombre_rol_clean = normalize_str(nombre_rol)
        for cat in guild.categories:
            if normalize_str(cat.name) == nombre_rol_clean:
                return cat
        return None

    def canal_existe(self, category: disnake.CategoryChannel, canal_nombre: str) -> bool:
        # --- MEJORA: Se normaliza también el nombre del canal a buscar ---
        canal_nombre_clean = normalize_str(canal_nombre)
        for c in category.channels:
            if normalize_str(c.name) == canal_nombre_clean:
                return True
        return False

    # --- CORRECCIÓN: La función ahora acepta un guild_id para ser más robusta ---
    async def configurar_servidor_api(self, guild_id: int, cursos: list):
        try:
            guild = await self.bot.fetch_guild(guild_id)
            if guild is None:
                # Esto solo pasaría si el ID es válido pero el bot no está en el servidor.
                print(f"❌ Error crítico: No se encontró el servidor con ID {guild_id} o el bot no es miembro.")
                raise ValueError(f"No se pudo encontrar el servidor con ID {guild_id}")
        except disnake.NotFound:
            print(f"❌ Error crítico: El servidor con ID {guild_id} no existe.")
            raise ValueError(f"El servidor con ID {guild_id} no existe.")

        # --- MEJORA: Refrescar la lista de roles una sola vez al principio ---
        await guild.fetch_roles()

        for curso_str in cursos:
            # --- MEJORA: Limpiar espacios y caracteres invisibles del nombre del curso ---
            curso_str_limpio = curso_str.strip()
            try:
                grado, curso_nombre = curso_str_limpio.split(" ", 1)
            except ValueError:
                print(f"Formato inválido para curso: {curso_str_limpio}")
                continue

            nombre_rol = f"{grado} {curso_nombre}"

            # Busca o crea el rol para el curso
            role = disnake.utils.get(guild.roles, name=nombre_rol)
            if role is None:
                role = await guild.create_role(name=nombre_rol)
                print(f"Rol '{nombre_rol}' creado.")
            else:
                print(f"Rol '{nombre_rol}' ya existe.")

            # Busca los roles de permisos
            admin_role = disnake.utils.get(guild.roles, name="admin")
            jefe_role = disnake.utils.get(guild.roles, name="jefe")
            tutor_role = disnake.utils.get(guild.roles, name="tutor")
            delegado_role = disnake.utils.get(guild.roles, name="delegado")

            if None in [admin_role, jefe_role, tutor_role, delegado_role]:
                print("❌ Uno o más roles requeridos no existen: admin, jefe, tutor o delegado. Saltando este curso.")
                continue

            # Busca o crea la categoría para el curso
            category = await self.buscar_categoria_por_nombre(guild, nombre_rol)
            if category is None:
                overwrites_cat = {
                    guild.default_role: disnake.PermissionOverwrite(view_channel=False),
                    admin_role: disnake.PermissionOverwrite(view_channel=True),
                    jefe_role: disnake.PermissionOverwrite(view_channel=True),
                    role: disnake.PermissionOverwrite(view_channel=True)
                }
                category = await guild.create_category(nombre_rol, overwrites=overwrites_cat)
                print(f"Categoría '{nombre_rol}' creada.")
            else:
                print(f"Categoría '{nombre_rol}' ya existe.")

            # --- MEJORA: Lógica de creación de canales refactorizada para evitar duplicación ---
            
            # Permisos para canales normales (alumnos del curso)
            overwrites_normal = {
                category.guild.default_role: disnake.PermissionOverwrite(view_channel=False),
                admin_role: disnake.PermissionOverwrite(view_channel=True),
                jefe_role: disnake.PermissionOverwrite(view_channel=True),
                role: disnake.PermissionOverwrite(view_channel=True)
            }

            # Permisos para canales de delegado (solo tutores y delegados)
            overwrites_delegado = {
                category.guild.default_role: disnake.PermissionOverwrite(view_channel=False),
                admin_role: disnake.PermissionOverwrite(view_channel=True),
                jefe_role: disnake.PermissionOverwrite(view_channel=True),
                tutor_role: disnake.PermissionOverwrite(view_channel=True),
                delegado_role: disnake.PermissionOverwrite(view_channel=True)
            }

            # Lista de canales a crear si no existen
            canales_a_crear = [
                {"nombre": "📌・general", "tipo": "texto", "perms": overwrites_normal},
                {"nombre": "❓・dudas", "tipo": "texto", "perms": overwrites_normal},
                {"nombre": "📌・delegado - texto", "tipo": "texto", "perms": overwrites_delegado},
                {"nombre": "📌・delegado - voz", "tipo": "voz", "perms": overwrites_delegado},
            ]

            for canal_info in canales_a_crear:
                if not self.canal_existe(category, canal_info["nombre"]):
                    if canal_info["tipo"] == "texto":
                        await category.create_text_channel(canal_info["nombre"], overwrites=canal_info["perms"])
                    elif canal_info["tipo"] == "voz":
                        await category.create_voice_channel(canal_info["nombre"], overwrites=canal_info["perms"])
                    print(f"Canal '{canal_info['nombre']}' creado en categoría '{nombre_rol}'")

            # Asignar permisos explícitos al rol en la categoría
            permisos_avanzados = disnake.PermissionOverwrite(
                read_messages=True,
                send_messages=True,
                attach_files=True,
                connect=True,
                speak=True
            )
            await category.set_permissions(role, overwrite=permisos_avanzados)
            print(f"Permisos asignados para rol '{nombre_rol}' en categoría '{nombre_rol}'.")

        print("Configuración completada para todos los cursos.")

def setup(bot):
    bot.add_cog(AñadirCursosCogs(bot))