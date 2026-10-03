using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;   // ← AGREGAR ESTA LÍNEA
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using MiProyecto.Helpers;
using MiProyecto.Models;

namespace MiProyecto.Controllers
{
    /// <summary>
    /// ═══════════════════════════════════════════════════════════════
    /// 📌 CAJA REPORT CONTROLLER (ESC/POS)
    /// ═══════════════════════════════════════════════════════════════
    /// Controller para impresión de reportes de caja usando ESC/POS.
    ///
    /// Endpoints:
    ///   GET  /CajaReport  →  status del servicio
    ///   POST /CajaReport  →  imprime APERTURA o CIERRE de caja
    ///
    /// El JSON de entrada tiene la estructura CajaReportRequest:
    ///   {
    ///     "tipo_reporte": "APERTURA" | "CIERRE",
    ///     "documento": {...},
    ///     "emisor": {...},
    ///     "caja": {...},
    ///     "resumen": {...},
    ///     "desglose_medios": [...],
    ///     "movimientos": {...},
    ///     "monedero": [...]
    ///   }
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class CajaReportController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // 📌 Ancho de línea para papel 80mm
        private const int ANCHO = EscPosBuilder.ANCHO_80MM;

        public CajaReportController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ═══════════════════════════════════════════════════════════
        // GET /CajaReport  →  Status
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok("✅ Servicio de reportes de caja listo (ESC/POS)");
        }

        // ═══════════════════════════════════════════════════════════
        // POST /CajaReport  →  Imprimir reporte de caja
        // ═══════════════════════════════════════════════════════════
        [SupportedOSPlatform("windows")]
        [HttpPost]
        public IActionResult SendCajaReport([FromBody] CajaReportRequest request)
        {
            // ─── Validaciones ───
            if (request == null)
                return BadRequest(new { mensaje = "❌ Request vacío" });

            if (string.IsNullOrWhiteSpace(request.TipoReporte))
                return BadRequest(new { mensaje = "❌ Falta campo tipo_reporte" });

            string tipo = request.TipoReporte.Trim().ToUpper();
            if (tipo != "APERTURA" && tipo != "CIERRE")
                return BadRequest(new { mensaje = "❌ tipo_reporte debe ser APERTURA o CIERRE" });

            // ═════════════════════════════════════════════════════════
            // 1) GUARDAR JSON EN DISCO (auditoría)
            // ═════════════════════════════════════════════════════════
            string? rutaLocal = null;
            try
            {
                rutaLocal = GuardarJsonEnDisco(request, tipo);
            }
            catch (Exception ex)
            {
                // No bloquea la impresión si falla el guardado
                Console.WriteLine($"Error al guardar JSON: {ex.Message}");
            }

            // ═════════════════════════════════════════════════════════
            // 2) GENERAR TICKET (ESC/POS)
            // ═════════════════════════════════════════════════════════
            try
            {
                byte[] ticketBytes;

                if (tipo == "APERTURA")
                {
                    ticketBytes = GenerarTicketApertura(request);
                }
                else // CIERRE
                {
                    ticketBytes = GenerarTicketCierre(request);
                }

                // ═══════════════════════════════════════════════════
                // 3) ENVIAR A IMPRESORA (RAW)
                // ═══════════════════════════════════════════════════
                string preferred = _configuration["PrintSettings:PrinterName"]
                                   ?? "POS-80C";

                string? printerToUse = RawPrinterHelper.BuscarImpresora(preferred);

                if (string.IsNullOrEmpty(printerToUse))
                {
                    return BadRequest(new
                    {
                        mensaje = $"❌ No se encontró la impresora '{preferred}'",
                        impresoras = PrinterSettings.InstalledPrinters.Cast<string>().ToList()
                    });
                }

                RawPrinterHelper.SendBytesToPrinter(printerToUse, ticketBytes);

                return Ok(new
                {
                    mensaje = $"🖨️ Ticket de {tipo} enviado correctamente (ESC/POS)",
                    impresora = printerToUse,
                    rutaJsonUsada = rutaLocal,
                    tipo_reporte = tipo
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    mensaje = "❌ Error al imprimir reporte de caja",
                    detalle = ex.Message,
                    tipo_reporte = tipo
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 📌 GUARDAR JSON EN DISCO
        // ═══════════════════════════════════════════════════════════
        private string GuardarJsonEnDisco(CajaReportRequest request, string tipo)
        {
            string? jsonFolder = _configuration["VoucherConfig:RutaJson"]
                                 ?? _configuration["PrintSettings:JsonFolder"];

            if (string.IsNullOrWhiteSpace(jsonFolder))
            {
                string appFolder = AppContext.BaseDirectory;
                jsonFolder = Path.Combine(appFolder, "Jsons");
            }

            jsonFolder = jsonFolder.Replace('/', Path.DirectorySeparatorChar);

            if (!Directory.Exists(jsonFolder))
                Directory.CreateDirectory(jsonFolder);

            string nombreArchivo = request.NombreJson
                ?? $"{tipo}-{DateTime.Now:yyyyMMddHHmmss}.json";

            string ruta = Path.Combine(jsonFolder, nombreArchivo);

            string jsonString = System.Text.Json.JsonSerializer.Serialize(
                request,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

            // File.WriteAllText(ruta, jsonString);
            System.IO.File.WriteAllText(ruta, jsonString);
            return ruta;
        }

        // ═══════════════════════════════════════════════════════════
        // 📌 GENERAR TICKET DE APERTURA (ESC/POS)
        // ═══════════════════════════════════════════════════════════
        private byte[] GenerarTicketApertura(CajaReportRequest r)
        {
            var p = new EscPosBuilder();
            p.Inicializar();

            // ─── ENCABEZADO (centrado, negrita) ───
            p.Centrar();
            p.Negrita(true);
            p.Linea(r.Emisor?.Datos?.RazonSocial ?? "");
            p.Negrita(false);

            p.Linea(r.Emisor?.Datos?.NombreComercial ?? "");
            p.Linea($"RUC: {r.Emisor?.Documento?.Numero ?? ""}");
            p.Linea(r.Emisor?.Ubicacion?.Direccion ?? "");
            p.Linea();

            p.Linea('=', ANCHO);

            // ─── TÍTULO ───
            p.Negrita(true);
            p.TamañoLetra(0x11);   // doble ancho
            p.Linea("APERTURA DE CAJA");
            p.TamañoLetra(0x00);   // normal
            p.Negrita(false);

            p.Linea('=', ANCHO);

            // ─── DATOS DE LA CAJA ───
            p.AlinearIzquierda();
            p.Linea($"FECHA:   {r.Documento?.FechaEmision ?? ""}");
            p.Linea($"HORA:    {r.Documento?.HoraEmision ?? ""}");
            p.Linea($"CAJA:    {r.Caja?.NombreCaja ?? ""}");
            p.Linea($"LOCAL:   {r.Caja?.NombreLocal ?? ""}");
            p.Linea($"USUARIO: {r.Caja?.Usuario ?? ""}");
            p.Linea($"TURNO:   {r.Caja?.Turno ?? ""}");
            p.Linea($"MONEDA:  {r.Caja?.Moneda ?? ""}");
            p.Linea('-', ANCHO);

            // ─── MONTO INICIAL ───
            p.Centrar();
            p.Negrita(true);
            p.Linea("MONTO INICIAL");
            p.Negrita(false);
            p.AlinearIzquierda();
            p.Linea('-', ANCHO);

            // Fila: etiqueta izquierda + valor derecha
            decimal efectivoInicial = r.Resumen?.EfectivoInicial ?? 0m;
            p.Linea(EscPosBuilder.FormatoFila(
                "Efectivo inicial:",
                EscPosBuilder.FormatoMonto(efectivoInicial),
                ANCHO));

            p.Linea('=', ANCHO);

            // ─── DESGLOSE MONEDERO ───
            if (r.Monedero != null && r.Monedero.Count > 0)
            {
                p.Centrar();
                p.Negrita(true);
                p.Linea("DESGLOSE MONEDERO");
                p.Negrita(false);
                p.AlinearIzquierda();
                p.Linea('-', ANCHO);

                // Cabecera de columnas
                p.Linea(EscPosBuilder.Formato3Cols(
                    "DENOM.", "CANT.", "SUBTOTAL",
                    14, 8, ANCHO - 22));
                p.Linea('-', ANCHO);

                decimal totalMonedero = 0m;

                foreach (var m in r.Monedero)
                {
                    string denom = EscPosBuilder.FormatoSoles(m.Denominacion);
                    string cant = $"x{m.Cantidad}";
                    string subtotal = EscPosBuilder.FormatoSoles(m.Subtotal);

                    p.Linea(EscPosBuilder.Formato3Cols(
                        denom, cant, subtotal,
                        14, 8, ANCHO - 22));

                    totalMonedero += m.Subtotal;
                }

                p.Linea('-', ANCHO);

                // Total del monedero
                p.Negrita(true);
                p.Linea(EscPosBuilder.FormatoFila(
                    "TOTAL:",
                    EscPosBuilder.FormatoMonto(totalMonedero),
                    ANCHO));
                p.Negrita(false);
            }

            p.Linea('=', ANCHO);
            p.Linea();

            // ─── PIE ───
            p.Centrar();
            p.Negrita(true);
            p.Linea("Caja aperturada");
            p.Negrita(false);
            p.Linea("¡Buen turno!");
            p.Linea();

            // ─── CORTAR PAPEL ───
            p.Cortar();

            return p.ToBytes();
        }

        // ═══════════════════════════════════════════════════════════
        // 📌 GENERAR TICKET DE CIERRE (ESC/POS)
        // ═══════════════════════════════════════════════════════════
        private byte[] GenerarTicketCierre(CajaReportRequest r)
        {
            var p = new EscPosBuilder();
            p.Inicializar();

            // ─── ENCABEZADO ───
            p.Centrar();
            p.Negrita(true);
            p.Linea(r.Emisor?.Datos?.RazonSocial ?? "");
            p.Negrita(false);

            p.Linea(r.Emisor?.Datos?.NombreComercial ?? "");
            p.Linea($"RUC: {r.Emisor?.Documento?.Numero ?? ""}");
            p.Linea(r.Emisor?.Ubicacion?.Direccion ?? "");
            p.Linea();

            p.Linea('=', ANCHO);

            // ─── TÍTULO ───
            p.Negrita(true);
            p.TamañoLetra(0x11);
            p.Linea("CIERRE DE CAJA");
            p.TamañoLetra(0x00);
            p.Negrita(false);

            p.Linea('=', ANCHO);

            // ─── DATOS DE LA CAJA ───
            p.AlinearIzquierda();
            p.Linea($"FECHA APERTURA: {r.Caja?.FechaApertura ?? ""}");
            p.Linea($"FECHA CIERRE:   {r.Caja?.FechaCierre ?? ""}");
            p.Linea($"CAJA:           {r.Caja?.NombreCaja ?? ""}");
            p.Linea($"LOCAL:          {r.Caja?.NombreLocal ?? ""}");
            p.Linea($"USUARIO:        {r.Caja?.Usuario ?? ""}");
            p.Linea($"TURNO:          {r.Caja?.Turno ?? ""}");
            p.Linea('-', ANCHO);

            // ─── RESUMEN DEL DÍA ───
            p.Centrar();
            p.Negrita(true);
            p.Linea("RESUMEN DEL DIA");
            p.Negrita(false);
            p.AlinearIzquierda();
            p.Linea('-', ANCHO);

            p.Linea(EscPosBuilder.FormatoFila(
                "Cant. ventas:",
                (r.Resumen?.CantVentas ?? 0).ToString(),
                ANCHO));

            p.Linea(EscPosBuilder.FormatoFila(
                "Total ventas:",
                EscPosBuilder.FormatoMonto(r.Resumen?.TotalVentas ?? 0m),
                ANCHO));

            p.Linea(EscPosBuilder.FormatoFila(
                "IGV:",
                EscPosBuilder.FormatoMonto(r.Resumen?.TotalIgv ?? 0m),
                ANCHO));

            if ((r.Resumen?.TotalExonerado ?? 0m) > 0)
            {
                p.Linea(EscPosBuilder.FormatoFila(
                    "Exonerado:",
                    EscPosBuilder.FormatoMonto(r.Resumen.TotalExonerado),
                    ANCHO));
            }

            if ((r.Resumen?.TotalInafecto ?? 0m) > 0)
            {
                p.Linea(EscPosBuilder.FormatoFila(
                    "Inafecto:",
                    EscPosBuilder.FormatoMonto(r.Resumen.TotalInafecto),
                    ANCHO));
            }

            if ((r.Resumen?.TotalIcbper ?? 0m) > 0)
            {
                p.Linea(EscPosBuilder.FormatoFila(
                    "ICBPER:",
                    EscPosBuilder.FormatoMonto(r.Resumen.TotalIcbper),
                    ANCHO));
            }

            p.Linea('-', ANCHO);

            // ─── DESGLOSE POR MEDIO ───
            p.Centrar();
            p.Negrita(true);
            p.Linea("DESGLOSE POR MEDIO");
            p.Negrita(false);
            p.AlinearIzquierda();
            p.Linea('-', ANCHO);

            if (r.DesgloseMedios != null && r.DesgloseMedios.Count > 0)
            {
                foreach (var m in r.DesgloseMedios)
                {
                    string etiqueta = $"{m.Medio} ({m.Cantidad})";
                    string monto = EscPosBuilder.FormatoMonto(m.Monto);
                    p.Linea(EscPosBuilder.FormatoFila(etiqueta, monto, ANCHO));
                }
            }
            else
            {
                p.Linea("Sin ventas");
            }

            p.Linea('-', ANCHO);

            // ─── MOVIMIENTOS ───
            decimal entradas = r.Movimientos?.Entradas ?? 0m;
            decimal salidas = r.Movimientos?.Salidas ?? 0m;

            if (entradas > 0 || salidas > 0)
            {
                p.Centrar();
                p.Negrita(true);
                p.Linea("MOVIMIENTOS");
                p.Negrita(false);
                p.AlinearIzquierda();
                p.Linea('-', ANCHO);

                p.Linea(EscPosBuilder.FormatoFila(
                    "Entradas:", EscPosBuilder.FormatoMonto(entradas), ANCHO));

                p.Linea(EscPosBuilder.FormatoFila(
                    "Salidas:", EscPosBuilder.FormatoMonto(salidas), ANCHO));

                p.Linea('-', ANCHO);
            }

            // ─── EFECTIVO ESPERADO ───
            p.Centrar();
            p.Negrita(true);
            p.Linea("EFECTIVO ESPERADO");
            p.Negrita(false);
            p.AlinearIzquierda();
            p.Linea('-', ANCHO);

            p.Linea(EscPosBuilder.FormatoFila(
                "Inicial:",
                EscPosBuilder.FormatoMonto(r.Resumen?.EfectivoInicial ?? 0m),
                ANCHO));

            p.Linea(EscPosBuilder.FormatoFila(
                "+ Cobrado:",
                EscPosBuilder.FormatoMonto(r.Resumen?.EfectivoCobrado ?? 0m),
                ANCHO));

            p.Linea(EscPosBuilder.FormatoFila(
                "+ Entradas:",
                EscPosBuilder.FormatoMonto(entradas),
                ANCHO));

            p.Linea(EscPosBuilder.FormatoFila(
                "- Salidas:",
                EscPosBuilder.FormatoMonto(salidas),
                ANCHO));

            p.Linea('-', ANCHO);

            // Total esperado (con negrita y doble ancho)
            p.Negrita(true);
            p.Linea(EscPosBuilder.FormatoFila(
                "= ESPERADO:",
                EscPosBuilder.FormatoMonto(r.Resumen?.EfectivoEsperado ?? 0m),
                ANCHO));
            p.Negrita(false);

            p.Linea(EscPosBuilder.FormatoFila(
                "CONTADO:",
                EscPosBuilder.FormatoMonto(r.Resumen?.EfectivoContado ?? 0m),
                ANCHO));

            // ─── DIFERENCIA ───
            decimal diff = r.Resumen?.Diferencia ?? 0m;
            string estadoCierre;

            if (Math.Abs(diff) < 0.01m)
                estadoCierre = "CUADRA";
            else if (diff > 0)
                estadoCierre = "SOBRANTE " + EscPosBuilder.FormatoMonto(diff);
            else
                estadoCierre = "FALTANTE " + EscPosBuilder.FormatoMonto(Math.Abs(diff));

            p.Linea('-', ANCHO);
            p.Negrita(true);
            p.Linea(EscPosBuilder.FormatoFila("DIFERENCIA:", estadoCierre, ANCHO));
            p.Negrita(false);

            p.Linea('=', ANCHO);
            p.Linea();

            // ─── PIE ───
            p.Centrar();
            p.Negrita(true);
            p.Linea("Cierre completado");
            p.Negrita(false);
            p.Linea("¡Hasta pronto!");
            p.Linea();

            // ─── CORTAR PAPEL ───
            p.Cortar();

            return p.ToBytes();
        }
    }
}