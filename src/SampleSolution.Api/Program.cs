using System.Net.Security;
using Microsoft.OpenApi.Models;
using SampleSolution.Api.Middleware;
using SampleSolution.Api.Services;
using SampleSolution.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services.Configure<AzureBlobSettings>(builder.Configuration.GetSection(AzureBlobSettings.AzureBlobSettingsName));

services.AddControllers();

services.AddEndpointsApiExplorer();

services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SampleSolution API", Version = "v1" });
});

services.AddHttpClient("ImageDownloader")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        MaxConnectionsPerServer = 20,
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
            EncryptionPolicy = EncryptionPolicy.RequireEncryption
        },
        ConnectTimeout = TimeSpan.FromSeconds(10),

        KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(30)
    });

services.AddScoped<IAzureBlobService, AzureBlobService>();

var app = builder.Build();

app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SampleSolution API v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();