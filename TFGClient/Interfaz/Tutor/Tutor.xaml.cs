using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using TFGClient.Models;
using TFGClient.Services;
using TFGClient.Services;

namespace TFGClient.Interfaz;

public partial class Tutor : ContentPage
{
	public Tutor()
	{
		InitializeComponent();
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        if (profesor != null)
        {
            NombreProfesor.Text = $"{profesor.Nombre} {profesor.Apellido}";
        }
    }

    private void crearCanalTemporal(object sender, EventArgs e)
    {

    }

    private async void asignarDelegado(object sender, EventArgs e)
    {
        var modal = new AsignarDelegado();
        await Navigation.PushModalAsync(modal);
    }

    private void revocarDelegado(object sender, EventArgs e)
    {

    }

    private void crearVotacion(object sender, EventArgs e)
    {

    }

    private void crearCanalFCT(object sender, EventArgs e)
    {

    }

    private void OnTutoriaSelected(object sender, SelectionChangedEventArgs e)
    {

    }

    private void OnAlumnoSelected(object sender, SelectionChangedEventArgs e)
    {

    }

    private async void IniciarTutoria(object sender, EventArgs e)
    {
        try
        {
            var url = $"http://TU_IP_LOCAL:5000/api/iniciar_tutoria/{Tutoria.Text}";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Error", "No se pudo iniciar la tutoría. Verifica tu rol o conexión.", "OK");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var resultado = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (resultado != null && resultado.ContainsKey("mensaje"))
                {
                    await DisplayAlert("Éxito", resultado["mensaje"], "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Respuesta inesperada del servidor.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Excepción al iniciar tutoría: {ex.Message}", "OK");
        }
    }

}