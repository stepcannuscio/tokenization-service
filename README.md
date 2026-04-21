# Tokenization Service

A portfolio-ready .NET 9 backend that tokenizes payment credentials with tenant isolation, envelope encryption, idempotency, health checks, and a testable layered architecture.

## What This Is

This project is intentionally scoped as a focused backend service, not a full payment gateway. It is designed to demonstrate how I structure a secure, maintainable API that handles sensitive data carefully while still being easy for another engineer to read, run, and test.

## What It Demonstrates

- Layered `Api`, `Application`, `Domain`, and `Infrastructure` projects
- Token creation, lookup, detokenization, and deletion endpoints
- Tenant-aware authorization using `tenant_id` as the canonical claim
- Envelope encryption with AES-256-GCM and blind indexing for lookups
- Operational safeguards such as idempotency, rate limiting, logging, and health checks
- A split test strategy with fast unit tests and Docker-backed integration tests
- CI-ready repo standards with pinned SDK, analyzers, and GitHub Actions

## Quick Start

### Prerequisites

- .NET SDK `9.0.305` or compatible `9.0.x` feature band
- Docker Desktop or another Docker runtime

### 1. Start local dependencies

```bash
docker compose up -d
```

This starts:

- SQL Server on `localhost,14333`
- Redis on `localhost:6379`

### 2. Create local settings

```bash
cp src/Tokenization.Api/appsettings.Development.example.json src/Tokenization.Api/appsettings.Development.json
```

The checked-in development example is already configured for:

- local SQL Server and Redis
- the in-memory key provider
- development-only bearer auth so you can call the API immediately

### 3. Run the API

```bash
dotnet restore TokenizationService.sln
dotnet run --project src/Tokenization.Api --launch-profile https
```

Swagger is available at `https://localhost:7182/swagger`.

### 4. Smoke test the API

```bash
curl -k https://localhost:7182/api/health
```

## How Auth Works Locally

Production auth still uses JWT bearer validation against the configured OIDC issuer.

For local development only, `appsettings.Development.example.json` enables a simple development bearer token:

- Header: `Authorization: Bearer local-dev-token`
- Tenant claim: `demo-tenant`
- Scopes: `tokens.read`, `tokens.create`, `tokens.delete`, `tokens.detokenize`
- Role: `token-admin`

This path is intentionally development-only and should never be enabled outside `Development`.

## Sample API Call

Create a token with the same headers shown in Swagger:

```bash
curl -k https://localhost:7182/api/v1/tokens \
  -X POST \
  -H "Authorization: Bearer local-dev-token" \
  -H "Idempotency-Key: 11111111-1111-1111-1111-111111111111" \
  -H "X-API-Version: 1.0" \
  -H "Content-Type: application/json" \
  -d '{
    "pan": "4111111111111111",
    "expirationMonth": 12,
    "expirationYear": 2030,
    "cardholderName": "Alex Example",
    "network": "Visa",
    "customerId": "customer-123",
    "paymentMethodType": "Card",
    "tokenType": "OneTime",
    "currency": "USD",
    "country": "US",
    "maxUses": 1
  }'
```

Example response:

```json
{
  "token": "tok_01JW3H6X6S0S5F6M4H0C5J0G8D",
  "maskedData": "411111******1111",
  "last4": "1111",
  "paymentMethodType": "Card",
  "network": "Visa"
}
```

## Testing

Fast unit suite:

```bash
dotnet test tests/Tokenization.UnitTests/Tokenization.UnitTests.csproj
```

Docker-backed integration suite:

```bash
dotnet test tests/Tokenization.IntegrationTests/Tokenization.IntegrationTests.csproj
```

Key Vault integration tests remain opt-in. Set `RUN_KEYVAULT_TESTS=true` and provide Key Vault configuration before running the integration suite.

More detail:

- [Architecture Notes](docs/architecture.md)
- [Testing Guide](docs/testing.md)

## Architecture Notes

```text
HTTP request
  -> Tokenization.Api
  -> MediatR command/handler in Tokenization.Application
  -> domain contracts and value objects in Tokenization.Domain
  -> EF Core, crypto, cache, auth context, and Key Vault adapters in Tokenization.Infrastructure
```

The API layer owns transport concerns, the application layer coordinates use cases, the domain layer defines business contracts, and the infrastructure layer handles persistence and external integrations.

## Tradeoffs

- `EnsureCreated` is used only in explicit development/test startup paths to keep local setup simple without pretending migrations are production-ready.
- Development auth exists purely to improve onboarding; production auth still uses normal JWT bearer validation.
- Integration tests use Testcontainers for realism, which makes Docker a deliberate dependency for that suite.
- The project focuses on readability and security-conscious patterns over maximizing feature breadth.

## License

MIT. See [LICENSE](LICENSE).
