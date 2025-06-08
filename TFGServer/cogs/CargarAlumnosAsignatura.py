import disnake
from disnake.ext import commands
from db_connection import Database

class CargarAlumnosAsignatura(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    async def cargarAlumnosAsignatura(self, guild_id: int, nombre_asignatura: str, discord_id_profesor: int = None):
        guild = self.bot.get_guild(guild_id)
        if not guild:
            return {"error": "Guild no encontrado"}

        profesor = guild.get_member(discord_id_profesor) if discord_id_profesor else None
        if not profesor:
            return {"error": "Profesor no encontrado en el servidor"}

        # Buscar un rol del profesor que contenga la asignatura en su nombre
        rol_asignatura = None
        for rol in profesor.roles:
            if nombre_asignatura.lower() in rol.name.lower():
                rol_asignatura = rol
                break

        if not rol_asignatura:
            return {"error": f"No se encontró un rol del profesor que contenga la asignatura '{nombre_asignatura}'"}

        # Buscar todos los miembros con ese rol, excepto el profesor
        miembros_con_rol = [
            member for member in guild.members
            if rol_asignatura in member.roles and member.id != discord_id_profesor
        ]

        alumnos = [member.name for member in miembros_con_rol]

        if alumnos:
            return {"alumnos": alumnos}
        else:
            return {"error": f"No se encontraron alumnos con el rol '{rol_asignatura.name}'"}

# Aseguramos que el cog se añada al bot
def setup(bot):
    bot.add_cog(CargarAlumnosAsignatura(bot))
