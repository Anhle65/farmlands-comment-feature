# Farmlands Comment Feature

A standalone commenting feature designed to be embedded in a blog. Supports posting top-level comments, one level of replies, editing and deleting your own comments within 5 minutes, and sorting newest-first. No real auth — a name is captured in the browser `userName` and a uuid per-browser `userId` is generated and persisted in `localStorage`.

## Tech Stack

- **Frontend:** React 19 + TypeScript + Vite
- **Backend:** ASP.NET Core (.NET 10) Web API
- **Database:** SQLite (via EF Core)
- **Tests:** xUnit (backend), Postman collection (API integration)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 20+ and npm (Vite 8 requires Node 20.19+)

## Running Locally

Open two terminals — one for the backend, one for the frontend.

### 1. Backend (API + database)

```bash
cd backend
dotnet run
```

- Listens on **http://localhost:8000**
- On first startup, EF Core applies the initial migration and creates `backend/Comments.db`, then seeds a few demo comments. Both steps are idempotent — restarting the server does not duplicate data. To start fresh, stop the server and delete `backend/Comments.db`.
- CORS is allowed for `http://localhost:5173` (the Vite dev server).

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

- Vite serves the app at **http://localhost:5173**.
- The frontend points at `http://localhost:8000` (hardcoded in `frontend/src/api.ts`). If you change the backend port, update that constant.

### Tests

```bash
cd backend
dotnet test
```

Runs the xUnit suite under `backend/CommentApi.Tests/`. There are no automated frontend tests in this submission (see *Decisions & Tradeoffs*).

### Postman collection

The `postman/` directory contains a collection and a `local` environment that hit the running backend:

- `postman/comments.postman_collection.json`
- `postman/local.postman_environment.json`

Import both into Postman and select the `local` environment to exercise the API end-to-end.

## Project Structure

```
backend/                      ASP.NET Core Web API
  Controllers/                CommentsController — GET/POST/PATCH/DELETE /api/comments
  Models/                     Comment entity
  Data/                       EF Core DbContext + CommentStore
  Migrations/                 EF Core migrations (Init creates Comments table)
  Authorization/              Per-request author-id check for edit/delete
  Validation/                 Content length + non-empty rules
  CommentApi.Tests/           xUnit unit tests
frontend/
  src/components/             CommentList, CommentCard
  src/api.ts                  Axios calls to the backend
  src/types.ts                Shared Comment type
postman/                      API collection + local environment
```

## Decisions & Tradeoffs

- **SQLite over Postgres/SQL Server.** Zero-setup for reviewers — no Docker, no service to install. The schema and EF Core code would work against another provider with only the connection-string and provider package swapped.
- **No auth, browser-scoped identity.** A `userId` UUID is generated once and stored in `localStorage`, alongside the user's typed name. The 5-minute edit/delete window is enforced *both* in the UI (buttons hidden when expired or not your comment) and on the server (the controller checks the same window before mutating). Client checks are convenience; the server is authoritative.
- **Soft delete (tombstone) instead of hard delete.** Deleting a parent comment that has replies would orphan them, so the row stays with `isDeleted = true` and the UI renders `[comment deleted]`. Replies underneath stay intact.
- **One level of reply nesting.** The data model permits arbitrary depth (each comment has a nullable `parentId`), but the UI flattens replies under their top-level parent rather than recursing — matches the spec and keeps the UI simple.
- **Newest-first at the top level, oldest-first within a reply thread.** Top-level comments sort by `createdAt` descending; replies sort ascending within their parent so conversations read top-to-bottom.
- **Backend tests via xUnit only; frontend tests skipped.** xUnit covers the validation, authorization, and store logic. The Postman collection exercises the full HTTP surface against a running server. No frontend test framework is installed — this was a conscious tradeoff to keep the deliverable focused; adding Vitest + Testing Library is the first thing on the *improvements* list.
- **Inline form duplication in `CommentList` and `CommentCard`.** The "Add comment" form and the "Reply" form share the same shape but live in separate components. We unified the *handler* (`handlePost(parentId: number | null, content)`) but intentionally kept the JSX duplicated rather than extracting a shared composer — at this size the abstraction wasn't paying for itself.

## What I'd Improve With More Time

- **Frontend test infrastructure.** Add Vitest + `@testing-library/react` + `jsdom` and write component tests for `CommentList`/`CommentCard` (validation, the 5-minute window UI, optimistic vs. server state).
- **Real authentication.** Replace the localStorage `userId` with proper sign-in so identity survives across browsers and can't be spoofed by editing localStorage.
- **Pagination / virtualisation.** The current implementation fetches all comments at once. Cursor-based pagination on the server and a windowed list on the client would scale better.


## AI Usage

AI tools (Claude Code) were used throughout this exercise. The strategy and a record of prompts follow.

### Strategy

- **Backend (.NET / EF Core / xUnit).** Used Claude to scaffold the controller, EF Core model, and migration; to draft xUnit tests for validation and the 5-minute window; and to wire up the per-request author-id check. Hand-tweaked names, error shapes, and seed data. 
- **Database.** Schema + initial EF Core migration generated with Claude; reviewed by hand. Settled on SQLite for reviewer-friendliness (decision discussed above). The `parentId` nullability and the soft-delete `isDeleted` column came out of that discussion.
- **Frontend (React + TypeScript).** Claude drafted `CommentList` and `CommentCard`, the axios client in `api.ts`, and the inline edit / reply / delete flows. Most pushback happened here: I declined a proposed shared `CommentComposer` component twice in favour of unifying the handler (`handlePost`) and leaving the form JSX inline at each call site. I also asked Claude to identify redundancies in code that can be removed.

- **What was written by hand.** Naming choices, sketching unit tests for each layer that cover enough cases for each feature, write core logic for each methods but ask Claude to check quality + refactor, the CLAUDE.md workflow rules (TDD, no real auth, validation limits), the project structure, and the final calls on tradeoffs. Claude generated template files + fixes code; I drove the design, made decision and double-check code.

### Prompts

The verbatim prompts below are grouped by area. The frontend section captures this README's session in full; earlier sessions (backend, database, initial frontend scaffolding) are listed as placeholders for me to paste in.

#### Backend

Listed in chronological order. TDD-driven throughout: every new behaviour started with a failing xUnit test, then the smallest production change to make it green, then a refactor. The arc of the sessions: project rules → 5-minute-window clock injection → `SoftDelete` redesign + delete authorization → controller status-code assertion style → PATCH endpoint via `JsonPatchDocument`.

- "create CLAUDE.md with this requirements before do anything. NOTE: we use TDD during this folder"

- "look at the /CommentApi.Tests/Unit/CommentAuthorizationTests.cs(37), why this test return False?" (boundary test was flaking — `DateTimeOffset.UtcNow` was being read twice, once when seeding `CreatedAt` and once inside `CheckCanModify`, so the elapsed delta sometimes crossed the window)
- "yes injected clock for the test" (added a `DateTimeOffset now` parameter to `CheckCanModify` so the test pins both ends of the comparison; updated the four existing tests to pass a fixed `Now`)

- "so how to assign id for the test? Also as a senior in testing, do you think my SoftDelete is good" (asked for a senior-level review of the test set)
- "As a senior testing, will you take the authorization inside both model and controller layers?" (settled the design: store does persistence only, controller delegates the owner + 5-minute check to `CommentAuthorization.CheckCanModify`)
- "so please update my test" (dropped the now-redundant `SoftDelete_WrongAuthor` test from the store suite — authorization is `CheckCanModify`'s job. This follow SOLID priciple of each function should do 1 thing)

- "I update Authorization shape, please follow this" (after Claude advised dropping the name check, the user reduced `CheckCanModify` to a one-arg `isOwner` and showed me the new shape)
- "look at the requirement, it mentioned about verify by name" (direct pushback — the brief explicitly says verify by name; I reversed my prior advice and acknowledged the miss)
- "let's go with Path A — name only, however name and its session both need match, so that other people with same name but different uuid can not authorize" (committed to the tuple design with a clear rationale, rather than accepting either of my offered paths verbatim)

- "when we use this IsAssignableFrom and when use http status code" (settled the rule: `IsAssignableFrom<IStatusCodeActionResult>` for status-only assertions; `IsType<T>` only when the next line needs `.Value`)

- "how to pass the content as patchDoc" (settled on `JsonPatchDocument<Comment>` rather than a typed DTO so the request body is a real RFC 6902 patch document)

#### Database

- "Now I write enough logic, how to create a schema in Sqlite to work as real table, then run and test in real website"
- "what is the different of using ef and the using Microsoft.Data.Sqlite;" (with a `Microsoft.Data.Sqlite` raw-SQL code sample pasted in for comparison)
- "teach me how to create and migrate"
- "in Program.cs explain why we need Database.Migrate() + SeedIfEmpty()"
- "review again please"
- "I update fixture in code, please check" (xUnit `IDisposable` fixture opening a SQLite `:memory:` connection per test)
- "I update but some test not pass" (4 seed-driven tests dropped, 1 converted to use the new fixture)
- "review in CommentStore.cs for me" (caught a `Max(c => c.Id) + 1` bug that would throw on an empty table and defeat AUTOINCREMENT)
- "so now look at the tests and codebase, is it good enough to cover basic feature base on the requirements? Any edge-case that I forgot?"

#### Frontend

- "now you are a senior in UX/UI deign. We have basically backend + database, look closely in the requirement in Claude.md. I am not focusing on UI + frontend. I need basic, acceptable for the appearance. Follow SOLID principle, now I am extracting CommentCard.tsx where I can pass into a ListComment.tsx. My requirement are: a form for create new comment that will be reused in reply + edit, a "reply" button, for author - show a delete + edit option. All these actions on the same card below comment's content. Output: sketches some designs please"
- "please use Comment component in CommentList, remove the use of CommentRow"
- "please hook the handleEdit function for reply comment and use axios
- "for the form, please add a textfield for input name"
- "why we need to use application/json-patch+json?"
- "any concerns of using application/json-patch+json"
- "add a button for 'Add comment' so that when I click, I create a comment with parentId is null"
- "use mui/material to re-design for UI"