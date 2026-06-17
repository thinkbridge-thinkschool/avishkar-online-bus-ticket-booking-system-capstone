# Online Bus Ticket Booking System

## Project Overview

.NET 10 Clean Architecture API deployed to Azure via the Azure Developer CLI (`azd`) with Azure Deployment Stacks managing both `dev` and `prod` environments.

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
