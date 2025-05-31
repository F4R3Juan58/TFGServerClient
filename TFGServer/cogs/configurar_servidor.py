from disnake.ext import commands
import disnake
from db_connection import Database
import unicodedata

def normalize_str(s: str) -> str:
    # Normaliza quitando acentos y espacios, para comparaciones consistentes
    return unicodedata.normalize('NFKD', s).encode('ascii', 'ignore').decode('ascii').strip().lower()

class ConfigurarServidorCog(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    async def buscar_categoria_por_nombre(self, guild: disnake.Guild, nombre_rol: str):
        await guild.fetch_channels()  # Forzar refresco del cache de canales y categorías
        nombre_rol_clean = normalize_str(nombre_rol)
        for cat in guild.categories:
            if normalize_str(cat.name) == nombre_rol_clean:
                return cat
        return None

    def canal_existe(self, category: disnake.CategoryChannel, canal_nombre: str) -> bool:
        canal_nombre_clean = canal_nombre.strip().lower()
        for c in category.channels:
            if c.name.strip().lower() == canal_nombre_clean:
                return True
        return False

    async def configurar_servidor_api(self, guild: disnake.Guild, cursos: list):
        for curso_obj in cursos:
            grado = curso_obj.get("grado")
            curso_nombre = curso_obj.get("curso")

            rol_info = await self.db.obtener_rol_por_grado_y_curso(grado, curso_nombre)
            if not rol_info:
                print(f"No se encontró rol para el curso {grado} {curso_nombre}.")
                continue

            nombre_rol = rol_info['NombreRol'].strip()

            # Buscar o crear rol
            role = disnake.utils.get(guild.roles, name=nombre_rol)
            if role is None:
                role = await guild.create_role(name=nombre_rol)
                print(f"Rol '{nombre_rol}' creado.")
            else:
                print(f"Rol '{nombre_rol}' ya existe.")

            # Buscar o crear categoría usando la versión async y normalizada
            category = await self.buscar_categoria_por_nombre(guild, nombre_rol)
            if category is None:
                category = await guild.create_category(nombre_rol)
                print(f"Categoría '{nombre_rol}' creada.")

                # Crear canales por defecto
                await guild.create_text_channel("❓・dudas", category=category)
                await guild.create_text_channel("📌・general", category=category)
                print(f"Canales por defecto creados en '{nombre_rol}'")
            else:
                print(f"Categoría '{nombre_rol}' ya existe.")

                if not self.canal_existe(category, "📌・general"):
                    await guild.create_text_channel("📌・general", category=category)
                    print(f"Canal '📌・general' creado en categoría '{nombre_rol}'")

                if not self.canal_existe(category, "❓・dudas"):
                    await guild.create_text_channel("❓・dudas", category=category)
                    print(f"Canal '❓・dudas' creado en categoría '{nombre_rol}'")

            # Definir permisos que quieres asignar al rol en la categoría
            permisos_avanzados = disnake.PermissionOverwrite(
                read_messages=True,
                send_messages=True,
                attach_files=True,
                connect=True
            )

            # Asignar permisos avanzados en la categoría para el rol
            await category.set_permissions(role, overwrite=permisos_avanzados)
            print(f"Permisos avanzados asignados para rol '{nombre_rol}' en la categoría '{nombre_rol}'.")

        print("Configuración completada para todos los cursos.")

def setup(bot):
    bot.add_cog(ConfigurarServidorCog(bot))