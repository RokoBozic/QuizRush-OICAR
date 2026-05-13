# QuizRush - Local Setup

## Prerequisites
- .NET 8 SDK
- SQL Server Express (or update connection string to your local SQL instance)

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

