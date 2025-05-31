namespace TFGClient
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("Login", typeof(Login));
            Routing.RegisterRoute("Registro", typeof(Registro));
            Routing.RegisterRoute("Profesor", typeof(Interfaz.PaginaProfesor));
            Routing.RegisterRoute("Tutor", typeof(Interfaz.Tutor));
            Routing.RegisterRoute("Administrador", typeof(Interfaz.Administrador));
            Routing.RegisterRoute("JefeDepartamento", typeof(Interfaz.JefeDepartamento));
        }


    }
}
