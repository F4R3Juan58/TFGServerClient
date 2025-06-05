from disnake.ext import commands
import disnake
from db_connection import Database
from collections import defaultdict
import unicodedata

def limpiar_texto(nombre: str) -> str:
    return unicodedata.normalize("NFKD", nombre).encode("ascii", "ignore").decode("ascii").strip().lower()

class VotacionDelegadoCogs(commands.Cog):
    def __init__(self, bot):
        self.bot = bot
        self.db = Database()
        self.votaciones = {}

    async def iniciar_votacion_manual(self, email: str):
        insti_id = await self.db.obtener_insti_id_por_email(email)
        if not insti_id:
            raise Exception("No se encontró instituto para este email")

        servidor = await self.db.obtener_servidor_por_insti_id(insti_id)
        if not servidor:
            raise Exception("No se encontró el servidor asociado al instituto")

        guild = self.bot.get_guild(int(servidor["DiscordID"]))
        if not guild:
            raise Exception("Guild de Discord no encontrado")

        discord_id_profesor = await self.db.obtener_discord_id_por_email(email)
        miembro = await guild.fetch_member(discord_id_profesor)

        canal = next((c for c in guild.text_channels if "general" in c.name.lower()), None)
        if not canal:
            raise Exception("No se encontró un canal general para lanzar la votación")

        async def send_func(*args, **kwargs):
            return await canal.send(*args, **kwargs)

        await self._iniciar_votacion_interna(guild, miembro, send_func, email)

    async def _iniciar_votacion_interna(self, guild, profesor, send_func, email):
        curso_nombre = await self.db.obtener_nombre_categoria_por_profesor(email)

        print(f"[DEBUG] Buscando categoría con nombre exacto: '{limpiar_texto(curso_nombre)}'")
        print("[DEBUG] Categorías disponibles en el servidor:")
        for c in guild.categories:
            print(f" - '{limpiar_texto(c.name)}'")

        categoria = next(
            (c for c in guild.categories if limpiar_texto(c.name) == limpiar_texto(curso_nombre)),
            None
        )

        if not categoria:
            categoria = next(
                (c for c in guild.categories if limpiar_texto(curso_nombre) in limpiar_texto(c.name)),
                None
            )
            if categoria:
                print(f"[DEBUG] Se encontró categoría parcialmente coincidente: '{categoria.name}'")

        if not categoria:
            await send_func("❌ Categoría del curso no encontrada.")
            return

        rol_alumno = disnake.utils.find(lambda r: r.name.lower() == "alumno", guild.roles)
        rol_curso = disnake.utils.get(guild.roles, name=curso_nombre)

        if not rol_alumno or not rol_curso:
            await send_func("❌ Roles requeridos no encontrados.")
            return

        candidatos = [
            m for m in guild.members
            if rol_alumno in m.roles and rol_curso in m.roles
        ]

        if not candidatos:
            await send_func("❌ No hay alumnos válidos para votar.")
            return

        votacion_data = {
            "candidatos": {c.id: {"usuario": c, "votos": 0} for c in candidatos},
            "votantes": defaultdict(lambda: {"votado_a": set(), "usos": {"verde": False, "rojo": False, "blanco": False}}),
        }

        self.votaciones[guild.id] = votacion_data

        embed = disnake.Embed(title="🗳️ Votación para Delegado", description="Pulsa para votar a los candidatos")
        for c in candidatos:
            embed.add_field(name=c.display_name, value="✅ 0 | ❌ 0 | ⚪ 0", inline=False)

        view = VotacionView(candidatos, self.votaciones, guild.id, self.terminar_votacion)
        await send_func(embed=embed, view=view)

    async def terminar_votacion(self, guild_id, canal):
        datos = self.votaciones.get(guild_id)
        if not datos:
            return

        resultados = [(c["usuario"], c["votos"]) for c in datos["candidatos"].values()]
        max_votos = max(r[1] for r in resultados)
        ganadores = [u for u, v in resultados if v == max_votos]

        if len(ganadores) == 1:
            await canal.send(f"✅ El delegado elegido es **{ganadores[0].mention}** con {max_votos} votos.")
        else:
            await canal.send(f"⚠️ Hubo empate entre: {', '.join(u.mention for u in ganadores)}. Se generará nueva votación.")

        del self.votaciones[guild_id]

class VotacionView(disnake.ui.View):
    def __init__(self, candidatos, votaciones, guild_id, terminar_callback):
        super().__init__(timeout=None)
        self.votaciones = votaciones
        self.guild_id = guild_id
        self.terminar_callback = terminar_callback

        for candidato in candidatos:
            self.add_item(BotonVotar(candidato.id, "✅", "verde"))
            self.add_item(BotonVotar(candidato.id, "❌", "rojo"))
            self.add_item(BotonVotar(candidato.id, "⚪", "blanco"))

    async def interaction_check(self, interaction: disnake.MessageInteraction):
        return True

class BotonVotar(disnake.ui.Button):
    def __init__(self, candidato_id, emoji, tipo):
        super().__init__(style=disnake.ButtonStyle.secondary, label="", emoji=emoji)
        self.candidato_id = candidato_id
        self.tipo = tipo

    async def callback(self, interaction: disnake.MessageInteraction):
        datos = self.view.votaciones[self.view.guild_id]
        votante = interaction.user.id

        if datos["votantes"][votante]["usos"][self.tipo]:
            await interaction.response.send_message("❌ Ya usaste este tipo de voto.", ephemeral=True)
            return

        if self.candidato_id in datos["votantes"][votante]["votado_a"]:
            await interaction.response.send_message("❌ Ya votaste a este alumno.", ephemeral=True)
            return

        datos["votantes"][votante]["votado_a"].add(self.candidato_id)
        datos["votantes"][votante]["usos"][self.tipo] = True

        if self.tipo == "verde":
            datos["candidatos"][self.candidato_id]["votos"] += 1
        elif self.tipo == "rojo":
            datos["candidatos"][self.candidato_id]["votos"] -= 1

        await interaction.response.send_message("✅ Voto registrado.", ephemeral=True)

        if all(v["usos"]["verde"] or v["usos"]["blanco"] for v in datos["votantes"].values()):
            await self.view.terminar_callback(self.view.guild_id, interaction.channel)

def setup(bot):
    bot.add_cog(VotacionDelegadoCogs(bot))