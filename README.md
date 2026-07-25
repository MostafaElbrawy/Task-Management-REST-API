# Task Management API

A REST API for managing projects and their tasks, with per-user data isolation via JWT authentication.

## Tech Stack

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + SQL Server
- ASP.NET Core Identity (custom `ApplicationUser`/`ApplicationRole`, `int` keys)
- JWT bearer authentication
- FluentValidation
- Swagger / OpenAPI

## Quick Start (Docker)

Prerequisite: Docker Desktop (or Docker Engine + Compose).

```bash
git clone https://github.com/MostafaElbrawy/Task-Management-REST-API
cd "Task Management REST API"
docker compose up --build
```

That's it — SQL Server starts, migrations apply, and demo data seeds automatically. Open:

```
http://localhost:8080/swagger
```

Stop with `docker compose down` (add `-v` to also wipe the DB volume).

## Manual Setup (without Docker)

Prerequisites: .NET 10 SDK, a SQL Server instance, `dotnet-ef` (`dotnet tool install --global dotnet-ef`).

```bash
cd Task_Management
dotnet restore
dotnet ef database update
dotnet run
```

Swagger UI: `https://localhost:{port}/swagger`.

---

## Authentication

1. `POST /api/account/register` or `POST /api/account/login` — both return a JWT.
2. Send it on every subsequent request: `Authorization: Bearer <access_token>`.
3. In Swagger: click **Authorize**, paste `Bearer <access_token>`.

---

## API Documentation

All responses share this envelope:

```json
{
  "success": true,
  "message": "string or null",
  "data": {},
  "errors": [],
  "statusCode": 201
}
```

### Account

| Method | Endpoint                | Description              |
| ------ | ----------------------- | ------------------------ |
| POST   | `/api/account/register` | Create a new account     |
| POST   | `/api/account/login`    | Log in and receive a JWT |

**POST /api/account/register**

```json
{
  "email": "alice@example.com",
  "password": "Password123!",
  "phoneNumber": "01000000000"
}
```

```json
// 201
{
  "success": true,
  "message": "Account created and logged in successfully",
  "data": {
    "id": 1,
    "email": "alice@example.com",
    "roles": [],
    "accessToken": "eyJhbGciOi...",
    "expiresIn": 3600
  },
  "errors": [],
  "statusCode": 201
}
```

**POST /api/account/login** — same request/response shape, existing credentials.

### Projects

_(all require `Authorization: Bearer <token>`)_

| Method | Endpoint                           | Description                              |
| ------ | ---------------------------------- | ---------------------------------------- |
| GET    | `/api/projects?page=1&pageSize=10` | List your projects (paginated)           |
| GET    | `/api/projects/{id}`               | Get one project                          |
| POST   | `/api/projects`                    | Create a project                         |
| PUT    | `/api/projects/{id}`               | Update a project                         |
| DELETE | `/api/projects/{id}`               | Delete a project (cascades to its tasks) |

**POST /api/projects**

```json
{ "name": "E-Commerce Platform", "description": "Storefront rebuild" }
```

Duplicate names return `422`.

### Tasks

_(all require `Authorization: Bearer <token>`)_

| Method | Endpoint                          | Description                                       |
| ------ | --------------------------------- | ------------------------------------------------- |
| GET    | `/api/projects/{projectId}/tasks` | List tasks for one project (filter/sort/paginate) |
| GET    | `/api/tasks`                      | List all your tasks (filter/sort/paginate/search) |
| GET    | `/api/tasks/{id}`                 | Get one task                                      |
| POST   | `/api/projects/{projectId}/tasks` | Create a task under a project                     |
| PUT    | `/api/tasks/{id}`                 | Update a task                                     |
| DELETE | `/api/tasks/{id}`                 | Delete a task                                     |

**Query params** (list endpoints): `status` (`Todo`/`InProgress`/`Done`), `priority` (`Low`/`Medium`/`High`), `dueDateFrom`/`dueDateTo`, `sortColumn` (`DueDate`/`Priority`/`CreatedAt`), `sortOption` (`Asc`/`Desc`), `page`/`pageSize`, and `searchTerm` (GET `/api/tasks` only).

**POST /api/projects/{projectId}/tasks**

```json
{
  "title": "Implement payment gateway",
  "description": "Integrate Stripe checkout",
  "status": "Todo",
  "priority": "High",
  "dueDate": "2026-08-15"
}
```

A past `dueDate` returns `422`.

---

## Schema / Data Model Rationale

- **ApplicationUser → Project**: one-to-many via `UserId`; all project/task queries filter by the authenticated user.
- **Project → Task**: one-to-many via `ProjectId`, cascade delete at the DB level.
- **Status/Priority enums**: each includes a `None` sentinel, rejected by validation — distinguishes "unset" from a deliberate choice.
- **Indexes**: `Task.ProjectId`/`Status`/`Priority`/`DueDate` (filter/sort columns), unique index on `Project.Name` per user (no-duplicate-names rule enforced at the DB level).

## Running Tests

```bash
cd Task_Management.Tests
dotnet test
```

Runs entirely against EF Core InMemory — no Docker or SQL Server needed.

- **Unit tests** (`Services/`): `TaskServiceTests.cs`, `ProjectServiceTests.cs`, `AccountServiceTests.cs` — mock all dependencies to isolate each service.
- **Integration tests** (`Integration/CriticalFlowIntegrationTests.cs`): real controller → service → repository → DB chain, covering the 3 required flows — create→add task→mark done→delete (with cascade check), filter by status+priority, search+pagination.
