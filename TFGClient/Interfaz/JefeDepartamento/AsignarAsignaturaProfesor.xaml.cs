namespace TFGClient
{
    public partial class AsignarAsignaturaProfesor : ContentPage
    {

        public AsignarAsignaturaProfesor()
        {
            InitializeComponent();
        }

       

        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void Guardar_Clicked(object sender, EventArgs e)
        {
            DisplayAlert("ACCIÓN REALIZADA CON ÉXITO", "", "OK");
        }

    }
}
