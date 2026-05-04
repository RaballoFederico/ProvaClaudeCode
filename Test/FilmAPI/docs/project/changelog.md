# Changelog

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
