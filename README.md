# Tokenization Service

A standalone .NET 9 tokenization service for payment credentials. It focuses on secure token issuance, detokenization, tenant isolation, and the infrastructure patterns you would expect in a modern backend service without pretending to be a full payment gateway.

## What This Project Demonstrates

- Clean Architecture split across `Api`, `Application`, `Domain`, and `Infrastructure`
- Envelope encryption for sensitive payloads with AES-256-GCM
- Blind indexing for tenant/customer lookups without exposing plaintext identifiers
- JWT-based tenant scoping with `tenant_id` as the canonical claim
- Backward compatibility for legacy `merchant_id` claims
- Idempotency, rate limiting, structured logging, and health checks
- Unit-first test coverage plus Docker-backed integration tests

## Repository Layout

```text
src/
  Tokenization.Api/
  Tokenization.Application/
  Tokenization.Domain/
  Tokenization.Infrastructure/
tests/
  Tokenization.Tests/
TokenizationService.sln
```

## API Surface

- `POST /api/v1/tokens`
- `GET /api/v1/tokens/{tokenId}`
- `DELETE /api/v1/tokens/{tokenId}`
- `POST /api/v1/tokens/{tokenId}/detokenize`
- `GET /api/health`
- `GET /api/health/live`
- `GET /api/health/ready`

The service expects authenticated requests. New integrations should send a `tenant_id` claim. Legacy tokens that still use `merchant_id` are accepted and normalized into the tenant context.

## Local Development

### Prerequisites

- .NET 9 SDK
- Docker Desktop or another Docker runtime

### Start local dependencies

```bash
docker compose up -d
```

This starts:

- SQL Server on `localhost,14333`
- Redis on `localhost:6379`

### Configure the API

Use the checked-in example as your starting point:

```bash
cp src/Tokenization.Api/appsettings.Development.example.json src/Tokenization.Api/appsettings.Development.json
```

The example is set up for local Docker services and the in-memory key provider.

### Run the API

```bash
dotnet restore TokenizationService.sln
dotnet run --project src/Tokenization.Api
```

Swagger is available at `https://localhost:7182/swagger` in development.

## Testing

### Default test command

Runs the unit-focused suite and skips integration namespaces:

```bash
dotnet test tests/Tokenization.Tests/Tokenization.Tests.csproj --filter "FullyQualifiedName!~Integration"
```

### Docker-backed integration tests

These tests use Testcontainers and require Docker:

```bash
dotnet test tests/Tokenization.Tests/Tokenization.Tests.csproj
```

### Key Vault integration tests

These tests are opt-in. Set `RUN_KEYVAULT_TESTS=true` and provide Key Vault settings via `appsettings.Development.json` or environment variables before running the full integration suite.

## Security Notes

This project uses PCI-conscious design patterns, but it is not presented here as audited or certified compliance software. The goal is to demonstrate careful handling of sensitive payment data and multi-tenant boundaries in a portfolio-ready service.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
