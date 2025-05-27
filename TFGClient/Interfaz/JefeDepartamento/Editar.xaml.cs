namespace TFGClient
{
    public partial class Editar : ContentPage
    {

        public Editar()
        {
            InitializeComponent();
        }

       

        private async void Cerrar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void Confirmar_Clicked(object sender, EventArgs e)
        {
            DisplayAlert("ACCIÓN REALIZADA CON ÉXITO","","OK");
        }
    }
}
