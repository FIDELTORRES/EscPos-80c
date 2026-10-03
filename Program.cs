using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

var builder = WebApplication.CreateBuilder(args);

// Permitir ejecutar como Windows Service con nombre amigable
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "Servicio de Impresión";
});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirLocalhost", policy =>
    {
        policy.WithOrigins(
                "http://127.0.0.1:8092",
                "http://localhost:8092",
                "http://localhost:8000",        // ← Django local
                "http://127.0.0.1:8000",        // ← Django local (alternativo)
                "https://elfide.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Agregar servicios de controladores
builder.Services.AddControllers();

// Agregar Swagger (opcional)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Escuchar en todas las interfaces en el puerto 5183
// app.Urls.Add("http://0.0.0.0:5183");
// Leer puerto desde appsettings.json con fallback a 5183
int port = builder.Configuration.GetValue<int>("PrintSettings:Port", 5183);

// Escuchar en todas las interfaces en el puerto configurado
app.Urls.Add($"http://0.0.0.0:{port}");

// Usar Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Activar CORS antes de los controladores
app.UseCors("PermitirLocalhost");

app.UseHttpsRedirection();
app.UseAuthorization();

// Mapear controladores
app.MapControllers();

// Ejecutar la app (Kestrel)
app.Run();
