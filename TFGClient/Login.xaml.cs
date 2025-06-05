using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
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
            // Ocultar botón de atrás
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                IsVisible = false
            });

            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);

            // Ocultar barra de navegación completa
            NavigationPage.SetHasNavigationBar(this, false);


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
                    // Hacer la consulta al backend Flask para obtener la invitación al Discord
                    var cliente = new HttpClient();
                    var contenido = new StringContent(JsonConvert.SerializeObject(new { email = alumno.Email }), Encoding.UTF8, "application/json");

                    var respuesta = await cliente.PostAsync("http://localhost:5000/obtener-invitacion", contenido);
                    if (respuesta.IsSuccessStatusCode)
                    {
                        var json = await respuesta.Content.ReadAsStringAsync();
                        var invitacion = (string)JsonConvert.DeserializeObject<dynamic>(json).invitacion;

                        if (!string.IsNullOrEmpty(invitacion))
                        {
                            // Acceso para alumnos, abrir la invitación de Discord
                            var uri = new Uri(invitacion);
                            await Launcher.Default.OpenAsync(uri);
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo obtener la invitación al Discord.", "OK");
                    }
                }
                else if (profesor != null)
                {
                    // ✅ Guardar el email SOLO si es profesor
                    Preferences.Set("UsuarioEmail", profesor.Email);
                    SesionUsuario.Instancia.ProfesorLogueado = profesor;
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

    public class SesionUsuario
    {
        public static SesionUsuario _instancia;

        public static SesionUsuario Instancia
        {
            get
            {
                if (_instancia == null)
                    _instancia = new SesionUsuario();

                return _instancia;
            }
        }

        public Profesor ProfesorLogueado { get; set; }

        public SesionUsuario() { }
    }
}
