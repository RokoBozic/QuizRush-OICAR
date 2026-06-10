# QuizRush Tests — Exam Prep Guide

**Last validated:** all **32 unit tests pass** (`dotnet test QuizRush.slnx`)

**Integration tests:** manual only — see `docs/IntegrationTests.docx` (Word document).

Use this doc to understand **what each test does**, **why it exists**, and **how the test code works**.

---

## How to run

```bash
dotnet test QuizRush.slnx                          # all 32 unit tests
dotnet test QuizRush.Tests                         # backend only (22)
dotnet test QuizRush.Web.Tests                     # web only (5)
dotnet test QuizRush.Mobile.Tests                  # mobile only (5)
dotnet test --filter "FullyQualifiedName~GameHub"  # one class
```

---

## Unit vs integration (30-second version)

| | **Unit test (automated)** | **Integration test (manual)** |
|---|---------------------------|-------------------------------|
| Where | C# in `*Tests` projects | `docs/IntegrationTests.docx` |
| Tests | One class/function | Full user flows in browser/app |
| Database/API | Fake (InMemory DB, mock HTTP) | Real API + Web + Mobile running |
| Example | `CalculatePoints` math | Register → host game → player joins |

---

## Test syntax cheat sheet

### `[Fact]`
Marks a method as a **single test case**. xUnit runs it automatically.

```csharp
[Fact]
public void MyTest() { ... }
```

### `async Task` + `await`
Used when the test waits on I/O (HTTP, database, SignalR).

```csharp
[Fact]
public async Task MyAsyncTest()
{
    var result = await service.DoSomethingAsync();
    Assert.NotNull(result);
}
```

### `Assert.*` — the core checks

| Method | Meaning | Example |
|--------|---------|---------|
| `Assert.Equal(expected, actual)` | Values must match | `Assert.Equal(70, result)` |
| `Assert.NotNull(value)` | Must not be null | `Assert.NotNull(session)` |
| `Assert.True(condition)` | Condition must be true | `Assert.True(response.IsSuccessStatusCode)` |
| `Assert.False(condition)` | Condition must be false | `Assert.False(await auth.IsAuthenticatedAsync())` |
| `Assert.Single(collection)` | Collection has exactly 1 item | `Assert.Single(quiz.Questions)` |
| `Assert.ThrowsAsync<T>(...)` | Async code must throw type `T` | `await Assert.ThrowsAsync<ArgumentException>(...)` |

### Arrange – Act – Assert (AAA pattern)

Every good test follows three steps:

```csharp
// ARRANGE — set up inputs
var service = new QuizService(context);

// ACT — call the method under test
var result = await service.CreateAsync(model, creatorId: 1);

// ASSERT — verify outcome
Assert.Equal("Sample Quiz", result.Title);
```

### EF Core InMemory (backend unit tests)

Replaces SQL Server with an in-memory database — fast, isolated, no LocalDB needed.

```csharp
var options = new DbContextOptionsBuilder<QuizRushDbContext>()
    .UseInMemoryDatabase(databaseName: "UniqueDbName")  // unique name per test
    .Options;
using var context = new QuizRushDbContext(options);
var service = new QuizService(context);
```

### Mock HTTP (`StubHttpMessageHandler`)

Web unit tests fake the API — no server needed.

```csharp
var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
{
    Content = JsonContent.Create(authResponse)
});
var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
```

### `FakeLocalStorageService`

Subclass of `LocalStorageService` that stores data in a `Dictionary` instead of browser JS localStorage.

---

## Validation summary

| Check | Result |
|-------|--------|
| All 32 unit tests execute | **Pass** |
| Assertions match production code | **Valid** |
| Score math correct | **Valid** (formula: `base × (1 − seconds × 0.03)`) |
| Exception types match services | **Valid** |
| Integration testing | **Manual** — `docs/IntegrationTests.docx` |

---

# Project 1: `QuizRush.Tests` — Backend (22 unit tests)

## A. Scoring — `GameHubTests.cs` (8 unit tests)

> **Tests:** `ScoreCalculationService` (live game scoring). File name says "GameHub" but these are **unit** tests, not hub tests.

| # | Test | Purpose | What it proves |
|---|------|---------|----------------|
| 1 | `CalculatePoints_CorrectAnswer_AppliesTimePenalty` | Time matters | 100 pts, 10 sec → **70** (3% penalty per second) |
| 2 | `CalculatePoints_WrongAnswer_ReturnsZero` | Wrong = no points | Any wrong answer → **0** |
| 3 | `CalculatePoints_NeverDropsBelowZero` | Score floor | Huge time penalty → **0**, not negative |
| 4 | `ApplyGambling_CorrectAnswer_AddsStakeFromPlayerScore` | Gamble win | 50% gamble on 100 pts, correct → **+50** stake added |
| 5 | `ApplyGambling_WrongAnswer_LosesStakeFromPlayerScore` | Gamble loss | 50% gamble, wrong → **−50** stake lost |
| 6 | `ApplyGambling_100Points50PercentWin_...` | Full gamble win scenario | Starting 100 → ends **150** |
| 7 | `ApplyGambling_100Points50PercentWrong_...` | Full gamble loss scenario | Starting 100 → ends **50** |
| 8 | `ApplyGambling_ZeroPercentage_LeavesScoreUnchanged` | No gamble | 0% gamble → score unchanged |

**Syntax used:** `[Fact]`, `new ScoreCalculationService()`, `Assert.Equal`, no async, no mocks.

**Exam tip:** Know the formula: `earned = basePoints × max(1 − time × 0.03, 0)`. Wrong answer always returns 0 before gambling.

---

## B. Quiz CRUD — `Quiz/` folder (6 unit tests)

| # | Test | Purpose | What it proves |
|---|------|---------|----------------|
| 1 | `CreateQuiz_ValidData_ReturnsCreatedQuiz` | Happy path create | Valid quiz saved with title + 1 question |
| 2 | `CreateQuiz_NoQuestions_ThrowsArgumentException` | Validation | Empty questions list → rejected |
| 3 | `CreateQuiz_NoCorrectAnswer_ThrowsArgumentException` | Validation | Question with no correct answer → rejected |
| 4 | `UpdateQuiz_ValidData_UpdatesQuiz` | Happy path update | Title and question text change in DB |
| 5 | `UpdateQuiz_NonExistentId_ThrowsKeyNotFoundException` | Not found | Fake quiz ID → exception |
| 6 | `DeleteQuiz_ExistingId_RemovesQuiz` | Happy path delete | Quiz gone after delete |

**Syntax used:** `UseInMemoryDatabase`, `Assert.ThrowsAsync<ArgumentException>`, `Assert.Single`.

**Exam tip:** `QuizService` validates: ≥1 question, each question has ≥1 correct answer.

---

## C. Game sessions — `GameSession/` folder (4 unit tests)

| # | Test | Purpose | What it proves |
|---|------|---------|----------------|
| 1 | `CreateSession_ValidQuiz_ReturnsSessionWithCode` | Host creates game | Returns 6-character PIN code |
| 2 | `CreateSession_NonExistentQuiz_ThrowsKeyNotFoundException` | Bad quiz ID | Invalid quiz → exception |
| 3 | `GetSessionByCode_ExistingCode_ReturnsSession` | PIN lookup works | Find session by code |
| 4 | `GetSessionByCode_NonExistentCode_ReturnsNull` | Unknown PIN | Fake code → null |

**Syntax used:** InMemory DB, `Assert.NotNull`, `Assert.Equal`, string length check.

**Exam tip:** Session codes are **6 characters**, uppercase. Created only if host owns the quiz.

---

## D. User account — `User/` folder (4 unit tests)

| # | Test | Purpose | What it proves |
|---|------|---------|----------------|
| 1 | `UpdateProfile_ValidData_UpdatesUsernameAndEmail` | Profile edit | Username and email saved |
| 2 | `UpdateProfile_NonExistentUser_ThrowsKeyNotFoundException` | Missing user | Fake user ID → exception |
| 3 | `ChangePassword_CorrectCurrentPassword_UpdatesPassword` | Password change OK | New password works via hash verify |
| 4 | `ChangePassword_WrongCurrentPassword_ThrowsUnauthorizedAccessException` | Wrong old password | Rejected with `UnauthorizedAccessException` |

**Syntax used:** InMemory DB, `PasswordHashProvider.VerifyPassword`, `Assert.ThrowsAsync`.

---

# Project 2: `QuizRush.Web.Tests` — Web client (5 unit tests)

## F. Web unit — `Unit/AuthServiceTests.cs` (5 tests)

Tests Blazor `AuthService` with **fake HTTP** + **fake localStorage**.

| # | Test | Purpose | What it proves |
|---|------|---------|----------------|
| 1 | `LoginAsync_OnSuccess_StoresTokenAndReturnsAuth` | Login works | JWT saved to storage after successful API response |
| 2 | `LoginAsync_OnFailure_ReturnsNull` | Login fails safely | 401 → null, no token stored |
| 3 | `LogoutAsync_ClearsStoredToken` | Logout works | Token removed from storage |
| 4 | `IsAuthenticatedAsync_ReturnsFalseWhenNoToken` | Auth check | Empty storage → not authenticated |
| 5 | `RegisterAsync_OnSuccess_ReturnsMessage` | Register works | API success message returned |

**Syntax used:** `StubHttpMessageHandler`, `FakeLocalStorageService` (override), `JsonContent.Create`, `HttpStatusCode`.

**Exam tip:** Web `AuthService` stores token under key `"token"` in localStorage. Failed login returns `null`, not an exception.

---

# Project 3: `QuizRush.Mobile.Tests` — Mobile client (5 unit tests)

## H. Mobile unit — `Unit/AppSessionTests.cs` (5 tests)

Tests `AppSession` — in-memory login state for the MAUI app (no API, no UI).

| # | Test | Purpose | What it proves |
|---|------|---------|----------------|
| 1 | `Set_MarksSessionAuthenticated` | After login | `IsAuthenticated = true`, username/email set |
| 2 | `Clear_MarksSessionUnauthenticated` | After logout | `IsAuthenticated = false`, token null |
| 3 | `SessionChanged_FiresOnSet` | UI refresh event | `SessionChanged` event fires on login |
| 4 | `Restore_PopulatesIdentityFromStorage` | App restart | Token/username/email restored from storage |
| 5 | `UpdateIdentity_UpdatesUsernameAndEmail` | Profile update | Name/email change, token stays same |

**Syntax used:** Plain `[Fact]`, event subscription `session.SessionChanged += () => fired = true`, no mocks.

**Exam tip:** `AppSession` is the mobile app's **single source of truth** for "who is logged in". UI listens to `SessionChanged`.

---

# Manual integration tests (Word document)

Open **`docs/IntegrationTests.docx`** — BonCon-style manual tests (**5 Web + 5 Mobile**, not merged):

**Web (Blazor):**
1. Test Web login functionality  
2. Test Web Register functionality  
3. Test Web Adding quizzes  
4. Test Web Host game session  
5. Test Web Player join and live gameplay  

**Mobile (MAUI):**
1. Test Mobile login and register functionality  
2. Test Mobile Player join game  
3. Test Mobile Host game session  
4. Test Mobile Quizzes management  
5. Test Mobile Profile and password change  

Each section uses **Heading 1 + bullet steps** (UI actions, API calls, expected behaviour).

---

# Quick exam Q&A

**Q: What framework do we use?**  
A: xUnit. Tests are marked with `[Fact]`.

**Q: How do backend unit tests avoid needing SQL Server?**  
A: `UseInMemoryDatabase()` — EF Core in-memory provider.

**Q: How do integration tests work?**  
A: They are **manual** — follow the steps in `docs/IntegrationTests.docx` with API + Web + Mobile running.

**Q: What does `Assert.ThrowsAsync<T>` test?**  
A: That async code throws a specific exception type (e.g. validation failure).

**Q: What is NOT tested automatically?**  
A: Full end-to-end flows (login → host → play → leaderboard). Those are in the Word integration doc.

**Q: What must be running for unit tests?**  
A: Nothing external. InMemory DB and mock HTTP only.

---

# Test count at a glance

| Project | Unit tests |
|---------|------------|
| QuizRush.Tests | **22** |
| QuizRush.Web.Tests | **5** |
| QuizRush.Mobile.Tests | **5** |
| **Total automated** | **32** |
| **Manual integration** | `docs/IntegrationTests.docx` (7 flows) |
