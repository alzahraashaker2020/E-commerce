using System.Text.Json;
using Shop.BLL.Response_VM;
using Shop.DAL.ViewModels;
using Xunit;
namespace Shop.RegressionTests;

public class ApiContractTests
{
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public void Existing_envelope_keeps_status_null_and_message(int status)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ApiResponse<object>.Fail("original", status), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var body = json.RootElement; Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal(status, body.GetProperty("statusCode").GetInt32()); Assert.Equal("original", body.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind); Assert.Equal(4, body.EnumerateObject().Count());
    }
    [Fact]
    public void Middleware_default_serialization_is_not_mistaken_for_MVC_web_casing()
    {
        var json = JsonSerializer.Serialize(ApiResponse<object>.Fail("Unexpected error", 500));
        Assert.Contains("\"Success\"", json); Assert.DoesNotContain("\"success\"", json);
    }
    [Fact]
    public void Request_fingerprint_is_order_sensitive_and_keeps_key()
    {
        var key = Guid.NewGuid(); var request = new PlaceOrderRequest { IdempotencyKey = key, Lines = new() { new() { ProductId = 1, Quantity = 1 }, new() { ProductId = 2, Quantity = 1 } } };
        string first = JsonSerializer.Serialize(request); request.Lines.Reverse();
        Assert.NotEqual(first, JsonSerializer.Serialize(request)); Assert.Contains(key.ToString(), first);
    }
}
