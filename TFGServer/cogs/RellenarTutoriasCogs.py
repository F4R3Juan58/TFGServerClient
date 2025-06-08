import disnake
from disnake.ext import commands
from db_connection import Database

class RellenarTutoriasCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()  # Asegúrate de que tienes la clase Database correctamente configurada.

    async def obtener_tutorias(self, insti_id: int, discord_id_profesor: int):
        # Obtener servidor y guild
        try:
            # Verificamos si obtenemos el servidor correctamente
            print(f"Debug: Buscando servidor para instituto {insti_id}")
            servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
            if not servidor:
                raise Exception("Servidor no encontrado para el instituto")
            
            print(f"Debug: Servidor encontrado: {servidor['DiscordID']}")
            guild = self.bot.get_guild(int(servidor["DiscordID"]))
            if not guild:
                raise Exception("Guild no encontrado en Discord")
            print(f"Debug: Guild encontrado: {guild.name}")

        except Exception as e:
            print(f"Error obteniendo servidor o guild: {e}")
            raise

        # Obtener el profesor como miembro
        try:
            print(f"Debug: Buscando profesor con DiscordID {discord_id_profesor}")
            profesor = await guild.fetch_member(discord_id_profesor)
            if not profesor:
                raise Exception("Profesor no encontrado en Discord")
            print(f"Debug: Profesor encontrado: {profesor.display_name}")

        except Exception as e:
            print(f"Error obteniendo miembro: {e}")
            raise

        # Obtener la categoría principal del profesor (usando el rol de curso)
        try:
            print("Debug: Buscando categoría principal del profesor...")
            categoria = None
            for role in profesor.roles:
                if " - " in role.name:  # Suponemos que el nombre del rol incluye el nombre del curso
                    categoria = disnake.utils.get(guild.categories, name=role.name.split(" - ")[0])
                    if categoria:
                        break
            
            if not categoria:
                raise Exception("Categoría principal del profesor no encontrada.")
            print(f"Debug: Categoría principal del profesor encontrada: {categoria.name}")

        except Exception as e:
            print(f"Error obteniendo categoría: {e}")
            raise

        # Obtener los canales de texto que empiezan con TFG, FCT o Tutoría en la categoría del profesor
        print("Debug: Buscando canales de texto relacionados con 'TFG', 'FCT' o 'Tutoría'...")
        tutorias = []
        for canal in categoria.text_channels:  # Cambié de `voice_channels` a `text_channels`
            if canal.name.lower().startswith(("tfg", "fct", "tutoría")):
                tutorias.append(canal.name)
                print(f"Debug: Canal de texto encontrado: {canal.name}")

        if not tutorias:
            print("Debug: No se encontraron tutorías en la categoría.")
        else:
            print(f"Debug: Se encontraron las siguientes tutorías: {', '.join(tutorias)}")

        return tutorias

def setup(bot):
    bot.add_cog(RellenarTutoriasCogs(bot))