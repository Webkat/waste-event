# Waste2x – Live Field Activity Dashboard

Real-time dashboard that shows live field activity (trucks, collection requests
and service status) for a waste management system. The backend consumes the provided
`WasteEventService` event stream and exposes it to a Vue 3 frontend via SSE.

## Architecture

```
CSV files → WasteEventService.Listen() → BackgroundService → DTO mapping
   → Channel fan-out → SSE endpoint → EventSource → Vue store → map + feed + summary
```

- **Transport:** Server-Sent Events (SSE) — one-way stream, native browser support.
- **Fan-out:** the source is read once in a `BackgroundService` and broadcast to all
  clients via `System.Threading.Channels`, so multiple operators can share the same stream.
- **Contract:** each event is mapped to a flat DTO with an `eventType` discriminator.

## Run with Docker (recommended)

Requires Docker Desktop. From the repository root:

```
docker compose up --build
```

Open http://localhost:8080

Compose starts two containers: backend (ASP.NET Core) and frontend (Vue app
served by nginx). nginx serves the built frontend and proxies `/api` calls to the
backend, so everything runs on the same origin — no CORS configuration needed.

## Run locally without Docker

Requires the .NET 10 SDK and Node.js. Two parts, each in its own terminal.

### Backend

```
cd WasteEventApi
dotnet run --urls "http://localhost:5180"
```

The stream can be tested directly in a browser or with:

```
curl.exe -N http://localhost:5180/api/events/stream
```

### Frontend

```
cd WasteEventFrontend
npm install
npm run dev
```

Open http://localhost:3000

## Project structure

| Folder | Contents |
|---|---|
| `WasteEventLib/` | Provided event model, service and CSV data (unchanged) |
| `WasteEventApi/` | ASP.NET Core backend: SSE endpoint, DTO mapping, broadcaster, hosted service |
| `WasteEventFrontend/` | Vue 3 + Vuetify + OpenLayers dashboard |

## Configuration

`speedMultiplier` is set in `WasteEventApi/appsettings.json` under `WasteEvents:SpeedMultiplier`
(default 120). A higher value means faster replay of the simulated dataset.

## Note on replay

`WasteEventService.Listen()` replays the dataset once from app start and stops when
there are no more events. A client sees events from the moment it connects — there
is no history or loop. Restart the backend (or `docker compose up`) to replay the
dataset from the beginning.
