# Day 32 Postmortem — Online Bus Ticket Booking System

**Avishkar · ThinkBridge Capstone · June 26, 2026 · DEPLOYED**  
Live: `https://app-busbooking-prod-wa7imf.azurewebsites.net`

---

## What I'd Do Differently

**1. Grant `db_ddladmin` to the Managed Identity in Bicep from day one.**  
`MigrateAsync()` creates `__EFMigrationsHistory` on first run — that's a DDL operation. The Bicep only granted `db_datareader` + `db_datawriter`, so every first startup in Azure failed with "CREATE TABLE permission denied." Should have been in the Bicep post-provision script on Day 25.

**2. Never call `EnsureDeletedAsync` outside a `IsDevelopment()` guard.**  
The seeder called it to wipe the schema on a fresh DB. In Azure, that connects to the `master` database to issue `DROP DATABASE` — a Managed Identity user doesn't exist in master. It always throws. Migrations handle schema creation idempotently; `EnsureDeletedAsync` should only exist in local dev.

**3. Validate config values by format, not just presence.**  
When an Azure Key Vault reference fails, the App Service returns the literal `@Microsoft.KeyVault(...)` string — not null, not empty. `!string.IsNullOrEmpty()` passes right through it. The guard needed to be `value.StartsWith("InstrumentationKey=")`.

**4. Check Azure resource naming limits before writing Bicep.**  
Key Vault max is 24 characters. The generated name was 25. One character caused two deployment crashes because fixing the name left the already-deployed App Service setting pointing at the old (now deleted) vault.

**5. Register the production redirect URI in the `azd` post-provision hook.**  
It was a manual step — easy to forget, not in any checklist. A single `az rest --method PATCH` call to MS Graph belongs in `postprovision.ps1` alongside the SQL contained-user creation.

---

## Hardest Bug: The Key Vault Cascade

No single error was the bug. It was three failures with no visible connection between them.

The Key Vault name exceeded Azure's 24-char limit by one character. I shortened the suffix and reprovisioned. But the App Service setting `APPLICATIONINSIGHTS_CONNECTION_STRING` — written during the previous provision — still pointed at the old vault name, which was now gone.

When a KV reference fails, the App Service platform doesn't return null. It returns the literal `@Microsoft.KeyVault(VaultName=kv-busbooking-prod-wa7imf;...)` string as the config value. The `IsNullOrEmpty` guard in `Program.cs` passed. Azure Monitor's OpenTelemetry exporter received this garbage, tried to parse `InstrumentationKey` out of it, and threw. The host crashed during `StartAsync`. Crash loop. 503.

```
FAIL  KV reference: VaultName=kv-busbooking-prod-wa7imf (25 chars, gone)
FAIL  App Service resolves setting to literal string, not null
      → "@Microsoft.KeyVault(VaultName=kv-busbooking-prod-wa7imf;...)"

      if (!string.IsNullOrEmpty(value))   // ← passes. not empty.
          otelBuilder.UseAzureMonitor();  // ← reads config internally

FAIL  AzureMonitorMetricExporter: Required keyword 'InstrumentationKey'
      is missing in connection string.
FAIL  Unhandled exception → Host.StartAsync → process abort
FAIL  App Service crash loop → HTTP 503

FIX   Set APPLICATIONINSIGHTS_CONNECTION_STRING directly (bypass KV)
FIX   Guard: value.StartsWith("InstrumentationKey=")
OK    App starts. Seeder runs. 4 cities, 8 routes, 266 schedules.
```

Fix: set `APPLICATIONINSIGHTS_CONNECTION_STRING` directly from the real connection string (bypassing KV entirely), and harden the guard to `value.StartsWith("InstrumentationKey=")`. Finding the log and tracing three-layer failures took most of a day. The actual fix took ten minutes.

---

## Proudest Achievement: Zero Secrets in the Connection String

The production SQL connection string has no username, no password, and no shared key. Just one non-obvious keyword: `Authentication=Active Directory Default`.

That tells Microsoft.Data.SqlClient to use `DefaultAzureCredential`, which on App Service finds the system-assigned Managed Identity, acquires a short-lived Entra ID token, and presents it to SQL Server. SQL recognises the identity because a contained user was created by name via `sqlcmd`. No secret is ever transmitted — the proof of identity is a signed JWT that expires in minutes.

Five pieces had to work in sequence: the Bicep `identity: SystemAssigned` block, the RBAC role assignment, the `sqlcmd` `CREATE USER ... FROM EXTERNAL PROVIDER` statement, the `db_ddladmin`/`db_datareader`/`db_datawriter` role grants, and the connection string format. Any one of them wrong and it silently fails.

For a capstone, this was unnecessary. A SQL username in an environment variable would have worked in ten minutes. Doing it the right way took two days. It was worth it.

---

**32 days · 10 Azure resources · 266 seeded schedules · 3 crash loops fixed · 0 plaintext secrets**
