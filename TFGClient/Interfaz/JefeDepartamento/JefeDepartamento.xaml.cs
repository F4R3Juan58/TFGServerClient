using System;
using System.Collections.ObjectModel;
using System.Linq;
using TFGClient.Models;
using TFGClient.Services;
using Microsoft.Maui.Storage;

namespace TFGClient.Interfaz;

public partial class JefeDepartamento : ContentPage
{
    private readonly DatabaseService _databaseService = new DatabaseService();

    // Colecciones enlazadas a la UI
    public ObservableCollection<Profesor> Profesores { get; set; } = new ObservableCollection<Profesor>();
    public ObservableCollection<Profesor> Tutores { get; set; } = new ObservableCollection<Profesor>();

    public JefeDepartamento()
    {
        InitializeComponent();
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        if (profesor != null)
        {
            NombreProfesor.Text = $"{profesor.Nombre} {profesor.Apellido}";
        }

        // Asignar BindingContext para el enlace de datos
        BindingContext = this;

        // Cargar profesores y tutores al iniciar la p�gina
        CargarProfesoresYTutoresAsync();
    }

    private async void CargarProfesoresYTutoresAsync()
    {
        try
        {
            // Obtener email del usuario conectado desde Preferences
            string emailUsuario = Preferences.Get("UsuarioEmail", string.Empty);
            if (string.IsNullOrEmpty(emailUsuario))
            {
                await DisplayAlert("Error", "No se encontr� el email del usuario conectado.", "OK");
                return;
            }

            // Obtener el profesor conectado para saber su InstiID
            Profesor? profesorConectado = _databaseService.ObtenerProfesorPorEmail(emailUsuario);
            if (profesorConectado == null)
            {
                await DisplayAlert("Error", "No se encontr� el usuario conectado en la base de datos.", "OK");
                return;
            }
            if (profesorConectado == null)
            {
                await DisplayAlert("Error", "No se encontr� el usuario conectado en la base de datos.", "OK");
                return;
            }

            int instiID = profesorConectado.InstiID;

            // Obtener todos los profesores
            var todosProfesores = _databaseService.ObtenerTodosLosProfesores();

            // Filtrar por InstiID del usuario conectado
            var profesoresFiltrados = todosProfesores.Where(p => p.InstiID == instiID);

            // Separar en tutores y profesores normales
            var listaTutores = profesoresFiltrados.Where(p => p.IsTutor).ToList();
            var listaProfesores = profesoresFiltrados.Where(p => !p.IsTutor).ToList();

            // Limpiar colecciones y a�adir los datos filtrados
            Tutores.Clear();
            foreach (var tutor in listaTutores)
                Tutores.Add(tutor);

            Profesores.Clear();
            foreach (var profe in listaProfesores)
                Profesores.Add(profe);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error cargando profesores y tutores: {ex.Message}", "OK");
        }
    }

    private async void NuevoTutor(object sender, EventArgs e)
    {
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        var todosProfesores = _databaseService.ObtenerTodosLosProfesores();

        // Filtrar y convertir a List
        var profesoresFiltrados = todosProfesores
            .Where(p => p.InstiID == profesor.InstiID)
            .ToList();

        var listaProfesores = profesoresFiltrados.Where(p => !p.IsTutor).ToList();

        await Application.Current.MainPage.Navigation.PushModalAsync(new NuevoTutor(listaProfesores));
    }

    private async void AsignarAsignaturaProfesor(object sender, EventArgs e)
    {
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        var todosProfesores = _databaseService.ObtenerTodosLosProfesores();

        // Filtrar y convertir a List
        var profesoresFiltrados = todosProfesores
            .Where(p => p.InstiID == profesor.InstiID)
            .ToList();

        await Application.Current.MainPage.Navigation.PushModalAsync(new AsignarAsignaturaProfesor(profesoresFiltrados));

    }

    private async void Editar(object sender, EventArgs e)
    {
        // Verifica si hay algún elemento seleccionado
        var profesorSeleccionado = ListaProfesoresCollectionView.SelectedItem as Profesor;

        if (profesorSeleccionado == null)
        {
            // Muestra el mensaje de alerta si no hay seleccionado
            await DisplayAlert("Aviso", "Por favor selecciona un profesor para editar.", "OK");
            return;
        }

        // Obtén el nombre del profesor
        string nombreProfesor = profesorSeleccionado.NombreCompleto;

        // Abre la página de edición y pásale el nombre como parámetro
        await Application.Current.MainPage.Navigation.PushModalAsync(new Editar(nombreProfesor));
    }



    private async void ModificarHorario(object sender, EventArgs e)
    {
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        var todosProfesores = _databaseService.ObtenerTodosLosProfesores();

        // Filtrar y convertir a List
        var profesoresFiltrados = todosProfesores
            .Where(p => p.InstiID == profesor.InstiID)
            .ToList();
        await Application.Current.MainPage.Navigation.PushModalAsync(new ModificarHorario(profesoresFiltrados));
    }

    private async void EliminarAsignatura(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushModalAsync(new BloquearAsignatura());
    }

    private async void BloquearCanal(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushModalAsync(new BloquearCanal());
    }

    private void MostrarGestionAcademica(object sender, EventArgs e)
    {
        GestionAcademica.IsVisible = true;
        AdministrarProfesores.IsVisible = false;
    }

    private void VolverGestionAcademica(object sender, EventArgs e)
    {
        GestionAcademica.IsVisible = true;
        AdministrarProfesores.IsVisible = false;
    }

    private void MostrarAdministrarProfesores(object sender, EventArgs e)
    {
        GestionAcademica.IsVisible = false;
        AdministrarProfesores.IsVisible = true;
    }
}
