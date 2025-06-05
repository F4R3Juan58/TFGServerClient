from disnake.ext import commands
import disnake
from db_connection import Database

class AsignarAsignaturaCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    # Obtener las asignaturas asignadas al profesor
    async def obtener_asignaturas_de_profesor(self, insti_id: int, discord_id: int) -> list[str]:
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Servidor de Discord no encontrado por ID")

        try:
            miembro = await guild.fetch_member(discord_id)
        except disnake.NotFound:
            raise Exception("Profesor no encontrado en Discord (fetch fallido)")

        # Filtramos los roles que contienen ' - ' en el nombre y obtenemos la parte de la asignatura
        return [
            rol.name.split(" - ")[-1]
            for rol in miembro.roles
            if " - " in rol.name
        ]

    # Obtener todas las asignaturas disponibles en el servidor
    async def obtener_asignaturas_totales(self, insti_id: int) -> list[str]:
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Servidor de Discord no encontrado")

        # Recuperamos los roles del servidor
        await guild.fetch_roles()

        # Filtramos los roles con ' - ' y obtenemos las asignaturas
        return sorted({
            rol.name.split(" - ")[-1]
            for rol in guild.roles
            if " - " in rol.name
        })

    # Obtener el curso del profesor a partir de sus roles
    async def obtener_categoria_curso_profesor(self, insti_id: int, discord_id: int) -> str:
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Servidor de Discord no encontrado por ID")

        try:
            miembro = await guild.fetch_member(discord_id)
        except disnake.NotFound:
            raise Exception("Miembro no encontrado en Discord (fetch fallido)")

        # Buscar el primer rol que contiene ' - ' y obtener el curso (parte antes del guion)
        for rol in miembro.roles:
            if " - " in rol.name:
                return rol.name.split(" - ")[0]

        # Si no se encuentra ningún rol, lanzamos una excepción
        raise Exception("No se pudo determinar el curso del profesor")

    # Cambiar el rol asignado al profesor
    async def cambiar_rol_asignatura(self, insti_id: int, discord_id: int, rol_a_quitar: str, rol_a_agregar: str = None):
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Servidor de Discord no encontrado por ID")

        # Recuperamos los roles del servidor
        await guild.fetch_roles()

        try:
            miembro = await guild.fetch_member(discord_id)
        except disnake.NotFound:
            raise Exception("Miembro no encontrado en Discord (fetch fallido)")

        # Obtener el prefijo del curso (ej. "1º DAM")
        curso_prefix = await self.obtener_categoria_curso_profesor(insti_id, discord_id)

        # Construir el nombre completo del rol para agregar y quitar
        nombre_completo_quitar = f"{curso_prefix} - {rol_a_quitar}" if rol_a_quitar else None
        nombre_completo_agregar = f"{curso_prefix} - {rol_a_agregar}" if rol_a_agregar else None

        print(f"[INFO] Curso prefijo: {curso_prefix}")
        print(f"[INFO] Rol a quitar: {nombre_completo_quitar}")
        print(f"[INFO] Rol a agregar: {nombre_completo_agregar}")

        # Quitar el rol si es necesario
        if nombre_completo_quitar:
            rol_quitar = disnake.utils.get(guild.roles, name=nombre_completo_quitar)
            if rol_quitar and rol_quitar in miembro.roles:
                await miembro.remove_roles(rol_quitar)
                print(f"[OK] Rol quitado: {rol_quitar.name}")
            else:
                print(f"[WARN] Rol '{nombre_completo_quitar}' no encontrado o no está asignado")

        # Agregar el rol si es necesario
        if nombre_completo_agregar:
            rol_agregar = disnake.utils.get(guild.roles, name=nombre_completo_agregar)
            if rol_agregar and rol_agregar not in miembro.roles:
                await miembro.add_roles(rol_agregar)
                print(f"[OK] Rol agregado: {rol_agregar.name}")
            else:
                print(f"[WARN] Rol '{nombre_completo_agregar}' no encontrado o ya asignado")

# Función para registrar el cog
def setup(bot):
    bot.add_cog(AsignarAsignaturaCogs(bot))
