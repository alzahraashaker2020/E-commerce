using Microsoft.Extensions.Configuration;
using Shop.Api.Tenancy;
using Xunit;

namespace Shop.Infrastructure.Tests.Provisioning;

public class SqlServerIdentityTests
{
    [Fact]
    public void Local_server_aliases_resolve_to_one_database_identity()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SHOP_TEST_SQLSERVER")))return;
        string Connection(string host)=>$"Server={host};Database=master;Integrated Security=True;TrustServerCertificate=True";
        Assert.Equal(StoreConnections.SqlServerIdentity(Connection("localhost")),StoreConnections.SqlServerIdentity(Connection(".")));
    }
}
