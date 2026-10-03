using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiProyecto.Models
{
    public class PrintTicketRequest
    {

        public string? UrlJson { get; set; }

        public string? NombreJson { get; set; }
        public string? ArchivoJson { get; set; }
        
        [JsonPropertyName("documento")]
        public Documento Documento { get; set; } = new Documento();

        [JsonPropertyName("emisor")]
        public Emisor Emisor { get; set; } = new Emisor();

        [JsonPropertyName("cliente")]
        public Cliente Cliente { get; set; } = new Cliente();

        [JsonPropertyName("notas")]
        public List<Nota> Notas { get; set; } = new List<Nota>();

        [JsonPropertyName("tipopago")]
        public TipoPago TipoPago { get; set; } = new TipoPago();

        [JsonPropertyName("detallecuotas")]
        public List<object> DetalleCuotas { get; set; } = new List<object>();

        [JsonPropertyName("impuestos")]
        public Impuestos Impuestos { get; set; } = new Impuestos();

        [JsonPropertyName("items")]
        public List<Item> Items { get; set; } = new List<Item>();

        [JsonPropertyName("total")]
        public Total Total { get; set; } = new Total();

        [JsonPropertyName("idpedidoweb")]
        public string IdPedidoWeb { get; set; } = "";

        // Nuevos campos para impresión del ticket
        [JsonPropertyName("cajero")]
        public string Cajero { get; set; } = "";

        [JsonPropertyName("vendedor")]
        public string Vendedor { get; set; } = "";

        [JsonPropertyName("nrocuotas")]
        public string Nrocuotas { get; set; } = "";

        [JsonPropertyName("nrocaja")]
        public string NroCaja { get; set; } = "";

        [JsonPropertyName("idcajero")]
        public string IdCajero { get; set; } = "";

        [JsonPropertyName("tipopagos")]
        public List<TipopagoItem> Tipopagos { get; set; } = new List<TipopagoItem>();

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = "";
    }

    public class TipopagoItem
    {
        [JsonPropertyName("medio")]
        public string Medio { get; set; } = "";

        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }

        [JsonPropertyName("nro_operacion")]
        public string NroOperacion { get; set; } = "";

        [JsonPropertyName("dinero_entregado")]
        public decimal DineroEntregado { get; set; }

        [JsonPropertyName("vuelto")]
        public decimal Vuelto { get; set; }
    }
    public class Documento
    {
        [JsonPropertyName("numero")]
        public string Numero { get; set; } = "";

        [JsonPropertyName("fecha_emision")]
        public string FechaEmision { get; set; } = "";

        [JsonPropertyName("hora_emision")]
        public string HoraEmision { get; set; } = "";

        [JsonPropertyName("tipo_factura")]
        public string TipoFactura { get; set; } = "";

        [JsonPropertyName("moneda")]
        public string Moneda { get; set; } = "";

        [JsonPropertyName("tipo_transaccion")]
        public string TipoTransaccion { get; set; } = "";

        [JsonPropertyName("numero_items")]
        public int NumeroItems { get; set; }

        [JsonPropertyName("orden_compra")]
        public int OrdenCompra { get; set; }
    }

    public class Emisor
    {
        [JsonPropertyName("documento")]
        public DocumentoEmisor Documento { get; set; } = new DocumentoEmisor();

        [JsonPropertyName("datos")]
        public Datos Datos { get; set; } = new Datos();

        [JsonPropertyName("ubicacion")]
        public Ubicacion Ubicacion { get; set; } = new Ubicacion();
    }

    public class Cliente
    {
        [JsonPropertyName("documento")]
        public DocumentoCliente Documento { get; set; } = new DocumentoCliente();

        [JsonPropertyName("datos")]
        public Datos Datos { get; set; } = new Datos();

        [JsonPropertyName("ubicacion")]
        public Ubicacion Ubicacion { get; set; } = new Ubicacion();
    }

    public class Datos
    {
        [JsonPropertyName("razon_social")]
        public string RazonSocial { get; set; } = "";

        [JsonPropertyName("nombre_comercial")]
        public string NombreComercial { get; set; } = "";
    }

    public class DocumentoEmisor
    {
        [JsonPropertyName("numero")]
        public string Numero { get; set; } = "";

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "";
    }

    public class DocumentoCliente
    {
        [JsonPropertyName("numero")]
        public string Numero { get; set; } = "";

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "";
    }

    public class Ubicacion
    {
        [JsonPropertyName("ubigeo")]
        public string? Ubigeo { get; set; }

        [JsonPropertyName("anexo")]
        public string? Anexo { get; set; }

        [JsonPropertyName("direccion")]
        public string Direccion { get; set; } = "";

        [JsonPropertyName("urbanizacion")]
        public string Urbanizacion { get; set; } = "";

        [JsonPropertyName("provincia")]
        public string Provincia { get; set; } = "";

        [JsonPropertyName("departamento")]
        public string Departamento { get; set; } = "";

        [JsonPropertyName("distrito")]
        public string Distrito { get; set; } = "";

        [JsonPropertyName("pais")]
        public string Pais { get; set; } = "";
    }

    public class Nota
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("valor")]
        public string Valor { get; set; } = "";
    }

    public class TipoPago
    {
        [JsonPropertyName("forma_pago")]
        public string FormaPago { get; set; } = "";

        [JsonPropertyName("monto_total")]
        public string MontoTotal { get; set; } = "";
    }

    public class Impuestos
    {
        [JsonPropertyName("total_ope_gravadas")]
        public string TotalOpeGravadas { get; set; } = "";

        [JsonPropertyName("total_igv_gravadas")]
        public string TotalIgvGravadas { get; set; } = "";

        [JsonPropertyName("total_ope_gratuitas")]
        public string TotalOpeGratuitas { get; set; } = "";

        [JsonPropertyName("total_igv_gratuitas")]
        public string TotalIgvGratuitas { get; set; } = "";

        [JsonPropertyName("total_ope_exoneradas")]
        public string TotalOpeExoneradas { get; set; } = "";

        [JsonPropertyName("total_ope_inafectas")]
        public string TotalOpeInafectas { get; set; } = "";

        [JsonPropertyName("total_ope_icbper")]
        public string TotalOpeIcbper { get; set; } = "";
    }

    public class Item
    {
        [JsonPropertyName("item")]
        public int ItemNumber { get; set; }

        [JsonPropertyName("codigo_producto")]
        public string CodigoProducto { get; set; } = "";

        [JsonPropertyName("nombre_producto")]
        public string NombreProducto { get; set; } = "";

        [JsonPropertyName("codigo_afectacion")]
        public string CodigoAfectacion { get; set; } = "";

        [JsonPropertyName("cantidad")]
        public string Cantidad { get; set; } = "";

        [JsonPropertyName("unidad_medida")]
        public string UnidadMedida { get; set; } = "";

        [JsonPropertyName("valor_unitario")]
        public string ValorUnitario { get; set; } = "";

        [JsonPropertyName("precio_unitario")]
        public string PrecioUnitario { get; set; } = "";

        [JsonPropertyName("precio_referencial")]
        public string PrecioReferencial { get; set; } = "";

        [JsonPropertyName("descuento")]
        public string Descuento { get; set; } = "";

        [JsonPropertyName("valor_venta")]
        public string ValorVenta { get; set; } = "";

        [JsonPropertyName("porcentaje_igv")]
        public string PorcentajeIgv { get; set; } = "";

        [JsonPropertyName("total_igv")]
        public string TotalIgv { get; set; } = "";

        [JsonPropertyName("total_isc")]
        public string TotalIsc { get; set; } = "";

        [JsonPropertyName("tasa_icbper")]
        public string? TasaIcbper { get; set; }

        [JsonPropertyName("total_icbper")]
        public string TotalIcbper { get; set; } = "";
    }

    public class Total
    {
        [JsonPropertyName("lineas")]
        public string Lineas { get; set; } = "";

        [JsonPropertyName("total_impuestos")]
        public string TotalImpuestos { get; set; } = "";

        [JsonPropertyName("descuentoGlobal")]
        public string DescuentoGlobal { get; set; } = "";

        [JsonPropertyName("motivoDsctoGlobal")]
        public string MotivoDsctoGlobal { get; set; } = "";

        [JsonPropertyName("descuento")]
        public string Descuento { get; set; } = "";

        [JsonPropertyName("cargo")]
        public string Cargo { get; set; } = "";

        [JsonPropertyName("icbper")]
        public string Icbper { get; set; } = "";

        [JsonPropertyName("pagable")]
        public string Pagable { get; set; } = "";
    }

}
