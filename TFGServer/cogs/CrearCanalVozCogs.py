import disnake
from disnake.ext import commands
import asyncio
from db_connection import Database

class CrearCanalVozCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()
        self.voice_channels_temporales = set()

    async def crear_canal_voz(self, insti_id: int, discord_id_profesor: int, discord_id_alumno: int):
        # Depuración: Verificación de entrada
        print(f"Debug: Creando canal de voz para instituto {insti_id}, profesor {discord_id_profesor}, alumno {discord_id_alumno}")

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
            # Usamos fetch_member en vez de get_member para asegurar que obtenemos al miembro actualizado
            profesor = await guild.fetch_member(discord_id_profesor)
            if not profesor:
                raise Exception(f"Profesor no encontrado en Discord con ID {discord_id_profesor}")
            print(f"Debug: Profesor encontrado: {profesor.display_name}")

            alumno = await guild.fetch_member(discord_id_alumno)
            if not alumno:
                raise Exception(f"Alumno no encontrado en Discord con ID {discord_id_alumno}")
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

        # Nombre del canal será "Tutoría - Nombre del Alumno"
        nombre_canal = f"Tutoría - {alumno.display_name}"
        print(f"Debug: Nombre del canal: {nombre_canal}")

        # Crear el rol con el mismo nombre que el canal
        try:
            rol = await guild.create_role(name=nombre_canal, mentionable=True)
            print(f"Debug: Rol creado con nombre: {nombre_canal}")
        except Exception as e:
            print(f"Error creando rol: {e}")
            raise

        # --- INICIO DE LA MODIFICACIÓN ---
        # Buscar el rol "tutor" en el servidor
        rol_tutor = disnake.utils.get(guild.roles, name="tutor")
        if not rol_tutor:
            # Si no se encuentra el rol tutor, se revierte la creación del rol de sesión para evitar dejar basura.
            await rol.delete()
            raise Exception("El rol 'tutor' no fue encontrado en el servidor.")

        # Crear permisos para el canal de voz
        overwrites = {
            guild.default_role: disnake.PermissionOverwrite(connect=False, view_channel=False),
            rol: disnake.PermissionOverwrite(connect=True, view_channel=True),
            rol_tutor: disnake.PermissionOverwrite(connect=True, view_channel=True),
            profesor: disnake.PermissionOverwrite(manage_channels=True) # El profesor que crea puede gestionar el canal
        }
        print(f"Debug: Permisos definidos para el canal.")
        # --- FIN DE LA MODIFICACIÓN ---

        # Crear el canal de voz en la categoría principal del profesor
        try:
            canal = await guild.create_voice_channel(name=nombre_canal, overwrites=overwrites, category=categoria)
            print(f"Debug: Canal de voz creado: {nombre_canal}")
        except Exception as e:
            print(f"Error creando canal de voz: {e}")
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
        self.voice_channels_temporales.add(canal.id)

        # Enviar mensaje al canal de texto anunciando la creación del canal
        try:
            await guild.text_channels[0].send(f"¡Tutoría iniciada en el canal {nombre_canal} para {alumno.display_name}!")
            print(f"Debug: Mensaje enviado al canal de texto.")
        except Exception as e:
            print(f"Error enviando mensaje de anuncio: {e}")

        # Función para verificar si el canal queda vacío
        def check_channel_empty(member, before, after):
            if before.channel and before.channel.id == canal.id:
                if len(before.channel.members) == 0:
                    return True
            return False

        # Esperar a que el canal quede vacío y eliminarlo
        try:
            await self.bot.wait_for("voice_state_update", check=check_channel_empty, timeout=3600)  # 1 hora máximo
            await canal.delete()
            await rol.delete()
            self.voice_channels_temporales.remove(canal.id)

            # Enviar mensaje de tutoría finalizada
            await guild.text_channels[0].send(f"¡La tutoría en el canal {nombre_canal} ha finalizado!")
            print(f"Debug: Canal y rol eliminados después de que el canal quedara vacío.")
        except asyncio.TimeoutError:
            # Si el canal sigue activo después de 1 hora
            if canal.id in self.voice_channels_temporales:
                await canal.delete()
                await rol.delete()
                self.voice_channels_temporales.remove(canal.id)

                # Enviar mensaje de tutoría finalizada
                await guild.text_channels[0].send(f"¡La tutoría en el canal {nombre_canal} ha finalizado por inactividad durante 1 hora!")
                print(f"Debug: Canal y rol eliminados por inactividad.")

        return f"Canal de voz '{nombre_canal}' creado correctamente."

def setup(bot):
    bot.add_cog(CrearCanalVozCogs(bot))