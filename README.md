# AIMyrtana

A **.NET 10** monorepo for AI agents. The platform provides a reusable core for building products where an LLM agent communicates with users across multiple channels: Telegram, WhatsApp, SMS, and web.

---

## Repository Structure

```
src/
├── Products/          # Products (deployed independently to VPS)
│   ├── AgentForSite/  # AI agent for embedding on websites
│   └── LinguaBot/     # Telegram bot for language learning
│
└── Shared/            # Shared libraries used by all products
    ├── Agents/        # Agent core framework (AgentCore)
    ├── Messaging/     # Channel adapters and messaging runtime
    ├── Model/         # Database access (EF Core + PostgreSQL)
    └── Tools/         # Utility tools (AdminBot, TcpTestClient)
```

---

## Products

### AgentForSite

An AI agent designed to be embedded on external websites. Clients connect via HTTP/WebSocket and the agent answers visitor questions in real time.

| Project | Purpose |
|---|---|
| `AgentForSite.Api` | ASP.NET Core Web API — entry point, serves landing page from `wwwroot` |
| `AgentForSite.WebAdapter` | Adapter for incoming messages from the website |
| `AgentForSite.AdapterInit` | Adapter registration in DI |
| `AgentForSite.AgentImplementations` | Concrete agent implementations (uses AgentCore) |
| `AgentForSite.AgentPolicies` | Agent behavior policies |
| `AgentForSite.ProjectFlows` | Conversation flows/scenarios |
| `AgentForSite.Worker` | Background worker — **not deployed** to production (excluded in CI) |

Deploy path: `dist/agentforsite/api/` → `/var/www/aimyrtana/agentforsite/`

---

### LinguaBot

A Telegram bot for learning foreign languages. Supports lesson scheduling via cron expressions and stores user progress in PostgreSQL.

| Project | Purpose |
|---|---|
| `LinguaBot.Api` | ASP.NET Core Web API — Telegram Webhook endpoint |
| `LinguaBot.Domain` | Domain models and business logic |
| `LinguaBot.Data` | EF Core DbContext, Npgsql, migrations |
| `LinguaBot.Agent` | LLM agent powered by Semantic Kernel |
| `LinguaBot.Scheduler` | Lesson scheduler (Cronos + Telegram.Bot) |
| `LinguaBot.MessageHandlers` | Incoming Telegram message handlers |
| `LinguaBot.AdapterInit` | Telegram adapter registration in DI |

Deploy path: `dist/linguabot/api/` → `/var/www/aimyrtana/linguabot/`

---

## Shared Libraries

### Agents (AgentCore)

A custom agent framework, not tied to any specific LLM provider.

| Library | Purpose |
|---|---|
| `AgentCore.Abstractions` | Agent interfaces and contracts |
| `AgentCore.Integrations` | Integrations with external services (OpenAI, etc.) |
| `AgentCore` | Implementation: turn orchestration, policies, logging |

### Messaging

Abstractions and adapters for messaging over any channel.

| Library | Purpose |
|---|---|
| `Messaging.Abstractions` | Contracts: `IMessage`, `IAdapter`, `IMessageHandler` |
| `Messaging.Runtime` | Message dispatcher — routes incoming messages to the correct handler |
| `WebSites.Abstractions` | Contracts for the web channel |
| `WebSites.Runtime` | Web adapter implementation |
| `Adapters.Telegram` | Telegram Bot API adapter (`Telegram.Bot`) |
| `Adapters.WhatsApp` | WhatsApp adapter |
| `Adapters.Sms` | SMS adapter |
| `Adapters.TcpTest` | TCP adapter for local testing |

### Model

| Library | Purpose |
|---|---|
| `MyOwnDb` | EF Core + Npgsql: base DbContext, DI registration helpers |

### Tools

| Tool | Purpose |
|---|---|
| `MyrtanaAdminTelegramm` | Telegram bot for administering VPS services (stop/start/status) |
| `TcpTestClient.Console` | Console client for testing the TCP adapter locally |

---

## Tech Stack

| Category | Technology |
|---|---|
| Runtime | .NET 10 |
| Web API | ASP.NET Core 10 |
| LLM Orchestration | Custom AgentCore + [Microsoft Semantic Kernel 1.74](https://github.com/microsoft/semantic-kernel) |
| Database | PostgreSQL + EF Core 10 + Npgsql 10 |
| Telegram | [Telegram.Bot 22](https://github.com/TelegramBots/Telegram.Bot) |
| Scheduler | [Cronos 0.9](https://github.com/HangfireIO/Cronos) |
| DI / Hosting | `Microsoft.Extensions.Hosting` / `Microsoft.Extensions.DependencyInjection` |
| CI/CD | GitHub Actions |
| Server | Linux VPS, systemd |

---

## Auto-Deploy (CI/CD)

Deployment is triggered automatically on **push to `main`** when files under `src/` change.

### Build selection logic

```
Changes detected in src/Shared/**?
        │
        ├─ YES → rebuild ALL products
        │
        └─ NO  → check src/Products/<Name>/**
                      │
                      ├─ changed → include in build
                      └─ unchanged → skip
```

If no product is affected, the entire CI pipeline is skipped (`skip=true`).

### Pipeline steps

```
1. Checkout (full history)
2. Decide products to build   ← smart git diff analysis
3. Setup .NET 10
4. dotnet publish (Release)   → dist/<product>/api/
                               → dist/<product>/worker/  (if present)
                               → dist/myrtanaadmintelegramm/  (always)
5. rsync dist/ → VPS /var/www/aimyrtana/
6. systemctl try-restart myrtana-admin-telegram.service
```

> `AgentForSite.Worker` is intentionally excluded from publish — the production configuration does not require a background worker for this product.

### Required Secrets

| Secret | Description |
|---|---|
| `UBUNTU_AI_MYRTANA` | Private SSH key for deployment |
| `REMOTE_HOST` | VPS IP address or hostname |
| `REMOTE_USER` | SSH user on the VPS |

### VPS Services

Each product runs as a systemd service. After deployment, only `myrtana-admin-telegram.service` is restarted automatically; other services are restarted manually as needed or via AdminBot.

---

## Local Development

```bash
# Telegram adapters require a Webhook — for local development
# use Adapters.TcpTest + TcpTestClient.Console instead

dotnet run --project src/Products/AgentForSite/AgentForSite.Api
dotnet run --project src/Products/LinguaBot/LinguaBot.Api
```

For database access, make sure PostgreSQL is available and the connection string is set in `appsettings.Development.json`.
