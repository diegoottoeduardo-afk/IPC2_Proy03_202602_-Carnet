using DteBackend.Models;
using System.Text.Json;

namespace DteBackend.Data
{
    public static class DataStore
    {
        public static List<Contribuyente> Contribuyentes = new List<Contribuyente>();
        public static List<FacturaGuardada> Facturas = new List<FacturaGuardada>();

        private static string archivoRtu = "rtu.json";
        private static string archivoFacturas = "facturas.json";

        static DataStore()
        {
            CargarDatos();
        }

        public static void CargarDatos()
        {
            if (File.Exists(archivoRtu))
            {
                string json = File.ReadAllText(archivoRtu);
                Contribuyentes = JsonSerializer.Deserialize<List<Contribuyente>>(json) ?? new List<Contribuyente>();
            }

            if (File.Exists(archivoFacturas))
            {
                string json = File.ReadAllText(archivoFacturas);
                Facturas = JsonSerializer.Deserialize<List<FacturaGuardada>>(json) ?? new List<FacturaGuardada>();
            }
        }

        public static void GuardarRtu()
        {
            string json = JsonSerializer.Serialize(Contribuyentes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(archivoRtu, json);
        }

        public static void GuardarFacturas()
        {
            string json = JsonSerializer.Serialize(Facturas, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(archivoFacturas, json);
        }

        public static void Reiniciar()
        {
            Contribuyentes.Clear();
            Facturas.Clear();
            GuardarRtu();
            GuardarFacturas();
        }
    }
}