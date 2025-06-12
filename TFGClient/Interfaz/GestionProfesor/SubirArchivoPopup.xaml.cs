using Newtonsoft.Json;
using System.Text;

namespace TFGClient
{
    public partial class SubirArchivoPopup : ContentPage
    {
        private FileResult archivoSeleccionado;
        private string asignatura;

        public SubirArchivoPopup(string Asignatura)
        {
            InitializeComponent();
            asignatura = Asignatura;
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
            {
                await DisplayAlert("Error", "No se ha seleccionado ningún archivo.", "OK");
                return;
            }

            Cargando.IsRunning = true;
            Cargando.IsVisible = true;
            SubirButton.IsEnabled = false;

            try
            {
                // Abrimos el archivo seleccionado
                using var stream = await archivoSeleccionado.OpenReadAsync();
                // Creamos el contenido para la solicitud (archivo)
                using var content = new MultipartFormDataContent();

                // También agregamos otros datos, como InstiID y DiscordID
                var profesor = SesionUsuario.Instancia.ProfesorLogueado;
                content.Add(new StreamContent(stream), "archivo", archivoSeleccionado.FileName);
                content.Add(new StringContent(profesor.InstiID.ToString()), "InstiID");
                content.Add(new StringContent(profesor.DiscordID.ToString()), "DiscordID");
                content.Add(new StringContent(asignatura), "nombre_asignatura");


                using var client = new HttpClient();
                Console.WriteLine("[DEBUG] Enviando solicitud POST al servidor...");
                var response = await client.PostAsync("http://13.38.70.221:5000/api/subir_archivo", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Archivo subido correctamente", "OK");
                }
                else
                {
                    await DisplayAlert("Error", $"Falló la subida: {response.ReasonPhrase}", "OK");
                    Console.WriteLine("[DEBUG] Respuesta no exitosa del servidor: " + response.ReasonPhrase);
                }
            }
            catch (HttpRequestException httpEx)
            {
                await DisplayAlert("Error", $"Error de solicitud HTTP: {httpEx.Message}", "OK");
                Console.WriteLine("[DEBUG] Error de solicitud HTTP: " + httpEx.Message);
            }
            catch (TaskCanceledException taskEx)
            {
                await DisplayAlert("Error", "La solicitud ha expirado. Por favor, intenta de nuevo.", "OK");
                Console.WriteLine("[DEBUG] Error de tiempo de espera de la solicitud: " + taskEx.Message);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
                Console.WriteLine("[DEBUG] Error general: " + ex.Message);
            }
            finally
            {
                // Restablecemos la UI después de completar la operación
                Cargando.IsRunning = false;
                Cargando.IsVisible = false;
                SubirButton.IsEnabled = true;
                await Navigation.PopModalAsync(); // Cierra la página modal
                Console.WriteLine("[DEBUG] Finalizada la operación de subida del archivo.");
            }
        }


        private async void Cerrar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
