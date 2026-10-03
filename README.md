📄 ARCHIVO 1: README.md
Va en la raíz del proyecto.

markdown
# 🖨️ EscPos-80c

> **API .NET para impresión en impresoras térmicas POS-80C usando comandos ESC/POS**
>
> Servicio Windows que expone endpoints HTTP para imprimir tickets de venta, apertura y cierre de caja desde cualquier aplicación (Django, Node.js, etc.)

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-Service-blue.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/FIDELTORRES/EscPos-80c.svg?style=social)](https://github.com/FIDELTORRES/EscPos-80c/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/FIDELTORRES/EscPos-80c.svg?style=social)](https://github.com/FIDELTORRES/EscPos-80c/network)

---

## 📋 Tabla de contenidos

- [Descripción](#-descripción)
- [Características](#-características)
- [Arquitectura](#-arquitectura)
- [Requisitos](#-requisitos)
- [Instalación](#-instalación)
- [Configuración](#-configuración)
- [Endpoints](#-endpoints)
- [Estructura del JSON](#-estructura-del-json)
- [Ejemplos de uso](#-ejemplos-de-uso)
- [Troubleshooting](#-troubleshooting)
- [Changelog](#-changelog)
- [Autor](#-autor)
- [Licencia](#-licencia)

---

## 📋 Descripción

`EscPos-80c` es un **servicio Windows** desarrollado en **.NET 8** que actúa como puente entre tu sistema de ventas (POS) y la impresora térmica **POS-80C** (o cualquier impresora compatible con ESC/POS).

### ❌ Problema que resuelve

- `Graphics.DrawString` de .NET **colapsa los espacios** → alineación rota
- Fuentes de Windows **no son monoespaciadas** en impresora térmica
- **Sin corte automático** de papel
- **QR borroso** enviado como bitmap
- Dependencia del driver de Windows (lento, con buffers)

### ✅ Solución

- Comandos **ESC/POS nativos** (font interno monoespaciado)
- **Corte automático** de papel
- **QR nativo** (nítido y rápido)
- **Negritas, tamaños y alineación** sin problemas
- Impresión **RAW** (sin driver de Windows)

---

## ✨ Características

| Característica | Descripción |
|-----------------------------|-----------------------------------------------------|
| 🎫 **Tickets de venta**     | TICKET, BOLETA y FACTURA                            |
| 📊 **Reportes de caja**     | APERTURA y CIERRE                                   |
| 💳 **Multi-medio de pago**  | Efectivo, Yape, Plin, Tarjeta                       |
| 📝 **Monto en letras**      | "VEINTE Y 50/100 SOLES"                             |
| 🔢 **ICBPER**               | Impuesto a bolsas plásticas                         |  
| 🏢 **Datos del cliente**    | DNI/RUC + ubigeo (distrito/provincia/departamento)  |
| 📱 **QR nativo ESC/POS**     | Generado por la impresora                           |
| ✂️ **Corte automático**     | Comando `GS V 0`                                    |
| 🖨️ **Servicio Windows**     | Arranca automáticamente con el sistema              |
| 🌐 **API HTTP REST**        | Fácil integración con cualquier lenguaje            |

---

## 🏗️ Arquitectura
┌─────────────────┐ ┌──────────────────┐ ┌──────────────┐
│ Cliente         │ HTTP │ API .NET │ RAW │ Impresora │
│ (Django)        │ ──────▶ │ (localhost:5183)│ ──────▶ │ POS-80C │
└─────────────────┘ └──────────────────┘ └──────────────┘
│
▼
┌──────────────┐
│ JSON en      │
│ disco        │
│ (auditoría)  │
└──────────────┘

text

### 📁 Estructura del proyecto
EscPos-80c/
├── Controllers/
│ ├── PrintController.cs # Endpoint /Print (ventas)
│ └── CajaReportController.cs # Endpoint /CajaReport (caja)
├── Helpers/
│ ├── RawPrinterHelper.cs # Envío RAW a impresora
│ └── EscPosBuilder.cs # Builder de comandos ESC/POS
├── Models/
│ ├── PrintTicketRequest.cs # Modelo de ticket de venta
│ └── CajaReportRequest.cs # Modelo de ticket de caja
├── Program.cs # Configuración del servicio
├── appsettings.json # Configuración
├── instalar.bat # Script de instalación
├── LICENSE
└── README.md

text

---

## 📦 Requisitos

### 🖥️ Sistema Operativo

| Componente         | Versión mínima                    |
|--------------------|-----------------------------------|
| **Windows**        | 10 (1809+) / 11 / Server 2019+    |
| **Arquitectura**   | x64 (64 bits)                     |

### 🛠️ Software de desarrollo
-------------------------------------------------------------------------------------------------------------------------
| Software                             | Versión      | Descarga                                                        |
|--------------------------------------|--------------|-----------------------------------------------------------------|
| **.NET SDK**                         | 8.0.x        | [Descargar](https://dotnet.microsoft.com/download/dotnet/8.0)   |
| **Visual Studio** (recomendado)      | 2022 v17.8+  | [Descargar](https://visualstudio.microsoft.com/)                |
| **Visual Studio Code** (alternativa) | Última       | [Descargar](https://code.visualstudio.com/)                     |
| **Git**                              | Última       | [Descargar](https://git-scm.com/)                               |

**Workloads de Visual Studio requeridos:**
- ✅ Desarrollo de escritorio de .NET
- ✅ Desarrollo de ASP.NET y web

### 🚀 Software de producción

**Opción A — Self-contained (recomendado):**

No requiere instalar .NET en la máquina destino.

```bash
dotnet publish -c Release -r win-x64 --self-contained
Opción B — Framework-dependent:

Requiere instalar .NET Desktop Runtime 8.0 (x64):
Descargar

bash
dotnet publish -c Release -r win-x64
📚 Paquetes NuGet
Paquete	Versión	Propósito
QRCoder	1.4.3+	Generación de códigos QR
Microsoft.Extensions.Hosting.WindowsServices	8.0.0+	Ejecutar como Servicio Windows
System.Drawing.Common	8.0.0+	Impresión (solo Windows)
System.Text.Json	8.0.0+	Serialización JSON
Se instalan automáticamente al compilar:

bash
dotnet restore
🖨️ Impresora térmica
Requisito	Detalle
Modelo	POS-80C o compatible con ESC/POS
Compatibilidad	Epson TM-T20, TM-T88, Xprinter, 3nStar, Bixolon, etc.
Conexión	USB o Red (Ethernet)
Ancho de papel	80 mm (48 caracteres por línea)
Cortador automático	Recomendado (opcional)
Driver	Instalado en Windows
Nombre en Windows	POS-80C (o ajustar en appsettings.json)
Verificar el nombre de la impresora:

powershell
Get-Printer | Format-Table Name, DriverName, PortName
🌐 Puertos y red
Puerto	Uso	Dirección
5183	API HTTP del servicio	http://localhost:5183
Verificar que el puerto esté libre:

powershell
netstat -ano | findstr :5183
CORS configurado para aceptar:

http://localhost:8000 (Django local)

http://127.0.0.1:8000

http://localhost:8092

http://127.0.0.1:8092

https://elfide.com

🔐 Permisos requeridos
Permiso	Propósito
Administrador	Instalar/desinstalar el Servicio Windows
Escritura	Guardar JSONs en C:\10413820532\data y \comprobantes
Impresión	El usuario del servicio debe poder imprimir
📁 Estructura de carpetas esperada
text
C:\10413820532\
├── data\                    ← JSONs temporales (auditoría de impresión)
│   └── Imprimir-*.json
└── comprobantes\            ← JSONs para facturación electrónica SUNAT
    ├── 10413820532-BV01-*.json
    ├── 10413820532-FV01-*.json
    └── 10413820532-TICK-*.json
La carpeta se crea automáticamente si no existe.

🚀 Instalación
Instalación rápida (recomendado)
Ejecutar el script automático:

Abre CMD como Administrador

Navega al directorio del proyecto

Ejecuta:

bash
instalar.bat
Eso hace todo:

Detiene el servicio existente

Compila el proyecto

Copia los archivos

Crea e inicia el servicio Windows

Instalación manual
Ver la guía completa en docs/INSTALACION.md.

🔧 Configuración
appsettings.json
json
{
  "PrintSettings": {
    "Port": 5183,
    "PrinterName": "POS-80C",
    "JsonFolder": "C:\\10413820532\\data"
  },
  "VoucherConfig": {
    "RutaJson": "C:\\10413820532\\data"
  }
}
Campo	Descripción
Port	Puerto donde escucha el servicio (default: 5183)
PrinterName	Nombre exacto de la impresora en Windows
JsonFolder	Carpeta donde se guardan los JSON (auditoría)
📡 Endpoints
🎫 POST /Print → Imprimir ticket de venta
Request:

json
{
  "NombreJson": "Imprimir-10413820532-BV01-17.json",
  "ArchivoJson": "{...json stringify...}"
}
Response (200):

json
{
  "mensaje": "🖨️ Ticket enviado correctamente",
  "impresora": "POS-80C",
  "rutaJsonUsada": "C:\\10413820532\\data\\Imprimir-10413820532-BV01-17.json"
}
📊 POST /CajaReport → Imprimir reporte de caja
Request:

json
{
  "tipo_reporte": "APERTURA",
  "documento": {...},
  "emisor": {...},
  "caja": {...},
  "resumen": {...},
  "monedero": [...]
}
Response (200):

json
{
  "mensaje": "🖨️ Ticket de APERTURA enviado correctamente",
  "impresora": "POS-80C",
  "tipo_reporte": "APERTURA"
}
✅ GET /Print → Status del servicio de ventas
Response:

text
✅ Impresora lista para imprimir
✅ GET /CajaReport → Status del servicio de caja
Response:

text
✅ Servicio de reportes de caja listo (ESC/POS)
📄 Estructura del JSON
Ticket de venta
json
{
  "documento": {
    "numero": "BV01-17",
    "fecha_emision": "2026-10-02",
    "hora_emision": "15:13:43",
    "tipo_factura": "03",
    "moneda": "PEN"
  },
  "emisor": {
    "documento": { "numero": "10413820532", "tipo": "6" },
    "datos": {
      "razon_social": "FIDEL TORRES IPANAQUE",
      "nombre_comercial": "ELFIDE.COM"
    },
    "ubicacion": {...}
  },
  "cliente": {
    "documento": { "numero": "41382053", "tipo": "1" },
    "datos": { "razon_social": "FIDEL TORRES IPANAQUE" },
    "ubicacion": {...}
  },
  "notas": [
    { "id": "1000", "valor": "SEIS Y 50/100 SOLES" }
  ],
  "tipopago": {
    "forma_pago": "Contado",
    "monto_total": "6.50"
  },
  "impuestos": {...},
  "items": [...],
  "total": {...},
  "cajero": "elfide",
  "tipopagos": [...],
  "hash": "Venta Interna"
}
Tipos de comprobante (tipo_factura):

Código	Tipo
01	FACTURA ELECTRÓNICA
03	BOLETA DE VENTA ELECTRÓNICA
07	NOTA DE CRÉDITO
08	NOTA DE DÉBITO
10	TICKET DE VENTA INTERNA
🧪 Ejemplos de uso
Enviar un ticket (PowerShell)
powershell
$body = @{
    NombreJson = "test.json"
    ArchivoJson = Get-Content "ticket.json" -Raw
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5183/Print" `
    -Method Post -Body $body -ContentType "application/json"
Enviar desde Python
python
import requests, json

payload = {
    "NombreJson": "test.json",
    "ArchivoJson": json.dumps({...})
}
r = requests.post('http://localhost:5183/Print', json=payload)
print(r.json())
Enviar desde cURL
bash
curl -X POST "http://localhost:5183/Print" \
    -H "Content-Type: application/json" \
    -d @payload.json
🐛 Troubleshooting
❌ No imprime nada
Causa	Solución
Servicio detenido	Start-Service ServicioImpresion
Impresora apagada	Encender POS-80C
Nombre incorrecto	Verificar PrinterName en appsettings.json
Puerto ocupado	netstat -ano | findstr :5183
❌ Los espacios no se respetan
✅ Usar ESC/POS (este proyecto). Graphics.DrawString no funciona.

❌ Imprime 2 veces
Verificar que solo haya 1 llamada a pd.Print() en el controlador.

❌ QR borroso
✅ Usar QR nativo ESC/POS (GS ( k). No enviar como bitmap.

❌ Error de compilación: django_filters
No es de este proyecto — es del cliente Python. Verificar el venv activado.

📋 Ver logs del servicio
powershell
Get-EventLog -LogName Application -Source "ServicioImpresion" -Newest 50
🔄 Reiniciar el servicio
powershell
Restart-Service ServicioImpresion
📝 Changelog
[1.0.0] - 2026-10-02
✨ Versión inicial

✅ Impresión ESC/POS de tickets de venta

✅ Impresión ESC/POS de apertura y cierre de caja

✅ QR nativo

✅ Corte automático

✅ Multi-medio de pago

✅ Datos del cliente (DNI/RUC + ubigeo)

✅ ICBPER por item y por comprobante

👤 Autor
Fidel Torres

GitHub: @FIDELTORRES

Repositorio: EscPos-80c

📄 Licencia
Este proyecto está bajo la Licencia MIT. Ver el archivo LICENSE para más detalles.

🙏 Agradecimientos
QRCoder — Generación de códigos QR

Documentación ESC/POS de Epson

Comunidad de .NET

⭐ ¿Te sirvió?
Si este proyecto te ayudó, dale una ⭐ al repositorio y compártelo.

Hecho con ❤️ en Perú 🇵🇪

text

---

# 📄 ARCHIVO 2: `LICENSE`

**Va en la raíz del proyecto. Contenido estándar MIT.**

```text
MIT License

Copyright (c) 2026 Fidel Torres

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
📄 ARCHIVO 3: .gitignore
Va en la raíz del proyecto.

gitignore
# ═══════════════════════════════════════════════════════════
# 🎯 .NET — Compilados
# ═══════════════════════════════════════════════════════════
[Bb]in/
[Oo]bj/
[Pp]ublish/
[Oo]ut/
[Ll]og/
[Ll]ogs/
*.dll
*.exe
*.pdb
*.cache
*.deps.json
*.runtimeconfig.json
*.staticwebassets.endpoints.json

# ═══════════════════════════════════════════════════════════
# 🎯 Visual Studio
# ═══════════════════════════════════════════════════════════
.vs/
*.suo
*.user
*.userosscache
*.sln.docstates
*.userprefs
*.rsuser
*.sln
!*.sln  # (opcional: si quieres versionar la solución, quita esta línea)

# Archivos de usuario de ReSharper
*.DotSettings.user
_ReSharper*/
*.[Rr]e[Ss]harper

# ═══════════════════════════════════════════════════════════
# 🎯 Visual Studio Code
# ═══════════════════════════════════════════════════════════
.vscode/
!.vscode/extensions.json
!.vscode/settings.json
!.vscode/tasks.json
!.vscode/launch.json

# ═══════════════════════════════════════════════════════════
# 🎯 NuGet
# ═══════════════════════════════════════════════════════════
*.nupkg
*.snupkg
**/[Pp]ackages/*
!**/[Pp]ackages/build/
project.lock.json
project.fragment.lock.json
artifacts/

# NUGET packages con credenciales
.nuget/
*.nuget.props
*.nuget.targets

# ═══════════════════════════════════════════════════════════
# 🎯 Archivos de configuración locales
# ═══════════════════════════════════════════════════════════
# appsettings.Development.json (si contiene datos sensibles)
# appsettings.Local.json
# appsettings.*.local.json
# appsettings.Production.json

# ═══════════════════════════════════════════════════════════
# 🎯 Logs y datos generados
# ═══════════════════════════════════════════════════════════
*.log
logs/
*.txt
!LICENSE
!README.md
!CONTRIBUTING.md
!CHANGELOG.md

# Carpeta Jsons (temporal)
Jsons/

# ═══════════════════════════════════════════════════════════
# 🎯 Sistema Operativo
# ═══════════════════════════════════════════════════════════
# Windows
Thumbs.db
ehthumbs.db
Desktop.ini
$RECYCLE.BIN/
*.lnk
*.stackdump

# macOS
.DS_Store
.AppleDouble
.LSOverride
._*

# Linux
*~
.fuse_hidden*
.directory
.Trash-*

# ═══════════════════════════════════════════════════════════
# 🎯 Base de datos local (si aplica)
# ═══════════════════════════════════════════════════════════
*.db
*.db-shm
*.db-wal
*.sqlite
*.sqlite3

# ═══════════════════════════════════════════════════════════
# 🎯 Certificados y secretos (⚠️ NUNCA SUBIR)
# ═══════════════════════════════════════════════════════════
*.pfx
*.p12
*.pem
*.key
*.crt
*.cer
secrets.json
secrets.xml
.env
.env.local
.env.*.local

# ═══════════════════════════════════════════════════════════
# 🎯 Archivos temporales de compilación
# ═══════════════════════════════════════════════════════════
*.tmp
*.temp
*.bak
*.swp
*~
*.orig
*.rej

# ═══════════════════════════════════════════════════════════
# 🎯 Excepciones (asegurar que se versionen)
# ═══════════════════════════════════════════════════════════
!LICENSE
!README.md
!CONTRIBUTING.md
!CHANGELOG.md
!appsettings.json
!appsettings.Development.json
!instalar.bat
!.gitignore
!docs/
!docs/**
📄 ARCHIVO 4: docs/INSTALACION.md
Va en la carpeta docs/.

markdown
# 📖 Guía de Instalación Detallada

Guía paso a paso para instalar y configurar el servicio `EscPos-80c` en Windows.

---

## 📋 Tabla de contenidos

1. [Prerrequisitos](#-prerrequisitos)
2. [Instalación automática](#-instalación-automática)
3. [Instalación manual](#-instalación-manual)
4. [Configuración](#-configuración)
5. [Verificación](#-verificación)
6. [Primera impresión](#-primera-impresión)
7. [Solución de problemas](#-solución-de-problemas)
8. [Desinstalación](#-desinstalación)

---

## 🔧 Prerrequisitos

Antes de empezar, asegúrate de tener:

- ✅ Windows 10/11 o Windows Server 2019+
- ✅ .NET SDK 8.0 (para compilar)
- ✅ Visual Studio 2022 (o VS Code)
- ✅ Impresora POS-80C (o compatible)
- ✅ Driver de la impresora instalado
- ✅ Permisos de Administrador
- ✅ Puerto 5183 libre

### Verificar .NET SDK

```bash
dotnet --version
Debe mostrar: 8.0.xxx

Si no lo tienes, descárgalo aquí.

Verificar la impresora
powershell
Get-Printer | Format-Table Name, DriverName, PortName
Debe mostrar algo como:

text
Name      DriverName              PortName
----      ----------              --------
POS-80C   POS-80C Series Driver   USB001
Anota el nombre exacto (POS-80C o similar) — lo necesitarás después.

Verificar el puerto
powershell
netstat -ano | findstr :5183
Si no muestra nada, el puerto está libre ✅
Si muestra algo, hay otro servicio usando el puerto ⚠️

🚀 Instalación automática
Recomendado para la mayoría de usuarios.

Paso 1: Abrir CMD como Administrador
Presiona Windows + X

Selecciona "Terminal (Admin)" o "Símbolo del sistema (Admin)"

Confirma el UAC (si aparece)

Paso 2: Navegar al proyecto
bash
cd C:\ruta\al\proyecto\EscPos-80c
Paso 3: Ejecutar el instalador
bash
instalar.bat
Paso 4: Esperar
Verás algo así:

text
═══════════════════════════════════════════════════════
🖨️  INSTALADOR DE SERVICIO DE IMPRESIÓN .NET
═══════════════════════════════════════════════════════

🧹 Validando servicio existente...
⚠️  Servicio existente detectado, deteniendo y eliminando...

🛠️  Compilando proyecto .NET...
✅ Compilación exitosa

📂 Copiando archivos de publicación...
✅ Archivos copiados

⚙️  Creando servicio de Windows...
✅ Servicio creado

🟢 Iniciando servicio...
✅ Servicio iniciado

✅ INSTALACIÓN COMPLETADA

═══════════════════════════════════════════════════════
NOMBRE_SERVICIO: ServicioImpresion
        TIPO               : 10  WIN32_OWN_PROCESS
        ESTADO             : 4  RUNNING
═══════════════════════════════════════════════════════
Si ves ESTADO: 4 RUNNING, todo está OK. ✅

🛠️ Instalación manual
Para cuando necesites control total del proceso.

Paso 1: Compilar el proyecto
bash
cd C:\ruta\al\proyecto\EscPos-80c

# Restaurar paquetes
dotnet restore

# Compilar (self-contained)
dotnet publish -c Release -r win-x64 --self-contained
Los archivos compilados quedan en:

text
bin\Release\net8.0\win-x64\publish\
Paso 2: Copiar al directorio del servicio
powershell
# Crear carpeta destino
mkdir C:\Servicios\publish

# Copiar archivos
Copy-Item -Path "bin\Release\net8.0\win-x64\publish\*" `
          -Destination "C:\Servicios\publish\" `
          -Recurse -Force
Paso 3: Detener y eliminar servicio existente (si lo hay)
powershell
# Como Administrador
Stop-Service ServicioImpresion -ErrorAction SilentlyContinue
sc.exe delete ServicioImpresion
Paso 4: Crear el servicio
powershell
sc.exe create ServicioImpresion binPath="C:\Servicios\publish\MiProyecto.exe" start=auto
Parámetros:

Parámetro	Valor	Descripción
binPath	C:\Servicios\publish\MiProyecto.exe	Ruta del ejecutable
start	auto	Inicia automáticamente con Windows
Paso 5: Iniciar el servicio
powershell
Start-Service ServicioImpresion
Paso 6: Verificar
powershell
Get-Service ServicioImpresion
Debe mostrar:

text
Status   Name               DisplayName
------   ----               -----------
Running  ServicioImpresion  Servicio de Impresión
⚙️ Configuración
Editar appsettings.json
Ubicación: C:\Servicios\publish\appsettings.json

json
{
  "PrintSettings": {
    "Port": 5183,
    "PrinterName": "POS-80C",
    "JsonFolder": "C:\\10413820532\\data"
  },
  "VoucherConfig": {
    "RutaJson": "C:\\10413820532\\data"
  }
}
Ajustar el nombre de la impresora
Si tu impresora no se llama POS-80C, cámbialo:

json
"PrinterName": "Nombre-Exacto-De-Tu-Impresora"
Después reinicia el servicio:

powershell
Restart-Service ServicioImpresion
Cambiar el puerto
Si el puerto 5183 está ocupado:

json
"Port": 5184
Reinicia el servicio:

powershell
Restart-Service ServicioImpresion
Cambiar la carpeta de JSONs
json
"JsonFolder": "D:\\Impresiones\\data"
Crea la carpeta antes:

powershell
mkdir D:\Impresiones\data
✅ Verificación
1. Verificar que el servicio está corriendo
powershell
Get-Service ServicioImpresion
Debe mostrar Running.

2. Verificar el endpoint
powershell
Invoke-RestMethod -Uri "http://localhost:5183/Print" -Method Get
Respuesta esperada:

text
✅ Impresora lista para imprimir
3. Verificar el endpoint de caja
powershell
Invoke-RestMethod -Uri "http://localhost:5183/CajaReport" -Method Get
Respuesta esperada:

text
✅ Servicio de reportes de caja listo (ESC/POS)
4. Verificar los logs
powershell
Get-EventLog -LogName Application -Source "ServicioImpresion" -Newest 20
🎫 Primera impresión
Prueba rápida con PowerShell
powershell
$body = @{
    tipo_reporte = "APERTURA"
    NombreJson = "test-apertura.json"
    documento = @{
        numero = "TEST-1"
        fecha_emision = "02/10/2026"
        hora_emision = "12:00:00"
        moneda = "PEN"
    }
    emisor = @{
        documento = @{ numero = "10413820532"; tipo = "6" }
        datos = @{ razon_social = "TEST"; nombre_comercial = "TEST" }
        ubicacion = @{ direccion = "TEST" }
    }
    caja = @{
        idcaja = 999
        nrocaja = "01"
        nombre_caja = "CAJA TEST"
        nombre_local = "LOCAL TEST"
        usuario = "test"
        turno = "MANANA"
        fecha_apertura = "02/10/2026 12:00:00"
        moneda = "NUEVO SOL"
    }
    resumen = @{ monto_inicial = 50.00; efectivo_inicial = 50.00 }
    monedero = @( @{ denominacion = 50.00; cantidad = 1; subtotal = 50.00 } )
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "http://localhost:5183/CajaReport" `
    -Method Post -Body $body -ContentType "application/json"
Debería imprimir un ticket de prueba.

🐛 Solución de problemas
❌ El servicio no inicia
Verificar logs:

powershell
Get-EventLog -LogName Application -Source "ServicioImpresion" -Newest 50
Causas comunes:

Causa	Solución
Falta .NET Runtime	Instalar .NET Desktop Runtime 8.0
Puerto ocupado	Cambiar puerto en appsettings.json
Ruta incorrecta	Verificar binPath del servicio
Permisos insuficientes	Ejecutar como Administrador
❌ No imprime
Verificar:

Impresora encendida ✅

Nombre correcto en appsettings.json ✅

Driver instalado ✅

powershell
Get-Printer | Format-Table Name, DriverName
❌ Error "Printer not found"
El nombre de la impresora es distinto.

powershell
# Ver nombres exactos
Get-Printer | Select-Object Name
Copiar el nombre exacto a appsettings.json.

❌ Imprime 2 veces
Verificar:

Que el driver de la impresora NO esté configurado con 2 copias

Que en PrintController.cs solo haya 1 pd.Print()

❌ Los espacios se colapsan
Estás usando Graphics.DrawString en vez de ESC/POS.

Verificar que el controlador use RawPrinterHelper.SendBytesToPrinter(...).

❌ Error de compilación
Verificar .NET SDK instalado:

bash
dotnet --version
Restaurar paquetes:

bash
dotnet restore

Limpiar y recompilar:

bash
dotnet clean
dotnet publish -c Release -r win-x64 --self-contained
🗑️ Desinstalación
Detener el servicio
powershell
Stop-Service ServicioImpresion
Eliminar el servicio
powershell
sc.exe delete ServicioImpresion
Eliminar archivos
powershell
Remove-Item -Path "C:\Servicios\publish" -Recurse -Force
Verificar
powershell
Get-Service ServicioImpresion
Debe mostrar error "No se encuentra el servicio". ✅

📞 Soporte
Si tienes problemas:

Revisa Troubleshooting en el README

Abre un Issue en GitHub

Revisa los logs del servicio

📋 Resumen
Paso	Comando
1. Verificar .NET	dotnet --version
2. Verificar impresora	Get-Printer
3. Compilar	dotnet publish -c Release -r win-x64 --self-contained
4. Copiar archivos	Copy-Item ... -Destination C:\Servicios\publish\
5. Crear servicio	sc.exe create ServicioImpresion ...
6. Iniciar	Start-Service ServicioImpresion
7. Verificar	Get-Service ServicioImpresion
O simplemente:

bash
instalar.bat
¡Listo! El servicio debería estar funcionando. 🎉

text

---

## 🎯 Estructura final de archivos
EscPos-80c/
├── Controllers/
├── Helpers/
├── Models/
├── docs/
│ ├── INSTALACION.md ← NUEVO
│ └── (opcional) CONFIGURACION.md
├── .gitignore ← NUEVO
├── LICENSE ← NUEVO
├── README.md ← ACTUALIZADO
├── Program.cs
├── appsettings.json
├── instalar.bat
└── MiProyecto.csproj

text

---

## 📋 Cómo subirlos a GitHub

### Opción 1: Desde VS Code

1. **Crea los 4 archivos** en tu proyecto local:
   - `README.md` (raíz)
   - `LICENSE` (raíz)
   - `.gitignore` (raíz)
   - `docs/INSTALACION.md`

2. **Abre VS Code** en la carpeta del proyecto

3. **Panel Source Control** (Ctrl+Shift+G)
   - Click en **"+"** junto a cada archivo
   - Commit: `docs: agregar README, LICENSE, .gitignore y guía de instalación`
   - Click en **"Sync Changes"**

### Opción 2: Desde terminal

```bash
cd C:\ruta\al\proyecto\EscPos-80c

# Crear carpeta docs
mkdir docs

# (Crea los archivos con su contenido)

# Agregar todo
git add .
git commit -m "docs: agregar README, LICENSE, .gitignore y guía de instalación"
git push origin main
Opción 3: Desde GitHub web
Ve al repo: https://github.com/FIDELTORRES/EscPos-80c

Add file → Create new file

Crea cada archivo uno por uno

Commit cada uno

