using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using TFGClient.Models;
using TFGClient.Services;


namespace TFGClient.Interfaz;

public partial class PaginaProfesor : ContentPage
{
    private string categoriaId;
    private string Asignatura;

    public PaginaProfesor()
    {
        InitializeComponent();
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        if (profesor != null)
        {
            NombreProfesor.Text = $"{profesor.Nombre} {profesor.Apellido}";
        }
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            IsVisible = false
        });
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();  // Aseguramos que la implementación base se ejecute

        // Obtenemos el profesor logueado
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;

        if (profesor != null)
        {
            try
            {
                // Obtenemos las asignaturas desde el servidor
                var asignaturas = await ObtenerAsignaturasDesdeServidor();
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar una alerta
                await DisplayAlert("Error", $"Hubo un problema al obtener las asignaturas: {ex.Message}", "OK");
            }
        }
        else
        {
            // Si no hay profesor logueado, mostrar un mensaje adecuado
            await DisplayAlert("Error", "No se encontró información del profesor. Por favor, asegúrese de estar logueado.", "OK");
        }
    }


    private async Task<List<string>> ObtenerAsignaturasDesdeServidor()
    {
        // Deshabilitar la interactividad del CollectionView mientras cargamos las asignaturas
        AsignaturasCollection.IsEnabled = false;

        var profesor = SesionUsuario.Instancia.ProfesorLogueado;

        var dataToSend = new
        {
            InstiID = profesor.InstiID,
            DiscordID = profesor.DiscordID
        };

        var json = JsonConvert.SerializeObject(dataToSend);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = new HttpClient();
        var response = await client.PostAsync("http://13.38.70.221:5000/obtener-asignaturas-profesor", content);

        if (!response.IsSuccessStatusCode)
        {
            await DisplayAlert("Error", "No se pudieron obtener las asignaturas del servidor.", "OK");
            AsignaturasCollection.IsEnabled = true; // Volver a habilitar el CollectionView
            return new List<string>(); // Retornar una lista vacía
        }

        var body = await response.Content.ReadAsStringAsync();

        try
        {
            // Deserializamos el JSON
            var result = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(body);

            // Verificar si el JSON contiene la clave "actuales"
            if (result != null && result.ContainsKey("actuales"))
            {
                // Mostramos las asignaturas en el CollectionView
                AsignaturasCollection.ItemsSource = result["actuales"];
            }
            else
            {
                // Si "actuales" no está presente o vacío, mostrar un mensaje
                await DisplayAlert("Sin asignaturas", "No se encontraron asignaturas para este profesor.", "OK");
            }
        }
        catch (JsonException ex)
        {
            // Si ocurre un error en la deserialización, mostrar el error
            await DisplayAlert("Error", $"Error al procesar las asignaturas: {ex.Message}", "OK");
        }

        // Habilitamos el CollectionView después de cargar los datos
        AsignaturasCollection.IsEnabled = true;

        // Retornar la lista de asignaturas, en caso de que necesites la lista para otros usos
        return new List<string>(); // Si deseas retornar una lista vacía, puedes ajustar esto según lo necesites.
    }
    private async void OnAsignaturaSelected(object sender, SelectionChangedEventArgs e)
    {
        // Verificamos si hay una asignatura seleccionada
        if (e.CurrentSelection.FirstOrDefault() is string asignatura)
        {
            try
            {
                // Asignamos la asignatura al label para mostrarla
                AsignaturaLabel.Text = asignatura;
                Asignatura = asignatura;

                // Obtenemos los alumnos asociados a la asignatura
                var alumnos = await ObtenerAlumnosDesdeServidor(asignatura);

                // Asignamos la lista de alumnos al CollectionView
                if (alumnos.Count > 0)
                {
                    AlumnosCollection.ItemsSource = alumnos;
                }
                else
                {
                    // Si no se encontraron alumnos, mostrar un mensaje
                    await DisplayAlert("Sin alumnos", "No se encontraron alumnos para esta asignatura.", "OK");
                }

                // Des-seleccionamos visualmente el item seleccionado
                ((CollectionView)sender).SelectedItem = null;
            }
            catch (Exception ex)
            {
                // En caso de error, mostramos un mensaje de alerta
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }
    }



    private async Task<List<string>> ObtenerAlumnosDesdeServidor(string asignatura)
    {
        try
        {
            var profesor = SesionUsuario.Instancia.ProfesorLogueado;

            var dataToSend = new
            {
                InstiID = profesor.InstiID,
                DiscordID = profesor.DiscordID,
                AsignaturaName = asignatura
            };

            var json = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();

            var response = await client.PostAsync("http://13.38.70.221:5000/obtener-alumnos-por-asignatura", content);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "No se pudieron obtener los alumnos del servidor.", "OK");
                return new List<string>();
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseJson))
            {
                await DisplayAlert("Error", "El servidor devolvió una respuesta vacía.", "OK");
                return new List<string>();
            }

            // Deserializar usando la clase AlumnosResponse
            var alumnosResponse = JsonConvert.DeserializeObject<AlumnosResponse>(responseJson);

            if (alumnosResponse == null || alumnosResponse.Alumnos == null || alumnosResponse.Alumnos.Count == 0)
            {
                await DisplayAlert("Sin alumnos", "No se encontraron alumnos para esta asignatura.", "OK");
                return new List<string>();
            }

            return alumnosResponse.Alumnos;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al obtener alumnos: {ex.Message}", "OK");
            return new List<string>();
        }
    }



    private async Task<List<(string Nombre, string DiscordID)>> ObtenerAlumnosConectadosClase()
    {
        using (var client = new HttpClient())
        {
            try
            {
                var url = "http://13.38.70.221:5000/api/alumnos_conectados";

                var data = new
                {
                    profesor_discord_id = SesionUsuario.Instancia.ProfesorLogueado.DiscordID,
                };

                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                    return new List<(string, string)>();

                var json = await response.Content.ReadAsStringAsync();

                // Deserializa como lista de diccionarios (o usa un tipo explícito si prefieres)
                var alumnos = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);

                return alumnos.Select(a =>
                    (a.GetValueOrDefault("nombre") ?? "", a.GetValueOrDefault("discord_id") ?? "")
                ).ToList();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al obtener alumnos conectados: {ex.Message}", "OK");
                return new List<(string, string)>();
            }
        }
    }





    private async void iniciarClase(object sender, EventArgs e)
    {

        bool exito = await CrearCanalVoz(Asignatura);
    }


    private async Task<bool> CrearCanalVoz(string nombreCanal)
    {
        using (var client = new HttpClient())
        {
            try
            {
                var url = "http://13.38.70.221:5000/api/crear_canal_voz";

                var data = new
                {
                    InstiID = SesionUsuario.Instancia.ProfesorLogueado.InstiID,
                    DiscordID = SesionUsuario.Instancia.ProfesorLogueado.DiscordID,
                    nombre_canal = nombreCanal
                };

                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo crear el canal de voz: {ex.Message}", "OK");
                return false;
            }
        }
    }

    private async void AbrirPopupSubirDocumento(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushModalAsync(new SubirArchivoPopup(Asignatura));
    }

    private async void ReestablecerAsignatura(object sender, EventArgs e)
    {
        using (var client = new HttpClient())
        {
            try
            {
                var url = "http://13.38.70.221:5000/api/reestablecer_asignatura";

                var data = new
                {
                    nombre_asignatura = Asignatura
                };

                var content = new StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(data),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    await App.Current.MainPage.DisplayAlert("Éxito", "La asignatura fue reestablecida correctamente.", "OK");
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Error", "No se pudo reestablecer la asignatura.", "OK");
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"Hubo un problema: {ex.Message}", "OK");
            }
        }
    }


    private async void ExpulsarAlumno(object sender, EventArgs e)
    {
        {
            var nombresAlumnos = await ObtenerAlumnosConectadosClase();

            // Convertir List<(string Nombre, string DiscordID)> a List<AlumnoClase>
            var alumnosClase = nombresAlumnos.Select(alumno => new AlumnoClase
            {
                Id = alumno.DiscordID,
                Nombre = alumno.Nombre
            }).ToList();

            var modal = new ExpulsarAlumnoClase(alumnosClase);
            await Navigation.PushModalAsync(modal);
        }

    }

    private async void ExpulsarAlumnoAsignatura(object sender, EventArgs e)
    {
        string categoria_id = categoriaId;
        var nombresAlumnos = await ObtenerAlumnosDesdeServidor(Asignatura);

        // Convertir List<string> a List<Alumno>
        var alumnosClase = nombresAlumnos.Select(nombre => new AlumnoClase
        {
            Id = nombre,
            Nombre = nombre
        }).ToList();

        var modal = new ExpulsarAlumnoAsignatura(alumnosClase);
        await Navigation.PushModalAsync(modal);
    }

    private async void AbrirCuestionario(object sender, EventArgs e)
    {
        var modal = new Cuestionario(Asignatura);
        await Navigation.PushModalAsync(modal);
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

    public class AlumnosResponse
    {
        [JsonProperty("alumnos")]
        public List<string> Alumnos { get; set; }
    }

}
