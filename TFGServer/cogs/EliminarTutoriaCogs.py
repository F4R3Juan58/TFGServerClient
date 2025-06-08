import disnake
from disnake.ext import commands
from db_connection import Database

class EliminarTutoriaCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()  # Asegúrate de que tienes la clase Database correctamente configurada.

    @commands.command()
    async def eliminar_tutoria(self, ctx, insti_id: int, discord_id_profesor: int, nombre_tutoria: str):
        """ Elimina una tutoría de Discord (canal de texto y rol asociado). """
        try:
            # Obtener servidor y guild
            print(f"Debug: Buscando servidor para instituto {insti_id}")
            servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
            if not servidor:
                raise Exception("Servidor no encontrado para el instituto")
            
            print(f"Debug: Servidor encontrado: {servidor['DiscordID']}")
            guild = self.bot.get_guild(int(servidor["DiscordID"]))
            if not guild:
                raise Exception("Guild no encontrado en Discord")
            print(f"Debug: Guild encontrado: {guild.name}")

            # Obtener el profesor como miembro
            print(f"Debug: Buscando profesor con DiscordID {discord_id_profesor}")
            profesor = await guild.fetch_member(discord_id_profesor)
            if not profesor:
                raise Exception("Profesor no encontrado en Discord")
            print(f"Debug: Profesor encontrado: {profesor.display_name}")

            # Buscar el canal de texto relacionado con el nombre de la tutoría
            tutorias = [canal for canal in guild.text_channels if canal.name.lower() == nombre_tutoria.lower()]

            if not tutorias:
                raise Exception(f"Canal de tutoría '{nombre_tutoria}' no encontrado en Discord.")
            
            canal_tutoria = tutorias[0]
            print(f"Debug: Canal de tutoría encontrado: {canal_tutoria.name}")

            # Eliminar el rol asociado
            rol = disnake.utils.get(guild.roles, name=nombre_tutoria)
            if rol:
                await rol.delete()
                print(f"Debug: Rol '{rol.name}' eliminado.")

            # Eliminar el canal de texto
            await canal_tutoria.delete()
            print(f"Debug: Canal de tutoría '{canal_tutoria.name}' eliminado.")

            # Mensaje de confirmación (opcional)
            print(f"✅ La tutoría '{nombre_tutoria}' ha sido eliminada correctamente.")

        except Exception as e:
            print(f"Error al eliminar la tutoría: {e}")
            # Si no hay ctx, podemos imprimir el error
            print(f"❌ Error al eliminar la tutoría: {e}")

def setup(bot):
    bot.add_cog(EliminarTutoriaCogs(bot))