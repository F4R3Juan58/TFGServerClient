namespace TFGClient
{
    public partial class Editar : ContentPage
    {
        string nombreProfesor;

        public Editar(string Nombre)
        {
            InitializeComponent();
            NombreProfesor.Text = Nombre;
        }

        private async void Cerrar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void Confirmar_Clicked(object sender, EventArgs e)
        {
            DisplayAlert("ACCIÓN REALIZADA CON ÉXITO", "", "OK");
        }
    }
}
