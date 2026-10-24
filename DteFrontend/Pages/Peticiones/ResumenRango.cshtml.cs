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

        // Desglose de Errores por Rango (Página 3 Enunciado / Rúbrica HC)
        public List<DetalleErroresFechaViewModel> ReporteErrores { get; set; } = new List<DetalleErroresFechaViewModel>();

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(FechaInicio) && string.IsNullOrWhiteSpace(FechaFin))
            {
                FechaInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
                FechaFin = DateTime.Today.ToString("yyyy-MM-dd");
            }

            HayBusqueda = true;
            string backendUrl = _configuration["BackendUrl"] ?? "http://localhost:5025";
            var client = _httpClientFactory.CreateClient();

            // 1. Resumen de Valores (con o sin IVA)
            try
            {
                string endpoint = $"{backendUrl}/api/Reportes/resumenrango?fechaInicio={FechaInicio}&fechaFin={FechaFin}&conIva={ConIva}";
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
            }
            catch { }

            // 2. Reporte de Errores Detallado (Página 3 Enunciado)
            try
            {
                string endpointErr = $"{backendUrl}/api/Reportes/reporteerrores?fechaInicio={FechaInicio}&fechaFin={FechaFin}";
                var responseErr = await client.GetAsync(endpointErr);

                if (responseErr.IsSuccessStatusCode)
                {
                    string jsonErr = await responseErr.Content.ReadAsStringAsync();
                    using var docErr = JsonDocument.Parse(jsonErr);
                    var rootErr = docErr.RootElement;

                    if (rootErr.TryGetProperty("reportePorFecha", out var arrErr))
                    {
                        foreach (var item in arrErr.EnumerateArray())
                        {
                            var errObj = item.GetProperty("listadoErrores");
                            ReporteErrores.Add(new DetalleErroresFechaViewModel
                            {
                                Fecha = item.GetProperty("fecha").GetString(),
                                TotalRecibidas = item.GetProperty("cantidadTotalFacturasRecibidas").GetInt32(),
                                NitEmisorInvalido = errObj.GetProperty("nitEmisorInvalido").GetInt32(),
                                NitReceptorInvalido = errObj.GetProperty("nitReceptorInvalido").GetInt32(),
                                NitEmisorInexistente = errObj.GetProperty("nitEmisorInexistente").GetInt32(),
                                NitReceptorInexistente = errObj.GetProperty("nitReceptorInexistente").GetInt32(),
                                IvaMalCalculado = errObj.GetProperty("ivaMalCalculado").GetInt32(),
                                TotalMalCalculado = errObj.GetProperty("totalMalCalculado").GetInt32(),
                                ReferenciaDuplicada = errObj.GetProperty("referenciaDuplicada").GetInt32(),
                                FacturasSinErrores = item.GetProperty("cantidadFacturasSinErrores").GetInt32(),
                                CantidadEmisores = item.GetProperty("cantidadEmisoresFacturas").GetInt32(),
                                CantidadReceptores = item.GetProperty("cantidadReceptoresFacturas").GetInt32()
                            });
                        }
                    }
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

    public class DetalleErroresFechaViewModel
    {
        public string Fecha { get; set; }
        public int TotalRecibidas { get; set; }
        public int NitEmisorInvalido { get; set; }
        public int NitReceptorInvalido { get; set; }
        public int NitEmisorInexistente { get; set; }
        public int NitReceptorInexistente { get; set; }
        public int IvaMalCalculado { get; set; }
        public int TotalMalCalculado { get; set; }
        public int ReferenciaDuplicada { get; set; }
        public int FacturasSinErrores { get; set; }
        public int CantidadEmisores { get; set; }
        public int CantidadReceptores { get; set; }
    }
}
