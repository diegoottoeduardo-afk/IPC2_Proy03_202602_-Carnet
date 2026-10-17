using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace DteFrontend.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public string TipoMensaje { get; set; } = "RTU";

        [BindProperty]
        public IFormFile ArchivoJson { get; set; }

        [BindProperty]
        public string ContenidoManualJson { get; set; }

        public string MensajeExito { get; set; }
        public string MensajeError { get; set; }
        public string RespuestaJsonCruda { get; set; }

        // Datos para mostrar resumen amigable de RTU
        public int? RtuNuevos { get; set; }
        public int? RtuActualizados { get; set; }
        public int? RtuInvalidos { get; set; }

        // Datos para mostrar resumen amigable de Facturas
        public int? FacturasAprobadas { get; set; }
        public int? FacturasRechazadasEmisor { get; set; }
        public int? FacturasRechazadasReceptor { get; set; }
        public List<DetalleAprobacionView> Aprobaciones { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string jsonAEnviar = string.Empty;

            // 1. Obtener contenido del archivo subido o del área de texto
            if (ArchivoJson != null && ArchivoJson.Length > 0)
            {
                using (var reader = new StreamReader(ArchivoJson.OpenReadStream(), Encoding.UTF8))
                {
                    jsonAEnviar = await reader.ReadToEndAsync();
                }
            }
            else if (!string.IsNullOrWhiteSpace(ContenidoManualJson))
            {
                jsonAEnviar = ContenidoManualJson.Trim();
            }
            else
            {
                MensajeError = "Por favor selecciona un archivo .json o ingresa el texto JSON en el área indicada.";
                return Page();
            }

            // Validar que sea un JSON con formato válido básico
            try
            {
                using var jsonDoc = JsonDocument.Parse(jsonAEnviar);
            }
            catch (Exception ex)
            {
                MensajeError = "El contenido proporcionado no es un JSON válido: " + ex.Message;
                return Page();
            }

            string backendUrl = _configuration["BackendUrl"] ?? "http://localhost:5025";
            string endpoint = TipoMensaje == "RTU" 
                ? $"{backendUrl}/api/Rtu/actualizar" 
                : $"{backendUrl}/api/Autorizaciones/procesar";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(jsonAEnviar, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(endpoint, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Formatear JSON de respuesta para lectura limpia
                    try
                    {
                        var obj = JsonSerializer.Deserialize<object>(responseBody);
                        RespuestaJsonCruda = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                    }
                    catch
                    {
                        RespuestaJsonCruda = responseBody;
                    }

                    // Parsear datos para la vista
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;

                    if (TipoMensaje == "RTU")
                    {
                        if (root.TryGetProperty("CONTRIBUYENTES_NUEVOS", out var pNuevos)) RtuNuevos = pNuevos.GetInt32();
                        if (root.TryGetProperty("CONTRIBUYENTES_ACTUALIZADOS", out var pAct)) RtuActualizados = pAct.GetInt32();
                        if (root.TryGetProperty("NITS_INVALIDOS", out var pInv)) RtuInvalidos = pInv.GetInt32();

                        MensajeExito = "¡Actualización de RTU procesada exitosamente!";
                    }
                    else
                    {
                        if (root.TryGetProperty("resultadoAutorizaciones", out var res))
                        {
                            if (res.TryGetProperty("cantidadAutorizacionesAprobadas", out var pApr)) FacturasAprobadas = pApr.GetInt32();
                            if (res.TryGetProperty("cantidadAutorizacionesRechazadasPorNitEmisorInexistente", out var pRechEm)) FacturasRechazadasEmisor = pRechEm.GetInt32();
                            if (res.TryGetProperty("cantidadAutorizacionesRechazadasPorNitReceptorInexistente", out var pRechRec)) FacturasRechazadasReceptor = pRechRec.GetInt32();

                            if (res.TryGetProperty("detalleAprobaciones", out var arr) && arr.ValueKind == JsonValueKind.Array)
                            {
                                Aprobaciones = new List<DetalleAprobacionView>();
                                foreach (var elem in arr.EnumerateArray())
                                {
                                    string referencia = elem.GetProperty("referencia").GetString();
                                    long codigo = elem.GetProperty("codigoAprobacion").GetInt64();
                                    Aprobaciones.Add(new DetalleAprobacionView { Referencia = referencia, CodigoAprobacion = codigo });
                                }
                            }
                        }

                        MensajeExito = "¡Solicitud de autorizaciones de facturas procesada exitosamente!";
                    }
                }
                else
                {
                    MensajeError = $"Error del servidor Backend ({response.StatusCode}): {responseBody}";
                }
            }
            catch (Exception ex)
            {
                MensajeError = "No se pudo conectar con el Backend (API). Verifica que esté en ejecución en " + backendUrl + ". Detalle: " + ex.Message;
            }

            return Page();
        }
    }

    public class DetalleAprobacionView
    {
        public string Referencia { get; set; }
        public long CodigoAprobacion { get; set; }
    }
}
