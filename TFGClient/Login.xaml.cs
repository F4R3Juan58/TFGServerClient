namespace TFGClient;

public partial class Login : ContentPage
{
	public Login()
	{
		InitializeComponent();
	}

    private async void onLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfesorHome());
    }

    private async void onRegistroClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Registro());
    }
}