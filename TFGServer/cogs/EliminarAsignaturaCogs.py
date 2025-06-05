from disnake.ext import commands
import disnake
from db_connection import Database

class EliminarAsignaturaCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    async def obtener_asignaturas_por_grado(self, insti_id: int, curso_grado: str) -> list[str]:
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado.")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Guild no disponible.")

        await guild.fetch_channels()
        asignaturas = []

        for cat in guild.categories:
            if cat.name.startswith(curso_grado + " - "):
                partes = cat.name.split(" - ", 1)
                if len(partes) == 2:
                    asignaturas.append(partes[1].strip())

        return sorted(asignaturas)

    async def eliminar_asignatura(self, insti_id: int, curso_grado: str, nombre_asignatura: str):
        nombre_categoria = f"{curso_grado} - {nombre_asignatura}"
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)

        if not servidor:
            raise Exception("Servidor no encontrado.")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Guild no disponible.")

        await guild.fetch_channels()

        # Eliminar canales y categoría
        categoria = disnake.utils.get(guild.categories, name=nombre_categoria)
        if categoria:
            for canal in categoria.channels:
                try:
                    await canal.delete()
                except Exception as e:
                    print(f"❌ Error al eliminar canal: {e}")
            await categoria.delete()
            print(f"✅ Categoría '{nombre_categoria}' eliminada.")
        else:
            print(f"⚠️ Categoría '{nombre_categoria}' no encontrada.")

        # Eliminar rol
        rol = disnake.utils.get(guild.roles, name=nombre_categoria)
        if rol:
            await rol.delete()
            print(f"✅ Rol '{nombre_categoria}' eliminado.")
        else:
            print(f"⚠️ Rol '{nombre_categoria}' no encontrado.")

def setup(bot):
    bot.add_cog(EliminarAsignaturaCogs(bot))