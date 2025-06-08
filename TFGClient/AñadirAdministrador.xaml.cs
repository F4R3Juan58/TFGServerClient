using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using TFGClient.Services;


namespace TFGClient
{
    public partial class AñadirAdministrador : ContentPage
    {
        private readonly RellenarPickers rellenar = new();
        private ObservableCollection<string> cursosCompletos = new();
        private readonly DatabaseService db = new();


        public AñadirAdministrador()
        {
            InitializeComponent();
            InicializarEventos();

            // Ocultar botón de atrás
            NavigationPage.SetHasBackButton(this, false);

            // Ocultar barra de navegación completa
            NavigationPage.SetHasNavigationBar(this, false);

            // Para Shell
            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
        }

        private void InicializarEventos()
        {
            CAPicker.ItemsSource = rellenar.CargarNombreComunidades();
            CAPicker.SelectedIndexChanged += (s, e) =>
            {
                CargarLocalidades();
                // Limpiar Institutos cuando cambie Comunidad Autónoma
                InstitutoPicker.ItemsSource = null;
            };
            LocalidadPicker.SelectedIndexChanged += (s, e) => CargarInstitutos();
        }

        private void OnContinuar2Clicked(object sender, EventArgs e)
        {
            // Validación simple de campos
            if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
                string.IsNullOrWhiteSpace(ApellidosEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(ContrasenaEntry.Text) ||
                string.IsNullOrWhiteSpace(ContrasenaConfirmacionEntry.Text) ||
                CAPicker.SelectedItem == null ||
                LocalidadPicker.SelectedItem == null ||
                InstitutoPicker.SelectedItem == null)
            {
                DisplayAlert("Error", "Por favor, rellena todos los campos.", "OK");
                return;
            }

            if (ContrasenaEntry.Text != ContrasenaConfirmacionEntry.Text)
            {
                DisplayAlert("Error", "Las contraseñas no coinciden.", "OK");
                return;
            }

            // Aquí iría la lógica para continuar con el registro del administrador
            // Por ejemplo, enviar datos al servidor, etc.

            DisplayAlert("Éxito", "Administrador registrado correctamente.", "OK");
        }

        private void CargarLocalidades()
        {
            try
            {
                if (CAPicker.SelectedItem == null) return;

                string nombreCA = (CAPicker.SelectedItem as string)!;
                int comunidadId = ObtenerComunidadId(nombreCA);

                if (comunidadId > 0)
                {
                    LocalidadPicker.ItemsSource = rellenar.CargarLocalidades(comunidadId);
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar las localidades: {ex.Message}", "OK");
            }
        }


        private void CargarInstitutos()
        {
            try
            {
                if (CAPicker.SelectedItem == null || LocalidadPicker.SelectedItem == null) return;

                string nombreCA = (CAPicker.SelectedItem as string)!;
                string localidad = (LocalidadPicker.SelectedItem as string)!;

                int comunidadId = ObtenerComunidadId(nombreCA);

                if (comunidadId > 0)
                {
                    InstitutoPicker.ItemsSource = rellenar.CargarNombreInstitutos(comunidadId, localidad);
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar los institutos: {ex.Message}", "OK");
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
                string nombre = NombreEntry.Text?.Trim();
                string apellidos = ApellidosEntry.Text?.Trim();
                string email = EmailEntry.Text?.Trim();
                string password = ContrasenaEntry.Text?.Trim();

                string confirmacion = ContrasenaConfirmacionEntry.Text?.Trim();

                if (password != confirmacion)
                {
                    await DisplayAlert("Error", "Las contraseñas no coinciden.", "OK");
                    return;
                }

                string comunidadNombre = CAPicker.SelectedItem?.ToString();
                string localidad = LocalidadPicker.SelectedItem?.ToString();
                string institutoNombre = InstitutoPicker.SelectedItem?.ToString();


                if (db.ExisteEmail(email))
                {
                    await DisplayAlert("Error", "Este correo ya está registrado.", "OK");
                    return;
                }

                // Hashear la password
                string contraseñaHash = HashearContraseña(password);

                // Obtener IDs reales
                int institutoId = ObtenerInstitutoId(institutoNombre);
                int rolId = 1;
                string discordId = null; // aún no disponible

                var administrador = new Models.Administradores
                {
                    Nombre = nombre,
                    Apellido = apellidos,
                    Password = contraseñaHash,
                    Email = email,
                    InstiID = institutoId,
                    RolID = rolId,
                    DiscordID = discordId
                };

                if (db.InsertarAdministrador(administrador))
                    await DisplayAlert("Éxito", "Administrador registrado correctamente.", "OK");
                else
                    await DisplayAlert("Error", "No se pudo registrar el administrador.", "OK");
            await Navigation.PushAsync(new Login());

        }

        private string HashearContraseña(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private int ObtenerComunidadId(string nombre)
        {
            return db.ObtenerTodasLasComunidades()
                     .FirstOrDefault(c => c.Nombre == nombre)?.ID ?? 0;
        }

        private int ObtenerInstitutoId(string nombre)
        {
            return db.ObtenerTodosLosInstitutos()
                     .FirstOrDefault(i => i.Nombre == nombre)?.ID ?? 0;
        }

        private int ObtenerCursoId(string nombre)
        {
            return db.ObtenerTodosLosCursos()
                     .FirstOrDefault(c => c.Nombre == nombre)?.ID ?? 0;
        }

    }
}
