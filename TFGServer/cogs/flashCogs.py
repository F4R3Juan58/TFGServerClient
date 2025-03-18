import discord
from discord.ext import commands
from flask import Flask, request, jsonify
import threading
import asyncio
import traceback

class flaskCog(commands.Cog):
    def __init__(self, bot):
        self.bot = bot

        self.app = Flask(__name__)

        @self.app.route('/addRol', methods=['POST'])
        def addRol():
            try:
                data = request.get_json()
                print("Datos recibidos:", data)

                rol_nombre = data.get('name')
                guild_id = 1328440231434649664

                guild = self.bot.get_guild(guild_id)

                future = asyncio.run_coroutine_threadsafe(
                    guild.create_role(name=rol_nombre), self.bot.loop
                )
                new_role = future.result() 

                print(f"Rol '{rol_nombre}' creado exitosamente.")

                return jsonify({"message": f"Rol '{rol_nombre}' creado con éxito en el servidor {guild.name}."}), 200

            except Exception as e:
                # Agregamos más detalles del error para depuración
                error_details = traceback.format_exc()
                print(f"Error al crear el rol: {error_details}")
                return jsonify({"error": f"Hubo un error: {str(e)}", "details": error_details}), 500
            

        @self.app.route('/addTxtCanal', methods=['POST'])
        def addTxtCanal():
            try:
                data = request.get_json()
                print("Datos recibidos:", data)

                canal_nombre = data.get('name')
                guild_id = 1328440231434649664

                guild = self.bot.get_guild(guild_id)

                if not guild:
                    return jsonify({"error": f"No se encontró el servidor con ID {guild_id}"}), 404

                print(f"Servidor encontrado: {guild.name}")

                future = asyncio.run_coroutine_threadsafe(
                    guild.create_text_channel (name=canal_nombre), self.bot.loop
                )
                new_chanel = future.result()

                print(f"Canal de texto '{canal_nombre}' creado exitosamente.")

                return jsonify({"message": f"Canal '{canal_nombre}' creado con éxito en el servidor {guild.name}."}), 200

            except Exception as e:
                # Agregamos más detalles del error para depuración
                error_details = traceback.format_exc()
                print(f"Error al crear el canal: {error_details}")
                return jsonify({"error": f"Hubo un error: {str(e)}", "details": error_details}), 500

        @self.app.route('/addVcCanal', methods=['POST'])
        def addVcCanal():
            try:
                data = request.get_json()
                print("Datos recibidos:", data)

                canal_nombre = data.get('name')
                guild_id = 1328440231434649664

                guild = self.bot.get_guild(guild_id)

                if not guild:
                    return jsonify({"error": f"No se encontró el servidor con ID {guild_id}"}), 404

                print(f"Servidor encontrado: {guild.name}")

                future = asyncio.run_coroutine_threadsafe(
                    guild.create_voice_channel (name=canal_nombre), self.bot.loop
                )
                new_chanel = future.result()

                print(f"Canal de voz '{canal_nombre}' creado exitosamente.")

                return jsonify({"message": f"Canal '{canal_nombre}' creado con éxito en el servidor {guild.name}."}), 200

            except Exception as e:
                # Agregamos más detalles del error para depuración
                error_details = traceback.format_exc()
                print(f"Error al crear el canal: {error_details}")
                return jsonify({"error": f"Hubo un error: {str(e)}", "details": error_details}), 500
            
            
        @self.app.route('/addCategoria', methods=['POST'])
        def addCategoria():
            try:
                data = request.get_json()
                print("Datos recibidos:", data)

                rol_nombre = data.get('name')
                guild_id = 1328440231434649664

                guild = self.bot.get_guild(guild_id)

                future = asyncio.run_coroutine_threadsafe(
                    guild.create_category_channel(name=rol_nombre), self.bot.loop
                )
                new_role = future.result() 

                print(f"Rol '{rol_nombre}' creado exitosamente.")

                return jsonify({"message": f"Rol '{rol_nombre}' creado con éxito en el servidor {guild.name}."}), 200

            except Exception as e:
                # Agregamos más detalles del error para depuración
                error_details = traceback.format_exc()
                print(f"Error al crear el rol: {error_details}")
                return jsonify({"error": f"Hubo un error: {str(e)}", "details": error_details}), 500


        @self.app.route('/getCategorias', methods=['GET'])
        def obtener_categorias():
            guild_id = 1328440231434649664  # Cambiar este ID según el servidor que desees

            # Obtener el servidor
            guild = self.bot.get_guild(guild_id)
            if not guild:
                return jsonify({"error": "No se encontró el servidor"}), 404

            # Obtener las categorías del servidor
            categorias = guild.categories

            # Preparar los nombres de las categorías para enviarlas
            categorias_nombres = [categoria.name for categoria in categorias]

            # Enviar las categorías como respuesta JSON
            return jsonify({"categorias": categorias_nombres}), 200


        def run_flask():
            self.app.run(port=5000)

        # Iniciar Flask en un hilo para no bloquear el bot
        threading.Thread(target=run_flask).start()

# Registrar el Cog
async def setup(bot):
    await bot.add_cog(flaskCog(bot))  # No usar 'await' aquí
