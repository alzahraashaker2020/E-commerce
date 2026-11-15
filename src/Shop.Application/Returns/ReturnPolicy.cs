namespace Shop.Application.Returns;

public static class ReturnPolicy
{
    public static string Next(string state, string action, string notes, bool restock, decimal refund, decimal max, string reference)
    {
        string next = action switch
        {
            "Approve" when state == "Requested" => "Approved",
            "Reject" when state is "Requested" or "Approved" => "Rejected",
            "Receive" when state == "Approved" => "Received",
            "RecordRefund" when state == "Received" => "Completed",
            _ => throw new InvalidOperationException("This action is unavailable for the current return status.")
        };
        if (notes.Trim().Length == 0) throw new InvalidOperationException("Add a reason or inspection note.");
        if (action != "Receive" && restock) throw new InvalidOperationException("Stock can be restored only during inspection.");
        if (action == "RecordRefund" && (refund < 0 || refund > max || decimal.Round(refund, 2) != refund || (refund > 0 && string.IsNullOrWhiteSpace(reference)))) throw new InvalidOperationException("Enter a valid refund amount and external payment reference.");
        return next;
    }
    public static decimal MaximumRefund(decimal total, decimal shipping, decimal subtotal, decimal lineTotal, int quantity, int lineQuantity)
    {
        decimal refundable = Math.Max(0, total - shipping);
        return subtotal <= 0 ? 0 : decimal.Floor(refundable * lineTotal / subtotal * quantity / lineQuantity * 100) / 100;
    }
}
