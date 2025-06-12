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
    private string _tutoriaSeleccionadaAnterior = null; 


    // Bandera para evitar loops al cambiar SelectedItem programáticamente
    private bool _ignorarEvento = false;

    public Tutor()
    {
        InitializeComponent();
    }

    // Evento que se ejecuta cada vez que la página aparece
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Cargar los datos nuevamente
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;

        if (profesor != null)
        {
            NombreProfesor.Text = $"{profesor.Nombre} {profesor.Apellido}";
            CargarAlumnos(profesor.InstiID, profesor.CursoID);
            ObtenerTutorias();  // Cargar las tutorías también cuando la página aparece
        }
    }

    private async void ObtenerTutorias()
    {
        var instiId = SesionUsuario.Instancia.ProfesorLogueado.InstiID;
        var profesorId = SesionUsuario.Instancia.ProfesorLogueado.DiscordID;
        await DisplayAlert("Debug", $"{instiId}, {profesorId}", "OK");


        // Crear un objeto con los datos necesarios
        var dataToSend = new
        {
            insti_id = instiId,
            profesor_id = profesorId
        };

        var jsonData = JsonConvert.SerializeObject(dataToSend);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsync("http://13.38.70.221:5000/obtener-tutorias-profesor", content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var tutorias = JsonConvert.DeserializeObject<TutoriasResponse>(responseBody);

            // Limpiar la lista de tutorías anterior y actualizarla con los nuevos datos
            TutoriasCollection.ItemsSource = tutorias.Tutorias;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            await DisplayAlert("Error", $"No se pudieron obtener las tutorías: {error}", "OK");
        }
    }



    private void OnTutoriaSelected(object sender, SelectionChangedEventArgs e)
    {
        // Obtener la tutoría seleccionada (nombre del canal)
        var seleccionActual = e.CurrentSelection.FirstOrDefault() as string;

        if (seleccionActual != null)
        {
            // Guardar la tutoría seleccionada
            _tutoriaSeleccionadaAnterior = seleccionActual;

            // Cambiar la visibilidad de los botones de acuerdo a la tutoría seleccionada
            GridBotonesGenerales.IsVisible = false;
            GridBotonesAlumno.IsVisible = true; // Aquí puedes añadir botones relacionados con la tutoría
        }
        else
        {
            // Si no hay selección, volver a mostrar los botones generales
            _tutoriaSeleccionadaAnterior = null;
            GridBotonesGenerales.IsVisible = true;
            GridBotonesAlumno.IsVisible = false;
        }
    }


    // Clase para mapear la respuesta de tutorías
    public class TutoriasResponse
    {
        public string Status { get; set; }
        public List<string> Tutorias { get; set; }
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

    private async void CrearCanalTexto(object sender, EventArgs e)
    {
        if (_alumnoSeleccionadoAnterior != null)
        {
            var alumnoId = _alumnoSeleccionadoAnterior.DiscordID;
            var profesorId = SesionUsuario.Instancia.ProfesorLogueado.DiscordID;
            var instiId = SesionUsuario.Instancia.ProfesorLogueado.InstiID;

            // Crear un objeto con los datos necesarios
            var dataToSend = new
            {
                alumno_id = alumnoId,
                profesor_id = profesorId,
                insti_id = instiId
            };

            var jsonData = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("http://13.38.70.221:5000/crear-canal-texto", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                // Mostrar confirmación
                await DisplayAlert("Canal de Texto", "Canal creado correctamente.", "OK");

                // Limpiar la lista de tutorías actual y actualizarla con los datos del servidor
                TutoriasCollection.ItemsSource = null;  // Limpiar los datos antiguos de la lista

                // Actualizar la lista de tutorías
                ObtenerTutorias();  // Volver a cargar las tutorías actualizadas
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo crear el canal: {error}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Por favor, selecciona un alumno.", "OK");
        }
    }

    private async void asignarDelegado(object sender, EventArgs e)
    {
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        if (profesor == null)
        {
            await DisplayAlert("Error", "No se encontró el profesor logueado.", "OK");
            return;
        }

        // Obtener los alumnos del curso
        var alumnos = _db.ObtenerAlumnosPorInstitutoYCurso(profesor.InstiID, profesor.CursoID)
                         .Select(a => new Alumno
                         {
                             ID = a.ID,
                             Nombre = a.Nombre,
                             DiscordID = a.DiscordID,
                             IsDelegado = a.IsDelegado
                         })
                         .ToList();

        // Lanzar modal con parámetros correctos
        var modal = new AsignarDelegado(alumnos, profesor.InstiID, profesor.CursoID);
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
        var response = await httpClient.PostAsync("http://13.38.70.221:5000/abrir-votacion-delegado", content);

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

    private async void CrearCanalVoz(object sender, EventArgs e)
    {
        if (_alumnoSeleccionadoAnterior != null)
        {
            var alumnoId = _alumnoSeleccionadoAnterior.DiscordID;
            var profesorId = SesionUsuario.Instancia.ProfesorLogueado.DiscordID;
            var instiId = SesionUsuario.Instancia.ProfesorLogueado.InstiID;

            // Crear un objeto con los datos necesarios
            var dataToSend = new
            {
                alumno_id = alumnoId,
                profesor_id = profesorId,
                insti_id = instiId
            };

            var jsonData = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("http://13.38.70.221:5000/crear-canal-tutoria", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                // Mostrar confirmación
                await DisplayAlert("Canal de Voz", "Canal creado correctamente.", "OK");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo crear el canal: {error}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Por favor, selecciona un alumno.", "OK");
        }
    }

    private async void CrearCanalFCT(object sender, EventArgs e)
    {
        if (_alumnoSeleccionadoAnterior != null)
        {
            var alumnoId = _alumnoSeleccionadoAnterior.DiscordID;
            var profesorId = SesionUsuario.Instancia.ProfesorLogueado.DiscordID;
            var instiId = SesionUsuario.Instancia.ProfesorLogueado.InstiID;

            // Crear un objeto con los datos necesarios
            var dataToSend = new
            {
                alumno_id = alumnoId,
                profesor_id = profesorId,
                insti_id = instiId
            };

            var jsonData = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("http://13.38.70.221:5000/crear-canal-fct", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                // Mostrar confirmación
                await DisplayAlert("Canal FCT", "Canal creado correctamente.", "OK");

                // Limpiar la lista de tutorías actual y actualizarla con los datos del servidor
                TutoriasCollection.ItemsSource = null;  // Limpiar los datos antiguos de la lista

                // Actualizar la lista de tutorías
                ObtenerTutorias();  // Volver a cargar las tutorías actualizadas
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo crear el canal: {error}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Por favor, selecciona un alumno.", "OK");
        }
    }

    private async void CrearCanalTFG(object sender, EventArgs e)
    {
        if (_alumnoSeleccionadoAnterior != null)
        {
            var alumnoId = _alumnoSeleccionadoAnterior.DiscordID;
            var profesorId = SesionUsuario.Instancia.ProfesorLogueado.DiscordID;
            var instiId = SesionUsuario.Instancia.ProfesorLogueado.InstiID;

            // Crear un objeto con los datos necesarios
            var dataToSend = new
            {
                alumno_id = alumnoId,
                profesor_id = profesorId,
                insti_id = instiId
            };

            var jsonData = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("http://13.38.70.221:5000/crear-canal-tfg", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                // Mostrar confirmación
                await DisplayAlert("Canal TFG", "Canal creado correctamente.", "OK");

                // Limpiar la lista de tutorías actual y actualizarla con los datos del servidor
                TutoriasCollection.ItemsSource = null;  // Limpiar los datos antiguos de la lista

                // Actualizar la lista de tutorías
                ObtenerTutorias();  // Volver a cargar las tutorías actualizadas
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo crear el canal: {error}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Por favor, selecciona un alumno.", "OK");
        }
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

            var url = "http://13.38.70.221:5000/iniciar_tutoria";  // URL corregida

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

    private async void EliminarTutoria(object sender, EventArgs e)
    {
        if (_tutoriaSeleccionadaAnterior != null)
        {
            var profesorId = SesionUsuario.Instancia.ProfesorLogueado.DiscordID;
            var instiId = SesionUsuario.Instancia.ProfesorLogueado.InstiID;
            var nombreTutoria = _tutoriaSeleccionadaAnterior;  // Aquí usamos la tutoría seleccionada

            // Crear un objeto con los datos necesarios
            var dataToSend = new
            {
                insti_id = instiId,
                profesor_id = profesorId,
                nombre_tutoria = nombreTutoria
            };

            var jsonData = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("http://13.38.70.221:5000/eliminar-tutoria", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                // Mejorar el mensaje de confirmación
                await DisplayAlert("Eliminación de Tutoría",
                    $"La tutoría '{nombreTutoria}' ha sido eliminada correctamente. ¡La lista se actualizará ahora!",
                    "OK");

                // Limpiar la lista de tutorías actual y actualizarla con los datos del servidor
                TutoriasCollection.ItemsSource = null;  // Limpiar los datos antiguos de la lista

                // Actualizar la lista de tutorías
                ObtenerTutorias();  // Volver a cargar las tutorías actualizadas
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo eliminar la tutoría '{nombreTutoria}': {error}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Por favor, selecciona una tutoría para eliminar.", "OK");
        }
    }

    private async void cerrarSesion(object sender, EventArgs e)
    {
        bool confirmar = await Application.Current.MainPage.DisplayAlert(
            "Cerrar sesión", "¿Estás seguro de que quieres cerrar sesión?", "Sí", "Cancelar");

        if (!confirmar)
            return;

        Preferences.Clear();
        SesionUsuario.Instancia.CerrarSesion();

        // Vuelve al Shell con la página de login como inicio
        Application.Current.MainPage = new AppShell();

        // Navega a la página de login (puede ser un route como "LoginPage")
        await Shell.Current.GoToAsync("//Login");
    }
}