using Dapper;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;

namespace TFGClient
{
    public class Comunidad
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
    }

    public class Localidad
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
        public int ComunidadID { get; set; }
    }

    public class Instituto
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
        public int ComunidadID { get; set; }
        public string Localidad { get; set; }
    }

    public class DatabaseService
    {
        private readonly string connectionString = "server=13.38.70.221;user=tfg;database=DiscordDatabase;port=3306;password=2DamTfg;";

        public ObservableCollection<Comunidad> ObtenerComunidades()
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var comunidades = connection.Query<Comunidad>("SELECT ID, Nombre FROM Comunidad_Autonoma");
            return new ObservableCollection<Comunidad>(comunidades);
        }

        public ObservableCollection<Localidad> ObtenerLocalidadesPorComunidad(int comunidadId)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var localidades = connection.Query<Localidad>(
                "SELECT ID, Nombre, ComunidadID FROM Localidad WHERE ComunidadID = @ComunidadID",
                new { ComunidadID = comunidadId });
            return new ObservableCollection<Localidad>(localidades);
        }

        public ObservableCollection<Instituto> ObtenerInstitutosPorLocalidad(int comunidadId, string localidad)
        {
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            var institutos = connection.Query<Instituto>(
                "SELECT ID, Nombre, ComunidadID, Localidad FROM Institutos WHERE ComunidadID = @ComunidadID AND Localidad = @Localidad",
                new { ComunidadID = comunidadId, Localidad = localidad });
            return new ObservableCollection<Instituto>(institutos);
        }
    }
}

