using System.Collections.ObjectModel;

namespace TFGClient
{
    class RellenarPickers
    {
        private readonly DatabaseService dbService = new();

        public ObservableCollection<Comunidad> CargarComunidades() => dbService.ObtenerComunidades();

        public ObservableCollection<Localidad> CargarLocalidades(int comunidadId) => dbService.ObtenerLocalidadesPorComunidad(comunidadId);

        public ObservableCollection<Instituto> CargarInstitutos(int comunidadId, string localidad) => dbService.ObtenerInstitutosPorLocalidad(comunidadId, localidad);
    }

}
