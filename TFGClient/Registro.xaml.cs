<<<<<<< HEAD
﻿using System.Net.Mail;
using System.Net;

namespace TFGClient
{
    public partial class Registro : ContentPage
    {
        private string codigoGenerado = string.Empty;

=======
﻿namespace TFGClient
{
    public partial class Registro : ContentPage
    {
>>>>>>> 291f71ef951dacfab9ef5be5f9143f8236aa0381
        public Registro()
        {
            InitializeComponent();
        }

<<<<<<< HEAD
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
=======
        private void OnVerificacionClicked(object sender, EventArgs e)
        {
>>>>>>> 291f71ef951dacfab9ef5be5f9143f8236aa0381
            Formulario.IsVisible = false;
            Verificacion.IsVisible = true;
        }

        private void OnContinuarClicked(object sender, EventArgs e)
        {
<<<<<<< HEAD
            if (CodigoEntry.Text == codigoGenerado)
            {
                Formulario2.IsVisible = true;
                Verificacion.IsVisible = false;
            }
            else
            {
                DisplayAlert("Error","El codigo que has introducido no es correcto","Ok");
            }
            
=======
            Formulario2.IsVisible = true;
            Verificacion.IsVisible = false;
>>>>>>> 291f71ef951dacfab9ef5be5f9143f8236aa0381
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            DisplayAlert("Registro", "Registro completado", "OK");
        }
    }

}
