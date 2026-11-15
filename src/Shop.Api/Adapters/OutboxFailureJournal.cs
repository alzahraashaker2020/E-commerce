using Shop.Application.Notifications;
using Shop.Api.Tenancy;
using Shop.Api.Operations;
namespace Shop.Api.Adapters;

public sealed class OutboxFailureJournal : IOutboxFailureJournal
{
    private readonly OperationJournal _journal; private readonly string _storeId;
    public OutboxFailureJournal(OperationJournal journal, StoreScope scope) { _journal = journal; _storeId = scope.StoreId; }
    public Task RecordFailureAsync(string messageId) => _journal.WriteAsync(_storeId, "email-worker", "System", "EmailDeliveryFailed", "JOB", 500, messageId, "DeliveryFailure");
}
