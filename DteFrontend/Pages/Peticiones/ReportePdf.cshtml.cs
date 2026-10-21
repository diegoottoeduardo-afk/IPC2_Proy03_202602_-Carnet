using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace DteFrontend.Pages.Peticiones
{
    public class ReportePdfModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ReportePdfModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public string BackendUrl { get; set; }
        public int TotalContribuyentes { get; set; }
        public int FacturasAprobadas { get; set; }
        public int FacturasRechazadas { get; set; }
        public decimal MontoTotalFacturado { get; set; }
        public decimal TotalIva { get; set; }

        public List<FacturaItemModel> FacturasRecientes { get; set; } = new List<FacturaItemModel>();

        public async Task OnGetAsync()
        {
            BackendUrl = _configuration["BackendUrl"] ?? "http://localhost:5025";
            string endpoint = $"{BackendUrl}/api/Reportes/consultadatos?nit=TODOS&tipo=emitidas";

            // Consultar datos de resumen
            try
            {
                var client = _httpClientFactory.CreateClient();
                // Obtener rango general de facturas
                string urlRango = $"{BackendUrl}/api/Reportes/resumenrango?conIva=true";
                var respRango = await client.GetAsync(urlRango);
                if (respRango.IsSuccessStatusCode)
                {
                    string json = await respRango.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    MontoTotalFacturado = doc.RootElement.GetProperty("totalAcumulado").GetDecimal();
                }
            }
            catch { }
        }
    }

    public class FacturaItemModel
    {
        public string Fecha { get; set; }
        public string Referencia { get; set; }
        public string NitEmisor { get; set; }
        public string NitReceptor { get; set; }
        public decimal Total { get; set; }
        public long CodigoAprobacion { get; set; }
    }
}
