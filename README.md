# Task Management API

A REST API for managing projects and their tasks, with per-user data isolation via JWT authentication.

## Tech Stack

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + SQL Server
- ASP.NET Core Identity (custom `ApplicationUser`/`ApplicationRole`, `int` keys)
- JWT bearer authentication
- FluentValidation
- Swagger / OpenAPI

## Setup Instructions

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, a local instance, or a container)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef` (if not already installed)

### 1. Clone the repository

```bash
git clone <https://github.com/MostafaElbrawy/Task-Management-REST-API>
cd Task_Management
```

### 2. Configure the database connection and JWT settings

In `appsettings.json` (or `appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "TaskManagementApi",
    "Audience": "TaskManagementApiUsers",
    "Key": "gqOqGqIghWGnVqgmeVSgGkesd6IMfTJ8ZL2ArWdciqe",
    "ExpiresInMinutes": 60
  }
}
```

### 3. Apply migrations

```bash
dotnet ef database update
```

### 4. Run the app

```bash
dotnet run
```

### 5. Explore the API

Swagger UI is available at `https://localhost:{7219}/swagger` once the app is running.

## Authentication

1. `POST /api/account/register` to create an account, or `POST /api/account/login` if you already have one. Both return a JWT access token.
2. Include the token on every subsequent request:
   ```
   Authorization: Bearer <access_token>
   ```
3. In Swagger, click **Authorize** and paste `Bearer <access_token>`.

---

## API Documentation

All responses are wrapped in a standard envelope:

```json
{
  "success": true,
  "message": "string or null",
  "data": {},
  "errors": [],
  "statusCode": 201
}
```

_(field names assumed from `ApiResponse<T>` usage in the codebase — adjust if your actual class differs)_

### Account

| Method | Endpoint                | Description              |
| ------ | ----------------------- | ------------------------ |
| POST   | `/api/account/register` | Create a new account     |
| POST   | `/api/account/login`    | Log in and receive a JWT |

**POST /api/account/register**

```json
// Request
{
  "email": "alice@example.com",
  "password": "Password123!",
  "phoneNumber": "01000000000"
}
```

```json
// 201 Response
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

**POST /api/account/login**

```json
// Request
{ "email": "alice@example.com", "password": "Password123!" }
```

Returns the same shape as register on success.

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
// Request
{ "name": "E-Commerce Platform", "description": "Storefront rebuild" }
```

```json
// 201 Response
{
  "success": true,
  "data": {
    "id": 3,
    "name": "E-Commerce Platform",
    "description": "Storefront rebuild",
    "createdAt": "2026-07-24T10:00:00Z",
    "updatedAt": "2026-07-24T10:00:00Z"
  }
}
```

Duplicate names return a `400` validation error.

### Tasks

_(all require `Authorization: Bearer <token>`)_

| Method | Endpoint                          | Description                                                       |
| ------ | --------------------------------- | ----------------------------------------------------------------- |
| GET    | `/api/projects/{projectId}/tasks` | List tasks for one project (filter/sort/paginate)                 |
| GET    | `/api/tasks`                      | List all your tasks across projects (filter/sort/paginate/search) |
| GET    | `/api/tasks/{id}`                 | Get one task                                                      |
| POST   | `/api/projects/{projectId}/tasks` | Create a task under a project                                     |
| PUT    | `/api/tasks/{id}`                 | Update a task                                                     |
| DELETE | `/api/tasks/{id}`                 | Delete a task                                                     |

**Query parameters** (on both list endpoints):

| Param                                | Type                                   | Notes                              |
| ------------------------------------ | -------------------------------------- | ---------------------------------- |
| `status`                             | `Todo` \| `InProgress` \| `Done`       | Optional filter                    |
| `priority`                           | `Low` \| `Medium` \| `High`            | Optional filter                    |
| `dueDateFrom` / `dueDateTo`          | ISO date                               | Optional range filter              |
| `sortColumn`                         | `DueDate` \| `Priority` \| `CreatedAt` | Defaults to `Id` if omitted        |
| `sortOption`                         | `Asc` \| `Desc`                        | Sort direction                     |
| `page` / `pageSize`                  | int                                    | Pagination                         |
| `searchTerm` _(GET /api/tasks only)_ | string                                 | Partial match on title/description |

**POST /api/projects/{projectId}/tasks**

```json
// Request
{
  "title": "Implement payment gateway",
  "description": "Integrate Stripe checkout",
  "status": "Todo",
  "priority": "High",
  "dueDate": "2026-08-15"
}
```

```json
// 201 Response
{
  "success": true,
  "data": {
    "id": 42,
    "projectName": "E-Commerce Platform",
    "title": "Implement payment gateway",
    "description": "Integrate Stripe checkout",
    "status": "Todo",
    "priority": "High",
    "dueDate": "2026-08-15T00:00:00Z",
    "createdAt": "2026-07-24T10:05:00Z",
    "updatedAt": "2026-07-24T10:05:00Z"
  }
}
```

A `dueDate` in the past returns a `400` validation error.

---

## Schema / Data Model Rationale

- **ApplicationUser → Project**: one-to-many. Every project belongs to exactly one user, enforced via `UserId` foreign key; all project/task queries filter by the authenticated user's Id so users can only see their own data.
- **Project → Task**: one-to-many, `ProjectId` foreign key on `Task`, configured with cascade delete so removing a project removes its tasks automatically at the database level (not just in application code).
- **Status / Priority enums**: stored with `[EnumMember]` string values for readable JSON payloads. Each enum includes a `None` member used purely as a validation sentinel — it's rejected by application-layer validation and should never be persisted; it exists so an unset/invalid incoming value can be distinguished from a deliberately-chosen one.
- **Timestamps**: `CreatedAt`/`UpdatedAt` on both `Project` and `Task` support audit trails and `createdAt` sorting.
- **Recommended indexes**: `Task.ProjectId` (FK lookups), `Task.Status`, `Task.Priority`, `Task.DueDate` (filter/sort columns), and a unique index on `Project.Name` per user (or globally, depending on your uniqueness rule) to enforce the "no duplicate project names" business rule at the database level, not just in application code.

---

## Running Tests

Test project is not set up yet. Planned approach:

- **Unit tests** (xUnit) for `TaskService` business logic: status/priority validation, due-date-in-the-past checks, status-transition handling.
- **Integration tests** (xUnit + `WebApplicationFactory`) for the three critical flows: create project → add task → mark done → delete project; filter by status+priority; search + pagination.

Once added, tests will run with:

```bash
dotnet test
```

## Known Limitations

- No automated tests yet (see above).
- Docker/`docker-compose` setup not yet added.
- Soft deletes not implemented (hard deletes currently).
