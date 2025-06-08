using Dapper;
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

        public Administradores? ObtenerAdministradorPorEmailYPassword(string email, string contraseñaHash)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            return connection.QueryFirstOrDefault<Administradores>(
                "SELECT * FROM Administradores WHERE Email = @Email AND password = @password",
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
                    (Nombre, Apellido, Password, Email, ComunidadID, InstiID, CursoID, RolID, IsDelegado, Puntos, DiscordID) 
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
                    (Nombre, Apellido, Password, Email, ComunidadID, InstiID, RolID, IsJefe, IsTutor, CursoID, DiscordID) 
                    VALUES 
                    (@Nombre, @Apellido, @password, @Email, @ComunidadID, @InstiID, @RolID, @IsJefe, @IsTutor, @CursoID, @DiscordID)";

            int rows = connection.Execute(query, profesor);
            return rows > 0;
        }

        public bool InsertarAdministrador(Administradores administrador)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"INSERT INTO Administradores 
                    (Nombre, Apellido, Password, Email, InstiID, RolID, DiscordID) 
                    VALUES 
                    (@Nombre, @Apellido, @password, @Email, @InstiID, @RolID, @DiscordID)";

            int rows = connection.Execute(query, administrador);
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

        public string? ObtenerNombreInstituto(int institutoId)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = "SELECT Nombre FROM Institutos WHERE ID = @ID";

                // Usamos QueryFirstOrDefault para obtener un único resultado o null si no existe
                string? nombreInstituto = connection.QueryFirstOrDefault<string?>(query, new { ID = institutoId });

                return nombreInstituto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el nombre del instituto: {ex.Message}");
                return null;
            }
        }

        public bool EliminarServidor(int instiId)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string query = "DELETE FROM ServidoresDiscord WHERE InstiID = @InstiID";

                int filasAfectadas = connection.Execute(query, new { InstiID = instiId });

                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar el servidor: {ex.Message}");
                return false;
            }
        }


        public bool MarcarComoTutor(int profesorId)
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            string query = "UPDATE Profesores SET IsTutor = 1 WHERE ID = @ID";
            int result = conn.Execute(query, new { ID = profesorId });
            return result > 0;
        }

        public bool AsignarCursoAlProfesor(int profesorId, string categoria)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                // 1. Obtener el rol relacionado con esa categoría
                var rol = conn.QueryFirstOrDefault<dynamic>(
                    "SELECT ID FROM Roles WHERE NombreRol = @NombreRol",
                    new { NombreRol = categoria });

                if (rol == null)
                    return false;

                int rolId = rol.ID;

                // 2. Obtener curso relacionado con ese RolID
                var curso = conn.QueryFirstOrDefault<dynamic>(
                    "SELECT ID FROM Cursos WHERE RolID = @RolID",
                    new { RolID = rolId });

                if (curso == null)
                    return false;

                int cursoId = curso.ID;

                // 3. Actualizar el curso del profesor
                int filasAfectadas = conn.Execute(
                    "UPDATE Profesores SET CursoID = @CursoID WHERE ID = @ProfesorID",
                    new { CursoID = cursoId, ProfesorID = profesorId });

                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al asignar curso al profesor: {ex.Message}");
                return false;
            }
        }
        public ObservableCollection<Alumno> ObtenerAlumnosPorInstitutoYCurso(int instiId, int cursoId)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var alumnos = connection.Query<Alumno>(
                "SELECT * FROM Alumnos WHERE InstiID = @InstiID AND CursoID = @CursoID",
                new { InstiID = instiId, CursoID = cursoId });

            return new ObservableCollection<Alumno>(alumnos);
        }

        public bool EsAdministrador(string email)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                // Verifica si el correo existe en la tabla Administradores
                var admin = conn.QueryFirstOrDefault<dynamic>(
                    "SELECT ID FROM Administradores WHERE Email = @Email",
                    new { Email = email });

                return admin != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al comprobar administrador: {ex.Message}");
                return false;
            }
        }

        public bool EsProfesor(string email)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                // Verifica si el correo existe en la tabla Administradores
                var profesor = conn.QueryFirstOrDefault<dynamic>(
                    "SELECT ID FROM Profesores WHERE Email = @Email",
                    new { Email = email });

                return profesor != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al comprobar profesor: {ex.Message}");
                return false;
            }
        }

        public bool CambiarEstadoIsJefe(int profesorId)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                conn.Open();

                // Actualizar IsJefe al valor contrario (true -> false, false -> true)
                int filasAfectadas = conn.Execute(
                    @"UPDATE Profesores 
              SET IsJefe = NOT IsJefe
              WHERE ID = @ProfesorID",
                    new { ProfesorID = profesorId });

                return filasAfectadas > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cambiar estado de IsJefe: {ex.Message}");
                return false;
            }
        }

    }
}
