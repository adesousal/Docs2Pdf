using Docs2Pdf.Api.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var frontendOrigin = builder.Configuration["FrontendOrigin"] ?? "http://localhost:4200";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendOrigin) // Restringe apenas para o seu GitHub Pages
              .AllowAnyMethod()                         // Permite POST, GET, OPTIONS, etc.
              .AllowAnyHeader()                         // Permite cabeçalhos customizados (como o do Localtonet)
              .WithExposedHeaders("Content-Disposition", "Content-Length"); // Permite ao Angular ler o PDF retornado
    });
});

builder.Services.AddSingleton<FileValidationService>();
builder.Services.AddSingleton<TempFileService>();

// Use Gotenberg for faster, persistent document conversion
// Instead of spawning LibreOffice for each file, Gotenberg runs as a persistent service
builder.Services.AddHttpClient<GotenbergConversionService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddSingleton<PdfConversionService>();

builder.Services.Configure<FormOptions>(options =>
{
    // Allow larger combined uploads when users send many documents/images in one request.
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
