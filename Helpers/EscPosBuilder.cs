using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MiProyecto.Helpers
{
    /// <summary>
    /// ═══════════════════════════════════════════════════════════════
    /// 📌 ESC/POS BUILDER
    /// ═══════════════════════════════════════════════════════════════
    /// Constructor de comandos ESC/POS para impresoras térmicas.
    ///
    /// Uso típico:
    ///   var p = new EscPosBuilder();
    ///   p.Inicializar();
    ///   p.Centrar();
    ///   p.Negrita(true);
    ///   p.Texto("MI EMPRESA");
    ///   p.Negrita(false);
    ///   p.AlinearIzquierda();
    ///   p.Texto("RUC: 10413820532");
    ///   p.Linea('=');
    ///   ...
    ///   p.Cortar();
    ///   byte[] data = p.ToBytes();
    ///
    /// Comandos ESC/POS usados (compatibles con Epson TM-T20, POS-80C,
    /// 3nStar, etc.):
    ///   ESC @     → inicializar
    ///   ESC a n   → alinear (0=izq, 1=centro, 2=der)
    ///   ESC E n   → negrita on/off
    ///   GS ! n    → tamaño de letra (n = 0x00, 0x11, 0x22...)
    ///   GS V 0    → cortar papel
    ///   GS ( k    → imprimir QR nativo
    ///   LF (0x0A) → salto de línea
    /// </summary>
    public class EscPosBuilder
    {
        // ═══════════════════════════════════════════════════════════════
        // 📌 CONSTANTES
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Ancho de línea para papel 80mm (48 caracteres).</summary>
        public const int ANCHO_80MM = 48;

        /// <summary>Ancho de línea para papel 58mm (32 caracteres).</summary>
        public const int ANCHO_58MM = 32;

        // ═══════════════════════════════════════════════════════════════
        // 📌 BUFFER INTERNO
        // ═══════════════════════════════════════════════════════════════

        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly Encoding _encoding;

        public EscPosBuilder()
        {
            // CP437 es el code page estándar de las impresoras térmicas.
            // Soporta acentos y caracteres especiales latinos.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                _encoding = Encoding.GetEncoding(437);
            }
            catch
            {
                // Fallback: Latin-1 (ISO-8859-1)
                _encoding = Encoding.GetEncoding("ISO-8859-1");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 📌 MÉTODO PRINCIPAL: OBTENER BYTES
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Devuelve el array de bytes con todos los comandos acumulados.</summary>
        public byte[] ToBytes()
        {
            return _buffer.ToArray();
        }

        // ═══════════════════════════════════════════════════════════════
        // 📌 ESCRITURA BAJA (bytes)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Escribe bytes directos al buffer.</summary>
        private void EscribirBytes(params byte[] bytes)
        {
            _buffer.Write(bytes, 0, bytes.Length);
        }

        /// <summary>Escribe un texto codificado en el buffer.</summary>
        private void EscribirTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;
            byte[] bytes = _encoding.GetBytes(texto);
            _buffer.Write(bytes, 0, bytes.Length);
        }

        // ═══════════════════════════════════════════════════════════════
        // 📌 COMANDOS ESC/POS BÁSICOS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// ESC @ → Inicializa la impresora.
        /// Limpia el buffer interno y deja la impresora en estado por defecto.
        /// SIEMPRE llamar al inicio de un ticket.
        /// </summary>
        public EscPosBuilder Inicializar()
        {
            EscribirBytes(0x1B, 0x40);   // ESC @
            return this;
        }

        /// <summary>
        /// ESC a 0 → Alinea a la izquierda.
        /// </summary>
        public EscPosBuilder AlinearIzquierda()
        {
            EscribirBytes(0x1B, 0x61, 0x00);
            return this;
        }

        /// <summary>
        /// ESC a 1 → Centrar texto.
        /// </summary>
        public EscPosBuilder Centrar()
        {
            EscribirBytes(0x1B, 0x61, 0x01);
            return this;
        }

        /// <summary>
        /// ESC a 2 → Alinea a la derecha.
        /// </summary>
        public EscPosBuilder AlinearDerecha()
        {
            EscribirBytes(0x1B, 0x61, 0x02);
            return this;
        }

        /// <summary>
        /// ESC E n → Activa o desactiva negrita.
        /// </summary>
        public EscPosBuilder Negrita(bool activa)
        {
            EscribirBytes(0x1B, 0x45, (byte)(activa ? 1 : 0));
            return this;
        }

        /// <summary>
        /// ESC - n → Activa o desactiva subrayado.
        /// </summary>
        public EscPosBuilder Subrayado(bool activa)
        {
            EscribirBytes(0x1B, 0x2D, (byte)(activa ? 1 : 0));
            return this;
        }

        /// <summary>
        /// GS ! n → Cambia el tamaño de la letra.
        /// n:
        ///   0x00 → normal
        ///   0x11 → doble ancho
        ///   0x22 → doble ancho y doble alto
        ///   0x33 → triple ancho y triple alto
        /// </summary>
        public EscPosBuilder TamañoLetra(byte n = 0x00)
        {
            EscribirBytes(0x1D, 0x21, n);
            return this;
        }

        /// <summary>
        /// ESC d n → Avanza n líneas en blanco.
        /// </summary>
        public EscPosBuilder AvanzarLineas(int n)
        {
            if (n < 0) n = 0;
            if (n > 255) n = 255;
            EscribirBytes(0x1B, 0x64, (byte)n);
            return this;
        }

        // ═══════════════════════════════════════════════════════════════
        // 📌 ESCRITURA DE TEXTO
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Escribe un texto SIN salto de línea.
        /// </summary>
        public EscPosBuilder Texto(string texto)
        {
            EscribirTexto(texto ?? "");
            return this;
        }

        /// <summary>
        /// Escribe un texto CON salto de línea al final.
        /// </summary>
        public EscPosBuilder Linea(string texto = "")
        {
            EscribirTexto(texto ?? "");
            EscribirBytes(0x0A);   // LF
            return this;
        }

        /// <summary>
        /// Escribe n saltos de línea.
        /// </summary>
        public EscPosBuilder SaltoLinea(int n = 1)
        {
            for (int i = 0; i < n; i++)
                EscribirBytes(0x0A);
            return this;
        }

        /// <summary>
        /// Dibuja una línea completa de un carácter repetido.
        /// Ej: Linea('=')  →  ================================================
        /// </summary>
        public EscPosBuilder Linea(char caracter, int ancho = ANCHO_80MM)
        {
            string s = new string(caracter, ancho);
            EscribirTexto(s);
            EscribirBytes(0x0A);
            return this;
        }

        // ═══════════════════════════════════════════════════════════════
        // 📌 FORMATEO (PADDING / COLUMNAS)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Centra un texto en el ancho especificado (agregando espacios).
        /// </summary>
        public static string CentrarTexto(string texto, int ancho = ANCHO_80MM)
        {
            texto ??= "";
            if (texto.Length >= ancho)
                return texto.Substring(0, ancho);

            int espacios = (ancho - texto.Length) / 2;
            return new string(' ', espacios) + texto;
        }

        /// <summary>
        /// Alinea un texto a la derecha del ancho especificado.
        /// </summary>
        public static string AlinearDerechaTexto(string texto, int ancho = ANCHO_80MM)
        {
            texto ??= "";
            if (texto.Length >= ancho)
                return texto.Substring(0, ancho);

            return texto.PadLeft(ancho);
        }

        /// <summary>
        /// Formatea una fila con 2 columnas: etiqueta a la izquierda,
        /// valor a la derecha. Ej:
        ///   FormatoFila("Total:", "10.00", 48)
        ///   → "Total:                                      10.00"
        /// </summary>
        public static string FormatoFila(string etiqueta, string valor, int ancho = ANCHO_80MM)
        {
            etiqueta ??= "";
            valor ??= "";
            int espacios = ancho - etiqueta.Length - valor.Length;
            if (espacios < 1) espacios = 1;
            return etiqueta + new string(' ', espacios) + valor;
        }

        /// <summary>
        /// Formatea una fila con 3 columnas de ancho fijo.
        /// Ej:
        ///   Formato3Cols("S/ 10.00", "x1", "S/ 10.00", 14, 8, 26)
        ///   → "S/ 10.00     x1      S/ 10.00"
        /// </summary>
        public static string Formato3Cols(
            string col1, string col2, string col3,
            int ancho1, int ancho2, int ancho3)
        {
            col1 ??= "";
            col2 ??= "";
            col3 ??= "";

            if (col1.Length > ancho1) col1 = col1.Substring(0, ancho1);
            if (col2.Length > ancho2) col2 = col2.Substring(0, ancho2);
            if (col3.Length > ancho3) col3 = col3.Substring(0, ancho3);

            return col1.PadRight(ancho1) + col2.PadRight(ancho2) + col3.PadLeft(ancho3);
        }

        /// <summary>
        /// Formatea un monto a 2 decimales con cultura invariante.
        /// </summary>
        public static string FormatoMonto(decimal monto)
        {
            return monto.ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formatea un monto con prefijo "S/ ".
        /// </summary>
        public static string FormatoSoles(decimal monto)
        {
            return "S/ " + FormatoMonto(monto);
        }

        // ═══════════════════════════════════════════════════════════════
        // 📌 CÓDIGO QR NATIVO (GS ( k)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera un código QR usando el comando nativo ESC/POS.
        /// Requiere: impresora compatible con GS ( k (Epson TM, 3nStar, POS-80C).
        ///
        /// Params:
        ///   contenido  → texto a codificar (ej: "10413820532-TICK-123")
        ///   tamaño     → tamaño del módulo QR (1-16, por defecto 6)
        ///   correccion → nivel de corrección (L=48, M=49, Q=50, H=51)
        /// </summary>
        public EscPosBuilder CodigoQR(string contenido, byte tamaño = 6, byte correccion = 49)
        {
            contenido ??= "";

            byte[] data = _encoding.GetBytes(contenido);
            int len = data.Length + 3;

            byte pL = (byte)(len & 0xFF);
            byte pH = (byte)((len >> 8) & 0xFF);

            // ─── 1. Establecer modelo QR ───
            EscribirBytes(0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00);

            // ─── 2. Establecer tamaño del módulo ───
            EscribirBytes(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, tamaño);

            // ─── 3. Establecer nivel de corrección ───
            EscribirBytes(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, correccion);

            // ─── 4. Guardar los datos del QR ───
            EscribirBytes(0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30);
            EscribirBytes(data);

            // ─── 5. Imprimir el QR ───
            EscribirBytes(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30);

            return this;
        }

        // ═══════════════════════════════════════════════════════════════
        // 📌 CORTAR PAPEL
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// GS V m → Corta el papel.
        /// m = 0: corte completo
        /// m = 1: corte parcial
        /// </summary>
        public EscPosBuilder Cortar(byte modo = 0)
        {
            // Avanza 3 líneas antes de cortar (para que el contenido
            // no quede pegado al borde cortado)
            AvanzarLineas(3);
            EscribirBytes(0x1D, 0x56, modo);
            return this;
        }

        /// <summary>
        /// Abre el cajón de dinero (si la impresora tiene el conector RJ11).
        /// </summary>
        public EscPosBuilder AbrirCajon()
        {
            // ESC p m t1 t2 → Pulso al pin 2 del conector del cajón
            EscribirBytes(0x1B, 0x70, 0x00, 0x19, 0xFA);
            return this;
        }
    }
}