namespace Shop.DAL.Data;

/// <summary>Per-store managed storefront document and opt-in mailing list.</summary>
public static class StorefrontSchema
{
    /// <summary>Portable schema for SQLite and PostgreSQL.</summary>
    public const string Sql="""
        CREATE TABLE "StorefrontContent" ("Id" integer PRIMARY KEY, "Version" integer NOT NULL, "Document" text NOT NULL);
        CREATE TABLE "StorefrontSubscribers" ("Email" text PRIMARY KEY, "JoinedAtUtc" text NOT NULL);
        """;
}
