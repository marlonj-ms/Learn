namespace DI_Logging_MinHost;
using Microsoft.Extensions.Logging;

public sealed class FakeInventory : IInventory
{
    private readonly ILogger<FakeInventory> _logger;

    public FakeInventory(ILogger<FakeInventory> logger)
    {
        _logger = logger;
        _logger.LogInformation("[TRACE] FakeInventory created. Instance={Instance}", GetHashCode());
    }

    public bool IsAvailable(string productName, int quantity) => true;

}
