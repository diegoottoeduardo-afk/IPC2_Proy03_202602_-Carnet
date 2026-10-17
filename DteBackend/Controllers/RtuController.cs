using Microsoft.AspNetCore.Mvc;
using DteBackend.Models;
using DteBackend.Data;
using DteBackend.Utils;

namespace DteBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RtuController : ControllerBase
    {
        [HttpPost("actualizar")]
        public IActionResult ActualizarRtu([FromBody] List<Contribuyente> nuevosContribuyentes)
        {
            if (nuevosContribuyentes == null || nuevosContribuyentes.Count == 0)
                return BadRequest("El archivo JSON está vacío o es inválido.");

            int nuevos = 0;
            int actualizados = 0;
            int invalidos = 0;

            foreach (var contribuyente in nuevosContribuyentes)
            {
                if (!NitValidator.EsNitValido(contribuyente.NIT))
                {
                    invalidos++;
                    continue;
                }

                var existente = DataStore.Contribuyentes.FirstOrDefault(c => c.NIT == contribuyente.NIT);

                if (existente != null)
                {
                    existente.Nombre = contribuyente.Nombre;
                    actualizados++;
                }
                else
                {
                    DataStore.Contribuyentes.Add(contribuyente);
                    nuevos++;
                }
            }

            DataStore.GuardarRtu();

            return Ok(new
            {
                CONTRIBUYENTES_NUEVOS = nuevos,
                CONTRIBUYENTES_ACTUALIZADOS = actualizados,
                NITS_INVALIDOS = invalidos
            });
        }
    }
}
