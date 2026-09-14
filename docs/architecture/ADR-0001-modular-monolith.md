# ADR-0001: Begin as a modular monolith

- Status: Accepted
- Date: 2026-09-11

## Context

Nexora spans CRM, membership, events, finance, engagement, work management, and reporting. These domains need boundaries, but the first implementation does not yet have the traffic profile, independent team ownership, or availability requirements that would justify distributed transactions and multiple production services.

## Decision

Build one ASP.NET Core API and one worker deployment with explicit domain modules. Each module owns its domain, application use cases, persistence configuration, endpoints, and contracts. Modules communicate through public contracts and in-process events. Integration events are persisted through a transactional outbox.

The web client remains a separate Next.js application. A module may be extracted into a service only after a documented scaling, ownership, security, or availability reason appears.

## Consequences

- Local development, deployment, observability, and data consistency remain straightforward.
- Module boundaries must be enforced through code review and architecture tests.
- Some shared infrastructure is intentional, but modules cannot directly depend on another module's internal model.
- Later extraction is possible through established contracts and outbox events.
