# ADR-0003: Use an OIDC-backed secure session

- Status: Accepted
- Date: 2026-09-11

## Context

Nexora will hold personal, membership, and financial information. A browser application that stores long-lived bearer or refresh tokens in JavaScript-accessible storage increases the impact of an XSS defect.

## Decision

Use OpenID Connect authorization-code flow with PKCE and a backend-for-frontend session boundary. Store the browser session in secure, HTTP-only cookies. Prefer Microsoft Entra ID for workforce SSO and support ASP.NET Core Identity for managed external accounts when required.

## Consequences

- Authentication remains standards-based and supports SSO and MFA.
- The application must implement CSRF protection and appropriate cookie policies.
- Local development needs a replaceable development identity provider/session adapter.
