using Shop.Application.Returns;
using Shop.DAL.Models;
namespace Shop.Api.Security;
// Compatibility facade: existing HTTP ownership checks and public contracts stay in place.
public sealed class OrderReturnsService
{
    private readonly ReturnService _service;
    public OrderReturnsService(ReturnService service) => _service = service;
    public Task<List<ReturnEntry>> ReadAsync(int? orderId = null) => _service.ReadAsync(orderId);
    public Task<ReturnEntry> RequestAsync(Order order, Guid key, int itemId, int quantity, string reason) => _service.RequestAsync(order.Id, key, itemId, quantity, reason);
    public Task<ReturnEntry> ChangeAsync(string id, int version, string action, string notes, bool restock, decimal refund, string reference, string actor) => _service.ChangeAsync(id, version, action, notes, restock, refund, reference, actor);
}
