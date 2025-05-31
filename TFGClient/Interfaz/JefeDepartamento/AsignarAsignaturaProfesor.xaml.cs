using TFGClient.Models;
namespace TFGClient
{
    public partial class AsignarAsignaturaProfesor : ContentPage
    {
        private List<Profesor> _profesores;
        public AsignarAsignaturaProfesor(List<Profesor> listaProfesores)
        {
            InitializeComponent();

            ProfesorPicker.ItemsSource = listaProfesores;
            ProfesorPicker.ItemDisplayBinding = new Binding("NombreCompleto");
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
