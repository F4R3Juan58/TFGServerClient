import disnake
from disnake.ext import commands

class ServidorEliminador(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    async def eliminar_servidor(self, nombre_instituto: str, usuario_id: int):
        # Buscar el servidor con ese nombre
        guild = disnake.utils.get(self.bot.guilds, name=nombre_instituto)
        if not guild:
            print(f"❌ No existe ningún servidor llamado '{nombre_instituto}'.")
            return False

        # Intentar eliminar el servidor
        try:
            await guild.delete()
            print(f"✅ Servidor '{nombre_instituto}' eliminado correctamente.")

            # Opcional: enviar mensaje al admin confirmando eliminación
            usuario = await self.bot.fetch_user(usuario_id)
            if usuario:
                await usuario.send(f"✅ El servidor '{nombre_instituto}' ha sido eliminado correctamente.")
            return True

        except disnake.Forbidden:
            print(f"❌ No tengo permisos para eliminar el servidor '{nombre_instituto}'.")
            return False
        except Exception as e:
            print(f"❌ Error eliminando el servidor: {e}")
            return False

def setup(bot):
    bot.add_cog(ServidorEliminador(bot))