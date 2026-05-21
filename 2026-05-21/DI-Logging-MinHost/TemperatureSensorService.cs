using Microsoft.Extensions.Logging;

namespace DI_Logging_MinHost;

public sealed class TemperatureSensorService
{
    private readonly ILogger<TemperatureSensorService> _logger;

    public TemperatureSensorService(ILogger<TemperatureSensorService> logger)
    {
        _logger = logger;
    }

    public void Record(double reading)
    {
        _logger.LogInformation("The temperature sensor reported {Reading}", reading);

        if (reading > 100)
        {
            _logger.LogWarning("Temperature is too high: {Reading}", reading);
        }
    }
}
