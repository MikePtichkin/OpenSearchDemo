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
//Serilog.Debugging.SelfLog.Enable(Console.Error);
//
//var loggerConfiguration = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .Enrich.FromLogContext()
//    .WriteTo.Console()
//    .WriteTo.OpenTelemetry(
//        opts =>
//        {
//            opts.ResourceAttributes.Add("service.instance.id", Environment.MachineName);
//            opts.BatchingOptions.EagerlyEmitFirstEvent = true;
//            opts.BatchingOptions.BatchSizeLimit = 30;
//            opts.BatchingOptions.BufferingTimeLimit = TimeSpan.FromSeconds(2);
//            opts.BatchingOptions.QueueLimit = 60;
//            opts.BatchingOptions.RetryTimeLimit = TimeSpan.FromMinutes(10);
//        },
//        builder.Configuration.GetValue<string?>);
//
//Log.Logger = loggerConfiguration.CreateLogger();
//builder.Host.UseSerilog();

// Настройка OpenTelemetry Logging
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;

    //options.AddConsoleExporter(option => option.Targets = ConsoleExporterOutputTargets.Console);
    options.AddOtlpExporter();
});

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
            .AddOtlpExporter(o =>
            {
                o.ExportProcessorType = ExportProcessorType.Batch;
                o.BatchExportProcessorOptions.MaxQueueSize = 10_000;
                o.BatchExportProcessorOptions.ScheduledDelayMilliseconds = 5_000;
                o.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = 30_000;
            });
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(o =>
            {
                o.BatchExportProcessorOptions.MaxQueueSize = 10_000;
            });
    });
//.UseOtlpExporter();

// Настройка DataProtection для сохранения ключей
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("WeatherService");

builder.Services.AddControllers();

builder.Services.AddHostedService<BackgroundLoggerService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Logger.LogInformation("App started");

app.Run();
