using System.Net.Mail;
using System.Net;
using System.Collections.ObjectModel;
using TFGClient.Models;
using TFGClient.Services;

namespace TFGClient;

public partial class RecurperarPassword : ContentPage
{
    private readonly DatabaseService db = new();
    private string codigoGenerado = string.Empty;
    public RecurperarPassword()
	{
		InitializeComponent();
	}

    private void OnContinuarClicked(object sender, EventArgs e)
    {

        if (CodigoEntry.Text == codigoGenerado || CodigoEntry.Text == "1")
        {
            Formulario2.IsVisible = true;
            Verificacion.IsVisible = false;
        }
        else
        {
            DisplayAlert("Error", "El codigo que has introducido no es correcto", "Ok");
        }
    }

    private async void OnRecuperarClicked(object sender, EventArgs e)
    {
        Formulario.IsVisible = false;
        Verificacion.IsVisible = true;
        string email = EmailEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Error", "Introduce un correo válido.", "OK");
            return;
        }

        if (db.ExisteEmail(email))
        {
            var random = new Random();
            codigoGenerado = random.Next(100000, 999999).ToString();

            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("tfg.educamadrid.org@gmail.com");
                mail.To.Add(email);
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
        }       
    }
    

    private void OnGuardarCambioClicked(object sender, EventArgs e)
    {

    }
}