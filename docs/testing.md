# Testing Guide

## Test Projects

- `tests/Tokenization.UnitTests`
  Fast tests for controllers, middleware, handlers, services, mappers, and infrastructure units.
- `tests/Tokenization.IntegrationTests`
  A mixed suite: fast API-host integration tests plus Docker-backed infrastructure tests.
- `tests/TestCommon`
  Shared fixtures and test helpers used by both suites.

## Commands

Run the fast suite:

```bash
dotnet test tests/Tokenization.UnitTests/Tokenization.UnitTests.csproj
```

Run the Docker-backed suite:

```bash
dotnet test tests/Tokenization.IntegrationTests/Tokenization.IntegrationTests.csproj
```

Run everything through the solution:

```bash
dotnet build TokenizationService.sln
```

## Docker Requirements

The API controller integration tests use an in-memory SQLite database so they stay fast in the local development loop.

The infrastructure-focused integration tests still use Testcontainers for SQL Server and Redis. Docker must be running before you execute those parts of the suite.

When the local development SQL Server from `docker compose up -d` is already available on `localhost,14333`, the SQL-backed fixtures will reuse it instead of cold-starting a fresh SQL container. If that local dependency is not available, the fixtures fall back to Testcontainers automatically.

For local compose SQL Server, `Database:TrustServerCertificate` is an optional override. If your development connection string already includes `TrustServerCertificate=True`, the runtime preserves that value and you do not need to duplicate it as a separate setting.

Typical local flow:

```bash
docker info
dotnet test tests/Tokenization.IntegrationTests/Tokenization.IntegrationTests.csproj
```

If Docker is unavailable, the integration fixtures surface a clear message explaining that the suite requires Docker.

## Key Vault Tests

Azure Key Vault integration tests are opt-in.

The default integration suite uses the in-memory key provider for its crypto-dependent coverage so it can run on any machine with Docker. Only the tests under the dedicated Key Vault path require Azure access.

Before running them:

1. Set `RUN_KEYVAULT_TESTS=true`.
2. Provide Key Vault configuration through `src/Tokenization.Api/appsettings.Development.json` or environment variables.
3. Run the integration project.

## CI

GitHub Actions is configured to run:

- restore/build
- unit tests with coverage collection
- integration tests in a Docker-capable runner

That keeps the default local development loop fast while still preserving a realistic integration path.
