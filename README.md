# QuizRush - Local Setup

## Prerequisites
- .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio / SQL Server Express) **or** SQL Server Express

## Database connection (local dev)

Default connection string (Windows auth, database created on API startup in Development):

```
Server=(localdb)\mssqllocaldb;Database=QuizRushDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Configured in `QuizRush.Api/appsettings.json` and `appsettings.Development.json` as `ConnectionStrings:Default`.

**SQL Server Express instead of LocalDB** — set `ConnectionStrings:Default` in `appsettings.Development.json` or user secrets:

```
Server=.\SQLEXPRESS;Database=QuizRushDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Override without editing files:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=.\SQLEXPRESS;Database=QuizRushDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" --project QuizRush.Api
```

In Development, the API applies EF Core migrations automatically so `QuizRushDb` is created if missing. For manual setup: `dotnet ef database update --project QuizRush.Infrastructure --startup-project QuizRush.Api`.

## Run Order (important)
1. Start API:
   - `dotnet run --project QuizRush.Api`
   - Default URL: `http://localhost:5176`
2. Start Web:
   - `dotnet run --project QuizRush.Web`
   - Default URL: `http://localhost:5261`

## If the API runs on a different port
Set `QuizRush.Web/appsettings.json` -> `QuizRush:ApiBaseUrl` to your API URL.

Example:
```json
"QuizRush": {
  "ApiBaseUrl": "http://localhost:6001"
}
```

## Common issue: Web page loads but hosting/join features fail
- Confirm API is running and reachable at `http://localhost:5176/swagger` (or your configured port).
- Confirm SQL connection in `QuizRush.Api/appsettings.json` is valid on your machine:
  - `ConnectionStrings:Default`

