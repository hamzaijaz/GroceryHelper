# GroceryHelper

An ASP.NET Core 8 Web API for managing a grocery list. It provides CRUD endpoints for grocery items, which can be tagged with a category and filtered by it. It is intended to be called from a separate frontend app.

Data is held in an in-memory store, so it is lost whenever the app restarts. The storage sits behind the `IGroceryRepository` interface, so a real database can replace it later without changing the controller.

## Getting started

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

```bash
dotnet run --project GroceryHelper --launch-profile http
```

- **API:** `http://localhost:5001/api/groceries`
- **Swagger UI:** `http://localhost:5001/swagger`. It is available in the Development environment only.

The `https` profile also listens on `https://localhost:7200`.

[GroceryHelper/GroceryHelper.http](GroceryHelper/GroceryHelper.http) has ready-made requests for each endpoint, for use with the VS Code REST Client or the Visual Studio HTTP editor.

## API

Base route: `/api/groceries`

| Method | Route | Description | Responses |
|---|---|---|---|
| `GET` | `/api/groceries?category={category}` | Lists groceries sorted by name. `category` is optional. | `200`, `400` for an unknown category |
| `POST` | `/api/groceries` | Creates a grocery. | `201`, `400` |
| `PUT` | `/api/groceries/{id}` | Replaces a grocery's name, quantity and category. | `200`, `400`, `404` |
| `DELETE` | `/api/groceries/{id}` | Removes a grocery. | `204`, `404` |

`{id}` must be a GUID; any other value returns `404`.

### Request body (`POST` and `PUT`)

```json
{
  "name": "Milk",
  "quantity": 2,
  "category": "Dairy"
}
```

| Field | Rules |
|---|---|
| `name` | Required and not blank, up to 100 characters. Leading and trailing spaces are trimmed. |
| `quantity` | Integer from 1 to 1000. |
| `category` | Optional. If given, it must be one of the category names below. |

### Response body

```json
{
  "id": "3f2b8c1e-7a4d-4e9b-9c55-2d1f0a6b7e10",
  "name": "Milk",
  "quantity": 2,
  "category": "Dairy"
}
```

`category` is `null` when the grocery has no category.

### Categories

`Dairy`, `Fruit`, `Vegetables`, `Meat`, `FrozenFood`, `Snacks`, `Drinks`, `Baby`, `PersonalCare`, `Household`, `CleaningSupplies`, `PetCare`, `Oils`, `DryFruit`

- Categories are sent and returned as names, and incoming names ignore case.
- Numeric values such as `3` are rejected with `400`.
- Swagger shows the category search filter as a dropdown.
- To add a category, add a member to [GroceryCategory.cs](GroceryHelper/Models/GroceryCategory.cs).

### Errors

Errors are returned as RFC 7807 problem details (`application/problem+json`):

- **`400` validation errors** list the field-level messages under `errors`.
- **`404` responses** use the standard problem details body.
- **Unhandled exceptions** return a generic `500` that does not expose exception details. The full exception is logged on the server.

## Configuration

### CORS

The frontend's address must be listed in `Cors:AllowedOrigins`. Requests from any other origin are blocked by the browser.

| File | Allowed origins |
|---|---|
| [appsettings.Development.json](GroceryHelper/appsettings.Development.json) | `http://localhost:3000`, `http://localhost:4200` and `http://localhost:5173` (the default React, Angular and Vite ports) |
| [appsettings.json](GroceryHelper/appsettings.json) | None. Add the deployed frontend's URL before release. |

You can also set the origins with environment variables, for example `Cors__AllowedOrigins__0=https://app.example.com`.

### Logging

The repository logs every operation with structured fields (`GroceryId`, `GroceryName`, `GroceryCount`, `Category`). For example:

```
Added grocery 'Milk' (Id: 3f2b8c1e-...)
Updated grocery 'Milk' to 'Oat Milk' (Id: 3f2b8c1e-...)
Retrieved 3 groceries (category filter: Dairy)
```

Update and delete requests for a missing id are logged as warnings. Log levels are set in the `Logging` section of `appsettings*.json`.

## Project structure

```
GroceryHelper/                  Web API project (also contains GroceryHelper.sln)
  Controllers/                  GroceriesController: HTTP endpoints
  Models/                       Grocery, GroceryRequest (validation), GroceryCategory
  Repositories/                 IGroceryRepository and the in-memory implementation
  Program.cs                    Service registration, CORS, exception handling, JSON options
GroceryHelper.Tests/            xUnit test project
  Controllers/                  Integration tests through the full HTTP pipeline
  Repositories/                 Unit tests for the in-memory repository
```

## Testing

The project was built test-first. Run the tests with:

```bash
dotnet test GroceryHelper/GroceryHelper.sln
```

- **Integration tests** use `WebApplicationFactory` to call the real HTTP pipeline. They cover each endpoint, validation, unknown categories, 404s, the 500 error response, the lowercase routes in Swagger, and CORS for allowed and blocked origins. Each test starts with an empty store.
- **Unit tests** cover the repository: adding, updating, deleting, filtering, sorting, and log content. Log content is checked with `FakeLogger`.

Test libraries: xUnit, NSubstitute and `Microsoft.Extensions.Diagnostics.Testing`.
