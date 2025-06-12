using Newtonsoft.Json;
using System.Text;
using TFGClient.Models;

namespace TFGClient
{
    public partial class BloquearAsignatura : ContentPage
    {
        private int _instiId;

        public BloquearAsignatura()
        {
            InitializeComponent();

            var profe = SesionUsuario.Instancia.ProfesorLogueado;
            if (profe != null)
                _instiId = profe.InstiID;

            _ = CargarCursosBaseAsync();
            cursoPicker.SelectedIndexChanged += CursoPicker_SelectedIndexChanged;
        }

        private async Task CargarCursosBaseAsync()
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
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var categorias = JsonConvert.DeserializeObject<List<string>>(jsonString);
                    cursoPicker.ItemsSource = categorias;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudieron cargar los cursos base: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error de conexión: {ex.Message}", "OK");
            }
        }

        private async void CursoPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            asignaturaPicker.ItemsSource = null;

            if (cursoPicker.SelectedItem is not string cursoSeleccionado)
                return;

            try
            {
                var dataToSend = new
                {
                    InstiID = _instiId,
                    CursoGrado = cursoSeleccionado
                };

                var json = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                var response = await client.PostAsync("http://13.38.70.221:5000/obtener-asignaturas", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var asignaturas = JsonConvert.DeserializeObject<List<string>>(jsonString);
                    asignaturaPicker.ItemsSource = asignaturas;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudieron cargar las asignaturas: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error de red: {ex.Message}", "OK");
            }
        }

        private async void Bloquear_Clicked(object sender, EventArgs e)
        {
            if (cursoPicker.SelectedItem is not string cursoGrado ||
                asignaturaPicker.SelectedItem is not string asignatura)
            {
                await DisplayAlert("Error", "Selecciona un curso y una asignatura para eliminar.", "OK");
                return;
            }

            var dataToSend = new
            {
                InstiID = _instiId,
                CursoGrado = cursoGrado,
                Asignatura = asignatura
            };

            var json = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync("http://13.38.70.221:5000/eliminar-asignatura", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Asignatura eliminada", "La asignatura y su categoría han sido eliminadas correctamente.", "OK");
                    await Navigation.PopModalAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudo eliminar la asignatura: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error de conexión: {ex.Message}", "OK");
            }
        }

        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}