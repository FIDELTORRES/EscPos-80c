---
name: escpos-project-changes
description: "Usar al implementar, depurar o revisar cambios en este servicio de impresión ESC/POS con .NET 8: endpoints HTTP, modelos de tickets y reportes de caja, salida de impresora, configuración, Servicio de Windows o documentación."
---

# Cambios en el proyecto EscPos-80c

Usa este flujo de trabajo para modificar el repositorio EscPos-80c. El proyecto es una API ASP.NET Core con .NET 8 que se ejecuta como Servicio de Windows y envía tickets de venta e informes de caja a impresoras térmicas.

## Antes de cambiar el código

1. Lee el cambio solicitado e identifica qué comportamientos y consumidores deben seguir siendo compatibles.
2. Inspecciona la implementación actual y quién la utiliza antes de basarte en el README. La documentación puede no estar sincronizada con el código.
3. Sigue el recorrido desde el endpoint HTTP hasta el modelo, la generación del ticket, la configuración y la salida a la impresora. Comprueba si también hay que actualizar el endpoint relacionado, el ejemplo de solicitud y la documentación.
4. Si no está claro un comportamiento importante (por ejemplo, cambiar el protocolo de impresión o un contrato JSON existente), acláralo antes de tomar una decisión incompatible.

## Mapa del proyecto

- `Program.cs`: inicio de ASP.NET Core, integración con el Servicio de Windows, CORS, Swagger y puerto de escucha configurado.
- `Controllers/PrintController.cs`: `GET` y `POST /Print`, procesamiento de solicitudes de tickets de venta, almacenamiento de JSON e impresión de tickets.
- `Controllers/CajaReportController.cs`: `GET` y `POST /CajaReport`, informes de apertura/cierre de caja y salida del ticket del informe.
- `Models/PrintTicketRequest.cs`: contrato JSON del ticket de venta. Conserva los valores de `JsonPropertyName` y sus valores predeterminados, salvo que el cambio solicitado modifique explícitamente el contrato público.
- `Models/CajaReportRequest.cs`: contrato JSON del informe de caja, incluidos `tipo_reporte`, `APERTURA`/`CIERRE` y los datos del informe.
- `Helpers/EscPosBuilder.cs`: generador de comandos ESC/POS y texto; define anchos de 48 caracteres para papel de 80 mm y 32 para 58 mm, además de la codificación de texto de la impresora.
- `Helpers/RawPrinterHelper.cs`: interoperabilidad con la cola de impresión de Windows para enviar bytes RAW a una impresora.
- `appsettings.json`: nombre de la impresora, puerto de escucha y rutas de almacenamiento de JSON.
- `MiProyecto.http`: ejemplos HTTP; confirma que las rutas de ejemplo coincidan con la API real antes de basarte en ellas o actualizarlas.
- `README.md` y `docs/`: documentación de la API, instalación y operación dirigida a usuarios.

## Pautas de implementación

- Mantén separadas las dos rutas de impresión, salvo que el cambio solicitado busque unificarlas. `CajaReportController` genera bytes ESC/POS con `EscPosBuilder` y los envía mediante `RawPrinterHelper`; el `PrintController` actual renderiza la salida con `System.Drawing.Printing`. No supongas que la descripción del README sobre ESC/POS RAW se aplica igual a ambos endpoints.
- Conserva las rutas de los endpoints, los métodos HTTP, los nombres de campos JSON, las formas de las respuestas, las claves de configuración y el comportamiento de impresión actual, salvo que el cambio solicitado requiera modificarlo y documentarlo.
- Al cambiar un modelo de solicitud, revisa las validaciones del controlador y todos los ejemplos y documentos afectados. Mantén el tratamiento de valores nulos e inicializa colecciones y modelos anidados de forma coherente con el código cercano.
- Al modificar el diseño de un ticket, ten en cuenta el ancho de línea de la impresora, la codificación, la alineación, los saltos de línea, los comandos QR/código de barras, el corte del papel y si la salida usa GDI o ESC/POS RAW. No dependas únicamente de una impresora física para verificar el cambio.
- Mantén las operaciones específicas de la impresora y de Windows en la ruta de impresión existente. Informa los errores explícitamente; no devuelvas éxito si falla la llamada a la cola de impresión ni ignores entradas no válidas.
- Trata la configuración de la impresora, el inicio del servicio, los orígenes CORS y las rutas de archivos JSON de auditoría como comportamientos operativos. Mantén coherentes los cambios en el código, `appsettings*.json` y la documentación correspondiente.
- Limita el cambio a lo necesario. Actualiza la documentación y los ejemplos HTTP directamente relacionados, pero no reescribas partes ajenas del README.

## Validación

1. Comprueba qué solución y proyectos de pruebas están disponibles antes de elegir los comandos.
2. Después de modificar código, compila la solución con `dotnet build .\MiProyecto.sln`.
3. Si el repositorio contiene proyectos de pruebas, ejecuta las pruebas pertinentes con `dotnet test .\MiProyecto.sln`. No afirmes que las pruebas pasaron si no existen o no se ejecutaron.
4. Para cambios en la API, verifica el contrato de solicitud/respuesta afectado y ajusta los ejemplos de `MiProyecto.http` para que coincidan con las rutas reales. Si la API está en ejecución, usa los endpoints de estado documentados para una comprobación básica que no imprima.
5. Para verificar la impresión física se necesita una impresora compatible con Windows y su controlador instalado. Indica claramente si no fue posible comprobar el comportamiento en el equipo.
6. Revisa el diff final para detectar cambios accidentales en archivos generados, artefactos de publicación, configuración local y archivos no relacionados.
