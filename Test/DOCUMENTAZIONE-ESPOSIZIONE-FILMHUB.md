# Documentazione Completa Progetto FilmHub (Backend + Frontend)

## 1. Obiettivo del progetto
Il progetto implementa una piattaforma cinema full-stack chiamata **FilmHub**, con queste finalita principali:
- consultazione catalogo film e programmazione pubblica;
- gestione utenti autenticati con ruoli e permessi;
- acquisto biglietti con gestione posti e pagamento;
- area amministrativa per contenuti (film, registi, cinema, sale, show);
- validazione titoli di accesso in fase di check-in.

Il repository di lavoro corrente e:
- `D:\Scuola\5IA\INFO\ClaudeCode\Test`

## 2. Architettura generale
Il sistema e composto da due applicazioni ASP.NET Core separate:

1. **Backend API**
- percorso: `D:\Scuola\5IA\INFO\ClaudeCode\Test\backend`
- tecnologia: ASP.NET Core Minimal API (.NET 10), Entity Framework Core, MySQL
- responsabilita: business logic, sicurezza, persistenza, integrazioni esterne (Stripe, SMTP, OAuth)

2. **Frontend Web**
- percorso: `D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend`
- tecnologia: host ASP.NET Core statico + pagine HTML/CSS/JS in `wwwroot`
- responsabilita: UI/UX, chiamate API, route-guard client, gestione sessione lato browser

## 3. Struttura cartelle (parte importante per esposizione)
### 3.1 Backend
- `Program.cs`: bootstrap applicazione, DI, auth, CORS, rate limiting, mapping endpoints.
- `Data/FilmDbContext.cs`: configurazione ORM e mapping DB.
- `Model/*.cs`: entita dominio.
- `DTO/*.cs`: contratti input/output API.
- `Endpoints/*.cs`: route HTTP organizzate per area funzionale.
- `Services/*.cs`: logica applicativa (auth, pagamento, biglietti, credito, email, TMDB, PDF).
- `tests/`: test unitari e d'integrazione.

### 3.2 Frontend
- `wwwroot/*.html`: pagine utente e pagine di gestione.
- `wwwroot/js/auth.js`: autenticazione client, refresh token, login/logout.
- `wwwroot/js/api-client.js`: wrapper fetch e funzioni verso backend.
- `wwwroot/js/route-guard.js`: controllo accesso alle pagine in base ad auth/ruolo.
- `wwwroot/components/*.html`: navbar, sidebar, footer riutilizzabili.

## 4. Modello dati (dominio)
Entita principali in `backend/Model`:
- anagrafiche: `Utente`, `Ruolo`, `UtenteRuolo`, `RefreshToken`, `NotificaUtente`;
- catalogo: `Film`, `Regista`, `Categoria`, `FilmCategoria`;
- struttura cinema: `Cinema`, `Sala`, `Show`, `Proiezione`;
- vendita e ticketing: `Acquisto`, `Biglietto`, `PrenotazioneTemporanea`, `Prenotazione`;
- funzioni utente: `ProiezioneSalvata`, `CreditoUtente`, `TransazioneCredito`.

Questo modello supporta sia la parte pubblica (navigazione film/proiezioni) sia la parte transazionale (prenotazione/acquisto/validazione).

## 5. Sicurezza applicativa
### 5.1 Autenticazione
- access token JWT (header `Authorization: Bearer ...`)
- refresh token in cookie `HttpOnly` (server side in `AuthEndpoints`)
- endpoint login: `POST /auth/login`
- endpoint refresh: `POST /auth/refresh`
- endpoint logout: `POST /auth/logout`

### 5.2 Ruoli e autorizzazioni
Policy definite in `Program.cs`:
- `AdminOnly`
- `PowerUserOrAdmin`
- `Authenticated`

Uso pratico:
- endpoint gestione contenuti riservati a Admin/PowerUser;
- endpoint amministrativi puri riservati ad Admin;
- endpoint utente personale protetti da `Authenticated`.

### 5.3 Hardening
In `Program.cs` sono configurati:
- CORS controllato;
- rate limiting globale e per endpoint sensibili (`auth`, `auth-sensitive`, `webhook`);
- security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy);
- validazione variabili critiche all'avvio (JWT/Stripe/OAuth).

## 6. Flusso autenticazione e recupero credenziali
### 6.1 Login (nuovo requisito)
Il login avviene **solo con username + password** (non email).
- pagina: `D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html`
- client auth: `D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js`
- backend: `POST /auth/login` in `AuthEndpoints.cs`, validazione in `AuthService.cs`

### 6.2 Recupero password
- UI: bottone "Password dimenticata?" in `login.html` apre una tendina dedicata;
- utente inserisce email e invia richiesta;
- API: `POST /auth/forgot-password`;
- backend invia email con link tokenizzato;
- completamento: `POST /auth/reset-password` con nuova password.

### 6.3 Recupero account (piu sicuro)
Flusso separato rispetto al reset classico:
1. utente clicca "Recupera account";
2. inserisce email;
3. API `POST /auth/recover-account` invia link con `recoverToken`;
4. utente imposta nuova password;
5. API `POST /auth/recover-account/complete` conclude il recupero e restituisce lo username.

Obiettivo: nessuna esposizione username senza prima reimpostare password.

## 7. Mappa API backend per macro-area
Riferimento cartella: `D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints`

### 7.1 Auth e profilo
- `AuthEndpoints.cs`: login, refresh, register, me, external auth, forgot/reset/recover account.
- `UserEndpoints.cs`: profilo utente, cambio password, preferenze, biglietti, acquisti, proiezioni salvate.

### 7.2 Catalogo e contenuti
- `FilmsEndpoints.cs`
- `RegistiEndpoints.cs`
- `CategorieEndpoints.cs`
- `CinemasEndpoints.cs`
- `ProiezioniEndpoints.cs`
- `ShowsEndpoints.cs`
- `SaleEndpoints.cs`

### 7.3 Programmazione pubblica
- `ProgrammazioneEndpoints.cs`
- route pubbliche per feed film/shows e ricerca programmazione.

### 7.4 Acquisto, credito e pagamenti
- `AcquistoEndpoints.cs`: lock posti, rinnovo lock, conferma acquisto, checkout.
- `CreditoEndpoints.cs`: ricariche admin, storico credito, checkout ricarica utente.
- `StripeWebhookEndpoints.cs`: conferme asincrone Stripe.

### 7.5 Operazioni amministrative e check-in
- `AdminEndpoints.cs`: gestione utenti/ruoli, attivazione/disattivazione, sync TMDB.
- `ValidazioneEndpoints.cs`: verifica e conferma validazione biglietti.
- `NotificheEndpoints.cs`: notifiche utente autenticato.

## 8. Mappa pagine frontend (con finalita)
Riferimento cartella: `D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot`

### 8.1 Accesso e identita
- `login.html`: autenticazione + recupero password + recupero account.
- `register.html`: registrazione account.
- `profilo.html`: gestione dati profilo e preferenze.

### 8.2 Navigazione pubblica
- `home.html`: landing principale.
- `programmazione.html`: palinsesto principale consultabile.
- `scheda-film.html`: dettaglio film.
- `films.html`, `registi.html`, `categorie.html`, `cinemas.html`.
- `proiezioni-pubblico.html`: vista pubblica proiezioni.

### 8.3 Area acquisto utente
- `acquista.html`: selezione posti/acquisto.
- `pagamento.html`: fase di pagamento.
- `conferma-acquisto.html`: esito/checkout completato.
- `user-biglietti.html`: elenco titoli acquistati.
- `my-cinemas.html`: cinema preferiti/salvati.

### 8.4 Area operativa/gestionale
- `index.html`: dashboard gestionale.
- `proiezioni.html`, `shows.html`, `sale.html`.
- `validazione.html`, `check-in.html`: flussi controllo accessi.
- `ricarica-credito.html`, `ricarica-credito-utente.html`.

### 8.5 Pagine informative
- `privacy.html`, `termini.html`, `supporto.html`.

## 9. Servizi core backend (business logic)
Riferimento: `D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services`

- `AuthService`: login, register, reset password, gestione sessioni e refresh.
- `JwtService`: generazione/verifica token.
- `BigliettoService`: emissione e gestione biglietti.
- `PagamentoService`: integrazione pagamenti e checkout.
- `CreditoService`: wallet/credito utente.
- `EmailService` + `EmailComposer`: invii SMTP e template mail.
- `PdfService`: generazione allegati PDF (ticket/ricevute).
- `TMDBService` + `TMDBFilmSyncService`: metadati film/cast da TMDB.

## 10. Flussi end-to-end da raccontare all'esame
### Flusso A: Utente standard acquista un biglietto
1. Login da `login.html` (`POST /auth/login`).
2. Scelta film/show su `programmazione.html` / `scheda-film.html`.
3. Avvio acquisto su `acquista.html`.
4. Lock posti (`/acquisto/lock-posti`) per evitare doppie prenotazioni.
5. Pagamento (`/acquisto/pagamento` o checkout Stripe).
6. Conferma (`/acquisto/conferma`) + eventuale invio email/PDF.
7. Consultazione in `user-biglietti.html`.

### Flusso B: Operatore valida un ticket
1. Accesso operatore (Admin/PowerUser).
2. Apertura `validazione.html` o `check-in.html`.
3. Verifica codice (`/validazione/verifica` o `/validazione/qr/{codiceHash}`).
4. Conferma validazione (`/validazione/conferma`) con controllo cinema.

### Flusso C: Recupero account sicuro
1. Utente clicca "Recupera account" in `login.html`.
2. Inserisce email e invia (`/auth/recover-account`).
3. Riceve link con token.
4. Imposta nuova password (`/auth/recover-account/complete`).
5. Backend restituisce username da usare per login.

## 11. Configurazione tecnica da conoscere durante esposizione
Riferimento file:
- `D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\.env.example`
- `D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\appsettings.json`

Variabili chiave:
- DB: `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`
- JWT: `JWT_SECRET_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`
- Stripe: `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`
- SMTP: `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM`
- OAuth esterno: `EXTERNAL_AUTH_*`

## 12. Come avviare il progetto (demo rapida)
Dalla root `D:\Scuola\5IA\INFO\ClaudeCode\Test`:

Opzione script:
- `start-dev.cmd`
- `stop-dev.cmd`

Opzione manuale:
- backend: `dotnet run --project backend/FilmAPI.csproj --urls "http://localhost:5001"`
- frontend: `dotnet run --project frontend/FilmFrontend.csproj --urls "http://localhost:5285"`

## 13. Punti forti da sottolineare in presentazione
- separazione chiara frontend/backend;
- design ad endpoint modulari per dominio;
- sicurezza concreta (JWT + refresh HttpOnly + policy ruoli + rate limit + CSP);
- flussi reali da prodotto (lock posti, checkout, webhook, validazione ticket);
- recupero account sicuro con reset password obbligatorio.

## 14. Limiti e miglioramenti futuri (ottimo per domanda finale)
- estendere copertura test su tutti i flussi auth e acquisto;
- introdurre audit trail avanzato per azioni amministrative;
- migliorare monitoraggio (metriche business + osservabilita centralizzata);
- introdurre pipeline deploy multi-ambiente con secret management dedicato.

---
Documento creato per esposizione tecnica. Se vuoi, nel prossimo passo posso prepararti anche una **scaletta orale da 10 minuti** gia pronta, con testo quasi da ripetere slide per slide.

## 15. Lettura guidata del codice (passo-passo)
Questa sezione e pensata per esposizione tecnica: non solo "cosa fa", ma **come lo fa nel codice**.

### 15.1 Bootstrap backend (`Program.cs`)
Riferimento: [Program.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs)

1. Caricamento ambiente e modalita test:
- [Program.cs:14](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:14) carica `.env` con `Env.Load()`.
- [Program.cs:18](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:18) definisce `isTesting` per separare runtime reale e test.

2. Configurazione DB condizionata:
- [Program.cs:21](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:21) evita DB reale nei test.
- [Program.cs:23](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:23) legge `DB_*` da env.
- [Program.cs:30](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:30) registra `FilmDbContext` MySQL.

3. Dependency Injection servizi:
- [Program.cs:41](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:41)–[55](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:55): registrazione `AuthService`, `PagamentoService`, `BigliettoService`, `EmailService`, hosted services (sync TMDB e cleanup prenotazioni temporanee).

4. JWT e policy ruoli:
- [Program.cs:66](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:66) abilita `JwtBearer`.
- [Program.cs:69](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:69)–[79](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:79): validazione issuer/audience/lifetime/signing key.
- [Program.cs:82](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:82)–[87](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:87): policy `AdminOnly`, `PowerUserOrAdmin`, `Authenticated`.

5. Rate limiting e CORS:
- [Program.cs:94](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:94)–[141](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:141): limiti globali + policy `auth`, `auth-sensitive`, `webhook`.
- [Program.cs:143](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:143)–[181](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:181): CORS con whitelist dinamica e fallback localhost in dev.

6. Security middleware:
- [Program.cs:215](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:215)–[223](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:223): header hardening e CSP.
- [Program.cs:256](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:256)–[260](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:260): ordine corretto `UseRateLimiter` -> `UseAuthentication` -> `UseAuthorization`.

7. Mappatura endpoint:
- [Program.cs:317](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:317)–[333](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs:333): aggancio di tutte le aree API.

---

### 15.2 Autenticazione backend (`AuthEndpoints` + `AuthService`)
Riferimenti:
- [AuthEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs)
- [AuthService.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs)

#### A) Login classico (solo username)
1. endpoint ingresso:
- [AuthEndpoints.cs:21](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:21) `POST /auth/login`.

2. verifica credenziali:
- [AuthService.cs:24](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:24) `LoginAsync`.
- [AuthService.cs:25](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:25)–[31](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:31): trim username e check input.
- [AuthService.cs:33](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:33)–[40](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:40): query utente **solo su `Username`** attivo.
- [AuthService.cs:42](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:42): verifica hash BCrypt password.

3. emissione sessione:
- [AuthService.cs:317](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:317) `BuildLoginResponseAsync`.
- [AuthService.cs:321](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:321): aggiorna `DataUltimoAccesso`.
- [AuthService.cs:324](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:324): genera `accessToken` + expiry.
- [AuthService.cs:326](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:326)–[338](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs:338): crea refresh token hashato a DB.

4. refresh token cookie:
- [AuthEndpoints.cs:29](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:29) set cookie HTTP-only.
- [AuthEndpoints.cs:366](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:366) `AppendRefreshTokenCookie`.

#### B) Recupero password
1. richiesta reset:
- [AuthEndpoints.cs:138](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:138) `POST /auth/forgot-password`.
- [AuthEndpoints.cs:162](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:162): genera token random.
- [AuthEndpoints.cs:163](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:163): salva token in cache con TTL 30 min.
- [AuthEndpoints.cs:167](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:167): crea link verso `login.html?resetToken=...`.

2. conferma reset:
- [AuthEndpoints.cs:274](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:274) `POST /auth/reset-password`.
- [AuthEndpoints.cs:291](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:291): recupera userId da cache token.
- [AuthEndpoints.cs:297](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:297): `authService.SetPasswordAsync(...)`.

#### C) Recupero account (nuovo flusso sicuro)
1. avvio recupero account:
- [AuthEndpoints.cs:200](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:200) `POST /auth/recover-account`.
- [AuthEndpoints.cs:224](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:224): token cache `acct-recover:*`.
- [AuthEndpoints.cs:226](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:226): link `login.html?recoverToken=...`.

2. completamento recupero:
- [AuthEndpoints.cs:255](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:255) `POST /auth/recover-account/complete`.
- [AuthEndpoints.cs:279](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:279): password obbligatoria via `SetPasswordAsync`.
- [AuthEndpoints.cs:301](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs:301): risposta con `username` solo a fine procedura.

---

### 15.3 Frontend login e sessione (`login.html` + `auth.js`)
Riferimenti:
- [login.html](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html)
- [auth.js](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js)

#### A) Login form
- [login.html:537](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html:537): submit handler.
- [login.html:555](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html:555): chiama `Auth.login(username, password)`.
- [auth.js:113](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js:113): `POST /auth/login` con `credentials: 'include'`.
- [auth.js:67](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js:67): `saveSession` persiste `accessToken`, expiry, user in localStorage.

#### B) Refresh automatico
- [auth.js:96](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js:96): `scheduleAutoRefresh()`.
- [auth.js:270](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js:270): `refresh()` prova rinnovo con cookie refresh.
- [auth.js:312](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js:312): `ensureInitialized()` decide se usare token presente o fare refresh silenzioso.

#### C) Recupero password a tendina
- [login.html:446](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html:446): click su "Password dimenticata?" apre/chiude pannello.
- [login.html:455](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html:455): bottone `Invia email reset`.
- [auth.js:160](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js:160): `Auth.forgotPassword(...)`.

#### D) Recupero account
- [login.html:419](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html:419): bottone "Recupera account".
- [auth.js:183](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js:183): `Auth.recoverAccount(...)`.
- [login.html:511](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html:511): se presente `recoverToken`, usa `completeAccountRecovery`.
- [auth.js:206](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js:206): `POST /auth/recover-account/complete`.

---

### 15.4 Acquisto biglietti (backend)
Riferimenti:
- [AcquistoEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AcquistoEndpoints.cs)
- [BigliettoService.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\BigliettoService.cs)
- [PagamentoService.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\PagamentoService.cs)

Passi tecnici principali:
1. accesso area acquisto protetta:
- gruppo route [AcquistoEndpoints.cs:12](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AcquistoEndpoints.cs:12) con `RequireAuthorization("Authenticated")`.

2. lock posti:
- endpoint `POST /acquisto/lock-posti` in [AcquistoEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AcquistoEndpoints.cs).
- logica lock/consistenza in [BigliettoService.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\BigliettoService.cs) (prenotazioni temporanee e scadenza).

3. checkout/pagamento:
- `POST /acquisto/checkout-session` e `POST /acquisto/pagamento` in [AcquistoEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AcquistoEndpoints.cs).
- integrazione Stripe e calcolo importi in [PagamentoService.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\PagamentoService.cs).

4. conferma acquisto:
- `POST /acquisto/conferma` in [AcquistoEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AcquistoEndpoints.cs).
- emissione biglietti + eventuali notifiche/email tramite servizi dedicati.

---

### 15.5 Validazione ticket
Riferimento: [ValidazioneEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\ValidazioneEndpoints.cs)

- [ValidazioneEndpoints.cs:12](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\ValidazioneEndpoints.cs:12): gruppo `/validazione`.
- endpoint verifica:
1. `POST /validazione/verifica`
2. `GET /validazione/qr/{codiceHash}`
3. `POST /validazione/conferma`

In esposizione puoi sottolineare che il check-in non si limita a "codice esistente", ma integra anche i vincoli di autorizzazione ruolo e contesto cinema.

---

### 15.6 API client e route guard lato browser
Riferimenti:
- [api-client.js](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\api-client.js)
- [route-guard.js](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\route-guard.js)

- `api-client.js`: centralizza chiamate HTTP verso backend (`/programmazione`, `/films`, `/cinemas`, `/acquisto`, ecc.) e standardizza error handling.
- `route-guard.js`: evita accesso pagine riservate se non autenticato o senza ruolo adeguato.

Questo rende la UX robusta, ma soprattutto allinea il frontend con le policy backend (difesa in profondita: controllo lato UI + controllo lato API).

---

### 15.7 Come presentare il codice oralmente (schema consigliato)
1. Parti da [Program.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Program.cs): spiega infrastruttura sicurezza.
2. Vai su [AuthEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AuthEndpoints.cs) + [AuthService.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Services\AuthService.cs): spiega identita/sessione.
3. Mostra [login.html](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\login.html) + [auth.js](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\auth.js): collega backend e UX.
4. Chiudi con [AcquistoEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\AcquistoEndpoints.cs) e [ValidazioneEndpoints.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Endpoints\ValidazioneEndpoints.cs): fai vedere il valore reale di business.

