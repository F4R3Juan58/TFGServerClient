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
        public ObservableCollection<string> CategoriasParaEliminar { get; set; } = new ObservableCollection<string>();


        public ObservableCollection<Curso> Cursos { get; set; }
        string institutoName;

        public Administrador()
        {
            InitializeComponent();
            Cursos = new ObservableCollection<Curso>();
            _databaseService = new DatabaseService();
            Alumnos = new ObservableCollection<Alumno>();
            Profesores = new ObservableCollection<Profesor>();
            var profesor = SesionUsuario.Instancia.ProfesorLogueado;
            if (profesor != null)
            {
                NombreProfesor.Text = $"{profesor.Nombre} {profesor.Apellido}";
                institutoName = _databaseService.ObtenerNombreInstituto(profesor.InstiID);
            }
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


        private async void InicializarEventos()
        {
            NivelPicker.SelectedIndexChanged += (s, e) =>
            {
                CargarFamilias();
            };
            await CargarCursosParaEliminarDesdeServidorAsync();

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

        private void ResetSidebarButtonsBackground()
        {
            SideBarCrearServidor.BackgroundColor = Colors.Transparent;
            SideBarEliminarServidor.BackgroundColor = Colors.Transparent;
            SideBarAñadirCurso.BackgroundColor = Colors.Transparent;
            SideBarEliminarCurso.BackgroundColor = Colors.Transparent;
            SideBarGestionAlumnos.BackgroundColor = Colors.Transparent;
            SideBarGestionProfesores.BackgroundColor = Colors.Transparent;
        }

        private void HideAllAreas()
        {
            AreaCrearServidor.IsVisible = false;
            AreaEliminarServidor.IsVisible = false;
            AreaAñadirCurso.IsVisible = false;
            AreaEliminarCurso.IsVisible = false;
            AreaGestionAlumnos.IsVisible = false;
            AreaGestionProfesores.IsVisible = false;
        }

        private void OnSidebarCrearServidorTapped(object sender, EventArgs e)
        {
            ResetSidebarButtonsBackground();
            HideAllAreas();
            ServidorNombreEntry.Text = institutoName;
            SideBarCrearServidor.BackgroundColor = Colors.LightGray;
            AreaCrearServidor.IsVisible = true;
        }

        private void OnSidebarEliminarServidorTapped(object sender, EventArgs e)
        {
            ResetSidebarButtonsBackground();
            HideAllAreas();
            ServidorNombreEntryEliminar.Text = institutoName;
            SideBarEliminarServidor.BackgroundColor = Colors.LightGray;
            AreaEliminarServidor.IsVisible = true;
        }

        private void OnSidebarAñadirCursoTapped(object sender, EventArgs e)
        {
            ResetSidebarButtonsBackground();
            HideAllAreas();

            SideBarAñadirCurso.BackgroundColor = Colors.LightGray;
            AreaAñadirCurso.IsVisible = true;
        }

        private async void OnSidebarEliminarCursoTapped(object sender, EventArgs e)
        {
            ResetSidebarButtonsBackground();
            HideAllAreas();

            SideBarEliminarCurso.BackgroundColor = Colors.LightGray;
            AreaEliminarCurso.IsVisible = true;

            await CargarCursosParaEliminarDesdeServidorAsync();

        }


        private async Task CargarCursosParaEliminarDesdeServidorAsync()
        {
            try
            {
                var profesor = SesionUsuario.Instancia.ProfesorLogueado;

                if (profesor == null)
                {
                    await DisplayAlert("Error", "No se encontró el profesor logueado.", "OK");
                    return;
                }

                

                var dataToSend = new
                {
                    InstiID = profesor.InstiID,
                };

                var jsonData = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync("http://localhost:5000/obtener-categorias", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var categorias = JsonConvert.DeserializeObject<List<string>>(json);

                    if (categorias != null)
                    {
                        Cursos.Clear();

                        foreach (var cat in categorias)
                        {
                            string nivel = "";
                            string familia = "";
                            string cursosSeleccionados = "";
                            string grado = "";

                            var partes = cat.Split(' ', 2);
                            if (partes.Length == 2)
                            {
                                cursosSeleccionados = partes[0];
                                grado = partes[1];
                            }
                            else
                            {
                                cursosSeleccionados = cat;
                            }

                            Cursos.Add(new Curso
                            {
                                Nivel = nivel,
                                Familia = familia,
                                CursosSeleccionados = cursosSeleccionados,
                                Grado = grado
                            });
                        }
                    }
                }
                else
                {
                    await DisplayAlert("Error", "No se pudieron obtener las categorías del servidor.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar categorías: {ex.Message}", "OK");
            }
        }





        private void OnSidebarGestionAlumnosTapped(object sender, EventArgs e)
        {
            ResetSidebarButtonsBackground();
            HideAllAreas();

            SideBarGestionAlumnos.BackgroundColor = Colors.LightGray;
            AreaGestionAlumnos.IsVisible = true;
        }

        private void OnSidebarGestionProfesoresTapped(object sender, EventArgs e)
        {
            ResetSidebarButtonsBackground();
            HideAllAreas();

            SideBarGestionProfesores.BackgroundColor = Colors.LightGray;
            AreaGestionProfesores.IsVisible = true;
        }


        // Métodos vacíos para los botones
        private void OnCrearServidorButtonClicked(object sender, EventArgs e)
        {
            AreaPrincipal.IsVisible = false;
            AreaCrearServidor.IsVisible = true;
            SideBarCrearServidor.BackgroundColor = Colors.LightGray;
        }
        private void OnGestionarPermisosButtonClicked(object sender, EventArgs e)
        {
            AreaPrincipal.IsVisible = false;
            AreaCrearServidor.IsVisible = false;
            SideBarCrearServidor.BackgroundColor = Colors.Transparent;

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
        private async void OnEliminarCursoClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var profesor = SesionUsuario.Instancia.ProfesorLogueado;

            var cursoAEliminar = button.BindingContext as Curso;
            if (cursoAEliminar == null) return;

            // Confirmar con el usuario antes de eliminar
            bool confirmar = await DisplayAlert("Confirmar eliminación",
            $"¿Quieres eliminar la categoría '{cursoAEliminar.CursosSeleccionados} {cursoAEliminar.Grado}' del servidor Discord?",
            "Sí", "No");

            if (!confirmar) return;

            try
            {
                // Preparar objeto JSON con los datos necesarios para eliminar
                var dataToSend = new
                {
                    InstiID = profesor.InstiID,
                    categoria = $"{cursoAEliminar.CursosSeleccionados} {cursoAEliminar.Grado}"
                };

                var jsonData = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();

                // Cambia esta URL por la de tu endpoint Flask para eliminar categorías
                var response = await httpClient.PostAsync("http://localhost:5000/eliminar-categoria", content);

                if (response.IsSuccessStatusCode)
                {
                    // Eliminar localmente de la colección para actualizar UI
                    Cursos.Remove(cursoAEliminar);

                    await DisplayAlert("Éxito", "Categoría eliminada correctamente del servidor Discord.", "OK");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error en servidor: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Excepción al conectar con servidor: {ex.Message}", "OK");
            }
        }


        private async void CrearServidorDiscord(object sender, EventArgs e)
        {
            try
            {
                string email = Preferences.Get("UsuarioEmail", "");

                if (string.IsNullOrWhiteSpace(email))
                {
                    await DisplayAlert("Error", "No se encontró el correo del usuario.", "OK");
                    return;
                }

                var profesor = _databaseService.ObtenerProfesorPorEmail(email);
                if (profesor == null)
                {
                    await DisplayAlert("Acceso denegado", "Solo los profesores pueden crear servidores de Discord.", "OK");
                    return;
                }

                string usuarioEmail = profesor.Email;
                int instiId = profesor.InstiID;

                var institutos = _databaseService.ObtenerTodosLosInstitutos();
                var instituto = institutos.FirstOrDefault(i => i.ID == instiId);

                if (instituto == null)
                {
                    await DisplayAlert("Error", "No se encontró el instituto.", "OK");
                    return;
                }

                string nombreInstituto = instituto.Nombre;

                var dataToSend = new
                {
                    nombre = nombreInstituto,
                    email = usuarioEmail,
                    insti_id = instiId  // <-- Esto es importante
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

        // Nuevo método para la sección "Configurar servidor"
        private async void AñadirCursos(object sender, EventArgs e)
        {

            try
            {
                // Concatenar todos los cursos y grados seleccionados
                string cursosGradosConcatenados = ObtenerDatosConcatenados();

                // Mostrar los cursos concatenados antes de enviarlos
                await DisplayAlert("Cursos a enviar", cursosGradosConcatenados, "OK");

                var profesor = SesionUsuario.Instancia.ProfesorLogueado;

                // Crear el objeto que vamos a enviar al servidor Flask
                var dataToSend = new
                {
                    InstiID = profesor.InstiID,
                    cursosGrados = cursosGradosConcatenados
                };

                // Serializar a JSON
                var jsonData = JsonConvert.SerializeObject(dataToSend);
                Console.WriteLine("JSON enviado: " + jsonData);


                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Crear cliente HTTP y hacer POST a Flask (localhost:5000)
                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync("http://localhost:5000/añadir-cursos", content);

                // Manejar respuesta
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Cursos enviados correctamente al servidor Flask.", "OK");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Fallo en servidor Flask: {error}", "OK");
                }
                Cursos.Clear();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Excepción al conectar con Flask: {ex.Message}", "OK");
            }
        }



        private string ObtenerDatosConcatenados()
        {
            var listaConcat = Cursos
            .Select(c => $"{c.CursosSeleccionados} {c.Grado}")
            .ToList();

            return string.Join(", ", listaConcat);
        }


        // Método para buscar alumnos por nombre
        private void BuscarAlumno_Clicked(object sender, EventArgs e)
        {
            // Obtener el nombre de la búsqueda
            string nombreBusqueda = BuscarAlumnoEntry.Text;
            var profe = SesionUsuario.Instancia.ProfesorLogueado;
            // Si el nombre no está vacío, realizar la búsqueda
            if (!string.IsNullOrEmpty(nombreBusqueda))
            {
                // Consultamos a la base de datos por alumnos cuyo nombre coincida con la búsqueda
                var alumnosEncontrados = _databaseService.ObtenerTodosLosAlumnos()
                .Where(a => a.Nombre.Contains(nombreBusqueda, StringComparison.OrdinalIgnoreCase) && a.InstiID == profe.InstiID)
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

        private void BuscarProfesor_Clicked(object sender, EventArgs e)
        {
            // Obtener el nombre de la búsqueda
            string nombreBusqueda = BuscarProfesorEntry.Text;
            var profe = SesionUsuario.Instancia.ProfesorLogueado;

            // Si el nombre no está vacío, realizar la búsqueda
            if (!string.IsNullOrEmpty(nombreBusqueda))
            {
                // Consultamos a la base de datos por alumnos cuyo nombre coincida con la búsqueda
                var profesoresEncontrados = _databaseService.ObtenerTodosLosProfesores()
                .Where(a => a.Nombre.Contains(nombreBusqueda, StringComparison.OrdinalIgnoreCase) && a.InstiID == profe.InstiID)
                .ToList();

                // Limpiamos la colección actual
                Profesores.Clear();

                // Añadimos los resultados a la colección
                foreach (var profesor in profesoresEncontrados)
                {
                    Profesores.Add(profesor);
                }
            }
            else
            {
                // Si no se encuentra ningún nombre en la búsqueda, mostrar todos los alumnos
                var todosLosProfesores = _databaseService.ObtenerTodosLosProfesores();
                Profesores.Clear();
                foreach (var profesor in todosLosProfesores)
                {
                    Profesores.Add(profesor);
                }
            }
        }

        private async void OnEliminarAlumnoClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var alumno = button.BindingContext as Alumno;
            if (alumno == null) return;

            bool confirmar = await DisplayAlert("Confirmar", $"¿Quieres eliminar al alumno {alumno.Nombre} {alumno.Apellido} y expulsarlo del Discord?", "Sí", "No");
            if (!confirmar) return;
            var profesor = SesionUsuario.Instancia.ProfesorLogueado;
            try
            {
                // Preparar JSON con el DiscordID para expulsar del servidor Discord vía Flask
                var dataToSend = new
                {
                    InstiID = profesor.InstiID,
                    discordId = alumno.DiscordID  // Asegúrate que tienes esta propiedad en Alumno
                };

                var jsonData = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync("http://localhost:5000/expulsar-usuario", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudo expulsar del Discord: {error}", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error conectando con servidor Discord: {ex.Message}", "OK");
                return;
            }

            // Si todo va bien, elimina de la base de datos local
            bool eliminado = _databaseService.EliminarAlumno(alumno.ID);
            if (eliminado)
            {
                Alumnos.Remove(alumno);
                await DisplayAlert("Éxito", "Alumno eliminado y expulsado del Discord correctamente.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "No se pudo eliminar el alumno localmente.", "OK");
            }
        }


        private async void OnEliminarProfesorClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var profesor = button.BindingContext as Profesor;
            if (profesor == null)
                return;

            bool confirmar = await DisplayAlert("Confirmar", $"¿Quieres eliminar al profesor {profesor.Nombre} {profesor.Apellido} y expulsarlo del Discord?", "Sí", "No");
            if (!confirmar)
                return;

            try
            {
                // Preparar JSON con DiscordID para expulsar al usuario vía Flask
                var dataToSend = new
                {
                    InstiID = profesor.InstiID,
                    discordId = profesor.DiscordID  // Asegúrate que Profesor tiene esta propiedad
                };

                var jsonData = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync("http://localhost:5000/expulsar-usuario", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudo expulsar del Discord: {error}", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error conectando con servidor Discord: {ex.Message}", "OK");
                return;
            }

            // Si la expulsión fue exitosa, eliminar localmente
            bool eliminado = _databaseService.EliminarProfesor(profesor.ID);
            if (eliminado)
            {
                Profesores.Remove(profesor);
                await DisplayAlert("Éxito", "Profesor eliminado y expulsado del Discord correctamente.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "No se pudo eliminar el profesor localmente.", "OK");
            }
        }

        private async void EliminarServidorDiscord(object sender, EventArgs e)
        {
            try
            {
                var profesor = SesionUsuario.Instancia.ProfesorLogueado;
                if (profesor == null)
                {
                    await DisplayAlert("Error", "No se encontró el profesor logueado.", "OK");
                    return;
                }

                bool confirmar = await DisplayAlert("Confirmar eliminación",
                    $"¿Quieres eliminar el servidor Discord asociado al instituto {profesor.InstiID}?",
                    "Sí", "No");
                if (!confirmar)
                    return;

                var dataToSend = new
                {
                    InstiID = profesor.InstiID
                };

                var jsonData = JsonConvert.SerializeObject(dataToSend);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync("http://localhost:5000/eliminar-servidor", content);

                if (response.IsSuccessStatusCode)
                {
                    // Si se eliminó en Discord, también eliminamos localmente el registro de la base de datos
                    bool eliminadoLocal = _databaseService.EliminarServidor(profesor.InstiID);
                    if (eliminadoLocal)
                    {
                        await DisplayAlert("Éxito", "Servidor Discord eliminado correctamente y registro local borrado.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Advertencia", "Servidor eliminado en Discord, pero no se pudo borrar el registro local.", "OK");
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error al eliminar el servidor: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error conectando con servidor: {ex.Message}", "OK");
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