# Stato Avanzamento Fasi

| Fase | Stato | Note |
|---|---|---|
| Fase 1 | Completata | Modelli auth/categorie estesi, JWT configurato, migration `AddCategorieAndAuth` creata, seed aggiornato |
| Fase 2 | Completata | Aggiunti `ICategoriaService`/`CategoriaService`, endpoint categorie su service, DTO film con `CategorieIds` |
| Fase 3 | Completata | Aggiunti `IAuthService`/`AuthService`, `AuthEndpoints` delegati ai servizi |
| Fase 4 | Completata | Middleware auth attivo, policy configurate, CORS con header `Authorization`, ruoli su endpoint aggiornati |
| Fase 5 | Completata | Endpoint `/profilo`, `/prenotazioni`, `/admin/utenti` aggiunti; ownership check mantenuto; blocco downgrade ultimo admin |
| Fase 6 | Completata | `CustomWebApplicationFactory` estesa con reset DB auth seed e helper client admin; test integrazione aggiornati per auth |
| Fase 7 | Completata | `auth.js` e `api-client.js` presenti e allineati al flusso Bearer/refresh; pagine login/registrazione con JS; navbar auth-aware |
| Fase 8 | Completata | Aggiunto `route-guard.js` con mappa permessi e redirect |
| Fase 9 | Completata | Create `programmazione.html` e `categorie.html`, aggiunti filtri citta/data/categoria, badge/filtro categorie su film/home |
| Fase 10 | Completata | Supporto `?prenota=id` in `profilo.js`, modifica profilo e annullamento prenotazioni operativi |
| Fase 11 | Quasi completata | Build/test completi verdi; resta solo verifica manuale end-to-end per tutti i profili |

## Checklist Fase 1

- [x] Modelli `UserRole`, `User`, `RefreshToken`, `Prenotazione`, `Categoria`, `FilmCategoria`
- [x] Aggiornati `Film` e `FilmDbContext`
- [x] Pacchetti JwtBearer e BCrypt presenti
- [x] Configurazione JWT in `Program.cs`
- [x] Aggiornati `.env` e `.env.example`
- [x] Migration `AddCategorieAndAuth` creata
- [x] Seed admin + 12 categorie
- [x] Test esistenti completamente verdi

## Checklist Fase 2

- [x] Creati `ICategoriaService` e `CategoriaService` con CRUD
- [x] DTO categorie presenti
- [x] DTO/endpoint film aggiornati per `CategorieIds` e include categorie
- [x] Endpoint categorie con codici risposta coerenti
- [x] Registrazione DI per service
- [ ] Verifica manuale Swagger completata

## Checklist Fase 3

- [x] Creati `IAuthService` e `AuthService` con register/login/refresh/logout/me
- [x] DTO auth presenti
- [x] `AuthEndpoints` aggiornati
- [x] Registrazione DI per auth service
- [ ] Verifica completa flussi e codici errore

## Checklist Fase 4

- [x] `UseAuthentication`/`UseAuthorization` attivi
- [x] Policy `AdminOnly`/`PowerUserOrAdmin`/`Authenticated` configurate
- [x] CORS aggiornato con header `Authorization`
- [x] Matrice permessi endpoint aggiornata (admin/powerUser/user/anonimo)

## Checklist Fase 5

- [x] Endpoint profilo e prenotazioni esposti (`/profilo`, `/prenotazioni`)
- [x] Endpoint admin utenti esposto (`/admin/utenti`)
- [x] Ownership check preservato su operazioni utente
- [x] Blocco downgrade ultimo admin implementato
- [ ] Verifica Swagger fase completata

## Checklist Fase 6

- [x] `CustomWebApplicationFactory` estesa
- [x] Helper client autenticato (admin)
- [x] Reset DB esteso con seed auth base
- [x] Test integrazione aggiornati per endpoint protetti
- [x] Suite completa test verde
- [ ] A1-A8, RB1-RB8, CAT1-CAT5, PR1-PR5 da aggiungere (non presenti in questa codebase)

## Checklist Fase 7

- [x] `auth.js` presente
- [x] `api-client.js` con Bearer + refresh interceptor
- [x] Login/registrazione con JS
- [x] Navbar auth-aware

## Checklist Fase 8

- [x] Creato `route-guard.js`
- [x] Mappa permessi pagine e redirect implementata
- [x] Layout principali usano navbar auth-aware
- [x] Inclusione `route-guard.js` in tutte le pagine

## Checklist Fase 9

- [x] Creare `programmazione.html` con filtri citta/data/categoria e prenota auth-aware
- [x] Creare `categorie.html` CRUD admin
- [x] Aggiornare `films/index/home` per categorie (badge + multi-select + filtro)

## Checklist Fase 10

- [x] `profilo.html` con dati personali e prenotazioni
- [x] Supporto parametro `?prenota=id`
- [x] Modifica profilo
- [x] Annullamento prenotazioni

## Checklist Fase 11

- [x] Test verdi finali
- [ ] Verifica manuale flussi Admin/PowerUser/User/Anonimo
- [ ] Verifica redirect frontend
- [x] `status.md` aggiornato
- [x] `changelog.md` aggiornato in modo finale
