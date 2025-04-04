using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFGClient.Models
{
    public class Profesor
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Contraseña { get; set; }
        public string Email { get; set; }
        public int ComunidadID { get; set; }
        public int InstiID { get; set; }
        public int RolID { get; set; }
        public bool IsJefe { get; set; }
        public bool IsTutor { get; set; }
        public int CursoID { get; set; }
        public string DiscordID { get; set; }
    }
}

