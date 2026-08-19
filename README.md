# Core Libraries

## Overview

This folder contains shared libraries used across the platform. These libraries implement the foundation for:

- DDD patterns and domain primitives
- CQRS implementation and mediator-less dispatching
- Clean architecture support and separation of concerns
- Cross-cutting concerns (caching, localization, auditing, storage)
- Infrastructure abstractions (EF integrations, outbox, background processing)

The artifacts under `libs/` are consumed by application modules and microservices in the solution.

## Architecture philosophy

The core libraries follow these principles:

- DDD: keep domain primitives (entities, value objects, domain events) isolated from infrastructure.
- CQRS: explicit command/query contracts and handlers with pipeline behaviors (no MediatR dependency).
- Clean architecture: directional dependencies (Domain → Application → Infrastructure → Host).
- Single responsibility and small focused libraries to allow selective consumption.

## Libraries overview

Project | Responsibility | Used by | Description
---|---:|---:|---
`TaindSoft.Core` | Shared primitives and general helpers | All modules | Core constants, DTOs, exceptions, time provider
`TaindSoft.Core.Domain` | Domain primitives | Application, Infrastructure | Entities, AggregateRoot, repository and unit-of-work interfaces, domain events
`TaindSoft.Core.Application` | Application / CQRS abstractions | Web / Modules | ICommand/IQuery, handlers, dispatcher, pipelines
`TaindSoft.Core.Infrastructure` | Infrastructure implementations | App modules | EF extensions, domain event dispatcher, outbox, idempotency, storage providers
`TaindSoft.Core.Mapping` | Object mapping abstraction | Application, Infrastructure | `IObjectMapper`, mapping profiles, mapping helpers
`TaindSoft.Core.Caching` | Caching abstractions and implementations | App modules | Cache providers (memory, redis), distributed caching helpers
`TaindSoft.Core.HttpApi` | HTTP API primitives | Web hosts | Endpoint base types, API conventions, ProblemDetails factory
`TaindSoft.Core.HttpApi.Host` | Host-specific middleware/services | Monolith / microservices hosts | Correlation id, global exception handling, health checks, background tasks
`TaindSoft.Core.Localization` | Localization helpers | UI / API | Resource managers, culture provider
`TaindSoft.Core.Identity` | Identity helpers | Hosts / Modules | Current user service, permission checkers, password hashing
`TaindSoft.AdminUI` | Blazor admin UI components | Admin projects | Reusable UI components, navigation, authentication services

## Core capabilities

### Domain support
- `Entity` / `AggregateRoot` base types
- Domain events and dispatching APIs
- Repository and unit-of-work interfaces

### Application support
- `ICommand` / `IQuery` contracts and typed handlers
- `CQRSDispatcher` and pipeline behaviors (validation, logging, transactions)

### Infrastructure support
- Persistence helpers (EF DbContext base, interceptors)
- Outbox pattern and background outbox processor
- Idempotency and seeding abstractions
- Storage providers (local FS, pluggable providers)

### Web support
- Base endpoint type and API registration helpers
- ProblemDetails factory and global exception middleware
- Health checks (including outbox backlog)

## Integration guide

To reference a library from a consuming project inside the solution:

```powershell
dotnet add <consuming-project.csproj> reference ..\libs\TaindSoft.Core\TaindSoft.Core.csproj
```

Common usage patterns:

- Use `ICommand`/`IQuery` implementations and register handlers in DI. Use `CQRSDispatcher` to dispatch.
- Use `IRepository<T>` + `IUnitOfWork` inside command handlers for transactional operations.
- Raise domain events from aggregates and rely on `DomainEventDispatcher` which dispatches in-memory handlers and publishes to outbox.

## Dependency rules
- Domain projects: should have no external dependencies (pure domain).
- Application projects: may depend on Domain only.
- Infrastructure projects: may depend on Application and Domain.
- Host / Web: may depend on Application and Infrastructure.

## Suggested improvements
- Add a single consolidated developer guide with DI registration snippets for common host types.
- Provide more examples for the mapping and CQRS pipelines to reduce onboarding friction.
- Add tests / usage samples showing UnitOfWork / transaction ownership.

---

For per-project details, see each project's README under `libs/TaindSoft.*`.