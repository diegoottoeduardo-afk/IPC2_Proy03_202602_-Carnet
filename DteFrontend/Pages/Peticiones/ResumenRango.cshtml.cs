using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace DteFrontend.Pages.Peticiones
{
    public class ResumenRangoModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ResumenRangoModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string FechaInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FechaFin { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ConIva { get; set; } = true;

        public bool HayBusqueda { get; set; }
        public string MensajeError { get; set; }

        public string ModoTexto { get; set; }
        public decimal TotalAcumulado { get; set; }
        public List<ResumenRangoFechaViewModel> DetallePorFecha { get; set; } = new List<ResumenRangoFechaViewModel>();

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(FechaInicio) && string.IsNullOrWhiteSpace(FechaFin))
            {
                // Establecer primer día del mes actual y hoy por defecto
                FechaInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
                FechaFin = DateTime.Today.ToString("yyyy-MM-dd");
            }

            HayBusqueda = true;
            string backendUrl = _configuration["BackendUrl"] ?? "http://localhost:5025";
            string endpoint = $"{backendUrl}/api/Reportes/resumenrango?fechaInicio={FechaInicio}&fechaFin={FechaFin}&conIva={ConIva}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    ModoTexto = root.GetProperty("modo").GetString();
                    TotalAcumulado = root.GetProperty("totalAcumulado").GetDecimal();

                    if (root.TryGetProperty("resumenPorFecha", out var arr))
                    {
                        foreach (var elem in arr.EnumerateArray())
                        {
                            DetallePorFecha.Add(new ResumenRangoFechaViewModel
                            {
                                Fecha = elem.GetProperty("fecha").GetString(),
                                CantidadFacturas = elem.GetProperty("cantidadFacturas").GetInt32(),
                                ValorAutorizado = elem.GetProperty("valorAutorizado").GetDecimal()
                            });
                        }
                    }
                }
                else
                {
                    MensajeError = $"Error del servidor: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                MensajeError = "No se pudo conectar con el Backend: " + ex.Message;
            }
        }
    }

    public class ResumenRangoFechaViewModel
    {
        public string Fecha { get; set; }
        public int CantidadFacturas { get; set; }
        public decimal ValorAutorizado { get; set; }
    }
}
