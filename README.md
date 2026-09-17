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
