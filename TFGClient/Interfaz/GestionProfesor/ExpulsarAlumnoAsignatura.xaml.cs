using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using TFGClient.Models;
using System.Collections.Generic;
using System.Linq;
using System;


namespace TFGClient.Interfaz
{
    public partial class ExpulsarAlumnoAsignatura : ContentPage
    {
        private readonly string categoriaId;
        private AlumnoClase alumnoSeleccionado;

        public ExpulsarAlumnoAsignatura(List<AlumnoClase> alumnos)
        {
            InitializeComponent();
            AlumnosCollectionView.ItemsSource = alumnos;
        }

        private void OnAlumnoSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                alumnoSeleccionado = (AlumnoClase)e.CurrentSelection.First();
                ExpulsarButton.IsEnabled = true;
            }
            else
            {
                alumnoSeleccionado = null;
                ExpulsarButton.IsEnabled = false;
            }
        }

        private async void ExpulsarButton_Clicked(object sender, EventArgs e)
        {
            if (alumnoSeleccionado == null) return;

            using var client = new HttpClient();
            var url = "http://13.38.70.221:5000/api/expulsar_alumno";
            var data = new
            {
                alumno_id = alumnoSeleccionado.Id
            };

            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
                await DisplayAlert("Éxito", "Alumno expulsado correctamente", "OK");
            else
                await DisplayAlert("Error", "No se pudo expulsar al alumno", "OK");

            await Navigation.PopModalAsync();
        }

        private async void CerrarModal_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}