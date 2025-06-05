using Microsoft.Maui.Controls;
using TFGClient.Models;

namespace TFGClient
{
    public partial class ModificarHorario : ContentPage
    {
        public ModificarHorario(List<Profesor> listaProfesores)
        {
            InitializeComponent();

            profesorPicker.ItemsSource = listaProfesores;
            profesorPicker.ItemDisplayBinding = new Binding("NombreCompleto");

            diaPicker.SelectedIndexChanged += DiaPicker_SelectedIndexChanged;
        }

        private void DiaPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (diaPicker.SelectedItem is string seleccion)
            {
                if (seleccion == "Vespertino")
                    horarioPicker.SelectedItem = "15:30 - 21:30";
                else if (seleccion == "Diurno")
                    horarioPicker.SelectedItem = "8:15 - 15:15";
            }
        }

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            if (profesorPicker.SelectedItem is not Profesor profesor ||
                diaPicker.SelectedItem is not string franja ||
                horarioPicker.SelectedItem is not string horario)
            {
                await DisplayAlert("Error", "Completa todos los campos antes de guardar.", "OK");
                return;
            }

            // Aquí podrías guardar en base de datos si es necesario
            await DisplayAlert("Horario modificado", $"El horario de {profesor.NombreCompleto} ha sido actualizado a:\n\nFranja: {franja}\nHorario: {horario}", "OK");
            await Navigation.PopModalAsync();
        }

        private async void Cancelar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}