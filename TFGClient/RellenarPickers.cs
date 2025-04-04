using System.Collections.ObjectModel;
using System.Text;
using MySql.Data.MySqlClient;

namespace TFGClient
{
    class RellenarPickers
    {
        private string connectionString = "server=13.38.70.221;user=admin;database=DiscordDatabase;port=3306;password=User@123;";

        public ObservableCollection<Tareas> cargarComunidadAutonoma()
        {
            var tareas = new ObservableCollection<Tareas>();
            var Query = "SELECT * FROM Tarea";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Verifica si la tabla Departamento tiene datos
                
                using (var command = new MySqlConnection(Query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tareas.Add(new Tareas
                            {
                                Id = reader.GetInt32(0),
                                Marcado = reader.GetBoolean(1),
                                Nombre = reader.GetString(2),
                            });
                        }
                    }
                }
            }
            return tareas;
        }
    }
}
