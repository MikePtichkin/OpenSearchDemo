using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace OpenSearchDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        var activitySource = new ActivitySource("WeatherService");
        using var activity = activitySource.StartActivity(
            kind: ActivityKind.Server,
            name: "GetWeatherForecast");

        activity?.SetTag("http.method", "GET");

        _logger.LogInformation("Getting weather forecast");

        var result = Enumerable.Range(1, 3).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        foreach (var item in result)
        {
            _logger.LogInformation("Date: {Date}, Temperature: {TemperatureC}, Summary: {Summary}",
                item.Date,
                item.TemperatureC,
                item.Summary);
        }

        _logger.LogInformation("Generated {Count} forecasts", result.Length);
        return result;
    }
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}