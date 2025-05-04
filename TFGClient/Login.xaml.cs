using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TFGClient.Models;
using TFGClient.Services;

namespace TFGClient
{
    public partial class Login : ContentPage
    {
        private readonly DatabaseService db = new();

        public Login()
        {
            InitializeComponent();
        }

        private async void onLoginClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim() ?? "";
            string contraseña = ContrasenaEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(contraseña))
            {
                await DisplayAlert("Error", "Introduce tu correo y contraseña.", "OK");
                return;
            }

            string contraseñaHash = HashearContraseña(contraseña);

            try
            {
                var alumno = db.ObtenerAlumnoPorEmailYPassword(email, contraseñaHash);
                var profesor = db.ObtenerProfesorPorEmailYPassword(email, contraseñaHash);

                if (alumno != null)
                {
                    var uri = new Uri("https://discord.gg/f3YA784A"); // Aquí tu lógica real después
                    await Launcher.Default.OpenAsync(uri);
                }
                else if (profesor != null)
                {
                    await Shell.Current.GoToAsync("Profesor");
                }
                else
                {
                    await DisplayAlert("Error", "Usuario o contraseña incorrectos.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al intentar iniciar sesión: {ex.Message}", "OK");
            }
        }

        private async void onRegistroClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Registro());
        }

        private string HashearContraseña(string contraseña)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(contraseña);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async void onModificarClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ModificarDatos());
        }

        private async void onRecuperarClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RecurperarPassword());
        }
    }
}