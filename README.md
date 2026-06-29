# Waste2x – Live Field Activity Dashboard

Real-time dashboard der viser live field activity (lastbiler, indsamlingsanmodninger
og service-status) for et waste management-system. Backend forbruger den udleverede
`WasteEventService` event-stream og eksponerer den til en Vue 3-frontend via SSE.

## Arkitektur

```
CSV-filer → WasteEventService.Listen() → BackgroundService → DTO-mapping
   → Channel fan-out → SSE-endpoint → EventSource → Vue store → kort + feed + summary
```

- **Transport:** Server-Sent Events (SSE) — én-vejs stream, native browser-support.
- **Fan-out:** kilden læses én gang i en `BackgroundService` og broadcastes til alle
  klienter via `System.Threading.Channels`, så flere operatører kan dele samme stream.
- **Kontrakt:** hvert event mappes til en flad DTO med en `eventType`-diskriminator.

## Kør med Docker (anbefalet)

Kræver Docker Desktop. Fra repo-roden:

```
docker compose up --build
```

Åbn http://localhost:8080

Compose starter to containere: backend (ASP.NET Core) og frontend (Vue-app
serveret af nginx). nginx serverer den byggede frontend og proxy'er `/api`-kald
videre til backend, så alt kører på samme origin — ingen CORS-opsætning nødvendig.

## Kør lokalt uden Docker

Kræver .NET 10 SDK og Node.js. To dele, hver i sin terminal.

### Backend

```
cd WasteEventApi
dotnet run --urls "http://localhost:5180"
```

Streamen kan testes direkte i en browser eller med:

```
curl.exe -N http://localhost:5180/api/events/stream
```

### Frontend

```
cd WasteEventFrontend
npm install
npm run dev
```

Åbn http://localhost:3000

## Projektstruktur

| Mappe | Indhold |
|---|---|
| `WasteEventLib/` | Udleveret event-model, service og CSV-data (uændret) |
| `WasteEventApi/` | ASP.NET Core backend: SSE-endpoint, DTO-mapping, broadcaster, hosted service |
| `WasteEventFrontend/` | Vue 3 + Vuetify + OpenLayers dashboard |

## Konfiguration

`speedMultiplier` styres i `WasteEventApi/appsettings.json` under `WasteEvents:SpeedMultiplier`
(standard 120). Højere værdi = hurtigere replay af det simulerede datasæt.

## Bemærk om replay

`WasteEventService.Listen()` afspiller datasættet én gang fra app-start og stopper,
når der ikke er flere events. En klient ser events fra det øjeblik den forbinder —
der er ingen historik eller loop. Genstart backend (eller `docker compose up`) for
at afspille datasættet forfra.
