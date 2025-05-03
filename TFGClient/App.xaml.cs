namespace TFGClient
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            // Espera a que la Shell esté lista para navegar
            Shell.Current.Dispatcher.Dispatch(async () =>
            {
                await Shell.Current.GoToAsync("Login");
            });
        }
    }

}
