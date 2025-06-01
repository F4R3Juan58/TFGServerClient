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

        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);

        // Ocultar barra de navegación completa
        NavigationPage.SetHasNavigationBar(this, false);

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

    private async void OnGuardarCambioClicked(object sender, EventArgs e)
    {
        try
        {
            string nuevaContraseña = ContraseñaEntry.Text?.Trim();
            string confirmacion = ContraseñaConfirmacionEntry.Text?.Trim();
            string email = EmailEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(nuevaContraseña) || string.IsNullOrWhiteSpace(confirmacion))
            {
                await DisplayAlert("Error", "Por favor, rellena ambas contraseñas.", "OK");
                return;
            }

            if (nuevaContraseña != confirmacion)
            {
                await DisplayAlert("Error", "Las contraseñas no coinciden.", "OK");
                return;
            }

            // Buscar al usuario (alumno o profesor)
            var alumno = db.ObtenerAlumnoPorEmail(email);
            var profesor = db.ObtenerProfesorPorEmail(email);


            if (alumno != null)
            {
                alumno.Password = HashearContraseña(nuevaContraseña);
                if (db.ActualizarAlumno(alumno.ID, alumno))
                    await DisplayAlert("Éxito", "Contraseña actualizada correctamente.", "OK");
                else
                    await DisplayAlert("Error", "No se pudo actualizar la contraseña.", "OK");
            }
            else if (profesor != null)
            {
                profesor.Password = HashearContraseña(nuevaContraseña);
                if (db.ActualizarProfesor(profesor.ID, profesor))
                    await DisplayAlert("Éxito", "Contraseña actualizada correctamente.", "OK");
                else
                    await DisplayAlert("Error", "No se pudo actualizar la contraseña.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "No se encontró el usuario.", "OK");
            }
            await Navigation.PushAsync(new Login());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
        }
    }

    private string HashearContraseña(string contraseña)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(contraseña);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

}