using Newtonsoft.Json;
using System.Text;
using TFGClient.Models;
using TFGClient.Services;

namespace TFGClient
{
    public partial class NuevoTutor : ContentPage
    {
        private List<Profesor> _profesores;
        private int _instiId;
        public NuevoTutor(List<Profesor> listaProfesores, int instiId)
        {
            InitializeComponent();
            _instiId = instiId;
            profesorPicker.ItemsSource = listaProfesores;
            profesorPicker.ItemDisplayBinding = new Binding("NombreCompleto");
            _ = CargarCategoriasAsync();  // carga inicial
        }

        private async Task CargarCategoriasAsync()
        {
            try
            {
                var dataToSend = new
                {
                    InstiID = _instiId
                };

                var json = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                var response = await client.PostAsync("http://13.38.70.221:5000/obtener-categorias-para-tutor", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var categorias = JsonConvert.DeserializeObject<List<string>>(jsonString);
                    cursoGradoPicker.ItemsSource = categorias;
                }
                else
                {
                    await DisplayAlert("Error", "No se pudieron obtener las categorías", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error de conexión: {ex.Message}", "OK");
            }
        }

        private async void Cerrar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void Confirmar_Clicked(object sender, EventArgs e)
        {
            if (profesorPicker.SelectedItem is not Profesor profesorSeleccionado ||
                cursoGradoPicker.SelectedItem is not string categoria)
            {
                await DisplayAlert("Error", "Debes seleccionar un profesor y una categoría.", "OK");
                return;
            }

            var dataToSend = new
            {
                InstiID = _instiId,
                categoria = categoria,
                discordId = profesorSeleccionado.DiscordID
            };

            var jsonData = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync("http://13.38.70.221:5000/asignar-tutor", content);

                if (response.IsSuccessStatusCode)
                {
                    // Marcar como tutor en base de datos local
                    var dbService = new DatabaseService();
                    bool actualizado = dbService.MarcarComoTutor(profesorSeleccionado.ID);

                    if (!actualizado)
                        await DisplayAlert("Advertencia", "Tutor asignado en Discord, pero no se actualizó el campo IsTutor localmente.", "OK");

                    // ➕ Nuevo paso: actualizar curso según categoría
                    bool cursoAsignado = dbService.AsignarCursoAlProfesor(profesorSeleccionado.ID, categoria);

                    if (!cursoAsignado)
                        await DisplayAlert("Advertencia", "No se pudo asignar el curso correspondiente al profesor.", "OK");

                    MessagingCenter.Send(this, "TutorAsignado");
                    await DisplayAlert("Éxito", "Tutor asignado correctamente.", "OK");
                    await Navigation.PopModalAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Fallo al asignar tutor: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Excepción: {ex.Message}", "OK");
            }
        }
    }
}
