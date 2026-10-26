using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Shop.Api.Tenancy;

public static class SubscriptionSchema
{
    public static void Apply(PlatformContext db, bool seedPlans = true)
    {
        db.Database.OpenConnection();
        using (var inspect = db.Database.GetDbConnection().CreateCommand())
        {
            inspect.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Plans'";
            if (Convert.ToInt64(inspect.ExecuteScalar()) == 0)
            {
                var source = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
                string root = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(source.DataSource))!, "backups");
                Directory.CreateDirectory(root);
                using var backup = new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                    { DataSource = Path.Combine(root, "platform-before-subscriptions-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N") + ".db") }.ToString());
                backup.Open(); source.BackupDatabase(backup);
            }
        }
        using var transaction = db.Database.BeginTransaction();
        foreach (string column in new[] { "SubscriptionRequired", "HasUsedTrial" })
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('Stores') WHERE name='{column}'";
            if (Convert.ToInt64(command.ExecuteScalar()) == 0)
                db.Database.ExecuteSqlRaw($"ALTER TABLE Stores ADD COLUMN {column} INTEGER NOT NULL DEFAULT 0");
        }
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS Plans (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL,
                PriceMinor INTEGER NOT NULL, Currency TEXT NOT NULL, PeriodMonths INTEGER NOT NULL, TrialDays INTEGER NOT NULL,
                MaxProducts INTEGER NULL, IsActive INTEGER NOT NULL, Version INTEGER NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Plans_Name ON Plans(Name);
            CREATE TABLE IF NOT EXISTS Subscriptions (StoreId TEXT PRIMARY KEY REFERENCES Stores(Id), PlanId INTEGER NOT NULL REFERENCES Plans(Id),
                PlanName TEXT NOT NULL, PriceMinor INTEGER NOT NULL, Currency TEXT NOT NULL, PeriodMonths INTEGER NOT NULL,
                MaxProducts INTEGER NULL, StartsAtUtc TEXT NOT NULL, EndsAtUtc TEXT NOT NULL, IsTrial INTEGER NOT NULL,
                IsCancelled INTEGER NOT NULL, Version INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS SubscriptionEvents (Id TEXT PRIMARY KEY, StoreId TEXT NOT NULL, Action TEXT NOT NULL,
                PlanId INTEGER NOT NULL, PlanName TEXT NOT NULL, PriceMinor INTEGER NOT NULL, Currency TEXT NOT NULL,
                StartsAtUtc TEXT NOT NULL, EndsAtUtc TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, ActorId TEXT NOT NULL, Reference TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_SubscriptionEvents_StoreId_CreatedAtUtc ON SubscriptionEvents(StoreId,CreatedAtUtc);
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS MerchantApplications (
                Id TEXT PRIMARY KEY, FullName TEXT NOT NULL, Email TEXT NOT NULL, Phone TEXT NOT NULL,
                Username TEXT NOT NULL, PasswordHash TEXT NOT NULL, StoreName TEXT NOT NULL,
                BusinessCategory TEXT NOT NULL, BusinessDescription TEXT NOT NULL, BusinessMode TEXT NOT NULL,
                PlanId INTEGER NOT NULL, PlanName TEXT NOT NULL, TrialDays INTEGER NOT NULL, MaxProducts INTEGER NULL, State TEXT NOT NULL, ReviewNote TEXT NOT NULL, StoreId TEXT NULL,
                ReviewedBy TEXT NULL, CreatedAtUtc TEXT NOT NULL, ReviewedAtUtc TEXT NULL, Version INTEGER NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_MerchantApplications_Email ON MerchantApplications(Email);
            """);
        using(var planColumn=db.Database.GetDbConnection().CreateCommand())
        {
            planColumn.Transaction=transaction.GetDbTransaction();
            planColumn.CommandText="SELECT COUNT(*) FROM pragma_table_info('Plans') WHERE name='AllowMerchantApplications'";
            if(Convert.ToInt64(planColumn.ExecuteScalar())==0)
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE Plans ADD COLUMN AllowMerchantApplications INTEGER NOT NULL DEFAULT 0");
                db.Database.ExecuteSqlRaw("UPDATE Plans SET AllowMerchantApplications=1 WHERE Name='Free Trial' AND PriceMinor=0 AND TrialDays>0");
            }
        }
        // Additive commercial terms and enforced feature snapshots; preserve existing entitlements.
        foreach(var entry in new (string Table,string Column,string Sql)[]{
            (Table:"Plans",Column:"FeaturesJson",Sql:"TEXT NOT NULL DEFAULT '[\"Discounts\",\"Newsletter\",\"StorefrontDesign\",\"Marketplace\"]'"),
            ("Plans","Description","TEXT NOT NULL DEFAULT ''"),
            ("Plans","NameAr","TEXT NOT NULL DEFAULT ''"),
            ("Plans","DescriptionAr","TEXT NOT NULL DEFAULT ''"),
            ("Subscriptions","FeaturesJson","TEXT NOT NULL DEFAULT '[\"Discounts\",\"Newsletter\",\"StorefrontDesign\",\"Marketplace\"]'"),
            ("MerchantApplications","FeaturesJson","TEXT NOT NULL DEFAULT '[\"Discounts\",\"Newsletter\",\"StorefrontDesign\",\"Marketplace\"]'"),
            ("MerchantApplications","PriceMinor","INTEGER NOT NULL DEFAULT 0"),
            ("MerchantApplications","Currency","TEXT NOT NULL DEFAULT 'EGP'"),
            ("MerchantApplications","PeriodMonths","INTEGER NOT NULL DEFAULT 1")})
        {
            using var inspectColumn=db.Database.GetDbConnection().CreateCommand();
            inspectColumn.Transaction=transaction.GetDbTransaction();
            inspectColumn.CommandText=$"SELECT COUNT(*) FROM pragma_table_info('{entry.Table}') WHERE name='{entry.Column}'";
            if(Convert.ToInt64(inspectColumn.ExecuteScalar())==0)
                db.Database.ExecuteSqlRaw($"ALTER TABLE {entry.Table} ADD COLUMN {entry.Column} {entry.Sql}");
        }
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS PlatformWebsite (Id INTEGER PRIMARY KEY,Version INTEGER NOT NULL,Document TEXT NOT NULL)");
        bool seedDrafts=seedPlans && !db.Plans.Any();
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS MerchantOnboardingSetup (Id INTEGER PRIMARY KEY)");
        using var onboardingMarker=db.Database.GetDbConnection().CreateCommand();
        onboardingMarker.Transaction=transaction.GetDbTransaction();
        onboardingMarker.CommandText="SELECT COUNT(*) FROM MerchantOnboardingSetup WHERE Id=1";
        bool initializeOnboarding=seedPlans && Convert.ToInt64(onboardingMarker.ExecuteScalar())==0;
        if (initializeOnboarding && !db.Plans.Any(p => p.Name == "Free Trial"))
        {
            db.Plans.Add(new SubscriptionPlan { Name="Free Trial", PriceMinor=0, TrialDays=14, MaxProducts=50, IsActive=true, AllowMerchantApplications=true });
            db.SaveChanges();
        }
        if(initializeOnboarding)db.Database.ExecuteSqlRaw("INSERT INTO MerchantOnboardingSetup(Id) VALUES(1)");
        if(seedDrafts)
        {
            foreach(var draft in new[]{new SubscriptionPlan {Name="Starter",MaxProducts=100,TrialDays=14,IsActive=false},
                new SubscriptionPlan {Name="Growth",MaxProducts=1000,TrialDays=14,IsActive=false},
                new SubscriptionPlan {Name="Pro",TrialDays=14,IsActive=false}})
                if(!db.Plans.Any(p=>p.Name==draft.Name))db.Plans.Add(draft);
            db.SaveChanges();
        }
        transaction.Commit(); db.Database.CloseConnection();
    }
}
