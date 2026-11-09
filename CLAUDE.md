# Project Overview

This repository contains a .NET 10 web application for managing home libraries.

## Primary Goals

1. Maintain clean architecture.
2. Ensure test coverage remains above 80%.
3. Prioritize readability over cleverness.
4. Minimize external dependencies.

---

# Technology Stack

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- React Native
- TypeScript
- Google sign-in plus JWT authentication
- Azure hosting with Key Vault
- Azure OpenAI and Azure Vision: Vision reads text from a photo of one book cover or a whole bookshelf; OpenAI turns that text into structured titles and authors
- Open Library and Google Books APIs for ISBN lookup and book search
- Expo for React Native

---

# Hosting & Infrastructure

- API: Azure App Service.
- Web build of the Expo app: Azure Static Web Apps.
- Database: Azure Database for PostgreSQL (Flexible Server).
- Infrastructure as Bicep, deployed with `azd`.
- Secrets and service access:
  - Preferred: managed identity with Azure RBAC roles for Key Vault, Azure OpenAI and Azure Vision, with key-based auth disabled.
  - Allowed when the deploying account cannot create role assignments (e.g. it only has Contributor), or when the project owner decides to keep it: Key Vault uses access policies granting the API's managed identity `get` on secrets, and the OpenAI/Vision API keys are stored **only** in Key Vault and reach the API through Key Vault references in App Service settings.
  - Either way, never store API keys, passwords or connection strings in source, committed config files or logs. Code reads endpoints and keys from configuration, so moving from keys to managed identity is a configuration change, not a code change.
  - Current state (2026-11-09): resources live in `alexandria-rg` in subscription `613c8ce3-c66d-4b8b-bc19-ca52cd3fdfc8` (azd environment `alexandria-prod`). The deploying account is Owner there, so the preferred setup is possible; the key-based fallback is kept for now by the project owner's choice. Ask before switching.
- Do not use .NET Aspire or Azure Container Apps (cost).

## Local Development

- Use a locally installed PostgreSQL instance (no Docker).
- Keep local connection strings and secrets in .NET user-secrets, never in committed files.

---

# Reference

- The `old` branch contains the previous implementation. Use it as a reference for feature behavior only; do not copy its structure wholesale.

---

# Architecture Rules

## Backend

- Controllers should be thin.
- Business logic belongs in Services.
- Data access belongs in Repositories.
- Use dependency injection.
- Avoid static state.

## Frontend
- React Native Web development friendly for mobile devices.
- Use functional React components.
- Use hooks over class components.
- Keep components under 300 lines when possible.
- Place reusable UI in `/components`.
- Shared StyleSheet.create modules in /styles.
- No style={{...}} inline objects in components.

---

# Coding Standards

## C#

- Enable nullable reference types.
- Prefer `var` when the type is obvious.
- Use async/await for all I/O operations.
- Avoid `.Result` and `.Wait()`.

Example:

```csharp
public async Task<Customer?> GetCustomerAsync(Guid id)
{
    return await repository.GetByIdAsync(id);
}
```

---

# Testing Requirements

When modifying production code:

- Add or update unit tests.
- Use xUnit.
- Mock dependencies with Moq.
- Do not remove existing tests without explanation.

Coverage targets:

- Services: >90%
- Controllers: >80%

There are no frontend (React Native) tests.

---

# Database Rules

- Use EF Core migrations.
- Never hand-edit migration snapshots.
- Prefer strongly typed LINQ queries.
- Avoid raw SQL unless performance requires it.

---

# Security Requirements

- Validate all user input.
- Parameterize database queries.
- Never log secrets.
- Use least-privilege access patterns.

---

# Pull Request Standards

Every change should include:

1. Summary
2. Reason for change
3. Test evidence
4. Potential risks

---

# AI-Specific Instructions

Before generating code:

1. Look for existing patterns in the codebase.
2. Reuse existing abstractions whenever possible.
3. Match project naming conventions.
4. Prefer modifying existing files over creating new ones.
5. Explain architectural tradeoffs when introducing new patterns.

When uncertain:

- Ask a clarifying question rather than making architectural assumptions.

Avoid:

- Large refactors unless requested.
- Renaming public APIs.
- Adding new dependencies.
- Generating placeholder implementations.