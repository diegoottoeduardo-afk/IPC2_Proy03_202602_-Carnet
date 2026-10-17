namespace DteBackend.Models
{
    public class FacturaGuardada
    {
        public DateTime Fecha { get; set; }
        public string TiempoTexto { get; set; }
        public string Referencia { get; set; }
        public string NitEmisor { get; set; }
        public string NitReceptor { get; set; }
        public decimal Valor { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public long CodigoAprobacion { get; set; }
        public bool Aprobada { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
    }
}
