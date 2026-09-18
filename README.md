# InternLink - ASP.NET Core MVC

This solution is an ASP.NET Core MVC conversion of the InternLink internship governance application.

## Visual Studio
1. Open `InternLink/InternLink.sln`.
2. Set `InternLink` as the startup project.
3. Build the solution.
4. Run with Ctrl+F5 or F5.

## Demo accounts
Password for the seeded accounts: `Demo!Pass123`

- Student: `student@internlink.demo`
- Coordinator: `coordinator@internlink.demo`
- Faculty: `faculty@internlink.demo`

## Clearance PDF
The Student clearance page now has a **Download PDF** action that returns an actual PDF file directly. It does not open the browser print dialog. The certificate includes the enrolled student's name and the faculty member stored on the final grade.

## Database
SQLite is stored in `internlink.db`. On a fresh database the demo records are seeded automatically.

If you are testing the seeded demo again after an earlier run, stop the app and remove the existing `internlink.db` so the seed data can be recreated.

## Deploying (Render)

This repo includes a `Dockerfile` and `render.yaml` for one-click deployment on
[Render](https://render.com)'s free tier: **New + → Blueprint**, pick this repo,
and Apply. The blueprint provisions a free Render Postgres database alongside
the web service; the app detects the `DATABASE_URL` env var Render injects and
switches from SQLite to Postgres automatically (see `Program.cs`), so data
survives restarts and redeploys.

Render's free Postgres databases are deleted 30 days after creation. When
that happens, re-run the Blueprint (or create a fresh free database and point
`DATABASE_URL` at it) — the schema and demo accounts are recreated
automatically on first startup.
