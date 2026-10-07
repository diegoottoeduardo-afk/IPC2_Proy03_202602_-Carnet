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
                return BadRequest("El JSON está vacío o es inválido.");

            int nuevos = 0;
            int actualizados = 0;
            int invalidos = 0;

            var dataStore = DataStore.Instancia;

            foreach (var contribuyente in nuevosContribuyentes)
            {
                // 1. Validar el NIT según nuestra clase Utils
                if (!NitValidator.EsNitValido(contribuyente.NIT))
                {
                    invalidos++;
                    continue; // NIT inválido: ignora y pasa al siguiente
                }

                // 2. Buscar si el NIT ya existe
                var existente = dataStore.Contribuyentes.FirstOrDefault(c => c.NIT == contribuyente.NIT);

                if (existente != null)
                {
                    // Actualizar el nombre del existente
                    existente.Nombre = contribuyente.Nombre;
                    actualizados++;
                }
                else
                {
                    // Es válido y no existe, se crea
                    dataStore.Contribuyentes.Add(contribuyente);
                    nuevos++;
                }
            }

            // 3. Persistir en el archivo físico
            dataStore.GuardarRtu();

            // 4. Armar la respuesta exacta que pide el enunciado
            var respuesta = new
            {
                CONTRIBUYENTES_NUEVOS = nuevos,
                CONTRIBUYENTES_ACTUALIZADOS = actualizados,
                NITS_INVALIDOS = invalidos
            };

            return Ok(respuesta);
        }
    }
}
