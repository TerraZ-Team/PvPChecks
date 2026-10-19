# PvPChecks

Плагин для **Terraria / TShock 6.1.3** (.NET 9): ограничения на оружие, экипировку, баффы, снаряды и телепортацию, а также настраиваемый **запрет команд в PvP**.

## Возможности

- Запрещённые оружие, броня и аксессуары по ID.
- Проверки баффов, снарядов, дублирующейся экипировки и сетевых PvP-пакетов.
- Ограничение телепортации при включённом PvP.
- Запрет отдельных команд и их алиасов: **`heal` по умолчанию**.
- Обход проверки команд по permission `pvpchecks.commands.bypass` и общих проверок по `pvpchecks.ignore`.

Запрет команд работает **всё время, пока у игрока включён PvP-флаг** (`TPlayer.hostile`). Это не система «в бою» с задержкой после последнего удара: при выключении PvP запрет команд снимается. Если на арене игроки не должны отключать PvP самостоятельно, запрет изменения PvP включается отдельным механизмом.

## Установка

1. Соберите `src/PvPChecks.csproj` под .NET 9 или получите `PvPChecks.dll` из артефакта GitHub Actions.
2. Поместите `PvPChecks.dll` в каталог `ServerPlugins` совместимого сервера TShock.
3. Перезапустите сервер. Плагин создаст `pvpchecks.json` внутри каталога `TShock.SavePath` (обычно `tshock/`).

## Конфигурация

При запуске плагин сохраняет конфиг, добавляя новые параметры в ранее существующий файл и сохраняя старые списки. Пример:

~~~json
{
  "weaponBans": [],
  "accsBans": [],
  "armorBans": [],
  "buffBans": [],
  "projBans": [],
  "portalGunBlock": true,
  "blockCommandsInPvP": true,
  "commandBans": ["heal"]
}
~~~

- `blockCommandsInPvP`: включить или отключить запрет команд.
- `commandBans`: перечень имён команд, например `["heal", "home", "warp", "spawn"]`. Регистр и префикс `/` не учитываются. Если указано главное имя команды, её алиасы также блокируются.
- Пустой список `commandBans` разрешает команды при включённом PvP.
- `portalGunBlock` оставлен для совместимости со старым конфигом, но отдельной логики для этого поля нет.

После изменения конфига необходим перезапуск сервера.

## Команды

| Команда | Назначение | Право |
|---|---|---|
| `/pvpitembans [страница]` | Список запрещённых предметов | Не требуется |
| `/pvpbuffbans [страница]` | Список запрещённых баффов | Не требуется |
| `/pvpprojbans [страница]` | Список запрещённых снарядов | Не требуется |
| `/banitem add/del <ID/имя>` | Запретить или разрешить предмет | `pvpchecks.ban` |
| `/banbuff add/del <ID/имя>` | Запретить или разрешить бафф | `pvpchecks.ban` |
| `/banproj add/del <ID>` | Запретить или разрешить снаряд | `pvpchecks.ban` |

Право `pvpchecks.commands.bypass` исключает только новую блокировку команд. Право `pvpchecks.ignore` сохраняет прежнее поведение обхода проверок и также исключает новую блокировку.

## Структура проекта

- `src/PvPChecks.cs`: создание и регистрация обработчиков.
- `src/Configuration/Config.cs`: настройка и совместимость со старым JSON.
- `src/Handlers/PvPEnforcementHandler.cs`: ограничения и сетевые проверки PvP.
- `src/Handlers/PvPCommandBlockHandler.cs`: блокировка команд в PvP.
- `src/Handlers/CommandBanPolicy.cs`: сравнение команд и алиасов.
- `src/Commands/BanCommandHandler.cs`: администрирование списков.
- `tests/`: тесты политики запрета команд.

## Сборка и CI/CD

~~~bash
dotnet restore src/PvPChecks.csproj
dotnet build src/PvPChecks.csproj -c Release --no-restore
dotnet run --project tests/PvPChecks.PolicyTests.csproj -c Release
~~~

GitHub Actions (`.github/workflows/build-deploy.yml`): сборка и тесты запускаются при открытии PR, при пуше тега и вручную. Артефакт содержит одну `PvPChecks.dll`.

Деплой после успешной сборки производится **только по тегу или вручную** на Windows runner с метками `self-hosted, windows, x64`. Целевой каталог берётся из переменной репозитория `PLUGIN_DEPLOY_DIR`, а если она не установлена, используется `C:\github-deploy`. Workflow копирует DLL, но не перезапускает сервер.
