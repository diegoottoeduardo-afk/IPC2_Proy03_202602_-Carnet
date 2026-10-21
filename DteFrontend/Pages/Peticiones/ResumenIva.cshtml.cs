using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace DteFrontend.Pages.Peticiones
{
    public class ResumenIvaModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ResumenIvaModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string Fecha { get; set; }

        public bool HayBusqueda { get; set; }
        public string MensajeError { get; set; }

        public string FechaMostrada { get; set; }
        public decimal TotalIvaEmitido { get; set; }
        public decimal TotalIvaRecibido { get; set; }
        public List<ResumenNitViewModel> Movimientos { get; set; } = new List<ResumenNitViewModel>();

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Fecha))
            {
                // Por defecto hoy
                Fecha = DateTime.Today.ToString("yyyy-MM-dd");
            }

            HayBusqueda = true;
            string backendUrl = _configuration["BackendUrl"] ?? "http://localhost:5025";
            string endpoint = $"{backendUrl}/api/Reportes/resumeniva?fecha={Fecha}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    FechaMostrada = root.GetProperty("fecha").GetString();

                    if (root.TryGetProperty("resumenNits", out var arr))
                    {
                        foreach (var elem in arr.EnumerateArray())
                        {
                            var nitItem = new ResumenNitViewModel
                            {
                                Nit = elem.GetProperty("nit").GetString(),
                                Nombre = elem.GetProperty("nombre").GetString(),
                                IvaEmitido = elem.GetProperty("ivaEmitido").GetDecimal(),
                                IvaRecibido = elem.GetProperty("ivaRecibido").GetDecimal()
                            };

                            TotalIvaEmitido += nitItem.IvaEmitido;
                            TotalIvaRecibido += nitItem.IvaRecibido;
                            Movimientos.Add(nitItem);
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

    public class ResumenNitViewModel
    {
        public string Nit { get; set; }
        public string Nombre { get; set; }
        public decimal IvaEmitido { get; set; }
        public decimal IvaRecibido { get; set; }
    }
}
