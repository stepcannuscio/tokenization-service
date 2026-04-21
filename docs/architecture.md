# Architecture Notes

## Overview

`tokenization-service` follows a straightforward layered structure:

```text
Client
  -> Tokenization.Api
      Controllers, auth, versioning, OpenAPI, middleware
  -> Tokenization.Application
      Commands, handlers, validators, orchestration
  -> Tokenization.Domain
      Contracts, entities, value objects, exceptions
  -> Tokenization.Infrastructure
      EF Core, crypto, caching, tenant context, Key Vault, health checks
```

## Request Flow

For a create-token request:

1. `TokensController` accepts the HTTP request and maps it into a command.
2. MediatR sends the command through validation behavior.
3. Application code calls `ITokenService` to coordinate the use case.
4. Infrastructure encrypts the payload, computes blind indexes, and persists the token record.
5. The API maps the result back into a response DTO and returns a versioned `201 Created` location header.

## Security-Oriented Design Choices

- Tenant context is normalized to `tenant_id`, while legacy `merchant_id` claims are still accepted.
- Sensitive payloads are encrypted before persistence.
- Blind indexes support lookups without storing plaintext tenant/customer identifiers in queryable fields.
- Idempotency is required on write operations so repeated calls do not create duplicate tokens.
- Logging and DTO mapping avoid exposing sensitive payment data in normal responses.

## Intentional Tradeoffs

- This repo optimizes for clarity and interview discussion, not full production completeness.
- Development auth is provided to make the project runnable on a clean machine in minutes.
- Database schema creation is explicit in development/test startup rather than hidden in `DbContext` construction.
- The architecture keeps a few abstractions that are useful for testability, while avoiding a full framework-heavy “enterprise” setup.
