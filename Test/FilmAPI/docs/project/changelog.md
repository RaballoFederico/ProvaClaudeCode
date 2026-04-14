# Changelog

## 2026-04-14

- Implementata Iterazione 4 con modello dati multi-sala: `Sala`, `Show`, `Acquisto`, `Biglietto`, `CreditoUtente`, `TransazioneCredito`, `PrenotazioneTemporanea`, estensioni su `Film`, `Cinema`, `Utente`.
- Aggiornato `FilmDbContext` con nuovi `DbSet`, relazioni e indici (`Show` unique su sala/data/orario, hash/codice ticket univoci, indici lock temporanei).
- Aggiunti servizi business: `SalaService`, `ShowService`, `BigliettoService`, `CreditoService`, `PagamentoService`, `PrenotazioneTempCleanupService`, piu servizi `EmailService` e `PdfService`.
- Introdotta integrazione Stripe server-side con creazione `PaymentIntent` e verifica pagamento completato prima della conferma acquisto.
- Implementato invio email di conferma acquisto (SMTP) con allegato PDF ticket multipagina.
- Estesi endpoint backend: sale/show/programmazione/acquisto/credito/validazione, inclusi endpoint `payment-intent`, dettaglio lock e download PDF.
- Rifatto frontend pubblico con `programmazione.html`, `scheda-film.html`, `my-cinemas.html` e flusso acquisto `acquista.html` -> `pagamento.html`.
- Implementate pagine backoffice `sale.html`, `shows.html`, `validazione.html`, `ricarica-credito.html` e pagina utente `user-biglietti.html`.
- Migliorata gestione redirect login con callback completa (path + querystring).
- Aggiornato seed DB con dataset esteso e coerente per demo end-to-end.

## 2026-04-08

- Implementata struttura servizi per categorie (`ICategoriaService`, `CategoriaService`) e auth (`IAuthService`, `AuthService`), con wiring DI in `Program.cs`.
- Refactor di `AuthEndpoints` e `CategorieEndpoints` per delegare logica applicativa ai service e uniformare i codici risposta.
- Esteso modello dati con `RefreshToken` e `Prenotazione`, aggiornato `FilmDbContext`, creato migration `AddCategorieAndAuth`.
- Aggiornate policy/ruoli endpoint (Admin/PowerUser/User), CORS con header `Authorization`, e regole anti-downgrade ultimo admin.
- Aggiornato seed iniziale con 12 categorie e account admin.
- Estesa `CustomWebApplicationFactory` con seed auth e helper `CreateAdminClientAsync`; adeguati test integrazione agli endpoint protetti.
- Aggiunto frontend `route-guard.js` per mappa permessi pagina e redirect automatici.
- Aggiunte pagine frontend `programmazione.html` (filtri citta/data/categoria + prenota auth-aware) e `categorie.html` (CRUD admin).
- Aggiornate `films.html` e `index.html` per categorie (badge, filtro categoria, multi-select nel form film).
- Aggiornati `navbar.html`/`sidebar.html` con navigazione auth-aware estesa e link admin per categorie.
- Completato supporto `?prenota=id` in `js/pages/profilo.js` con apertura automatica modal prenotazione.
