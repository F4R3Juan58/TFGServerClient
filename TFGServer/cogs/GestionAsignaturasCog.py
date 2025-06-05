from disnake.ext import commands
import disnake

class GestionAsignaturasCog(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

    async def reestablecer_asignatura(self, nombre_asignatura):
        print(f"[DEBUG] Ejecutando reestablecer para: {nombre_asignatura}")

        for guild in self.bot.guilds:
            print(f"[DEBUG] Buscando en servidor: {guild.name}")

            # Búsqueda más segura de categoría
            categoria = next(
                (cat for cat in guild.categories if nombre_asignatura.lower() in cat.name.lower()),
                None
            )

            if not categoria:
                print(f"[DEBUG] Categoría no encontrada para '{nombre_asignatura}' en {guild.name}")
                continue

            # Guardar nombres de los canales actuales
            nombres_canales = [canal.name for canal in categoria.channels]
            print(f"[DEBUG] Canales detectados en '{categoria.name}': {nombres_canales}")

            # Eliminar los canales existentes
            for canal in categoria.channels:
                try:
                    await canal.delete(reason="Reestablecimiento de asignatura")
                    print(f"[DEBUG] Canal eliminado: {canal.name}")
                except Exception as e:
                    print(f"[ERROR] No se pudo eliminar el canal {canal.name}: {e}")

            # Recrear los canales vacíos
            for nombre in nombres_canales:
                try:
                    await guild.create_text_channel(
                        name=nombre,
                        category=categoria
                    )
                    print(f"[DEBUG] Canal recreado: {nombre}")
                except Exception as e:
                    print(f"[ERROR] No se pudo crear el canal {nombre}: {e}")

        print(f"[DEBUG] Reestablecimiento completo para: {nombre_asignatura}")

def setup(bot):
    bot.add_cog(GestionAsignaturasCog(bot))
