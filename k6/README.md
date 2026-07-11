# k6 load testing

HTTP-level load tests, complementary to (not a replacement for) the other two performance
layers already in this repo:

- **`tests/BusBooking.Benchmarks`** — BenchmarkDotNet, measures a single hot-path query
  (`IScheduleRepository.SearchAsync`) at the EF Core/repository level, in-process, with no
  network/serialization/auth overhead.
- **README.md's "App Insights + KQL" section** — real production p50/p99 numbers pulled from
  Application Insights via `percentile(duration, N)` KQL queries.

k6 is the missing third layer: real HTTP requests against the actually-running API, through
the full pipeline (rate limiting, auth, output caching, etc.).

## Running locally

1. Start the API: `dotnet run --project src/BusBooking.Api` (or use an already-running instance
   — these scripts just need a base URL).
2. Run a scenario:
   ```bash
   k6 run k6/scenarios/city-list.js -e BASE_URL=https://localhost:7174
   ```

k6's built-in `http_req_duration` metric reports `p(90)`/`p(95)`/`p(99)` automatically in the
summary — no custom instrumentation needed. The `thresholds` block in each script turns a
percentile into a pass/fail gate (`k6` exits non-zero if crossed), so a script can be used as
a regression check, not just an informational report.

## Scenarios

| Script | Endpoint | Auth |
|---|---|---|
| `city-list.js` | `GET /api/v1/cities` | Anonymous |
| `route-list.js` | `GET /api/v1/routes` | Bearer token (`-e TOKEN=<jwt>`) |
| `schedule-search.js` | `GET /api/v1/schedules/search` | Anonymous — needs `FROM_CITY_ID`/`TO_CITY_ID`/`TRAVEL_DATE` env vars for a schedule that actually exists |

All three are read-only and safe to run repeatedly without corrupting seed data.

## ⚠️ The global rate limiter will dominate any real run

Verified by actually running `city-list.js` against a local instance: the "api" rate-limit
policy (`Program.cs`) is a **fixed 60 requests/minute, shared across all clients — not
partitioned per IP**, and unlike the auth-related limits it is not configurable via
`RateLimits:*` settings. A `k6` run with more than a handful of VUs saturates this within the
first second, and every subsequent request in that 1-minute window gets `429`, not the actual
handler latency. This is expected, correct behavior of the rate limiter doing its job — but it
means:

- For genuine latency measurement, either run against an environment where this policy has
  been temporarily raised/removed, or keep VUs low enough that total request volume stays
  under 60/min (not useful for a real load test, but fine for a quick p95 smoke check).
- Don't be surprised if `http_req_failed` fails its threshold on a fresh run against an
  otherwise-idle API — check the response status codes (`429` = rate limited, working as
  intended; anything else is a real problem) before concluding there's a regression.

## CI: not wired in as a blocking gate

Deliberate trade-off: a meaningful load test needs a warm, realistically-seeded, actually-running
API — not the ephemeral in-memory test doubles `dotnet-tests` uses — and GitHub-hosted runners
are noisy enough that percentile thresholds measured there would produce false-positive
failures unrelated to real regressions. Instead there's a separate `workflow_dispatch`-only
workflow (`.github/workflows/k6-smoke.yml`) a human triggers manually (e.g. pre-release, or
against a deployed dev/staging slot) — it never runs automatically on push/PR. The primary
supported path is local, per above.
