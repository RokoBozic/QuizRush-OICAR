# QuizRush - Changes Summary

## 1. JWT Service Layer Refactoring

### Problem
The `GenerateJwtToken` method was directly inside `AuthController`, which violates separation of concerns — controllers should only handle HTTP requests, not business logic.

### What was done

**Created `QuizRush.Core/Services/IJwtTokenService.cs`**
- Defines the contract for JWT token generation
- `public` interface so other projects can reference it
- Single method: `string GenerateToken(User user)`

**Created `QuizRush.Infrastructure/Services/JwtTokenService.cs`**
- Implements `IJwtTokenService`
- Configuration values (`Issuer`, `Audience`, `DurationInMinutes`, `Key`) are read once in the constructor and stored as private fields
- JWT token contains only `ClaimTypes.NameIdentifier` (user ID) — username and email removed from claims

**Updated `QuizRush.Api/Controllers/AuthController.cs`**
- Removed `IConfiguration` dependency
- Removed `GenerateJwtToken` private method
- Now injects and uses `IJwtTokenService`

**Updated `QuizRush.Api/Program.cs`**
- Registered `IJwtTokenService` → `JwtTokenService` in the DI container

---

## 2. JWT Secret Key via Environment Variable

### Problem
The JWT secret key was hardcoded in `appsettings.json`, which means it gets committed to git and is visible to everyone with repository access.

### What was done

**Updated `appsettings.json`**
- `Jwt:Key` is now an empty string — safe to commit

**How the secret is provided**
- .NET's configuration system automatically picks up environment variables that match the config structure
- The environment variable name is `Jwt__Key` (double underscore = nesting)
- For local development: use .NET User Secrets
  ```
  dotnet user-secrets set "Jwt:Key" "your_secret_key_here"
  ```
- For production: set `Jwt__Key` as a system environment variable on the server

---

## 3. Removed Username and Email from JWT Claims

### Problem
JWT tokens are base64-encoded and readable by anyone. Putting username and email in the token unnecessarily exposes user data.

### What was done
- Removed `ClaimTypes.Name` (username) from claims
- Removed `ClaimTypes.Email` from claims
- Only `ClaimTypes.NameIdentifier` (user ID) remains — enough to identify the user, everything else can be looked up from the database

---

## 4. Quiz CRUD Service Layer (In Progress)

Following the same layered architecture pattern established with JWT.

### Created `QuizRush.Core/ViewModels/QuizViewModels.cs`
Three classes representing what the client sends:
- `QuizViewModel` — title, description, list of questions
- `QuestionViewModel` — text, points value, time limit, list of answers
- `AnswerViewModel` — text, whether it's correct

ViewModels intentionally exclude server-side fields (`Id`, `CreatorId`, `CreatedAt`) that the client should never set.

### Created `QuizRush.Core/Services/IQuizService.cs`
Defines the contract:
```csharp
Task<IEnumerable<Quiz>> GetAllAsync();
Task<Quiz?> GetByIdAsync(long id);
Task<Quiz> CreateAsync(QuizViewModel model, long creatorId);
Task UpdateAsync(long id, QuizViewModel model);
Task DeleteAsync(long id);
```

### Created `QuizRush.Infrastructure/Services/QuizService.cs`
Implements `IQuizService`:
- **Validation lives in the service**, not the controller — improved over previous ARDENT project
- `CreateAsync` — validates questions/answers, maps ViewModel to entities, sets `CreatorId` from JWT (not from client)
- `UpdateAsync` — replaces all questions and answers with the new ones from the ViewModel
- `DeleteAsync` — removes the quiz by ID
- Throws `ArgumentException` for invalid input, `KeyNotFoundException` for missing records
- Uses eager loading (`.Include().ThenInclude()`) to always return quizzes with their questions and answers

---

## Architecture Overview

```
QuizRush.Core          — Entities, Interfaces, ViewModels (no dependencies)
QuizRush.Infrastructure — Service implementations, DbContext (depends on Core)
QuizRush.Api           — Controllers, Program.cs (depends on Core + Infrastructure)
QuizRush.Tests         — Unit tests (depends on all layers)
```

---

## 5. Sprint 3 - GameHub Contract + DTOs

### Problem
Sprint 3 requires a real-time contract between clients and server, but no SignalR hub interfaces or game-specific DTOs existed yet.

### What was done

**Created `QuizRush.Core/Hubs/IGameHubClient.cs`**
- Defines hub methods invoked by host and players:
  - Host methods: `HostGame`, `StartGame`, `NextQuestion`, `EndGame`
  - Player methods: `JoinGame`, `SubmitAnswer`, `PlaceGamble`, `LeaveGame`

**Created `QuizRush.Core/Hubs/IGameHubServer.cs`**
- Defines server-to-client events for:
  - Lobby/player changes (`PlayerJoined`, `PlayerLeft`)
  - Question lifecycle (`QuestionReady`, `QuestionAnswered`, `AllPlayersAnswered`, `AnswerRevealed`)
  - Session lifecycle (`GameStarted`, `GameEnded`, `SessionExpired`, `GameError`)

**Created `QuizRush.Core/ViewModels/GameHubViewModels.cs`**
- Added hub DTOs:
  - `QuestionData`
  - `AnswerOptionData`
  - `AnswerData`
  - `LeaderboardData`
  - `PlayerAnswerSubmissionData`

---

## 6. Sprint 3 - SignalR GameHub + API Wiring

### Problem
The API had REST endpoints for quiz/session management, but no real-time orchestration for live game flow (join/start/answer/leaderboard/end).

### What was done

**Created `QuizRush.Api/Hubs/GameHub.cs`**
- Implemented SignalR hub using `Hub<IGameHubServer>`
- Added in-memory active session tracking with connection/session mappings
- Implemented host flow:
  - `HostGame` creates session via `IGameSessionService`
  - `StartGame` loads first question and broadcasts to group
  - `NextQuestion` advances flow or ends game
  - `EndGame` broadcasts leaderboard and marks DB session as completed
- Implemented player flow:
  - `JoinGame` supports guest join by PIN (no auth required)
  - `SubmitAnswer` validates answer, applies scoring + gamble adjustment, persists `PlayerAnswer`
  - `PlaceGamble` validates 0-100 and persists `GamblingAction`
  - `LeaveGame` and disconnect cleanup

**Access control enforced in hub logic**
- Hosting is restricted to authenticated users (`ClaimTypes.NameIdentifier` required)
- Guests can still join and play by PIN

**Updated `QuizRush.Api/Program.cs`**
- Added SignalR registration: `builder.Services.AddSignalR()`
- Mapped GameHub endpoint: `app.MapHub<GameHub>("/hub/game")`
- Registered `ScoreCalculationService` in DI

---

## 7. Sprint 3 - Scoring Service + Unit Tests

### Problem
Sprint 3 scoring logic (time penalty + gambling) was not centralized in a dedicated service and had no focused test coverage.

### What was done

**Created `QuizRush.Infrastructure/Services/ScoreCalculationService.cs`**
- `CalculatePoints(basePoints, timeToAnswerSeconds, isCorrect)`
  - 3% per-second penalty
  - Minimum clamped to 0
  - Wrong answers return 0
- `ApplyGambling(basePoints, gamblingPercentage, isCorrect)`
  - Correct answer: adds gamble amount
  - Wrong answer: subtracts gamble amount

**Created `QuizRush.Tests/GameHubTests.cs`**
- Added tests for scoring and gambling behavior:
  - Correct answer applies time penalty
  - Wrong answer returns zero
  - Correct gamble increases points
  - Wrong gamble reduces points

---

## 8. Sprint 3 - Blazor Web Project Scaffold (Phase 3 setup)

### Problem
Sprint 3 requires a web UI layer, but `QuizRush.Web` project and its service infrastructure did not exist.

### What was done

**Created `QuizRush.Web` project**
- Generated Blazor web app scaffold
- Added project to solution (`QuizRush.slnx`)

**Updated `QuizRush.Web/QuizRush.Web.csproj`**
- Added project references to:
  - `QuizRush.Core`
  - `QuizRush.Infrastructure`
- Added SignalR client package for hub communication

**Updated `QuizRush.Web/Program.cs`**
- Added shared `HttpClient` targeting API base URL (`http://localhost:5176`)
- Registered web services:
  - `GameService`
  - `AuthService`
  - `LocalStorageService`

**Created `QuizRush.Web/Services/GameService.cs`**
- Wraps hub calls (`HostGame`, `JoinGame`, `StartGame`, `SubmitAnswer`, etc.)
- Wires server events (`QuestionReady`, `ScoresUpdated`, `GameEnded`, `GameError`) to component-friendly C# events

**Created `QuizRush.Web/Services/AuthService.cs`**
- Implements login/register API calls
- Persists JWT token into local storage

**Created `QuizRush.Web/Services/LocalStorageService.cs`**
- JS interop wrapper for `localStorage` get/set/remove

---

## 9. Execution Notes / Environment

### Branch setup status
- The requested "create feature branch" step could not be executed from this shell because `git` is not available in the environment (`git` command not recognized).
- All code changes were still implemented in the working tree so they can be committed once git is available in your terminal/IDE.

### Build verification
- `QuizRush.slnx` builds successfully.
- `QuizRush.Tests` target framework was aligned to `net8.0` and tests now run successfully (`21/21` passing).

---

## 10. Sprint 3 - Host/Player Blazor UI (Phase 4)

### Problem
Even with GameHub and services in place, the web app still showed default template pages and had no gameplay screens.

### What was done

**Created auth pages**
- `QuizRush.Web/Components/Pages/Login.razor`
- `QuizRush.Web/Components/Pages/Register.razor`
- Added JWT token persistence and auth utility methods in `AuthService`

**Created host and player pages**
- `QuizRush.Web/Components/Pages/HostGame.razor`
- `QuizRush.Web/Components/Pages/JoinGame.razor`
- Host page now:
  - Loads quizzes for authenticated users
  - Creates session via hub and displays session PIN
  - Starts/advances/ends game flow
- Join page now:
  - Lets guests enter PIN + name
  - Handles answer submission and gambling choice
  - Displays question and leaderboard updates

**Created game components**
- Host components:
  - `HostQuizSelector.razor`
  - `HostWaitingRoom.razor`
  - `HostQuestionDisplay.razor`
  - `HostLeaderboard.razor`
- Player components:
  - `PlayerPinJoin.razor`
  - `PlayerWaitingRoom.razor`
  - `PlayerQuestionDisplay.razor`
  - `PlayerGamblingWidget.razor`
  - `PlayerLeaderboard.razor`
  - `PlayerFeedback.razor`
- Shared components:
  - `Leaderboard.razor`
  - `QuestionTimer.razor`
  - `PointsPopup.razor`

**Updated navigation**
- `NavMenu.razor` now links to:
  - Home
  - Join Game
  - Host Game
  - Login
  - Register

---

## 11. Sprint 3 - GameHub Hardening + Access Enforcement

### Problem
GameHub methods needed stricter host-only control and clearer lifecycle broadcasting.

### What was done

**Updated `QuizRush.Api/Hubs/GameHub.cs`**
- Added XML summaries on hub methods
- Enforced host-only access for:
  - `StartGame`
  - `NextQuestion`
  - `EndGame`
- Tracked host connection id per active session
- Added lifecycle broadcasts:
  - `AnswerRevealed`
  - `ScoresUpdated`
  - `SubmissionPhaseEnded`

**Access behavior now**
- Registered users can host
- Guests can join/play via PIN
- Non-host callers cannot control question progression

---

## 12. Sprint 3 - Documentation Handoff

### What was done

**Created `PROJECT_KNOWLEDGE.md`**
- Updated architecture to include Blazor + SignalR
- Documented hub contracts and access rules
- Added component hierarchy overview

**Created `DEPLOYMENT_SPRINT_3.md`**
- Added local run instructions for API + Web
- Documented JWT key and DB prerequisites
- Added port mapping summary
