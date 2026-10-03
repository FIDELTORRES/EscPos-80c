# 📝 Changelog

Todos los cambios notables en este proyecto se documentarán en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto se adhiere al [Versionado Semántico](https://semver.org/lang/es/).

---

## [No publicado]

### Por agregar
- Soporte para impresoras de 58mm
- Endpoint de reimpresión de tickets
- Apertura automática del cajón de dinero
- Múltiples copias configurables

---

## [1.0.0] - 2026-10-02

### ✨ Agregado
- Impresión ESC/POS nativa para impresoras POS-80C
- Servicio Windows autoinstalable
- Endpoint `POST /Print` para tickets de venta
- Endpoint `POST /CajaReport` para apertura y cierre de caja
- Soporte para TICKET, BOLETA y FACTURA
- Multi-medio de pago (Efectivo, Yape, Plin, Tarjeta)
- QR nativo ESC/POS (nítido y rápido)
- Corte automático de papel
- Datos del cliente (DNI/RUC + ubigeo)
- Monto en letras (ej: "VEINTE Y 50/100 SOLES")
- Impuesto ICBPER por item
- Guardado de JSONs en disco (auditoría)
- Multi-medio en ticket (YAPE + EFECTIVO + PLIN)
- Formato de fecha y hora en el ticket
- Logging de operaciones

### 🔧 Técnico
- Migración completa de `Graphics.DrawString` a comandos ESC/POS
- Implementación de `RawPrinterHelper` para envío RAW
- Implementación de `EscPosBuilder` para construcción de comandos
- Modelos separados para venta y caja
- Soporte .NET 8.0 self-contained
- CORS configurado para Django local

---

## Formato de versiones

- **MAJOR** (X.0.0): Cambios incompatibles en la API
- **MINOR** (0.X.0): Nueva funcionalidad compatible
- **PATCH** (0.0.X): Corrección de bugs compatible

## Tipos de cambios

- `✨ Agregado` — Nuevas características
- `🔧 Cambiado` — Cambios en funcionalidad existente
- `🐛 Corregido` — Corrección de bugs
- `🗑️ Eliminado` — Funcionalidad removida
- `🔒 Seguridad` — Correcciones de seguridad
- `📝 Documentación` — Cambios solo en documentación

---

[No publicado]: https://github.com/FIDELTORRES/EscPos-80c/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/FIDELTORRES/EscPos-80c/releases/tag/v1.0.0