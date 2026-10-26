using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shop.DAL.Data;

if (args.Contains("--postgres")) { PostgresChecks.Run(); return; }
if(args.Length==3 && args[0]=="--due-test-email")
{
    string path=Path.GetFullPath(args[1]);
    if(!path.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(Path.GetDirectoryName(path)!).StartsWith("shop-security-")||Path.GetFileName(path)!="test.db")throw new InvalidOperationException("Only isolated security-test data can be modified.");
    using var connection=new SqliteConnection("Data Source="+path);connection.Open();
    using var command=connection.CreateCommand();command.CommandText="UPDATE EmailOutbox SET NextAttemptUtc='2000-01-01T00:00:00.0000000Z' WHERE Id=$id AND State='Pending'";
    command.Parameters.AddWithValue("$id",args[2]);command.ExecuteNonQuery();return;
}
if(args.Length==3 && args[0]=="--expire-test-account-actions")
{
    string path=Path.GetFullPath(args[1]);
    if(!path.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)||
        !Path.GetFileName(Path.GetDirectoryName(path)!).StartsWith("shop-security-")||Path.GetFileName(path)!="test.db")
        throw new InvalidOperationException("Only an isolated security-test store database can be modified.");
    using var connection=new SqliteConnection("Data Source="+path);connection.Open();
    using var command=connection.CreateCommand();command.CommandText="UPDATE CustomerActions SET ExpiresAtUtc='2000-01-01T00:00:00.0000000Z' WHERE CustomerId=$id";
    command.Parameters.AddWithValue("$id",int.Parse(args[2]));
    if(command.ExecuteNonQuery()==0)throw new Exception("No test action found");
    Console.WriteLine("Isolated customer actions expired.");return;
}
if (args.Length == 3 && args[0] == "--expire-test-subscription")
{
    string path = Path.GetFullPath(args[1]);
    if (!path.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) ||
        !Path.GetFileName(Path.GetDirectoryName(path)!).StartsWith("shop-security-") || Path.GetFileName(path) != "platform.db")
        throw new InvalidOperationException("Only an isolated security-test platform database can be modified.");
    using var connection = new SqliteConnection("Data Source=" + path); connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "UPDATE Subscriptions SET EndsAtUtc='2000-01-01 00:00:00', Version=Version+1 WHERE StoreId=$store";
    command.Parameters.AddWithValue("$store", args[2]);
    if (command.ExecuteNonQuery() != 1) throw new Exception("Test subscription not found");
    Console.WriteLine("Isolated test subscription expired."); return;
}

var root = Path.Combine(Path.GetTempPath(), "shop-schema-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int checks = 0;
ShopContext Open(string path) => new(new DbContextOptionsBuilder<ShopContext>().UseSqlite("Data Source=" + path).Options);
void Expect(bool condition, string label)
{
    if (!condition) throw new Exception(label);
    checks++;
}
int Version(ShopContext db)
{
    db.Database.OpenConnection();
    try { return SqliteSchemaUpgrade.ReadVersion(db); }
    finally { db.Database.CloseConnection(); }
}
void Run(string label, Action action)
{
    action(); Console.WriteLine("PASS: " + label);
}
string Create(string name)
{
    var path = Path.Combine(root, name + ".db");
    using var db = Open(path);
    db.Database.EnsureCreated();
    return path;
}
long Scalar(string path, string sql)
{
    using var connection = new SqliteConnection("Data Source=" + path);
    connection.Open(); using var command = connection.CreateCommand(); command.CommandText = sql;
    return Convert.ToInt64(command.ExecuteScalar());
}
Run("fresh database and repeat startup", () =>
{
    var path = Create("fresh");
    using var db = Open(path);
    int products = db.Products.Count();
    SqliteSchemaUpgrade.Apply(db, newlyCreated: true);
    Expect(Version(db) == 9, "Fresh schema version");
    SqliteSchemaUpgrade.Apply(db);
    Expect(db.Products.Count() == products, "Repeat startup preserves products");
    Expect(Scalar(path, "SELECT COUNT(*) FROM __ShopSchemaVersions") == 9, "Versions recorded once");
    Expect(!Directory.Exists(Path.Combine(root, "backups")), "No backup for a fresh/already current schema");
});
Run("existing database is backed up and user configuration preserved", () =>
{
    var path = Create("existing");
    using var db = Open(path);
    var settings = db.StoreSettings.Single(); settings.CurrencyCode = "EUR"; settings.CurrencySymbol = "€";
    db.OrderStatusDefinitions.Single(s => s.IsDefault).Name = "Waiting for approval";
    db.SaveChanges();
    int products = db.Products.Count(); int orders = db.Orders.Count();
    SqliteSchemaUpgrade.Apply(db);
    Expect(Version(db) == 9, "Existing database version");
    Expect(db.Products.Count() == products && db.Orders.Count() == orders, "Rows preserved");
    Expect(db.Currencies.Any(c => c.Code == "EUR"), "Existing custom currency adopted");
    Expect(db.OrderStatusDefinitions.Single(s => s.IsDefault).Name == "Waiting for approval", "Customized status preserved");
    var backups = Directory.GetFiles(Path.Combine(root, "backups"), "existing.db*.db");
    Expect(backups.Length == 1, "One pre-upgrade backup");
    using var backup = Open(backups[0]);
    Expect(Version(backup) == 0 && backup.Orders.Count() == orders, "Backup is readable and predates upgrade");
    SqliteSchemaUpgrade.Apply(db);
    Expect(Directory.GetFiles(Path.Combine(root, "backups"), "existing.db*.db").Length == 1, "Restart does not create repeated backups");
    string restoredPath = Path.Combine(root, "restored.db");
    File.Copy(backups[0], restoredPath);
    using var restored = Open(restoredPath);
    SqliteSchemaUpgrade.Apply(restored);
    Expect(restored.Orders.Count() == orders && Version(restored) == 9, "Backup can be restored and upgraded");
    Expect(restored.StoreSettings.Single().CurrencyCode == "EUR", "Restore retains merchant settings");
});
Run("missing additive columns upgrade without losing existing stock or image", () =>
{
    var path = Create("additive");
    using var db = Open(path);
    var variant = db.ProductVariants.First(); variant.ImageUrl = "/uploads/legacy.png";
    int stock = variant.StockQuantity; int id = variant.Id;
    db.SaveChanges(); db.ChangeTracker.Clear();
    db.Database.ExecuteSqlRaw("ALTER TABLE Orders DROP COLUMN StockDeducted");
    db.Database.ExecuteSqlRaw("ALTER TABLE ProductVariants DROP COLUMN ImageUrls");
    SqliteSchemaUpgrade.Apply(db);
    var upgraded = db.ProductVariants.Single(v => v.Id == id);
    Expect(upgraded.StockQuantity == stock && upgraded.ImageUrl == "/uploads/legacy.png", "Existing values survive additive upgrade");
    Expect(upgraded.ImageUrls.Count == 0 && Version(db) == 9, "New gallery and schema marker initialized");
});
Run("a failed upgrade rolls back schema and history", () =>
{
    var path = Create("rollback");
    using var db = Open(path);
    db.Database.ExecuteSqlRaw("ALTER TABLE Reviews RENAME COLUMN Comment TO BrokenComment");
    int products = db.Products.Count(); bool failed = false;
    try { SqliteSchemaUpgrade.Apply(db); } catch (InvalidOperationException) { failed = true; }
    Expect(failed, "Schema drift rejected");
    Expect(Version(db) == 0, "Failed transaction did not record versions");
    Expect(db.Products.Count() == products, "Failure preserves products");
    Expect(Scalar(path, "SELECT COUNT(*) FROM Orders WHERE StatusDefinitionId IS NOT NULL") == 0, "Backfill rolled back");
});
Run("future versions are rejected without modifying the database", () =>
{
    var path = Create("future");
    using var db = Open(path); SqliteSchemaUpgrade.Apply(db, true);
    db.Database.ExecuteSqlRaw("INSERT INTO __ShopSchemaVersions VALUES (10, 'future')");
    bool failed = false;
    try { SqliteSchemaUpgrade.Apply(db); } catch (InvalidOperationException) { failed = true; }
    Expect(failed && Version(db) == 10, "Newer schema remains intact");
});
Run("unsupported legacy catalog is rejected without resetting data", () =>
{
    var path = Create("unsupported");
    using var db = Open(path);
    db.Database.ExecuteSqlRaw("ALTER TABLE Products RENAME COLUMN StockQuantity TO LegacyQuantity");
    int products = (int)Scalar(path, "SELECT COUNT(*) FROM Products"); bool failed = false;
    try { SqliteSchemaUpgrade.Apply(db); } catch (InvalidOperationException) { failed = true; }
    Expect(failed && Version(db) == 0, "Unsupported baseline rejected");
    Expect(Scalar(path, "SELECT COUNT(*) FROM Products") == products, "Legacy rows preserved");
});
Run("schema history gaps are rejected", () =>
{
    var path = Create("gap");
    using var db = Open(path); SqliteSchemaUpgrade.Apply(db, true);
    db.Database.ExecuteSqlRaw("DELETE FROM __ShopSchemaVersions WHERE Version = 2");
    bool failed = false;
    try { SqliteSchemaUpgrade.Apply(db); } catch (InvalidOperationException) { failed = true; }
    Expect(failed, "Broken history rejected");
});
Run("concurrent startup records upgrades once", () =>
{
    var path = Create("concurrent");
    Task.WaitAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
    {
        using var db = Open(path); SqliteSchemaUpgrade.Apply(db);
    })).ToArray());
    using var verify = Open(path);
    Expect(Version(verify) == 9 && Scalar(path, "SELECT COUNT(*) FROM __ShopSchemaVersions") == 9, "Concurrent startup did not duplicate schema history");
});
Console.WriteLine($"PASS: {checks} database schema assertions. Isolated files: {root}");
