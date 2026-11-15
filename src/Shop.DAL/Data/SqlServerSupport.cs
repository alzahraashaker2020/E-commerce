using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Shop.DAL.Data;

/// <summary>Provider-specific syntax and error classification, without changing workflow policies.</summary>
public static class SqlServerSupport
{
    public static string Limit(DbContext db, int count) => db.Database.IsSqlServer()
        ? $" OFFSET 0 ROWS FETCH NEXT {count} ROWS ONLY" : $" LIMIT {count}";
    public static bool Duplicate(Exception ex) => ex is SqlException { Number: 2601 or 2627 }
        || ex.InnerException is SqlException { Number: 2601 or 2627 };
    public static bool SerializationFailure(Exception ex) => ex is SqlException { Number: 1205 or 3960 }
        || ex.InnerException is SqlException { Number: 1205 or 3960 };
    public static bool Concurrency(Exception ex) => Duplicate(ex) || SerializationFailure(ex);
    public static string InsertMissing(DbContext db, string table, string columns, string values, string predicate, string legacy)
        => db.Database.IsSqlServer()
            ? $"INSERT INTO {table} ({columns}) SELECT {values} WHERE NOT EXISTS (SELECT 1 FROM {table} WITH (UPDLOCK,HOLDLOCK) WHERE {predicate})"
            : legacy;
    public static void AddValue(this DbParameterCollection parameters, string name, object value)
    {
        // Sqlite and SqlClient both accept DbParameter instances from their own factory.
        DbParameter parameter = parameters.GetType().Namespace?.Contains("SqlClient") == true
            ? new SqlParameter() : new Microsoft.Data.Sqlite.SqliteParameter();
        parameter.ParameterName = name; parameter.Value = value; parameters.Add(parameter);
    }
}
