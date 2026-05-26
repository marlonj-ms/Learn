using Microsoft.Extensions.Logging;
namespace DI_Logging_MinHost;

public sealed class OrderService
{
	private readonly ILogger<OrderService> _logger;
    private readonly IInventory _inventory;


	public OrderService(ILogger<OrderService> logger,   IInventory inventory)
	{
		_logger=logger;
        _inventory=inventory;

        _logger.LogInformation("[TRACE] OrderService created. Instance={Instance}", GetHashCode());
	}

	public void PlaceOrder(string productName, int quantity)
	{	
        if (!_inventory.IsAvailable(productName, quantity))
        {
            _logger.LogWarning("Order rejected: {Product} x {Quantity} not in stock", productName, quantity);
            return;        // ← 早退（early return）
        }

        _logger.LogInformation("Order placed: {Product} x {Quantity}", productName, quantity);

        if (quantity > 100)
        {
            _logger.LogWarning("Bulk order detected: {Quantity} units", quantity);
        }
	}

}

public sealed class ProbeService
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}
