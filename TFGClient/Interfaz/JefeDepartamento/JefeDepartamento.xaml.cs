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

    public ObservableCollection<Profesor> Profesores { get; set; } = new ObservableCollection<Profesor>();
    public ObservableCollection<Profesor> Tutores { get; set; } = new ObservableCollection<Profesor>();
    public ObservableCollection<Profesor> ProfesoresNoTutores { get; set; } = new ObservableCollection<Profesor>();

    public JefeDepartamento()
    {
        InitializeComponent();
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        if (profesor != null)
        {
            NombreProfesor.Text = $"{profesor.Nombre} {profesor.Apellido}";
        }

        BindingContext = this;
        CargarProfesoresYTutoresAsync();

        MessagingCenter.Subscribe<NuevoTutor>(this, "TutorAsignado", (sender) =>
        {
            CargarProfesoresYTutoresAsync();
        });
    }

    private async void CargarProfesoresYTutoresAsync()
    {
        try
        {
            string emailUsuario = Preferences.Get("UsuarioEmail", string.Empty);
            if (string.IsNullOrEmpty(emailUsuario))
            {
                await DisplayAlert("Error", "No se encontró el email del usuario conectado.", "OK");
                return;
            }

            Profesor? profesorConectado = _databaseService.ObtenerProfesorPorEmail(emailUsuario);
            if (profesorConectado == null)
            {
                await DisplayAlert("Error", "No se encontró el usuario conectado en la base de datos.", "OK");
                return;
            }

            int instiID = profesorConectado.InstiID;
            var todosProfesores = _databaseService.ObtenerTodosLosProfesores();
            var profesoresFiltrados = todosProfesores.Where(p => p.InstiID == instiID);

            var listaTutores = profesoresFiltrados.Where(p => p.IsTutor).ToList();
            var listaNoTutores = profesoresFiltrados.Where(p => !p.IsTutor).ToList();
            var listaTodos = profesoresFiltrados.ToList();

            Tutores.Clear();
            foreach (var tutor in listaTutores)
                Tutores.Add(tutor);

            Profesores.Clear();
            foreach (var profe in listaTodos)
                Profesores.Add(profe);

            ProfesoresNoTutores.Clear();
            foreach (var profe in listaNoTutores)
                ProfesoresNoTutores.Add(profe);
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

        var profesoresFiltrados = todosProfesores
            .Where(p => p.InstiID == profesor.InstiID)
            .ToList();

        var listaProfesores = profesoresFiltrados.Where(p => !p.IsTutor).ToList();
        int instiId = profesor.InstiID;

        await Application.Current.MainPage.Navigation.PushModalAsync(
            new NuevoTutor(listaProfesores, instiId)
        );
    }

    private async void AsignarAsignaturaProfesor(object sender, EventArgs e)
    {
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        var todosProfesores = _databaseService.ObtenerTodosLosProfesores();

        var profesoresFiltrados = todosProfesores
            .Where(p => p.InstiID == profesor.InstiID)
            .ToList();

        await Application.Current.MainPage.Navigation.PushModalAsync(new AsignarAsignaturaProfesor(profesoresFiltrados));
    }

    private async void Editar(object sender, EventArgs e)
    {
        var profesorSeleccionado = ListaProfesoresCollectionView.SelectedItem as Profesor;

        if (profesorSeleccionado == null)
        {
            await DisplayAlert("Aviso", "Por favor selecciona un profesor para editar.", "OK");
            return;
        }

        await Application.Current.MainPage.Navigation.PushModalAsync(new Editar(profesorSeleccionado));
    }

    private async void ModificarHorario(object sender, EventArgs e)
    {
        var profesor = SesionUsuario.Instancia.ProfesorLogueado;
        var todosProfesores = _databaseService.ObtenerTodosLosProfesores();

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