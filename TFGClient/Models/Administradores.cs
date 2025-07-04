﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFGClient.Models
{
    public class Administradores
    {
        public int ID { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public int InstiID { get; set; }
        public int RolID { get; set; }
        public string DiscordID { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}";

        public ObservableCollection<string> Roles { get; set; } = new ObservableCollection<string>();

    }
}

