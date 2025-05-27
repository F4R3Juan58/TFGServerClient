namespace TFGClient
{
    public partial class SubirArchivoPopup : ContentPage
    {
        private FileResult archivoSeleccionado;

        public SubirArchivoPopup()
        {
            InitializeComponent();
        }

        private async void SeleccionarArchivo_Clicked(object sender, EventArgs e)
        {
            archivoSeleccionado = await FilePicker.PickAsync();

            if (archivoSeleccionado != null)
            {
                NombreArchivoLabel.Text = $"Archivo: {archivoSeleccionado.FileName}";
                SubirButton.IsEnabled = true;

                // Oculta la imagen y muestra el recuadro con el nombre
                BotonSeleccionArchivo.IsVisible = false;
                ArchivoSeleccionadoFrame.IsVisible = true;
            }
        }

        private async void SubirArchivo_Clicked(object sender, EventArgs e)
        {
            if (archivoSeleccionado == null)
                return;

            Cargando.IsRunning = true;
            Cargando.IsVisible = true;
            SubirButton.IsEnabled = false;

            try
            {
                using var stream = await archivoSeleccionado.OpenReadAsync();
                using var content = new MultipartFormDataContent();
                content.Add(new StreamContent(stream), "archivo", archivoSeleccionado.FileName);

                using var client = new HttpClient();
                var response = await client.PostAsync("http://127.0.0.1:5000/api/subir_archivo", content);

                if (response.IsSuccessStatusCode)
                    await DisplayAlert("Éxito", "Archivo subido correctamente", "OK");
                else
                    await DisplayAlert("Error", "Falló la subida", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
            finally
            {
                Cargando.IsRunning = false;
                Cargando.IsVisible = false;
                await Navigation.PopModalAsync(); // Cierra la página modal
            }
        }

        private async void Cerrar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
