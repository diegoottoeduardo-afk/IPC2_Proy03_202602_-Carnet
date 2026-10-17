using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace DteFrontend.Pages
{
    public class InicializarModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public InicializarModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public string MensajeExito { get; set; }
        public string MensajeError { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string backendUrl = _configuration["BackendUrl"] ?? "http://localhost:5025";
            string endpoint = $"{backendUrl}/api/Sistema/inicializar";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(endpoint, null);

                if (response.IsSuccessStatusCode)
                {
                    MensajeExito = "El sistema ha sido reiniciado a su estado inicial. Se han eliminado todos los contribuyentes y facturas almacenadas.";
                }
                else
                {
                    MensajeError = $"El servidor respondió con error: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                MensajeError = "No se pudo conectar con la API. Detalle: " + ex.Message;
            }

            return Page();
        }
    }
}
