from disnake.ext import commands
import disnake
import unicodedata

def normalize_str(s: str) -> str:
    return unicodedata.normalize('NFKD', s).encode('ascii', 'ignore').decode('ascii').strip().lower()

class EliminarCursosCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    async def buscar_categoria_por_nombre(self, guild: disnake.Guild, nombre_rol: str):
        await guild.fetch_channels()  # Refrescar cache
        nombre_rol_clean = normalize_str(nombre_rol)
        for cat in guild.categories:
            if normalize_str(cat.name) == nombre_rol_clean:
                return cat
        return None

    async def eliminar_categoria_y_canales(self, guild: disnake.Guild, categoria_nombre: str):
        category = await self.buscar_categoria_por_nombre(guild, categoria_nombre)
        if category is None:
            print(f"Categoría '{categoria_nombre}' no encontrada, se continúa.")
            return

        # Eliminar canales dentro de la categoría
        for channel in category.channels:
            try:
                await channel.delete()
                print(f"Canal '{channel.name}' eliminado.")
            except Exception as e:
                print(f"Error eliminando canal '{channel.name}': {e}")

        # Eliminar categoría
        try:
            await category.delete()
            print(f"Categoría '{categoria_nombre}' eliminada.")
        except Exception as e:
            print(f"Error eliminando categoría '{categoria_nombre}': {e}")

    async def eliminar_rol_por_nombre(self, guild: disnake.Guild, nombre_rol: str):
        role = disnake.utils.get(guild.roles, name=nombre_rol)
        if role is None:
            print(f"Rol '{nombre_rol}' no encontrado, se continúa.")
            return
        try:
            await role.delete()
            print(f"Rol '{nombre_rol}' eliminado.")
        except Exception as e:
            print(f"Error eliminando rol '{nombre_rol}': {e}")

    async def eliminar_cursos(self, guild: disnake.Guild, cursos: list):
        for curso_str in cursos:
            try:
                grado, curso_nombre = curso_str.split(" ", 1)
            except ValueError:
                print(f"Formato inválido para curso: {curso_str}")
                continue

            nombre_rol = f"{grado} {curso_nombre}"

            # Primero eliminar rol
            await self.eliminar_rol_por_nombre(guild, nombre_rol)

            # Luego eliminar categoría y canales
            await self.eliminar_categoria_y_canales(guild, nombre_rol)

        print("Eliminación completada para todos los cursos.")

def setup(bot):
    bot.add_cog(EliminarCursosCogs(bot))
