from disnake.ext import commands
import disnake
from db_connection import Database

class AsignarDelegadoCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    # Comando de Discord para uso manual (si quisieras usarlo con !!asignar_delegado)
    @commands.command(name="asignar_delegado")
    async def asignar_delegado_cmd(self, ctx, insti_id: int, discord_id_alumno: int):
        await self.asignar_delegado_logica(insti_id, discord_id_alumno)
        await ctx.send("✅ Delegado asignado correctamente.")

    # Método reutilizable desde Flask
    async def asignar_delegado_logica(self, insti_id: int, discord_id_alumno: int):
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("❌ No se encontró el servidor para ese instituto.")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("❌ Guild no encontrado.")

        # Crear rol si no existe
        rol_delegado = disnake.utils.get(guild.roles, name="Delegado")
        if rol_delegado is None:
            rol_delegado = await guild.create_role(name="Delegado")
            print("🆕 Rol 'Delegado' creado.")

        # Buscar delegado anterior y removerle el rol si tiene DiscordID
        delegado_antiguo = await self.db.obtener_alumno_delegado(insti_id)
        if delegado_antiguo and delegado_antiguo.get("DiscordID"):
            miembro_antiguo = guild.get_member(int(delegado_antiguo["DiscordID"]))
            if miembro_antiguo and rol_delegado in miembro_antiguo.roles:
                await miembro_antiguo.remove_roles(rol_delegado)
                print(f"🗑️ Rol eliminado de: {miembro_antiguo.display_name}")

        # Asignar el rol al nuevo delegado
        nuevo_miembro = guild.get_member(discord_id_alumno)
        if not nuevo_miembro:
            raise Exception("❌ Alumno no encontrado en el servidor.")

        await nuevo_miembro.add_roles(rol_delegado)
        print(f"✅ Nuevo delegado asignado: {nuevo_miembro.display_name}")

        try:
            await nuevo_miembro.send("📢 Has sido asignado como delegado de tu clase. ¡Enhorabuena!")
        except Exception as e:
            print(f"⚠️ No se pudo enviar DM al alumno: {e}")

def setup(bot):
    bot.add_cog(AsignarDelegadoCogs(bot))