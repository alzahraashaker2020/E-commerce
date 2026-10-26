namespace Shop.Api.Tenancy;

public static class SubscriptionAccess
{
    public static bool IsCurrent(StoreSubscription? subscription) => subscription is not null &&
        !subscription.IsCancelled && subscription.StartsAtUtc <= DateTime.UtcNow && subscription.EndsAtUtc > DateTime.UtcNow;
    public static string Status(StoreRegistration store, StoreSubscription? subscription) => subscription is null
        ? (store.SubscriptionRequired ? "Pending" : "Legacy")
        : subscription.IsCancelled ? "Cancelled" : !IsCurrent(subscription) ? "Expired" : subscription.IsTrial ? "Trialing" : "Active";
}
