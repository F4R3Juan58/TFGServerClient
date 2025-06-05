from disnake.ext import commands
import disnake
from db_connection import Database

class AñadirAsignaturaCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()

    async def obtener_categorias_validas(self, insti_id: int) -> list[str]:
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado para el instituto.")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Guild no encontrado.")

        await guild.fetch_channels()

        return sorted([
            cat.name.strip()
            for cat in guild.categories
            if cat.name.strip().lower() not in ["text channels", "voice channels"]
        ])

    async def crear_asignatura(self, insti_id: int, curso_grado: str, nombre_asignatura: str, discord_id_profesor: int):
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado para el instituto.")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Guild no encontrado.")

        await guild.fetch_channels()

        nombre_rol = f"{curso_grado} - {nombre_asignatura}".strip()
        nombre_categoria = nombre_rol

        # ──────────── Crear o buscar rol ────────────
        rol = disnake.utils.get(guild.roles, name=nombre_rol)
        if rol is None:
            rol = await guild.create_role(name=nombre_rol)
            print(f"🆕 Rol '{nombre_rol}' creado.")
        else:
            print(f"⚠️ Rol '{nombre_rol}' ya existe. No se crea duplicado.")

        # ──────────── Determinar posición ────────────
        base_categoria = disnake.utils.get(guild.categories, name=curso_grado)
        nueva_posicion = base_categoria.position + 1 if base_categoria else None

        # ──────────── Crear o buscar categoría ────────────
        categoria = disnake.utils.get(guild.categories, name=nombre_categoria)
        if categoria is None:
            categoria = await guild.create_category(name=nombre_categoria)
            print(f"🆕 Categoría '{nombre_categoria}' creada.")

            # Reordenar manualmente
            if nueva_posicion is not None:
                await categoria.edit(position=nueva_posicion)
                print(f"📦 Categoría '{nombre_categoria}' movida a posición {nueva_posicion}.")
        else:
            print(f"⚠️ Categoría '{nombre_categoria}' ya existe. No se crea duplicado.")

        # ──────────── Asignar permisos al rol ────────────
        permisos = disnake.PermissionOverwrite(
            read_messages=True,
            send_messages=True,
            attach_files=True,
            connect=True,
            manage_channels=True
        )
        await categoria.set_permissions(rol, overwrite=permisos)
        print(f"🔐 Permisos asignados al rol '{nombre_rol}'.")

        # ──────────── Crear canales por defecto ────────────
        canales_defecto = [
            "📌・general", "❓・dudas", "📝・exámenes",
            "📚・teoría", "📤・entregas", "📎・tareas"
        ]
        nombres_existentes = [c.name for c in categoria.channels]

        for canal_nombre in canales_defecto:
            if canal_nombre not in nombres_existentes:
                await guild.create_text_channel(canal_nombre, category=categoria)
                print(f"✅ Canal '{canal_nombre}' creado.")
            else:
                print(f"⚠️ Canal '{canal_nombre}' ya existe.")

        # ──────────── Asignar rol al profesor ────────────
        miembro = guild.get_member(discord_id_profesor)
        if miembro is None:
            print(f"❌ No se encontró al profesor con DiscordID {discord_id_profesor}.")
        else:
            if rol not in miembro.roles:
                await miembro.add_roles(rol)
                print(f"✅ Rol '{nombre_rol}' asignado a {miembro.name}.")
            else:
                print(f"ℹ️ {miembro.name} ya tiene el rol '{nombre_rol}'.")

def setup(bot):
    bot.add_cog(AñadirAsignaturaCogs(bot))