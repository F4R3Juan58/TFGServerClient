import aiomysql
import asyncio

class Database:
    def __init__(self):
        self.db_config = {
            'host': '13.38.70.221',
            'port': 3306,
            'user': 'tfg',
            'password': '2DamTfg',
            'db': 'DiscordDatabase',
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
                await cur.execute("SELECT ID FROM ServidoresDiscord WHERE InstiID = %s", (insti_id,))
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