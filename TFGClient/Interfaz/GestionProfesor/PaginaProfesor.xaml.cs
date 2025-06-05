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
        var response = await client.PostAsync("http://localhost:5000/obtener-asignaturas-profesor", content);

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
            // Obtención de los datos del profesor logueado
            var profesor = SesionUsuario.Instancia.ProfesorLogueado;

            // Crear el objeto para enviar en la solicitud
            var dataToSend = new
            {
                InstiID = profesor.InstiID,
                DiscordID = profesor.DiscordID,
                AsignaturaName = asignatura
            };

            // Serialización del objeto a JSON
            var json = JsonConvert.SerializeObject(dataToSend);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();

            // Realizamos la solicitud POST al servidor para obtener los alumnos por asignatura
            var response = await client.PostAsync("http://localhost:5000/obtener-alumnos-por-asignatura", content);

            // Si la respuesta no es exitosa, mostramos un mensaje y retornamos una lista vacía
            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "No se pudieron obtener los alumnos del servidor.", "OK");
                return new List<string>(); // Retornar lista vacía
            }

            // Leemos el contenido de la respuesta
            var responseJson = await response.Content.ReadAsStringAsync();

            // Verificamos si el JSON está vacío o es inválido
            if (string.IsNullOrEmpty(responseJson))
            {
                await DisplayAlert("Error", "El servidor devolvió una respuesta vacía.", "OK");
                return new List<string>(); // Retornar lista vacía
            }

            // Deserializamos el JSON y retornamos la lista de alumnos
            var alumnos = JsonConvert.DeserializeObject<List<string>>(responseJson);

            // Si no hay alumnos, mostramos un mensaje y retornamos una lista vacía
            if (alumnos == null || alumnos.Count == 0)
            {
                await DisplayAlert("Sin alumnos", "No se encontraron alumnos para esta asignatura.", "OK");
                return new List<string>(); // Retornar lista vacía
            }

            // Retornamos la lista de alumnos obtenida
            return alumnos;
        }
        catch (Exception ex)
        {
            // En caso de error, mostramos el mensaje de error
            await DisplayAlert("Error", $"Error al obtener alumnos: {ex.Message}", "OK");
            return new List<string>(); // Retornar lista vacía en caso de error
        }
    }



    private async Task<List<string>> ObtenerAlumnosConectadosClase(string categoriaId)
    {
        using (var client = new HttpClient())
        {
            try
            {
                var url = $"http://127.0.0.1:5000/api/alumnos_conectados?categoria_id={Uri.EscapeDataString(categoriaId)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al obtener alumnos conectados: {ex.Message}", "OK");
                return new List<string>();
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
                var url = "http://localhost:5000/api/crear_canal_voz";

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
                var url = "http://127.0.0.1:5000/api/reestablecer_asignatura";

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
        string categoria_id = categoriaId;
        var nombresAlumnos = await ObtenerAlumnosConectadosClase(categoria_id);

        // Convertir List<string> a List<Alumno>
        var alumnosClase = nombresAlumnos.Select(nombre => new AlumnoClase
        {
            Id = nombre,
            Nombre = nombre
        }).ToList();

        var modal = new ExpulsarAlumnoClase(categoria_id, alumnosClase);
        await Navigation.PushModalAsync(modal);
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
        var modal = new Cuestionario();
        await Navigation.PushModalAsync(modal);
    }
}
