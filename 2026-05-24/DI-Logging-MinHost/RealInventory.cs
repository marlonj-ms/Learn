namespace DI_Logging_MinHost;
using Microsoft.Extensions.Logging;

public sealed class RealInventory : IInventory
{
    private readonly ILogger<RealInventory> _logger;

    private readonly Dictionary<string, int> _stock = new ()
    {
        ["Pen"] = 10,
        ["Notebook"] = 200,
        ["Car"] = 3,
    };

    public RealInventory(ILogger<RealInventory> logger)
    {
        _logger = logger;
        _logger.LogInformation("[TRACE] RealInventory created. Instance={Instance}", GetHashCode());
    }   

    public bool IsAvailable(string productName, int quantity)
    {
        if(!_stock.TryGetValue(productName, out var available))
        {
            _logger.LogWarning("Product not found: {Product}", productName);
            return false;
        }

        var ok= ( available >= quantity);
        _logger.LogInformation(
            "Stock check for {Product}: requested {Requested}, available {Available}, result {Ok}",
            productName, quantity, available, ok);
        return ok;
    }




}