namespace TFGClient;

public partial class Login : ContentPage
{
	public Login()
	{
		InitializeComponent();
	}

    private async void onLoginClicked(object sender, EventArgs e)
    {
        
        bool isProferor = false;
        if (isProferor == false)
        {
            var uri = new Uri("https://discord.gg/f3YA784A");
            await Launcher.Default.OpenAsync(uri);
        }
        else
        {
            await Navigation.PushAsync(new ProfesorHome());
        }
    }

    private async void onRegistroClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Registro());
    }
}