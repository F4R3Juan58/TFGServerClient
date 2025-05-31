using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using TFGClient.Models;
using TFGClient.Services;

namespace TFGClient.Interfaz
{
    public partial class Administrador : ContentPage
    {
        public string SelectedMenuItem { get; set; }
        private readonly DatabaseService _databaseService;
        public ObservableCollection<Alumno> Alumnos { get; set; }
        public ObservableCollection<Profesor> Profesores { get; set; }
        private readonly RellenarPickers rellenar = new();
        private ObservableCollection<string> cursosCompletos = new();
        public ObservableCollection<Opcion> Opciones1 { get; set; }
        public ObservableCollection<Opcion> Opciones2 { get; set; }

        public ObservableCollection<Curso> Cursos { get; set; }

        public Administrador()
        {
            InitializeComponent();
            Cursos = new ObservableCollection<Curso>();
            _databaseService = new DatabaseService();
            Alumnos = new ObservableCollection<Alumno>();
            Profesores = new ObservableCollection<Profesor>();
            Opciones1 = new ObservableCollection<Opcion>
    {
        new Opcion { Nombre = "1º", EsSeleccionado = false },
    };

            // Definir opciones para 2º
            Opciones2 = new ObservableCollection<Opcion>
    {
        new Opcion { Nombre = "2º", EsSeleccionado = false },
    };
            BindingContext = this;
            InicializarEventos();
            cargarBBDD();

        }

        private void InicializarEventos()
        {
            NivelPicker.SelectedIndexChanged += (s, e) =>
            {
                CargarFamilias();
            };

            FamiliaPicker.SelectedIndexChanged += (s, e) => CargarCursos();
        }

        public void cargarBBDD()
        {
            try
            {
                NivelPicker.ItemsSource = rellenar.CargarNiveles();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar los datos de base: {ex.Message}", "OK");
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

        private void CargarCursos()
        {
            try
            {
                if (NivelPicker.SelectedItem == null || FamiliaPicker.SelectedItem == null)
                    return;

                string nivel = (NivelPicker.SelectedItem as string)!;
                string familia = (FamiliaPicker.SelectedItem as string)!;

                CursoPicker.ItemsSource = rellenar.CargarCursos("1º", familia, nivel);
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar los cursos: {ex.Message}", "OK");
            }
        }

        // Métodos para manejar el clic en los botones del menú lateral
        private void OnSidebarCrearServidorTapped(object sender, EventArgs e)
        {
            SelectedMenuItem = "CrearServidor";
            AreaPrincipal.IsVisible = false;
            AreaCrearServidor.IsVisible = true;
            AreaPermisos.IsVisible = false;
            SideBarCrearServidor.BackgroundColor = Colors.LightGray;
            SideBarPermisos.BackgroundColor = Colors.Transparent;
        }

        private void OnSidebarPermisosTapped(object sender, EventArgs e)
        {
            SelectedMenuItem = "Permisos";
            AreaPrincipal.IsVisible = false;
            AreaCrearServidor.IsVisible = false;
            AreaPermisos.IsVisible = true;
            SideBarCrearServidor.BackgroundColor = Colors.Transparent;
            SideBarPermisos.BackgroundColor = Colors.LightGray;
        }

        private void OnSidebarIncidenciasTapped(object sender, EventArgs e)
        {
            SelectedMenuItem = "Incidencias";
            OnPropertyChanged(nameof(SelectedMenuItem));
        }

        private void OnSidebarLogsTapped(object sender, EventArgs e)
        {
            SelectedMenuItem = "Logs";
            OnPropertyChanged(nameof(SelectedMenuItem));
        }

        // Métodos vacíos para los botones
        private void OnCrearServidorButtonClicked(object sender, EventArgs e)
        {
            AreaPrincipal.IsVisible = false;
            AreaCrearServidor.IsVisible = true;
            SideBarCrearServidor.BackgroundColor = Colors.LightGray;
            SideBarPermisos.BackgroundColor = Colors.Transparent;
        }
        private void OnGestionarPermisosButtonClicked(object sender, EventArgs e)
        {
            AreaPrincipal.IsVisible = false;
            AreaCrearServidor.IsVisible = false;
            AreaPermisos.IsVisible = true;
            SideBarCrearServidor.BackgroundColor = Colors.Transparent;
            SideBarPermisos.BackgroundColor = Colors.LightGray;
        }
        private void OnVerIncidenciasResueltasTapped(object sender, EventArgs e) { }
        private void OnAccederLogsButtonClicked(object sender, EventArgs e) { }


        // Método para agregar un curso
        private void AddCurso(object sender, EventArgs e)
        {
            // Verificamos si todos los Pickers tienen un valor seleccionado
            if (NivelPicker.SelectedItem == null ||
                FamiliaPicker.SelectedItem == null ||
                CursoPicker.SelectedItem == null ||
                (!Opciones1.Any(o => o.EsSeleccionado) && !Opciones2.Any(o => o.EsSeleccionado))) // Verificamos si al menos un CheckBox está seleccionado
            {
                DisplayAlert("Error", "Por favor, seleccione todos los campos y al menos un curso.", "OK");
                return; // No agregar el curso si falta algún valor o ningún CheckBox está seleccionado
            }

            // Creamos un nuevo objeto Curso con los datos seleccionados
            var nuevoCurso = new Curso
            {
                Nivel = NivelPicker.SelectedItem.ToString(),
                Familia = FamiliaPicker.SelectedItem.ToString(),
                Grado = CursoPicker.SelectedItem.ToString(),
            };

            // Creamos una lista temporal para los cursos seleccionados
            List<string> cursosSeleccionados = new List<string>();

            // Si el CheckBox "1º" está seleccionado, lo añadimos a la lista de cursos seleccionados
            if (Opciones1.Any(o => o.EsSeleccionado))
            {
                cursosSeleccionados.Add("1º");
            }

            // Si el CheckBox "2º" está seleccionado, lo añadimos a la lista de cursos seleccionados
            if (Opciones2.Any(o => o.EsSeleccionado))
            {
                cursosSeleccionados.Add("2º");
            }

            // Añadimos cada curso a la colección (primero 1º, luego 2º si están seleccionados)
            foreach (var curso in cursosSeleccionados)
            {
                var nuevoCursoConCurso = new Curso
                {
                    Nivel = nuevoCurso.Nivel,
                    Familia = nuevoCurso.Familia,
                    Grado = nuevoCurso.Grado,
                    CursosSeleccionados = curso
                };
                Cursos.Add(nuevoCursoConCurso);
            }

            // Limpiar los Pickers y CheckBoxes después de agregar el curso
            NivelPicker.SelectedItem = null;
            FamiliaPicker.SelectedItem = null;
            CursoPicker.SelectedItem = null;
            Curso1.IsChecked = false;
            Curso2.IsChecked = false;
        }

        // Método para manejar el clic en el botón de eliminar
        private void OnEliminarCursoClicked(object sender, EventArgs e)
        {
            // Encontrar el botón clickeado
            var button = sender as Button;

            if (button == null) return;

            // Obtener el objeto Curso asociado a ese botón
            var cursoAEliminar = button.BindingContext as Curso;

            if (cursoAEliminar != null)
            {
                // Eliminar el curso de la colección
                Cursos.Remove(cursoAEliminar);

                // Opcional: Si tienes algún mensaje que mostrar o un comportamiento adicional
                DisplayAlert("Eliminado", "El curso fue eliminado.", "OK");
            }
        }

        private async void CrearServidorDiscord(object sender, EventArgs e)
        {
            try
            {
                string emailsesion = Preferences.Get("UsuarioEmail", "");

                if (string.IsNullOrWhiteSpace(emailsesion))
                {
                    await DisplayAlert("Error", "No se encontró el correo del usuario.", "OK");
                    return;
                }

                var profesor = _databaseService.ObtenerProfesorPorEmail(emailsesion);
                if (profesor == null)
                {
                    await DisplayAlert("Acceso denegado", "Solo los profesores pueden crear servidores de Discord.", "OK");
                    return;
                }

                int usuarioId = profesor.ID;
                int instiId = profesor.InstiID;
                String email = profesor.Email;

                var institutos = _databaseService.ObtenerTodosLosInstitutos();
                var instituto = institutos.FirstOrDefault(i => i.ID == instiId);

                if (instituto == null)
                {
                    await DisplayAlert("Error", "No se encontró el instituto.", "OK");
                    return;
                }

                string nombreInstituto = instituto.Nombre;

                // Agregamos el email al objeto que enviamos al backend
                var dataToSend = new
                {
                    nombre = nombreInstituto,
                    usuario_id = usuarioId,
                    insti_id = instiId,
                    email
                };

                using var httpClient = new HttpClient();
                var content = new StringContent(
                    JsonConvert.SerializeObject(dataToSend),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync("http://localhost:5000/crear-servidor", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);

                    if (result != null && result.TryGetValue("invite", out string inviteUrl))
                    {
                        bool abrir = await DisplayAlert("Servidor creado", $"Invitación:\n{inviteUrl}", "Abrir", "Cerrar");
                        if (abrir)
                        {
                            await Launcher.Default.OpenAsync(inviteUrl);
                        }
                    }
                    else
                    {
                        await DisplayAlert("Servidor creado", "El servidor fue creado, pero no se pudo generar una invitación.", "OK");
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error desde el servidor: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error inesperado: {ex.Message}", "OK");
            }
        }

        private async void ConfigurarServidor(object sender, EventArgs e)
        {
            try
            {
                string emailsesion = Preferences.Get("UsuarioEmail", "");

                if (string.IsNullOrWhiteSpace(emailsesion))
                {
                    await DisplayAlert("Error", "No se encontró el correo del usuario.", "OK");
                    return;
                }

                var cursosLista = ObtenerDatosCursos();

                if (cursosLista.Count == 0)
                {
                    await DisplayAlert("Error", "No se han seleccionado cursos o grados.", "OK");
                    return;
                }

                var dataToSend = new Dictionary<string, object>
        {
            { "email", emailsesion },
            { "cursos", cursosLista }
        };

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsJsonAsync("http://localhost:5000/configurar-servidor", dataToSend);
                    var responseText = await response.Content.ReadAsStringAsync();

                    await DisplayAlert("Cursos enviados", Newtonsoft.Json.JsonConvert.SerializeObject(cursosLista), "OK");

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("✅ Configuración completada", "El servidor ha sido configurado correctamente con los roles y categorías.", "OK");
                    }
                    else
                    {
                        try
                        {
                            var json = JsonDocument.Parse(responseText);
                            var root = json.RootElement;

                            if (root.TryGetProperty("invite_url", out var inviteUrl))
                            {
                                bool abrir = await Application.Current.MainPage.DisplayAlert(
                                    "🔗 El bot aún no está en el servidor",
                                    "Antes de continuar, debes invitar al bot a tu servidor.\n\nPulsa en 'Invitar Bot' para abrir el enlace.\n\nDespués, vuelve a esta pantalla y pulsa el botón de 'Configurar Servidor' de nuevo.",
                                    "Invitar Bot", "Cancelar");

                                if (abrir)
                                {
                                    await Launcher.Default.OpenAsync(inviteUrl.GetString());
                                    await DisplayAlert("📌 Importante",
                                        "Una vez que hayas invitado al bot, vuelve aquí y pulsa nuevamente el botón 'Configurar Servidor' para aplicar los cambios.",
                                        "Entendido");
                                }
                            }
                            else
                            {
                                await DisplayAlert("Error", root.GetProperty("error").GetString(), "OK");
                            }
                        }
                        catch
                        {
                            await DisplayAlert("Error", "Hubo un problema al procesar la respuesta del servidor.", "OK");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }

        private List<object> ObtenerDatosCursos()
        {
            var listaCursos = new List<object>();

            foreach (var curso in Cursos)
            {
                listaCursos.Add(new
                {
                    grado = curso.CursosSeleccionados?.Trim(),
                    curso = curso.Grado?.Trim()
                });
            }

            return listaCursos;
        }

        private async Task<HttpResponseMessage> EnviarDatosAlServidorFlask(object data)
        {
            // Usar HttpClient para enviar los datos a tu servidor Flask
            using (var client = new HttpClient())
            {
                // Configura la URL de tu servidor Flask
                var url = "localhost/crearServidor"; // Cambia la URL a la de tu servidor Flask

                // Crear el contenido a enviar (se asume que Flask espera JSON)
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                // Enviar la solicitud HTTP POST
                return await client.PostAsync(url, content);
            }
        }

        // Método para buscar alumnos por nombre
        private void BuscarAlumno(object sender, EventArgs e)
        {
            // Obtener el nombre de la búsqueda
            string nombreBusqueda = NombreEntry.Text;

            // Si el nombre no está vacío, realizar la búsqueda
            if (!string.IsNullOrEmpty(nombreBusqueda))
            {
                // Consultamos a la base de datos por alumnos cuyo nombre coincida con la búsqueda
                var alumnosEncontrados = _databaseService.ObtenerTodosLosAlumnos()
                    .Where(a => a.Nombre.Contains(nombreBusqueda, StringComparison.OrdinalIgnoreCase)) // Buscar por nombre
                    .ToList();

                // Limpiamos la colección actual
                Alumnos.Clear();

                // Añadimos los resultados a la colección
                foreach (var alumno in alumnosEncontrados)
                {
                    Alumnos.Add(alumno);
                }
            }
            else
            {
                // Si no se encuentra ningún nombre en la búsqueda, mostrar todos los alumnos
                var todosLosAlumnos = _databaseService.ObtenerTodosLosAlumnos();
                Alumnos.Clear();
                foreach (var alumno in todosLosAlumnos)
                {
                    Alumnos.Add(alumno);
                }
            }
        }


        // Método para obtener los roles de Discord del servidor Flask
        private async Task<List<string>> ObtenerRolesDiscord(string discordId)
        {
            var roles = new List<string>();

            // Realizar la solicitud HTTP a Flask para obtener los roles
            var url = "http://your-flask-server-url/roles";  // URL de tu servidor Flask
            var client = new HttpClient();
            var response = await client.GetAsync($"{url}?discordId={discordId}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                roles = JsonConvert.DeserializeObject<List<string>>(result);
            }

            return roles;
        }


        private async void OnAlumnoTapped(object sender, ItemTappedEventArgs e)
        {
            var alumno = e.Item as Alumno;
            if (alumno != null)
            {
                var roles = await ObtenerRolesDiscord(alumno.DiscordID);
                alumno.Roles = new ObservableCollection<string>(roles);
                // Actualiza la UI, si es necesario
            }
        }

        private async void OnRemoveRoleClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var role = button?.BindingContext as string;
            var alumno = button?.Parent?.BindingContext as Alumno;
            var profesor = button?.Parent?.BindingContext as Profesor;

            if (alumno != null && role != null)
            {
                alumno.Roles.Remove(role);
                await EliminarRolDelServidor(role, alumno.DiscordID);
            }
        }

        private async Task EliminarRolDelServidor(string role, string discordId)
        {
            var url = "http://your-flask-server-url/remove-role";
            var client = new HttpClient();

            var data = new
            {
                DiscordId = discordId,
                Role = role
            };

            var jsonData = JsonConvert.SerializeObject(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                DisplayAlert("Éxito", $"Rol {role} eliminado correctamente.", "OK");
            }
            else
            {
                DisplayAlert("Error", "No se pudo eliminar el rol.", "OK");
            }
        }

    }
    // Clase que representa un curso
    public class Curso
    {
        public string Nivel { get; set; }
        public string Familia { get; set; }
        public string CursosSeleccionados { get; set; } // Cursos seleccionados con CheckBox
        public string Grado { get; set; }
    }

    // Clase que representa las opciones de CheckBox
    public class Opcion
    {
        public string Nombre { get; set; } // Nombre de la opción (por ejemplo, "1º", "2º")
        public bool EsSeleccionado { get; set; } // Si está marcado o no
    }

}