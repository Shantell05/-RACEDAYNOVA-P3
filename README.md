# RaceDay Nova - Part 2: RESTful API

PROG6212 Programming 2B - Part 2 (Restful API Development)
Student: **Shantell Malatjie (ST10475153)**

RaceDay Nova is an event management system for the South African running, walking and cycling community.
This repository holds the **ASP.NET Core 8 Web API** that powers it: SQL Server via Entity Framework Core (code-first),
server-side session authentication, role-based access for **Organisers** and **Participants**, Swagger, unit/integration
tests and a GitHub Actions CI/CD pipeline. The design follows the Part 1 ERD, SQL script and endpoint plan (see `/docs`).

| | |
|---|---|
| **GitHub repository** | `PASTE YOUR REPOSITORY LINK HERE` |
| **YouTube video (unlisted)** | `PASTE YOUR UNLISTED YOUTUBE LINK HERE` |

---

## Repository structure

```
RaceDayNova.sln
.github/workflows/part2-ci.yml     GitHub Actions: validate structure, build, run all tests
docs/                              Part 1 ERD, SQL script, endpoint plan + Part 2 notes
src/RaceDayNova.Api/
  Controllers/                     Auth, Users, Events, EventCategories, Categories, Enrolments, Results, Health
  Models/                          Seven EF Core entities + constants (roles, statuses)
  Data/                            RaceDayDbContext (Fluent API) and SeedData
  Dtos/                            Request / response models and mappers
  Security/                        PBKDF2 password hasher, [SessionAuthorize] attribute
  Services/                        Weather provider (Open-Meteo)
  Swagger/                         Operation filter that documents access rules
tests/RaceDayNova.Tests/           xUnit tests (integration + unit)
```

## Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* SQL Server - **LocalDB** (installed with Visual Studio) or any SQL Server instance; SSMS to inspect the data
* Visual Studio 2022 (or VS Code / `dotnet` CLI)

## Setup and run instructions

1. **Clone** the repository and open `RaceDayNova.sln` in Visual Studio (or open a terminal in the repo root).
2. **Connection string** - open `src/RaceDayNova.Api/appsettings.json`. The default targets LocalDB and creates a database called
   `RaceDayNovaApiDB`:
   ```json
   "RaceDayNovaDb": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayNovaApiDB;Trusted_Connection=True;TrustServerCertificate=True"
   ```
   Use a different server by changing `Server=`. (Use a **new** database name rather than the Part 1 `RaceDayNovaDB`, which already
   contains tables created by the SQL script.)
3. **Create the database (code-first).** Install the EF tool once, then create and apply the initial migration:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate --project src/RaceDayNova.Api
   dotnet ef database update --project src/RaceDayNova.Api
   ```
   Commit the generated `Migrations` folder. (If you skip this step the API still builds the schema from the model on first start.)
   The database is seeded with the same data as the Part 1 script.
4. **Run the API:**
   ```bash
   dotnet run --project src/RaceDayNova.Api --launch-profile https
   ```
   Open **https://localhost:7080/swagger** (or press F5 in Visual Studio - it opens Swagger automatically).
5. **Run the tests:**
   ```bash
   dotnet test
   ```
   The tests use an in-memory database and a fake weather provider, so they need no SQL Server and no internet.

### Demo accounts (seeded, password `Password123!`)

| Role | Email |
|---|---|
| Organiser | `thandi.maseko@racedaynova.co.za` (owns events 2001 and 2003) |
| Organiser | `johan.botha@racedaynova.co.za` (owns event 2002) |
| Participant | `lebo.molefe@racedaynova.co.za` |
| Participant | `zanele.khumalo@racedaynova.co.za` |

Organisers need no invite code: choose **Organiser** when registering, or log in as a Participant and use **Profile -> Switch to Organiser**.

### Trying it in Swagger

1. `POST /api/auth/login` with a demo account. The browser stores the session cookie automatically.
2. Call any endpoint - each one lists who may use it. Log out with `POST /api/auth/logout` and log in as another role to see the 403 responses.

---

## The two roles

| | **Organiser** | **Participant** |
|---|---|---|
| Registration | Open to anyone | Open to anyone |
| Events | Create, edit and delete **their own** events | Browse and view events |
| Categories | Create / edit / delete categories; add them to their events | View categories |
| Enrolments | View all enrolments for **their** events | Enter an event by choosing a category; view / cancel **their own** enrolments |
| Results | Capture and correct finish time, position and pace for **their** events | View **their own** results |
| Profile | View / update own profile | View / update own profile |

Organiser functionality is closed to Participants and vice versa: the API answers **403 Forbidden** to a logged-in user with the wrong role
(or who does not own the resource) and **401 Unauthorized** to anyone not logged in.

## Authentication design

* **Register** `POST /api/auth/register` - personal details plus a role. Organiser accounts need no code, and a logged-in user can switch role with `PUT /api/users/me/role`. Passwords must be 8+ characters with a letter and a digit.
* **Passwords are hashed** with salted PBKDF2 (HMAC-SHA256, 210 000 iterations) - the plain text is never stored.
* **Login** `POST /api/auth/login` creates a **server-side session** that stores the user's **id and role**; the browser receives only an opaque,
  `HttpOnly` session cookie. Every later request is authorised from the session by the `[SessionAuthorize]` attribute - nothing the client sends
  can change its role.
* **Logout** `POST /api/auth/logout` destroys the session. Sessions expire after 60 idle minutes.

## Endpoints

All endpoints are visible and testable in Swagger, each with a description, expected input and responses.

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Public |
| GET / PUT | `/api/users/me` | Any logged-in user |
| GET | `/api/events` (filters: eventType, city, province, status, fromDate, toDate, search, organiserId) | Public |
| GET | `/api/events/{id}` | Public |
| POST | `/api/events` | Organiser |
| PUT / DELETE | `/api/events/{id}` | Organiser (owner) |
| GET | `/api/categories` | Public |
| POST | `/api/categories` | Organiser |
| PUT / DELETE | `/api/categories/{id}` | Organiser |
| GET | `/api/events/{id}/categories` | Public |
| POST | `/api/events/{id}/categories` | Organiser (owner) |
| DELETE | `/api/events/{id}/categories/{categoryId}` | Organiser (owner) |
| POST | `/api/events/{id}/enrolments` | Participant |
| GET | `/api/enrolments/me` | Participant |
| GET | `/api/events/{id}/enrolments` | Organiser (owner) |
| DELETE | `/api/enrolments/{id}` | Participant (own) |
| POST | `/api/enrolments/{id}/result` | Organiser (owner) |
| PUT | `/api/results/{id}` | Organiser (owner) |
| GET | `/api/results/me` | Participant |
| GET | `/api/events/{id}/results` | Organiser (owner) |
| GET | `/api/events/{id}/route` | Public |
| GET | `/api/events/{id}/weather` | Public |
| GET | `/api/health` | Public |

See [`docs/Part2_API_Implementation_Notes.md`](docs/Part2_API_Implementation_Notes.md) for business rules, status codes and the deviations from the Part 1 plan.

## Unit tests

`tests/RaceDayNova.Tests` (xUnit) starts the real API in memory and calls it over HTTP. It covers success **and** failure scenarios for:

* **Registration and login** - valid/duplicate/invalid registration, role switching, wrong password, session cookie, logout, password hashing
* **Event management** - Organisers create / update / delete; Participants and anonymous callers rejected; ownership enforced
* **Role enforcement** - a matrix of every Organiser-only, Participant-only and logged-in-only endpoint tested as the wrong role (403) and anonymous (401)
* **Event enrolments** - enrolling, recording of participant + event + category, duplicates, closed events, full categories, cancelling, re-enrolling
* **Categories, results, profile, route, weather, health and Swagger** (checks every planned endpoint appears in the Swagger document)

## CI/CD

`.github/workflows/part2-ci.yml` runs on every push and pull request. It (1) **validates the repository structure** (required files, folders and README
sections), then (2) restores, builds in Release and **runs all tests**, uploading the results.

**Green build screenshot:**

![Green CI build](docs/images/ci-green-build.png)

> Add your screenshot as `docs/images/ci-green-build.png` after the first green run on GitHub.

## Video

Unlisted YouTube video demonstrating the running API, code structure, endpoints, authentication and unit tests:
`PASTE YOUR UNLISTED YOUTUBE LINK HERE`

## Design notes

* **Code-first, matching Part 1:** table/column names, types, keys, unique indexes, defaults, CHECK constraints and cascade rules in
  `RaceDayDbContext` mirror `docs/RaceDay_Nova_Database.sql` and the ERD (seven entities).
* **Consistent errors:** failures return RFC 7807 problem details with the right status (400, 401, 403, 404, 409, 503).
* **Data integrity:** one active enrolment per participant per event, category capacity enforced, unique finishing positions per category,
  events with enrolments are cancelled rather than erased so results history is preserved.

---

# Part 3 - MVC Frontend, Azure Blob Storage and Docker

Part 3 adds an ASP.NET Core 8 MVC frontend under `src/RaceDayNova.Mvc`. The frontend uses the existing Part 2 REST API for all data operations; it has **no database context or direct database access**. It uses a responsive RaceDay Nova design, role-specific navigation, server-side MVC session state, and the API's session cookie for authenticated requests.

## Part 3 features

- **Authentication:** participant and organiser registration, login, local session state, API session-cookie forwarding, logout, and protected role-specific pages.
- **Organiser pages:** dashboard, event creation/editing/deletion, event banner upload, reusable category management, assigning categories and entry fees to events, enrolment viewing, and results entry.
- **Participant pages:** event browsing and filters, event details/categories/weather, event enrolment, personal entries and cancellation, race history/results, and profile updates/profile picture upload.
- **UI:** responsive layouts, mobile navigation wrapping, validation messages, accessible labels, status badges, success/error feedback, and consistent RaceDay Nova colours and typography.
- **Image storage:** the MVC app sends image files to the API. The API validates image type and size and stores files in Azure Blob Storage. It saves event banner and profile image URLs on the related database records. MVC never talks directly to Azure.
- **Docker:** multi-stage Dockerfile at both repository root and `src/RaceDayNova.Mvc/Dockerfile`.
- **CI:** `.github/workflows/part3-ci.yml` validates the Part 3 structure, restores and builds the solution, runs the API tests, and builds the MVC container image.

## Run locally (Visual Studio 2022 / .NET 8)

1. Install the .NET 8 SDK and open `RaceDayNova.sln`.
2. Configure the API database in `src/RaceDayNova.Api/appsettings.json`. The supplied API defaults to SQL Server LocalDB. Ensure LocalDB is available, or change the connection string to your SQL Server instance.
3. Start `RaceDayNova.Api` first using the `https` launch profile. The API should be reachable at `https://localhost:7080` and `http://localhost:5080`.
4. Start `RaceDayNova.Mvc`. The default API URL in `src/RaceDayNova.Mvc/appsettings.json` is `https://localhost:7080/`.
5. Open the MVC URL shown in the console. For seeded accounts, use the demo credentials in the API section above (`Password123!`).
6. Organiser registration needs no code. Users can also switch role from the Profile page (`PUT /api/users/me/role`).

## Azure Blob Storage configuration

Set the following API configuration values using user secrets or environment variables. Keep real credentials out of Git:

- `AzureBlob__ConnectionString` - the storage account connection string.
- `AzureBlob__Container` - container name, default `raceday-media`.

The API upload routes are `POST /api/events/{id}/banner` and `POST /api/users/me/profile-picture`, both using multipart form data with a `file` field. Uploads accept JPEG, PNG or WebP up to 5 MB. The storage account/container must allow the returned image URL to be read by the frontend; follow your institution's Azure security guidance before enabling public blob access in production. If the connection string is not configured, the rest of the app remains usable and upload requests return a clear service-unavailable response.

## Run the MVC frontend in Docker

Start the API on the host first using the API `http` launch profile (port 5080), and make sure its database is available. Then, from the repository root, build and run the MVC container:

```bash
docker build -f src/RaceDayNova.Mvc/Dockerfile -t racedaynova-mvc:part3 .
docker run --rm -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5080/ --add-host=host.docker.internal:host-gateway racedaynova-mvc:part3
```

Open `http://localhost:8080`. On Docker Desktop for Windows/macOS, `host.docker.internal` is normally available automatically; the `--add-host` argument supports Linux Docker. The API remains a separate service, consistent with the requirement that the MVC frontend consume the Part 2 API. To run the API itself in a container, configure an appropriate reachable SQL Server and API connection string rather than using Windows LocalDB from a Linux container.

## Verify the application

- Browse events without logging in; use the search, activity and city filters.
- Register a Participant, log in, open an event, select a category, and check **My entries** and **My results**.
- Log out, then log in as an Organiser. Check the dashboard, create/edit an event, create categories, attach categories and entry fees, review enrolments, and capture a result.
- Test a wrong-role URL and an unauthenticated protected URL. The MVC redirects users to login; the API remains the authority for role and resource ownership checks.
- Upload a banner/profile picture with Azure configured, then verify the image URL is returned by the API and displayed in the MVC pages.
- Build the solution, run the API tests, and run the Docker command above before recording the demonstration.

## Part 3 GitHub workflow and commit evidence

Work on a clearly named branch such as `part-3-mvc-frontend`. The rubric requires at least 20 meaningful commits; these must be created and pushed by the project owner as real Git history, not simulated by this archive. Suggested commit-sized steps:

1. Add MVC project and solution configuration.
2. Add API client and session-cookie forwarding.
3. Add shared layout and responsive design tokens.
4. Add event browsing and search filters.
5. Add registration page and validation.
6. Add login and role session handling.
7. Add logout and protected navigation.
8. Add organiser dashboard.
9. Add event creation form.
10. Add event editing and deletion.
11. Add reusable category management.
12. Add event-category assignment and fees.
13. Add organiser enrolment viewing.
14. Add results capture.
15. Add participant home and event detail pages.
16. Add participant enrolment and cancellation.
17. Add participant results history.
18. Add profile editing.
19. Add API image upload integration.
20. Add Azure configuration, Docker, README, and Part 3 CI workflow.

After pushing, open the GitHub Actions run and capture a screenshot of a successful green build. Replace the placeholder repository and unlisted YouTube links near the top of this README with your real links before submission.

## Demonstration video checklist

Record an unlisted YouTube video showing: (1) application and API startup, (2) public event browsing and filtering, (3) Participant registration/login/enrolment/my entries/my results/profile, (4) Organiser login/dashboard/event/category/enrolment/result workflows, (5) Azure banner upload and displayed image, (6) key MVC-to-API code, (7) Docker build and run command, and (8) the green GitHub Actions workflow. Explain that MVC uses the API for all data operations and does not connect to the database directly.
