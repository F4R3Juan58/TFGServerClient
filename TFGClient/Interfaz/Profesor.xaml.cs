using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using TFGClient.Models;
using TFGClient.Services;


namespace TFGClient.Interfaz;

public partial class Profesor : ContentPage
{
    private string categoriaId;
    private string Asignatura;

    public Profesor()
	{
		InitializeComponent();
	}

    private async void OnAppearing(object sender, EventArgs e)
    {
        var asignaturas = await ObtenerAsignaturasDesdeServidor();
        AsignaturasCollection.ItemsSource = asignaturas;

        string nombreAsignatura = "matemáticas"; // reemplaza dinámicamente si es necesario
        categoriaId = await ObtenerCategoriaIdDesdeServidor(nombreAsignatura);

        if (!string.IsNullOrEmpty(categoriaId))
        {
            Console.WriteLine($"ID de la categoría: {categoriaId}");
            // aquí podrías habilitar botones o guardar ese ID para próximas acciones
        }
        else
        {
            await DisplayAlert("Error", "No se encontró la categoría", "OK");
        }
    }

    private async Task<List<string>> ObtenerAsignaturasDesdeServidor()
    {
        using (var client = new HttpClient())
        {
            try
            {
                var url = "http://TU_IP_LOCAL:5000/api/roles_usuario"; // Cambia a tu IP real
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al obtener asignaturas: {ex.Message}", "OK");
                return new List<string>();
            }
        }
    }

    private async void OnAsignaturaSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string asignatura)
        {
            AsignaturaLabel.Text = asignatura;
            Asignatura = asignatura;
            string categoriaId = await ObtenerCategoriaIdDesdeServidor(asignatura);
            var alumnos = await ObtenerAlumnosDesdeServidor(asignatura);
            AlumnosCollection.ItemsSource = alumnos;

            if (!string.IsNullOrEmpty(categoriaId))
            {
                Console.WriteLine($"Asignatura seleccionada: {asignatura} - Categoría ID: {categoriaId}");

            }
            else
            {
                await DisplayAlert("Error", $"No se encontró la categoría para {asignatura}", "OK");
            }

            // Des-seleccionar visualmente
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async Task<List<string>> ObtenerAlumnosDesdeServidor(string asignatura)
    {
        using (var client = new HttpClient())
        {
            try
            {
                var url = $"http://TU_IP_LOCAL:5000/api/alumnos_por_asignatura?asignatura={Uri.EscapeDataString(asignatura)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al obtener alumnos: {ex.Message}", "OK");
                return new List<string>();
            }
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



    private async Task<string> ObtenerCategoriaIdDesdeServidor(string nombreAsignatura)
    {
        using (var client = new HttpClient())
        {
            try
            {
                var url = $"http://TU_IP_LOCAL:5000/api/categoria_id?nombre={Uri.EscapeDataString(nombreAsignatura)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return string.Empty;

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return data != null && data.ContainsKey("id") ? data["id"] : string.Empty;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al obtener categoría: {ex.Message}", "OK");
                return string.Empty;
            }
        }
    }


    private async void iniciarClase(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(categoriaId))
        {
            await DisplayAlert("Error", "No se ha seleccionado una asignatura válida", "OK");
            return;
        }

        bool exito = await CrearCanalVoz(categoriaId, "Clase");

        if (exito)
            await DisplayAlert("Éxito", "Canal de voz 'Clase' creado correctamente", "OK");
        else
            await DisplayAlert("Error", "No se pudo crear el canal de voz", "OK");
    }


    private async Task<bool> CrearCanalVoz(string categoriaId, string nombreCanal)
    {
        using (var client = new HttpClient())
        {
            try
            {
                var url = "http://TU_IP_LOCAL:5000/api/crear_canal_voz";

                var data = new
                {
                    categoria_id = categoriaId,
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
        await Application.Current.MainPage.Navigation.PushModalAsync(new SubirArchivoPopup());
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
                    Categoria_id = categoriaId
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
