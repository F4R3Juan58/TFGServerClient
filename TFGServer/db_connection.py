import aiomysql

class Database:
    def __init__(self):
        self.db_config = {
            'host': '13.38.70.221',
            'port': 3306,
            'user': 'tfg',
            'password': '2DamTfg',
            'db': 'DiscordDatabase',
            'autocommit': False
        }
        self.pool = None

    async def init_pool(self):
        if self.pool is None:
            self.pool = await aiomysql.create_pool(**self.db_config)

    async def close_pool(self):
        if self.pool:
            self.pool.close()
            await self.pool.wait_closed()

    async def check_server_exists(self, insti_id: int) -> bool:
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor() as cur:
                await cur.execute(
                    "SELECT ID FROM ServidoresDiscord WHERE InstiID = %s",
                    (insti_id,)
                )
                result = await cur.fetchone()
                return result is not None

    async def save_server(self, insti_id: int, nombre: str, discord_id: int) -> bool:
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor() as cur:
                await cur.execute(
                    "INSERT INTO ServidoresDiscord (InstiID, Nombre, DiscordID) VALUES (%s, %s, %s)",
                    (insti_id, nombre, discord_id)
                )
                await conn.commit()
                return True

    async def save_invitation(self, email: str, invite_code: str):
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor() as cur:
                await cur.execute(
                    "INSERT INTO conexiones (email, invitacion) VALUES (%s, %s)",
                    (email, invite_code)
                )
                await conn.commit()

    async def get_connection_by_invite(self, invite_code: str):
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor(aiomysql.DictCursor) as cur:
                await cur.execute(
                    "SELECT * FROM conexiones WHERE invitacion = %s AND discordid IS NULL",
                    (invite_code,)
                )
                return await cur.fetchone()

    async def update_discordid_by_invite(self, invite_code: str, discordid: int):
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor() as cur:
                await cur.execute(
                    "UPDATE conexiones SET discordid = %s WHERE invitacion = %s",
                    (discordid, invite_code)
                )
                await conn.commit()

    async def update_discordid_by_email_all(self, email: str, discordid: int) -> bool:
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor() as cur:
                tablas = ['Alumnos', 'Profesores', 'Administradores']
                actualizado = False
                for tabla in tablas:
                    await cur.execute(
                        f"UPDATE {tabla} SET discordid = %s WHERE email = %s",
                        (discordid, email)
                    )
                    if cur.rowcount > 0:
                        actualizado = True

                if actualizado:
                    await cur.execute(
                        "UPDATE conexiones SET discordid = %s WHERE email = %s",
                        (discordid, email)
                    )
                    await cur.execute(
                        "DELETE FROM conexiones WHERE email = %s OR discordid = %s",
                        (email, discordid)
                    )
                    await conn.commit()

                return actualizado

    async def obtener_insti_id_por_email(self, email: str) -> int | None:
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor(aiomysql.DictCursor) as cur:
                for tabla in ['Profesores', 'Administradores']:
                    await cur.execute(
                        f"SELECT InstiID FROM {tabla} WHERE Email = %s",
                        (email,)
                    )
                    resultado = await cur.fetchone()
                    if resultado:
                        return resultado['InstiID']
        return None

    async def obtener_servidor_por_insti_id(self, insti_id: int) -> dict | None:
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor(aiomysql.DictCursor) as cur:
                await cur.execute(
                    "SELECT * FROM ServidoresDiscord WHERE InstiID = %s",
                    (insti_id,)
                )
                return await cur.fetchone()

    async def obtener_rol_por_grado_y_curso(self, grado: str, curso_nombre: str) -> dict | None:
        await self.init_pool()
        async with self.pool.acquire() as conn:
            async with conn.cursor(aiomysql.DictCursor) as cur:
                await cur.execute(
                    """
                    SELECT r.ID, r.NombreRol
                    FROM Roles r
                    JOIN Cursos c ON c.RolID = r.ID
                    WHERE c.Grado = %s AND c.Nombre = %s
                    """,
                    (grado, curso_nombre)
                )
                return await cur.fetchone()