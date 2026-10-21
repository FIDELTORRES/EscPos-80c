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

        /// <summary>
        /// Endpoint de estado para verificar que la API está viva.
        /// GET /Print
        /// </summary>
        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok("✅ Impresora lista para imprimir");
        }

        /// <summary>
        /// Endpoint principal para imprimir tickets.
        /// POST /Print
        /// </summary>
        [SupportedOSPlatform("windows")]
        [HttpPost]
        public IActionResult SendPrint([FromBody] PrintTicketRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ArchivoJson) || string.IsNullOrWhiteSpace(request.NombreJson))
                return BadRequest(new { mensaje = "❌ JSON inválido o vacío" });

            try
            {
                // ============================================================
                // 📌 1. CONFIGURACIÓN DE CARPETA Y GUARDADO DEL JSON
                // ============================================================
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

                string rutaLocal = Path.Combine(jsonFolder, request.NombreJson!);
                System.IO.File.WriteAllText(rutaLocal, request.ArchivoJson);

                string jsonContenido = System.IO.File.ReadAllText(rutaLocal);
                PrintTicketRequest? deserializedTicket =
                    System.Text.Json.JsonSerializer.Deserialize<PrintTicketRequest>(jsonContenido);

                if (deserializedTicket is null)
                    return BadRequest(new { mensaje = "❌ Error al deserializar JSON" });

                PrintTicketRequest ticketData = deserializedTicket;

                // ============================================================
                // 📌 2. DETECCIÓN DEL TIPO DE DOCUMENTO
                // ============================================================
                string tipo = ticketData?.Documento?.TipoFactura?.Trim() ?? "";
                string ticket;

                if (tipo == "CIERRE")
                    ticket = GenerarTicketCierre(ticketData);
                else
                    ticket = GenerarTicketVenta(ticketData); // Aquí entra NC, Factura, Boleta, etc.

                // ============================================================
                // 📌 3. GENERACIÓN DEL QR
                // ============================================================
                Bitmap qrImage = GenerarQR(ticketData.Documento?.Numero ?? "");

                // ============================================================
                // 📌 4. SELECCIÓN DE IMPRESORA (Busca "POS-80C" por defecto)
                // ============================================================
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

                // ============================================================
                // 📌 5. ENVÍO A LA IMPRESORA
                // ============================================================
                using var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerToUse;
                pd.PrintPage += (sender, e) =>
                {
                    if (e.Graphics == null) return;
                    using var font = new Font("Consolas", 10, FontStyle.Regular);
                    float y = 0;

                    // Dibujar el cuerpo del ticket
                    e.Graphics.DrawString(ticket, font, Brushes.Black, new PointF(0, y));
                    y += e.Graphics.MeasureString(ticket, font).Height + 10;

                    // Dibujar el QR
                    int qrSize = 120;
                    float qrX = (e.PageBounds.Width - qrSize) / 2f;
                    e.Graphics.DrawImage(qrImage, qrX, y, qrSize, qrSize);
                    y += qrSize + 10;

                    // Dibujar el Hash si es que existe
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

                pd.Print();
                qrImage.Dispose();

                return Ok(new { mensaje = "🖨️ Ticket enviado correctamente", impresora = printerToUse, rutaJsonUsada = rutaLocal });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "❌ Error al imprimir", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Genera la imagen del código QR usando la librería QRCoder.
        /// </summary>
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

        /// <summary>
        /// Genera el texto formateado del ticket para Facturas, Boletas, Notas de Crédito y Tickets.
        /// </summary>
        [SupportedOSPlatform("windows")]
        private string GenerarTicketVenta(PrintTicketRequest r)
        {
            var sb = new StringBuilder();
            int ancho = 42; // Ancho estándar de la impresora POS-80C (80mm)

            // ============================================================
            // 📌 1. ENCABEZADO DEL EMISOR
            // ============================================================
            sb.AppendLine(Centrar(r.Emisor?.Datos?.RazonSocial ?? "", ancho));
            sb.AppendLine(Centrar(r.Emisor?.Datos?.NombreComercial ?? "", ancho));
            sb.AppendLine(Centrar($"RUC: {r.Emisor?.Documento?.Numero ?? ""}", ancho));
            sb.AppendLine(Centrar(r.Emisor?.Ubicacion?.Direccion ?? "", ancho));
            sb.AppendLine(Centrar($"urb. {r.Emisor?.Ubicacion?.Urbanizacion ?? ""}", ancho));
            sb.AppendLine(Centrar($"{r.Emisor?.Ubicacion?.Distrito ?? ""} - {r.Emisor?.Ubicacion?.Provincia ?? ""} - {r.Emisor?.Ubicacion?.Departamento ?? ""}", ancho));
            sb.AppendLine(new string('-', ancho));

            // ============================================================
            // 📌 2. TÍTULO DEL COMPROBANTE (Según el tipo)
            // ============================================================
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

            // Formateo del número (Ej: BC01-00000017)
            string nroOriginal = r.Documento?.Numero ?? "FV01-1";
            string serie = nroOriginal.Substring(0, 4);
            string numeroStr = nroOriginal.Substring(5);
            string numeroFormateado = int.Parse(numeroStr).ToString("D8");
            string nroComprobante = $"{serie}-{numeroFormateado}";
            sb.AppendLine(Centrar(nroComprobante, ancho));

            sb.AppendLine($"FECHA: {r.Documento?.FechaEmision ?? ""}  HORA: {r.Documento?.HoraEmision ?? ""}");

            // ============================================================
            // 📌 3. DATOS DEL CLIENTE (Para Facturas, Boletas y NC)
            // ============================================================
            // ⭐ CAMBIO: Se agregó el tipo "07" para que las Notas de Crédito impriman el cliente
            if (tipo == "01" || tipo == "03" || tipo == "07")
            {
                sb.AppendLine(new string('=', ancho));

                string razonSocial = (r.Cliente?.Datos?.RazonSocial ?? "").Trim();
                string numDoc = (r.Cliente?.Documento?.Numero ?? "").Trim();
                string direccion = (r.Cliente?.Ubicacion?.Direccion ?? "").Trim();
                string distrito = (r.Cliente?.Ubicacion?.Distrito ?? "").Trim();
                string provincia = (r.Cliente?.Ubicacion?.Provincia ?? "").Trim();
                string departamento = (r.Cliente?.Ubicacion?.Departamento ?? "").Trim();

                string etiquetaDoc = (tipo == "01") ? "RUC:     " : "DNI:     ";

                sb.AppendLine($"CLIENTE:  {razonSocial}");
                sb.AppendLine($"{etiquetaDoc}{numDoc}");

                if (!string.IsNullOrWhiteSpace(direccion) && direccion != ".")
                    sb.AppendLine($"DIRECCION: {direccion}");

                string ubigeoLinea = "";
                if (!string.IsNullOrWhiteSpace(distrito)) ubigeoLinea += distrito;
                if (!string.IsNullOrWhiteSpace(provincia))
                    ubigeoLinea += (ubigeoLinea.Length > 0 ? " - " : "") + provincia;
                if (!string.IsNullOrWhiteSpace(departamento))
                    ubigeoLinea += (ubigeoLinea.Length > 0 ? " - " : "") + departamento;

                if (!string.IsNullOrWhiteSpace(ubigeoLinea))
                    sb.AppendLine($"DISTRITO: {ubigeoLinea}");

                // ============================================================
                // 📌 4. DATOS DEL COMPROBANTE MODIFICADO (Solo para NC tipo 07)
                // ============================================================
                // ⭐ CAMBIO: Se agregó este bloque para mostrar a qué boleta/factura afecta y el motivo.
                if (tipo == "07" && r.Impresion != null)
                {
                    sb.AppendLine(new string('-', ancho));
                    sb.AppendLine(Centrar("DOCUMENTO QUE MODIFICA", ancho));
                    sb.AppendLine($"Nro.: {r.Impresion.ComprobanteModificado}");
                    
                    if (r.Facturas != null && r.Facturas.Count > 0)
                    {
                        var factura = r.Facturas[0];
                        if (factura?.Motivo != null)
                        {
                            sb.AppendLine($"MOTIVO: {factura.Motivo.Descripcion}");
                            
                            // ⭐ FIX: Dividir el sustento por PALABRAS, no por letras
                            if (!string.IsNullOrWhiteSpace(factura.Motivo.Sustento))
                            {
                                string sustento = factura.Motivo.Sustento.Trim();
                                int maxAncho = 32; // Ancho máximo de la línea
                                
                                // Dividimos todo el texto en palabras sueltas
                                string[] palabras = sustento.Split(' ');
                                string lineaActual = "SUSTENTO: ";

                                foreach (string palabra in palabras)
                                {
                                    // Si agregar la palabra excede el ancho, imprimimos la línea y saltamos
                                    if ((lineaActual + " " + palabra).Length > maxAncho)
                                    {
                                        sb.AppendLine(lineaActual.TrimEnd());
                                        // La siguiente línea lleva una sangría para que se vea ordenado
                                        lineaActual = "          " + palabra + " "; 
                                    }
                                    else
                                    {
                                        // Si cabe, la agregamos a la línea actual
                                        lineaActual += palabra + " ";
                                    }
                                }
                                
                                // Imprimimos la última línea que quede
                                if (!string.IsNullOrWhiteSpace(lineaActual))
                                {
                                    sb.AppendLine(lineaActual.TrimEnd());
                                }
                            }
                            
                        }
                    }
                }
            }

            // ============================================================
            // 📌 5. TABLA DE ITEMS
            // ============================================================
            sb.AppendLine(new string('=', ancho));
            sb.AppendLine("CANT  DESCRIPCION        P/U  SUBT");
            sb.AppendLine(new string('-', ancho));

            decimal totalGravados = 0m;
            foreach (var item in r.Items ?? new List<Item>())
            {
                decimal cantidad = ParseDecimal(item?.Cantidad);
                decimal valorUnit = ParseDecimal(item?.PrecioUnitario);
                decimal subtotalItem = cantidad * valorUnit;

                if (item?.CodigoAfectacion != "20")
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

            // ============================================================
            // 📌 6. TOTALES
            // ============================================================
            decimal valor = decimal.Parse(r.Impuestos?.TotalOpeGravadas ?? "0");
            decimal igv = decimal.Parse(r.Total?.TotalImpuestos ?? "0");
            decimal total = decimal.Parse(r.Total?.Pagable ?? "0");
            decimal exonerados = decimal.Parse(r.Impuestos?.TotalOpeExoneradas ?? "0");
            decimal icbper = decimal.Parse(r.Total?.Icbper ?? "0");

            int maxEtiqueta = 15;
            int espacioNumerico = 35 - maxEtiqueta;

            sb.AppendLine(new string('-', 35));
            sb.AppendLine("VALOR ABSOLUTO:".PadRight(maxEtiqueta) + valor.ToString("0.00").PadLeft(espacioNumerico));
            sb.AppendLine("I.G.V 18%:".PadRight(maxEtiqueta) + igv.ToString("0.00").PadLeft(espacioNumerico));
            if (exonerados > 0)
                sb.AppendLine("EXONERADO:".PadRight(maxEtiqueta) + exonerados.ToString("0.00").PadLeft(espacioNumerico));
            if (icbper > 0)
                sb.AppendLine("ICBPER:".PadRight(maxEtiqueta) + icbper.ToString("0.00").PadLeft(espacioNumerico));
            sb.AppendLine("TOTAL:".PadRight(maxEtiqueta) + total.ToString("0.00").PadLeft(espacioNumerico));
            sb.AppendLine(new string('-', 35));

            // ============================================================
            // 📌 7. DATOS DEL CAJERO Y CAJA
            // ============================================================
            // ⭐ CAMBIO: Ahora lee primero del bloque "impresion" (si es NC) y si no, usa los datos normales.
            string cajero = r.Impresion?.Cajero ?? r.Cajero ?? "";
            string nrocaja = r.Impresion?.NroCaja ?? r.NroCaja ?? "";
            string idcajero = r.Impresion?.IdCajero ?? r.IdCajero ?? "";

            sb.AppendLine($"CAJERO: {cajero}");
            sb.AppendLine($"NRO CAJA: {nrocaja}");
            sb.AppendLine($"ID CAJERO: {idcajero}");

            if (r.TipoPago != null)
            {
                sb.AppendLine($"FORMA DE PAGO: {r.TipoPago.FormaPago ?? "Contado"}");
            }

            // ============================================================
            // 📌 8. MONTO EN LETRAS
            // ============================================================
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
            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Convierte un objeto a decimal de forma segura.
        /// </summary>
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

        /// <summary>
        /// Centra un texto en un ancho dado.
        /// </summary>
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

            // Encabezado
            sb.AppendLine(Centrar(r.Emisor?.Datos?.RazonSocial ?? "", ancho));
            sb.AppendLine(Centrar(r.Emisor?.Datos?.NombreComercial ?? "", ancho));
            sb.AppendLine(Centrar($"RUC: {r.Emisor?.Documento?.Numero ?? ""}", ancho));
            sb.AppendLine(Centrar(r.Emisor?.Ubicacion?.Direccion ?? "", ancho));
            sb.AppendLine(new string('=', ancho));
            sb.AppendLine(Centrar("CIERRE DE CAJA", ancho));
            sb.AppendLine(new string('=', ancho));

            // Datos
            sb.AppendLine($"FECHA:  {r.Documento?.FechaEmision ?? ""}");
            sb.AppendLine($"HORA:   {r.Documento?.HoraEmision ?? ""}");
            sb.AppendLine($"CAJA:   {r.NroCaja ?? ""}");
            sb.AppendLine($"CAJERO: {r.Cajero ?? ""}");
            sb.AppendLine(new string('-', ancho));

            // Resumen
            sb.AppendLine(Centrar("RESUMEN DEL DIA", ancho));
            sb.AppendLine(new string('-', ancho));

            decimal cantVentas = 0m, totalVentas = 0m, totalIgv = 0m, totalExonerado = 0m, totalInafecto = 0m, totalIcbper = 0m;
            decimal totalEntradas = 0m, totalSalidas = 0m, efectivoInicial = 0m, efectivoCobrado = 0m;
            decimal efectivoEsperado = 0m, efectivoContado = 0m, diferencia = 0m;

            foreach (var item in r.Items ?? new List<Item>())
            {
                string desc = item?.NombreProducto ?? "";
                if (desc.StartsWith("CANTVENTAS:")) decimal.TryParse(desc.Substring(11), out cantVentas);
                else if (desc.StartsWith("VENTAS:")) decimal.TryParse(desc.Substring(7), out totalVentas);
                else if (desc.StartsWith("IGV:")) decimal.TryParse(desc.Substring(4), out totalIgv);
                else if (desc.StartsWith("EXONERADO:")) decimal.TryParse(desc.Substring(10), out totalExonerado);
                else if (desc.StartsWith("INAFECTO:")) decimal.TryParse(desc.Substring(9), out totalInafecto);
                else if (desc.StartsWith("ICBPER:")) decimal.TryParse(desc.Substring(7), out totalIcbper);
                else if (desc.StartsWith("ENTRADAS:")) decimal.TryParse(desc.Substring(9), out totalEntradas);
                else if (desc.StartsWith("SALIDAS:")) decimal.TryParse(desc.Substring(8), out totalSalidas);
                else if (desc.StartsWith("EFECTIVO_INICIAL:")) decimal.TryParse(desc.Substring(17), out efectivoInicial);
                else if (desc.StartsWith("EFECTIVO_COBRADO:")) decimal.TryParse(desc.Substring(17), out efectivoCobrado);
                else if (desc.StartsWith("EFECTIVO_ESPERADO:")) decimal.TryParse(desc.Substring(18), out efectivoEsperado);
                else if (desc.StartsWith("EFECTIVO_CONTADO:")) decimal.TryParse(desc.Substring(17), out efectivoContado);
                else if (desc.StartsWith("DIFERENCIA:")) decimal.TryParse(desc.Substring(11), out diferencia);
            }

            int maxLbl = 22, padding = ancho - maxLbl;
            sb.AppendLine("Cant. ventas:".PadRight(maxLbl) + cantVentas.ToString("0").PadLeft(padding));
            sb.AppendLine("Total ventas:".PadRight(maxLbl) + totalVentas.ToString("0.00").PadLeft(padding));
            sb.AppendLine("IGV:".PadRight(maxLbl) + totalIgv.ToString("0.00").PadLeft(padding));
            if (totalExonerado > 0) sb.AppendLine("Exonerado:".PadRight(maxLbl) + totalExonerado.ToString("0.00").PadLeft(padding));
            if (totalInafecto > 0) sb.AppendLine("Inafecto:".PadRight(maxLbl) + totalInafecto.ToString("0.00").PadLeft(padding));
            if (totalIcbper > 0) sb.AppendLine("ICBPER:".PadRight(maxLbl) + totalIcbper.ToString("0.00").PadLeft(padding));
            sb.AppendLine(new string('-', ancho));

            sb.AppendLine(Centrar("DESGLOSE POR MEDIO", ancho));
            sb.AppendLine(new string('-', ancho));
            if (r.Tipopagos != null && r.Tipopagos.Count > 0)
                foreach (var pago in r.Tipopagos) sb.AppendLine((pago.Medio ?? "").PadRight(maxLbl) + pago.Monto.ToString("0.00").PadLeft(padding));
            else sb.AppendLine("Sin ventas".PadRight(ancho));
            sb.AppendLine(new string('-', ancho));

            if (totalEntradas > 0 || totalSalidas > 0)
            {
                sb.AppendLine(Centrar("MOVIMIENTOS", ancho));
                sb.AppendLine(new string('-', ancho));
                sb.AppendLine("Entradas:".PadRight(maxLbl) + totalEntradas.ToString("0.00").PadLeft(padding));
                sb.AppendLine("Salidas:".PadRight(maxLbl) + totalSalidas.ToString("0.00").PadLeft(padding));
                sb.AppendLine(new string('-', ancho));
            }

            sb.AppendLine(Centrar("EFECTIVO ESPERADO", ancho));
            sb.AppendLine(new string('-', ancho));
            sb.AppendLine("Inicial:".PadRight(maxLbl) + efectivoInicial.ToString("0.00").PadLeft(padding));
            sb.AppendLine("+ Cobrado:".PadRight(maxLbl) + efectivoCobrado.ToString("0.00").PadLeft(padding));
            sb.AppendLine("+ Entradas:".PadRight(maxLbl) + totalEntradas.ToString("0.00").PadLeft(padding));
            sb.AppendLine("- Salidas:".PadRight(maxLbl) + totalSalidas.ToString("0.00").PadLeft(padding));
            sb.AppendLine(new string('-', ancho));
            sb.AppendLine("= ESPERADO:".PadRight(maxLbl) + efectivoEsperado.ToString("0.00").PadLeft(padding));
            sb.AppendLine("CONTADO:".PadRight(maxLbl) + efectivoContado.ToString("0.00").PadLeft(padding));

            string estadoCierre = Math.Abs(diferencia) < 0.01m ? "CUADRA" : (diferencia > 0 ? "SOBRANTE " + diferencia.ToString("0.00") : "FALTANTE " + Math.Abs(diferencia).ToString("0.00"));
            sb.AppendLine("DIFERENCIA:".PadRight(maxLbl) + estadoCierre.PadLeft(padding));
            sb.AppendLine(new string('=', ancho));
            sb.AppendLine(Centrar("Cierre completado", ancho));
            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }
    }
}