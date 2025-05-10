using System.Collections.ObjectModel;
using System.Text;
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
        public ObservableCollection<Opcion> Opciones1 { get; set; } // Opciones para 1º
        public ObservableCollection<Opcion> Opciones2 { get; set; } // Opciones para 2º

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


        private async void CrearSercidorDiscord(object sender, EventArgs e)
        {
            try
            {
                // Concatenar todos los cursos y grados seleccionados
                string cursosGradosConcatenados = ObtenerDatosConcatenados();

                // Crear el objeto que vamos a enviar al servidor Flask
                var dataToSend = new
                {
                    cursosGrados = cursosGradosConcatenados
                };

                // Hacer la llamada HTTP para enviar los datos al servidor Flask
                var response = await EnviarDatosAlServidorFlask(dataToSend);

                // Mostrar una respuesta de éxito o error
                if (response.IsSuccessStatusCode)
                {
                    DisplayAlert("Éxito", "Datos enviados correctamente.", "OK");
                }
                else
                {
                    DisplayAlert("Error", "Hubo un problema al enviar los datos.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Manejar cualquier error
                DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }

        private string ObtenerDatosConcatenados()
        {
            // Obtener todos los cursos y grados seleccionados
            var cursosGrados = new List<string>();

            // Concatenar los cursos
            foreach (var curso in Cursos)
            {
                cursosGrados.Add(curso.CursosSeleccionados); // Puedes agregar otros campos como `Grado` si es necesario
            }

            // Crear una cadena con los cursos y grados concatenados
            return string.Join(", ", cursosGrados);
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