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

            if (!string.IsNullOrWhiteSpace(fechaInicio))
                DateTime.TryParse(fechaInicio, out fInicio);

            if (!string.IsNullOrWhiteSpace(fechaFin))
            {
                if (DateTime.TryParse(fechaFin, out fFin))
                    fFin = fFin.Date.AddDays(1).AddTicks(-1);
            }

            // Filtrar facturas aprobadas
            var query = DataStore.Facturas.Where(f => f.Aprobada);

            if (tipo == "recibidas")
            {
                query = query.Where(f => f.NitReceptor == nit.Trim());
            }
            else
            {
                query = query.Where(f => f.NitEmisor == nit.Trim());
            }

            var facturasFiltradas = query
                .Where(f => f.Fecha >= fInicio && f.Fecha <= fFin)
                .OrderBy(f => f.Fecha)
                .ToList();

            // Agrupar por fecha (dd/MM/yyyy)
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
            if (!string.IsNullOrWhiteSpace(fecha))
            {
                if (!DateTime.TryParse(fecha, out fechaFiltro))
                {
                    DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaFiltro);
                }
            }

            var facturasDelDia = DataStore.Facturas
                .Where(f => f.Aprobada && f.Fecha.Date == fechaFiltro.Date)
                .ToList();

            // Obtener todos los NITs con movimientos en esa fecha
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

            if (!string.IsNullOrWhiteSpace(fechaInicio))
                DateTime.TryParse(fechaInicio, out fInicio);

            if (!string.IsNullOrWhiteSpace(fechaFin))
            {
                if (DateTime.TryParse(fechaFin, out fFin))
                    fFin = fFin.Date.AddDays(1).AddTicks(-1);
            }

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

        // 4. Reporte en PDF
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
                $"   Facturas autorizadas (Aprobadas): {DataStore.Facturas.Count(f => f.Aprobada)}",
                $"   Facturas rechazadas: {DataStore.Facturas.Count(f => !f.Aprobada)}",
                $"   Monto total facturado (con IVA): Q {DataStore.Facturas.Where(f => f.Aprobada).Sum(f => f.Total):N2}",
                $"   Total IVA recaudado: Q {DataStore.Facturas.Where(f => f.Aprobada).Sum(f => f.Iva):N2}",
                "",
                "--------------------------------------------------------------------------------",
                "2. DETALLE DE FACTURAS AUTORIZADAS RECIENTES:",
                "Fecha       | Referencia | Emisor    | Receptor  | Total (Q)   | No. Autorizacion",
                "--------------------------------------------------------------------------------"
            };

            var aprobadas = DataStore.Facturas.Where(f => f.Aprobada).OrderByDescending(f => f.Fecha).Take(25).ToList();
            if (aprobadas.Count == 0)
            {
                lineas.Add("No hay facturas aprobadas en el sistema.");
            }
            else
            {
                foreach (var fac in aprobadas)
                {
                    string fStr = fac.Fecha.ToString("dd/MM/yyyy");
                    string refStr = (fac.Referencia ?? "").PadRight(10).Substring(0, 10);
                    string emStr = (fac.NitEmisor ?? "").PadRight(9).Substring(0, 9);
                    string recStr = (fac.NitReceptor ?? "").PadRight(9).Substring(0, 9);
                    string totStr = fac.Total.ToString("F2").PadLeft(11);
                    string codStr = fac.CodigoAprobacion.ToString();

                    lineas.Add($"{fStr}  | {refStr} | {emStr} | {recStr} | {totStr} | {codStr}");
                }
            }

            lineas.Add("--------------------------------------------------------------------------------");
            lineas.Add("Fin del reporte SAT - Sistema DTE IPC2.");

            byte[] pdfBytes = PdfGenerator.GenerarPdf("SAT - SUPERINTENDENCIA DE ADMINISTRACION TRIBUTARIA", lineas);

            return File(pdfBytes, "application/pdf", $"Reporte_SAT_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
    }
}
