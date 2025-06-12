using Newtonsoft.Json;
using System.Text;
using TFGClient.Models;
namespace TFGClient
{
    public partial class AsignarAsignaturaProfesor : ContentPage
    {
        private List<Profesor> _profesores;
        private int _instiId;

        public AsignarAsignaturaProfesor(List<Profesor> listaProfesores)
        {
            InitializeComponent();

            var profe = SesionUsuario.Instancia.ProfesorLogueado;
            if (profe != null)
                _instiId = profe.InstiID;

            ProfesorPicker.ItemsSource = listaProfesores;
            ProfesorPicker.ItemDisplayBinding = new Binding("NombreCompleto");

            _ = CargarCategoriasDesdeServidorAsync();
        }

        private async Task CargarCategoriasDesdeServidorAsync()
        {
            try
            {
                var dataToSend = new { InstiID = _instiId };
                var json = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                var response = await client.PostAsync("http://13.38.70.221:5000/obtener-categorias-para-tutor", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var categorias = JsonConvert.DeserializeObject<List<string>>(jsonResponse);
                    GradoPicker.ItemsSource = categorias;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudieron cargar los cursos: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error de red: {ex.Message}", "OK");
            }
        }


        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            if (ProfesorPicker.SelectedItem is not Profesor profesorSeleccionado ||
                GradoPicker.SelectedItem is not string cursoGrado ||
                string.IsNullOrWhiteSpace(AsignaturaEntry.Text))
            {
                await DisplayAlert("Error", "Por favor, completa todos los campos antes de guardar.", "OK");
                return;
            }

            var dataToSend = new
            {
                InstiID = _instiId,
                cursoGrado,
                asignatura = AsignaturaEntry.Text.Trim(),
                discordId = profesorSeleccionado.DiscordID  // ✅ añadido
            };

            var json = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync("http://13.38.70.221:5000/crear-asignatura", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Asignatura y categoría creadas correctamente en Discord.", "OK");
                    await Navigation.PopModalAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error desde el servidor: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error de red: {ex.Message}", "OK");
            }
        }
    }
}
