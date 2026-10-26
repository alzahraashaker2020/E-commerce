namespace Shop.DAL.Data;

/// <summary>Portable additive outbox schema, with ISO UTC timestamps.</summary>
public static class EmailOutboxSchema
{
    /// <summary>Outbox and delivery attempts.</summary>
    public const string Sql = """
        CREATE TABLE "EmailOutbox" (
          "Id" text PRIMARY KEY, "Recipient" text NOT NULL, "Subject" text NOT NULL,
          "Payload" text NOT NULL, "FileName" text NOT NULL, "State" text NOT NULL,
          "Attempts" integer NOT NULL, "CreatedAtUtc" text NOT NULL, "NextAttemptUtc" text NOT NULL,
          "ExpiresAtUtc" text NOT NULL, "Lease" text NOT NULL, "LeaseUntilUtc" text NOT NULL,
          "LastError" text NOT NULL, "DeliveredAtUtc" text NOT NULL);
        CREATE INDEX "IX_EmailOutbox_Due" ON "EmailOutbox" ("State", "NextAttemptUtc");
        CREATE TABLE "EmailAttempts" ("Id" text PRIMARY KEY, "MessageId" text NOT NULL REFERENCES "EmailOutbox"("Id"), "AttemptedAtUtc" text NOT NULL, "Outcome" text NOT NULL);
        """;
}
