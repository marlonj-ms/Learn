using Sensor = TemperatureSensor.Core.TemperatureSensor;


var builder = WebApplication.CreateBuilder(args);

double threshold = builder.Configuration.GetValue<double>("Sensor:Threshold");
builder.Services.AddSingleton<Sensor>(sp => new Sensor(threshold: threshold));

//builder.Services.AddSingleton<Sensor>(sp => new Sensor(threshold: 30.0));

var app = builder.Build();

var sensor = app.Services.GetRequiredService<Sensor>();
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Sensor threshold loaded from config: {Threshold}°C", threshold);
sensor.ThresholdExceeded += (sender, args) =>
{
    startupLogger.LogWarning(
        "🔥 THRESHOLD EXCEEDED at {Timestamp:HH:mm:ss}: reading={Reading}°C threshold={Threshold}°C",
        args.Timestamp, args.Reading, args.Threshold);
};

sensor.ThresholdExceeded += (sender, args) =>
{
    startupLogger.LogWarning(
        "Beep Beep, call the fire department! Threshold exceeded at {Timestamp:HH:mm:ss}: reading={Reading}°C threshold={Threshold}°C",
        args.Timestamp, args.Reading, args.Threshold);
};

app.MapGet("/", () => "Hello World!");

app.MapPost("/readings", (Sensor sensor, ILogger<Program> logger, ReadingDto dto) =>
{
    try
    {
        sensor.RecordReading(dto.Celsius);
        logger.LogInformation("Recorded reading: {Celsius}°C", dto.Celsius);
        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning("Rejected reading {Celsius}: {Reason}", dto.Celsius, ex.Message);
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

record ReadingDto(double Celsius);

public partial class Program { }  

