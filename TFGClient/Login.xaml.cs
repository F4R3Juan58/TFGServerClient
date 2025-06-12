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
                var administrador = db.ObtenerAdministradorPorEmailYPassword(email, contraseñaHash);

                if (email=="Administrador" && contraseña=="administrador")
                {
                    await Shell.Current.GoToAsync("AñadirAdministrador");
                }
                else if (administrador != null)
                {
                    // Guardar el email del administrador
                    Preferences.Set("UsuarioEmail", administrador.Email);
                    SesionUsuario.Instancia.AdministradorLogueado = administrador;
                    await Shell.Current.GoToAsync("Administrador");
                }
                else if  (alumno != null)
                {
                    // Hacer la consulta al backend Flask para obtener la invitación al Discord
                    var cliente = new HttpClient();
                    var contenido = new StringContent(JsonConvert.SerializeObject(new { email = alumno.Email }), Encoding.UTF8, "application/json");

                    var respuesta = await cliente.PostAsync("http://13.38.70.221:5000/obtener-invitacion", contenido);
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
                    }
                }
                else if (profesor != null)
                {
                    // ✅ Guardar el email SOLO si es profesor
                    // Hacer la consulta al backend Flask para obtener la invitación al Discord
                    var cliente = new HttpClient();
                    var contenido = new StringContent(JsonConvert.SerializeObject(new { email = profesor.Email }), Encoding.UTF8, "application/json");

                    var respuesta = await cliente.PostAsync("http://13.38.70.221:5000/obtener-invitacion", contenido);
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
                    }
                    Preferences.Set("UsuarioEmail", profesor.Email);
                    SesionUsuario.Instancia.ProfesorLogueado = profesor;
                    if (Shell.Current is AppShell appShell)
                    {
                        appShell.cargar();
                    }
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
        private static readonly Lazy<SesionUsuario> _instancia = new(() => new SesionUsuario());
        public static SesionUsuario Instancia => _instancia.Value;

        public Profesor ProfesorLogueado { get; set; }
        public Administradores AdministradorLogueado { get; set; }

        public bool HaySesionActiva => ProfesorLogueado != null || AdministradorLogueado != null;

        public void CerrarSesion()
        {
            ProfesorLogueado = null;
            AdministradorLogueado = null;
        }

        private SesionUsuario() { }
    }



}
