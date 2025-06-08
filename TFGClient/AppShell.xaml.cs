// AppShell.xaml.cs
using TFGClient.Services;

namespace TFGClient
{
    public partial class AppShell : Shell
    {
        private readonly DatabaseService _db = new();

        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("Login", typeof(Login));
            Routing.RegisterRoute("Registro", typeof(Registro));
            Routing.RegisterRoute("Profesor", typeof(Interfaz.PaginaProfesor));
            Routing.RegisterRoute("Tutor", typeof(Interfaz.Tutor));
            Routing.RegisterRoute("Administrador", typeof(Interfaz.Administrador));
            Routing.RegisterRoute("JefeDepartamento", typeof(Interfaz.JefeDepartamento));
            Routing.RegisterRoute("AñadirAdministrador", typeof(AñadirAdministrador));
        }

        public void cargar()
        {
            var profesor = SesionUsuario.Instancia.ProfesorLogueado;
            Login.IsVisible = false;
            Profesor.IsVisible = false;
            Tutor.IsVisible = false;
            JefeDepartamento.IsVisible = false;
            Administrador.IsVisible = false;

            if (_db.EsProfesor(profesor.Email))
            {
                Profesor.IsVisible = true;
            }
            if (profesor.IsTutor)
            {
                Tutor.IsVisible = true;
            }
            if (profesor.IsJefe)
            {
                JefeDepartamento.IsVisible = true;
            }
            if (_db.EsAdministrador(profesor.Email))
            {
                Administrador.IsVisible = true;
            }
        }
    }
}
