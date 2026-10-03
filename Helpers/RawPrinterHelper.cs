using System;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;

namespace MiProyecto.Helpers
{
    /// <summary>
    /// ═══════════════════════════════════════════════════════════════
    /// 📌 RAW PRINTER HELPER
    /// ═══════════════════════════════════════════════════════════════
    /// Envía bytes RAW (comandos ESC/POS) directamente a la impresora
    /// sin pasar por el driver de impresión de Windows.
    ///
    /// Esto permite:
    ///   - Imprimir comandos ESC/POS nativos
    ///   - Respetar el font interno monoespaciado de la impresora
    ///   - Alinear columnas perfectamente
    ///   - Usar negritas, tamaño doble, etc.
    ///   - Cortar papel automáticamente
    ///
    /// Uso:
    ///   byte[] data = EscPosBuilder.CrearTicket();
    ///   RawPrinterHelper.SendBytesToPrinter("POS-80C", data);
    /// </summary>
    public static class RawPrinterHelper
    {
        // ═══════════════════════════════════════════════════════════════
        // 📌 ESTRUCTURAS Y DLL IMPORTS DE WINDOWS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Estructura que describe el documento a imprimir.
        /// Windows la usa para saber qué tipo de datos enviamos.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        // DLL de Windows que maneja la cola de impresión
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA",
                   SetLastError = true, CharSet = CharSet.Ansi,
                   ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(
            [MarshalAs(UnmanagedType.LPStr)] string szPrinter,
            out IntPtr hPrinter,
            IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter",
                   SetLastError = true, ExactSpelling = true,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA",
                   SetLastError = true, CharSet = CharSet.Ansi,
                   ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(
            IntPtr hPrinter,
            int level,
            [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter",
                   SetLastError = true, ExactSpelling = true,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter",
                   SetLastError = true, ExactSpelling = true,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter",
                   SetLastError = true, ExactSpelling = true,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter",
                   SetLastError = true, ExactSpelling = true,
                   CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(
            IntPtr hPrinter,
            IntPtr pBytes,
            int dwCount,
            out int dwWritten);

        // ═══════════════════════════════════════════════════════════════
        // 📌 MÉTODO PRINCIPAL: ENVIAR BYTES A IMPRESORA
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Envía un array de bytes RAW a la impresora especificada.
        ///
        /// Params:
        ///   printerName → nombre exacto de la impresora (ej: "POS-80C")
        ///   bytes       → bytes a enviar (comandos ESC/POS + texto)
        ///
        /// Returns:
        ///   true si se envió OK, false si falla
        /// </summary>
        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                throw new ArgumentException("Nombre de impresora vacío", nameof(printerName));

            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("Bytes vacíos", nameof(bytes));

            IntPtr hPrinter = IntPtr.Zero;
            int dwWritten = 0;
            IntPtr pBytes = IntPtr.Zero;

            try
            {
                // 1) Abrir la impresora
                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    throw new Exception(
                        $"No se pudo abrir la impresora '{printerName}'. " +
                        $"Verifica que esté instalada y el nombre sea correcto.");
                }

                // 2) Iniciar documento
                DOCINFOA di = new DOCINFOA
                {
                    pDocName = "Ticket POS",
                    pDataType = "RAW"    // ← CLAVE: RAW permite enviar ESC/POS
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                {
                    throw new Exception("StartDocPrinter falló");
                }

                // 3) Iniciar página
                if (!StartPagePrinter(hPrinter))
                {
                    throw new Exception("StartPagePrinter falló");
                }

                // 4) Copiar los bytes a memoria no administrada
                pBytes = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, pBytes, bytes.Length);

                // 5) Enviar bytes a la impresora
                if (!WritePrinter(hPrinter, pBytes, bytes.Length, out dwWritten))
                {
                    throw new Exception("WritePrinter falló");
                }

                // 6) Cerrar página y documento
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);

                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                // Liberar memoria
                if (pBytes != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pBytes);

                // Cerrar impresora
                if (hPrinter != IntPtr.Zero)
                    ClosePrinter(hPrinter);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 📌 MÉTODO AUXILIAR: BUSCAR IMPRESORA POR NOMBRE
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Busca una impresora por nombre (búsqueda case-insensitive).
        /// Si la encuentra, devuelve el nombre exacto como está en Windows.
        /// Si no la encuentra, devuelve null.
        /// </summary>
        public static string BuscarImpresora(string nombreBuscado)
        {
            if (string.IsNullOrWhiteSpace(nombreBuscado))
                return null;

            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                if (string.Equals(p, nombreBuscado, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }

            // Segunda pasada: búsqueda parcial (contiene el texto)
            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                if (p.IndexOf(nombreBuscado, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return p;
                }
            }

            return null;
        }

        /// <summary>
        /// Devuelve la impresora por defecto del sistema.
        /// </summary>
        public static string ObtenerImpresoraPorDefecto()
        {
            var ps = new PrinterSettings();
            return ps.PrinterName;
        }
    }
}