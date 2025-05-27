namespace TFGClient
{
    public partial class BloquearCanal : ContentPage
    {

        public BloquearCanal()
        {
            InitializeComponent();
        }

        private async void Bloquear_Clicked(object sender, EventArgs e)
        {
            // Aquí va la lógica real para bloquear el canal
            await DisplayAlert("Canal bloqueado", "El canal ha sido bloqueado con éxito.", "OK");
            await Navigation.PopModalAsync();
        }

        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            // Cerrar el popup
            await Navigation.PopModalAsync();
        }
    }
}