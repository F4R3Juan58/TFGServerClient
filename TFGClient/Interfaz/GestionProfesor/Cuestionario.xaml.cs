using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using TFGClient.Models;
using TFGClient.Services;

namespace TFGClient
{
    public partial class Cuestionario : ContentPage
    {
        public ObservableCollection<CuestionarioForm> Cuestionarios { get; set; }
        string asignatura;

        public Cuestionario(string Asignatura)
        {
            InitializeComponent();
            asignatura = Asignatura;
            Cuestionarios = new ObservableCollection<CuestionarioForm>();
            CuestionariosCollectionView.ItemsSource = Cuestionarios;
        }

        // Clase para representar una pregunta y sus respuestas
        public class CuestionarioForm
        {
            public string Pregunta { get; set; }
            public string Correcta { get; set; }
            public string Respuesta1 { get; set; }
            public string Respuesta2 { get; set; }
            public string Respuesta3 { get; set; }
            public string Respuesta4 { get; set; }
        }

        // Cuando se presiona el botón "Añadir pregunta"
        private void OnAgregarPreguntaClicked(object sender, EventArgs e)
        {
            string pregunta = PreguntaEntry.Text;
            string respuesta1 = Respuesta1Entry.Text;
            string respuesta2 = Respuesta2Entry.Text;
            string respuesta3 = Respuesta3Entry.Text;
            string respuesta4 = Respuesta4Entry.Text;

            // Verificar si todos los campos están completos
            if (string.IsNullOrEmpty(pregunta) || string.IsNullOrEmpty(respuesta1) || string.IsNullOrEmpty(respuesta2) ||
                string.IsNullOrEmpty(respuesta3) || string.IsNullOrEmpty(respuesta4))
            {
                DisplayAlert("Error", "Por favor, rellena todos los campos", "OK");
                return;
            }

            // Agregar la nueva pregunta a la colección
            Cuestionarios.Add(new CuestionarioForm
            {
                Pregunta = pregunta,
                Correcta = respuesta1,
                Respuesta1 = respuesta1,
                Respuesta2 = respuesta2,
                Respuesta3 = respuesta3,
                Respuesta4 = respuesta4
            });

            // Limpiar los campos
            PreguntaEntry.Text = string.Empty;
            Respuesta1Entry.Text = string.Empty;
            Respuesta2Entry.Text = string.Empty;
            Respuesta3Entry.Text = string.Empty;
            Respuesta4Entry.Text = string.Empty;
        }

        // Cuando se presiona el botón "Enviar Cuestionarios"
        private async void OnEnviarCuestionariosClicked(object sender, EventArgs e)
        {
            if (Cuestionarios.Count == 0)
            {
                await DisplayAlert("Error", "No hay preguntas para enviar", "OK");
                return;
            }

            // Crear un objeto para enviar al servidor
            var profesor = SesionUsuario.Instancia.ProfesorLogueado;
            var instiID = profesor.InstiID;
            var discordID = profesor.DiscordID;
             // Asegúrate de que la propiedad Asignatura esté disponible en el objeto ProfesorLogueado

            var data = new
            {
                InstiID = instiID,
                DiscordID = discordID,
                Asignatura = asignatura,
                Cuestionarios = Cuestionarios
            };

            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://13.38.70.221:5000/api/enviar_cuestionarios";  // Reemplaza con tu URL de servidor Flask
                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Éxito", "Cuestionarios enviados correctamente", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo enviar los cuestionarios", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Hubo un error: {ex.Message}", "OK");
                }
            }
        }

        private async void OnCerrarModalClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(); // o PopAsync si usas Navigation.PushAsync
        }

    }
}