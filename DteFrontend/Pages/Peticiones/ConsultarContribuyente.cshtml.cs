using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace DteFrontend.Pages.Peticiones
{
    public class ConsultarContribuyenteModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ConsultarContribuyenteModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string Nit { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Tipo { get; set; } = "emitidas";

        [BindProperty(SupportsGet = true)]
        public string FechaInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FechaFin { get; set; }

        public bool HayBusqueda { get; set; }
        public string MensajeError { get; set; }

        public int CantidadTotalFacturas { get; set; }
        public decimal TotalFacturadoRango { get; set; }
        public List<GrupoFechaViewModel> DetallePorFecha { get; set; } = new List<GrupoFechaViewModel>();

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Nit))
                return;

            HayBusqueda = true;
            string backendUrl = _configuration["BackendUrl"] ?? "http://localhost:5025";
            string endpoint = $"{backendUrl}/api/Reportes/consultadatos?nit={Uri.EscapeDataString(Nit)}&tipo={Tipo}&fechaInicio={FechaInicio}&fechaFin={FechaFin}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    CantidadTotalFacturas = root.GetProperty("cantidadTotalFacturas").GetInt32();
                    TotalFacturadoRango = root.GetProperty("totalFacturadoRango").GetDecimal();

                    if (root.TryGetProperty("detallePorFecha", out var arr))
                    {
                        foreach (var g in arr.EnumerateArray())
                        {
                            var grupo = new GrupoFechaViewModel
                            {
                                Fecha = g.GetProperty("fecha").GetString(),
                                CantidadFacturas = g.GetProperty("cantidadFacturas").GetInt32(),
                                MontoTotal = g.GetProperty("montoTotal").GetDecimal()
                            };

                            if (g.TryGetProperty("facturas", out var fArr))
                            {
                                foreach (var fac in fArr.EnumerateArray())
                                {
                                    grupo.Facturas.Add(new FacturaDetalleViewModel
                                    {
                                        Referencia = fac.GetProperty("referencia").GetString(),
                                        NitEmisor = fac.GetProperty("nitEmisor").GetString(),
                                        NitReceptor = fac.GetProperty("nitReceptor").GetString(),
                                        Valor = fac.GetProperty("valor").GetDecimal(),
                                        Iva = fac.GetProperty("iva").GetDecimal(),
                                        Total = fac.GetProperty("total").GetDecimal(),
                                        CodigoAprobacion = fac.GetProperty("codigoAprobacion").GetInt64()
                                    });
                                }
                            }

                            DetallePorFecha.Add(grupo);
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

    public class GrupoFechaViewModel
    {
        public string Fecha { get; set; }
        public int CantidadFacturas { get; set; }
        public decimal MontoTotal { get; set; }
        public List<FacturaDetalleViewModel> Facturas { get; set; } = new List<FacturaDetalleViewModel>();
    }

    public class FacturaDetalleViewModel
    {
        public string Referencia { get; set; }
        public string NitEmisor { get; set; }
        public string NitReceptor { get; set; }
        public decimal Valor { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public long CodigoAprobacion { get; set; }
    }
}
