# Code Audit — Baseline

Findings from reviewing the repository before any functional change was made.
Every item below was verified against the running application or the compiler,
not inferred from reading alone. Reproduction commands are included so the
results can be checked independently.

Baseline commit: `79e16ec`
Environment: .NET SDK 9.0.311 / ASP.NET Core runtime 9.0.18, Windows 11

---

## Summary

| # | Finding | Severity | Fixed in |
|---|---------|----------|----------|
| 1 | Repeated requests crash with `InvalidOperationException` | 🔴 Critical | Phase 1 |
| 2 | `Categories` / `Auth` paths resolve to the wrong URL (404) | 🔴 Critical | Phase 1 |
| 3 | `AddHttpConfiguration()` is an empty no-op; Polly and `HttpClientSettings` unused | 🔴 High | Phase 1 |
| 4 | `Product.Images` is never populated — silent data loss | 🟠 High | Phase 1 |
| 5 | `BuildServiceProvider()` called inside a DI registration extension | 🟠 Medium | Phase 1 |
| 6 | `AddProblemDetails()` registered but no exception handler wired | 🟠 Medium | Phase 1 |
| 7 | Possible null dereference (CS8602) | 🟠 Medium | Phase 1 |
| 8 | `ProductsService` owns an undisposed `HttpClient` (CA1001) | 🟡 Low | Phase 1 |
| 9 | Injected `ILogger` never used (IDE0052) — no logging at all | 🟡 Low | Phase 1 |
| 10 | Duplicate and unused `PackageReference` entries (NU1504) | 🟡 Low | ✅ Phase 0 |
| 11 | No `CancellationToken` on any asynchronous call path | 🟡 Low | Phase 1 |
| 12 | `Price` modelled as `int?`; `slug` missing from DTOs | 🟡 Low | Phase 1 |
| 13 | Verb-in-path routing (`/getproducts`) | 🟡 Low | Phase 4 |
| 14 | Credentials committed to `appsettings.json` | 🟡 Low | Phase 2 |

---

## 1. Repeated requests crash the endpoint 🔴

`ProductsService` is registered as a **singleton** (`DefaultConfiguration.cs:13`),
constructs its own `HttpClient` (`ProductsService.cs:12`), and assigns
`BaseAddress` on **every** call (`ProductsService.cs:19`).

`HttpClient.BaseAddress` may only be set before the first request is sent. Since
the singleton's client survives across requests, the second call throws.

**Reproduction**

```bash
dotnet run --project src/CSharpApp.Api --launch-profile http
for i in 1 2 3; do
  curl -s -o /dev/null -w "#$i -> %{http_code}\n" \
    http://localhost:5225/api/v1/getproducts
done
```

**Result**

```
#1 -> 200
#2 -> 500
#3 -> 500
```

```
System.InvalidOperationException: This instance has already started one or more
requests. Properties can only be modified before sending the first request.
   at System.Net.Http.HttpClient.CheckDisposedOrStarted()
   at System.Net.Http.HttpClient.set_BaseAddress(Uri value)
   at CSharpApp.Application.Products.ProductsService.GetProducts()
```

This is the practical consequence of the "http client is not so efficient" note
in the README: the single supported endpoint works exactly once per process
lifetime.

**Fix** — typed client registered via `IHttpClientFactory`, with `BaseAddress`
configured once at registration time rather than per call.

---

## 2. `Categories` and `Auth` paths resolve to the wrong URL 🔴

```jsonc
"BaseUrl":    "https://api.escuelajs.co/api/v1/",
"Products":   "products",     // correct — relative
"Categories": "/categories",  // leading slash
"Auth":       "/auth/login"   // leading slash
```

A relative URI beginning with `/` is resolved against the **host root**, not
against the base path, so `BaseUrl` + `Categories` yields
`https://api.escuelajs.co/categories` instead of
`https://api.escuelajs.co/api/v1/categories`.

**Reproduction**

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://api.escuelajs.co/categories
curl -s -o /dev/null -w "%{http_code}\n" https://api.escuelajs.co/api/v1/categories
```

**Result** — `404` and `200` respectively.

Latent today because neither path is called yet; it would have broken Features
#2 and #3 on first use.

**Fix** — normalize the configured paths and validate settings at startup so a
malformed value fails fast instead of surfacing as a 404 at runtime.

---

## 3. `AddHttpConfiguration()` is an empty no-op 🔴

```csharp
public static IServiceCollection AddHttpConfiguration(this IServiceCollection services)
{
    return services;   // registers nothing
}
```

It is called from `Program.cs:10` and does nothing. Related dead configuration:

- `Microsoft.Extensions.Http.Polly` is referenced by the Infrastructure project
  but never used — no retry, no circuit breaker, no timeout.
- `HttpClientSettings` (`LifeTime`, `RetryCount`, `SleepDuration`) is bound from
  configuration and never read by anything.

The intended design is visible in the dependencies; the implementation is
missing. **Fix** — Phase 1 populates this method with the typed-client and
resilience registrations the settings clearly anticipate.

---

## 4. `Product.Images` is never populated — silent data loss 🟠

```csharp
[JsonPropertyName("images")]
public List<string> Images { get; } = [];   // no setter
```

`System.Text.Json` defaults to `JsonObjectCreationHandling.Replace`, which
requires a settable property. A get-only collection is skipped during
deserialization, so the list stays empty and the API silently drops the field.

**Reproduction** — compare upstream against our response for the same product:

```bash
curl -s https://api.escuelajs.co/api/v1/products/3 | jq .images
curl -s http://localhost:5225/api/v1/getproducts | jq '.[] | select(.id==3) | .images'
```

**Result**

```json
["https://i.imgur.com/cHddUCu.jpeg"]   // upstream
[]                                     // ours
```

**Fix** — give the property a setter (or apply
`[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]`) and cover
it with a deserialization unit test in Phase 7.

---

## 5. `BuildServiceProvider()` inside a DI registration extension 🟠

```csharp
var serviceProvider = services.BuildServiceProvider();
var configuration = serviceProvider.GetService<IConfiguration>();
```

This builds a **second, throwaway container** during registration. Any singleton
resolved through it is a separate instance from the one the application will
use, and the provider is never disposed. This is the pattern ASP0000 exists to
flag.

**Fix** — accept `IConfiguration` as a parameter, which is what the caller
already has available on `builder`.

---

## 6. `AddProblemDetails()` is registered but never activated 🟠

`Program.cs:11` registers ProblemDetails, but `UseExceptionHandler` is never
called, so nothing converts an exception into a ProblemDetails payload. The 500
responses in finding #1 leaked a full stack trace — acceptable in Development,
but the pipeline has no environment-aware handler to prevent it elsewhere.

**Fix** — wire a global exception handler that maps domain and upstream failures
onto correct status codes.

---

## 7–9. Compiler and analyzer findings 🟠🟡

Surfaced by the build once `latest-recommended` analysis was enabled in Phase 0:

| Rule | Location | Meaning |
|------|----------|---------|
| CS8602 | `ProductsService.cs:25` | `res.AsReadOnly()` on a possibly-null result — a malformed upstream payload yields `NullReferenceException` |
| CA1001 | `ProductsService.cs:3` | Type owns a disposable `HttpClient` field but is not disposable |
| IDE0052 | `ProductsService.cs:7` | `ILogger` is injected and assigned but never read — the service produces no logs |

Finding #9 is worth calling out: Serilog is configured, but the one service in
the application never logs anything.

---

## 10. Duplicate and unused package references 🟡 — fixed

`Microsoft.AspNetCore.OpenApi` was declared twice in `CSharpApp.Api.csproj`
(NU1504 on every restore), and `MediatR.Extensions.Autofac.DependencyInjection`
was referenced although Autofac is not used anywhere.

Fixed in Phase 0, commit `8012136`. Restore is now warning-free.

---

## 11–14. Design and hygiene 🟡

- **No `CancellationToken`** on any async signature. A client that disconnects
  mid-request cannot cancel the upstream call, so the work runs to completion
  regardless.
- **`Price` is `int?`.** Monetary values should be `decimal`. Also, upstream
  returns a `slug` field that neither DTO models.
- **Verb-in-path routing.** `GET /api/v1/getproducts` puts the verb in the URL.
  Replaced by `GET /api/v1/products` in Phase 4.
- **Credentials in `appsettings.json`.** `Username` / `Password` are committed
  to source control. Phase 2 moves them to user-secrets and environment
  variables, keeping the committed defaults so the project still runs
  out of the box.

---

## What this implies for the plan

Findings 1, 3, 5, 7, 8 and 9 all originate in the same place: a hand-rolled
`HttpClient` in a singleton with no factory, no resilience and no logging.
Fixing that one design decision resolves six findings at once, which is why the
HttpClient refactor is Phase 1 and everything else builds on top of it.
