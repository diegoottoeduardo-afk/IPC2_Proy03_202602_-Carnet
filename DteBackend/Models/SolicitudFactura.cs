namespace DteBackend.Models
{
    public class SolicitudFactura
    {
        public string TIEMPO { get; set; }
        public string REFERENCIA { get; set; }
        public string NIT_EMISOR { get; set; }
        public string NIT_RECEPTOR { get; set; }
        public decimal VALOR { get; set; }
        public decimal IVA { get; set; }
        public decimal TOTAL { get; set; }
    }
}