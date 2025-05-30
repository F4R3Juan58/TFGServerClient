﻿using Dapper;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using TFGClient.Models;

namespace TFGClient.Services
{
    public class DatabaseService
    {
        private readonly string connectionString = "server=13.38.70.221;user=tfg;database=DiscordDatabase;port=3306;password=2DamTfg;";

        public ObservableCollection<Comunidad> ObtenerTodasLasComunidades()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var result = connection.Query<Comunidad>("SELECT * FROM Comunidad_Autonoma");
            return new ObservableCollection<Comunidad>(result);
        }

        public ObservableCollection<Curso> ObtenerTodosLosCursos()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var result = connection.Query<Curso>("SELECT * FROM Cursos");
            return new ObservableCollection<Curso>(result);
        }

        public ObservableCollection<Instituto> ObtenerTodosLosInstitutos()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var result = connection.Query<Instituto>("SELECT * FROM Institutos");
            return new ObservableCollection<Instituto>(result);
        }

        public ObservableCollection<Alumno> ObtenerTodosLosAlumnos()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var result = connection.Query<Alumno>("SELECT * FROM Alumnos");
            return new ObservableCollection<Alumno>(result);
        }

        public ObservableCollection<Profesor> ObtenerTodosLosProfesores()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var result = connection.Query<Profesor>("SELECT * FROM Profesores");
            return new ObservableCollection<Profesor>(result);
        }

        public Alumno? ObtenerAlumnoPorEmailYPassword(string email, string contraseñaHash)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            return connection.QueryFirstOrDefault<Alumno>(
                "SELECT * FROM Alumnos WHERE Email = @Email AND password = @password",
                new { Email = email, password = contraseñaHash });
        }

        public Profesor? ObtenerProfesorPorEmailYPassword(string email, string contraseñaHash)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            return connection.QueryFirstOrDefault<Profesor>(
                "SELECT * FROM Profesores WHERE Email = @Email AND password = @password",
                new { Email = email, password = contraseñaHash });
        }

        public Alumno? ObtenerAlumnoPorEmail(string email)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            return connection.QueryFirstOrDefault<Alumno>(
                "SELECT * FROM Alumnos WHERE Email = @Email",
                new { Email = email }
            );
        }

        public Profesor? ObtenerProfesorPorEmail(string email)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            return connection.QueryFirstOrDefault<Profesor>(
                "SELECT * FROM Profesores WHERE Email = @Email",
                new { Email = email }
            );
        }

        public ObservableCollection<Rol> ObtenerTodosLosRoles()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var result = connection.Query<Rol>("SELECT * FROM Roles");
            return new ObservableCollection<Rol>(result);
        }
        public bool InsertarAlumno(Alumno alumno)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"INSERT INTO Alumnos 
                    (Nombre, Apellido, password, Email, ComunidadID, InstiID, CursoID, RolID, IsDelegado, Puntos, DiscordID) 
                    VALUES 
                    (@Nombre, @Apellido, @password, @Email, @ComunidadID, @InstiID, @CursoID, @RolID, @IsDelegado, @Puntos, @DiscordID)";

            int rows = connection.Execute(query, alumno);
            return rows > 0;
        }

        public bool InsertarProfesor(Profesor profesor)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"INSERT INTO Profesores 
                    (Nombre, Apellido, password, Email, ComunidadID, InstiID, RolID, IsJefe, IsTutor, CursoID, DiscordID) 
                    VALUES 
                    (@Nombre, @Apellido, @password, @Email, @ComunidadID, @InstiID, @RolID, @IsJefe, @IsTutor, @CursoID, @DiscordID)";

            int rows = connection.Execute(query, profesor);
            return rows > 0;
        }

        public bool ExisteEmail(string email)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var alumno = connection.QueryFirstOrDefault<string>("SELECT Email FROM Alumnos WHERE Email = @Email", new { Email = email });
            if (alumno != null) return true;

            var profesor = connection.QueryFirstOrDefault<string>("SELECT Email FROM Profesores WHERE Email = @Email", new { Email = email });
            return profesor != null;
        }

        public bool ActualizarAlumno(int alumnoId, Alumno alumno)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var (setSql, parametros) = UpdateHelper.GenerarUpdateDinamico(alumno, "ID", alumnoId);

            if (string.IsNullOrWhiteSpace(setSql))
                return false;

            string query = $"UPDATE Alumnos SET {setSql} WHERE ID = @ID";
            return connection.Execute(query, parametros) > 0;
        }

        public bool ActualizarProfesor(int profesorId, Profesor profesor)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var (setSql, parametros) = UpdateHelper.GenerarUpdateDinamico(profesor, "ID", profesorId);

            if (string.IsNullOrWhiteSpace(setSql))
                return false;

            string query = $"UPDATE Profesores SET {setSql} WHERE ID = @ID";
            return connection.Execute(query, parametros) > 0;
        }

        public bool EliminarAlumno(int alumnoId)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = "DELETE FROM Alumnos WHERE ID = @ID";
                int filasAfectadas = connection.Execute(query, new { ID = alumnoId });

                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar alumno: {ex.Message}");
                return false;
            }
        }

        public bool EliminarProfesor(int profesorId)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = "DELETE FROM Profesores WHERE ID = @ID";
                int filasAfectadas = connection.Execute(query, new { ID = profesorId });

                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar profesor: {ex.Message}");
                return false;
            }
        }

        public bool EliminarServidorDiscord(int servidorId)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = "DELETE FROM ServidoresDiscord WHERE ID = @ID";
                int filasAfectadas = connection.Execute(query, new { ID = servidorId });

                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar servidor: {ex.Message}");
                return false;
            }
        }
    }
}
