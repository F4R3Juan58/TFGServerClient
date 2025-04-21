using Dapper;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TFGClient.Services
{
    public static class UpdateHelper
    {
        public static (string sqlSet, DynamicParameters parametros) GenerarUpdateDinamico<T>(T objeto, string idNombre = "ID", int idValor = 0)
        {
            var updates = new List<string>();
            var parametros = new DynamicParameters();

            parametros.Add($"@{idNombre}", idValor);

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                string nombre = prop.Name;
                object valor = prop.GetValue(objeto);

                if (nombre == idNombre) continue; // Evitar modificar la PK

                if (valor is string str && !string.IsNullOrWhiteSpace(str))
                {
                    updates.Add($"{nombre} = @{nombre}");
                    parametros.Add($"@{nombre}", str);
                }
                else if (valor is int i && i > 0)
                {
                    updates.Add($"{nombre} = @{nombre}");
                    parametros.Add($"@{nombre}", i);
                }
                else if (valor is bool b)
                {
                    updates.Add($"{nombre} = @{nombre}");
                    parametros.Add($"@{nombre}", b);
                }
                else if (valor is not null)
                {
                    updates.Add($"{nombre} = @{nombre}");
                    parametros.Add($"@{nombre}", valor);
                }
            }

            return (string.Join(", ", updates), parametros);
        }
    }
}
