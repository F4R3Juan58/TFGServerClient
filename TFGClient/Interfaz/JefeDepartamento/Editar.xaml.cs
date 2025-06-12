using Newtonsoft.Json;
using System.Text;
using TFGClient.Services;
using TFGClient.Models;

namespace TFGClient
{
    public partial class Editar : ContentPage
    {
        private readonly DatabaseService _db = new();
        private readonly Profesor _profesor;
        private readonly int _instiId;

        public Editar(Profesor profesor)
        {
            InitializeComponent();
            _profesor = profesor;
            _instiId = profesor.InstiID;
            NombreProfesor.Text = profesor.NombreCompleto;

            _ = CargarAsignaturasDesdeDiscord();
        }

        private async Task CargarAsignaturasDesdeDiscord()
        {
            var payload = new
            {
                InstiID = _instiId,
                DiscordID = _profesor.DiscordID
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            var response = await client.PostAsync("http://13.38.70.221:5000/obtener-asignaturas-profesor", content);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "No se pudieron obtener las asignaturas del servidor.", "OK");
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(body);

            // Mostramos los sufijos de las asignaturas, pero dejamos la reconstrucción al backend
            Asignaturas.ItemsSource = result["actuales"];
            AsignaturasTotales.ItemsSource = result["todas"];
        }

        private async void Confirmar_Clicked(object sender, EventArgs e)
        {
            string quitar = Asignaturas.SelectedItem as string;
            string agregar = AsignaturasTotales.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(quitar))
            {
                await DisplayAlert("Error", "Selecciona una asignatura actual para quitar.", "OK");
                return;
            }

            var data = new
            {
                InstiID = _instiId,
                DiscordID = _profesor.DiscordID,
                NombreAsignaturaQuitar = quitar,
                NombreAsignaturaAgregar = agregar
            };

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            var response = await client.PostAsync("http://13.38.70.221:5000/modificar-asignatura-profesor", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "La asignación ha sido modificada.", "OK");
                await Navigation.PopModalAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", error, "OK");
            }
        }

        private async void Cerrar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
