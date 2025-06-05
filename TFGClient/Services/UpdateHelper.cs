using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TFGClient.Services
{
    public static class UpdateHelper
    {
        public static (string sqlSet, DynamicParameters parametros) GenerarUpdateDinamico<T>(T objeto, string idNombre = "ID", int idValor = 0)
        {
            var updates = new List<string>();
            var parametros = new DynamicParameters();

            // Añadir el parámetro del ID
            parametros.Add($"@{idNombre}", idValor);

            // Lista de propiedades a ignorar (ajustable según necesidades)
            var ignorar = new[] { idNombre, "Roles", "NombreCompleto" };

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                string nombre = prop.Name;
                object valor = prop.GetValue(objeto);

                // Si la propiedad está en la lista de propiedades a ignorar, la saltamos
                if (ignorar.Contains(nombre)) continue;

                // Solo actualizamos propiedades con valores válidos
                if (valor is string str && !string.IsNullOrWhiteSpace(str))
                {
                    updates.Add($"{nombre} = @{nombre}");
                    parametros.Add($"@{nombre}", str);
                }
                else if (valor is int i && i > 0) // Aseguramos que no se actualicen valores 0 o negativos
                {
                    updates.Add($"{nombre} = @{nombre}");
                    parametros.Add($"@{nombre}", i);
                }
                else if (valor is bool b)
                {
                    updates.Add($"{nombre} = @{nombre}");
                    parametros.Add($"@{nombre}", b);
                }
                else if (valor is not null) // Para otros tipos de datos
                {
                    updates.Add($"{nombre} = @{nombre}");
                    parametros.Add($"@{nombre}", valor);
                }
            }

            return (string.Join(", ", updates), parametros);
        }
    }
}
