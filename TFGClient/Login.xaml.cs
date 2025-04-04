namespace TFGClient;

public partial class Login : ContentPage
{
	public Login()
	{
		InitializeComponent();
	}

    private async void onLoginClicked(object sender, EventArgs e)
    {
        
        bool isProferor = true;
        if (isProferor == false)
        {
            var uri = new Uri("https://discord.gg/f3YA784A"); //AÑADIR LOGICA PARA QUE LA INVITACION LA PILLE DEL BOT DE PYTHON
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