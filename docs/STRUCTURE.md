## Структура репозитория (Shared + Products)

Цель: один большой репозиторий (monorepo), где есть **общие проекты** (переиспользуемые библиотеки) и **индивидуальные проекты** (конкретные продукты/лендинги со своими потоками сообщений и своими AI-агентами).

### Ключевые правила

- **Границы зависимостей**:
  - `src/Shared/*` **никогда не зависит** от `src/Products/*`.
  - `src/Products/*` может зависеть от `src/Shared/*`.
- **Общие проекты** содержат “механизмы” (движки, адаптеры, контракты), но **не содержат** конкретные policy/flows конкретного продукта.
- **Индивидуальные проекты** содержат “сценарии” (policy, project flows, message flows, handlers, настройки включения адаптеров).

### Рекомендуемый корень

В корне репозитория держим `src/` (код), а рядом — `docs/`, `infra/`, `tests/` при необходимости.

> Общий код лежит в `src/Shared/` (например, `src/Shared/Model/MyOwnDb`).

---

## Предложенная минимальная структура

```text
repo-root/
  src/
    Shared/                                   # ОБЩИЕ проекты (переиспользуются везде)
      Model/
        MyOwnDb/                              # (1) ваша персональная модель БД (EF Core DbContext + миграции)

      Messaging/                              
        Messaging.Abstractions/               # контракты/интерфейсы: IMessageAdapter, IWebhookHandler, IOutboundSender и т.п.
        Messaging.Runtime/                    # (2) общая "обёртка": роутинг, пайплайны, ретраи, логирование, метрики

        Adapters.Telegram/                    # (2) адаптер Telegram (inbound webhook + outbound send)
        Adapters.WhatsApp/                    # (2) адаптер WhatsApp (через выбранного провайдера)
        Adapters.Sms/                         # (2) адаптер SMS (провайдер/шлюз)

        WebSites.Abstractions/                # (2) контракты для сайтов/виджетов: лид-формы, модели, события
        WebSites.Runtime/                     # (2) общая обёртка для сайтов: rate limit, anti-spam, общие endpoints (опц.)

      Tools/
        TcpTestClient.Console/                # (2) тестовый TCP консольный клиент

      Agents/
        AgentCore/                            # (3) Agent Core (НЕ policy, НЕ project flows)
        AgentCore.Abstractions/               # интерфейсы для policy/flows, которые реализуют продукты
        AgentCore.Integrations/               # общие интеграции core: storage-абстракции, telemetry, логирование

    Products/                                 # ИНДИВИДУАЛЬНЫЕ проекты (каждый продукт — свой набор сценариев)
      ProductA/
        ProductA.Api/                         # backend API: endpoints для сайта/админки + webhooks (tg/wa/sms)
        ProductA.Worker/                      # фоновые задачи: отправка, ретраи, расписания, очереди (опционально, но рекомендовано)

        Frontend/
          Landing/                            # (1) лендинг(и) продукта
          Admin/                              # (1) админка продукта (опционально)

        Messaging/
          ProductA.AdapterInit/               # (2) какие адаптеры включены, конфиги, DI-композиция
          ProductA.MessageFlows/              # (2) свой поток сообщений продукта (цепочки, маршрутизация)
          ProductA.MessageHandlers/           # (4) свои обработчики сообщений на базе общих библиотек

        Agents/
          ProductA.AgentImplementations/      # (3) конкретные агенты продукта
          ProductA.AgentPolicies/             # (3) policy продукта
          ProductA.ProjectFlows/              # (3) project flows продукта

      ProductB/
        ...                                   # аналогично: независимые flows/policies/handlers

  docs/
    STRUCTURE.md                              # этот документ
```

---

## Пояснение: где “сервисы” для Telegram/SMS/WhatsApp

В минимальном варианте это не отдельные деплоимые приложения, а **общие библиотеки-адаптеры** в `src/Shared/Messaging/Adapters.*`, которые подключаются в индивидуальном продукте:

- **Входящие вебхуки** обслуживает `ProductX.Api` (публичный HTTP за reverse-proxy).
- **Исходящая отправка/ретраи/очереди** выполняет `ProductX.Worker`.

Если со временем потребуется масштабирование/изоляция, можно выделить отдельное приложение уровня `Apps/BotGateway`, но на старте это не обязательно.

---

## Заметки по деплою (VPS Linux)

- Reverse-proxy (NGINX/Caddy) принимает `https://...` и проксирует:
  - `site` → фронтенд (статика или SSR)
  - `api` / `/webhooks/*` → `ProductX.Api`
- `ProductX.Api` и `ProductX.Worker` обычно слушают только внутренние порты (не наружу).

