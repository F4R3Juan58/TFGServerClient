namespace TFGClient
{
    public partial class Registro : ContentPage
    {
        public Registro()
        {
            InitializeComponent();
        }

        private void OnVerificacionClicked(object sender, EventArgs e)
        {
            Formulario.IsVisible = false;
            Verificacion.IsVisible = true;
        }

        private void OnContinuarClicked(object sender, EventArgs e)
        {
            Formulario2.IsVisible = true;
            Verificacion.IsVisible = false;
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            DisplayAlert("Registro", "Registro completado", "OK");
        }
    }

}
