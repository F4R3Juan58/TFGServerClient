from disnake.ext import commands
import disnake
import os
from db_connection import Database
from werkzeug.utils import secure_filename

class SubirArchivoCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()
        self.upload_folder = 'uploads'  # Carpeta donde se guardan los archivos

    async def subir_archivo(self, insti_id, discord_id_profesor, nombre_asignatura, archivo_path):
        try:
            print(f"Recibiendo archivo para la asignatura {nombre_asignatura}...")
            print(f"Archivo recibido en ruta: {archivo_path}")

            # Obtener el servidor
            servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
            if not servidor:
                print(f"Error: Servidor con InstiID {insti_id} no encontrado.")
                return

            guild = self.bot.get_guild(int(servidor["DiscordID"]))
            if not guild:
                print(f"Error: Guild no encontrado para DiscordID {servidor['DiscordID']}.")
                return

            # Buscar la categoría
            categorias = [
                categoria for categoria in guild.categories
                if '-' in categoria.name and nombre_asignatura.lower() in categoria.name.lower()
            ]

            if not categorias:
                print(f"No se encontró categoría para la asignatura '{nombre_asignatura}'.")
                return

            categoria = categorias[0]
            canal_teoria = disnake.utils.get(categoria.channels, name="📚・teoría")
            if not canal_teoria:
                print(f"No se encontró canal '📚・teoría' en la categoría '{categoria.name}'.")
                return

            # Enviar el archivo desde la ruta
            await canal_teoria.send(
                f"Se ha subido un nuevo archivo para la asignatura '{nombre_asignatura}':",
                file=disnake.File(archivo_path)
            )

            print(f"Archivo subido correctamente al canal '{canal_teoria.name}'.")

        except Exception as e:
            print(f"Error al subir el archivo desde Flask: {str(e)}")

def setup(bot):
    bot.add_cog(SubirArchivoCogs(bot))
