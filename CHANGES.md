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
