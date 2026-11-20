# Docsum

A personal document assistant for German bureaucratic letters.
Upload a PDF, get a plain-language summary, and ask questions about what you need to do next.

## Features

- **Cookie-based authentication** (BCrypt password hashing, HttpOnly cookie)
- **Nested folders** — organise letters in a folder tree
- **PDF upload** — validated by magic bytes, 10 MB limit, stored under server-generated names
- **Background processing** — text extraction on upload, status transitions (`Uploaded → Processing → Done/Failed`)
- **AI summary** — short plain-language summary of each letter (optional, requires an API key)
- **Document chat** — ask questions grounded in the letter text; conversation history is persisted
- **Move / delete** — move documents between folders, delete documents and folders safely

## Tech stack

- Frontend: React 19, TypeScript, React Router, TanStack Query, Vite
- Backend: ASP.NET Core (Clean Architecture), EF Core
- Database: PostgreSQL (Podman)
- PDF text extraction: PdfPig
- AI: any OpenAI-compatible chat-completions endpoint

## Architecture

```text
Domain ← Application ← Infrastructure ← Server
```

| Project | Responsibility |
|---|---|
| `Docsum.Domain` | `User`, `Document`, `Folder`, `ChatMessage` |
| `Docsum.Application` | Services, DTOs, abstractions (no EF/HTTP) |
| `Docsum.Infrastructure` | EF Core, repositories, file storage, PdfPig, AI client, processing worker |
| `Docsum.Server` | ASP.NET Core controllers, cookie auth, DI composition |
| `Docsum.Application.Tests` | xUnit tests with hand-written fakes |
| `Docsum.Client` | React SPA |

## Running locally

1. Start the database:

   ```bash
   podman start docsum-postgres
   ```

2. Configure the connection string (once):

   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" \
     "Host=localhost;Database=docsum;Username=docsum_admin;Password=..."
   ```

3. Apply migrations:

   ```bash
   dotnet ef database update --project Docsum.Infrastructure --startup-project Docsum.Server
   ```

4. Start the API:

   ```bash
   dotnet run --project Docsum.Server --launch-profile http
   ```

5. Start the client in another terminal:

   ```bash
   cd Docsum.Client
   bun install
   bun run dev
   ```

The client runs on `http://localhost:5173` and proxies `/api` to `http://localhost:5119`.

Uploaded files are written to `Docsum.Server/storage` by default. Override with
`Storage:Path` in configuration for deployments.

## AI configuration (optional)

Text extraction works without AI. Summaries and chat require an OpenAI-compatible
endpoint (OpenAI, Azure OpenAI, OpenRouter, Gemini's OpenAI-compatible API, …):

```bash
dotnet user-secrets set "Ai:BaseUrl" "https://api.openai.com/v1"
dotnet user-secrets set "Ai:Model"   "gpt-4o-mini"
dotnet user-secrets set "Ai:ApiKey"  "sk-..."
```

Other settings:

```jsonc
"Ai": {
  "BaseUrl": "https://api.openai.com/v1",
  "Model": "gpt-4o-mini",
  "ApiKey": "",          // keep out of appsettings.json; use user-secrets/env vars
  "TimeoutSeconds": 120
}
```

Without a key the API still extracts text and marks documents `Done`; `summary`
stays `null` and chat returns `503`.

> **Privacy:** letters are sent to the configured provider when summaries or chat
> answers are generated. These are often sensitive tax/insurance documents. Use a
> provider with appropriate data-processing terms, or a self-hosted model.

## Processing pipeline

A hosted worker polls every 3 seconds for new uploads:

1. `Uploaded` → `Processing`
2. Extract text with PdfPig → `ExtractedText`
3. If AI is configured, generate a summary → `Summary`
4. `Done` (or `Failed` with an `ErrorMessage` when the PDF cannot be read)

Scanned PDFs without a text layer are not OCR'd; they end up `Failed`.

## API

All document/folder/chat routes require the `auth_token` cookie.

### Auth

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/auth/register` | `{ email, password }` |
| `POST` | `/api/auth/login` | `{ email, password }`, sets cookie |
| `POST` | `/api/auth/logout` | clears cookie |
| `GET` | `/api/auth/me` | current user |

### Folders

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/folders` | flat list `[{ id, name, parentId }]` |
| `POST` | `/api/folders` | `{ name, parentId? }` |
| `PUT` | `/api/folders/{id}` | `{ name, parentId? }` (rename/move) |
| `DELETE` | `/api/folders/{id}` | contents move one level up, then folder is removed |

### Documents

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/documents` | list documents |
| `GET` | `/api/documents/{id}` | detail incl. `summary`, `errorMessage`, `folderId` |
| `POST` | `/api/documents` | multipart: `file`, optional `folderId` |
| `PATCH` | `/api/documents/{id}` | `{ folderId }` — move (`null` = root) |
| `DELETE` | `/api/documents/{id}` | removes DB row and stored file |

### Chat

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/documents/{id}/chat` | full conversation history |
| `POST` | `/api/documents/{id}/chat` | `{ question }` → `{ user, assistant }` |

### Example

```bash
curl -c cookies.txt -X POST http://localhost:5119/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"..."}'

curl -b cookies.txt -F "file=@letter.pdf;type=application/pdf" \
  http://localhost:5119/api/documents

curl -b cookies.txt -H "Content-Type: application/json" \
  -d '{"question":"What is the deadline?"}' \
  http://localhost:5119/api/documents/1/chat
```

## Tests

```bash
dotnet test
```

Tests cover authentication, document upload/move/delete, folder validation and
cycle prevention, and chat behaviour (including AI-unavailable paths).
