using System.Text.Json.Serialization;

namespace DteBackend.Models
{
    public class Contribuyente
    {
        [JsonPropertyName("NIT")]
        public string NIT { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }
    }
}
