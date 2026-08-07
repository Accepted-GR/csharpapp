# C# Accepted Assessment app

A .NET 9 web API that wraps the Platzi Fake Store API
(<https://api.escuelajs.co>) and serves products and categories.

This is my submission for the Accepted assessment. The original assignment brief
is preserved in [`docs/ASSIGNMENT.md`](docs/ASSIGNMENT.md).

---

## Quick start

```bash
git clone https://github.com/Inde16/csharpapp.git
cd csharpapp/src
dotnet run --project CSharpApp.Api --launch-profile http
```

The API listens on <http://localhost:5225>. It runs immediately after clone — the
public demo credentials are in `appsettings.json`.

```bash
curl http://localhost:5225/api/v1/products
```

Run the tests:

```bash
cd src && dotnet test
```

> 79 tests, no network required.

---

## Endpoints

| Method | Route | Returns |
| --- | --- | --- |
| `GET` | `/api/v1/products` | `200` all products |
| `GET` | `/api/v1/products/{id}` | `200`, or `404` if absent |
| `POST` | `/api/v1/products` | `201` + `Location`, or `400` |
| `PUT` | `/api/v1/products/{id}` | `200`, `400`, or `404` |
| `DELETE` | `/api/v1/products/{id}` | `204`, or `404` |
| `GET` | `/api/v1/categories` | `200` all categories |
| `GET` | `/api/v1/categories/{id}` | `200`, or `404` |
| `POST` | `/api/v1/categories` | `201` + `Location`, or `400` |
| `PUT` | `/api/v1/categories/{id}` | `200`, `400`, or `404` |
| `DELETE` | `/api/v1/categories/{id}` | `204`, or `404` |
| `GET` | `/api/v1/auth/profile` | `200` the authenticated user |

`GET /api/v1/getproducts` was **replaced** by `GET /api/v1/products`. The verb
belonged in the method, not the path.

OpenAPI is served at `/openapi/v1.json` in Development.

---

## The headline bug

The brief noted that "the use of http client is not so much efficient". In
practice it was worse than inefficient — **the only implemented endpoint worked
exactly once per process lifetime**:

```
#1 -> 200
#2 -> 500
#3 -> 500
```

```
System.InvalidOperationException: This instance has already started one or more
requests. Properties can only be modified before sending the first request.
   at System.Net.Http.HttpClient.set_BaseAddress(Uri value)
```

`ProductsService` was a singleton that constructed its own `HttpClient` and
assigned `BaseAddress` on every call. `BaseAddress` may only be set before the
first request is sent, so the second call threw.

Registering it as a typed client through `IHttpClientFactory`, with the address
configured once, removes the mutation rather than working around it. Response
time also drops from **1.44s to ~0.19s** after the first call, which is
connection reuse becoming visible.

A regression test calls the endpoint three times through the real container.

---

## What was wrong, and what changed

Before writing any feature code I audited the repository and recorded 14 findings,
each verified against the running application or the compiler rather than inferred
from reading. The full write-up with reproduction commands is in
[`docs/AUDIT.md`](docs/AUDIT.md).

| # | Finding | Fixed |
| --- | --- | --- |
| 1 | Repeated requests crash with `InvalidOperationException` | Typed client via `IHttpClientFactory` |
| 2 | `Categories` / `Auth` paths resolve to the wrong URL (404) | Paths corrected, and validated at startup |
| 3 | `AddHttpConfiguration()` was an empty no-op | Resilience pipeline implemented |
| 4 | `Product.Images` silently dropped every image URL | Setter added; regression test |
| 5 | `BuildServiceProvider()` inside a DI registration | `IConfiguration` passed as a parameter |
| 6 | `AddProblemDetails()` registered but never activated | Global exception handler wired |
| 7 | Possible null dereference (CS8602) | Handled explicitly and logged |
| 8 | Undisposed `HttpClient` owned by a service (CA1001) | Ownership moved to the factory |
| 9 | Injected `ILogger` never used — no logging at all | Source-generated logging throughout |
| 10 | Duplicate / unused `PackageReference` (NU1504) | Removed |
| 11 | No `CancellationToken` on any async path | Threaded end to end |
| 12 | `Price` as `int`; `slug` missing from DTOs | `decimal`; `slug` added |
| 13 | Verb-in-path routing | RESTful resource routes |
| 14 | Credentials committed to `appsettings.json` | user-secrets and environment variables supported |

**Finding #4 is worth singling out.** `Images` was declared get-only, which
`System.Text.Json` skips, so every upstream image URL was dropped and the field
serialized back out as an empty array — with no error anywhere:

```jsonc
["https://i.imgur.com/cHddUCu.jpeg"]   // upstream
[]                                     // what we returned
```

---

## Architecture

Four projects, dependencies pointing inward. This was the existing shape; I kept
it rather than imposing a new one.

```
CSharpApp.Api             endpoints, middleware, pipeline
      ↓
CSharpApp.Infrastructure  DI wiring, BearerTokenHandler
      ↓
CSharpApp.Application     services, validators, JwtExpiry
      ↓
CSharpApp.Core            DTOs, interfaces, settings, exceptions   ← no dependencies
```

### A request, end to end

```
1  RequestPerformanceMiddleware   monotonic timer starts, CorrelationId pushed
2  UseExceptionHandler            wraps everything below
3  routing → ProductEndpoints     resolves IProductsService
4  ProductsService                GET products/42 on its typed client
5    └ BearerTokenHandler         attaches "Authorization: Bearer …"
6        └ resilience pipeline    total timeout → retry → breaker → attempt timeout
7            └ HttpClientHandler  the network call
8  UpstreamResponse               translates upstream's non-standard responses
9  TypedResults.Ok                200 + JSON
10 middleware finally block       logs method, route, status, elapsed ms
```

Two ordering decisions carry weight:

- **The performance middleware runs first**, ahead of the exception handler, so
  the cost of handling a failure is measured too. That is the timing most worth
  knowing, and registering it later would have silently excluded it.
- **The bearer handler sits outside the resilience pipeline**, so retries reuse
  one token instead of re-authenticating per attempt, and a 401 replay re-runs
  the whole resilient call.

### Resilience

Every typed client carries the same pipeline, built from `HttpClientSettings` —
settings that were bound from configuration but never read before.

| Strategy | Purpose |
| --- | --- |
| total timeout | hard ceiling on the whole operation, retries included |
| retry | exponential backoff with jitter, transient failures only |
| circuit breaker | 50% failure ratio over 30s, then fail fast for 15s |
| attempt timeout | cuts one slow attempt short so it can be retried |

Uses **Polly v8** (`Microsoft.Extensions.Http.Resilience`). The repository
referenced the v7 package without using it; v8 is the supported approach on
.NET 9, with declarative options and explicit ordering rather than order implied
by nesting.

### Authentication

`AuthService` is a singleton so the token cache is shared — the upstream access
token is valid for 20 days, so authenticating per request would be pure waste.

```
GetAccessTokenAsync()
  ├─ cached and still valid?  → return it (no lock taken)
  └─ otherwise
       ├─ enter SemaphoreSlim
       ├─ re-check inside the gate    ← 20 concurrent callers ⇒ 1 login
       ├─ refresh token available?    → POST /auth/refresh-token
       │     └─ failed? fall through — expected, not an error
       ├─ POST /auth/login
       └─ read `exp` from the JWT, cache with a 2-minute skew margin
```

Two details prevent subtle failures:

- The auth client is a **named client with no bearer handler**. Acquiring a token
  must not itself require a token, or authentication recurses.
- Because it is a singleton, `AuthService` takes `IHttpClientFactory` and creates
  a client per call rather than holding one. A singleton holding an `HttpClient`
  pins its handler for the lifetime of the process and defeats rotation — the
  same class of problem the refactor set out to fix.

Expiry is read from the token's `exp` claim rather than assumed. No JWT library is
pulled in to do so: we are the client, not the resource server, and validation is
the upstream's responsibility. Reading one unauthenticated claim is all that is
needed — and the upstream access token lasts 20 days while its refresh token
lasts 10 hours, the reverse of the usual arrangement, which is exactly what a
hard-coded assumption would get wrong.

`GET /api/v1/auth/profile` exists because **nothing else can demonstrate that auth
works**: the upstream API returns `201` for `POST /products` with no token at all.

---

## Correcting the upstream API

A substantial part of this solution is *not* passing upstream's behaviour through.

| Upstream | This API |
| --- | --- |
| missing entity → `400` + `EntityNotFoundError` | **`404`** |
| malformed body → `500 Internal server error` | **`400`** with per-field messages |
| create → `200` | **`201`** + `Location` header |
| delete → `200`, body `true` | **`204`** |
| product `PUT` with a partial body → `500` | full replacement, validated first |

The not-found translation inspects the response **body**, not only the status, so
a genuinely malformed request stays a bad request instead of becoming a `404`.

Validation runs in an endpoint filter, so an endpoint can assume its body is valid
and none can forget to check. It is not conformance for its own sake — upstream
answers a product with a missing price with an opaque `500`, so this is a real
improvement over what is being wrapped:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Price": ["Price is required and must be greater than zero."],
    "Images[0]": ["Each image must be an absolute http or https URL."]
  }
}
```

Everything else maps through a global exception handler to RFC 9457
ProblemDetails:

```
BrokenCircuitException        → 503     UpstreamNotFoundException     → 404
TimeoutRejectedException      → 504     AuthenticationFailedException → 502
HttpRequestException          → 502     anything unrecognised         → 500
JsonException                 → 502
```

The handler branches on whether the exception was *recognised*, not on its status
code. Every mapped upstream failure is itself a 5xx, so a `statusCode >= 500` test
would classify all of them as unhandled defects and make a routine upstream blip
look like a bug in this service.

---

## Observability

`RequestPerformanceMiddleware` implements feature #4. Every request logs method,
route, status and elapsed milliseconds as **structured** properties:

```json
{"Method":"GET","Route":"/api/v{version:apiVersion}/products",
 "StatusCode":200,"ElapsedMs":140.79,"CorrelationId":"0HNNKATNVCNJK:00000001"}
```

- The **route pattern** is logged, not the raw path. `/products/{id}` aggregates;
  `/products/42` would produce a separate timing series per id.
- Requests at or above a configurable threshold (default 500ms) escalate to
  `Warning`. Logging everything at one level makes the slow ones invisible.
- Timing uses `TimeProvider.GetTimestamp`, which is monotonic — a clock adjustment
  mid-request cannot produce an absurd duration.
- A correlation id is pushed onto the Serilog `LogContext`, so **every** log
  written during a request carries it. An inbound `X-Correlation-Id` is honoured
  so a request can be traced across service boundaries.

Responses carry `X-Correlation-Id` and `X-Response-Time-Ms`.

---

## Configuration

Settings are bound with `ValidateDataAnnotations().ValidateOnStart()`, so a bad
value fails the host at startup with a precise message rather than surfacing later
as a puzzling 404. A custom `RelativePathAttribute` rejects the leading-slash
mistake behind finding #2 and suggests the corrected value.

Credentials resolve through the normal provider chain — `appsettings.json`, then
user-secrets, then environment variables — so the committed defaults keep the
project runnable while real values stay out of the repository:

```bash
cd src/CSharpApp.Api
dotnet user-secrets set "RestApiSettings:Username" "you@example.com"
dotnet user-secrets set "RestApiSettings:Password" "your-password"
```

Full reference, including provider precedence and what a production deployment
would do differently: [`docs/CONFIGURATION.md`](docs/CONFIGURATION.md).

---

## Testing

**79 tests, no network required.**

| Area | Tests | Notable |
| --- | ---: | --- |
| `JwtExpiry` | 12 | base64url padding; malformed input degrades rather than throws |
| `AuthService` | 11 | **20 concurrent callers ⇒ 1 login**; skew margin; refresh fallback |
| `BearerTokenHandler` | 6 | 401 replay; **request body survives the replay** |
| `ProductsService` | 11 | `Images` regression; a plain 400 must not become a 404 |
| Validators | 15 | rejects `javascript:` and relative image URLs |
| Performance middleware | 5 | threshold boundary; a throwing request is still measured |
| Integration | 19 | real DI graph, pipeline, routing, ProblemDetails |

The technique everything rests on is a **stubbed `HttpMessageHandler`**. It matters
for three reasons: the upstream is a mutable public demo whose data changed
mid-testing, so live assertions would be flaky; it makes otherwise undriveable
paths testable — opening the circuit breaker needs ten failures in a sampling
window, impractical by hand but immediate with a stub; and CI needs no network.

Integration tests boot the **real** application with `WebApplicationFactory` and
replace only the outbound transport, so the DI graph, middleware pipeline,
routing and ProblemDetails writer are all genuine.

`FakeTimeProvider` drives every expiry and timing case, which is why
`TimeProvider` is injected rather than reading `UtcNow` — a 20-day expiry is
otherwise untestable, and a "slow" request costs nothing to simulate.

Two tests exist because of bugs made during this work: a regression test for
finding #1, and a container-validation test added after an unregistered
`TimeProvider` compiled cleanly and failed only at runtime.

---

## Decisions and trade-offs

**CQRS was considered and not adopted.** The brief lists it as a strong plus, and
a MediatR implementation was built and then removed. With plain services already
in place, adding a mediator would have meant two architectures in one solution
rather than one done well. Validation still runs at the boundary — as an endpoint
filter instead of a pipeline behaviour — so the guarantee it provides is
unchanged. This is the item I would revisit first with more time.

**Docker is not included.** It is in the brief and it is absent; I would rather
say so than let it be discovered. A multi-stage Dockerfile on `aspnet:9.0` with a
non-root user is the next thing I would add.

**Users, files and GraphQL endpoints are deliberately out of scope.** They exist
upstream and appear in the Postman collection, but the brief specifies products,
categories, auth and the middleware. The collection documents the whole upstream
API, not the assignment. Adding another resource is mechanical — the categories
work is the evidence.

**MediatR would have been pinned to 12.x** (Apache-2.0). Later majors moved to a
commercial licence above a revenue threshold, and nothing here needed them.

**`TreatWarningsAsErrors` is on.** It was switched on at the end of the refactor,
once the pre-existing warnings were gone, and it has since caught a real leak
(`CA1001`, an undisposed `SemaphoreSlim`) at build time.

---

## Working method

Ten branches, each merged through a pull request, with commits that explain *why*
rather than restating the diff. The history is meant to be readable in order.

Three bugs I introduced during this work were caught before merge and are
documented in the commit messages that fixed them:

- **A captive dependency** — a singleton holding an injected `HttpClient`, pinning
  its handler for the process lifetime. Found by re-reading my own registration.
  It would have passed every test and degraded only after hours of uptime.
- **Profile on the unauthenticated client** — `GetProfileAsync` sat on the service
  that must not carry a token, so it returned 401. The token producer should not
  also be a token consumer; hence `IProfileService`.
- **An unreachable log branch** — the exception handler chose its level with
  `statusCode >= 500`, which classified every mapped upstream failure as an
  unhandled defect. Caught by reading log output during verification.

A fourth was found by the first test run: `JsonElement.TryGetInt64` throws on a
non-numeric element rather than returning `false`, so a token with a string `exp`
would have escaped as an unhandled 500. It survived manual verification because
the live API never returns a malformed token — precisely the class of defect curl
cannot find.

---

## Repository map

```
docs/
  ASSIGNMENT.md      the original brief, preserved
  AUDIT.md           14 findings with reproduction commands
  CONFIGURATION.md   every setting, provider precedence, production notes
src/
  CSharpApp.Api/             endpoints, middleware, Program.cs
  CSharpApp.Application/     services, validators, JWT expiry parsing
  CSharpApp.Core/            DTOs, interfaces, settings, exceptions
  CSharpApp.Infrastructure/  DI wiring, bearer token handler
  CSharpApp.Tests/           79 tests
postman.json / insomnia.json  upstream API collections, as supplied
```
