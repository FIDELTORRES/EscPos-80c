using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MiProyecto.Models
{
    /// <summary>
    /// Modelo de entrada para impresión de reportes de caja
    /// (Apertura y Cierre).
    ///
    /// Se envía desde Django al endpoint POST /CajaReport
    /// </summary>
    public class CajaReportRequest
    {
        // ══════════════════════════════════════════════════════════
        // CAMPOS DE CONTROL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Discriminador: "APERTURA" | "CIERRE"
        /// </summary>
        [JsonPropertyName("tipo_reporte")]
        public string TipoReporte { get; set; } = "APERTURA";

        // Nombre del archivo JSON a guardar
        public string? NombreJson { get; set; }

        // Contenido JSON (para compatibilidad)
        public string? ArchivoJson { get; set; }

        // ══════════════════════════════════════════════════════════
        // DOCUMENTO (cabecera)
        // ══════════════════════════════════════════════════════════
        [JsonPropertyName("documento")]
        public DocumentoCaja Documento { get; set; } = new DocumentoCaja();

        // ══════════════════════════════════════════════════════════
        // EMISOR (empresa)
        // ══════════════════════════════════════════════════════════
        [JsonPropertyName("emisor")]
        public EmisorCaja Emisor { get; set; } = new EmisorCaja();

        // ══════════════════════════════════════════════════════════
        // DATOS DE LA CAJA
        // ══════════════════════════════════════════════════════════
        [JsonPropertyName("caja")]
        public CajaInfo Caja { get; set; } = new CajaInfo();

        // ══════════════════════════════════════════════════════════
        // RESUMEN (apertura o cierre según tipo_reporte)
        // ══════════════════════════════════════════════════════════
        [JsonPropertyName("resumen")]
        public ResumenCaja Resumen { get; set; } = new ResumenCaja();

        // ══════════════════════════════════════════════════════════
        // DESGLOSE POR MEDIO (solo CIERRE)
        // ══════════════════════════════════════════════════════════
        [JsonPropertyName("desglose_medios")]
        public List<MedioPagoCaja> DesgloseMedios { get; set; } = new List<MedioPagoCaja>();

        // ══════════════════════════════════════════════════════════
        // MOVIMIENTOS DE CAJA (solo CIERRE)
        // ══════════════════════════════════════════════════════════
        [JsonPropertyName("movimientos")]
        public MovimientosCaja Movimientos { get; set; } = new MovimientosCaja();

        // ══════════════════════════════════════════════════════════
        // MONEDERO (apertura y cierre)
        // ══════════════════════════════════════════════════════════
        [JsonPropertyName("monedero")]
        public List<MonederoItem> Monedero { get; set; } = new List<MonederoItem>();
    }

    // ══════════════════════════════════════════════════════════
    // CLASES AUXILIARES
    // ══════════════════════════════════════════════════════════

    public class DocumentoCaja
    {
        [JsonPropertyName("numero")]
        public string Numero { get; set; } = "";

        [JsonPropertyName("fecha_emision")]
        public string FechaEmision { get; set; } = "";

        [JsonPropertyName("hora_emision")]
        public string HoraEmision { get; set; } = "";

        [JsonPropertyName("moneda")]
        public string Moneda { get; set; } = "PEN";
    }

    public class EmisorCaja
    {
        [JsonPropertyName("documento")]
        public DocumentoEmisorCaja Documento { get; set; } = new DocumentoEmisorCaja();

        [JsonPropertyName("datos")]
        public DatosEmisorCaja Datos { get; set; } = new DatosEmisorCaja();

        [JsonPropertyName("ubicacion")]
        public UbicacionEmisorCaja Ubicacion { get; set; } = new UbicacionEmisorCaja();
    }

    public class DocumentoEmisorCaja
    {
        [JsonPropertyName("numero")]
        public string Numero { get; set; } = "";

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "6";
    }

    public class DatosEmisorCaja
    {
        [JsonPropertyName("razon_social")]
        public string RazonSocial { get; set; } = "";

        [JsonPropertyName("nombre_comercial")]
        public string NombreComercial { get; set; } = "";
    }

    public class UbicacionEmisorCaja
    {
        [JsonPropertyName("direccion")]
        public string Direccion { get; set; } = "";

        [JsonPropertyName("urbanizacion")]
        public string Urbanizacion { get; set; } = "";

        [JsonPropertyName("distrito")]
        public string Distrito { get; set; } = "";

        [JsonPropertyName("provincia")]
        public string Provincia { get; set; } = "";

        [JsonPropertyName("departamento")]
        public string Departamento { get; set; } = "";

        [JsonPropertyName("pais")]
        public string Pais { get; set; } = "PE";
    }

    public class CajaInfo
    {
        [JsonPropertyName("idcaja")]
        public int IdCaja { get; set; }

        [JsonPropertyName("nrocaja")]
        public string NroCaja { get; set; } = "";

        [JsonPropertyName("nombre_caja")]
        public string NombreCaja { get; set; } = "";

        [JsonPropertyName("nombre_local")]
        public string NombreLocal { get; set; } = "";

        [JsonPropertyName("usuario")]
        public string Usuario { get; set; } = "";

        [JsonPropertyName("turno")]
        public string Turno { get; set; } = "";

        [JsonPropertyName("fecha_apertura")]
        public string FechaApertura { get; set; } = "";

        [JsonPropertyName("fecha_cierre")]
        public string FechaCierre { get; set; } = "";

        [JsonPropertyName("moneda")]
        public string Moneda { get; set; } = "NUEVO SOL";
    }

    public class ResumenCaja
    {
        // ─── Solo APERTURA ───
        [JsonPropertyName("monto_inicial")]
        public decimal MontoInicial { get; set; } = 0m;

        // ─── Solo CIERRE ───
        [JsonPropertyName("cant_ventas")]
        public int CantVentas { get; set; } = 0;

        [JsonPropertyName("total_ventas")]
        public decimal TotalVentas { get; set; } = 0m;

        [JsonPropertyName("total_igv")]
        public decimal TotalIgv { get; set; } = 0m;

        [JsonPropertyName("total_exonerado")]
        public decimal TotalExonerado { get; set; } = 0m;

        [JsonPropertyName("total_inafecto")]
        public decimal TotalInafecto { get; set; } = 0m;

        [JsonPropertyName("total_icbper")]
        public decimal TotalIcbper { get; set; } = 0m;

        [JsonPropertyName("efectivo_inicial")]
        public decimal EfectivoInicial { get; set; } = 0m;

        [JsonPropertyName("efectivo_cobrado")]
        public decimal EfectivoCobrado { get; set; } = 0m;

        [JsonPropertyName("efectivo_esperado")]
        public decimal EfectivoEsperado { get; set; } = 0m;

        [JsonPropertyName("efectivo_contado")]
        public decimal EfectivoContado { get; set; } = 0m;

        [JsonPropertyName("diferencia")]
        public decimal Diferencia { get; set; } = 0m;

        [JsonPropertyName("estado_cuadre")]
        public string EstadoCuadre { get; set; } = "CUADRA";
    }

    public class MedioPagoCaja
    {
        [JsonPropertyName("medio")]
        public string Medio { get; set; } = "";

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; } = 0;

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; } = 0m;
    }

    public class MovimientosCaja
    {
        [JsonPropertyName("entradas")]
        public decimal Entradas { get; set; } = 0m;

        [JsonPropertyName("salidas")]
        public decimal Salidas { get; set; } = 0m;
    }

    public class MonederoItem
    {
        [JsonPropertyName("denominacion")]
        public decimal Denominacion { get; set; } = 0m;

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; } = 0;

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; } = 0m;
    }
}