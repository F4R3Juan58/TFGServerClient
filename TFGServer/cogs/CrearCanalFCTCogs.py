import disnake
from disnake.ext import commands
import asyncio
from db_connection import Database

class CrearCanalFCTCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()  # Asegúrate de que tienes la clase Database correctamente configurada.
        self.text_channels_temporales = set()

    async def crear_canal_fct(self, insti_id: int, discord_id_profesor: int, discord_id_alumno: int):
        # Depuración: Verificación de entrada
        print(f"Debug: Creando canal FCT para instituto {insti_id}, profesor {discord_id_profesor}, alumno {discord_id_alumno}")

        # Obtener servidor y guild
        try:
            servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
            if not servidor:
                raise Exception("Servidor no encontrado para el instituto")

            guild = self.bot.get_guild(int(servidor["DiscordID"]))
            if not guild:
                raise Exception("Guild no encontrado en Discord")
            print(f"Debug: Guild encontrado: {guild.name}")
        except Exception as e:
            print(f"Error obteniendo servidor o guild: {e}")
            raise

        # Obtener el profesor (tutor) y el alumno como miembros
        try:
            profesor = await guild.fetch_member(discord_id_profesor)
            if not profesor:
                raise Exception("Profesor no encontrado en Discord")
            print(f"Debug: Profesor encontrado: {profesor.display_name}")

            alumno = await guild.fetch_member(discord_id_alumno)
            if not alumno:
                raise Exception("Alumno no encontrado en Discord")
            print(f"Debug: Alumno encontrado: {alumno.display_name}")
        except Exception as e:
            print(f"Error obteniendo miembro: {e}")
            raise

        # Obtener la categoría principal del profesor (usando el rol de curso)
        try:
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

        # Nombre del canal será "FCT - Nombre del Alumno"
        nombre_canal = f"FCT - {alumno.display_name}"
        print(f"Debug: Nombre del canal: {nombre_canal}")

        # Crear el rol con el mismo nombre que el canal
        try:
            rol = await guild.create_role(name=nombre_canal, mentionable=True)
            print(f"Debug: Rol creado con nombre: {nombre_canal}")
        except Exception as e:
            print(f"Error creando rol: {e}")
            raise

        # Crear permisos para el canal de texto
        overwrites = {
            guild.default_role: disnake.PermissionOverwrite(send_messages=False),
            profesor: disnake.PermissionOverwrite(send_messages=True),
            alumno: disnake.PermissionOverwrite(send_messages=True),
            rol: disnake.PermissionOverwrite(send_messages=True)
        }
        print(f"Debug: Permisos definidos para el canal.")

        # Crear el canal de texto en la categoría principal del profesor
        try:
            canal = await guild.create_text_channel(name=nombre_canal, overwrites=overwrites, category=categoria)
            print(f"Debug: Canal de texto creado: {nombre_canal}")
        except Exception as e:
            print(f"Error creando canal de texto: {e}")
            raise

        # Asignar el rol tanto al profesor como al alumno
        try:
            await profesor.add_roles(rol)
            await alumno.add_roles(rol)
            print(f"Debug: Rol asignado a {profesor.display_name} y {alumno.display_name}")
        except Exception as e:
            print(f"Error asignando rol: {e}")
            raise

        # Añadir el canal creado a la lista de canales temporales
        self.text_channels_temporales.add(canal.id)

        # Enviar mensaje al canal de texto anunciando la creación del canal
        try:
            await canal.send(f"¡FCT iniciada en el canal {nombre_canal} para {alumno.display_name}!")
            print(f"Debug: Mensaje enviado al canal de texto.")
        except Exception as e:
            print(f"Error enviando mensaje de anuncio: {e}")

        return f"Canal FCT '{nombre_canal}' creado correctamente."

def setup(bot):
    bot.add_cog(CrearCanalFCTCogs(bot))