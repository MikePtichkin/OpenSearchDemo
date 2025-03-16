using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenSearch;

var builder = WebApplication.CreateBuilder(args);

// Очищаем провайдеры логирования по умолчанию
builder.Logging.ClearProviders();

// Включаем SelfLog для отладки Serilog
Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

// Настройка Serilog
var logger = new LoggerConfiguration()
    .WriteTo.Console() // Логи в консоль
    .WriteTo.OpenSearch(new OpenSearchSinkOptions(new Uri("https://localhost:9200"))
    {
        AutoRegisterTemplate = true, // Автоматически регистрируем шаблон в OpenSearch
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.OSv1, // Версия шаблона
        MinimumLogEventLevel = LogEventLevel.Verbose, // Минимальный уровень логирования
        IndexFormat = "my-index-{0:yyyy.MM.dd}", // Формат индекса
        ModifyConnectionSettings = x => x.BasicAuthentication("admin", "Quintry@1234!") // Аутентификация
            .ServerCertificateValidationCallback((o, certificate, chain, errors) => true) // Игнорируем проверку сертификата
    })
    .CreateLogger();

builder.Host.UseSerilog(logger);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// Пример логирования
app.Logger.LogInformation("Приложение запущено!");

app.Run();
