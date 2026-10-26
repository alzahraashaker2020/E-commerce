using System.Text.Json;
namespace Shop.Api.Tenancy;
public static class PlanFeatures
{
    public static readonly string[] All={"Discounts","Newsletter","StorefrontDesign","Marketplace"};
    public const string DefaultJson="[\"Discounts\",\"Newsletter\",\"StorefrontDesign\",\"Marketplace\"]";
    public static string[] Read(string? json){try{return JsonSerializer.Deserialize<string[]>(json??"[]")?.Where(All.Contains).Distinct().ToArray()??Array.Empty<string>();}catch{return Array.Empty<string>();}}
    public static bool Allows(DeploymentProfile profile,StoreScope scope,string feature)=>!profile.SaasEnabled || !scope.Store!.SubscriptionRequired ||
        (scope.Subscription is not null && Read(scope.Subscription.FeaturesJson).Contains(feature));
}
