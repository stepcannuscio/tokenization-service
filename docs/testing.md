# Testing Guide

## Test Projects

- `tests/Tokenization.UnitTests`
  Fast tests for controllers, middleware, handlers, services, mappers, and infrastructure units.
- `tests/Tokenization.IntegrationTests`
  End-to-end and infrastructure-heavy tests backed by Testcontainers.
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

The integration project uses Testcontainers for SQL Server and Redis. Docker must be running before you execute the integration suite.

Typical local flow:

```bash
docker info
dotnet test tests/Tokenization.IntegrationTests/Tokenization.IntegrationTests.csproj
```

If Docker is unavailable, the integration fixtures surface a clear message explaining that the suite requires Docker.

## Key Vault Tests

Azure Key Vault integration tests are opt-in.

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
