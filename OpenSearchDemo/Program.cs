using Microsoft.AspNetCore.DataProtection;
using OpenSearchDemo;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка Serilog
Serilog.Debugging.SelfLog.Enable(Console.Error);

var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}")
    .WriteTo.OpenTelemetry(
        opts =>
        {
            opts.ResourceAttributes.Add("service.instance.id", Environment.MachineName);
            opts.BatchingOptions.EagerlyEmitFirstEvent = true;
            opts.BatchingOptions.BatchSizeLimit = 30;
            opts.BatchingOptions.BufferingTimeLimit = TimeSpan.FromSeconds(2);
            opts.BatchingOptions.QueueLimit = 60;
            opts.BatchingOptions.RetryTimeLimit = TimeSpan.FromMinutes(10);
            opts.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                ?? throw new ArgumentNullException("OTEL_EXPORTER_OTLP_ENDPOINT is not configured");
        },
        builder.Configuration.GetValue<string?>);

Log.Logger = loggerConfiguration.CreateLogger();
builder.Host.UseSerilog();

// Настройка OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddAttributes([new("service.instance.id", Environment.MachineName)]);
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                    ?? throw new ArgumentNullException("OTEL_EXPORTER_OTLP_ENDPOINT is not configured"));
            });
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(opts =>
            {
                opts.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                    ?? throw new ArgumentNullException("OTEL_EXPORTER_OTLP_ENDPOINT is not configured"));
            });
    });
//.UseOtlpExporter();

// Настройка DataProtection для сохранения ключей
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .SetApplicationName("WeatherService");

builder.Services.AddControllers();

builder.Services.AddHostedService<BackgroundLoggerService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Logger.LogInformation("App started");

app.Run();
