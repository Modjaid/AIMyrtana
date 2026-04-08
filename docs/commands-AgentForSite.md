# VPS Aimyrtana — основные команды (AgentForSite)

Краткая шпаргалка по SSH и управлению API на сервере. Подробности путей, портов и деплоя — в [vps-aimyrtana-infrastructure.md](./vps-aimyrtana-infrastructure.md).

Подключение (подставь свой хост и пользователя из настроек деплоя):

```bash
ssh <REMOTE_USER>@<REMOTE_HOST>
```

## AgentForSite API (systemd: `agentforsite-api`)

Каталог приложения: `/var/www/aimyrtana/agentforsite/api/`  
Прослушивание приложения: `http://127.0.0.1:5000`

| Действие | Команда |
|----------|---------|
| **Остановить** (выключить рантайм процесса) | `sudo systemctl stop agentforsite-api` |
| **Запустить** | `sudo systemctl start agentforsite-api` |
| **Перезапустить** (после деплоя или смены env) | `sudo systemctl restart agentforsite-api` |
| **Статус** (работает ли сервис) | `sudo systemctl status agentforsite-api --no-pager` |
| **Включить автозапуск после перезагрузки VPS** | `sudo systemctl enable agentforsite-api` |
| **Выключить автозапуск** (сервис не поднимется сам после reboot) | `sudo systemctl disable agentforsite-api` |

Логи сервиса:

```bash
sudo journalctl -u agentforsite-api -n 100 --no-pager
sudo journalctl -u agentforsite-api -f
```

Проверка, что API отвечает на сервере:

```bash
curl -i http://127.0.0.1:5000/health
```

## nginx (фронт с порта 80 на приложение)

После правок конфигов:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

Статус и логи:

```bash
sudo systemctl status nginx --no-pager
sudo journalctl -u nginx -n 50 --no-pager
```

## .NET runtime (справочно)

```bash
dotnet --list-runtimes
```

---

*Имя юнита и путь совпадают с документом по инфраструктуре; при добавлении других продуктов — отдельные `systemd`‑юниты и порты.*
