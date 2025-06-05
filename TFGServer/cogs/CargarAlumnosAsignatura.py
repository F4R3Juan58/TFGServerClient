import disnake
from disnake.ext import commands
from db_connection import Database

class CargarAlumnosAsignatura(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    async def cargarAlumnosAsignatura(self, guild_id: int, nombre_asignatura: str):
        """Busca los alumnos con el rol correspondiente al curso y devuelve la lista"""
        print(f"Buscando guild con ID: {guild_id}")  # Depuración: Verifica que el guild_id sea correcto

        # Obtener el guild (servidor de Discord)
        guild = self.bot.get_guild(guild_id)  

        if not guild:
            print(f"⚠️ Guild con ID {guild_id} no encontrado o el bot no está en ese servidor.")
            return {"error": "Guild no encontrado"}

        print(f"Guild encontrado: {guild.name}")  # Depuración: Verifica que el guild fue encontrado

        # Filtrar las categorías que contienen el nombre de la asignatura en su nombre
        categoria_asignatura = None
        print(f"Buscando categoría con la asignatura: {nombre_asignatura}")  # Depuración: Verifica la asignatura

        for category in guild.categories:
            print(f"Revisando categoría: {category.name}")  # Depuración: Imprimir nombre de la categoría
            if f" - {nombre_asignatura}" in category.name:
                categoria_asignatura = category
                break

        if not categoria_asignatura:
            print(f"⚠️ No se encontró la categoría para la asignatura: {nombre_asignatura}")
            return {"error": f"No se encontró la categoría para la asignatura: {nombre_asignatura}"}

        # Buscar el nombre del curso sin la asignatura (lo que está antes de " -")
        curso_nombre = categoria_asignatura.name.split(' - ')[0]
        print(f"Nombre del curso: {curso_nombre}")  # Depuración: Verifica el nombre del curso

        rol_curso = f"{curso_nombre}"  # El rol del curso sin la asignatura
        print(f"Buscando miembros con el rol de curso: {rol_curso}")  # Depuración: Verifica el rol del curso

        # Obtenemos a todos los miembros que tienen el rol del curso
        miembros_con_rol = [
            member for member in guild.members
            if any(role.name == rol_curso for role in member.roles)
        ]

        if miembros_con_rol:
            # Retornar los miembros con el rol correspondiente
            alumnos = [member.name for member in miembros_con_rol]
            print(f"Alumnos encontrados con el rol {rol_curso}: {', '.join(alumnos)}")  # Depuración: Lista de alumnos encontrados
            return {"alumnos": alumnos}
        else:
            print(f"No se encontraron alumnos con el rol de curso {rol_curso}.")  # Depuración: Si no se encuentran alumnos
            return {"error": f"No se encontraron alumnos con el rol de curso {rol_curso}."}

# Aseguramos que el cog se añada al bot
def setup(bot):
    bot.add_cog(CargarAlumnosAsignatura(bot))
