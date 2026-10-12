namespace DteBackend.Models
{
    public class RespuestaAutorizacion
    {
        public ContenidoRespuesta resultadoAutorizaciones { get; set; } = new ContenidoRespuesta();
    }

    public class ContenidoRespuesta
    {
        public int cantidadAutorizacionesAprobadas { get; set; }
        public int cantidadAutorizacionesRechazadasPorNitEmisorInexistente { get; set; }
        public int cantidadAutorizacionesRechazadasPorNitReceptorInexistente { get; set; }
        public List<Aprobacion> detalleAprobaciones { get; set; } = new List<Aprobacion>();
    }

    public class Aprobacion
    {
        public string referencia { get; set; }
        public long codigoAprobacion { get; set; }
    }
}