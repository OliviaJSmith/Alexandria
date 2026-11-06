# Alexandria

A home library management app: catalog your books, scan covers and shelves, share libraries with friends, and track loans.

## Repository layout

| Path | What it is |
| --- | --- |
| `src/Alexandria.Api` | ASP.NET Core (.NET 10) Web API: controllers → services → repositories, EF Core + PostgreSQL |
| `tests/Alexandria.Api.Tests` | xUnit + Moq tests for the API |
| `src/Alexandria.App` | Expo (React Native + TypeScript) app, also built for the web |
| `infra/` | Bicep templates deployed with `azd` |

Project rules for contributors (human or AI) live in [CLAUDE.md](CLAUDE.md).

## Local development

Prerequisites: .NET 10 SDK, Node.js 22+, a locally installed PostgreSQL.

```bash
dotnet build Alexandria.slnx
dotnet test Alexandria.slnx
```

```bash
cd src/Alexandria.App
npm install
npm run web
```

Local secrets (connection string, JWT signing key) go in .NET user-secrets for `src/Alexandria.Api`, never in committed files.

## Azure

Resources live in the `alexandria-rg` resource group (West US): App Service (API), Static Web Apps (web build), Azure Database for PostgreSQL, Key Vault, Azure OpenAI and Azure Vision. The API reaches Key Vault, OpenAI and Vision with its managed identity; no API keys are stored.

```bash
azd provision
```

```bash
azd deploy
```

The azd environment (`.azure/`, git-ignored) holds the generated Postgres admin password and JWT signing key.
