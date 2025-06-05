using System.Net.Mail;
using System.Net;
using System.Collections.ObjectModel;
using TFGClient.Models;
using TFGClient.Services;


namespace TFGClient
{
    public partial class Registro : ContentPage
    {
        private readonly RellenarPickers rellenar = new();
        private ObservableCollection<string> cursosCompletos = new();

        private string codigoGenerado = string.Empty;

        public Registro()
        {
            InitializeComponent();
            InicializarEventos();
            cargarBBDD();
            // Ocultar botón de atrás
            NavigationPage.SetHasBackButton(this, false);

            // Ocultar barra de navegación completa
            NavigationPage.SetHasNavigationBar(this, false);

            // O en caso de Shell:
            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
        }

        private void InicializarEventos()
        {
            CAPicker.SelectedIndexChanged += (s, e) => CargarLocalidades();
            LocalidadPicker.SelectedIndexChanged += (s, e) => CargarInstitutos();

            NivelPicker.SelectedIndexChanged += (s, e) => {
                CargarFamilias();
                CargarGrados();
            };

            FamiliaPicker.SelectedIndexChanged += (s, e) => CargarCursos();
            GradoPicker.SelectedIndexChanged += (s, e) => CargarCursos();
        }

        public void cargarBBDD()
        {
            try
            {
                CAPicker.ItemsSource = rellenar.CargarNombreComunidades();
                NivelPicker.ItemsSource = rellenar.CargarNiveles();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar los datos de base: {ex.Message}", "OK");
            }
        }

        private async void OnVerificacionClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlert("Error", "Introduce un correo válido.", "OK");
                return;
            }

            if (db.ExisteEmail(email))
            {
                await DisplayAlert("Error", "Este correo ya está registrado.", "OK");
                return;
            }

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
            if (PAPicker.SelectedItem.ToString() == "Profesor")
            {
                registro();
            }
            else
            {
                Formulario3.IsVisible = true;
                Formulario2.IsVisible = false;
            }

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


        private readonly DatabaseService db = new();

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            registro();
        }

        private async void registro()
        {
            try
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
                string tipoUsuario = PAPicker.SelectedItem?.ToString();
                string nivel = NivelPicker.SelectedItem?.ToString();
                string grado = GradoPicker.SelectedItem?.ToString();
                string familia = FamiliaPicker.SelectedItem?.ToString();
                string cursoNombre = CursoPicker.SelectedItem?.ToString();


                if (PAPicker.SelectedItem.ToString() == "Profesor")
                {
                    if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellidos) || string.IsNullOrWhiteSpace(email) ||
                   string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(comunidadNombre) || string.IsNullOrWhiteSpace(institutoNombre) ||
                   string.IsNullOrWhiteSpace(tipoUsuario))
                    {
                        await DisplayAlert("Error", "Por favor, rellena todos los campos.", "OK");
                        return;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellidos) || string.IsNullOrWhiteSpace(email) ||
                   string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(comunidadNombre) || string.IsNullOrWhiteSpace(institutoNombre) ||
                   string.IsNullOrWhiteSpace(tipoUsuario) || string.IsNullOrWhiteSpace(nivel) || string.IsNullOrWhiteSpace(grado) ||
                   string.IsNullOrWhiteSpace(familia) || string.IsNullOrWhiteSpace(cursoNombre))
                    {
                        await DisplayAlert("Error", "Por favor, rellena todos los campos.", "OK");
                        return;
                    }
                }

               

                if (db.ExisteEmail(email))
                {
                    await DisplayAlert("Error", "Este correo ya está registrado.", "OK");
                    return;
                }

                // Hashear la password
                string contraseñaHash = HashearContraseña(password);

                // Obtener IDs reales
                int comunidadId = ObtenerComunidadId(comunidadNombre);
                int institutoId = ObtenerInstitutoId(institutoNombre);
                int cursoId = ObtenerCursoId(cursoNombre);
                int rolId = tipoUsuario == "Alumno" ? 1 : 2; // Ajusta si es necesario
                string discordId = null; // aún no disponible

                if (tipoUsuario == "Alumno")
                {
                    var alumno = new Alumno
                    {
                        Nombre = nombre,
                        Apellido = apellidos,
                        Password = contraseñaHash,
                        Email = email,
                        ComunidadID = comunidadId,
                        InstiID = institutoId,
                        RolID = rolId,
                        IsDelegado = false,
                        Puntos = 0,
                        CursoID = cursoId,
                        DiscordID = discordId
                    };

                    if (db.InsertarAlumno(alumno))
                        await DisplayAlert("Éxito", "Alumno registrado correctamente.", "OK");
                    else
                        await DisplayAlert("Error", "No se pudo registrar el alumno.", "OK");
                }
                else if (tipoUsuario == "Profesor")
                {
                    var profesor = new Profesor
                    {
                        Nombre = nombre,
                        Apellido = apellidos,
                        Password = contraseñaHash,
                        Email = email,
                        ComunidadID = comunidadId,
                        InstiID = institutoId,
                        RolID = rolId,
                        IsJefe = false,
                        IsTutor = false,
                        CursoID = 183,
                        DiscordID = null
                    };

                    if (db.InsertarProfesor(profesor))
                        await DisplayAlert("Éxito", "Profesor registrado correctamente.", "OK");
                    else
                        await DisplayAlert("Error", "No se pudo registrar el profesor.", "OK");
                }
                await Navigation.PushAsync(new Login());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
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

        private void CargarFamilias()
        {
            try
            {
                if (NivelPicker.SelectedItem == null) return;

                string nivel = (NivelPicker.SelectedItem as string)!;
                FamiliaPicker.ItemsSource = rellenar.CargarFamiliasPorNivel(nivel);
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar las familias: {ex.Message}", "OK");
            }
        }

        private void CargarGrados()
        {
            try
            {
                GradoPicker.ItemsSource = rellenar.CargarGrados();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar los grados: {ex.Message}", "OK");
            }
        }

        private void CargarCursos()
        {
            try
            {
                if (NivelPicker.SelectedItem == null || GradoPicker.SelectedItem == null || FamiliaPicker.SelectedItem == null)
                    return;

                string nivel = (NivelPicker.SelectedItem as string)!;
                string grado = (GradoPicker.SelectedItem as string)!;
                string familia = (FamiliaPicker.SelectedItem as string)!;

                CursoPicker.ItemsSource = rellenar.CargarCursos(grado, familia, nivel);
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar los cursos: {ex.Message}", "OK");
            }
        }
    }

}
