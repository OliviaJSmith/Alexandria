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

## Notifications

Users get Web Push notifications on every browser where they turned them on (Profile → Notifications):

- **Borrow request:** the lender, when a friend asks to borrow a book.
- **Friend request:** the person who was asked.
- **Due tomorrow / due today:** the borrower of an active loan with a due date. Sent by a daily job (below).

The daily job is `.github/workflows/due-reminders.yml`, a GitHub Actions cron at 15:00 UTC.
- It calls `POST /api/jobs/due-reminders` with the `X-Job-Key` header.
- The key is the repository secret `ALEXANDRIA_JOB_KEY`. It must match the API's `Jobs:Key`, which comes from the azd environment's `JOBS_KEY` and is stored in Key Vault (user-secrets locally).
- "Today" means due within 24 hours, and "tomorrow" means due in 24–48 hours. Each reminder is sent once per loan.
- GitHub only runs scheduled workflows from the default branch.

The API sends them through [Lib.Net.Http.WebPush](https://github.com/tpeczek/Lib.Net.Http.WebPush), using standard `aes128gcm` encryption and VAPID. `public/sw.js` displays them.

- Keys: a VAPID key pair (P-256, base64url raw keys) in `WebPush:PublicKey`, `WebPush:PrivateKey` and `WebPush:Subject` (an `https:` or `mailto:` URL).
  - Locally they go in user-secrets.
  - In Azure they come from the azd environment (`WEBPUSH_PUBLIC_KEY`, `WEBPUSH_PRIVATE_KEY`); the private key is stored only in Key Vault.
  - Without keys, notifications are off and the Profile card is hidden.
- Android, Windows, macOS: works in Chrome, Edge and Firefox.
- iPhone/iPad (iOS 16.4+): only after **Share → Add to Home Screen**, opening Alexandria from that icon.

## Azure

Resources live in the `alexandria-rg` resource group: App Service (API), Key Vault, Azure OpenAI and Azure Vision in West US 3; Static Web Apps (web build) and Azure Database for PostgreSQL in West US 2. The API reads its secrets from Key Vault with its managed identity; OpenAI and Vision API keys are stored only in Key Vault (see the secrets rule in CLAUDE.md).

```bash
azd provision
```

```bash
azd deploy
```

The azd environment (`.azure/`, git-ignored) holds the generated Postgres admin password and JWT signing key.

Hosting knobs (set with `azd env set`): `APP_SERVICE_SKU` (default `F1`, the Free tier; use `B1` for always-on) and `POSTGRES_LOCATION` (defaults to the main region; set it when the subscription can't create PostgreSQL there).

If `azd deploy web` fails while uploading, build and upload the site directly (requires Node.js 22.20 or later):

```bash
cd src/Alexandria.App && npm run build
```

```bash
SWA_CLI_DEPLOYMENT_TOKEN=$(az staticwebapp secrets list -n <static-web-app-name> -g alexandria-rg --query properties.apiKey -o tsv) npx @azure/static-web-apps-cli deploy ./dist --env production --no-use-keychain
```
