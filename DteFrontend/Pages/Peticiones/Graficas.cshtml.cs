using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace DteFrontend.Pages.Peticiones
{
    public class GraficasModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public GraficasModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // Filtro Gráfica 1 (IVA)
        [BindProperty(SupportsGet = true)]
        public string FechaIva { get; set; }

        // Filtro Gráfica 2 (Rango)
        [BindProperty(SupportsGet = true)]
        public string FechaInicioRango { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FechaFinRango { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ConIva { get; set; } = true;

        // Datos serializados para Chart.js
        public string LabelsIvaJson { get; set; } = "[]";
        public string DataIvaEmitidoJson { get; set; } = "[]";
        public string DataIvaRecibidoJson { get; set; } = "[]";

        public string LabelsRangoJson { get; set; } = "[]";
        public string DataValoresRangoJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(FechaIva))
                FechaIva = DateTime.Today.ToString("yyyy-MM-dd");

            if (string.IsNullOrWhiteSpace(FechaInicioRango) && string.IsNullOrWhiteSpace(FechaFinRango))
            {
                FechaInicioRango = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
                FechaFinRango = DateTime.Today.ToString("yyyy-MM-dd");
            }

            string backendUrl = _configuration["BackendUrl"] ?? "http://localhost:5025";
            var client = _httpClientFactory.CreateClient();

            // 1. Cargar datos para Gráfica de IVA
            try
            {
                string endpointIva = $"{backendUrl}/api/Reportes/resumeniva?fecha={FechaIva}";
                var respIva = await client.GetAsync(endpointIva);
                if (respIva.IsSuccessStatusCode)
                {
                    string json = await respIva.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var labels = new List<string>();
                    var ivaEmitido = new List<decimal>();
                    var ivaRecibido = new List<decimal>();

                    if (root.TryGetProperty("resumenNits", out var arr))
                    {
                        foreach (var elem in arr.EnumerateArray())
                        {
                            labels.Add(elem.GetProperty("nit").GetString());
                            ivaEmitido.Add(elem.GetProperty("ivaEmitido").GetDecimal());
                            ivaRecibido.Add(elem.GetProperty("ivaRecibido").GetDecimal());
                        }
                    }

                    LabelsIvaJson = JsonSerializer.Serialize(labels);
                    DataIvaEmitidoJson = JsonSerializer.Serialize(ivaEmitido);
                    DataIvaRecibidoJson = JsonSerializer.Serialize(ivaRecibido);
                }
            }
            catch { }

            // 2. Cargar datos para Gráfica de Rango
            try
            {
                string endpointRango = $"{backendUrl}/api/Reportes/resumenrango?fechaInicio={FechaInicioRango}&fechaFin={FechaFinRango}&conIva={ConIva}";
                var respRango = await client.GetAsync(endpointRango);
                if (respRango.IsSuccessStatusCode)
                {
                    string json = await respRango.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var labels = new List<string>();
                    var valores = new List<decimal>();

                    if (root.TryGetProperty("resumenPorFecha", out var arr))
                    {
                        foreach (var elem in arr.EnumerateArray())
                        {
                            labels.Add(elem.GetProperty("fecha").GetString());
                            valores.Add(elem.GetProperty("valorAutorizado").GetDecimal());
                        }
                    }

                    LabelsRangoJson = JsonSerializer.Serialize(labels);
                    DataValoresRangoJson = JsonSerializer.Serialize(valores);
                }
            }
            catch { }
        }
    }
}
