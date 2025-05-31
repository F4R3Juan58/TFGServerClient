using TFGClient.Models;

namespace TFGClient
{
    public partial class NuevoTutor : ContentPage
    {
        private List<Profesor> _profesores;
        public NuevoTutor(List<Profesor> listaProfesores)
        {
            InitializeComponent();

            profesorPicker.ItemsSource = listaProfesores;
            profesorPicker.ItemDisplayBinding = new Binding("NombreCompleto");
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
