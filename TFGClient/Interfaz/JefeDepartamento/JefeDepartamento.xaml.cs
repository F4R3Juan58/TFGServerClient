namespace TFGClient.Interfaz;

public partial class JefeDepartamento : ContentPage
{
	public JefeDepartamento()
	{
		InitializeComponent();
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

    }

}