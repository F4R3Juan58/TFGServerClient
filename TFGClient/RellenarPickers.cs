using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Linq;
using TFGClient.Models;
using TFGClient.Services;

namespace TFGClient
{
    class RellenarPickers
    {
        private readonly DatabaseService dbService = new();

        public ObservableCollection<string> CargarNombreComunidades()
        {
            return new ObservableCollection<string>(dbService.ObtenerTodasLasComunidades().Select(c => c.Nombre));
        }

        public ObservableCollection<string> CargarLocalidades(int comunidadId)
        {
            var institutos = dbService.ObtenerTodosLosInstitutos().Where(i => i.ComunidadID == comunidadId);
            return new ObservableCollection<string>(institutos.Select(i => i.Localidad).Distinct());
        }

        public ObservableCollection<string> CargarNombreInstitutos(int comunidadId, string localidad)
        {
            var institutos = dbService.ObtenerTodosLosInstitutos()
                .Where(i => i.ComunidadID == comunidadId && i.Localidad == localidad);
            return new ObservableCollection<string>(institutos.Select(i => i.Nombre));
        }

        public ObservableCollection<string> CargarNiveles()
        {
            var cursos = dbService.ObtenerTodosLosCursos();
            return new ObservableCollection<string>(cursos.Select(c => c.Nivel).Distinct());
        }

        public ObservableCollection<string> CargarGrados()
        {
            var cursos = dbService.ObtenerTodosLosCursos();
            return new ObservableCollection<string>(cursos.Select(c => c.Grado).Distinct());
        }

        public ObservableCollection<string> CargarFamiliasPorNivel(string nivel)
        {
            var cursos = dbService.ObtenerTodosLosCursos()
                .Where(c => c.Nivel == nivel);
            return new ObservableCollection<string>(cursos.Select(c => c.FamiliaProfesional).Distinct());
        }

        public ObservableCollection<string> CargarCursos(string grado, string familia, string nivel)
        {
            var cursos = dbService.ObtenerTodosLosCursos()
                .Where(c => c.Grado == grado && c.FamiliaProfesional == familia && c.Nivel == nivel);
            return new ObservableCollection<string>(cursos.Select(c => c.Nombre));
        }
    }
}
