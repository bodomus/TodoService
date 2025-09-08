я# TodoService

Заключение

В проекте реализовано:

.NET микросервис TodoService (CRUD).

Postgres в Docker Compose.

EF Core миграции и фиксы.

Swagger/OpenAPI.

Unit-тесты (xUnit).

CI в GitHub Actions (build + test + Docker).

CD → пуш Docker-образа в GHCR.


Микросервис на **.NET 9** с **PostgreSQL**, **Swagger**, юнит-тестами и CI/CD на **GitHub Actions**.  
Поддерживается сборка и публикация Docker-образа в **GitHub Container Registry (GHCR)**.

---

## 📦 Стек
- ASP.NET Core Minimal API
- Entity Framework Core (Postgres provider)
- PostgreSQL 16 (Docker)
- Swagger / OpenAPI
- xUnit (WebApplicationFactory)
- GitHub Actions (CI/CD)
- Docker / Docker Compose

---

## 🚀 Запуск локально (Docker Compose)

```bash
# собрать и запустить сервис + базу
docker compose up --build


Сервис: http://localhost:8080/swagger

Postgres: localhost:5432, БД todos, логин postgres/postgres


Структура
TodoService-advanced-fixed/
 ├─ TodoService/          # основной сервис (.NET 9, EF Core, Swagger)
 ├─ TodoService.Tests/    # юнит-тесты (xUnit)
 └─ .github/workflows/    # GitHub Actions (docker.yml)

Миграции EF Core

# добавить новую миграцию
dotnet ef migrations add <Name> --project TodoService/TodoService.csproj --startup-project TodoService/TodoService.csproj

# применить миграции
dotnet ef database update --project TodoService/TodoService.csproj --startup-project TodoService/TodoService.csproj

Unit-тесты
# запуск всех тестов
dotnet test
Тесты проверяют:

/health endpoint

CRUD-операции /todos


CI/CD (GitHub Actions)

Файл: .github/workflows/docker.yml

При push / PR в main/master:

Поднимается Postgres (service).

dotnet restore, build, test.

Сохраняются результаты тестов (artifact).

Собирается и пушится Docker image.

Образы в GHCR

Публикуется в GitHub Container Registry:
docker pull ghcr.io/<owner>/todoservice:latest
Пример запуска:

docker run -p 8080:8080 \
  -e POSTGRES_CONN="Host=host.docker.internal;Port=5432;Database=todos;Username=postgres;Password=postgres" \
  ghcr.io/<owner>/todoservice:latest

Итог

✅ Микросервис (CRUD)

✅ Postgres + миграции

✅ Swagger/OpenAPI

✅ Unit-тесты

✅ CI: build + test + docker

✅ CD: push образа в GHCR







