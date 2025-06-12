using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using TFGClient.Models;
using TFGClient.Services;
using System.Collections.Generic;
using System.Linq;
using System;

namespace TFGClient
{
    public partial class AsignarDelegado : ContentPage
    {
        private readonly DatabaseService _databaseService = new DatabaseService();
        private Alumno alumnoSeleccionado;
        private readonly int _instiId;
        private readonly int _cursoId;
        private List<Alumno> _alumnos;

        public AsignarDelegado(List<Alumno> alumnos, int instiId, int cursoId)
        {
            InitializeComponent();
            _instiId = instiId;
            _cursoId = cursoId;
            _alumnos = alumnos;

            // Inicializar datos visuales
            AlumnosCollectionView.ItemsSource = alumnos.Where(a => a.IsDelegado == 0).ToList();
            AlumnosCollectionView.SelectionChanged += OnAlumnoSeleccionado;

            _ = CargarDelegado(); // async fire-and-forget
        }

        private async Task CargarDelegado()
        {
            try
            {
                var delegado = await _databaseService.ObtenerDelegadoPorCurso(_cursoId);

                if (delegado != null)
                    DelegadoLabel.Text = delegado.Nombre;
                else
                    DelegadoLabel.Text = "Sin asignar";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el delegado: {ex.Message}");
                await DisplayAlert("Error", "Hubo un problema al cargar el delegado", "OK");
            }
        }

        private void OnAlumnoSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                alumnoSeleccionado = (Alumno)e.CurrentSelection.First();
                AsignarDelegadoButton.IsEnabled = true;
            }
            else
            {
                alumnoSeleccionado = null;
                AsignarDelegadoButton.IsEnabled = false;
            }
        }

        private async void AsignarDelegado_Clicked(object sender, EventArgs e)
        {
            if (alumnoSeleccionado == null)
            {
                await DisplayAlert("Error", "Selecciona un alumno.", "OK");
                return;
            }

            try
            {
                // 1. Actualiza base de datos local
                bool actualizado = await _databaseService.AsignarDelegado(_cursoId, alumnoSeleccionado.ID);
                if (!actualizado)
                {
                    await DisplayAlert("Error", "No se pudo actualizar el delegado en la base de datos.", "OK");
                    return;
                }

                // 2. Llamada a Flask para aplicar rol en Discord
                var dataToSend = new
                {
                    InstiID = _instiId,
                    DiscordID = alumnoSeleccionado.DiscordID
                };

                var jsonData = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                var response = await client.PostAsync("http://13.38.70.221:5000/asignar-delegado", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Delegado asignado correctamente.", "OK");
                    await Navigation.PopModalAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Fallo en Discord: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Excepción: {ex.Message}", "OK");
            }
        }

        private async void CerrarModal_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
