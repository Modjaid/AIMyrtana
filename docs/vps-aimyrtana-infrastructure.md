# VPS Aimyrtana — что уже настроено

Краткая памятка по серверу и деплою, чтобы при добавлении новых продуктов в репозитории не разбираться заново.

## Сервер

- **ОС**: Ubuntu 24.04 LTS (пример: DigitalOcean droplet).
- **Публичный IPv4** (пример из настройки): `143.198.66.129` — при смене IP обнови документ и конфиги nginx при необходимости.

## .NET на VPS

- Установлен **runtime**, не SDK:
  - `aspnetcore-runtime-10.0` (вместе с `dotnet-runtime-10.0`, `dotnet-hostfxr-10.0`).
- **SDK на сервере не обязателен**, если сборка и `dotnet publish` выполняются в CI (GitHub Actions).
- Проверка:
  - `dotnet --list-runtimes` — должны быть `Microsoft.AspNetCore.App 10.0.x` и `Microsoft.NETCore.App 10.0.x`.

## Деплой из репозитория (GitHub Actions)

Файл: `.github/workflows/deploy.yml`.

- Триггер: push в `main`, если меняются пути под `src/**`.
- В CI ставится **.NET SDK 10.0.x**, для каждого выбранного продукта выполняется `dotnet publish` первого найденного `*.Api.csproj` / `*.Worker.csproj` (до глубины 3 в каталоге продукта).
- **Slug продукта** = имя папки в `src/Products/<Product>` **в нижнем регистре** (например `AgentForSite` → `agentforsite`).
- На сервер rsync кладёт артефакты в:
  - **`/var/www/aimyrtana/<slug>/api`** — веб/API (если есть `*.Api.csproj`);
  - **`/var/www/aimyrtana/<slug>/worker`** — worker (если есть `*.Worker.csproj`).

Секреты в GitHub (имена из workflow): `UBUNTU_AI_MYRTANA`, `REMOTE_HOST`, `REMOTE_USER` — в этом файле значения не дублируем.

## AgentForSite (уже настроено)

- **Каталог publish**: `/var/www/aimyrtana/agentforsite/api/`
- **Точка входа**: `AgentForSite.Api.dll` (запуск через `dotnet`).
- **systemd**: юнит **`agentforsite-api.service`**
  - Слушает только localhost: **`http://127.0.0.1:5000`** (`ASPNETCORE_URLS`).
  - `WorkingDirectory` = каталог publish выше.
- **nginx**: reverse proxy с **порта 80** на `http://127.0.0.1:5000` (отдельный site под IP/домен — как настроено у тебя в момент ввода в эксплуатацию).
- Проверки с сервера:
  - `curl -i http://127.0.0.1:5000/health` → JSON `status: ok`;
  - `curl -I http://127.0.0.1/` → ответ через nginx, `Content-Type: text/html` для главной.

## Что нужно для нового продукта с веб-API

1. **Папка** в `src/Products/<ИмяПродукта>/` и `*.Api.csproj` — тогда CI положит билд в `/var/www/aimyrtana/<slug>/api/`.
2. На VPS **отдельный порт** на localhost (не пересекаться с `5000`, если там уже AgentForSite), например `5001`.
3. **Новый systemd unit** по образцу `agentforsite-api.service`: свой `WorkingDirectory`, свой `ExecStart` к `...dll`, свой `ASPNETCORE_URLS=http://127.0.0.1:<порт>`.
4. **nginx**:
   - либо отдельный `server { listen 80; server_name ... }` с `proxy_pass` на новый порт;
   - либо один домен с разными `location` — если так удобнее маршрутизация.

Worker без HTTP: systemd без nginx, только юнит с `dotnet ...Worker.dll`.

## Фаервол и доступ из интернета

### Состояние на момент настройки

- **UFW** на дроплете был **`inactive`** — локальный фаервол Ubuntu **не блокировал** порт 80.
- С внешней сети запрос **`http://<публичный-IPv4>/`** давал **HTTP 200** (проверка через `curl` с другой машины). Если у тебя в браузере «не открывается», чаще всего дело не в UFW на сервере, а в **HTTPS без сертификата**, **Cloud Firewall** у провайдера или в **другой сети**/блокировке.

### DigitalOcean Cloud Firewall (проверить в первую очередь)

В панели: **Networking → Firewalls**. Если к дроплету привязан firewall:

- во **входящих правилах (Inbound)** должны быть разрешены **TCP 80** (HTTP) и, после настройки HTTPS, **TCP 443**;
- не забыть **применить firewall к нужному Droplet**.

Без правила на **80** браузер снаружи не достучится, даже если на сервере nginx слушает `0.0.0.0:80`.

### UFW (если решишь включить)

Пока UFW выключен, входящие не режутся им. Если включишь — **сначала** разреши SSH, иначе можно потерять доступ:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
sudo ufw status verbose
```

### Сайт не открывается в браузере — чеклист

1. **Адрес**: открывай явно **`http://143.198.66.129/`** (не `https://`, пока нет сертификата на IP — браузер покажет ошибку или «не удаётся подключиться» на 443).
2. **С другого устройства / мобильный интернет** — исключить блокировку в текущей Wi‑Fi сети.
3. **`server_name` в nginx**: если указан только IP, заход по IP обычно ок; если позже будет домен — добавь его в `server_name` и перезагрузи nginx.
4. **Проверка с твоего ПК** (Windows, в PowerShell или cmd):

   ```text
   curl.exe -I http://143.198.66.129/
   ```

   Ожидается строка `HTTP/1.1 200` и `Server: nginx/...`.
5. На сервере: `sudo ss -tlnp | grep ':80 '` — должен быть `0.0.0.0:80` у nginx.

## HTTPS

- Для **одного только IP** типовой сертификат Let’s Encrypt под домен обычно не подходит — нужен **домен**, указывающий на сервер.
- После появления домена: `certbot` + плагин nginx (или отдельная инструкция под твой DNS).

## Мелочи, которые уже всплывали

- Вставка из Windows Terminal в SSH иногда добавляет маркеры `^[[200~` … `^[[201~` (bracketed paste) — команды ломаются; надо вставлять аккуратно или набирать вручную.
- Пакет **`dotnet-host-10.0` без runtime** даёт ошибку вида отсутствия `host/fxr` — нужен полноценный runtime (`aspnetcore-runtime-10.0`).

## Полезные команды

```bash
sudo systemctl status agentforsite-api --no-pager
sudo journalctl -u agentforsite-api -n 100 --no-pager

sudo nginx -t
sudo systemctl reload nginx
sudo journalctl -u nginx -n 50 --no-pager

dotnet --list-runtimes
```

---

*Документ отражает состояние на момент первичной настройки; при смене портов, доменов или имён сервисов обнови соответствующие разделы.*
