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

        # --- MEJORA: Usar fetch_guild para más fiabilidad ---
        try:
            guild = await self.bot.fetch_guild(int(servidor["DiscordID"]))
        except disnake.NotFound:
            raise Exception("Guild no encontrado en Discord o el bot no es miembro.")

        await guild.fetch_channels()

        return sorted([
            cat.name.strip()
            for cat in guild.categories
            if cat.name.strip().lower() not in ["text channels", "voice channels", "general"] # Excluir también 'general'
        ])

    async def crear_asignatura(self, insti_id: int, curso_grado: str, nombre_asignatura: str, discord_id_profesor: int):
        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("Servidor no encontrado para el instituto.")

        # --- MEJORA: Usar fetch_guild para más fiabilidad ---
        try:
            guild = await self.bot.fetch_guild(int(servidor["DiscordID"]))
        except disnake.NotFound:
            raise Exception("Guild no encontrado en Discord o el bot no es miembro.")

        nombre_rol = f"{curso_grado} - {nombre_asignatura}".strip()
        nombre_categoria = nombre_rol

        # ──────────── Obtener roles para permisos ────────────
        admin_role = disnake.utils.get(guild.roles, name="admin")
        jefe_role = disnake.utils.get(guild.roles, name="jefe")
        if not admin_role or not jefe_role:
            raise Exception("No se encontraron los roles 'admin' y/o 'jefe' en el servidor.")

        # ──────────── Crear o buscar rol de la asignatura ────────────
        rol_asignatura = disnake.utils.get(guild.roles, name=nombre_rol)
        if rol_asignatura is None:
            rol_asignatura = await guild.create_role(name=nombre_rol)
            print(f"🆕 Rol '{nombre_rol}' creado.")
        else:
            print(f"⚠️ Rol '{nombre_rol}' ya existe. No se crea duplicado.")
            
        # ──────────── Definir permisos para la categoría ────────────
        overwrites = {
            guild.default_role: disnake.PermissionOverwrite(view_channel=False), # Everyone no ve nada
            admin_role: disnake.PermissionOverwrite(view_channel=True),
            jefe_role: disnake.PermissionOverwrite(view_channel=True),
            rol_asignatura: disnake.PermissionOverwrite(
                view_channel=True,          # Ver el canal
                send_messages=True,         # Enviar mensajes
                attach_files=True,          # Adjuntar archivos
                connect=True,               # Conectarse a canales de voz
                speak=True,                 # Hablar en canales de voz
                manage_channels=True,       # Permitir al profesor gestionar los canales de su asignatura
                manage_messages=True        # Permitir al profesor gestionar mensajes
            )
        }

        # ──────────── Determinar posición ────────────
        base_categoria = disnake.utils.get(guild.categories, name=curso_grado)
        nueva_posicion = base_categoria.position + 1 if base_categoria else None

        # ──────────── Crear o buscar categoría ────────────
        categoria = disnake.utils.get(guild.categories, name=nombre_categoria)
        if categoria is None:
            # Crear la categoría aplicando directamente los permisos
            categoria = await guild.create_category(name=nombre_categoria, overwrites=overwrites)
            print(f"🆕 Categoría '{nombre_categoria}' creada con permisos seguros.")

            # Reordenar manualmente
            if nueva_posicion is not None:
                await categoria.edit(position=nueva_posicion)
                print(f"📦 Categoría '{nombre_categoria}' movida a posición {nueva_posicion}.")
        else:
            print(f"⚠️ Categoría '{nombre_categoria}' ya existe. Asegurando permisos...")
            # Si la categoría ya existe, nos aseguramos de que tenga los permisos correctos
            await categoria.edit(overwrites=overwrites)


        # ──────────── Crear canales por defecto (heredarán permisos) ────────────
        canales_defecto = [
            "📌・general", "❓・dudas", "📝・exámenes",
            "📚・teoría", "📤・entregas", "📎・tareas"
        ]
        nombres_existentes = [c.name for c in categoria.channels]

        for canal_nombre in canales_defecto:
            if canal_nombre not in nombres_existentes:
                # No necesitamos especificar permisos, los hereda de la categoría
                await categoria.create_text_channel(canal_nombre)
                print(f"✅ Canal '{canal_nombre}' creado.")
            else:
                print(f"⚠️ Canal '{canal_nombre}' ya existe.")

        # ──────────── Asignar rol al profesor ────────────
        try:
            # --- MEJORA: Usar fetch_member para más fiabilidad ---
            miembro = await guild.fetch_member(discord_id_profesor)
            if rol_asignatura not in miembro.roles:
                await miembro.add_roles(rol_asignatura)
                print(f"✅ Rol '{nombre_rol}' asignado a {miembro.name}.")
            else:
                print(f"ℹ️ {miembro.name} ya tiene el rol '{nombre_rol}'.")
        except disnake.NotFound:
            print(f"❌ No se encontró al profesor con DiscordID {discord_id_profesor} en el servidor.")

def setup(bot):
    bot.add_cog(AñadirAsignaturaCogs(bot))