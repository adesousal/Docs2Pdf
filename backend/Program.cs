using Docs2Pdf.Api.Services;
using Microsoft.AspNetCore.Http.Features;

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
        policy.WithOrigins(frontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<FileValidationService>();
builder.Services.AddSingleton<TempFileService>();
builder.Services.AddSingleton<PdfConversionService>();

builder.Services.Configure<FormOptions>(options =>
{
    // Allow larger combined uploads when users send many documents/images in one request.
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
});

var app = builder.Build();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
