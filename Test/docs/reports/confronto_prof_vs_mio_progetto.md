# Confronto progetto prof vs mio progetto

- Progetto prof: `C:\Users\feder\AppData\Local\Temp\codex-film-prof-compare\film-app-main`
- Mio progetto: `D:\Scuola\5IA\INFO\ClaudeCode\Test`
- Confronto principale: esclusi `.git`, `node_modules`, `bin`, `obj`, `.runtime`, build/cache, `.log`, `.env`, `nul`; i binari sono contati ma non confrontati riga-per-riga.
- Metrica: `git diff --no-index --numstat` su copie filtrate temporanee; la differenza percentuale pesa le righe aggiunte/eliminate sul totale righe prof+tuo. I file assenti valgono come righe tutte diverse.

## Root esatto: prof root vs tuo root
- File prof: 373; file tuo: 568; percorsi comuni: 2
- File identici: 0; file testo modificati: 2; binari diversi: 0
- Solo prof: 371; solo tuo: 566
- Righe prof: 121347; righe tuo: 95696
- Similarita righe: 0.06%; differenza stimata: 99.94%
- Overlap percorsi: 0.54% del prof, 0.35% del tuo

| File comune piu diverso | Stato | Righe prof | Righe tue | Similarita | Righe add/del |
|---|---:|---:|---:|---:|---:|
| `.gitignore` | modificato | 492 | 20 | 7.81% | 472 |
| `backend/.env.example` | modificato | 104 | 50 | 64.94% | 54 |

Solo nel prof, primi 40:
- `backend/FilmAPI/DTO/AccountSecurityDTO.cs`
- `backend/FilmAPI/DTO/AuthDTO.cs`
- `backend/FilmAPI/DTO/BigliettoDTO.cs`
- `backend/FilmAPI/DTO/CategoriaDTO.cs`
- `backend/FilmAPI/DTO/CheckoutDTO.cs`
- `backend/FilmAPI/DTO/CinemaDTO.cs`
- `backend/FilmAPI/DTO/CinemaStaffDTO.cs`
- `backend/FilmAPI/DTO/CreditoDTO.cs`
- `backend/FilmAPI/DTO/ExternalAuthDTO.cs`
- `backend/FilmAPI/DTO/FilmDTO.cs`
- `backend/FilmAPI/DTO/MediaDTO.cs`
- `backend/FilmAPI/DTO/OrdineDTO.cs`
- `backend/FilmAPI/DTO/ProfiloDTO.cs`
- `backend/FilmAPI/DTO/ProgrammazioneDTO.cs`
- `backend/FilmAPI/DTO/RegistaDTO.cs`
- `backend/FilmAPI/DTO/SalaDTO.cs`
- `backend/FilmAPI/DTO/ShowCancellationDTO.cs`
- `backend/FilmAPI/DTO/ShowDTO.cs`
- `backend/FilmAPI/DTO/UserAdminDTO.cs`
- `backend/FilmAPI/DTO/UserDataExportDTO.cs`
- `backend/FilmAPI/Data/DataSeeder.cs`
- `backend/FilmAPI/Data/FilmDbContext.cs`
- `backend/FilmAPI/Endpoints/AdminUtentiEndpoints.cs`
- `backend/FilmAPI/Endpoints/AuthEndpoints.cs`
- `backend/FilmAPI/Endpoints/CategorieEndpoints.cs`
- `backend/FilmAPI/Endpoints/CheckoutEndpoints.cs`
- `backend/FilmAPI/Endpoints/CinemasEndpoints.cs`
- `backend/FilmAPI/Endpoints/CreditoEndpoints.cs`
- `backend/FilmAPI/Endpoints/EndpointRouteLocation.cs`
- `backend/FilmAPI/Endpoints/FilmsEndpoints.cs`
- `backend/FilmAPI/Endpoints/MediaEndpoints.cs`
- `backend/FilmAPI/Endpoints/PagamentoEndpoints.cs`
- `backend/FilmAPI/Endpoints/ProfiloEndpoints.cs`
- `backend/FilmAPI/Endpoints/ProgrammazioneEndpoints.cs`
- `backend/FilmAPI/Endpoints/RegistiEndpoints.cs`
- `backend/FilmAPI/Endpoints/SaleEndpoints.cs`
- `backend/FilmAPI/Endpoints/ShowCancellationEndpoints.cs`
- `backend/FilmAPI/Endpoints/ShowsEndpoints.cs`
- `backend/FilmAPI/Endpoints/ValidazioneBigliettiEndpoints.cs`
- `backend/FilmAPI/FilmAPI.csproj`

Solo nel tuo progetto, primi 40:
- `.github/workflows/ci.yml`
- `.vscode/settings.json`
- `DOCUMENTAZIONE_PRESENTAZIONE.docx`
- `Dockerfile.frontend`
- `FilmAPI.sln`
- `FilmAPI/.env.docker`
- `FilmAPI/.env.example`
- `FilmAPI/.gitignore`
- `FilmAPI/DTO/AuthDTOs.cs`
- `FilmAPI/DTO/AuthRequests.cs`
- `FilmAPI/DTO/AuthResponses.cs`
- `FilmAPI/DTO/CategoriaDTOs.cs`
- `FilmAPI/DTO/CinemaDTO.cs`
- `FilmAPI/DTO/FilmDTO.cs`
- `FilmAPI/DTO/ProiezioneDTO.cs`
- `FilmAPI/DTO/RegistaDTO.cs`
- `FilmAPI/Data/FilmDbContext.cs`
- `FilmAPI/Data/Migrations/20260312120528_InitialCreate.Designer.cs`
- `FilmAPI/Data/Migrations/20260312120528_InitialCreate.cs`
- `FilmAPI/Data/Migrations/20260401081248_AddAuthAndCategories.Designer.cs`
- `FilmAPI/Data/Migrations/20260401081248_AddAuthAndCategories.cs`
- `FilmAPI/Data/Migrations/20260408073125_AddCategorieAndAuth.Designer.cs`
- `FilmAPI/Data/Migrations/20260408073125_AddCategorieAndAuth.cs`
- `FilmAPI/Data/Migrations/FilmDbContextModelSnapshot.cs`
- `FilmAPI/Dockerfile`
- `FilmAPI/Endpoints/AdminEndpoints.cs`
- `FilmAPI/Endpoints/AuthEndpoints.cs`
- `FilmAPI/Endpoints/CategorieEndpoints.cs`
- `FilmAPI/Endpoints/CinemasEndpoints.cs`
- `FilmAPI/Endpoints/FilmsEndpoints.cs`
- `FilmAPI/Endpoints/ProiezioniEndpoints.cs`
- `FilmAPI/Endpoints/RegistiEndpoints.cs`
- `FilmAPI/Endpoints/UserEndpoints.cs`
- `FilmAPI/Endpoints/UsersEndpoints.cs`
- `FilmAPI/FilmAPI.csproj`
- `FilmAPI/FilmAPI.http`
- `FilmAPI/FilmFrontend/FilmFrontend.csproj`
- `FilmAPI/FilmFrontend/Program.cs`
- `FilmAPI/FilmFrontend/Properties/launchSettings.json`
- `FilmAPI/FilmFrontend/README.md`

## Backend mappato: prof backend/FilmAPI vs tuo backend
- File prof: 162; file tuo: 356; percorsi comuni: 45
- File identici: 0; file testo modificati: 45; binari diversi: 0
- Solo prof: 117; solo tuo: 311
- Righe prof: 31602; righe tuo: 52585
- Similarita righe: 12.55%; differenza stimata: 87.45%
- Overlap percorsi: 27.78% del prof, 12.64% del tuo

| File comune piu diverso | Stato | Righe prof | Righe tue | Similarita | Righe add/del |
|---|---:|---:|---:|---:|---:|
| `Endpoints/FilmsEndpoints.cs` | modificato | 64 | 1274 | 9.57% | 1210 |
| `Endpoints/ProgrammazioneEndpoints.cs` | modificato | 77 | 364 | 34.92% | 287 |
| `Endpoints/CinemasEndpoints.cs` | modificato | 51 | 240 | 35.05% | 189 |
| `appsettings.json` | modificato | 13 | 48 | 42.62% | 35 |
| `Services/BigliettoService.cs` | modificato | 285 | 812 | 51.96% | 527 |
| `Services/PagamentoService.cs` | modificato | 1012 | 405 | 57.16% | 607 |
| `Services/CreditoService.cs` | modificato | 362 | 159 | 61.04% | 203 |
| `Services/PdfService.cs` | modificato | 128 | 265 | 65.14% | 137 |
| `Services/EmailService.cs` | modificato | 350 | 176 | 66.92% | 174 |
| `Program.cs` | modificato | 282 | 523 | 70.06% | 241 |
| `Endpoints/CreditoEndpoints.cs` | modificato | 122 | 214 | 72.62% | 92 |
| `Endpoints/AuthEndpoints.cs` | modificato | 358 | 572 | 76.99% | 214 |
| `Endpoints/SaleEndpoints.cs` | modificato | 97 | 63 | 78.75% | 34 |
| `Model/Sala.cs` | modificato | 35 | 53 | 79.55% | 18 |
| `Services/CategoriaService.cs` | modificato | 106 | 159 | 80.00% | 53 |
| `Model/Categoria.cs` | modificato | 16 | 24 | 80.00% | 8 |
| `Model/Film.cs` | modificato | 44 | 61 | 83.81% | 17 |
| `Model/RefreshToken.cs` | modificato | 35 | 48 | 84.34% | 13 |
| `Model/Cinema.cs` | modificato | 34 | 46 | 85.00% | 12 |
| `Model/Biglietto.cs` | modificato | 85 | 64 | 85.91% | 21 |
| `Endpoints/CategorieEndpoints.cs` | modificato | 63 | 82 | 86.90% | 19 |
| `DTO/RegistaDTO.cs` | modificato | 33 | 26 | 88.14% | 7 |
| `Services/ExternalAuthService.cs` | modificato | 470 | 594 | 88.35% | 124 |
| `Services/ShowService.cs` | modificato | 363 | 288 | 88.48% | 75 |
| `DTO/BigliettoDTO.cs` | modificato | 123 | 99 | 89.19% | 24 |

Solo nel prof, primi 40:
- `DTO/AccountSecurityDTO.cs`
- `DTO/AuthDTO.cs`
- `DTO/CategoriaDTO.cs`
- `DTO/CheckoutDTO.cs`
- `DTO/CinemaStaffDTO.cs`
- `DTO/ExternalAuthDTO.cs`
- `DTO/MediaDTO.cs`
- `DTO/OrdineDTO.cs`
- `DTO/ProfiloDTO.cs`
- `DTO/ProgrammazioneDTO.cs`
- `DTO/ShowCancellationDTO.cs`
- `DTO/UserAdminDTO.cs`
- `DTO/UserDataExportDTO.cs`
- `Data/DataSeeder.cs`
- `Endpoints/AdminUtentiEndpoints.cs`
- `Endpoints/CheckoutEndpoints.cs`
- `Endpoints/EndpointRouteLocation.cs`
- `Endpoints/MediaEndpoints.cs`
- `Endpoints/PagamentoEndpoints.cs`
- `Endpoints/ProfiloEndpoints.cs`
- `Endpoints/ShowCancellationEndpoints.cs`
- `Endpoints/ValidazioneBigliettiEndpoints.cs`
- `Migrations/20260312120556_InitialCreate.Designer.cs`
- `Migrations/20260312120556_InitialCreate.cs`
- `Migrations/20260406071504_AddCategorieAndAuth.Designer.cs`
- `Migrations/20260406071504_AddCategorieAndAuth.cs`
- `Migrations/20260413200358_AddRefreshTokenDeviceId.Designer.cs`
- `Migrations/20260413200358_AddRefreshTokenDeviceId.cs`
- `Migrations/20260416171534_AddMultisalaTicketing.Designer.cs`
- `Migrations/20260416171534_AddMultisalaTicketing.cs`
- `Migrations/20260419152609_AddStripeCheckoutFieldsToOrdine.Designer.cs`
- `Migrations/20260419152609_AddStripeCheckoutFieldsToOrdine.cs`
- `Migrations/20260505170828_DropLegacyProiezionePrenotazioneTables.Designer.cs`
- `Migrations/20260505170828_DropLegacyProiezionePrenotazioneTables.cs`
- `Migrations/20260509071409_AddAccountSecurityAndExternalLogins.Designer.cs`
- `Migrations/20260509071409_AddAccountSecurityAndExternalLogins.cs`
- `Migrations/20260510101610_AddAccountDeletionFields.Designer.cs`
- `Migrations/20260510101610_AddAccountDeletionFields.cs`
- `Migrations/20260510115820_AddCinemaStaffAssignments.Designer.cs`
- `Migrations/20260510115820_AddCinemaStaffAssignments.cs`

Solo nel tuo progetto, primi 40:
- `.env.docker`
- `.env.example`
- `.gitignore`
- `DTO/AbbonamentiNewsletterDTO.cs`
- `DTO/AuthDTOs.cs`
- `DTO/CategoriaDTOs.cs`
- `DTO/EmailPdfDTO.cs`
- `DTO/NotificaDTO.cs`
- `DTO/PagamentoDTO.cs`
- `DTO/ProiezioneDTO.cs`
- `DTO/TMDBDTO.cs`
- `Data/Migrations/20260312120528_InitialCreate.Designer.cs`
- `Data/Migrations/20260312120528_InitialCreate.cs`
- `Data/Migrations/20260401081248_AddAuthAndCategories.Designer.cs`
- `Data/Migrations/20260401081248_AddAuthAndCategories.cs`
- `Data/Migrations/20260408073125_AddCategorieAndAuth.Designer.cs`
- `Data/Migrations/20260408073125_AddCategorieAndAuth.cs`
- `Data/Migrations/20260409111710_AddCinemaPostiMassimi.Designer.cs`
- `Data/Migrations/20260409111710_AddCinemaPostiMassimi.cs`
- `Data/Migrations/20260414190115_AddMultiSalaAndTickets.Designer.cs`
- `Data/Migrations/20260414190115_AddMultiSalaAndTickets.cs`
- `Data/Migrations/20260414192359_AlignModelSnapshot.Designer.cs`
- `Data/Migrations/20260414192359_AlignModelSnapshot.cs`
- `Data/Migrations/20260416104113_AddExternalAuthProviders.Designer.cs`
- `Data/Migrations/20260416104113_AddExternalAuthProviders.cs`
- `Data/Migrations/20260416112318_AddPaymentMethodMetadata.Designer.cs`
- `Data/Migrations/20260416112318_AddPaymentMethodMetadata.cs`
- `Data/Migrations/20260416113133_AddPreferredPaymentMethod.Designer.cs`
- `Data/Migrations/20260416113133_AddPreferredPaymentMethod.cs`
- `Data/Migrations/20260421101145_AddUserNotifications.Designer.cs`
- `Data/Migrations/20260421101145_AddUserNotifications.cs`
- `Data/Migrations/20260421152712_UpdateRefreshTokensAndRemoveUserRefreshFields.Designer.cs`
- `Data/Migrations/20260421152712_UpdateRefreshTokensAndRemoveUserRefreshFields.cs`
- `Data/Migrations/20260423155828_AddTmdbIdToFilm.Designer.cs`
- `Data/Migrations/20260423155828_AddTmdbIdToFilm.cs`
- `Data/Migrations/20260423161525_ExtendGenereColumn.Designer.cs`
- `Data/Migrations/20260423161525_ExtendGenereColumn.cs`
- `Data/Migrations/20260427070536_FixGenereLength.Designer.cs`
- `Data/Migrations/20260427070536_FixGenereLength.cs`
- `Data/Migrations/20260427070645_UpdateFilmGenereLength.Designer.cs`

## Backend alternativo: prof backend/FilmAPI vs tuo FilmAPI
- File prof: 162; file tuo: 129; percorsi comuni: 23
- File identici: 0; file testo modificati: 23; binari diversi: 0
- Solo prof: 139; solo tuo: 106
- Righe prof: 31602; righe tuo: 22344
- Similarita righe: 4.88%; differenza stimata: 95.12%
- Overlap percorsi: 14.20% del prof, 17.83% del tuo

| File comune piu diverso | Stato | Righe prof | Righe tue | Similarita | Righe add/del |
|---|---:|---:|---:|---:|---:|
| `Endpoints/AuthEndpoints.cs` | modificato | 358 | 89 | 39.82% | 269 |
| `Endpoints/FilmsEndpoints.cs` | modificato | 64 | 209 | 46.89% | 145 |
| `Data/FilmDbContext.cs` | modificato | 445 | 192 | 60.28% | 253 |
| `Program.cs` | modificato | 282 | 122 | 60.40% | 160 |
| `Services/AuthService.cs` | modificato | 456 | 219 | 64.89% | 237 |
| `Endpoints/CinemasEndpoints.cs` | modificato | 51 | 90 | 72.34% | 39 |
| `DTO/FilmDTO.cs` | modificato | 56 | 37 | 79.57% | 19 |
| `Services/CategoriaService.cs` | modificato | 106 | 150 | 82.81% | 44 |
| `DTO/CinemaDTO.cs` | modificato | 33 | 24 | 84.21% | 9 |
| `DTO/RegistaDTO.cs` | modificato | 33 | 24 | 84.21% | 9 |
| `Model/Categoria.cs` | modificato | 16 | 22 | 84.21% | 6 |
| `appsettings.json` | modificato | 13 | 17 | 86.67% | 4 |
| `Model/Cinema.cs` | modificato | 34 | 27 | 88.52% | 7 |
| `Endpoints/CategorieEndpoints.cs` | modificato | 63 | 79 | 88.73% | 16 |
| `Model/RefreshToken.cs` | modificato | 35 | 28 | 88.89% | 7 |
| `Model/Film.cs` | modificato | 44 | 39 | 93.98% | 5 |
| `Model/Regista.cs` | modificato | 24 | 26 | 96.00% | 2 |
| `Endpoints/RegistiEndpoints.cs` | modificato | 92 | 96 | 97.87% | 4 |
| `appsettings.Development.json` | modificato | 9 | 9 | 100.00% | 0 |
| `FilmAPI.csproj` | modificato | 36 | 36 | 100.00% | 0 |
| `FilmAPI.http` | modificato | 7 | 7 | 100.00% | 0 |
| `Model/FilmCategoria.cs` | modificato | 20 | 20 | 100.00% | 0 |
| `Properties/launchSettings.json` | modificato | 15 | 15 | 100.00% | 0 |

Solo nel prof, primi 40:
- `DTO/AccountSecurityDTO.cs`
- `DTO/AuthDTO.cs`
- `DTO/BigliettoDTO.cs`
- `DTO/CategoriaDTO.cs`
- `DTO/CheckoutDTO.cs`
- `DTO/CinemaStaffDTO.cs`
- `DTO/CreditoDTO.cs`
- `DTO/ExternalAuthDTO.cs`
- `DTO/MediaDTO.cs`
- `DTO/OrdineDTO.cs`
- `DTO/ProfiloDTO.cs`
- `DTO/ProgrammazioneDTO.cs`
- `DTO/SalaDTO.cs`
- `DTO/ShowCancellationDTO.cs`
- `DTO/ShowDTO.cs`
- `DTO/UserAdminDTO.cs`
- `DTO/UserDataExportDTO.cs`
- `Data/DataSeeder.cs`
- `Endpoints/AdminUtentiEndpoints.cs`
- `Endpoints/CheckoutEndpoints.cs`
- `Endpoints/CreditoEndpoints.cs`
- `Endpoints/EndpointRouteLocation.cs`
- `Endpoints/MediaEndpoints.cs`
- `Endpoints/PagamentoEndpoints.cs`
- `Endpoints/ProfiloEndpoints.cs`
- `Endpoints/ProgrammazioneEndpoints.cs`
- `Endpoints/SaleEndpoints.cs`
- `Endpoints/ShowCancellationEndpoints.cs`
- `Endpoints/ShowsEndpoints.cs`
- `Endpoints/ValidazioneBigliettiEndpoints.cs`
- `Migrations/20260312120556_InitialCreate.Designer.cs`
- `Migrations/20260312120556_InitialCreate.cs`
- `Migrations/20260406071504_AddCategorieAndAuth.Designer.cs`
- `Migrations/20260406071504_AddCategorieAndAuth.cs`
- `Migrations/20260413200358_AddRefreshTokenDeviceId.Designer.cs`
- `Migrations/20260413200358_AddRefreshTokenDeviceId.cs`
- `Migrations/20260416171534_AddMultisalaTicketing.Designer.cs`
- `Migrations/20260416171534_AddMultisalaTicketing.cs`
- `Migrations/20260419152609_AddStripeCheckoutFieldsToOrdine.Designer.cs`
- `Migrations/20260419152609_AddStripeCheckoutFieldsToOrdine.cs`

Solo nel tuo progetto, primi 40:
- `.env.docker`
- `.env.example`
- `.gitignore`
- `DTO/AuthDTOs.cs`
- `DTO/AuthRequests.cs`
- `DTO/AuthResponses.cs`
- `DTO/CategoriaDTOs.cs`
- `DTO/ProiezioneDTO.cs`
- `Data/Migrations/20260312120528_InitialCreate.Designer.cs`
- `Data/Migrations/20260312120528_InitialCreate.cs`
- `Data/Migrations/20260401081248_AddAuthAndCategories.Designer.cs`
- `Data/Migrations/20260401081248_AddAuthAndCategories.cs`
- `Data/Migrations/20260408073125_AddCategorieAndAuth.Designer.cs`
- `Data/Migrations/20260408073125_AddCategorieAndAuth.cs`
- `Data/Migrations/FilmDbContextModelSnapshot.cs`
- `Dockerfile`
- `Endpoints/AdminEndpoints.cs`
- `Endpoints/ProiezioniEndpoints.cs`
- `Endpoints/UserEndpoints.cs`
- `Endpoints/UsersEndpoints.cs`
- `FilmFrontend/FilmFrontend.csproj`
- `FilmFrontend/Program.cs`
- `FilmFrontend/Properties/launchSettings.json`
- `FilmFrontend/README.md`
- `FilmFrontend/appsettings.Development.json`
- `FilmFrontend/appsettings.json`
- `FilmFrontend/wwwroot/categorie.html`
- `FilmFrontend/wwwroot/cinemas.html`
- `FilmFrontend/wwwroot/components/footer.html`
- `FilmFrontend/wwwroot/components/navbar.html`
- `FilmFrontend/wwwroot/components/sidebar.html`
- `FilmFrontend/wwwroot/css/styles.css`
- `FilmFrontend/wwwroot/films.html`
- `FilmFrontend/wwwroot/home.html`
- `FilmFrontend/wwwroot/index.html`
- `FilmFrontend/wwwroot/js/api-client.js`
- `FilmFrontend/wwwroot/js/auth.js`
- `FilmFrontend/wwwroot/js/pages/profilo.js`
- `FilmFrontend/wwwroot/js/route-guard.js`
- `FilmFrontend/wwwroot/js/template-loader.js`

## Frontend mappato: prof frontend/CineBase.Web vs tuo frontend
- File prof: 84; file tuo: 64; percorsi comuni: 35
- File identici: 0; file testo modificati: 35; binari diversi: 0
- Solo prof: 49; solo tuo: 29
- Righe prof: 22936; righe tuo: 17743
- Similarita righe: 35.70%; differenza stimata: 64.30%
- Overlap percorsi: 41.67% del prof, 54.69% del tuo

| File comune piu diverso | Stato | Righe prof | Righe tue | Similarita | Righe add/del |
|---|---:|---:|---:|---:|---:|
| `wwwroot/js/pages/reimposta-password.js` | modificato | 165 | 30 | 30.77% | 135 |
| `wwwroot/index.html` | modificato | 139 | 527 | 41.74% | 388 |
| `wwwroot/pagamento.html` | modificato | 171 | 647 | 41.81% | 476 |
| `wwwroot/login.html` | modificato | 175 | 628 | 43.59% | 453 |
| `wwwroot/js/pages/recupera-password.js` | modificato | 65 | 19 | 45.24% | 46 |
| `wwwroot/js/template-loader.js` | modificato | 93 | 295 | 47.94% | 202 |
| `wwwroot/js/pages/social-login-complete.js` | modificato | 112 | 36 | 48.65% | 76 |
| `wwwroot/js/utils.js` | modificato | 104 | 297 | 51.87% | 193 |
| `wwwroot/programmazione.html` | modificato | 203 | 563 | 53.00% | 360 |
| `Program.cs` | modificato | 101 | 37 | 53.62% | 64 |
| `wwwroot/js/theme.js` | modificato | 76 | 207 | 53.71% | 131 |
| `wwwroot/scheda-film.html` | modificato | 234 | 621 | 54.74% | 387 |
| `wwwroot/ricarica-credito.html` | modificato | 136 | 354 | 55.51% | 218 |
| `wwwroot/js/pages/utenti.js` | modificato | 1076 | 421 | 56.25% | 655 |
| `wwwroot/privacy.html` | modificato | 276 | 108 | 56.25% | 168 |
| `wwwroot/films.html` | modificato | 310 | 770 | 57.41% | 460 |
| `wwwroot/js/route-guard.js` | modificato | 367 | 170 | 63.31% | 197 |
| `wwwroot/acquista.html` | modificato | 180 | 378 | 64.52% | 198 |
| `wwwroot/my-cinemas.html` | modificato | 138 | 279 | 66.19% | 141 |
| `wwwroot/cinemas.html` | modificato | 230 | 449 | 67.75% | 219 |
| `wwwroot/sale.html` | modificato | 236 | 395 | 74.80% | 159 |
| `wwwroot/js/auth.js` | modificato | 376 | 625 | 75.12% | 249 |
| `wwwroot/utenti.html` | modificato | 288 | 180 | 76.92% | 108 |
| `Properties/launchSettings.json` | modificato | 15 | 24 | 76.92% | 9 |
| `wwwroot/profilo.html` | modificato | 229 | 347 | 79.51% | 118 |

Solo nel prof, primi 40:
- `CineBase.Web.csproj`
- `FrontendAssemblyMarker.cs`
- `edge/README.md`
- `edge/_headers`
- `edge/_redirects`
- `edge/prepare-edge-deploy.mjs`
- `edge/routes-manifest.json`
- `edge/vercel.json`
- `wwwroot/components/footer-landing.html`
- `wwwroot/components/navbar-landing.html`
- `wwwroot/conferma-cancellazione.html`
- `wwwroot/cookie.html`
- `wwwroot/dashboard.html`
- `wwwroot/esito-acquisto.html`
- `wwwroot/js/admin-shell.js`
- `wwwroot/js/api.js`
- `wwwroot/js/cookie-banner.js`
- `wwwroot/js/date-rail.js`
- `wwwroot/js/form-handlers.js`
- `wwwroot/js/geolocation-preferences.js`
- `wwwroot/js/legal-config.js`
- `wwwroot/js/navbar.js`
- `wwwroot/js/pages/acquista.js`
- `wwwroot/js/pages/categorie.js`
- `wwwroot/js/pages/cinemas.js`
- `wwwroot/js/pages/conferma-cancellazione.js`
- `wwwroot/js/pages/esito-acquisto.js`
- `wwwroot/js/pages/films.js`
- `wwwroot/js/pages/home.js`
- `wwwroot/js/pages/login.js`
- `wwwroot/js/pages/my-cinemas.js`
- `wwwroot/js/pages/pagamento.js`
- `wwwroot/js/pages/programmazione.js`
- `wwwroot/js/pages/refund-review.js`
- `wwwroot/js/pages/registi.js`
- `wwwroot/js/pages/registrazione.js`
- `wwwroot/js/pages/ricarica-credito.js`
- `wwwroot/js/pages/sale.js`
- `wwwroot/js/pages/scheda-film.js`
- `wwwroot/js/pages/shows.js`

Solo nel tuo progetto, primi 40:
- `.gitignore`
- `FilmFrontend.csproj`
- `README.md`
- `wwwroot/abbonamenti.html`
- `wwwroot/assets/icons/favicon-dark-32.png`
- `wwwroot/assets/icons/favicon-dark.ico`
- `wwwroot/assets/icons/favicon-light-32.png`
- `wwwroot/assets/icons/favicon-light.ico`
- `wwwroot/assets/icons/logo-dark-96.png`
- `wwwroot/assets/icons/logo-light-96.png`
- `wwwroot/check-in.html`
- `wwwroot/components/footer.html`
- `wwwroot/components/navbar.html`
- `wwwroot/components/sidebar.html`
- `wwwroot/condizioni.html`
- `wwwroot/conferma-acquisto.html`
- `wwwroot/home.html`
- `wwwroot/js/api-client.js`
- `wwwroot/js/api-config.js`
- `wwwroot/js/usability-tools.js`
- `wwwroot/newsletter-admin.html`
- `wwwroot/proiezioni-pubblico.html`
- `wwwroot/proiezioni.html`
- `wwwroot/register.html`
- `wwwroot/ricarica-credito-utente.html`
- `wwwroot/supporto.html`
- `wwwroot/termini.html`
- `wwwroot/user-biglietti.html`
- `wwwroot/validazione.html`

## Tests backend: prof tests/backend vs tuo backend/tests
- File prof: 29; file tuo: 11; percorsi comuni: 5
- File identici: 0; file testo modificati: 5; binari diversi: 0
- Solo prof: 24; solo tuo: 6
- Righe prof: 12960; righe tuo: 1836
- Similarita righe: 8.31%; differenza stimata: 91.69%
- Overlap percorsi: 17.24% del prof, 45.45% del tuo

| File comune piu diverso | Stato | Righe prof | Righe tue | Similarita | Righe add/del |
|---|---:|---:|---:|---:|---:|
| `Integration/CustomWebApplicationFactory.cs` | modificato | 817 | 171 | 34.62% | 646 |
| `Unit/FilmServiceTests.cs` | modificato | 137 | 214 | 78.06% | 77 |
| `Unit/RegistaServiceTests.cs` | modificato | 179 | 215 | 90.86% | 36 |
| `Unit/CinemaServiceTests.cs` | modificato | 94 | 112 | 91.26% | 18 |
| `FilmAPI.Tests.csproj` | modificato | 35 | 34 | 98.55% | 1 |

Solo nel prof, primi 40:
- `Integration/AccountDeletionIntegrationTests.cs`
- `Integration/AccountTokenIntegrationTests.cs`
- `Integration/AdminUserSecurityIntegrationTests.cs`
- `Integration/ApiCanonicalNamespaceIntegrationTests.cs`
- `Integration/ApiIntegrationTests.cs`
- `Integration/AuthIntegrationTests.cs`
- `Integration/CategoriaIntegrationTests.cs`
- `Integration/CheckoutHostedIntegrationTests.cs`
- `Integration/CheckoutIntegrationTests.cs`
- `Integration/CinemaStaffAuthorizationIntegrationTests.cs`
- `Integration/CorsConfigurationIntegrationTests.cs`
- `Integration/ExternalAuthIntegrationTests.cs`
- `Integration/FrontendHostedSmokeTests.cs`
- `Integration/LegalAcceptanceIntegrationTests.cs`
- `Integration/PagamentoCreditoIntegrationTests.cs`
- `Integration/PasswordCredentialsIntegrationTests.cs`
- `Integration/ProgrammazioneIntegrationTests.cs`
- `Integration/RbacIntegrationTests.cs`
- `Integration/SalaIntegrationTests.cs`
- `Integration/ShowCancellationIntegrationTests.cs`
- `Integration/ShowIntegrationTests.cs`
- `Integration/TicketIntegrationTests.cs`
- `Integration/ValidazioneTicketIntegrationTests.cs`
- `Unit/BigliettoServiceTests.cs`

Solo nel tuo progetto, primi 40:
- `Integration/AuthSecurityIntegrationTests.cs`
- `Integration/CinemaEndpointsTests.cs`
- `Integration/FilmEndpointsTests.cs`
- `Integration/ProiezioneEndpointsTests.cs`
- `Integration/RegistaEndpointsTests.cs`
- `Unit/ProiezioneServiceTests.cs`

## Controllo root quasi completo
- File prof: 373; file tuo: 607; percorsi comuni: 2; solo prof: 371; solo tuo: 605
- Similarita righe: 0.01%; differenza stimata: 99.99%
