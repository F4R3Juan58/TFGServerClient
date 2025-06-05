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

    private Alumno? alumnoActual;
    private Profesor? profesorActual;

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

    private async void onModificarClicked(object sender, EventArgs e)
    {
        try
        {
            string emailOriginal = EmailEntry.Text?.Trim() ?? "";
            string password = ContrasenaEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(emailOriginal) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Debes introducir tu email y password actual.", "OK");
                return;
            }

            string contrasenaHash = HashearContrasena(password);

            alumnoActual = db.ObtenerAlumnoPorEmailYPassword(emailOriginal, contrasenaHash);
            profesorActual = db.ObtenerProfesorPorEmailYPassword(emailOriginal, contrasenaHash);

            if (alumnoActual != null)
            {
                // Rellenar los datos del alumno y mostrar el formulario correspondiente
                RellenarFormularioAlumno(alumnoActual);
                Formulario.IsVisible = false;
                FormularioAlumno.IsVisible = true; // Mostrar formulario alumno
            }
            else if (profesorActual != null)
            {
                // Rellenar los datos del profesor y mostrar el formulario correspondiente
                RellenarFormularioProfesor(profesorActual);
                Formulario.IsVisible = false;
                FormularioProfesor.IsVisible = true; // Mostrar formulario profesor
            }
            else
            {
                await DisplayAlert("Error", "Usuario no encontrado o credenciales incorrectas.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrio un error: {ex.Message}", "OK");
        }
    }

    private async void OnGuardarModificacionClicked(object sender, EventArgs e)
    {
        try
        {
            // Guardar los datos de Alumno
            if (alumnoActual != null)
            {
                var alumnoActualizado = new Alumno
                {
                    Nombre = nombreEntry.Text?.Trim(),
                    Apellido = ApellidosEntry.Text?.Trim(),
                    Email = ModificarEmailEntry.Text?.Trim(),
                    ComunidadID = ObtenerComunidadId((CAPicker.SelectedItem as string)!),
                    InstiID = ObtenerInstitutoId((InstitutoPicker.SelectedItem as string)!),
                    CursoID = ObtenerCursoId((CursoPicker.SelectedItem as string)!),
                    RolID = alumnoActual.RolID,
                    DiscordID = alumnoActual.DiscordID,
                    IsDelegado = alumnoActual.IsDelegado,
                    Puntos = alumnoActual.Puntos,
                    Password = alumnoActual.Password
                };

                if (db.ActualizarAlumno(alumnoActual.ID, alumnoActualizado))
                    await DisplayAlert("Éxito", "Datos del alumno modificados correctamente.", "OK");
                else
                    await DisplayAlert("Error", "No se pudieron modificar los datos del alumno.", "OK");
            }
            // Guardar los datos de Profesor
            else if (profesorActual != null)
            {
                var profesorActualizado = new Profesor
                {
                    Nombre = nombreEntryProfesor.Text?.Trim(),
                    Apellido = ApellidosEntryProfesor.Text?.Trim(),
                    Email = ModificarEmailEntryProfesor.Text?.Trim(),
                    ComunidadID = ObtenerComunidadId((CAPickerProfesor.SelectedItem as string)!),
                    InstiID = ObtenerInstitutoId((InstitutoPickerProfesor.SelectedItem as string)!),
                    RolID = profesorActual.RolID,
                    DiscordID = profesorActual.DiscordID,
                    IsJefe = profesorActual.IsJefe,
                    IsTutor = profesorActual.IsTutor,
                    Password = profesorActual.Password
                };

                if (db.ActualizarProfesor(profesorActual.ID, profesorActualizado))
                    await DisplayAlert("Éxito", "Datos del profesor modificados correctamente.", "OK");
                else
                    await DisplayAlert("Error", "No se pudieron modificar los datos del profesor.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Usuario no encontrado o credenciales incorrectas.", "OK");
            }

            // Redirigir a la pantalla de login
            await Navigation.PushAsync(new Login());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrio un error: {ex.Message}", "OK");
        }
    }

    private void RellenarFormularioAlumno(Alumno alumno)
    {
        nombreEntry.Text = alumno.Nombre;
        ApellidosEntry.Text = alumno.Apellido;
        ModificarEmailEntry.Text = alumno.Email;

        // Cargar Comunidad
        var comunidad = db.ObtenerTodasLasComunidades().FirstOrDefault(c => c.ID == alumno.ComunidadID);
        if (comunidad != null) CAPicker.SelectedItem = comunidad.Nombre;

        // Cargar Localidad e Instituto
        var instituto = db.ObtenerTodosLosInstitutos().FirstOrDefault(i => i.ID == alumno.InstiID);
        if (instituto != null)
        {
            LocalidadPicker.ItemsSource = rellenar.CargarLocalidades(alumno.ComunidadID);
            LocalidadPicker.SelectedItem = instituto.Localidad;

            InstitutoPicker.ItemsSource = rellenar.CargarNombreInstitutos(alumno.ComunidadID, instituto.Localidad);
            InstitutoPicker.SelectedItem = instituto.Nombre;
        }

        // Cargar Nivel, Familia, Grado y Curso
        var curso = db.ObtenerTodosLosCursos().FirstOrDefault(c => c.ID == alumno.CursoID);
        if (curso != null)
        {
            NivelPicker.SelectedItem = curso.Nivel;
            FamiliaPicker.ItemsSource = rellenar.CargarFamiliasPorNivel(curso.Nivel);
            FamiliaPicker.SelectedItem = curso.FamiliaProfesional;

            GradoPicker.ItemsSource = rellenar.CargarGrados();
            GradoPicker.SelectedItem = curso.Grado;

            CursoPicker.ItemsSource = rellenar.CargarCursos(curso.Grado, curso.FamiliaProfesional, curso.Nivel);
            CursoPicker.SelectedItem = curso.Nombre;
        }
    }

    private void RellenarFormularioProfesor(Profesor profesor)
    {
        nombreEntryProfesor.Text = profesor.Nombre;
        ApellidosEntryProfesor.Text = profesor.Apellido;
        ModificarEmailEntryProfesor.Text = profesor.Email;

        // Cargar Comunidad
        var comunidad = db.ObtenerTodasLasComunidades().FirstOrDefault(c => c.ID == profesor.ComunidadID);
        if (comunidad != null)
        {
            CAPickerProfesor.ItemsSource = rellenar.CargarNombreComunidades();
            CAPickerProfesor.SelectedItem = comunidad.Nombre;
        }
        // Cargar Localidad e Instituto
        var instituto = db.ObtenerTodosLosInstitutos().FirstOrDefault(i => i.ID == profesor.InstiID);
        if (instituto != null)
        {
            LocalidadPickerProfesor.ItemsSource = rellenar.CargarLocalidades(profesor.ComunidadID);
            LocalidadPickerProfesor.SelectedItem = instituto.Localidad;

            InstitutoPickerProfesor.ItemsSource = rellenar.CargarNombreInstitutos(profesor.ComunidadID, instituto.Localidad);
            InstitutoPickerProfesor.SelectedItem = instituto.Nombre;
        }
    }

    private string HashearContrasena(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}