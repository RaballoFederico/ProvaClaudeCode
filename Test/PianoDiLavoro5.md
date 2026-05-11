# Piano di Lavoro - Iterazione 5

Autore: OpenCode

Documento operativo per l'evoluzione dell'autenticazione CineBase dopo il completamento dell'Iterazione 4.1.

Branch target suggerito: `dev_iteration_5`

---

## Stato Avanzamento Fasi

| Fase | Stato | Data | Note |
| --- | --- | --- | --- |
| FASE 0 - Preflight auth e mappa superfici di sicurezza | **Pianificata** | - | Inventario codice auth, RBAC, frontend login/profilo/admin, env e test esistenti |
| FASE 1 - Modello dati credenziali, provider esterni e audit | **Pianificata** | - | Migration per password nullable controllata, external login, token azione, state OAuth, audit e auth version |
| FASE 2 - Infrastruttura email account e token temporanei | **Pianificata** | - | Token hashati single-use, email recupero password/invito/setup password, rate limit e template |
| FASE 3 - Backend cambio password e recupero password | **Pianificata** | - | `change-password`, `forgot-password`, `reset-password`, revoca sessioni e test automatici |
| FASE 4 - Backend social login Google/Microsoft per utenti `User` | **Pianificata** | - | OIDC Authorization Code + PKCE; Google aperto a email verificate, Microsoft aperto ad account personali e work/school; exchange code e blocco ruoli elevati |
| FASE 5 - Backend admin utenti: creazione, invito, elevazione e hardening ruoli | **Pianificata** | - | Admin-only, promozione controllata, blocco social-only elevati, audit e invalidazione token |
| FASE 6 - Frontend login, recupero password e sicurezza profilo | **Pianificata** | - | Bottoni social, pagine recupero/reset, sezione cambio password in profilo e redirect sicuri |
| FASE 7 - Frontend admin gestione utenti elevati | **Pianificata** | - | Nuova pagina `utenti.html`, creazione Power/Admin, elevazione utenti esistenti e indicatori sicurezza |
| FASE 8 - Test automatici estesi auth/security | **Pianificata** | - | Integration test per password, social, admin utenti, RBAC, audit, token replay e validazione Microsoft multi-audience |
| FASE 9 - Smoke test runtime e verifica manuale sicurezza | **Pianificata** | - | Verifica browser completa Admin/Power/User/anonimo, provider reali dove possibile, email reale opzionale |
| FASE 10 - Documentazione finale | **Pianificata** | - | Aggiornamento `status.md`, `changelog.md`, `.env.example`, tutorial social login e checklist piano |

---

## 1) Obiettivo Iterazione

L'Iterazione 5 consolida la gestione dell'identità e delle credenziali di CineBase introducendo:

- accesso social per utenti normali `User` tramite Google e Microsoft, con Google aperto agli account Google con email verificata e Microsoft aperto sia ad account personali Microsoft sia ad account work/school, incluso `issgreppi.it`;
- gestione completa delle credenziali locali per tutti gli utenti, inclusi `PowerUser` e `Admin`;
- cambio password autenticato;
- recupero password tramite email con link e token temporaneo single-use;
- strumenti amministrativi per creare o promuovere `PowerUser` e `Admin` senza consentire autoregistrazioni privilegiate;
- hardening di sessioni, token, redirect, audit e cambio ruolo.

L'obiettivo non è sostituire l'RBAC esistente, ma renderlo più robusto nel momento in cui vengono aggiunti provider esterni e flussi di recupero credenziali.

## 1.1 Stato reale di partenza

Da `docs/project/status.md` e `docs/project/changelog.md`:

- Iterazione 4.1 completata al 100%.
- Backend stabile con `198/198 PASS`, `0 FAIL`, `0 SKIP`.
- Dominio legacy `Proiezione`/`Prenotazione` rimosso da runtime backend, frontend, seeder, test, snapshot EF e DB locale.
- Autenticazione attuale basata su JWT access token + refresh token opaco, con rotazione refresh token e `DeviceId`.
- Ruoli attuali: `User = 0`, `PowerUser = 1`, `Admin = 2`.
- Registrazione pubblica locale crea sempre `User`.
- Login locale usa email + password BCrypt.
- `PasswordHash` è attualmente obbligatoria in `User`.
- `RefreshToken` è persistito e indicizzato per token, utente e device.
- Endpoint auth esistenti:
  - `POST /auth/register`
  - `POST /auth/login`
  - `POST /auth/refresh`
  - `POST /auth/logout`
  - `GET /auth/me`
- Endpoint admin utenti esistenti:
  - `GET /admin/utenti`
  - `PUT /admin/utenti/{id}/ruolo`
- Frontend esistente:
  - `login.html` + `js/pages/login.js`
  - `registrazione.html` + `js/pages/registrazione.js`
  - `profilo.html` + `js/pages/profilo.js`
  - `auth.js`, `api.js`, `route-guard.js`, `admin-shell.js`
- Non esiste ancora una pagina frontend dedicata alla gestione utenti admin.
- `route-guard.js` valida già `?redirect=` in alcuni casi, ma `login.js` e `Auth.redirectAfterLogin()` devono essere ricontrollati per evitare qualunque open redirect residuo.
- `IEmailService` esiste per i ticket, ma è specializzato su `SendOrderTicketsAsync`; per email account conviene introdurre un servizio dedicato o estendere in modo pulito l'infrastruttura SMTP.

## 1.2 Scope dell'iterazione

### In scope

- Social login Google per utenti normali `User` con qualunque account Google per cui Google restituisca un ID token valido e il claim `email_verified = true`, senza alcun vincolo sul dominio email (`gmail.com`, `issgreppi.it` o altri domini).
- Social login Microsoft identity platform per utenti normali `User`, consentendo account personali Microsoft (`outlook.com`, `hotmail.com`, `live.com`, `live.it`, ecc.) e account work/school Microsoft Entra ID, incluso `issgreppi.it`.
- Validazione backend dei token OIDC e dei claim provider-specifici: per Google email verificata; per Microsoft issuer/audience/tenant/identificatore stabile e disponibilità di un indirizzo email utilizzabile come contatto applicativo.
- Collegamento sicuro di un provider esterno a un account `User` esistente solo dopo validazione provider-specifica dell'identità e dell'indirizzo usato per il linking.
- Autocreazione account `User` da social login se l'indirizzo applicativo non è ancora registrato e il provider soddisfa le proprie regole: email verificata per Google, identità stabile più email-like disponibile per Microsoft.
- Blocco del social login per account `PowerUser` e `Admin`.
- Cambio password per utenti con credenziali locali.
- Recupero password via email con token temporaneo, hashato e single-use.
- Possibilità di impostare una password locale per account social-only tramite token email.
- Revoca refresh token e invalidazione token applicativi su reset password, cambio password e cambio ruolo.
- Pagina admin `utenti.html` per creazione/invito di `PowerUser`/`Admin` e promozione controllata di utenti esistenti.
- Audit delle operazioni sensibili su credenziali e ruoli.
- Test automatici backend estesi.
- Smoke test frontend e manual verification.
- Aggiornamento documentazione e `.env.example`.

### Out of scope

- Single Sign-On per `PowerUser` e `Admin` tramite provider esterno.
- Assegnazione automatica dei ruoli applicativi da gruppi Google Workspace o Microsoft Entra ID.
- MFA/TOTP o passkey/WebAuthn.
- Gestione completa del ciclo di vita HR/scuola, disattivazione automatica account o sincronizzazione directory.
- Invio SMS o recupero password via telefono.
- Migrazione a cookie HttpOnly per l'intero modello auth frontend.
- Rework grafico esteso delle pagine auth oltre agli elementi necessari.
- Uso di Microsoft Graph o permessi Microsoft oltre i soli scope OIDC minimi (`openid`, `profile`, `email`) per leggere dati organizzativi, gruppi, ruoli directory o profili estesi.

## 1.3 Vincoli di sicurezza vincolanti

1. Il social login non deve mai assegnare `PowerUser` o `Admin`.
2. Un account `PowerUser` o `Admin` non deve poter entrare tramite Google/Microsoft: deve usare credenziali locali.
3. Un utente registrato autonomamente, anche via social, nasce sempre `User`.
4. La promozione a `PowerUser` o `Admin` è solo `AdminOnly`.
5. Un account social-only non può essere promosso a ruolo elevato finché non ha impostato una password locale.
6. Dopo cambio ruolo, reset password o cambio password, i refresh token devono essere revocati e i JWT esistenti devono diventare non riutilizzabili entro un limite controllato.
7. I token di reset/invito/setup password non devono essere salvati in chiaro nel database.
8. Il flusso `forgot-password` deve restituire sempre una risposta generica per evitare enumerazione email.
9. Tutti i redirect da login, reset, social callback e route guard devono accettare solo path relativi interni.
10. Provider token e access token esterni non devono essere persistiti, salvo esplicita necessità futura.
11. Per Microsoft il backend deve validare firma, `aud`, `iss`, `exp`, `nonce`, `tid` e identificatore stabile (`oid` quando disponibile, altrimenti `sub`) usando metadata OIDC attendibili; l'email Microsoft non deve essere usata come identificatore primario perché `email` e `preferred_username` sono claim mutabili e non sempre presenti.
12. Ogni operazione admin su ruoli/credenziali deve essere auditata.

## 1.4 Nomenclatura canonica

| Concetto | Nome consigliato |
| --- | --- |
| Provider esterno | `ExternalLoginProvider` (`Google`, `Microsoft`) |
| Login esterno collegato | `UserExternalLogin` |
| Token recupero/setup/invito | `AccountActionToken` |
| Stato OAuth temporaneo | `ExternalAuthState` |
| Codice scambio social -> app token | `ExternalAuthExchangeCode` |
| Audit sicurezza account | `UserSecurityAuditLog` |
| Pagina richiesta recupero password | `recupera-password.html` |
| Pagina reset password | `reimposta-password.html` |
| Pagina completamento social login | `social-login-complete.html` |
| Pagina admin gestione utenti | `utenti.html` |

## 1.5 Riferimenti operativi

Per la parte social login, usare come riferimento concettuale il materiale del progetto EducationalGames indicato dal committente:

- `https://github.com/GreppiDev/Info5IA2526WebDev/blob/main/asp.net/api-samples/minimal-api/Esami/2023/EducationalGames/indicazioni-sviluppo-progetto.md#autenticazione-basata-su-microsoft-e-google---minimal-api`
- `https://github.com/GreppiDev/Info5IA2526WebDev/tree/main/asp.net/api-samples/minimal-api/Esami/2023/EducationalGames/EducationalGames`

Adattamento per CineBase:

- EducationalGames usa autenticazione esterna dentro un'app unificata con cookie; CineBase mantiene l'architettura attuale JWT + refresh token opaco.
- Rimane valido il principio di separare provider Google e Microsoft, gestire callback/failure provider-specifici e creare/collegare l'utente locale solo dopo validazione server-side.
- Per CineBase Google è un provider pubblico per utenti `User`: sono ammessi account Google con qualunque dominio email, purché Google certifichi quell'indirizzo con `email_verified = true`. Microsoft è anch'esso un provider pubblico per utenti `User`: sono ammessi account personali Microsoft e account work/school Microsoft Entra ID, incluso `issgreppi.it`, purché il token OIDC sia valido e produca un'identità stabile collegabile all'account applicativo.

## 1.6 Decisione aggiornata su Microsoft identity platform

La restrizione precedente a `issgreppi.it` non è più considerata un requisito funzionale. Dopo verifica della documentazione ufficiale Microsoft, l'Iterazione 5 deve supportare Microsoft come provider generalista, in modo analogo a Google per gli utenti normali, ma con regole di validazione diverse.

Riferimenti Microsoft rilevanti:

- `https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app`
- `https://learn.microsoft.com/en-us/entra/identity-platform/single-and-multi-tenant-apps`
- `https://learn.microsoft.com/en-us/entra/identity-platform/v2-protocols-oidc`
- `https://learn.microsoft.com/en-us/entra/identity-platform/id-token-claims-reference`
- `https://learn.microsoft.com/en-us/entra/identity-platform/publisher-verification-overview`
- `https://learn.microsoft.com/en-us/entra/identity-platform/scopes-oidc`

Conclusione tecnica:

- Microsoft identity platform consente registrazioni applicative per account personali Microsoft e account work/school configurando l'audience `Accounts in any organizational directory and personal Microsoft accounts` (`AzureADandPersonalMicrosoftAccount`).
- L'authority OIDC consigliata per questo scenario è `https://login.microsoftonline.com/common/v2.0`, perché accetta sia account personali sia account work/school.
- Per un login base con scope OIDC minimi (`openid`, `profile`, `email`) non emerge un obbligo generale di publisher verification. La publisher verification resta consigliata per fiducia e adozione, ma diventa più rilevante per app multitenant che chiedono permessi oltre il profilo base o per tenant con policy di consenso restrittive.
- Alcuni tenant Microsoft Entra ID possono comunque impedire il consenso utente o richiedere consenso amministrativo per policy interne. Il sistema deve gestire questi errori in modo chiaro, senza considerarli difetti del flusso applicativo.

Specifiche vincolanti per Microsoft in CineBase:

- La app registration Microsoft deve supportare account personali e account work/school.
- Il backend deve usare Authorization Code Flow + PKCE e validare server-side l'ID token.
- Il backend deve validare firma, `aud`, `iss`, `exp`, `nonce` e coerenza `tid`/issuer secondo metadata OIDC Microsoft.
- Il backend deve conservare come identificatore provider primario `Provider = Microsoft` + `ProviderTenantId = tid` + `ProviderUserId = oid` quando `oid` è presente; se `oid` manca, usare `sub` documentando che è pairwise per client/app.
- Per account personali Microsoft, il `tid` atteso è il tenant consumer Microsoft `9188040d-6c67-4c5b-b112-36a304b66dad`; per account work/school il `tid` identifica il tenant dell'organizzazione.
- `email` e `preferred_username` possono essere usati per mostrare l'indirizzo all'utente e per valorizzare l'email applicativa iniziale, ma non devono essere usati come identificatore stabile o come unica prova di autorizzazione.
- Se Microsoft non restituisce un claim email-like utilizzabile (`email` oppure `preferred_username` in formato email), l'autocreazione deve essere rifiutata con messaggio chiaro o rimandata a un flusso esplicito futuro di raccolta/verifica email.
- Il linking automatico a un account locale esistente con la stessa email è ammesso solo per account `User`, non disabilitati e non ambigui; deve essere auditato. Se si vuole un livello di sicurezza superiore, il piano consente di sostituire il linking automatico Microsoft con una conferma email locale, ma questa conferma non è obbligatoria nell'Iterazione 5 salvo decisione esplicita.
- Il social login Microsoft non deve mai autenticare account applicativi `PowerUser` o `Admin`.

Parti da controllare durante l'implementazione:

- configurazione `signInAudience` dell'app Microsoft su `AzureADandPersonalMicrosoftAccount`;
- uso dell'authority `common` e non di un tenant specifico;
- validazione issuer multi-tenant con metadata tenant-independent;
- gestione della chiave di firma con `kid` e key rollover;
- mapping stabile `tid + oid/sub` nella tabella `UserExternalLogin`;
- comportamento quando l'email Microsoft non è presente;
- comportamento quando un tenant work/school blocca il consenso utente;
- assenza di qualunque filtro hard-coded su `issgreppi.it` nel provider Microsoft, salvo eventuali filtri opzionali configurati esplicitamente in futuro;
- test con account personale Microsoft (`outlook.com`/`hotmail.com`/`live.*`) e account work/school (`issgreppi.it` o altro tenant disponibile);
- messaggi frontend per errori Microsoft centrati sulla causa reale: token non valido, consenso negato, policy tenant, email assente o account elevato.

---

## 2) Requisiti Funzionali Consolidati

## 2.1 Social login per utenti normali

Il login social deve supportare:

- Google OpenID Connect.
- Microsoft OpenID Connect per account personali Microsoft e account work/school Microsoft Entra ID.
- Creazione automatica di un account `User` quando il login avviene con Google, l'ID token Google è valido e contiene una email con `email_verified = true`, indipendentemente dal dominio dell'indirizzo.
- Creazione automatica di un account `User` quando il login avviene con Microsoft, l'ID token Microsoft è valido, contiene un'identità stabile (`tid` + `oid`/`sub`) e fornisce un indirizzo email-like utilizzabile come email applicativa.
- Collegamento al profilo esistente se l'indirizzo restituito dal provider corrisponde a un account `User` e soddisfa le regole provider-specifiche: email verificata per Google, email-like disponibile e identità stabile validata per Microsoft.
- Collegamento al profilo esistente da Microsoft solo se l'account applicativo è `User`, non disabilitato, non ambiguo e la policy di linking basata su email è accettata per l'Iterazione 5.
- Rifiuto esplicito del login Microsoft se il token non è valido, l'issuer non è coerente con il tenant, manca un identificatore stabile, manca un indirizzo email-like necessario alla creazione/linking, il consenso viene negato o il tenant esterno applica policy che impediscono il flusso.
- Rifiuto esplicito se l'account applicativo esistente è `PowerUser` o `Admin`.
- Emissione di JWT e refresh token applicativi esattamente come il login locale, passando da exchange code one-time.
- Conservazione minima dei dati provider: provider, subject/oid, tenant id quando disponibile, email usata nel login/linking, timestamp collegamento e ultimo accesso.

Regole provider:

- Google: validare `iss`, `aud`, firma, `exp`, `email` ed `email_verified == true`; non applicare vincoli su `hd` o sul dominio email. In pratica sono ammessi account Google con `gmail.com`, `issgreppi.it` o altri domini, purché Google dichiari l'email verificata. Se `hd` è presente, può essere registrato a scopo diagnostico ma non deve essere requisito di accesso.
- Microsoft: validare `iss`, `aud`, firma, `exp`, `nonce`, `tid`, `oid`/`sub`, `preferred_username` o `email`; usare l'authority `common` per accettare account personali Microsoft e account work/school. Non imporre vincoli di dominio o tenant in modo predefinito. Usare `tid + oid` o `tid + sub` come identità stabile del provider; usare `email`/`preferred_username` solo come email applicativa e dato di contatto, non come identificatore primario.

Nota Microsoft:

- Il provider Microsoft deve essere configurato come multi-audience (`AzureADandPersonalMicrosoftAccount`). La publisher verification non è un prerequisito generale per un login OIDC base con scope minimi, ma resta consigliata per aumentare fiducia e ridurre attriti di consenso in tenant esterni. I tenant work/school possono applicare policy locali che richiedono admin consent o bloccano l'app: il frontend deve mostrare un errore comprensibile e il backend deve auditare il rifiuto.

## 2.2 Credenziali locali

Tutti gli utenti con credenziali locali devono poter:

- modificare la propria password inserendo la password attuale;
- richiedere il recupero password da form pubblico;
- reimpostare password con token temporaneo ricevuto via email;
- essere disconnessi dalle sessioni esistenti dopo un reset o un cambio password.

Gli utenti social-only devono poter impostare una password locale tramite link email. Questo serve anche per rendere promuovibile un utente social-only a `PowerUser` o `Admin`.

## 2.3 Recupero password

Flusso richiesto:

1. L'utente clicca "Ho dimenticato la password" dal form login.
2. Viene aperta `recupera-password.html`.
3. L'utente inserisce l'email.
4. Il backend risponde sempre con messaggio generico.
5. Se l'email è registrata, il backend crea un token temporaneo specifico per utente e scopo.
6. Il token viene salvato solo come hash.
7. Viene inviata una email con link a `reimposta-password.html?token=...`.
8. Il token consente una sola reimpostazione entro TTL configurato.
9. Alla reimpostazione riuscita vengono revocati refresh token e invalidati i JWT precedenti.

TTL consigliati:

- reset password utente normale: 30 minuti;
- setup password per account social-only: 60 minuti;
- invito admin/power: 24 ore;
- exchange code social: 2 minuti;
- state OAuth: 10 minuti.

## 2.4 Creazione e promozione utenti elevati

Nessun utente può registrarsi autonomamente come `PowerUser` o `Admin`.

Flussi ammessi:

- `Admin` crea un nuovo account `PowerUser` o `Admin` tramite invito email, senza mostrare o inviare password in chiaro.
- `Admin` promuove un utente `User` già registrato, se quell'utente ha credenziali locali attive.
- `Admin` può inviare a un utente social-only un link per impostare la password locale; la promozione resta bloccata finché la password non è stata impostata.

Regole di sicurezza:

- mantenere il vincolo esistente che impedisce la degradazione dell'ultimo admin;
- impedire che un admin degradi sé stesso se è l'ultimo admin;
- revocare sessioni e incrementare `AuthVersion` al cambio ruolo;
- impedire login social a utenti già elevati;
- registrare audit su creazione, invito, promozione, degradazione e reset password admin-triggered.

## 2.5 Frontend utente

Le pagine auth devono evolvere così:

- `login.html`:
  - login locale esistente;
  - link "Ho dimenticato la password";
  - bottoni "Continua con Google" e "Continua con Microsoft";
  - messaggi chiari per consenso Microsoft negato, policy tenant, email Microsoft assente/non utilizzabile o account elevato che deve usare password locale.
- `registrazione.html`:
  - resta registrazione locale solo `User`;
  - può offrire gli stessi bottoni social, specificando che Google è aperto agli account Google verificati e Microsoft è aperto ad account personali e account work/school.
- `recupera-password.html`:
  - form email;
  - messaggio generico dopo submit.
- `reimposta-password.html`:
  - token da query string;
  - nuova password + conferma;
  - redirect a login al successo.
- `social-login-complete.html`:
  - scambia il codice temporaneo backend con token applicativi;
  - salva token usando `Auth.saveTokens` e `Auth.saveUser`;
  - redirige solo a path relativo interno.
- `profilo.html`:
  - sezione "Sicurezza account";
  - cambio password per utenti con password locale;
  - pulsante "Imposta password" per social-only, con invio link email.

## 2.6 Frontend admin

Nuova pagina `utenti.html` protetta da `AdminOnly`:

- tabella utenti con ricerca email/nome e filtro ruolo;
- colonne: email, nome, ruolo, provider collegati, password locale presente, data registrazione, ultimo login;
- creazione/invito nuovo `PowerUser` o `Admin`;
- promozione/degradazione ruolo con conferma;
- blocco visivo per utenti social-only non promuovibili;
- azione "Invia link imposta password";
- messaggi chiari per ultimo admin, account elevato, errori di consenso/policy Microsoft e sessioni revocate.

---

## 3) Decisioni Architetturali

## 3.1 Social login: flusso backend-mediated con exchange code

Scelta raccomandata: Authorization Code Flow + PKCE gestito dal backend, con redirect finale al frontend tramite exchange code temporaneo.

Motivazione:

- il frontend attuale è statico e salva i token applicativi in `localStorage`;
- non bisogna mettere access token applicativi direttamente nella query string;
- il backend deve essere l'unico punto che valida provider, email verificata Google, issuer/tenant/subject Microsoft e linking account;
- il pattern si integra con l'attuale `AuthResponseDTO` e refresh token applicativo.

Flusso:

1. Frontend apre `GET /auth/external/google/start?redirect=/profilo.html` o `GET /auth/external/microsoft/start?...`.
2. Backend valida il redirect relativo.
3. Backend crea `ExternalAuthState` con state hash, provider, redirect, nonce, code verifier PKCE e scadenza.
4. Backend reindirizza al provider.
5. Provider richiama `/auth/external/{provider}/callback`.
6. Backend valida state, scambia code e valida ID token secondo le regole del provider: Google richiede email verificata, Microsoft richiede issuer/audience/tenant/identificatore stabile coerenti e un indirizzo email-like se serve creare o collegare l'utente locale.
7. Backend crea/collega l'utente `User` oppure rifiuta.
8. Backend genera `ExternalAuthExchangeCode` one-time e redirect a `FRONTEND_BASE_URL/social-login-complete.html?code=...`.
9. Frontend chiama `POST /auth/external/exchange` con il code.
10. Backend restituisce `AuthResponseDTO` con JWT e refresh token applicativi.

## 3.2 Perché non usare token social direttamente nel frontend

Non usare Google/Microsoft SDK lato frontend come source of truth auth applicativa.

Motivi:

- il provider token non contiene i ruoli applicativi CineBase;
- le regole provider devono essere verificate lato backend: email verificata per Google, issuer/tenant/subject coerenti per Microsoft;
- il linking con account esistenti deve consultare il DB;
- bisogna impedire che provider esterni autentichino account elevati;
- il ciclo di refresh token applicativo è già custom e deve restare coerente.

## 3.3 Ruoli elevati e account social

Decisione vincolante:

- `User` può autenticarsi con password locale, Google o Microsoft; Google è consentito su qualunque dominio email se Google restituisce un'identità valida con `email_verified = true`, mentre Microsoft è consentito per account personali e work/school se il token OIDC è valido e contiene un'identità stabile collegabile.
- `PowerUser` e `Admin` possono autenticarsi solo con password locale.
- Se un account `User` social viene promosso, i provider collegati restano in storico ma non sono più utilizzabili per login finché il ruolo è elevato.
- La promozione incrementa `AuthVersion` e revoca refresh token.

Ragione:

- riduce il rischio che una compromissione o configurazione errata del provider esterno apra accesso amministrativo;
- mantiene controllo locale sulle credenziali privilegiate;
- evita ambiguità tra ruoli applicativi e identità esterne.

## 3.4 Opzioni valutate per creare `PowerUser` e `Admin`

| Opzione | Valutazione | Decisione |
| --- | --- | --- |
| Registrazione pubblica con scelta ruolo | Rischio critico di escalation, anche se nascosta nel frontend | **Vietata** |
| Admin crea utente con password temporanea mostrata a video | Funziona ma espone segreti e genera abitudini insicure | **Non consigliata** |
| Admin crea invito email e utente imposta password | Sicura, auditabile, nessuna password in chiaro | **Supportata e consigliata** |
| Admin promuove utente esistente con password locale | Utile per utenti già registrati, sicura se auditata | **Supportata** |
| Admin promuove utente social-only senza password locale | Rischio alto: account elevato dipendente solo da social login | **Vietata** |
| Ruoli automatici da gruppi Google/Microsoft | Potente ma richiede governance directory e test specifici | **Out of scope** |

## 3.5 Password e social-only

`PasswordHash` deve diventare nullable o semanticamente opzionale, perché un account creato via social può non avere password locale.

Regole:

- `PasswordHash == null` significa nessuna credenziale locale attiva.
- `LocalCredentialsEnabled == true` solo se esiste un hash valido.
- Login locale rifiuta account senza password con messaggio generico non enumerativo sul backend e messaggio UX utile sul frontend.
- `PowerUser` e `Admin` richiedono sempre `LocalCredentialsEnabled == true`.

## 3.6 Invalidazione sessioni e token

L'attuale JWT contiene il ruolo nel token. Se un ruolo cambia, un access token già emesso potrebbe restare valido fino alla scadenza. In questa iterazione va introdotto un meccanismo di invalidazione.

Soluzione consigliata:

- aggiungere `AuthVersion int` o `SecurityStamp string` su `User`;
- includere il valore nel JWT come claim, ad esempio `auth_version`;
- incrementare `AuthVersion` su cambio password, reset password, setup password, cambio ruolo, disabilitazione account o cambio provider critico;
- in `JwtBearerEvents.OnTokenValidated`, validare che il claim corrisponda al valore DB;
- usare cache breve in memoria solo se serve contenere il costo DB, con TTL massimo 60 secondi;
- revocare comunque tutti i refresh token dell'utente per forzare nuovo login.

Pass condition: un token emesso prima di una promozione/degradazione non deve poter accedere a endpoint con ruolo non più coerente oltre il TTL di cache dichiarato.

## 3.7 Token temporanei

I token per reset password, invito admin e setup password devono essere:

- generati con `RandomNumberGenerator.GetBytes(32+)`;
- codificati URL-safe;
- salvati solo come hash SHA-256 o HMAC-SHA256;
- legati a `UserId`, `Purpose`, scadenza, `CreatedAtUtc`, `UsedAtUtc` e `CreatedByUserId` quando applicabile;
- invalidati dopo uso;
- invalidati se viene creato un nuovo token dello stesso scopo per lo stesso utente;
- mai loggati in chiaro.

## 3.8 Email account

Usare la configurazione SMTP già introdotta nell'Iterazione 4, ma non riusare il servizio ticket in modo improprio.

Scelta consigliata:

- creare `IAccountEmailService` / `AccountEmailService`;
- riusare internamente le impostazioni `SMTP_*`;
- mantenere `IEmailService` per i biglietti finché non viene estratta una base comune;
- nei test sostituire `IAccountEmailService` con fake dedicato.

Email minime:

- reset password;
- setup password account social-only;
- invito `PowerUser`/`Admin`;
- notifica cambio password riuscito;
- notifica cambio ruolo, facoltativa ma consigliata.

---

## 4) Design Tecnico - Modello Dati

## 4.1 Modifiche a `User`

Estendere `backend/FilmAPI/Model/User.cs`:

```text
User(
  ...campi esistenti,
  PasswordHash string? nullable,
  NormalizedEmail string required unique,
  LocalCredentialsEnabled bool required default true,
  EmailVerifiedAtUtc datetime?,
  PasswordChangedAtUtc datetime?,
  MustChangePassword bool required default false,
  AuthVersion int required default 0,
  LastLoginAtUtc datetime?,
  LastLoginProvider string? max 30,
  IsDisabled bool required default false
)
```

Note:

- `NormalizedEmail` serve a rendere esplicita l'unicità case-insensitive.
- `PasswordHash` diventa nullable per account social-only.
- `IsDisabled` è opzionale ma consigliato perché la gestione utenti admin diventa più completa; se introdotto, tutti i login devono verificarlo.
- Gli utenti esistenti avranno `LocalCredentialsEnabled = true`, `AuthVersion = 0`, `EmailVerifiedAtUtc = null` salvo scelta di considerarli verificati per migrazione dev.

## 4.2 `UserExternalLogin`

Nuova entità:

```text
UserExternalLogin(
  Id int PK,
  UserId int FK,
  Provider ExternalLoginProvider required,
  ProviderUserId string required max 255,
  ProviderTenantId string? max 255,
  EmailAtLogin string required max 255,
  LinkedAtUtc datetime required,
  LastLoginAtUtc datetime?,
  RevokedAtUtc datetime?
)
UNIQUE(Provider, ProviderUserId)
INDEX(UserId, Provider)
INDEX(EmailAtLogin)
```

Enum:

```text
ExternalLoginProvider
- Google = 0
- Microsoft = 1
```

## 4.3 `AccountActionToken`

Nuova entità:

```text
AccountActionToken(
  Id int PK,
  UserId int FK,
  Purpose AccountActionTokenPurpose required,
  TokenHash string required unique max 128,
  ExpiresAtUtc datetime required,
  CreatedAtUtc datetime required,
  UsedAtUtc datetime?,
  RevokedAtUtc datetime?,
  CreatedByUserId int? FK,
  RequestIp string? max 64,
  UserAgent string? max 512
)
INDEX(UserId, Purpose, ExpiresAtUtc)
```

Enum:

```text
AccountActionTokenPurpose
- PasswordReset = 0
- SetPassword = 1
- AdminInvite = 2
```

## 4.4 `ExternalAuthState`

Nuova entità temporanea:

```text
ExternalAuthState(
  Id int PK,
  Provider ExternalLoginProvider required,
  StateHash string required unique max 128,
  CodeVerifier string required max 256,
  Nonce string required max 128,
  RedirectPath string required max 512,
  CreatedAtUtc datetime required,
  ExpiresAtUtc datetime required,
  ConsumedAtUtc datetime?,
  RequestIp string? max 64,
  UserAgent string? max 512
)
```

## 4.5 `ExternalAuthExchangeCode`

Nuova entità temporanea:

```text
ExternalAuthExchangeCode(
  Id int PK,
  UserId int FK,
  CodeHash string required unique max 128,
  RedirectPath string required max 512,
  CreatedAtUtc datetime required,
  ExpiresAtUtc datetime required,
  ConsumedAtUtc datetime?,
  Provider ExternalLoginProvider required
)
```

## 4.6 `UserSecurityAuditLog`

Nuova entità:

```text
UserSecurityAuditLog(
  Id int PK,
  UserId int? FK,
  ActorUserId int? FK,
  EventType string required max 80,
  Provider string? max 30,
  IpAddress string? max 64,
  UserAgent string? max 512,
  MetadataJson string? max 4000,
  CreatedAtUtc datetime required
)
INDEX(UserId, CreatedAtUtc)
INDEX(ActorUserId, CreatedAtUtc)
INDEX(EventType, CreatedAtUtc)
```

Eventi minimi:

- `PasswordChanged`
- `PasswordResetRequested`
- `PasswordResetCompleted`
- `SetPasswordRequested`
- `SetPasswordCompleted`
- `ExternalLoginSucceeded`
- `ExternalLoginRejectedDomain`
- `ExternalLoginRejectedElevatedRole`
- `ExternalLoginLinked`
- `AdminInviteCreated`
- `AdminUserCreated`
- `RoleChanged`
- `RoleChangeRejected`
- `RefreshTokensRevoked`

---

## 5) API e Permessi

## 5.1 Endpoint auth pubblici

| Endpoint | Auth | Scopo |
| --- | --- | --- |
| `POST /auth/forgot-password` | Anonymous | Richiede email recupero password, risposta sempre generica |
| `POST /auth/reset-password` | Anonymous | Reimposta password tramite token temporaneo |
| `GET /auth/external/providers` | Anonymous | Provider social configurati e regole applicate per ciascun provider |
| `GET /auth/external/google/start?redirect=` | Anonymous | Avvia flusso Google |
| `GET /auth/external/google/callback` | Anonymous | Callback Google lato backend |
| `GET /auth/external/microsoft/start?redirect=` | Anonymous | Avvia flusso Microsoft |
| `GET /auth/external/microsoft/callback` | Anonymous | Callback Microsoft lato backend |
| `POST /auth/external/exchange` | Anonymous | Scambia exchange code con `AuthResponseDTO` |

Endpoint esistenti da mantenere:

- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/refresh`
- `POST /auth/logout`
- `GET /auth/me`

## 5.2 Endpoint auth autenticati

| Endpoint | Auth | Scopo |
| --- | --- | --- |
| `POST /auth/change-password` | `Authenticated` | Cambio password con password attuale |
| `POST /auth/set-password/request` | `Authenticated` | Invia link per impostare password a social-only loggato |
| `GET /auth/security/me` | `Authenticated` | Stato sicurezza account: provider, password locale, ultimo cambio password |

## 5.3 Endpoint admin utenti

| Endpoint | Auth | Scopo |
| --- | --- | --- |
| `GET /admin/utenti?page=&pageSize=&search=&role=` | `AdminOnly` | Listing utenti paginato e filtrabile |
| `POST /admin/utenti/inviti` | `AdminOnly` | Crea invito Power/Admin con link setup password |
| `POST /admin/utenti/{id}/password-setup` | `AdminOnly` | Invia link impostazione password a utente esistente |
| `PUT /admin/utenti/{id}/ruolo` | `AdminOnly` | Cambio ruolo controllato, estende endpoint esistente |
| `GET /admin/utenti/{id}/security` | `AdminOnly` | Dettaglio sicurezza utente e provider collegati |

## 5.4 Matrice pagine frontend aggiornata

| Pagina | Anonimo | User | PowerUser | Admin |
| --- | --- | --- | --- | --- |
| `login.html` | SI | - | - | - |
| `registrazione.html` | SI | - | - | - |
| `recupera-password.html` | SI | SI | SI | SI |
| `reimposta-password.html` | SI | SI | SI | SI |
| `social-login-complete.html` | SI | SI | SI | SI |
| `profilo.html` | - | SI | SI | SI |
| `utenti.html` | - | - | - | SI |

Le altre pagine mantengono la matrice dell'Iterazione 4.1.

## 5.5 Variabili environment

Aggiornare `backend/.env.example`:

```env
# Account security / password reset
PASSWORD_RESET_TOKEN_TTL_MINUTES=30
SET_PASSWORD_TOKEN_TTL_MINUTES=60
ADMIN_INVITE_TOKEN_TTL_HOURS=24
AUTH_EXTERNAL_STATE_TTL_MINUTES=10
AUTH_EXTERNAL_EXCHANGE_TTL_MINUTES=2

# Google OIDC
GOOGLE_OAUTH_CLIENT_ID=<google_client_id>
GOOGLE_OAUTH_CLIENT_SECRET=<google_client_secret>
GOOGLE_OAUTH_REDIRECT_URI=http://localhost:5000/auth/external/google/callback
GOOGLE_REQUIRE_EMAIL_VERIFIED=true

# Microsoft OIDC / Entra ID
MICROSOFT_OAUTH_CLIENT_ID=<microsoft_client_id>
MICROSOFT_OAUTH_CLIENT_SECRET=<microsoft_client_secret>
MICROSOFT_OAUTH_REDIRECT_URI=http://localhost:5000/auth/external/microsoft/callback
MICROSOFT_AUTHORITY=common
MICROSOFT_ACCEPT_PERSONAL_ACCOUNTS=true
MICROSOFT_ACCEPT_WORK_SCHOOL_ACCOUNTS=true
MICROSOFT_REQUIRE_EMAIL_CLAIM=true

# Frontend URL already present but now required for reset/social links
FRONTEND_BASE_URL=http://localhost:5001
```

Note configurazione Microsoft:

- `MICROSOFT_AUTHORITY=common` indica l'endpoint Microsoft che accetta sia account personali sia account work/school.
- `MICROSOFT_ACCEPT_PERSONAL_ACCOUNTS` e `MICROSOFT_ACCEPT_WORK_SCHOOL_ACCOUNTS` devono restare entrambi `true` per soddisfare il requisito aggiornato.
- `MICROSOFT_REQUIRE_EMAIL_CLAIM=true` significa che, se Microsoft non restituisce `email` o `preferred_username` in formato email, CineBase non autocrea un utente perché non avrebbe un indirizzo locale affidabile da salvare.
- Non introdurre filtri `MICROSOFT_ALLOWED_TENANT_*` o `MICROSOFT_ALLOWED_DOMAIN_*` nell'Iterazione 5: il requisito aggiornato è Microsoft generalista con validazione token, non allowlist di tenant o domini.

---

## 6) Fasi di Implementazione

### FASE 0 - Preflight auth e mappa superfici di sicurezza

**Obiettivo**: evitare modifiche cieche su auth, RBAC e frontend.

**Attività**:

1. Inventariare backend auth e utenti:

```bash
rg -n "AuthService|IAuthService|AuthEndpoints|RefreshToken|PasswordHash|UserRole|AdminUtenti|UpdateUserRole|JWT|JwtBearer|RequireAuthorization|AdminOnly|PowerUserOrAdmin|Authenticated" backend/FilmAPI tests/backend --glob "!bin/**" --glob "!obj/**"
```

2. Inventariare frontend auth/redirect:

```bash
rg -n "redirect|login|registrazione|Auth\.|route-guard|cb_access_token|cb_refresh_token|getUserRole|adminPaths|PAGE_PERMISSIONS" frontend/CineBase.Web/wwwroot --glob "!**/*.map"
```

3. Inventariare email e SMTP:

```bash
rg -n "SMTP_|IEmailService|EmailService|SendOrderTicketsAsync|FRONTEND_BASE_URL" backend docs --glob "!bin/**" --glob "!obj/**"
```

4. Verificare test esistenti:

```bash
dotnet test tests/backend/FilmAPI.Tests.csproj
```

5. Annotare eventuali open redirect residui, in particolare in:
   - `frontend/CineBase.Web/wwwroot/js/pages/login.js`
   - `frontend/CineBase.Web/wwwroot/js/auth.js`
   - `frontend/CineBase.Web/wwwroot/js/route-guard.js`

**Verifica fase**:

- elenco file impattati aggiornato;
- baseline test nota;
- rischi auth/redirect mappati prima delle modifiche.

**Checklist fase**:

- [ ] Ricerca backend auth eseguita
- [ ] Ricerca frontend auth/redirect eseguita
- [ ] Ricerca email/SMTP eseguita
- [ ] Baseline test backend eseguita
- [ ] Open redirect residui annotati

---

### FASE 1 - Modello dati credenziali, provider esterni e audit

**Obiettivo**: introdurre la base persistente per social login, password opzionale, token temporanei e audit.

**Attività backend**:

1. Estendere `User` con i campi della sezione 4.1.
2. Creare enum:
   - `ExternalLoginProvider`
   - `AccountActionTokenPurpose`
3. Creare model:
   - `UserExternalLogin`
   - `AccountActionToken`
   - `ExternalAuthState`
   - `ExternalAuthExchangeCode`
   - `UserSecurityAuditLog`
4. Aggiornare `FilmDbContext` con `DbSet`, indici, unique constraint e delete behavior.
5. Creare migration `AddAccountSecurityAndExternalLogins`.
6. Ispezionare migration:
   - utenti esistenti mantengono credenziali locali attive;
   - `PasswordHash` viene reso nullable senza perdere hash esistenti;
   - `NormalizedEmail` viene valorizzato da email esistenti;
   - nessuna tabella ticketing/checkout/show viene toccata impropriamente.
7. Aggiornare `DataSeeder` se necessario per valorizzare i nuovi campi sugli utenti seed.

**Verifica fase**:

```bash
dotnet build backend/FilmAPI/FilmAPI.csproj
```

```bash
dotnet ef migrations script --project backend/FilmAPI/FilmAPI.csproj --startup-project backend/FilmAPI/FilmAPI.csproj
```

**Test automatici minimi**:

- migration model snapshot contiene le nuove tabelle;
- utenti seed/admin esistenti riescono ancora a fare login locale;
- `PasswordHash` nullable non rompe `AuthService.LoginAsync`.

**Checklist fase**:

- [ ] `User` esteso
- [ ] Model external login/token/state/audit creati
- [ ] `FilmDbContext` aggiornato
- [ ] Migration creata e ispezionata
- [ ] Seeder aggiornato se necessario
- [ ] Build backend verde

---

### FASE 2 - Infrastruttura email account e token temporanei

**Obiettivo**: creare un'infrastruttura riusabile e testabile per reset password, setup password e inviti admin.

**Attività backend**:

1. Creare DTO per token e email account dove necessario.
2. Creare `IAccountTokenService` / `AccountTokenService` con metodi:
   - `CreateTokenAsync(userId, purpose, ttl, actorUserId, context)`
   - `ValidateTokenAsync(token, purpose)`
   - `ConsumeTokenAsync(token, purpose)`
   - `RevokeActiveTokensAsync(userId, purpose)`
3. Creare `IAccountEmailService` / `AccountEmailService` con metodi:
   - `SendPasswordResetAsync(user, resetUrl)`
   - `SendSetPasswordAsync(user, setupUrl)`
   - `SendAdminInviteAsync(user, inviteUrl, role)`
   - `SendPasswordChangedAsync(user)` facoltativo ma consigliato
4. Implementare hashing token con SHA-256 o HMAC-SHA256.
5. Aggiungere helper centralizzato per costruire URL frontend da `FRONTEND_BASE_URL`.
6. Aggiungere helper centralizzato `RedirectUrlValidator` per path relativi interni.
7. Aggiungere audit service leggero, ad esempio `IUserSecurityAuditService`.
8. Registrare servizi in DI.
9. Estendere `CustomWebApplicationFactory` con fake `IAccountEmailService`.

**Verifica fase**:

```bash
dotnet build backend/FilmAPI/FilmAPI.csproj
```

**Test automatici minimi**:

- token salvato hashato, non in chiaro;
- token valido prima della scadenza;
- token scaduto rifiutato;
- token già usato rifiutato;
- creazione nuovo token revoca i token attivi dello stesso scopo;
- fake email riceve URL corretto e non logga token in chiaro.

**Checklist fase**:

- [ ] `AccountTokenService` implementato
- [ ] `AccountEmailService` implementato
- [ ] Validatore redirect interno creato
- [ ] Audit service creato
- [ ] Fake email test creato
- [ ] Test token/email verdi

---

### FASE 3 - Backend cambio password e recupero password

**Obiettivo**: completare la gestione credenziali locali per tutti gli utenti.

**Attività backend**:

1. Estendere DTO auth, preferibilmente in `DTO/AuthDTO.cs` o file dedicato:
   - `ChangePasswordRequestDTO`
   - `ForgotPasswordRequestDTO`
   - `ResetPasswordRequestDTO`
   - `AccountSecurityDTO`
2. Estendere `IAuthService` / `AuthService` con:
   - `ChangePasswordAsync(userId, dto, deviceId)`
   - `RequestPasswordResetAsync(dto, context)`
   - `ResetPasswordAsync(dto, context)`
   - `RequestSetPasswordAsync(userId, context)`
   - `GetAccountSecurityAsync(userId)`
3. Mappare nuovi endpoint in `AuthEndpoints`.
4. Validare password lato backend:
   - minimo 8 caratteri;
   - almeno maiuscola, minuscola e numero, coerente col frontend attuale;
   - blocco password uguale alla precedente, se verificabile.
5. `POST /auth/forgot-password`:
   - risposta sempre `200 OK` con messaggio generico;
   - crea token solo se utente esiste e non è disabilitato;
   - per social-only usa purpose `SetPassword` o permette reset per creare password locale, in base alla scelta implementativa documentata.
6. `POST /auth/reset-password`:
   - valida token;
   - aggiorna `PasswordHash` con BCrypt;
   - imposta `LocalCredentialsEnabled = true`;
   - aggiorna `PasswordChangedAtUtc`;
   - incrementa `AuthVersion`;
   - revoca refresh token;
   - consuma token;
   - scrive audit.
7. `POST /auth/change-password`:
   - richiede utente autenticato;
   - richiede password attuale se `LocalCredentialsEnabled = true`;
   - rifiuta social-only e suggerisce setup password via email;
   - aggiorna hash e revoca sessioni.
8. Aggiungere rate limiting su login e forgot password.

**Verifica fase**:

```bash
dotnet test tests/backend/FilmAPI.Tests.csproj --filter "FullyQualifiedName~Password"
```

**Test automatici minimi**:

- cambio password con password attuale corretta: OK;
- cambio password con password attuale errata: `400` o `401` coerente;
- login con vecchia password dopo cambio: fallisce;
- login con nuova password: OK;
- forgot password email esistente: risposta generica + email fake inviata;
- forgot password email inesistente: stessa risposta, nessuna email;
- reset token valido: password aggiornata;
- reset token riusato: rifiutato;
- reset token scaduto: rifiutato;
- reset revoca refresh token;
- reset incrementa `AuthVersion`;
- social-only può impostare password via token;
- audit scritto per richiesta e completamento reset.

**Checklist fase**:

- [ ] DTO credenziali creati
- [ ] Endpoint password mappati
- [ ] Cambio password implementato
- [ ] Forgot/reset password implementati
- [ ] Revoca sessioni implementata
- [ ] Rate limiting aggiunto
- [ ] Test password/reset verdi

---

### FASE 4 - Backend social login Google/Microsoft per utenti `User`

**Obiettivo**: aggiungere accesso social sicuro per utenti normali, con Google aperto agli account Google verificati e Microsoft aperto ad account personali Microsoft e account work/school Microsoft Entra ID.

**Attività backend**:

1. Aggiungere package OIDC se non già disponibili transitivamente:

```bash
dotnet add backend/FilmAPI/FilmAPI.csproj package Microsoft.IdentityModel.Protocols.OpenIdConnect
```

2. Creare DTO:
   - `ExternalProviderDTO`
   - `ExternalExchangeRequestDTO`
   - `ExternalLoginErrorDTO` se utile.
3. Creare servizi:
   - `IExternalAuthService` / `ExternalAuthService`
   - `IExternalAuthProvider`
   - `GoogleExternalAuthProvider`
   - `MicrosoftExternalAuthProvider`
4. Implementare `GET /auth/external/providers`.
5. Implementare start flow provider:
   - genera state e nonce;
   - genera PKCE code verifier/challenge;
   - salva `ExternalAuthState`;
   - valida `redirect` come path relativo;
   - redirect al provider.
6. Implementare callback provider:
   - valida state single-use;
   - scambia authorization code con token endpoint provider;
   - valida ID token con metadata OIDC;
   - per Google, valida `email_verified == true` e non applica vincoli di dominio;
   - per Microsoft, valida `iss`, `aud`, firma, `exp`, `nonce`, `tid`, `oid`/`sub`, coerenza issuer/tenant e indirizzo email-like se necessario a creazione o linking;
   - crea/collega account `User`;
   - rifiuta account esistente `PowerUser`/`Admin`;
   - crea exchange code one-time;
   - redirect a `social-login-complete.html`.
7. Implementare `POST /auth/external/exchange`:
   - consuma exchange code;
   - genera JWT e refresh token applicativi;
   - aggiorna `LastLoginAtUtc`, `LastLoginProvider`;
   - scrive audit.
8. Non salvare provider access token o refresh token.
9. Pulire state/exchange scaduti con hosted service o lazy cleanup.

**Verifica fase**:

```bash
dotnet build backend/FilmAPI/FilmAPI.csproj
```

**Test automatici minimi**:

- fake Google valido `utente@gmail.com`: crea `User`;
- fake Google valido `utente@outlook.com` con account Google e `email_verified = true`: crea `User`;
- fake Google valido `utente@issgreppi.it`: crea `User`;
- fake Microsoft valido account personale `utente@outlook.com`: crea `User`;
- fake Microsoft valido account work/school `utente@issgreppi.it`: crea `User`;
- fake Microsoft valido account work/school di tenant diverso: crea `User` se il token è valido e contiene email-like utilizzabile;
- Google valido con dominio generico: accettato se `email_verified == true`;
- email Google non verificata: rifiutata;
- Microsoft con issuer non coerente con `tid`: rifiutato;
- Microsoft senza `oid` e senza `sub`: rifiutato;
- Microsoft senza `email`/`preferred_username` email-like: rifiutato per autocreazione/linking;
- Microsoft con consenso negato o policy tenant che blocca l'app: rifiutato con errore gestibile;
- account `PowerUser` esistente con stessa email: social login rifiutato;
- account `Admin` esistente con stessa email: social login rifiutato;
- account `User` locale esistente: provider collegato;
- provider già collegato: login ritorna stesso utente;
- state mancante/scaduto/riusato: rifiutato;
- exchange code riusato: rifiutato;
- redirect esterno nel parametro `redirect`: normalizzato o rifiutato;
- ruolo nel token applicativo è sempre `User` per account social creati.

**Checklist fase**:

- [ ] Provider service Google creato
- [ ] Provider service Microsoft creato
- [ ] Start/callback/exchange implementati
- [ ] Email Google verificata validata backend senza vincolo dominio
- [ ] Casi Google espliciti coperti con `gmail.com`, `outlook.com` e `issgreppi.it`
- [ ] Microsoft personale e work/school coperti senza filtro hard-coded su dominio/tenant
- [ ] Issuer/tenant/subject Microsoft validati backend
- [ ] Casi Microsoft con email assente, consenso negato e policy tenant gestiti
- [ ] Ruoli elevati bloccati da social login
- [ ] State/exchange single-use implementati
- [ ] Test social verdi

---

### FASE 5 - Backend admin utenti: creazione, invito, elevazione e hardening ruoli

**Obiettivo**: completare la gestione sicura degli utenti privilegiati.

**Attività backend**:

1. Estendere DTO admin utenti:
   - `AdminUserListItemDTO`
   - `AdminUserPagedResultDTO`
   - `CreateAdminUserInviteDTO`
   - `AdminUserSecurityDTO`
   - `UpdateRuoloDTO` con eventuali campi di conferma/audit note.
2. Estendere `IUserAdminService` / `UserAdminService`:
   - listing paginato e filtrabile;
   - creazione invito per `PowerUser`/`Admin`;
   - invio setup password a utente esistente;
   - cambio ruolo con validazioni forti.
3. Mantenere compatibilità minima di `GET /admin/utenti` se usato da test esistenti, oppure aggiornare test e frontend in modo coordinato.
4. Regole cambio ruolo:
   - solo `AdminOnly`;
   - vietato creare/promuovere se utente disabilitato;
   - vietato promuovere social-only a `PowerUser`/`Admin`;
   - vietato degradare ultimo admin;
   - cambio a `PowerUser`/`Admin` richiede `LocalCredentialsEnabled = true`;
   - cambio ruolo incrementa `AuthVersion` e revoca refresh token;
   - social login futuro rifiutato per ruoli elevati.
5. Creazione invito:
   - admin inserisce email, nome, cognome, ruolo target;
   - email deve essere normalizzata;
   - se email già esistente, restituire `409` e suggerire promozione;
   - creare utente con ruolo target, `PasswordHash = null`, `LocalCredentialsEnabled = false`, `MustChangePassword = true` e account non utilizzabile finché non imposta password, oppure creare stato pending documentato;
   - inviare token `AdminInvite`;
   - al completamento invito, impostare password e attivare credenziali locali.
6. Scrivere audit per tutte le operazioni.

**Decisione implementativa consigliata per inviti**:

- se il modello introduce `IsDisabled`, creare l'utente invitato con `IsDisabled = true` e abilitarlo al completamento password;
- se non si introduce `IsDisabled`, bloccare login locale finché `LocalCredentialsEnabled = false` e gestire messaggio chiaro.

**Test automatici minimi**:

- admin crea invito `PowerUser`: OK + email fake;
- admin crea invito `Admin`: OK + email fake;
- power user crea invito: `403`;
- user crea invito: `403`;
- anonimo crea invito: `401`;
- invito email duplicata: `409`;
- completamento invito imposta password e consente login;
- promozione `User` locale a `PowerUser`: OK;
- promozione `User` locale a `Admin`: OK;
- promozione social-only a `PowerUser/Admin`: `409` con codice errore gestibile dal frontend;
- downgrade ultimo admin: bloccato;
- cambio ruolo revoca refresh token e incrementa `AuthVersion`;
- audit scritto.

**Checklist fase**:

- [ ] DTO admin utenti estesi
- [ ] Listing paginato/filtrato implementato
- [ ] Invito Admin/Power implementato
- [ ] Promozione controllata implementata
- [ ] Social-only elevazione bloccata
- [ ] Ultimo admin protetto
- [ ] Audit e revoca sessioni implementati
- [ ] Test admin utenti verdi

---

### FASE 6 - Frontend login, recupero password e sicurezza profilo

**Obiettivo**: esporre i nuovi flussi credenziali agli utenti finali mantenendo redirect sicuri.

**Attività frontend**:

1. Aggiornare `login.html`:
   - link `recupera-password.html`;
   - bottoni Google/Microsoft;
   - messaggi per errori social.
2. Aggiornare `js/pages/login.js`:
   - sanitizzare sempre `redirect` con helper condiviso;
   - non usare `decodeURIComponent(redirect)` verso `window.location.href` senza validazione;
   - gestire errori backend social riportati via query string.
3. Aggiornare `auth.js`:
   - helper `sanitizeRedirectPath`;
   - `startExternalLogin(provider, redirect)`;
   - metodi `forgotPassword`, `resetPassword`, `changePassword`, `requestSetPassword`;
   - rimuovere ogni redirect non validato.
4. Creare `recupera-password.html` + `js/pages/recupera-password.js`.
5. Creare `reimposta-password.html` + `js/pages/reimposta-password.js`.
6. Creare `social-login-complete.html` + `js/pages/social-login-complete.js`.
7. Aggiornare `registrazione.html` e `registrazione.js` con messaggio o bottoni social opzionali.
8. Aggiornare `profilo.html`:
   - sezione "Sicurezza account";
   - form cambio password;
   - stato provider collegati;
   - pulsante invia link setup password se social-only.
9. Aggiornare `api.js` con i metodi endpoint nuovi.
10. Aggiornare `route-guard.js` per le nuove pagine.
11. Verificare responsive mobile e desktop.

**Verifica fase**:

```bash
dotnet build frontend/CineBase.Web/CineBase.Web.csproj
```

**Smoke manuale fase**:

- login locale continua a funzionare;
- redirect dopo login accetta solo path interni;
- recupero password mostra sempre messaggio generico;
- reset password da link consente nuovo login;
- profilo cambia password e forza nuovo login sugli altri device;
- bottoni social reindirizzano al backend start endpoint;
- `social-login-complete.html` gestisce code valido, code scaduto e code riusato.

**Checklist fase**:

- [ ] Login UI aggiornata
- [ ] Redirect login hardenizzato
- [ ] Pagine recupero/reset create
- [ ] Pagina social complete creata
- [ ] Profilo sicurezza account aggiornato
- [ ] `api.js` aggiornato
- [ ] `route-guard.js` aggiornato
- [ ] Build frontend verde

---

### FASE 7 - Frontend admin gestione utenti elevati

**Obiettivo**: fornire agli admin una UI operativa per account privilegiati.

**Attività frontend**:

1. Creare `frontend/CineBase.Web/wwwroot/utenti.html`.
2. Creare `frontend/CineBase.Web/wwwroot/js/pages/utenti.js`.
3. Aggiornare `admin-shell.js`:
   - aggiungere link `Utenti` visibile solo ad `Admin`;
   - evitare che `PowerUser` veda link non utilizzabili.
4. Aggiornare `route-guard.js`:
   - `utenti.html` solo `admin`.
5. Implementare tabella utenti:
   - ricerca;
   - filtro ruolo;
   - paginazione;
   - badge provider social;
   - badge password locale presente/assente;
   - badge ruolo.
6. Implementare modale invito:
   - email;
   - nome;
   - cognome;
   - ruolo `PowerUser` o `Admin`;
   - conferma prima dell'invio.
7. Implementare azioni riga:
   - cambia ruolo;
   - invia link setup password;
   - visualizza stato sicurezza.
8. Gestire errori specifici:
   - ultimo admin;
   - utente social-only non promuovibile;
   - email duplicata;
   - permessi insufficienti;
   - token invito già generato.

**Verifica fase**:

```bash
dotnet build frontend/CineBase.Web/CineBase.Web.csproj
```

**Smoke manuale fase**:

- Admin vede `utenti.html` e link sidebar;
- PowerUser non vede link e viene rediretto se apre URL diretto;
- Admin crea invito PowerUser;
- Admin crea invito Admin;
- Admin promuove utente locale;
- Admin riceve blocco chiaro su utente social-only;
- Admin non può degradare ultimo admin.

**Checklist fase**:

- [ ] `utenti.html` creato
- [ ] `utenti.js` creato
- [ ] Sidebar admin aggiornata
- [ ] Route guard aggiornata
- [ ] Listing utenti operativo
- [ ] Invito Admin/Power operativo
- [ ] Promozione/degradazione UI operativa
- [ ] Build frontend verde

---

### FASE 8 - Test automatici estesi auth/security

**Obiettivo**: portare la nuova superficie auth sotto copertura automatica robusta.

**Nuovi file test consigliati**:

- `tests/backend/Integration/PasswordCredentialsIntegrationTests.cs`
- `tests/backend/Integration/ExternalAuthIntegrationTests.cs`
- `tests/backend/Integration/AdminUserSecurityIntegrationTests.cs`
- eventuali estensioni a `AuthIntegrationTests.cs` e `RbacIntegrationTests.cs`

**Aggiornare `CustomWebApplicationFactory`**:

- fake `IAccountEmailService`;
- fake provider OIDC Google/Microsoft o fake `IExternalAuthProvider`;
- helper per creare utenti social-only;
- helper per creare token reset/invito quando serve;
- helper per leggere audit log.

**Casi test obbligatori**:

Password e reset:

- cambio password success;
- cambio password password attuale errata;
- reset password email esistente/inesistente non enumerativo;
- token reset single-use;
- token reset scaduto;
- revoca refresh token post reset;
- invalidazione `AuthVersion`.

Social:

- Google valido con email verificata su dominio generico, ad esempio `gmail.com`;
- Google valido con email verificata su dominio generico esterno, ad esempio `outlook.com`, purché sia un account Google valido;
- Google valido con email verificata su dominio `issgreppi.it`;
- Microsoft valido con account personale, ad esempio `outlook.com`, `hotmail.com` o `live.*`;
- Microsoft valido con account work/school `issgreppi.it`;
- Microsoft valido con account work/school di tenant diverso;
- Google con email non verificata rifiutato;
- Microsoft con issuer non coerente con `tid` rifiutato;
- Microsoft senza identificatore stabile rifiutato;
- Microsoft senza email-like utilizzabile rifiutato per autocreazione/linking;
- Microsoft con consenso negato o policy tenant bloccante gestito con errore chiaro;
- account elevato rifiutato;
- linking account `User` esistente;
- exchange code single-use;
- state replay bloccato;
- redirect esterno bloccato.

Admin utenti:

- admin crea invito `PowerUser`;
- admin crea invito `Admin`;
- non-admin non può creare inviti;
- promozione utente locale OK;
- promozione social-only rifiutata;
- ultimo admin non degradabile;
- cambio ruolo revoca sessioni;
- audit presente.

RBAC e regressione:

- endpoint `utenti` AdminOnly;
- login/register/refresh/logout/me esistenti non regrediscono;
- `User` normale continua ad accedere a checkout/profilo;
- `PowerUser/Admin` continuano ad accedere alle pagine operative già esistenti.

**Comandi verifica**:

```bash
dotnet build backend/FilmAPI/FilmAPI.csproj
dotnet build tests/backend/FilmAPI.Tests.csproj
dotnet test tests/backend/FilmAPI.Tests.csproj
```

**Checklist fase**:

- [ ] Fake email account implementato
- [ ] Fake provider social implementato
- [ ] Test password/reset aggiunti
- [ ] Test social aggiunti
- [ ] Test admin utenti aggiunti
- [ ] Test RBAC estesi
- [ ] Suite backend verde

---

### FASE 9 - Smoke test runtime e verifica manuale sicurezza

**Obiettivo**: verificare end-to-end ciò che i test automatici non coprono pienamente, soprattutto browser, redirect ed email reale.

**Build da eseguire**:

```bash
dotnet build backend/FilmAPI/FilmAPI.csproj
dotnet build frontend/CineBase.Web/CineBase.Web.csproj
dotnet build backend/scripts/FilmApiSeeder/FilmApiSeeder.csproj
dotnet test tests/backend/FilmAPI.Tests.csproj
```

**Smoke runtime locale**:

- `login.html` 200;
- `registrazione.html` 200;
- `recupera-password.html` 200;
- `reimposta-password.html` 200;
- `social-login-complete.html` 200;
- `profilo.html` protetta;
- `utenti.html` AdminOnly;
- `GET /auth/external/providers` 200;
- `POST /auth/forgot-password` 200 generico;
- `POST /auth/change-password` 401 da anonimo;
- `GET /admin/utenti` 401 anonimo, 403 user/power, 200 admin.

**Verifica manuale browser**:

- login locale User;
- login locale PowerUser;
- login locale Admin;
- registrazione pubblica crea sempre User;
- cambio password User;
- reset password User;
- reset password Admin;
- apertura diretta `utenti.html` da PowerUser viene bloccata;
- Admin crea invito PowerUser;
- utente invitato imposta password e accede;
- Admin promuove utente locale;
- Admin non può promuovere social-only senza password;
- social login Google reale con account `gmail.com`, se credenziali provider disponibili;
- social login Google reale con account Google verificato su dominio esterno, ad esempio `outlook.com`, se credenziali provider disponibili;
- social login Google reale con account `@issgreppi.it`, se credenziali provider disponibili;
- social login Microsoft reale con account personale, ad esempio `outlook.com`, se credenziali provider disponibili;
- social login Microsoft reale con account work/school `@issgreppi.it`, se tenant/config disponibili;
- social login Microsoft reale con account work/school di altro tenant, se testabile;
- social login Microsoft con consenso negato o policy tenant bloccante mostra errore chiaro, se testabile;
- open redirect: `login.html?redirect=https://evil.example` non deve uscire dal sito;
- open redirect: callback social con redirect esterno non deve uscire dal sito.

**Verifica email reale opzionale ma consigliata**:

- invio reset password via SMTP configurato;
- link reset apre frontend corretto;
- token non riutilizzabile dopo reset.

**Checklist fase**:

- [ ] Build backend verde
- [ ] Build frontend verde
- [ ] Build seeder verde
- [ ] Test backend verdi
- [ ] Smoke pagine auth OK
- [ ] Smoke endpoint auth/admin OK
- [ ] Verifica manuale ruoli completata
- [ ] Verifica provider reali eseguita o motivazione documentata
- [ ] Verifica email reale eseguita o motivazione documentata
- [ ] Open redirect verificati

---

### FASE 10 - Documentazione finale

**Obiettivo**: rendere tracciabile lo stato dell'Iterazione 5.

Aggiornare:

- `docs/project/status.md`
- `docs/project/changelog.md`
- `docs/project/dev_iteration/5/PianoDiLavoro.md`
- `backend/.env.example`
- `docs/tutorials/TUTORIAL_SOCIAL_LOGIN_GOOGLE_MICROSOFT.md`

`status.md` deve indicare:

- fasi Iterazione 5 completate;
- numero test aggiornato;
- provider social supportati;
- regole provider applicate: Google email verificata, Microsoft personale/work-school con issuer, tenant e subject validati;
- stato verifica SMTP/provider reali;
- eventuali limiti residui.

`changelog.md` deve indicare:

- model/migration aggiunti;
- endpoint auth/social/password aggiunti;
- pagina admin utenti aggiunta;
- frontend login/reset/profilo aggiornato;
- hardening sessioni/redirect/ruoli;
- test automatici aggiunti;
- verifiche manuali eseguite.

Questo piano deve essere aggiornato:

- tabella `Stato Avanzamento Fasi`;
- checklist delle fasi;
- scostamenti tecnici reali;
- risultati finali test e smoke.

**Checklist fase**:

- [ ] `status.md` aggiornato
- [ ] `changelog.md` aggiornato
- [ ] `.env.example` aggiornato
- [ ] Tutorial social login Google/Microsoft aggiornato
- [ ] Piano Iterazione 5 aggiornato con esiti reali
- [ ] Eventuali limiti o test non eseguiti documentati

---

## 7) File e Aree Impattate

## 7.1 Backend `backend/FilmAPI/`

File da modificare:

- `Model/User.cs`
- `Model/RefreshToken.cs` se serve correlare meglio sessioni/device
- `Data/FilmDbContext.cs`
- `Services/AuthService.cs`
- `Services/IAuthService.cs`
- `Services/UserAdminService.cs`
- `Services/IUserAdminService.cs`
- `Endpoints/AuthEndpoints.cs`
- `Endpoints/AdminUtentiEndpoints.cs`
- `DTO/AuthDTO.cs`
- `DTO/UserAdminDTO.cs`
- `Program.cs`
- `FilmAPI.csproj`
- `Data/DataSeeder.cs`
- `Migrations/FilmDbContextModelSnapshot.cs`

Nuovi file probabili:

- `Model/ExternalLoginProvider.cs`
- `Model/UserExternalLogin.cs`
- `Model/AccountActionToken.cs`
- `Model/AccountActionTokenPurpose.cs`
- `Model/ExternalAuthState.cs`
- `Model/ExternalAuthExchangeCode.cs`
- `Model/UserSecurityAuditLog.cs`
- `DTO/AuthCredentialsDTO.cs` o estensione `AuthDTO.cs`
- `DTO/ExternalAuthDTO.cs`
- `DTO/AdminUserDTO.cs` se si separano i DTO admin estesi
- `Services/IAccountTokenService.cs`
- `Services/AccountTokenService.cs`
- `Services/IAccountEmailService.cs`
- `Services/AccountEmailService.cs`
- `Services/IExternalAuthService.cs`
- `Services/ExternalAuthService.cs`
- `Services/IExternalAuthProvider.cs`
- `Services/GoogleExternalAuthProvider.cs`
- `Services/MicrosoftExternalAuthProvider.cs`
- `Services/IUserSecurityAuditService.cs`
- `Services/UserSecurityAuditService.cs`
- `Services/RedirectUrlValidator.cs`

## 7.2 Frontend `frontend/CineBase.Web/wwwroot/`

File da modificare:

- `login.html`
- `registrazione.html`
- `profilo.html`
- `js/auth.js`
- `js/api.js`
- `js/route-guard.js`
- `js/admin-shell.js`
- `js/pages/login.js`
- `js/pages/registrazione.js`
- `js/pages/profilo.js`
- `css/styles.css` solo se necessario

Nuovi file:

- `recupera-password.html`
- `reimposta-password.html`
- `social-login-complete.html`
- `utenti.html`
- `js/pages/recupera-password.js`
- `js/pages/reimposta-password.js`
- `js/pages/social-login-complete.js`
- `js/pages/utenti.js`

## 7.3 Test

File da modificare:

- `tests/backend/Integration/AuthIntegrationTests.cs`
- `tests/backend/Integration/RbacIntegrationTests.cs`
- `tests/backend/Integration/CustomWebApplicationFactory.cs`

Nuovi file:

- `tests/backend/Integration/PasswordCredentialsIntegrationTests.cs`
- `tests/backend/Integration/ExternalAuthIntegrationTests.cs`
- `tests/backend/Integration/AdminUserSecurityIntegrationTests.cs`

## 7.4 Configurazione e documentazione

- `backend/.env.example`
- `docs/project/status.md`
- `docs/project/changelog.md`
- `docs/tutorials/TUTORIAL_SOCIAL_LOGIN_GOOGLE_MICROSOFT.md`

---

## 8) Rischi e Mitigazioni

| Rischio | Probabilità | Impatto | Mitigazione |
| --- | --- | --- | --- |
| Social login assegna o conserva privilegi elevati | Media senza vincoli | Critico | Social login solo `User`; rifiuto runtime per `PowerUser`/`Admin`; test dedicati |
| Email/domain spoofing provider | Media | Alto | Validare ID token provider; per Google richiedere `email_verified`, per Microsoft validare firma, issuer, tenant e subject stabile senza fidarsi del dominio email |
| Account takeover tramite linking email non verificata | Media | Alto | Collegare solo dopo validazione provider-specifica; Google richiede email verificata; Microsoft richiede identità stabile e linking auditato, con eventuale conferma email se si decide di rafforzare il flusso |
| Open redirect su login o social callback | Media | Alto | Helper unico per redirect relativi; test automatici e manuali con URL esterni |
| Token reset salvati in chiaro | Bassa se progettato bene | Critico | Salvare solo hash; non loggare token; test su DB |
| Enumerazione utenti da forgot password | Alta se non curata | Medio/Alto | Risposta sempre generica; rate limiting; audit |
| Vecchio JWT resta admin dopo downgrade | Alta con JWT stateless attuale | Alto | `AuthVersion`/`SecurityStamp` validato e refresh token revocati |
| Promozione social-only a ruolo elevato | Media | Alto | Blocco backend + UI; setup password obbligatorio prima della promozione |
| Password locale mancante rompe login esistente | Media | Alto | Migrazione con default per utenti esistenti; test regressione login |
| Microsoft claim email non uniforme | Media | Medio | Usare `email`/`preferred_username` solo come indirizzo applicativo, non come identificatore stabile; identificare il provider con `tid + oid/sub`; documentare claim supportati; test fake |
| Google aperto a domini generici crea utenti esterni | Media | Medio | Consentito dal requisito: assegnare sempre e solo ruolo `User`, nessuna elevazione senza password locale e audit su provider |
| Microsoft aperto ad account personali e work/school crea utenti esterni | Media | Medio | Consentito dal requisito: assegnare sempre e solo ruolo `User`, nessuna elevazione senza password locale, audit provider e gestione chiara di tenant policy/consenso negato |
| Provider reali non configurabili in locale | Alta | Medio | Test automatici con provider fake; smoke reale opzionale ma documentato |
| Email SMTP non configurata | Media | Medio | Fake test automatico; messaggi chiari; verifica reale opzionale; `.env.example` aggiornato |
| Aumento complessità auth | Alta | Medio | Fasi piccole, servizi separati, test mirati, nessun cambio cookie/token storage globale |

---

## 9) Piano Test Dettagliato

## 9.1 Test automatici backend obbligatori

| Area | Copertura minima |
| --- | --- |
| Login locale regressione | register/login/refresh/logout/me esistenti ancora verdi |
| Password change | success, password attuale errata, social-only senza password, revoca sessioni |
| Forgot/reset | no enumeration, token valido, token riuso, token scaduto, email fake, hash token |
| Setup password | account social-only imposta password e diventa promuovibile |
| Google social | email verificata su `gmail.com`, su dominio esterno tipo `outlook.com`, su `issgreppi.it`, email non verificata rifiutata, linking, ruolo User |
| Microsoft social | account personale valido, account work/school `issgreppi.it`, account work/school altro tenant, issuer/tenant incoerente rifiutato, subject mancante rifiutato, email-like assente rifiutata per autocreazione/linking, consenso negato gestito |
| Social security | Power/Admin rifiutati, exchange code one-time, state replay bloccato |
| Admin invite | create Admin/Power, duplicate email, non-admin forbidden, invito completato |
| Role management | promozione User locale, blocco social-only, blocco ultimo admin, revoca token |
| Audit | eventi sensibili creati con actor/user corretti |
| Redirect | redirect esterni rifiutati in login/social flow |

## 9.2 Test automatici frontend

Il repository non risulta avere un test runner frontend dedicato. In questa iterazione non è obbligatorio introdurre Playwright o Vitest solo per i nuovi flussi, salvo decisione esplicita.

Verifiche automatiche realistiche:

- build `frontend/CineBase.Web`;
- eventuale smoke HTTP statico se già usato nel progetto;
- test backend sugli endpoint che alimentano le pagine.

Se si decide di introdurre Playwright, limitarsi a smoke essenziali:

- login page render;
- forgot password form submit;
- reset password form con token fake intercettato;
- route guard `utenti.html` per ruoli.

## 9.3 Verifica manuale obbligatoria

- User locale: login, cambio password, forgot/reset.
- User social: login Google/Microsoft, profilo, checkout base non regressivo.
- PowerUser: login locale, accesso dashboard operativa, social login rifiutato.
- Admin: login locale, accesso `utenti.html`, invito/promozione/degradazione controllata.
- Anonimo: pagine pubbliche e auth forms.
- Redirect malevoli: nessuna uscita verso domini esterni.

---

## 10) Stima Effort

| Attività | Tempo stimato |
| --- | --- |
| Preflight e mappa auth | 30-45 min |
| Modello dati + migration | 60-120 min |
| Token/email account | 60-120 min |
| Cambio/reset password backend | 90-150 min |
| Social login backend Google/Microsoft | 180-300 min |
| Admin utenti backend | 90-180 min |
| Frontend auth/reset/profilo | 120-210 min |
| Frontend admin utenti | 120-210 min |
| Test automatici backend | 180-300 min |
| Smoke/manual verification | 60-120 min |
| Documentazione finale | 30-60 min |
| **Totale realistico** | **2-4 giornate tecniche**, dipendente dalla disponibilità credenziali Google/Microsoft reali |

---

## 11) Criteri di Accettazione Definitivi

L'Iterazione 5 può essere marcata completata solo se:

1. La registrazione pubblica locale crea sempre `User`.
2. Social login Google crea o collega solo utenti `User` quando Google restituisce un ID token valido con `email_verified = true`, senza alcun vincolo sul dominio email.
3. Social login Microsoft crea o collega solo utenti `User` con account personali Microsoft o account work/school quando il token OIDC è valido, issuer/tenant/subject sono coerenti e l'indirizzo email-like richiesto dal modello applicativo è disponibile.
4. Social login Microsoft con token non valido, issuer/tenant incoerente, subject assente, email-like assente per autocreazione/linking, consenso negato o policy tenant bloccante viene rifiutato con errore gestibile.
5. Social login per account `PowerUser` viene rifiutato.
6. Social login per account `Admin` viene rifiutato.
7. `PowerUser` e `Admin` possono accedere con credenziali locali.
8. Gli utenti con password locale possono cambiare password.
9. Cambio password invalida refresh token e token applicativi precedenti secondo `AuthVersion`/`SecurityStamp`.
10. Forgot password risponde in modo non enumerativo.
11. Reset password usa token temporaneo hashato e single-use.
12. Reset password revoca sessioni esistenti.
13. Account social-only può impostare password locale tramite link email.
14. Admin può creare invito `PowerUser` senza password in chiaro.
15. Admin può creare invito `Admin` senza password in chiaro.
16. Admin può promuovere un `User` locale a `PowerUser` o `Admin`.
17. Admin non può promuovere un account social-only finché non ha password locale.
18. Ultimo admin non può essere degradato.
19. Cambio ruolo revoca refresh token e invalida JWT precedenti.
20. Tutte le operazioni sensibili producono audit log.
21. `utenti.html` è accessibile solo ad `Admin`.
22. `PowerUser` non vede o non può usare strumenti di gestione utenti.
23. Tutti i redirect auth/social/reset sono limitati a path interni.
24. Build backend verde.
25. Build frontend verde.
26. Build seeder verde.
27. Suite backend completa verde.
28. `backend/.env.example` documenta tutte le variabili nuove.
29. `status.md` e `changelog.md` sono aggiornati.
30. Eventuali test provider reali non eseguiti sono dichiarati esplicitamente con motivo.

---

## 12) Prompt Operativo Consigliato

```text
Implementa l'Iterazione 5 descritta in `docs/project/dev_iteration/5/PianoDiLavoro.md`.

Obiettivo: aggiungere gestione credenziali completa, recupero password via email, social login Google aperto agli account Google verificati, social login Microsoft aperto agli account personali Microsoft e agli account work/school, e strumenti Admin per creare o promuovere PowerUser/Admin in modo sicuro.

Segui rigorosamente le fasi:
1. preflight auth e mappa superfici di sicurezza;
2. modello dati per password opzionale, provider esterni, token temporanei, state OAuth e audit;
3. infrastruttura token/email account;
4. cambio password e forgot/reset password backend;
5. social login backend Google/Microsoft con OIDC, Google senza vincolo dominio, Microsoft senza filtro dominio/tenant predefinito ma con validazione issuer/tenant/subject e blocco ruoli elevati;
6. gestione admin utenti con inviti e promozioni controllate;
7. frontend login/reset/profilo/social complete;
8. frontend `utenti.html` AdminOnly;
9. test automatici backend estesi;
10. smoke test runtime e verifica manuale sicurezza;
11. documentazione finale.

Non consentire mai autoregistrazione come PowerUser/Admin. Non consentire social login per PowerUser/Admin. Non salvare token temporanei in chiaro. Non lasciare redirect non validati. Non considerare completata la fase finché build backend/frontend/seeder e suite backend non sono verdi, oppure finché ogni verifica non eseguita è dichiarata esplicitamente con motivazione.
```
