using Microsoft.AspNetCore.Mvc;
using DteBackend.Data;
using DteBackend.Models;
using DteBackend.Utils;
using System.Globalization;

namespace DteBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private static bool TryParseFecha(string fechaStr, out DateTime fecha)
        {
            fecha = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(fechaStr))
                return false;

            string[] formatos = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(fechaStr.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                return true;

            return DateTime.TryParse(fechaStr, new CultureInfo("es-GT"), DateTimeStyles.None, out fecha);
        }

        // 1. Consultar Contribuyente (emitidas o recibidas en rango de fechas)
        [HttpGet("/ConsultaDatos")]
        [HttpGet("consultadatos")]
        public IActionResult ConsultarContribuyente(
            [FromQuery] string nit, 
            [FromQuery] string tipo, 
            [FromQuery] string fechaInicio, 
            [FromQuery] string fechaFin)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return BadRequest("El parámetro 'nit' es obligatorio.");

            tipo = (tipo ?? "emitidas").Trim().ToLower();

            DateTime fInicio = DateTime.MinValue;
            DateTime fFin = DateTime.MaxValue;

            if (TryParseFecha(fechaInicio, out var parsedInicio))
                fInicio = parsedInicio;

            if (TryParseFecha(fechaFin, out var parsedFin))
                fFin = parsedFin.Date.AddDays(1).AddTicks(-1);

            var query = DataStore.Facturas.Where(f => f.Aprobada);

            if (tipo == "recibidas")
                query = query.Where(f => f.NitReceptor == nit.Trim());
            else
                query = query.Where(f => f.NitEmisor == nit.Trim());

            var facturasFiltradas = query
                .Where(f => f.Fecha >= fInicio && f.Fecha <= fFin)
                .OrderBy(f => f.Fecha)
                .ToList();

            var gruposPorFecha = facturasFiltradas
                .GroupBy(f => f.Fecha.Date)
                .Select(g => new
                {
                    fecha = g.Key.ToString("dd/MM/yyyy"),
                    cantidadFacturas = g.Count(),
                    montoTotal = Math.Round(g.Sum(f => f.Total), 2),
                    facturas = g.Select(f => new
                    {
                        referencia = f.Referencia,
                        nitEmisor = f.NitEmisor,
                        nitReceptor = f.NitReceptor,
                        valor = f.Valor,
                        iva = f.Iva,
                        total = f.Total,
                        codigoAprobacion = f.CodigoAprobacion
                    }).ToList()
                })
                .ToList();

            decimal totalRango = Math.Round(facturasFiltradas.Sum(f => f.Total), 2);

            return Ok(new
            {
                nit = nit,
                tipo = tipo,
                fechaInicio = fechaInicio ?? "Inicio",
                fechaFin = fechaFin ?? "Fin",
                cantidadTotalFacturas = facturasFiltradas.Count,
                totalFacturadoRango = totalRango,
                detallePorFecha = gruposPorFecha
            });
        }

        // 2. Resumen de IVA por fecha y NIT
        [HttpGet("/ResumenIva")]
        [HttpGet("resumeniva")]
        public IActionResult ResumenIva([FromQuery] string fecha)
        {
            DateTime fechaFiltro = DateTime.Today;
            if (!TryParseFecha(fecha, out fechaFiltro))
            {
                fechaFiltro = DateTime.Today;
            }

            var facturasDelDia = DataStore.Facturas
                .Where(f => f.Aprobada && f.Fecha.Date == fechaFiltro.Date)
                .ToList();

            var nitsInvolucrados = facturasDelDia
                .Select(f => f.NitEmisor)
                .Union(facturasDelDia.Select(f => f.NitReceptor))
                .Distinct()
                .ToList();

            var resumenNits = new List<object>();

            foreach (var n in nitsInvolucrados)
            {
                var contrib = DataStore.Contribuyentes.FirstOrDefault(c => c.NIT == n);
                string nombre = contrib != null ? contrib.Nombre : "Contribuyente";

                decimal ivaEmitido = Math.Round(facturasDelDia.Where(f => f.NitEmisor == n).Sum(f => f.Iva), 2);
                decimal ivaRecibido = Math.Round(facturasDelDia.Where(f => f.NitReceptor == n).Sum(f => f.Iva), 2);

                resumenNits.Add(new
                {
                    nit = n,
                    nombre = nombre,
                    ivaEmitido = ivaEmitido,
                    ivaRecibido = ivaRecibido
                });
            }

            return Ok(new
            {
                fecha = fechaFiltro.ToString("dd/MM/yyyy"),
                cantidadMovimientos = facturasDelDia.Count,
                resumenNits = resumenNits
            });
        }

        // 3. Resumen por rango de fechas (Valores totales o sin IVA)
        [HttpGet("/ResumenRango")]
        [HttpGet("resumenrango")]
        public IActionResult ResumenRango(
            [FromQuery] string fechaInicio, 
            [FromQuery] string fechaFin, 
            [FromQuery] bool conIva = true)
        {
            DateTime fInicio = DateTime.MinValue;
            DateTime fFin = DateTime.MaxValue;

            if (TryParseFecha(fechaInicio, out var parsedInicio))
                fInicio = parsedInicio;

            if (TryParseFecha(fechaFin, out var parsedFin))
                fFin = parsedFin.Date.AddDays(1).AddTicks(-1);

            var facturasRango = DataStore.Facturas
                .Where(f => f.Aprobada && f.Fecha >= fInicio && f.Fecha <= fFin)
                .OrderBy(f => f.Fecha)
                .ToList();

            var agrupadas = facturasRango
                .GroupBy(f => f.Fecha.Date)
                .Select(g => new
                {
                    fecha = g.Key.ToString("dd/MM/yyyy"),
                    cantidadFacturas = g.Count(),
                    valorAutorizado = Math.Round(conIva ? g.Sum(f => f.Total) : g.Sum(f => f.Valor), 2)
                })
                .ToList();

            decimal totalAcumulado = Math.Round(conIva ? facturasRango.Sum(f => f.Total) : facturasRango.Sum(f => f.Valor), 2);

            return Ok(new
            {
                fechaInicio = fechaInicio ?? "Inicio",
                fechaFin = fechaFin ?? "Fin",
                conIva = conIva,
                modo = conIva ? "Valores Totales (Con IVA)" : "Valores sin IVA",
                totalAcumulado = totalAcumulado,
                resumenPorFecha = agrupadas
            });
        }

        // 4. Reporte detallado por rango de fechas (Página 3 Enunciado & Rúbrica HC)
        // Total facturas, listado de errores específicos, facturas sin errores, emisores y receptores
        [HttpGet("/ReporteRango")]
        [HttpGet("reporteerrores")]
        public IActionResult ReporteErroresRango(
            [FromQuery] string fechaInicio, 
            [FromQuery] string fechaFin)
        {
            DateTime fInicio = DateTime.MinValue;
            DateTime fFin = DateTime.MaxValue;

            if (TryParseFecha(fechaInicio, out var parsedInicio))
                fInicio = parsedInicio;

            if (TryParseFecha(fechaFin, out var parsedFin))
                fFin = parsedFin.Date.AddDays(1).AddTicks(-1);

            var facturasRango = DataStore.Facturas
                .Where(f => f.Fecha >= fInicio && f.Fecha <= fFin)
                .OrderBy(f => f.Fecha)
                .ToList();

            var agrupadas = facturasRango
                .GroupBy(f => f.Fecha.Date)
                .Select(g => new
                {
                    fecha = g.Key.ToString("dd/MM/yyyy"),
                    cantidadTotalFacturasRecibidas = g.Count(),
                    listadoErrores = new
                    {
                        nitEmisorInvalido = g.Count(f => f.Errores.Contains("NIT emisor inválido")),
                        nitReceptorInvalido = g.Count(f => f.Errores.Contains("NIT receptor inválido")),
                        nitEmisorInexistente = g.Count(f => f.Errores.Contains("NIT emisor inexistente")),
                        nitReceptorInexistente = g.Count(f => f.Errores.Contains("NIT receptor inexistente")),
                        ivaMalCalculado = g.Count(f => f.Errores.Contains("IVA mal calculado")),
                        totalMalCalculado = g.Count(f => f.Errores.Contains("TOTAL mal calculado")),
                        referenciaDuplicada = g.Count(f => f.Errores.Contains("REFERENCIA duplicada"))
                    },
                    cantidadFacturasSinErrores = g.Count(f => f.Aprobada),
                    cantidadEmisoresFacturas = g.Select(f => f.NitEmisor).Distinct().Count(),
                    cantidadReceptoresFacturas = g.Select(f => f.NitReceptor).Distinct().Count()
                })
                .ToList();

            return Ok(new
            {
                fechaInicio = fechaInicio ?? "Inicio",
                fechaFin = fechaFin ?? "Fin",
                reportePorFecha = agrupadas
            });
        }

        // 5. Reporte en PDF
        [HttpGet("pdf")]
        public IActionResult GenerarReportePdf()
        {
            var lineas = new List<string>
            {
                "REPORTE GENERAL DE DOCUMENTOS TRIBUTARIOS ELECTRONICOS (DTE)",
                $"Fecha de emision: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                "--------------------------------------------------------------------------------",
                "",
                "1. ESTADISTICAS GENERALES:",
                $"   Contribuyentes registrados en RTU: {DataStore.Contribuyentes.Count}",
                $"   Total facturas procesadas en historico: {DataStore.Facturas.Count}",
                $"   Facturas aprobadas: {DataStore.Facturas.Count(f => f.Aprobada)}",
                $"   Facturas rechazadas: {DataStore.Facturas.Count(f => !f.Aprobada)}",
                $"   Monto total autorizado (Con IVA): Q {DataStore.Facturas.Where(f => f.Aprobada).Sum(f => f.Total):N2}",
                $"   Monto total autorizado (Sin IVA): Q {DataStore.Facturas.Where(f => f.Aprobada).Sum(f => f.Valor):N2}",
                $"   Total IVA recaudado: Q {DataStore.Facturas.Where(f => f.Aprobada).Sum(f => f.Iva):N2}",
                "",
                "--------------------------------------------------------------------------------",
                "2. DETALLE DE ULTIMAS FACTURAS AUTORIZADAS:",
                "--------------------------------------------------------------------------------"
            };

            var ultimasAprobadas = DataStore.Facturas
                .Where(f => f.Aprobada)
                .OrderByDescending(f => f.Fecha)
                .Take(25)
                .ToList();

            if (ultimasAprobadas.Count == 0)
            {
                lineas.Add("   (No existen facturas autorizadas registradas actualmente)");
            }
            else
            {
                lineas.Add(string.Format("{0,-12} | {1,-10} | {2,-10} | {3,-10} | {4,-18}", 
                    "Fecha", "Emisor", "Receptor", "Total (Q)", "Autorizacion"));
                lineas.Add(new string('-', 76));

                foreach (var f in ultimasAprobadas)
                {
                    lineas.Add(string.Format("{0,-12} | {1,-10} | {2,-10} | {3,10:N2} | {4,-18}",
                        f.Fecha.ToString("dd/MM/yyyy"),
                        f.NitEmisor,
                        f.NitReceptor,
                        f.Total,
                        f.CodigoAprobacion));
                }
            }

            lineas.Add("");
            lineas.Add("--------------------------------------------------------------------------------");
            lineas.Add("Fin del reporte oficial - Superintendencia de Administracion Tributaria (SAT)");
            lineas.Add("--------------------------------------------------------------------------------");

            byte[] pdfBytes = PdfGenerator.GenerarPdf(
                "Superintendencia de Administracion Tributaria - SAT: Reporte DTE",
                lineas
            );

            return File(pdfBytes, "application/pdf", "Reporte_DTE_SAT.pdf");
        }
    }
}
