-- One-shot import of legacy ServiceStack UserAuth rows into ASP.NET Core
-- Identity's AspNetUsers table.
--
-- Hash strategy (see LegacyAwarePasswordHasher.cs):
--   * Salt IS NULL  → ServiceStack PBKDF2 hash, wire-compatible with Identity v3.
--                     Copied verbatim into PasswordHash.
--   * Salt NOT NULL → ServiceStack SaltedHash (HMAC-SHA-256). Wrapped as
--                     'SS$<salt>$<hash>' so LegacyAwarePasswordHasher can
--                     verify it on next login and re-hash to v3 transparently.
--
-- Run this against the Users database AFTER the AspNetUsers table exists
-- (i.e. after `dotnet ef database update --context AuthDbContext`).
-- The WHERE clause makes it safe to re-run.

INSERT INTO "AspNetUsers" (
    "Id",
    "UserName",
    "NormalizedUserName",
    "Email",
    "NormalizedEmail",
    "EmailConfirmed",
    "PasswordHash",
    "SecurityStamp",
    "ConcurrencyStamp",
    "PhoneNumberConfirmed",
    "TwoFactorEnabled",
    "LockoutEnabled",
    "AccessFailedCount",
    "FirstName",
    "LastName",
    "DisplayName"
)
SELECT
    gen_random_uuid()::text,
    LOWER(COALESCE(NULLIF("UserName", ''), "Email")),
    UPPER(COALESCE(NULLIF("UserName", ''), "Email")),
    "Email",
    UPPER("Email"),
    TRUE,                                              -- treat pre-existing accounts as confirmed
    CASE
        WHEN "Salt" IS NULL THEN "PasswordHash"
        ELSE 'SS$' || "Salt" || '$' || "PasswordHash"
    END,
    REPLACE(gen_random_uuid()::text, '-', ''),         -- SecurityStamp (Identity expects non-null)
    REPLACE(gen_random_uuid()::text, '-', ''),         -- ConcurrencyStamp
    FALSE,
    FALSE,
    TRUE,
    0,
    "FirstName",
    "LastName",
    "DisplayName"
FROM "UserAuth"
WHERE "Email" IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM "AspNetUsers" au WHERE au."NormalizedEmail" = UPPER("UserAuth"."Email")
  );
