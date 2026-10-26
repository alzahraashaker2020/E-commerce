using Shop.BLL.Email;
using Shop.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Shop.Api.Tenancy;

public static class StoreEmailPreferences
{
    public static void Read(ShopContext db,EmailOptions options)
    {
        db.Database.OpenConnection();
        try
        {
            using var command=db.Database.GetDbConnection().CreateCommand();
            command.CommandText="SELECT \"MerchantEmail\",\"CustomerOrdersEnabled\",\"CustomerStatusEnabled\",\"MerchantOrdersEnabled\" FROM \"StoreEmailPreferences\" WHERE \"Id\"=1";
            using var reader=command.ExecuteReader();
            if(reader.Read()){options.MerchantEmail=reader.GetString(0);options.CustomerOrdersEnabled=Convert.ToBoolean(reader.GetValue(1));options.CustomerStatusEnabled=Convert.ToBoolean(reader.GetValue(2));options.MerchantOrdersEnabled=Convert.ToBoolean(reader.GetValue(3));}
        }
        finally{db.Database.CloseConnection();}
    }
}
