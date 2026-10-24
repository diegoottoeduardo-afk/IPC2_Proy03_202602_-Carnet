using Microsoft.AspNetCore.Mvc;
using DteBackend.Data;

namespace DteBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SistemaController : ControllerBase
    {
        [HttpPost("inicializar")]
        [HttpPost("/Inicializar")]
        [HttpPost("/api/inicializar")]
        public IActionResult Inicializar()
        {
            DataStore.Reiniciar();
            return Ok(new { mensaje = "Sistema reseteado a su estado inicial." });
        }
    }
}
