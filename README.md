
# RACEDAYNOVA 
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
 
## GitHub Actions/ CI Workflow
 <img width="1360" height="673" alt="image" src="https://github.com/user-attachments/assets/2a48038c-07f8-4ba2-926c-297c27aea740" />

 <img width="1357" height="669" alt="image" src="https://github.com/user-attachments/assets/1cfea90d-35b9-47a1-8f16-ac241670ce47" />


## YouTube Video Link

https://youtu.be/KcU54pNc64M?si=3n3uw5Gh6QfOYIys
 
