# QuizRush — Integration Test Flows (Manual)

**Word document:** `docs/IntegrationTests.docx` (BonCon-style: 5 Web + 5 Mobile sections)

Integration testing is **manual only** — 10 separate flows (5 web, 5 mobile).

**Prerequisites:** SQL Server LocalDB, API on `http://localhost:5176`, Web on `http://localhost:5261`, Mobile emulator pointed at API.

Regenerate Word file:

```bash
python docs/build_integration_docx.py
```

---

## Web application — 5 integration tests

| # | Section | Route / focus |
|---|---------|---------------|
| 1 | Test Web login functionality | `/login`, POST `api/auth/login` |
| 2 | Test Web Register functionality | `/register`, POST `api/auth/register` |
| 3 | Test Web Adding quizzes | `/quizzes/create`, POST `api/quiz` |
| 4 | Test Web Host game session | `/host`, SignalR `HostGame` |
| 5 | Test Web Player join and live gameplay | `/join`, `/play/{PIN}`, full game loop |

---

## Mobile application — 5 integration tests

| # | Section | Tab / focus |
|---|---------|-------------|
| 1 | Test Mobile login and register functionality | Play tab, auth API |
| 2 | Test Mobile Player join game | Play tab, SignalR join + answer |
| 3 | Test Mobile Host game session | Host tab, host controls |
| 4 | Test Mobile Quizzes management | Quizzes tab, quiz CRUD |
| 5 | Test Mobile Profile and password change | Account tab, profile + password API |

---

## Automated tests (unit only)

```bash
dotnet test QuizRush.slnx   # 32 unit tests
```

See `testsMy.md` for unit test details.
