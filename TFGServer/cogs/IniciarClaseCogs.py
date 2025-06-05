from disnake.ext import commands
import disnake
import asyncio
from db_connection import Database

class IniciarClaseCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()
        self.voice_channels_temporales = set()

    async def iniciar_clase(self, insti_id: int, discord_id_profesor: int, nombre_asignatura: str):
        # Obtener servidor y guild
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            print("Servidor no encontrado para el instituto")
            raise Exception("Servidor no encontrado para el instituto")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            print("Guild no encontrado en Discord")
            raise Exception("Guild no encontrado en Discord")

        # Obtener el profesor como miembro
        profesor = await guild.fetch_member(discord_id_profesor)
        if not profesor:
            print("Profesor no encontrado en Discord")
            raise Exception("Profesor no encontrado en Discord")

        # Buscar roles con el nombre de la asignatura
        roles = [role.name for role in guild.roles if '-' in role.name and nombre_asignatura.lower() in role.name.lower()]
        if roles:
            print(f"Roles que contienen '-' y coinciden con '{nombre_asignatura}': {', '.join(roles)}")
        else:
            print(f"No se encontraron roles con '-' y que contengan '{nombre_asignatura}'.")

        # Buscar categorías con el nombre de la asignatura
        categorias = [categoria.name for categoria in guild.categories if '-' in categoria.name and nombre_asignatura.lower() in categoria.name.lower()]
        if categorias:
            print(f"Categorías que contienen '-' y coinciden con '{nombre_asignatura}': {', '.join(categorias)}")
        else:
            print(f"No se encontraron categorías con '-' y que contengan '{nombre_asignatura}'.")

        # Si no se encuentra ninguna categoría que coincida, lanzamos una excepción
        if not categorias:
            raise Exception(f"Categoría para la asignatura '{nombre_asignatura}' no encontrada")


        # Si no se encuentra ninguna categoría que coincida, lanzamos una excepción
        if not categorias:
            raise Exception(f"Categoría para la asignatura '{nombre_asignatura}' no encontrada")

        # Crear el canal de voz en la categoría correspondiente
        overwrites = {
            guild.default_role: disnake.PermissionOverwrite(connect=False),
            profesor: disnake.PermissionOverwrite(connect=True, manage_channels=True),
        }

        # Permitir acceso a todos los alumnos (rol "Alumno" de la categoría)
        rol_alumno = disnake.utils.find(lambda r: r.name.lower() == "alumno", guild.roles)
        if rol_alumno:
            overwrites[rol_alumno] = disnake.PermissionOverwrite(connect=True)

        canal_nombre = f"Clase {nombre_asignatura} - {profesor.display_name}"

        # Buscar la categoría que coincide con el nombre de la asignatura
        categorias = [categoria for categoria in guild.categories if '-' in categoria.name and nombre_asignatura.lower() in categoria.name.lower()]

        # Debug: Mostrar categorías encontradas
        print(f"[DEBUG] Categorías encontradas: {', '.join(categoria.name for categoria in categorias)}")

        if not categorias:
            print(f"[DEBUG] No se encontraron categorías con el nombre '{nombre_asignatura}'")
            raise Exception(f"Categoría para la asignatura '{nombre_asignatura}' no encontrada")
        elif len(categorias) > 1:
            print(f"[DEBUG] Se encontraron varias categorías: {', '.join(categoria.name for categoria in categorias)}")
            raise Exception(f"Se encontraron varias categorías para '{nombre_asignatura}'")
        else:
            categoria = categorias[0]  # Seleccionar la única categoría encontrada

        # Debug: Mostrar categoría seleccionada
        print(f"[DEBUG] Usando la categoría: {categoria.name}")

        # Crear el canal de voz en la categoría seleccionada
        try:
            canal = await guild.create_voice_channel(name=canal_nombre, category=categoria, overwrites=overwrites)
            self.voice_channels_temporales.add(canal.id)

            # Debug: Confirmación de creación del canal
            print(f"[DEBUG] Canal creado: {canal.name}, ID: {canal.id}")

            # Enviar mensaje de clase iniciada
            await guild.text_channels[0].send(f"¡Clase iniciada en el canal {canal_nombre}!")

            # Listener para detectar si el canal se queda vacío
            def check_channel_empty(member, before, after):
                if before.channel and before.channel.id == canal.id:
                    if len(before.channel.members) == 0:
                        print(f"[DEBUG] El canal {canal.nombre} está vacío")
                        return True
                return False

            # Esperar evento de que el canal quede vacío y eliminarlo
            try:
                print(f"[DEBUG] Esperando evento de actualización de estado en el canal de voz '{canal_nombre}'")
                await self.bot.wait_for("voice_state_update", check=check_channel_empty, timeout=3600)  # 1 hora max
                await canal.delete()
                self.voice_channels_temporales.remove(canal.id)

                # Enviar mensaje de clase finalizada
                print(f"[DEBUG] El canal de voz {canal_nombre} ha sido eliminado")
                await guild.text_channels[0].send(f"¡La clase en el canal {canal_nombre} ha finalizado!")

            except asyncio.TimeoutError:
                # Eliminar canal si sigue activo tras 1 hora
                if canal.id in self.voice_channels_temporales:
                    await canal.delete()
                    self.voice_channels_temporales.remove(canal.id)

                    # Enviar mensaje de clase finalizada
                    print(f"[DEBUG] El canal de voz {canal_nombre} ha sido eliminado tras el tiempo límite")
                    await guild.text_channels[0].send(f"¡La clase en el canal {canal_nombre} ha finalizado por timeout!")

        except Exception as e:
            # Capturar cualquier error durante la creación del canal
            print(f"[DEBUG] Error al crear el canal de voz: {e}")

        return f"Canal de clase '{canal_nombre}' creado correctamente."



def setup(bot):
    bot.add_cog(IniciarClaseCogs(bot))
