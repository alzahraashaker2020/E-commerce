using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Shop.DAL.Data;

namespace Shop.Infrastructure.Tests;

internal static class SqlServerTestDatabase
{
    internal static bool Configure(DbContextOptionsBuilder<ShopContext> builder)
    {
        var connection=Environment.GetEnvironmentVariable("SHOP_TEST_SQLSERVER");
        if(string.IsNullOrWhiteSpace(connection))return false;
        builder.UseSqlServer(new SqlConnectionStringBuilder(connection){InitialCatalog="matjark_test_"+Guid.NewGuid().ToString("N"),Pooling=false}.ToString());
        return true;
    }
}
