using System.Net.Mail;
using System.Net;
using System.Collections.ObjectModel;
using TFGClient.Models;
using TFGClient.Services;

namespace TFGClient;

public partial class ModificarDatos : ContentPage
{
    private readonly RellenarPickers rellenar = new();
    private ObservableCollection<string> cursosCompletos = new();
    private readonly DatabaseService db = new();

    public ModificarDatos()
	{
		InitializeComponent();
        InicializarEventos();
        cargarBBDD();
    }

    private void InicializarEventos()
    {
        CAPicker.SelectedIndexChanged += (s, e) => CargarLocalidades();
        LocalidadPicker.SelectedIndexChanged += (s, e) => CargarInstitutos();

        NivelPicker.SelectedIndexChanged += (s, e) => {
            CargarFamilias();
            CargarGrados();
        };

        FamiliaPicker.SelectedIndexChanged += (s, e) => CargarCursos();
        GradoPicker.SelectedIndexChanged += (s, e) => CargarCursos();
    }

    public void cargarBBDD()
    {
        try
        {
            CAPicker.ItemsSource = rellenar.CargarNombreComunidades();
            NivelPicker.ItemsSource = rellenar.CargarNiveles();
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"No se pudieron cargar los datos de base: {ex.Message}", "OK");
        }
    }

    private void CargarLocalidades()
    {
        try
        {
            if (CAPicker.SelectedItem == null) return;

            string nombreCA = (CAPicker.SelectedItem as string)!;
            int comunidadId = ObtenerComunidadId(nombreCA);

            if (comunidadId > 0)
            {
                LocalidadPicker.ItemsSource = rellenar.CargarLocalidades(comunidadId);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"No se pudieron cargar las localidades: {ex.Message}", "OK");
        }
    }

    private void CargarInstitutos()
    {
        try
        {
            if (CAPicker.SelectedItem == null || LocalidadPicker.SelectedItem == null) return;

            string nombreCA = (CAPicker.SelectedItem as string)!;
            string localidad = (LocalidadPicker.SelectedItem as string)!;

            int comunidadId = ObtenerComunidadId(nombreCA);

            if (comunidadId > 0)
            {
                InstitutoPicker.ItemsSource = rellenar.CargarNombreInstitutos(comunidadId, localidad);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"No se pudieron cargar los institutos: {ex.Message}", "OK");
        }
    }

    private void CargarFamilias()
    {
        try
        {
            if (NivelPicker.SelectedItem == null) return;

            string nivel = (NivelPicker.SelectedItem as string)!;
            FamiliaPicker.ItemsSource = rellenar.CargarFamiliasPorNivel(nivel);
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"No se pudieron cargar las familias: {ex.Message}", "OK");
        }
    }

    private void CargarGrados()
    {
        try
        {
            GradoPicker.ItemsSource = rellenar.CargarGrados();
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"No se pudieron cargar los grados: {ex.Message}", "OK");
        }
    }

    private void CargarCursos()
    {
        try
        {
            if (NivelPicker.SelectedItem == null || GradoPicker.SelectedItem == null || FamiliaPicker.SelectedItem == null)
                return;

            string nivel = (NivelPicker.SelectedItem as string)!;
            string grado = (GradoPicker.SelectedItem as string)!;
            string familia = (FamiliaPicker.SelectedItem as string)!;

            CursoPicker.ItemsSource = rellenar.CargarCursos(grado, familia, nivel);
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"No se pudieron cargar los cursos: {ex.Message}", "OK");
        }
    }

    private int ObtenerComunidadId(string nombre)
    {
        return db.ObtenerTodasLasComunidades()
                 .FirstOrDefault(c => c.Nombre == nombre)?.ID ?? 0;
    }

    private int ObtenerInstitutoId(string nombre)
    {
        return db.ObtenerTodosLosInstitutos()
                 .FirstOrDefault(i => i.Nombre == nombre)?.ID ?? 0;
    }

    private int ObtenerCursoId(string nombre)
    {
        return db.ObtenerTodosLosCursos()
                 .FirstOrDefault(c => c.Nombre == nombre)?.ID ?? 0;
    }

    private void onModificarClicked(object sender, EventArgs e)
    {
        Formulario.IsVisible = false;
        Formulario2.IsVisible = true;
    }
}