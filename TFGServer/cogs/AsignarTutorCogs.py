from disnake.ext import commands
import disnake
from db_connection import Database

class AsignarTutorCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    async def buscar_categoria_por_nombre(self, guild: disnake.Guild, nombre_categoria: str):
        await guild.fetch_channels()
        for cat in guild.categories:
            if cat.name.strip().lower() == nombre_categoria.strip().lower():
                return cat
        return None

    async def asignar_tutor_logica(self, insti_id: int, categoria: str, discord_id_profesor: int):
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("❌ No se encontró servidor para ese instituto.")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("❌ Guild no disponible.")

        # Buscar o crear rol "Tutor"
        rol_tutor = disnake.utils.get(guild.roles, name="Tutor")
        if rol_tutor is None:
            rol_tutor = await guild.create_role(name="Tutor")
            print("🔧 Rol 'Tutor' creado.")

        # Buscar al profesor en el guild
        miembro = guild.get_member(discord_id_profesor)
        if not miembro:
            raise Exception("❌ No se encontró al profesor en el servidor.")

        # Asignar rol Tutor
        await miembro.add_roles(rol_tutor)
        print(f"✅ Rol 'Tutor' asignado a {miembro.name}.")

        # Buscar la categoría
        categoria_discord = await self.buscar_categoria_por_nombre(guild, categoria)
        if categoria_discord is None:
            raise Exception("❌ Categoría no encontrada.")

        # Aplicar permisos en la categoría
        permisos = disnake.PermissionOverwrite(
            read_messages=True,
            send_messages=True,
            attach_files=True,
            connect=True,
            manage_channels=True
        )
        await categoria_discord.set_permissions(rol_tutor, overwrite=permisos)
        print(f"🔐 Permisos asignados al tutor en categoría '{categoria}'.")

        # Enviar DM si es posible
        try:
            await miembro.send(f"📚 Has sido asignado como tutor del curso '{categoria}'. ¡Bien hecho!")
        except Exception as e:
            print(f"⚠️ No se pudo enviar DM a {miembro.name}: {e}")

    @commands.command(name="asignar_tutor_categoria")
    async def asignar_tutor_categoria(self, ctx, insti_id: int, categoria: str, discord_id_profesor: int):
        try:
            await self.asignar_tutor_logica(insti_id, categoria, discord_id_profesor)
            await ctx.send(f"✅ Tutor asignado y permisos aplicados en categoría '{categoria}'.")
        except Exception as e:
            await ctx.send(str(e))

def setup(bot):
    bot.add_cog(AsignarTutorCogs(bot))