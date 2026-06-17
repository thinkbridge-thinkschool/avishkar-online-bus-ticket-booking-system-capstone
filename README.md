# Online Bus Ticket Booking System

## Project Overview

.NET 10 Clean Architecture API deployed to Azure via the Azure Developer CLI (`azd`) with Azure Deployment Stacks managing both `dev` and `prod` environments, and Managed Identity for all Azure resource access.

## Architecture

Four-layer Clean Architecture deployed as a Linux App Service on Azure:

| Layer | Responsibility |
|---|---|
| Domain | Entities, value objects, domain rules |
| Application | CQRS handlers, repository interfaces, event contracts |
| Infrastructure | EF Core, Service Bus, repositories, background services |
| Api | Minimal API endpoints, DI wiring, auth middleware |

## Contents

- [Day 24 — azd + Azure Deployment Stacks](#day-24--azd--azure-deployment-stacks)
- [Day 25 — Identity End-to-End](#day-25--identity-end-to-end)

---

## Day 24 — azd + Azure Deployment Stacks

**Online Bus Ticket Booking System** — .NET 10 Clean Architecture API deployed to Azure via the Azure Developer CLI (`azd`) with Azure Deployment Stacks managing both `dev` and `prod` environments.

---

## What Deployment Stacks give you over plain deployments

A plain `az deployment group create` is fire-and-forget — ARM deploys resources and has no memory of them. An **Azure Deployment Stack** owns every resource it created:

- **Orphan prevention** — drop a resource from the Bicep template and the stack *deletes* it on the next run; no forgotten resources accumulate.
- **Drift detection & prevention** — the stack attaches `denyWriteAndDelete` deny assignments to every managed resource, so any out-of-band manual write or delete fails immediately with `DenyAssignmentAuthorizationFailed` rather than silently drifting.
- **Atomic teardown** — `azd down` or `az stack group delete --action-on-unmanage deleteAll` removes every stack-managed resource in one operation.

---

## Project structure

```
.
├── azure.yaml                          # azd project config — Deployment Stacks entry point
├── infra/
│   ├── main.bicep                      # orchestrator (env-driven SKU derivation)
│   ├── main.parameters.json            # azd token-substitution bridge
│   ├── main.dev.bicepparam             # dev params (Basic SQL, B1 App Service)
│   ├── main.prod.bicepparam            # prod params (S2 SQL, B2 App Service)
│   └── modules/
│       ├── sql.bicep
│       ├── servicebus.bicep
│       └── api.bicep
└── src/
    ├── BusBooking.Api/
    ├── BusBooking.Application/
    ├── BusBooking.Domain/
    └── BusBooking.Infrastructure/
```

---

## azd configuration — `azure.yaml`

```yaml
name: bus-booking

services:
  api:
    project: ./src/BusBooking.Api
    language: dotnet
    host: appservice

infra:
  provider: bicep
  path: ./infra
  module: main

  # Azure Deployment Stacks (requires: azd config set alpha.deployment.stacks on)
  deploymentStacks:
    actionOnUnmanage:
      resources: delete          # resources dropped from template are DELETED
      resourceGroups: delete
      managementGroups: detach
    denySettings:
      mode: denyWriteAndDelete   # blocks manual writes/deletes → drift prevention
      applyToChildScopes: true   # deny assignments cascade to child resources
      excludedPrincipals:
        - bed97e4c-dfbd-4aa1-a1e4-ec7af963a676  # deploying user exempted for azd publish
```

![azure.yaml deploymentStacks config](Screenshots/05_vscode_azure_yaml_deploymentStacks_config.png.png)

---

## Environment-driven SKU derivation (`infra/main.bicep`)

SKU choices are resolved inside `main.bicep` from the `environment` parameter — no SKU parameters exposed to callers:

```bicep
var sqlSkuName        = environment == 'prod' ? 'S2'    : 'Basic'
var sqlCapacity       = environment == 'prod' ? 50      : 5
var serviceBusSku     = 'Standard'
var appServicePlanSku = environment == 'prod' ? 'B2'    : 'B1'
```

| SKU | dev | prod |
|-----|-----|------|
| Azure SQL | Basic (5 DTU, 2 GB) | S2 (50 DTU, 100 GB) |
| App Service | B1 (1 vCPU, 1.75 GB) | B2 (2 vCPU, 3.5 GB) |
| Service Bus | Standard | Standard |

---

## Deploy commands

### Prerequisites

```powershell
# Enable Deployment Stacks alpha feature (one-time per machine)
azd config set alpha.deployment.stacks on

azd auth login
```

### Dev environment

```powershell
azd env new dev
azd env set AZURE_SUBSCRIPTION_ID <id>   --environment dev
azd env set AZURE_RESOURCE_GROUP   rg-busbooking-dev --environment dev
azd env set AZURE_LOCATION         southeastasia      --environment dev
# Write SQL_ADMIN_PASSWORD to .azure/dev/.env (never in CLI args)

azd up --environment dev
```

### Prod environment

```powershell
az group create --name rg-busbooking-prod --location southeastasia

azd env new prod
azd env set AZURE_SUBSCRIPTION_ID <id>            --environment prod
azd env set AZURE_RESOURCE_GROUP   rg-busbooking-prod --environment prod
azd env set AZURE_LOCATION         southeastasia      --environment prod
# Write SQL_ADMIN_PASSWORD to .azure/prod/.env

azd provision --environment prod
dotnet ef database update --project src/BusBooking.Infrastructure \
                           --startup-project src/BusBooking.Api
azd deploy --environment prod
```

---

## azd environments registered

```
NAME    DEFAULT   LOCAL
dev     true      true
prod    false     true
```

![azd env list showing dev and prod](Screenshots/06_terminal_azd_env_list_dev_prod.png)

---

## Deploy output — both environments live

```
DEV  GET /api/schedules/search  →  200 OK
PROD GET /api/schedules/search  →  200 OK

Stack          State      Deny                Resources
azd-stack-dev  succeeded  denyWriteAndDelete  12
azd-stack-prod succeeded  denyWriteAndDelete  11
```

![Terminal: both APIs live and stack summary](Screenshots/01_terminal_both_apis_live_and_stack_summary.png)

---

## Azure resource groups

Both resource groups provisioned in Southeast Asia:

![Both resource groups in Azure Portal](Screenshots/07_portal_both_resource_groups_dev_prod.png)

---

## Dev Deployment Stack — Portal

Stack `azd-stack-dev` tracking 12 managed resources under `rg-busbooking-dev`:

![Dev Deployment Stack overview — 12 resources](Screenshots/02_portal_azd-stack-dev_overview_12_resources.png)

---

## Prod Deployment Stack — Portal

Stack `azd-stack-prod` tracking 11 managed resources under `rg-busbooking-prod`:

![Prod Deployment Stack overview — 11 resources](Screenshots/03_portal_azd-stack-prod_overview_11_resources_.png)

---

## Deny assignments — drift protection active

The stack attaches `denyWriteAndDelete` deny assignments to every managed resource. Any manual change attempted outside of a stack deployment is blocked:

![Deny assignments — denyWriteAndDelete mode](Screenshots/04_portal_dev_stack_deny_assignments_denyWriteAndDelete.png.png)

---

## Live API endpoints

Both environments expose the same Minimal API surface:

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/schedules/search?source=X&destination=Y&travelDate=YYYY-MM-DD` | Search available schedules |
| `GET` | `/api/schedules/{scheduleId}/seats` | List seats for a schedule |
| `POST` | `/api/bookings` | Create a booking |
| `GET` | `/api/bookings/user/{userId}` | Get user's bookings |
| `POST` | `/api/bookings/{bookingId}/cancel` | Cancel a booking |

OpenAPI spec: `/openapi/v1.json`

| Environment | URL |
|-------------|-----|
| dev | `https://app-busbooking-dev-paqrwn.azurewebsites.net` |
| prod | `https://app-busbooking-prod-wa7imf.azurewebsites.net` |

---

## EF Core migrations applied

Both Azure SQL databases have both migrations applied:

| Migration | Description |
|-----------|-------------|
| `20260612112133_InitialCreate` | Creates all tables (Bookings, Buses, Routes, Schedules, Seats) |
| `20260616060726_SnapshotSync` | Fixes column types, adds `IX_Routes_Source_Destination` index |

---

## Tech stack

- **.NET 10** — ASP.NET Core Minimal APIs, no controllers
- **EF Core 10** — SQL Server, code-first migrations, optimistic concurrency (`RowVersion`)
- **Azure Service Bus** — Standard tier, `booking-confirmed` and `booking-cancelled` topics
- **Azure Bicep** — IaC, three child modules composed by `main.bicep`
- **azd CLI 1.25.2** — `azure.yaml` wires service → infra, `azd up` = provision + build + deploy
- **Azure Deployment Stacks** — lifecycle ownership, deny assignments, atomic teardown

---

## Day 25 — Identity End-to-End

**Goal:** Remove every connection-string secret from the application. After Day 25, the App Service application settings contain zero passwords, zero SAS keys, and zero connection strings with credentials. All Azure resource access uses Azure AD identities.

| Path | Before | After |
|---|---|---|
| API → Azure SQL | Password in connection string | Managed Identity (`Authentication=Active Directory Default`) |
| API → Service Bus | SAS connection string | Managed Identity (`DefaultAzureCredential`) |
| API → clients | No authentication | Entra ID JWT bearer tokens (`Microsoft.Identity.Web`) |
| App Insights connection string | Plain app setting | Key Vault reference (`@Microsoft.KeyVault(...)`) |

---

### Architecture Changes

**Infrastructure (Bicep)**

- **System-Assigned Managed Identity** enabled on the App Service (`identity: { type: 'SystemAssigned' }`). Azure creates and rotates the identity certificate automatically.
- **Azure SQL** configured with a Microsoft Entra ID admin (the deploying user's account set via `Microsoft.Sql/servers/administrators` resource). The connection string uses `Authentication=Active Directory Default` — no `User ID` or `Password`.
- **Service Bus local authentication disabled** (`disableLocalAuth: true`) — all SAS keys are globally non-functional regardless of whether they exist.
- **Service Bus Data Sender RBAC role** assigned to the App Service Managed Identity — least privilege, send-only, no listen or manage.
- **Key Vault** created with `enableRbacAuthorization: true` (RBAC model, no legacy access policies). Stores the Application Insights connection string as a secret.
- **Key Vault Secrets User role** assigned to the App Service Managed Identity — read secrets only, no write or manage.
- **Application Insights connection string** moved from a plain app setting to a Key Vault reference: `@Microsoft.KeyVault(VaultName=kv-...;SecretName=appinsights-connection-string)`. The App Service platform resolves this transparently at runtime.
- **Post-provision hook** (`infra/hooks/postprovision.ps1`) creates the SQL contained database user (`CREATE USER [...] FROM EXTERNAL PROVIDER`) after each `azd provision` — this step cannot be done in Bicep as it requires an Azure AD-authenticated SQL connection.

**New files added**

```
infra/
├── modules/
│   └── keyvault.bicep              # Key Vault + secret + MI Secrets User role
└── hooks/
    ├── postprovision.ps1           # SQL contained user creation (Windows/pwsh)
    └── postprovision.sh            # SQL contained user creation (Linux/bash)
```

**.NET source**

- **`Azure.Identity`** package added to `BusBooking.Infrastructure` — provides `DefaultAzureCredential`.
- **`Microsoft.Identity.Web`** package added to `BusBooking.Api` — provides `AddMicrosoftIdentityWebApiAuthentication`.
- **`InfrastructureServiceExtensions.cs`** — Service Bus client changed from SAS connection string to `new ServiceBusClient(namespace, new DefaultAzureCredential())`. Registration is null-guarded so local dev (no namespace configured) falls back to `NoOpEventPublisher`.
- **`Program.cs`** — `AddMicrosoftIdentityWebApiAuthentication`, `UseAuthentication`, and `UseAuthorization` added. Reads `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Audience` from configuration.
- **`BookingEndpoints.cs`** and **`ScheduleEndpoints.cs`** — `.RequireAuthorization()` added to both endpoint groups.
- **`appsettings.json`** — `ServiceBus` moved out of `ConnectionStrings` into its own section (`ServiceBus:Namespace`); `AzureAd` section added with empty defaults for local dev.

**Entra ID App Registration**

A new app registration **BusBooking API** was created in Microsoft Entra ID:

| Field | Value |
|---|---|
| Application (client) ID | `cc1051c8-d4b5-49c9-a373-8780fb1c2a90` |
| Directory (tenant) ID | `7e394fc8-4b86-4cfe-810e-43f86f8bec47` |
| Audience | `api://cc1051c8-d4b5-49c9-a373-8780fb1c2a90` |

---

### Evidence Screenshots

#### SS-09 — App Service Managed Identity
![SS-09](Screenshots/SS-09_appservice-identity-system-assigned.png)

#### SS-10 — App Service Configuration (No Secrets)
![SS-10](Screenshots/SS-10_appservice-config-no-secrets.png)

#### SS-11 — SQL Server Entra ID Admin
![SS-11](Screenshots/SS-11_sql-aad-admin-portal.png)

#### SS-12 — Service Bus Local Authentication Disabled
![SS-12](Screenshots/SS-12_servicebus-local-auth-disabled.png)

#### SS-13 — Service Bus Shared Access Policies
![SS-13](Screenshots/SS-13_servicebus-no-shared-access-policies.png)

#### SS-14 — Service Bus Data Sender RBAC Assignment
![SS-14](Screenshots/SS-14_servicebus-iam-mi-sender.png)

#### SS-15 — Key Vault Secret
![SS-15](Screenshots/SS-15-keyvault-secret-appinsights.png)

#### SS-16 — Key Vault Secrets User Role Assignment
![SS-16](Screenshots/SS-16-keyvault-iam-mi-secrets-user.png)

#### SS-17 — Microsoft Entra ID App Registration
![SS-17](Screenshots/SS-17-entra-app-registration.png)

---

### Security Improvements

- No SQL passwords stored in code, environment variables, or App Service settings.
- No Service Bus connection strings stored in code or App Service settings.
- No Service Bus SAS authentication — `disableLocalAuth: true` makes all SAS keys non-functional.
- Application Insights connection string stored in Key Vault, never exposed as a plain app setting.
- Managed Identity used for all Azure resource access (SQL, Service Bus, Key Vault) — no credentials to rotate or leak.
- Microsoft Entra ID used for API authentication — every request requires a valid JWT bearer token.
- Zero secrets exposed in App Service application settings — verified via portal screenshot SS-10.
