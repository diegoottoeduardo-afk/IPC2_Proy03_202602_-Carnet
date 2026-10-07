using DteBackend.Models;
using System.Text.Json;

namespace DteBackend.Data
{
    public class DataStore
    {
        private static DataStore _instancia;
        private readonly string _rtuFilePath = "rtu.json";

        public List<Contribuyente> Contribuyentes { get; private set; }

        // Constructor privado para el Singleton
        private DataStore()
        {
            Contribuyentes = new List<Contribuyente>();
            CargarRtu();
        }

        public static DataStore Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    _instancia = new DataStore();
                }
                return _instancia;
            }
        }

        private void CargarRtu()
        {
            if (File.Exists(_rtuFilePath))
            {
                var json = File.ReadAllText(_rtuFilePath);
                Contribuyentes = JsonSerializer.Deserialize<List<Contribuyente>>(json) ?? new List<Contribuyente>();
            }
        }

        public void GuardarRtu()
        {
            var json = JsonSerializer.Serialize(Contribuyentes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_rtuFilePath, json);
        }

        public void LimpiarDatos()
        {
            Contribuyentes.Clear();
            GuardarRtu();
            // TODO: En el Release 2, limpiaremos también las facturas
        }
    }
}