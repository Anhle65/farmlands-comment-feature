# Farmlands Comment Feature

## Workflow

- **TDD is required in this folder.** Write a failing test first, then implement to pass, then refactor. Apply this to both backend (xUnit) and frontend work.

## The Exercise

Build a commenting feature that could be embedded into a blog. You don't need to build the blog itself — focus entirely on the comment section as a standalone feature.

## Requirements

Users should be able to:

- View the list of comments
- Add a new comment (with name and content)
- Reply to an existing comment (one level of nesting is fine)
- Edit or delete their own comments within 5 minutes of posting
- See comments sorted by newest first

## Constraints

- **No real auth** — use a simple "enter your name" flow and store the current user in the browser
- **Validation:** comments must be non-empty and under 1000 characters
- The **5-minute edit/delete window** must be enforced on **both the client and the server**

## Preferred Tech Stack

- **Frontend:** React
- **Backend:** C# (.NET)
- **Database:** SQL (SQL Server, PostgreSQL, or SQLite — your choice)

If you're significantly more comfortable in a different stack, you can use it — but the team works in the above, so submissions in this stack are easier to evaluate and discuss.

## AI Usage

AI tools (Copilot, Claude, ChatGPT, Cursor, etc.) are allowed. If used, include the prompts used or a short note on the strategy (what AI was used for, what was written by hand, where you pushed back on its suggestions). The interest is in how you work with these tools, not whether you avoid them.

## Deliverable

- A GitHub repository link
- A README that covers:
  - How to run the project locally
  - Any decisions or tradeoffs made
  - What you'd improve with more time
- A short note on AI usage (can live in the README)
