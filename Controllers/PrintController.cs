using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;
using System.Text;
using QRCoder;
using MiProyecto.Models;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace MiProyecto.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PrintController : ControllerBase
    {
         
        private readonly IConfiguration _configuration;
        public PrintController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok("✅ Impresora lista para imprimir");
        }

        [SupportedOSPlatform("windows")]
        [HttpPost]
        public IActionResult SendPrint([FromBody] PrintTicketRequest request)
        {

            if (request == null || string.IsNullOrWhiteSpace(request.ArchivoJson))
                return BadRequest(new { mensaje = "❌ JSON inválido o vacío" });

            try
            {
                // Ruta local usando NombreJson
               // Validación inicial
                if (request == null || string.IsNullOrWhiteSpace(request.ArchivoJson) || string.IsNullOrWhiteSpace(request.NombreJson))
                    return BadRequest(new { mensaje = "❌ JSON inválido o vacío" });

                // Ruta local usando NombreJson (null-forgiving para evitar CS8604)
                // string rutaLocal = Path.Combine(@"C:\Impresiones\Jsons", request.NombreJson!);
                // ← Leer ruta desde appsettings.json
                //string jsonFolder = _configuration["PrintSettings:JsonFolder"];
                // ← Leer ruta desde appsettings.json
                string? jsonFolder = _configuration["VoucherConfig:RutaJson"] 
                                     ?? _configuration["PrintSettings:JsonFolder"];

                // Si no está configurada, usar carpeta "Jsons" al lado del .exe
                // (portable: funciona en C:, D:, E:, sin importar el disco)
                if (string.IsNullOrWhiteSpace(jsonFolder))
                {
                    string appFolder = AppContext.BaseDirectory;
                    jsonFolder = Path.Combine(appFolder, "Jsons");
                }
                // Normalizar separadores (Linux usa / y Windows \)
                jsonFolder = jsonFolder.Replace('/', Path.DirectorySeparatorChar);
                
                // Crear la carpeta si no existe
                if (!Directory.Exists(jsonFolder))
                {
                    Directory.CreateDirectory(jsonFolder);
                }

                // Ruta local usando NombreJson
                string rutaLocal = Path.Combine(jsonFolder, request.NombreJson!);

                // Guardar el contenido recibido como archivo
                System.IO.File.WriteAllText(rutaLocal, request.ArchivoJson);

                // Leer el contenido real del JSON
                string jsonContenido = System.IO.File.ReadAllText(rutaLocal);

                // Deserializar
                var ticketData = System.Text.Json.JsonSerializer.Deserialize<PrintTicketRequest>(jsonContenido);
                if (ticketData == null)
                    return BadRequest(new { mensaje = "❌ Error al deserializar JSON" });


                // ← NUEVO: detectar tipo de documento
                string tipo = ticketData?.Documento?.TipoFactura?.Trim() ?? "";
                string ticket;

                if (tipo == "CIERRE")
                {
                    ticket = GenerarTicketCierre(ticketData);
                }
                else
                {
                    ticket = GenerarTicketVenta(ticketData);
                }

                Bitmap qrImage = GenerarQR(ticketData.Documento?.Numero ?? "");

                string preferred = "POS-80C";
                string printerToUse = new PrinterSettings().PrinterName;
                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    if (string.Equals(p, preferred, StringComparison.OrdinalIgnoreCase))
                    {
                        printerToUse = p;
                        break;
                    }
                }

                using var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerToUse;
                pd.PrintPage += (sender, e) =>
                {
                    if (e.Graphics == null) return;
                
                    using var font = new Font("Consolas", 10, FontStyle.Regular);
                    //using var font = new Font("Courier New", 10, FontStyle.Regular);
                    float y = 0;
            
                    e.Graphics.DrawString(ticket, font, Brushes.Black, new PointF(0, y));
                    y += e.Graphics.MeasureString(ticket, font).Height + 10;
            
                    int qrSize = 120;
                    float qrX = (e.PageBounds.Width - qrSize) / 2f;
                    e.Graphics.DrawImage(qrImage, qrX, y, qrSize, qrSize);
                    y += qrSize + 10;
            
                    if (!string.IsNullOrWhiteSpace(ticketData.Hash))
                    {
                        string hashText = $"HASH:\n{ticketData.Hash}";
                        var lines = hashText.Split('\n');
                        foreach (var line in lines)
                        {
                            float textWidth = e.Graphics.MeasureString(line, font).Width;
                            float textX = (e.PageBounds.Width - textWidth) / 2f;
                            e.Graphics.DrawString(line, font, Brushes.Black, new PointF(textX, y));
                            y += e.Graphics.MeasureString(line, font).Height;
                        }
                    }
                };

                // Enviar impresión
                pd.Print();
                qrImage.Dispose();

                return Ok(new { mensaje = "🖨️ Ticket enviado correctamente", impresora = printerToUse, rutaJsonUsada = rutaLocal });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "❌ Error al imprimir", detalle = ex.Message });
            }
        }

        [SupportedOSPlatform("windows")]
        private Bitmap GenerarQR(string contenido)
        {
            contenido ??= "";
            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new BitmapByteQRCode(qrData);
            byte[] qrBytes = qrCode.GetGraphic(5);

            using var ms = new MemoryStream(qrBytes);
            return new Bitmap(ms);
        }

        [SupportedOSPlatform("windows")]
        private string GenerarTicketVenta(PrintTicketRequest r)
        {
            var sb = new StringBuilder();
            int ancho = 42;

            sb.AppendLine(Centrar(r.Emisor?.Datos?.RazonSocial ?? "", ancho));
            sb.AppendLine(Centrar(r.Emisor?.Datos?.NombreComercial ?? "", ancho));
            sb.AppendLine(Centrar($"RUC: {r.Emisor?.Documento?.Numero ?? ""}", ancho));
            sb.AppendLine(Centrar(r.Emisor?.Ubicacion?.Direccion ?? "", ancho));
            sb.AppendLine(Centrar($"urb. {r.Emisor?.Ubicacion?.Urbanizacion ?? ""}", ancho));
            sb.AppendLine(Centrar($"{r.Emisor?.Ubicacion?.Distrito ?? ""} - {r.Emisor?.Ubicacion?.Provincia ?? ""} - {r.Emisor?.Ubicacion?.Departamento ?? ""}", ancho));
            sb.AppendLine(new string('-', ancho));

            string tipo = r.Documento?.TipoFactura?.Trim() ?? "";
            switch (tipo)
            {
                case "01": sb.AppendLine(Centrar("FACTURA ELECTRONICA", ancho)); break;
                case "03": sb.AppendLine(Centrar("BOLETA DE VENTA ELECTRONICA", ancho)); break;
                case "07": sb.AppendLine(Centrar("NOTA DE CREDITO ELECTRONICA", ancho)); break;
                case "08": sb.AppendLine(Centrar("NOTA DE DEBITO ELECTRONICA", ancho)); break;
                case "10": sb.AppendLine(Centrar("TICKET DE VENTA INTERNA", ancho)); break;
                default: sb.AppendLine(Centrar("TICKET DE VENTA", ancho)); break;
            }

            string nroOriginal = r.Documento?.Numero ?? "FV01-1";
            string serie = nroOriginal.Substring(0, 4);
            string numeroStr = nroOriginal.Substring(5);
            string numeroFormateado = int.Parse(numeroStr).ToString("D8");
            string nroComprobante = $"{serie}-{numeroFormateado}";
            sb.AppendLine(Centrar(nroComprobante, ancho));

            // sb.AppendLine($"FECHA: {r.Documento?.FechaEmision ?? ""}  HORA: {r.Documento?.HoraEmision ?? ""}");
            // sb.AppendLine(new string('=', ancho));
            // sb.AppendLine("CANT  DESCRIPCION        P/U  SUBT");
            // sb.AppendLine(new string('-', ancho));
                        sb.AppendLine($"FECHA: {r.Documento?.FechaEmision ?? ""}  HORA: {r.Documento?.HoraEmision ?? ""}");

            // ═══════════════════════════════════════════════════════════
            // 📌 DATOS DEL CLIENTE (solo Boleta y Factura)
            // ═══════════════════════════════════════════════════════════
            if (tipo == "01" || tipo == "03")
            {
                sb.AppendLine(new string('=', ancho));

                string razonSocial = (r.Cliente?.Datos?.RazonSocial ?? "").Trim();
                string numDoc = (r.Cliente?.Documento?.Numero ?? "").Trim();
                string nombreCom = (r.Cliente?.Datos?.NombreComercial ?? "").Trim();
                string direccion = (r.Cliente?.Ubicacion?.Direccion ?? "").Trim();
                string distrito = (r.Cliente?.Ubicacion?.Distrito ?? "").Trim();
                string provincia = (r.Cliente?.Ubicacion?.Provincia ?? "").Trim();
                string departamento = (r.Cliente?.Ubicacion?.Departamento ?? "").Trim();

                string etiquetaDoc = (tipo == "01") ? "RUC:     " : "DNI:     ";

                sb.AppendLine($"CLIENTE:  {razonSocial}");
                sb.AppendLine($"{etiquetaDoc}{numDoc}");

                if (tipo == "01" && !string.IsNullOrWhiteSpace(nombreCom))
                {
                    sb.AppendLine($"NOMBRE COMERCIAL: {nombreCom}");
                }

                if (!string.IsNullOrWhiteSpace(direccion) && direccion != ".")
                {
                    sb.AppendLine($"DIRECCION: {direccion}");
                }

                string ubigeoLinea = "";
                if (!string.IsNullOrWhiteSpace(distrito)) ubigeoLinea += distrito;
                if (!string.IsNullOrWhiteSpace(provincia))
                    ubigeoLinea += (ubigeoLinea.Length > 0 ? " - " : "") + provincia;
                if (!string.IsNullOrWhiteSpace(departamento))
                    ubigeoLinea += (ubigeoLinea.Length > 0 ? " - " : "") + departamento;

                if (!string.IsNullOrWhiteSpace(ubigeoLinea))
                {
                    sb.AppendLine($"DISTRITO: {ubigeoLinea}");
                }
            }

            sb.AppendLine(new string('=', ancho));

            sb.AppendLine("CANT  DESCRIPCION        P/U  SUBT");
            sb.AppendLine(new string('-', ancho));
            
            decimal totalExonerados = 0m;
            decimal totalGravados = 0m;

            foreach (var item in r.Items ?? new List<Item>())
            {
                decimal cantidad = ParseDecimal(item?.Cantidad);
                decimal valorUnit = ParseDecimal(item?.PrecioUnitario);
                decimal subtotalItem = cantidad * valorUnit;

                if (item?.CodigoAfectacion == "20")
                    totalExonerados += subtotalItem;
                else
                    totalGravados += subtotalItem;

                string descFull = item?.NombreProducto ?? "";
                var descChunks = new List<string>();
                int descColWidth = 16;

                for (int i = 0; i < descFull.Length; i += descColWidth)
                    descChunks.Add(descFull.Substring(i, Math.Min(descColWidth, descFull.Length - i)));

                if (descChunks.Count == 0) descChunks.Add("");

                string line1 = cantidad.ToString("F2", CultureInfo.InvariantCulture).PadRight(6)
                             + descChunks[0].PadRight(descColWidth)
                             + valorUnit.ToString("F2", CultureInfo.InvariantCulture).PadLeft(6)
                             + subtotalItem.ToString("F2", CultureInfo.InvariantCulture).PadLeft(6);
                sb.AppendLine(line1);

                for (int k = 1; k < descChunks.Count; k++)
                    sb.AppendLine(new string(' ', 6) + descChunks[k].PadRight(descColWidth));
            }

            decimal valor = decimal.Parse(r.Impuestos?.TotalOpeGravadas ?? "0");
            decimal igv = decimal.Parse(r.Total?.TotalImpuestos ?? "0");
            decimal total = decimal.Parse(r.Total?.Pagable ?? "0");
            decimal exonerados = decimal.Parse(r.Impuestos?.TotalOpeExoneradas ?? "0");
            decimal icbper = decimal.Parse(r.Total?.Icbper ?? "0");

            int nancho = 35;
            int maxEtiqueta = Math.Max(
                Math.Max("VALOR ABSOLUTO:".Length, "I.G.V 18%:".Length),
                Math.Max("EXONERADO:".Length, Math.Max("ICBPER:".Length, "TOTAL:".Length))
            );
            int espacioNumerico = nancho - maxEtiqueta;

            sb.AppendLine(new string('-', nancho));
            sb.AppendLine("VALOR ABSOLUTO:".PadRight(maxEtiqueta) + valor.ToString("0.00").PadLeft(espacioNumerico));
            sb.AppendLine("I.G.V 18%:".PadRight(maxEtiqueta) + igv.ToString("0.00").PadLeft(espacioNumerico));
            if (exonerados > 0)
                sb.AppendLine("EXONERADO:".PadRight(maxEtiqueta) + exonerados.ToString("0.00").PadLeft(espacioNumerico));
            if (icbper > 0)
                sb.AppendLine("ICBPER:".PadRight(maxEtiqueta) + icbper.ToString("0.00").PadLeft(espacioNumerico));
            sb.AppendLine("TOTAL:".PadRight(maxEtiqueta) + total.ToString("0.00").PadLeft(espacioNumerico));
            sb.AppendLine(new string('-', nancho));

            sb.AppendLine($"CAJERO: {r.Cajero ?? ""}");
            sb.AppendLine($"VENDEDOR: {r.Vendedor ?? ""}");
            sb.AppendLine($"NRO CUOTAS: {r.Nrocuotas}");
            sb.AppendLine($"NRO CAJA: {r.NroCaja}");
            sb.AppendLine($"ID CAJERO: {r.IdCajero}");

                        // ═══════════════════════════════════════════════════════════
            // TIPO DE PAGO + MEDIOS (formato compacto)
            // ═══════════════════════════════════════════════════════════
            // Formato:
            //   TIPO PAGO: Contado
            //   - YAPE: 10.90 op:1236585
            //   - EFECTIVO: 10.00
            //   - Entregado: 20.00 Vuelto: 10.00
            //
            // Reglas:
            //   - 1 línea por cada medio NO-efectivo (YAPE, PLIN, TARJETA...)
            //   - EFECTIVO siempre va al final y usa 2 líneas
            //   - Si el medio tiene nro_operacion, se agrega "op:XXXX"
            // ═══════════════════════════════════════════════════════════
            if (r.Tipopagos != null && r.Tipopagos.Count > 0)
            {
                // Encabezado con la forma de pago (Contado / Credito)
                string formaPago = r.TipoPago?.FormaPago ?? "Contado";
                sb.AppendLine($"TIPO PAGO: {formaPago}");

                // ─── 1. Separar EFECTIVO del resto ───
                var efectivo = r.Tipopagos
                    .FirstOrDefault(p => (p.Medio ?? "").ToUpper().Contains("EFECTIVO"));
                var otrosMedios = r.Tipopagos
                    .Where(p => !(p.Medio ?? "").ToUpper().Contains("EFECTIVO"))
                    .ToList();

                // ─── 2. Imprimir primero los otros medios (YAPE, PLIN, etc.) ───
                foreach (var pago in otrosMedios)
                {
                    string medio = (pago.Medio ?? "").ToUpper();
                    string linea = $"- {medio}: {pago.Monto:F2}";

                    // Si tiene nro. de operación, lo agregamos en la misma línea
                    if (!string.IsNullOrWhiteSpace(pago.NroOperacion))
                    {
                        linea += $" op:{pago.NroOperacion}";
                    }

                    sb.AppendLine(linea);
                }

                // ─── 3. Imprimir EFECTIVO al final (2 líneas) ───
                if (efectivo != null)
                {
                    sb.AppendLine($"- EFECTIVO: {efectivo.Monto:F2}");
                    sb.AppendLine($"- Entregado: {efectivo.DineroEntregado:F2} Vuelto: {efectivo.Vuelto:F2}");
                }
            }

            // ═══════════════════════════════════════════════════════════
            // MONTO EN LETRAS (SON: ...)
            // ═══════════════════════════════════════════════════════════
            // Viene en el JSON como: notas[0].valor
            // Ejemplo: "VEINTE Y 50/100 SOLES "
            // ═══════════════════════════════════════════════════════════
            if (r.Notas != null && r.Notas.Count > 0)
            {
                string montoLetras = r.Notas[0].Valor ?? "";
                if (!string.IsNullOrWhiteSpace(montoLetras))
                {
                    sb.AppendLine();
                    sb.AppendLine($"SON: {montoLetras.Trim()}");
                }
            }

            sb.AppendLine(new string('-', ancho));
            sb.AppendLine(Centrar("GRACIAS POR SU COMPRA!", ancho));

            // if (r.Tipopagos != null && r.Tipopagos.Count > 0)
            // {
            //     sb.AppendLine("TIPO PAGO:");
            //     foreach (var pago in r.Tipopagos)
            //     {
            //         sb.AppendLine($"- {pago.Medio}: {pago.Monto:F2} " +
            //                       $"{(string.IsNullOrWhiteSpace(pago.NroOperacion) ? "" : $"Op:{pago.NroOperacion}")}" +
            //                       $"{(pago.DineroEntregado > 0 ? $" Entregado:{pago.DineroEntregado:F2}" : "")}" +
            //                       $"{(pago.Vuelto > 0 ? $" Vuelto:{pago.Vuelto:F2}" : "")}");
            //     }
            // }
// 
            // sb.AppendLine(new string('-', ancho));
            // sb.AppendLine(Centrar("GRACIAS POR SU COMPRA!", ancho));
            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        private static decimal ParseDecimal(object? value)
        {
            if (value == null) return 0m;
            if (value is decimal d) return d;
            if (value is double db) return Convert.ToDecimal(db);
            if (value is float f) return Convert.ToDecimal(f);
            if (value is int i) return i;
            if (value is long l) return l;

            string s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            s = s.Replace(",", "");
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var res)) return res;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out res)) return res;

            return 0m;
        }
        private static string Recortar(string? s, int width)
        {
            s ??= "";
            return s.Length <= width ? s : s.Substring(0, width);
        }
        private static string Centrar(string? s, int width)
        {
            s ??= "";
            if (s.Length >= width) return s.Substring(0, width);
            int left = (width - s.Length) / 2;
            return new string(' ', left) + s;
        }
        
        // ============================================================
        // TICKET DE CIERRE DE CAJA
        // ============================================================
        [SupportedOSPlatform("windows")]
        private string GenerarTicketCierre(PrintTicketRequest r)
        {
            var sb = new StringBuilder();
            int ancho = 42;

            // ─── ENCABEZADO ───
            sb.AppendLine(Centrar(r.Emisor?.Datos?.RazonSocial ?? "", ancho));
            sb.AppendLine(Centrar(r.Emisor?.Datos?.NombreComercial ?? "", ancho));
            sb.AppendLine(Centrar($"RUC: {r.Emisor?.Documento?.Numero ?? ""}", ancho));
            sb.AppendLine(Centrar(r.Emisor?.Ubicacion?.Direccion ?? "", ancho));
            sb.AppendLine(new string('=', ancho));
            sb.AppendLine(Centrar("CIERRE DE CAJA", ancho));
            sb.AppendLine(new string('=', ancho));

            // ─── DATOS DEL CIERRE ───
            sb.AppendLine($"FECHA:  {r.Documento?.FechaEmision ?? ""}");
            sb.AppendLine($"HORA:   {r.Documento?.HoraEmision ?? ""}");
            sb.AppendLine($"CAJA:   {r.NroCaja ?? ""}");
            sb.AppendLine($"CAJERO: {r.Cajero ?? ""}");
            sb.AppendLine(new string('-', ancho));

            // ─── RESUMEN DEL DÍA (parseando items especiales) ───
            sb.AppendLine(Centrar("RESUMEN DEL DIA", ancho));
            sb.AppendLine(new string('-', ancho));

            decimal cantVentas = 0m;
            decimal totalVentas = 0m;
            decimal totalIgv = 0m;
            decimal totalExonerado = 0m;
            decimal totalInafecto = 0m;
            decimal totalIcbper = 0m;
            decimal totalEntradas = 0m;
            decimal totalSalidas = 0m;
            decimal efectivoInicial = 0m;
            decimal efectivoCobrado = 0m;
            decimal efectivoEsperado = 0m;
            decimal efectivoContado = 0m;
            decimal diferencia = 0m;

            foreach (var item in r.Items ?? new List<Item>())
            {
                string desc = item?.NombreProducto ?? "";
                string valStr = item?.PrecioUnitario ?? "0";

                if (desc.StartsWith("CANTVENTAS:"))
                    decimal.TryParse(desc.Substring(11), out cantVentas);
                else if (desc.StartsWith("VENTAS:"))
                    decimal.TryParse(desc.Substring(7), out totalVentas);
                else if (desc.StartsWith("IGV:"))
                    decimal.TryParse(desc.Substring(4), out totalIgv);
                else if (desc.StartsWith("EXONERADO:"))
                    decimal.TryParse(desc.Substring(10), out totalExonerado);
                else if (desc.StartsWith("INAFECTO:"))
                    decimal.TryParse(desc.Substring(9), out totalInafecto);
                else if (desc.StartsWith("ICBPER:"))
                    decimal.TryParse(desc.Substring(7), out totalIcbper);
                else if (desc.StartsWith("ENTRADAS:"))
                    decimal.TryParse(desc.Substring(9), out totalEntradas);
                else if (desc.StartsWith("SALIDAS:"))
                    decimal.TryParse(desc.Substring(8), out totalSalidas);
                else if (desc.StartsWith("EFECTIVO_INICIAL:"))
                    decimal.TryParse(desc.Substring(17), out efectivoInicial);
                else if (desc.StartsWith("EFECTIVO_COBRADO:"))
                    decimal.TryParse(desc.Substring(17), out efectivoCobrado);
                else if (desc.StartsWith("EFECTIVO_ESPERADO:"))
                    decimal.TryParse(desc.Substring(18), out efectivoEsperado);
                else if (desc.StartsWith("EFECTIVO_CONTADO:"))
                    decimal.TryParse(desc.Substring(17), out efectivoContado);
                else if (desc.StartsWith("DIFERENCIA:"))
                    decimal.TryParse(desc.Substring(11), out diferencia);
            }

            int maxLbl = 22;
            int padding = ancho - maxLbl;

            sb.AppendLine("Cant. ventas:".PadRight(maxLbl) + cantVentas.ToString("0").PadLeft(padding));
            sb.AppendLine("Total ventas:".PadRight(maxLbl) + totalVentas.ToString("0.00").PadLeft(padding));
            sb.AppendLine("IGV:".PadRight(maxLbl) + totalIgv.ToString("0.00").PadLeft(padding));
            if (totalExonerado > 0)
                sb.AppendLine("Exonerado:".PadRight(maxLbl) + totalExonerado.ToString("0.00").PadLeft(padding));
            if (totalInafecto > 0)
                sb.AppendLine("Inafecto:".PadRight(maxLbl) + totalInafecto.ToString("0.00").PadLeft(padding));
            if (totalIcbper > 0)
                sb.AppendLine("ICBPER:".PadRight(maxLbl) + totalIcbper.ToString("0.00").PadLeft(padding));

            sb.AppendLine(new string('-', ancho));

            // ─── DESGLOSE POR MEDIO ───
            sb.AppendLine(Centrar("DESGLOSE POR MEDIO", ancho));
            sb.AppendLine(new string('-', ancho));

            if (r.Tipopagos != null && r.Tipopagos.Count > 0)
            {
                foreach (var pago in r.Tipopagos)
                {
                    string medio = pago.Medio ?? "";
                    sb.AppendLine(medio.PadRight(maxLbl) + pago.Monto.ToString("0.00").PadLeft(padding));
                }
            }
            else
            {
                sb.AppendLine("Sin ventas".PadRight(ancho));
            }

            sb.AppendLine(new string('-', ancho));

            // ─── MOVIMIENTOS ───
            if (totalEntradas > 0 || totalSalidas > 0)
            {
                sb.AppendLine(Centrar("MOVIMIENTOS", ancho));
                sb.AppendLine(new string('-', ancho));
                sb.AppendLine("Entradas:".PadRight(maxLbl) + totalEntradas.ToString("0.00").PadLeft(padding));
                sb.AppendLine("Salidas:".PadRight(maxLbl) + totalSalidas.ToString("0.00").PadLeft(padding));
                sb.AppendLine(new string('-', ancho));
            }

            // ─── EFECTIVO ESPERADO ───
            sb.AppendLine(Centrar("EFECTIVO ESPERADO", ancho));
            sb.AppendLine(new string('-', ancho));
            sb.AppendLine("Inicial:".PadRight(maxLbl) + efectivoInicial.ToString("0.00").PadLeft(padding));
            sb.AppendLine("+ Cobrado:".PadRight(maxLbl) + efectivoCobrado.ToString("0.00").PadLeft(padding));
            sb.AppendLine("+ Entradas:".PadRight(maxLbl) + totalEntradas.ToString("0.00").PadLeft(padding));
            sb.AppendLine("- Salidas:".PadRight(maxLbl) + totalSalidas.ToString("0.00").PadLeft(padding));

            sb.AppendLine(new string('-', ancho));
            sb.AppendLine("= ESPERADO:".PadRight(maxLbl) + efectivoEsperado.ToString("0.00").PadLeft(padding));
            sb.AppendLine("CONTADO:".PadRight(maxLbl) + efectivoContado.ToString("0.00").PadLeft(padding));

            string estadoCierre;
            if (Math.Abs(diferencia) < 0.01m)
                estadoCierre = "CUADRA";
            else if (diferencia > 0)
                estadoCierre = "SOBRANTE " + diferencia.ToString("0.00");
            else
                estadoCierre = "FALTANTE " + Math.Abs(diferencia).ToString("0.00");

            sb.AppendLine("DIFERENCIA:".PadRight(maxLbl) + estadoCierre.PadLeft(padding));

            sb.AppendLine(new string('=', ancho));
            sb.AppendLine(Centrar("Cierre completado", ancho));
            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

    } // Fin de la clase PrintController
}   //  Fin del namespace

