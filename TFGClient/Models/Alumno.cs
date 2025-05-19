using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFGClient.Models
{
    public class Alumno
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public int ComunidadID { get; set; }
        public int InstiID { get; set; }
        public int RolID { get; set; }
        public bool IsDelegado { get; set; }
        public int Puntos { get; set; }
        public int CursoID { get; set; }
        public string DiscordID { get; set; }

        public ObservableCollection<string> Roles { get; set; } = new ObservableCollection<string>();
    }
}

