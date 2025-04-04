using Dapper;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using TFGClient.Models;

namespace TFGClient.Services
{
    public class DatabaseService
    {
        private readonly string connectionString = "server=13.38.70.221;user=admin;database=DiscordDatabase;port=3306;password=User@123;";

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

        public ObservableCollection<Rol> ObtenerTodosLosRoles()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var result = connection.Query<Rol>("SELECT * FROM Roles");
            return new ObservableCollection<Rol>(result);
        }
    }
}
