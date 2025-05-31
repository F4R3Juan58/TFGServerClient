namespace TFGClient
{
    public partial class BloquearAsignatura : ContentPage
    {

        public BloquearAsignatura()
        {
            InitializeComponent();
        }

        private async void Bloquear_Clicked(object sender, EventArgs e)
        {
            // Aquí va la lógica real para bloquear la asignatura
            await DisplayAlert("Asignatura bloqueada", "La asignatura ha sido bloqueada con éxito.", "OK");
            await Navigation.PopModalAsync();
        }

        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            // Cerrar el popup
            await Navigation.PopModalAsync();
        }
    }
}