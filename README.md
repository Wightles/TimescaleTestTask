# Timescale Test Task API

Небольшой ASP.NET Core Web API для загрузки CSV-файлов с измерениями и расчёта статистики по каждому файлу.

## Стек

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Swagger
- xUnit
- Docker Compose

## Запуск через Docker

```bash
docker compose up --build
```

API будет доступен на:

```text
http://localhost:53412
```

Swagger:

```text
http://localhost:53412/swagger
```

PostgreSQL поднимается в Docker Compose. Миграции применяются при старте приложения.

Остановить контейнеры:

```bash
docker compose down
```

## Локальный запуск

Для запуска без Docker нужен PostgreSQL и .NET 8 SDK.

Строка подключения задаётся через User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=timescale_api;Username=<db-user>;Password=<db-password>" --project TimescaleApi
```

Применить миграции:

```bash
dotnet ef database update --project TimescaleApi
```

Запустить API:

```bash
dotnet run --project TimescaleApi
```

Swagger при локальном запуске:

```text
http://localhost:5281/swagger
```

## Endpoints

### `POST /api/files/upload`

Загружает CSV-файл через `multipart/form-data`.

CSV должен иметь формат:

```csv
Date;ExecutionTime;Value
2026-01-01T00:00:00Z;10;100
2026-01-01T00:01:30Z;20;200
```

Основные правила:

- от 1 до 10000 строк;
- дата не раньше `2000-01-01T00:00:00Z` и не из будущего;
- `ExecutionTime >= 0`;
- `Value >= 0`;
- пустые значения и неверные типы запрещены.

Если загрузить файл с уже существующим именем, старые данные этого файла заменяются новыми.

### `GET /api/results`

Возвращает рассчитанные результаты.

Поддерживаемые фильтры:

- `FileName`
- `StartDateFrom`
- `StartDateTo`
- `AverageValueFrom`
- `AverageValueTo`
- `AverageExecutionTimeFrom`
- `AverageExecutionTimeTo`

### `GET /api/values/latest?fileName=...`

Возвращает последние 10 значений указанного файла, отсортированные по дате от новых к старым.

## Тесты

```bash
dotnet test
```

Тесты покрывают основную логику обработки CSV: валидацию, расчёт среднего значения и медианы, успешную загрузку и повторную загрузку файла с тем же именем.
