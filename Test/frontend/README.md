# FilmAPI + FilmFrontend

Documentazione tecnica aggiornata del progetto cinema composto da backend API e frontend statico.

## Panoramica

- `backend/`: API ASP.NET Core Minimal API (`net10.0`) con EF Core MySQL/MariaDB.
- `frontend/`: host ASP.NET Core per pagine statiche (`wwwroot`) e logica client JS.
- `backend/tests/`: test unitari e integrazione xUnit.
- `FilmAPI.sln`: solution principale con backend, frontend e test.

## Funzionalita principali

- Autenticazione JWT con refresh token in cookie HttpOnly.
- Ruoli applicativi (`Admin`, `PowerUser`, `User`) e route-guard frontend.
- Gestione catalogo (film, registi, cinema, sale, show, categorie).
- Programmazione pubblica con filtri e dettaglio film.
- Acquisto biglietti con lock temporaneo posti anti race condition.
- Pagamento misto (credito + Stripe Checkout).
- Ricarica credito utente (manuale + webhook Stripe asincrono).
- Validazione ticket manuale/QR con vincolo sul cinema.
- Invio email conferma con PDF allegato.

## Architettura di runtime

- Backend (default dev): `http://localhost:5001`
- Frontend (default dev): `http://localhost:5285`
- Endpoint health: `GET /health`

Frontend e backend comunicano via Fetch API (`frontend/wwwroot/js/api-client.js`).

## Sicurezza e hardening

- CORS limitato a host locali (`localhost`, `127.0.0.1`) in dev.
- Validazione configurazione critica all'avvio (JWT/Stripe/ExternalAuth).
- Rate limiting globale e policy specifica su endpoint auth/webhook.
- Logging richieste con evidenza errori 4xx/5xx e trace id.

## Configurazione

Variabili ambiente principali (`backend/.env.example`):

- Database: `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`
- JWT: `JWT_SECRET_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`
- Stripe: `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`
- SMTP: `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM`
- OAuth esterni: `EXTERNAL_AUTH_*`

Nota: in ambiente `Production` placeholder non validi su chiavi critiche bloccano lo startup.

## Avvio locale

### Opzione 1: script Windows

```bat
start-dev.cmd
```

Per fermare i processi:

```bat
stop-dev.cmd
```

### Opzione 2: manuale

Backend:

```bash
dotnet run --project backend/FilmAPI.csproj --urls "http://localhost:5001"
```

Frontend:

```bash
dotnet run --project frontend/FilmFrontend.csproj --urls "http://localhost:5285"
```

## Build e test

Build backend:

```bash
dotnet build backend/FilmAPI.csproj
```

Build frontend:

```bash
dotnet build frontend/FilmFrontend.csproj
```

Test backend:

```bash
dotnet test backend/tests/FilmAPI.Tests.csproj
```

## CI

Pipeline GitHub Actions disponibile in `.github/workflows/ci.yml` con:

- build backend
- build frontend
- test backend

## Note operative

- Il progetto contiene anche una copia storica in `FilmAPI/`; per sviluppo corrente usare `backend/` e `frontend/` dalla root `Test/`.
- I file log/runtime sono esclusi da git tramite `.gitignore` in root.
