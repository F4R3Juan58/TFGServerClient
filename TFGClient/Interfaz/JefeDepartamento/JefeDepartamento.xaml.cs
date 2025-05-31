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

        // Asignar BindingContext para el enlace de datos
        BindingContext = this;

        // Cargar profesores y tutores al iniciar la página
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
                await DisplayAlert("Error", "No se encontró el email del usuario conectado.", "OK");
                return;
            }

            // Obtener el profesor conectado para saber su InstiID
            Profesor? profesorConectado = _databaseService.ObtenerProfesorPorEmail(emailUsuario);
            if (profesorConectado == null)
            {
                await DisplayAlert("Error", "No se encontró el usuario conectado en la base de datos.", "OK");
                return;
            }
            if (profesorConectado == null)
            {
                await DisplayAlert("Error", "No se encontró el usuario conectado en la base de datos.", "OK");
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

            // Limpiar colecciones y añadir los datos filtrados
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
        await Application.Current.MainPage.Navigation.PushModalAsync(new NuevoTutor());
    }

    private async void AsignarAsignaturaProfesor(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushModalAsync(new AsignarAsignaturaProfesor());
    }

    private async void Editar(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushModalAsync(new Editar());
    }

    private async void ModificarHorario(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PushModalAsync(new ModificarHorario());
    }

    private async void BloquearAsignatura(object sender, EventArgs e)
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

    private void Button_Clicked(object sender, EventArgs e)
    {
        // Puedes implementar la lógica del botón aquí si hace falta
    }
}
