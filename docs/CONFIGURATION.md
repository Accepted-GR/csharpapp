# Configuration

## Settings

| Section | Key | Purpose |
|---------|-----|---------|
| `RestApiSettings` | `BaseUrl` | Upstream API base address. Must end with a trailing slash |
| | `Products` | Relative path to the products resource |
| | `Categories` | Relative path to the categories resource |
| | `Auth` | Relative path to the login endpoint |
| | `RefreshToken` | Relative path to the token refresh endpoint |
| | `Profile` | Relative path to the authenticated profile endpoint |
| | `Username` / `Password` | Upstream credentials |
| `HttpClientSettings` | `LifeTime` | Handler rotation interval, in minutes |
| | `RetryCount` | Retry attempts for transient failures |
| | `SleepDuration` | Base retry delay, in milliseconds |
| | `TimeoutSeconds` | Per-attempt timeout |

All paths under `RestApiSettings` are **relative** and must not begin with `/`. A
leading slash resolves against the host root and discards the base path, which was
audit finding #2. `RelativePathAttribute` rejects it at startup rather than letting
it surface as a puzzling 404.

Settings are validated on startup via
`ValidateDataAnnotations().ValidateOnStart()`, so a malformed value fails the host
immediately with a precise message instead of failing on the first request that
happens to use it.

## Credentials

`appsettings.json` carries the public demo credentials
(`john@mail.com` / `changeme`) so the project runs immediately after clone, which
is what the assignment asks for. They are **not** the intended source in a real
deployment.

Configuration providers are applied in order, each overriding the last:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. **user-secrets** (Development only)
4. **environment variables**
5. command line arguments

So anything supplied by a later provider wins without editing a tracked file.

### Local development — user-secrets

Secrets are stored outside the repository, in the user profile, and cannot be
committed by accident:

```bash
cd src/CSharpApp.Api
dotnet user-secrets set "RestApiSettings:Username" "your@email.com"
dotnet user-secrets set "RestApiSettings:Password" "your-password"
dotnet user-secrets list
```

### Containers and CI — environment variables

Nested keys use a double underscore as the separator:

```bash
RestApiSettings__Username=your@email.com
RestApiSettings__Password=your-password
```

> On Git Bash for Windows, a value beginning with `/` is rewritten into a Windows
> path by MSYS before the process ever sees it — `RestApiSettings__Categories=/categories`
> silently arrives as something like `C:/Program Files/Git/categories`. Prefix the
> command with `MSYS_NO_PATHCONV=1` when testing values that start with a slash.
> This cost some time during Phase 1 and produced a false negative when verifying
> the startup validation.

## What a production deployment would change

The credentials would be removed from `appsettings.json` entirely and injected by
the platform's secret store — Key Vault, Secrets Manager, or Kubernetes secrets
mounted as environment variables. They are retained here only so the assignment
is runnable without setup, and because these particular credentials are published
in the upstream API's own documentation.
