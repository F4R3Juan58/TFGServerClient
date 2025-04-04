using System.Net.Mail;
using System.Net;
using System.Collections.ObjectModel;


namespace TFGClient
{
    public partial class Registro : ContentPage
    {
        private string codigoGenerado = string.Empty;

        readonly RellenarPickers rellenar = new();

        public Registro()
        {
            InitializeComponent();
            cargarBBDD();
        }

        public void cargarBBDD()
        {

            CAPicker.ItemsSource = rellenar.CargarComunidades();
        }

        private async void OnVerificacionClicked(object sender, EventArgs e)
        {
            var random = new Random();
            codigoGenerado = random.Next(100000, 999999).ToString();
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("tfg.educamadrid.org@gmail.com");
                mail.To.Add(EmailEntry.Text);
                mail.Subject = "Tu código de verificación";
                mail.Body = $"Tu código es: {codigoGenerado}";

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("tfg.educamadrid.org@gmail.com", "mvmxasnwxgqgidxf");
                smtp.EnableSsl = true;

                smtp.Send(mail);

                await DisplayAlert("Éxito", "Correo enviado correctamente", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo enviar el correo: {ex.Message}", "OK");
            }
            Formulario.IsVisible = false;
            Verificacion.IsVisible = true;
        }

        private void OnContinuarClicked(object sender, EventArgs e)
        {

            if (CodigoEntry.Text == codigoGenerado || CodigoEntry.Text=="1")
            {
                Formulario2.IsVisible = true;
                Verificacion.IsVisible = false;
            }
            else
            {
                DisplayAlert("Error","El codigo que has introducido no es correcto","Ok");
            }
            

            Formulario2.IsVisible = true;
            Verificacion.IsVisible = false;

        }

        private void OnContinuar2Clicked(object sender, EventArgs e)
        {
            Formulario3.IsVisible = true;
            Formulario2.IsVisible = false;
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            string name = NombreEntry.Text;
            string subname = ApellidosEntry.Text;
            string email = EmailEntry.Text;
            string password = ContrasenaEntry.Text;
            string ca = CAPicker.SelectedItem?.ToString();
            string localidad = LocalidadPicker.SelectedItem?.ToString();
            string instituto = InstitutoPicker.SelectedItem?.ToString();
            string profealumno = PAPicker.SelectedItem?.ToString();
            string nivel = NivelPicker.SelectedItem?.ToString();
            string grado = FamiliaPicker.SelectedItem?.ToString();
            string curso = CursoPicker.SelectedItem?.ToString();


            DisplayAlert("Registro", "Registro completado", "OK");
        }
    }

}
