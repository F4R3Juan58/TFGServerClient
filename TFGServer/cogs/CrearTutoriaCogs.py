from disnake.ext import commands
import disnake
import asyncio
from db_connection import Database

class CrearTutoriaCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()
        self.voice_channels_temporales = set()

    async def crear_tutoria(self, insti_id: int, discord_id_profesor: int):
        # Obtener servidor y guild
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado para el instituto")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Guild no encontrado en Discord")

        # Obtener el profesor como miembro
        profesor = await guild.fetch_member(discord_id_profesor)
        if not profesor:
            raise Exception("Profesor no encontrado en Discord")

        # Obtener curso del profesor (por ejemplo, categoría base)
        curso_nombre = await self.obtener_categoria_curso_profesor(insti_id, discord_id_profesor)

        # Buscar categoría con ese nombre
        categoria = disnake.utils.get(guild.categories, name=curso_nombre)
        if not categoria:
            raise Exception(f"Categoría para curso '{curso_nombre}' no encontrada")

        # --- INICIO DE LA MODIFICACIÓN ---
        # Buscar los roles necesarios para los permisos
        rol_alumno = disnake.utils.get(guild.roles, name="alumno")
        rol_tutor = disnake.utils.get(guild.roles, name="tutor")

        if not rol_alumno or not rol_tutor:
            raise Exception("No se pudieron encontrar los roles 'alumno' y/o 'tutor' en el servidor.")

        # Crear canal de voz temporal con permisos específicos
        overwrites = {
            guild.default_role: disnake.PermissionOverwrite(connect=False, view_channel=False),
            profesor: disnake.PermissionOverwrite(connect=True, view_channel=True, manage_channels=True),
            rol_alumno: disnake.PermissionOverwrite(connect=True, view_channel=True),
            rol_tutor: disnake.PermissionOverwrite(connect=True, view_channel=True)
        }
        # --- FIN DE LA MODIFICACIÓN ---

        canal_nombre = f"Tutoria {profesor.display_name} - {curso_nombre}"
        canal = await guild.create_voice_channel(name=canal_nombre, category=categoria, overwrites=overwrites)

        self.voice_channels_temporales.add(canal.id)

        # Enviar mensaje de tutoría iniciada
        await guild.text_channels[0].send(f"¡Tutoría iniciada en el canal {canal_nombre}!")

        # Listener para detectar si el canal se queda vacío
        def check_channel_empty(member, before, after):
            if before.channel and before.channel.id == canal.id:
                if len(before.channel.members) == 0:
                    return True
            return False

        # Esperar evento de que el canal quede vacío y eliminarlo
        try:
            await self.bot.wait_for("voice_state_update", check=check_channel_empty, timeout=3600)  # 1 hora max
            await canal.delete()
            self.voice_channels_temporales.remove(canal.id)

            # Enviar mensaje de tutoría finalizada
            await guild.text_channels[0].send(f"¡La tutoría en el canal {canal_nombre} ha finalizado!")

        except asyncio.TimeoutError:
            # Eliminar canal si sigue activo tras 1 hora
            if canal.id in self.voice_channels_temporales:
                await canal.delete()
                self.voice_channels_temporales.remove(canal.id)

                # Enviar mensaje de tutoría finalizada
                await guild.text_channels[0].send(f"¡La tutoría en el canal {canal_nombre} ha finalizado!")

        return f"Canal de tutoría '{canal_nombre}' creado correctamente."

    async def obtener_categoria_curso_profesor(self, insti_id: int, discord_id: int) -> str:
        # Método similar a la implementación previa para obtener nombre categoría curso
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Guild no encontrado")

        miembro = await guild.fetch_member(discord_id)
        for rol in miembro.roles:
            if " - " in rol.name:
                return rol.name.split(" - ")[0]

        raise Exception("No se pudo determinar la categoría curso del profesor")

def setup(bot):
    bot.add_cog(CrearTutoriaCogs(bot))