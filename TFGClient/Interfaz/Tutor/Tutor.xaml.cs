using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using TFGClient.Models;
using TFGClient.Services;

namespace TFGClient.Interfaz;

public partial class Tutor : ContentPage
{
    private readonly DatabaseService _db = new();

    public ObservableCollection<Alumno> Alumnos { get; set; } = new();

    // Guarda el último alumno seleccionado para detectar re-selección
    private Alumno _alumnoSeleccionadoAnterior = null;

    // Bandera para evitar loops al cambiar SelectedItem programáticamente
    private bool _ignorarEvento = false;

    public Tutor()
    {
        InitializeComponent();
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;

        if (profesor != null)
        {
            NombreProfesor.Text = $"{profesor.Nombre} {profesor.Apellido}";
            CargarAlumnos(profesor.InstiID, profesor.CursoID);
        }
    }

    private void CargarAlumnos(int instiId, int cursoId)
    {
        var alumnos = _db.ObtenerAlumnosPorInstitutoYCurso(instiId, cursoId);

        // Asignamos la lista completa de objetos Alumno al ItemsSource
        AlumnosCollection.ItemsSource = alumnos;

        Alumnos.Clear();
        foreach (var alumno in alumnos)
            Alumnos.Add(alumno);
    }

    private void crearCanalTemporal(object sender, EventArgs e)
    {
        // Lógica para crear canal temporal
    }

    private async void asignarDelegado(object sender, EventArgs e)
    {
        var modal = new AsignarDelegado();
        await Navigation.PushModalAsync(modal);
    }

    private void revocarDelegado(object sender, EventArgs e)
    {
        // Lógica para revocar delegado
    }

    private async void AbrirVotacion(object sender, EventArgs e)
    {
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        if (profesor == null)
        {
            await DisplayAlert("Error", "No se encontró el profesor logueado.", "OK");
            return;
        }

        var dataToSend = new
        {
            Email = profesor.Email  // Ya lo tienes disponible
        };

        var jsonData = JsonConvert.SerializeObject(dataToSend);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsync("http://localhost:5000/abrir-votacion-delegado", content);

        if (response.IsSuccessStatusCode)
        {
            await DisplayAlert("Éxito", "Votación iniciada correctamente en Discord.", "OK");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            await DisplayAlert("Error", $"Error: {error}", "OK");
        }
    }


    private void crearCanalFCT(object sender, EventArgs e)
    {
        // Lógica para crear canal FCT
    }

    private void OnTutoriaSelected(object sender, SelectionChangedEventArgs e)
    {
        // Lógica para manejar selección de tutoria
    }

    private void OnAlumnoSelected(object sender, SelectionChangedEventArgs e)
    {
        var seleccionActual = e.CurrentSelection.FirstOrDefault() as Alumno;

        if (seleccionActual != null)
        {
            _alumnoSeleccionadoAnterior = seleccionActual;

            GridBotonesGenerales.IsVisible = false;
            GridBotonesAlumno.IsVisible = true;
        }
        else
        {
            _alumnoSeleccionadoAnterior = null;

            GridBotonesGenerales.IsVisible = true;
            GridBotonesAlumno.IsVisible = false;
        }
    }

    // Nuevo método para deseleccionar al pulsar en fondo o zona vacía
    private void OnFondoTapped(object sender, EventArgs e)
    {
        AlumnosCollection.SelectedItem = null;
        _alumnoSeleccionadoAnterior = null;

        GridBotonesGenerales.IsVisible = true;
        GridBotonesAlumno.IsVisible = false;
    }

    private async void IniciarTutoria(object sender, EventArgs e)
    {
        try
        {
            var profesor = SesionUsuario.Instancia.ProfesorLogueado;
            if (profesor == null)
            {
                await DisplayAlert("Error", "No hay profesor conectado.", "OK");
                return;
            }

            var url = "http://localhost:5000/iniciar_tutoria";  // URL corregida

            var payload = new
            {
                InstiID = profesor.InstiID,
                DiscordID = profesor.DiscordID
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "No se pudo iniciar la tutoría. Verifica tu rol o conexión.", "OK");
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);

            // Si la respuesta contiene un mensaje, muestra el resultado
            if (result != null && result.ContainsKey("mensaje"))
            {
                await DisplayAlert("Éxito", result["mensaje"], "OK");
            }
            else
            {
                await DisplayAlert("Error", "Respuesta inesperada del servidor.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Excepción al iniciar tutoría: {ex.Message}", "OK");
        }
    }
}