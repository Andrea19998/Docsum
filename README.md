# Docsum

A personal document assistant for German bureaucratic letters.
Upload a PDF, get a plain-language summary and the action you need to take.
*(Work in progress: authentication is done, document upload and AI summary are next.)*

## Tech stack
- Frontend: React, TypeScript, React Router, TanStack Query
- Backend: ASP.NET Core (Clean Architecture), EF Core
- Database: PostgreSQL (Podman)
- Auth: cookie-based sessions, BCrypt password hashing

## Running locally
1. Start the database: `podman start docsum-postgres`
2. Start the API: `dotnet run --project Docsum.Server --launch-profile http`
3. Start the client: `cd Docsum.Client && npm run dev`

The API connection string is read from user-secrets (`ConnectionStrings:Default`).

## Architecture
Domain ← Application ← Infrastructure ← Server (dependencies point inward).
