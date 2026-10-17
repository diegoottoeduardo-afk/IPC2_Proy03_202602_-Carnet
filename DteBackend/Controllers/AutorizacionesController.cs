using Microsoft.AspNetCore.Mvc;
using DteBackend.Models;
using DteBackend.Data;
using DteBackend.Utils;
using System.Text.RegularExpressions;

namespace DteBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutorizacionesController : ControllerBase
    {
        [HttpPost("procesar")]
        public IActionResult ProcesarFacturas([FromBody] List<SolicitudFactura> solicitudes)
        {
            if (solicitudes == null || solicitudes.Count == 0)
            {
                return BadRequest("No se recibieron datos en la solicitud.");
            }

            var respuesta = new RespuestaAutorizacion();

            int aprobadas = 0;
            int rechazosEmisor = 0;
            int rechazosReceptor = 0;

            foreach (var sol in solicitudes)
            {
                var nuevaFactura = new FacturaGuardada
                {
                    TiempoTexto = sol.TIEMPO,
                    Referencia = sol.REFERENCIA,
                    NitEmisor = sol.NIT_EMISOR,
                    NitReceptor = sol.NIT_RECEPTOR,
                    Valor = sol.VALOR,
                    Iva = sol.IVA,
                    Total = sol.TOTAL,
                    Aprobada = true
                };

                // Extraer fecha y hora usando Regex: dd/mm/yyyy hh24:mi
                var regex = new Regex(@"(\d{2})/(\d{2})/(\d{4})\s+(\d{2}):(\d{2})");
                var match = regex.Match(sol.TIEMPO ?? "");

                if (match.Success)
                {
                    int dia = int.Parse(match.Groups[1].Value);
                    int mes = int.Parse(match.Groups[2].Value);
                    int anio = int.Parse(match.Groups[3].Value);
                    int hora = int.Parse(match.Groups[4].Value);
                    int minuto = int.Parse(match.Groups[5].Value);

                    try
                    {
                        nuevaFactura.Fecha = new DateTime(anio, mes, dia, hora, minuto, 0);
                    }
                    catch
                    {
                        nuevaFactura.Fecha = DateTime.Today;
                        nuevaFactura.Aprobada = false;
                        nuevaFactura.Errores.Add("Fecha inválida");
                    }
                }
                else
                {
                    nuevaFactura.Fecha = DateTime.Today;
                    nuevaFactura.Aprobada = false;
                    nuevaFactura.Errores.Add("Formato de fecha inválido");
                }

                // Validar referencia (máximo 40 posiciones y única)
                if (string.IsNullOrWhiteSpace(sol.REFERENCIA) || sol.REFERENCIA.Length > 40)
                {
                    nuevaFactura.Aprobada = false;
                    nuevaFactura.Errores.Add("REFERENCIA inválida");
                }
                else
                {
                    bool referenciaRepetida = false;
                    for (int i = 0; i < DataStore.Facturas.Count; i++)
                    {
                        if (DataStore.Facturas[i].Referencia == sol.REFERENCIA && DataStore.Facturas[i].Aprobada)
                        {
                            referenciaRepetida = true;
                            break;
                        }
                    }

                    if (referenciaRepetida)
                    {
                        nuevaFactura.Aprobada = false;
                        nuevaFactura.Errores.Add("REFERENCIA duplicada");
                    }
                }

                // Validar algoritmo del NIT emisor y receptor
                if (!NitValidator.EsNitValido(sol.NIT_EMISOR))
                {
                    nuevaFactura.Aprobada = false;
                    nuevaFactura.Errores.Add("NIT emisor inválido");
                }

                if (!NitValidator.EsNitValido(sol.NIT_RECEPTOR))
                {
                    nuevaFactura.Aprobada = false;
                    nuevaFactura.Errores.Add("NIT receptor inválido");
                }

                // Validar que existan en el RTU
                bool emisorExiste = false;
                for (int i = 0; i < DataStore.Contribuyentes.Count; i++)
                {
                    if (DataStore.Contribuyentes[i].NIT == sol.NIT_EMISOR)
                    {
                        emisorExiste = true;
                        break;
                    }
                }
                if (!emisorExiste)
                {
                    nuevaFactura.Aprobada = false;
                    nuevaFactura.Errores.Add("NIT emisor inexistente");
                    rechazosEmisor++;
                }

                bool receptorExiste = false;
                for (int i = 0; i < DataStore.Contribuyentes.Count; i++)
                {
                    if (DataStore.Contribuyentes[i].NIT == sol.NIT_RECEPTOR)
                    {
                        receptorExiste = true;
                        break;
                    }
                }
                if (!receptorExiste)
                {
                    nuevaFactura.Aprobada = false;
                    nuevaFactura.Errores.Add("NIT receptor inexistente");
                    rechazosReceptor++;
                }

                // Validar cálculos (IVA = Redondear(Valor * 0.12, 2))
                decimal ivaCalculado = Math.Round(sol.VALOR * 0.12m, 2, MidpointRounding.AwayFromZero);
                if (sol.IVA != ivaCalculado)
                {
                    nuevaFactura.Aprobada = false;
                    nuevaFactura.Errores.Add("IVA mal calculado");
                }

                decimal totalCalculado = sol.VALOR + sol.IVA;
                if (sol.TOTAL != totalCalculado)
                {
                    nuevaFactura.Aprobada = false;
                    nuevaFactura.Errores.Add("TOTAL mal calculado");
                }

                // Si todo estuvo correcto, generamos el código de autorización
                if (nuevaFactura.Aprobada)
                {
                    aprobadas++;

                    int correlativo = 1;
                    for (int i = 0; i < DataStore.Facturas.Count; i++)
                    {
                        if (DataStore.Facturas[i].Aprobada && DataStore.Facturas[i].Fecha.Date == nuevaFactura.Fecha.Date)
                        {
                            correlativo++;
                        }
                    }

                    // Formato: yyyymmdd########
                    string codigoStr = nuevaFactura.Fecha.ToString("yyyyMMdd") + correlativo.ToString("D8");
                    nuevaFactura.CodigoAprobacion = long.Parse(codigoStr);

                    respuesta.resultadoAutorizaciones.detalleAprobaciones.Add(new Aprobacion
                    {
                        referencia = nuevaFactura.Referencia,
                        codigoAprobacion = nuevaFactura.CodigoAprobacion
                    });
                }

                DataStore.Facturas.Add(nuevaFactura);
            }

            DataStore.GuardarFacturas();

            respuesta.resultadoAutorizaciones.cantidadAutorizacionesAprobadas = aprobadas;
            respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorNitEmisorInexistente = rechazosEmisor;
            respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorNitReceptorInexistente = rechazosReceptor;

            return Ok(respuesta);
        }
    }
}
