using Microsoft.Maui.Controls;

namespace TFGClient
{
    public partial class ModificarHorario : ContentPage
    {

        public ModificarHorario()
        {
            InitializeComponent();
        }

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            // Lógica para guardar los datos
            await DisplayAlert("Horario modificado", "El horario ha sido modificado con éxito.", "OK");
            await Navigation.PopModalAsync();
        }

        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}