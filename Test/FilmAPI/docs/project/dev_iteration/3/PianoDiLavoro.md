# Piano di Lavoro - FilmAPI Iterazione 3

## Panoramica

Questa iterazione implementa un sistema completo di **autenticazione e autorizzazione** per il progetto FilmAPI, con supporto per **RBAC (Role-Based Access Control)** e gestione delle **categorie dei film**.

### Stack Tecnologico Aggiunto
- **Autenticazione**: JWT (JSON Web Tokens) con Access Token e Refresh Token
- **Password Hashing**: BCrypt
- **RBAC**: Role-based authorization middleware
- **Database**: Nuove tabelle per Utenti, Ruoli, Categorie e tabelle di join

---

## Fase 1: Setup Database e Modello Dati

### 1.1 Nuove Entità Backend

#### Utente
```csharp
public class Utente
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Telefono { get; set; }
    public DateTime DataRegistrazione { get; set; }
    public DateTime? DataUltimoAccesso { get; set; }
    public bool Attivo { get; set; }
    public ICollection<UtenteRuolo> UtentiRuoli { get; set; } = new List<UtenteRuolo>();
    public ICollection<ProiezioneSalvata> ProiezioniSalvate { get; set; } = new List<ProiezioneSalvata>();
}
```

#### Ruolo
```csharp
public class Ruolo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty; // "Admin", "PowerUser", "User"
    public string Descrizione { get; set; } = string.Empty;
    public ICollection<UtenteRuolo> UtentiRuoli { get; set; } = new List<UtenteRuolo>();
}
```

#### UtenteRuolo (Tabella di join)
```csharp
public class UtenteRuolo
{
    public int UtenteId { get; set; }
    public Utente Utente { get; set; } = null!;
    public int RuoloId { get; set; }
    public Ruolo Ruolo { get; set; } = null!;
}
```

#### Categoria
```csharp
public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty; // "Fantasy", "Horror", "Drammatico", "Commedia"
    public string? Descrizione { get; set; }
    public ICollection<FilmCategoria> FilmsCategorie { get; set; } = new List<FilmCategoria>();
}
```

#### FilmCategoria (Tabella di join molti-a-molti)
```csharp
public class FilmCategoria
{
    public int FilmId { get; set; }
    public Film Film { get; set; } = null!;
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
}
```

#### ProiezioneSalvata (Area Personale)
```csharp
public class ProiezioneSalvata
{
    public int Id { get; set; }
    public int UtenteId { get; set; }
    public Utente Utente { get; set; } = null!;
    public int ProiezioneId { get; set; }
    public Proiezione Proiezione { get; set; } = null!;
    public DateTime DataSalvataggio { get; set; }
    public bool Prenotato { get; set; } // Per prenotazione virtuale
    public DateTime? DataPrenotazione { get; set; }
    public int NumeroPosti { get; set; }
}
```

### 1.2 Aggiornamento FilmDbContext
- Aggiungere `DbSet<Utente>`
- Aggiungere `DbSet<Ruolo>`
- Aggiungere `DbSet<UtenteRuolo>`
- Aggiungere `DbSet<Categoria>`
- Aggiungere `DbSet<FilmCategoria>`
- Aggiungere `DbSet<ProiezioneSalvata>`
- Configurare relazioni con Fluent API
- Configurare indici unique per Username e Email

### 1.3 Migration Database
- Creare migration: `dotnet ef migrations add AddAuthAndCategories`
- Applicare migration: `dotnet ef database update`

### 1.4 Seed Dati Iniziali
- Creare ruoli: Admin, PowerUser, User
- Creare utente admin di default (username: admin, password: Admin123!)
- Creare categorie di esempio

---

## Fase 2: Implementazione JWT Authentication Backend

### 2.1 Configurazione JWT

#### Aggiungere pacchetti NuGet:
- `Microsoft.AspNetCore.Authentication.JwtBearer` (versione 9.x)

#### Configurazione in Program.cs:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)
            ),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Dopo app.UseCors()
app.UseAuthentication();
app.UseAuthorization();
```

### 2.2 Servizio JWT

Creare `Services/JwtService.cs`:
```csharp
public class JwtService
{
    private readonly IConfiguration _configuration;
    
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public (string AccessToken, string RefreshToken, DateTime AccessTokenExpiry, DateTime RefreshTokenExpiry) GenerateTokens(Utente utente, IEnumerable<string> ruoli)
    {
        // Generare Access Token (15 minuti)
        // Generare Refresh Token (7 giorni)
        // Restituire entrambi i token con le loro date di scadenza
    }
    
    public ClaimsPrincipal? ValidateToken(string token, bool isRefreshToken = false)
    {
        // Validare token e restituire ClaimsPrincipal
    }
}
```

### 2.3 DTO Autenticazione

Creare `DTO/Auth/AuthDTOs.cs`:
```csharp
public class LoginRequestDTO
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UtenteDTO Utente { get; set; } = null!;
}

public class RefreshTokenRequestDTO
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RegistrazioneRequestDTO
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Telefono { get; set; }
}

public class UtenteDTO
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public List<string> Ruoli { get; set; } = new();
}

public class ProfiloUtenteDTO
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Telefono { get; set; }
    public DateTime DataRegistrazione { get; set; }
    public List<string> Ruoli { get; set; } = new();
    public List<ProiezioneSalvataDTO> ProiezioniSalvate { get; set; } = new();
}

public class ProiezioneSalvataDTO
{
    public int Id { get; set; }
    public int ProiezioneId { get; set; }
    public string FilmTitolo { get; set; } = string.Empty;
    public string CinemaNome { get; set; } = string.Empty;
    public DateTime DataProiezione { get; set; }
    public TimeSpan OraProiezione { get; set; }
    public DateTime DataSalvataggio { get; set; }
    public bool Prenotato { get; set; }
    public int NumeroPosti { get; set; }
}

public class SalvaProiezioneRequestDTO
{
    public int ProiezioneId { get; set; }
}

public class PrenotazioneRequestDTO
{
    public int ProiezioneSalvataId { get; set; }
    public int NumeroPosti { get; set; }
}
```

---

## Fase 3: Implementazione RBAC

### 3.1 Attributi Autorizzazione Custom

Creare `Authorization/RolesAttribute.cs`:
```csharp
public class RolesAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    
    public RolesAttribute(params string[] roles)
    {
        _roles = roles;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Verificare se l'utente ha almeno uno dei ruoli richiesti
    }
}
```

### 3.2 Policy di Autorizzazione

In `Program.cs`:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("PowerUserOrAdmin", policy => policy.RequireRole("Admin", "PowerUser"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});
```

### 3.3 Ruoli e Permessi

#### Admin:
- Può fare tutto (CRUD su tutte le entità)
- Può gestire utenti e ruoli
- Accesso completo all'area amministrativa

#### PowerUser:
- CRUD su Film, Proiezioni, Registi
- **Solo Read** su Cinema (non può creare, modificare o eliminare)
- Accesso all'area amministrativa

#### User (Utente Autenticato):
- Può salvare proiezioni nell'area personale
- Può effettuare prenotazioni virtuali
- Può visualizzare le proiezioni in corso
- **NON** può accedere all'area amministrativa
- **NON** vede il bottone di accesso all'area admin

#### Utente Non Autenticato:
- Può visualizzare index.html
- Può visualizzare proiezioni in corso (solo lettura)
- Se tenta di prenotare → redirect a login
- Se tenta di accedere ad aree protette → redirect a login

---

## Fase 4: Endpoints Autenticazione

### 4.1 AuthEndpoints

Creare `Endpoints/AuthEndpoints.cs`:

```csharp
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");
        
        // POST /auth/login
        group.MapPost("/login", async (LoginRequestDTO request, FilmDbContext db, JwtService jwtService) =>
        {
            // Verificare credenziali con BCrypt
            // Generare token
            // Restituire token e dati utente
        });
        
        // POST /auth/refresh
        group.MapPost("/refresh", async (RefreshTokenRequestDTO request, FilmDbContext db, JwtService jwtService) =>
        {
            // Validare refresh token
            // Generare nuovi token
        });
        
        // POST /auth/logout
        group.MapPost("/logout", [Authorize] async (HttpContext context, FilmDbContext db) =>
        {
            // Invalidare refresh token (opzionale: blacklist)
        });
        
        // POST /auth/register
        group.MapPost("/register", async (RegistrazioneRequestDTO request, FilmDbContext db) =>
        {
            // Validare input
            // Verificare username/email unici
            // Hash password con BCrypt
            // Creare utente con ruolo "User"
        });
        
        // GET /auth/me
        group.MapGet("/me", [Authorize] async (HttpContext context, FilmDbContext db) =>
        {
            // Restituire dati utente corrente
        });
        
        return app;
    }
}
```

### 4.2 UserEndpoints (Area Personale)

Creare `Endpoints/UserEndpoints.cs`:

```csharp
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/user");
        
        // GET /user/profile - Profilo utente corrente
        group.MapGet("/profile", [Authorize] async (HttpContext context, FilmDbContext db) =>
        {
            // Restituire ProfiloUtenteDTO con proiezioni salvate
        });
        
        // PUT /user/profile - Aggiorna profilo
        group.MapPut("/profile", [Authorize] async (HttpContext context, UpdateProfiloRequestDTO request, FilmDbContext db) =>
        {
            // Aggiornare nome, cognome, telefono, email
        });
        
        // GET /user/proiezioni-salvate
        group.MapGet("/proiezioni-salvate", [Authorize] async (HttpContext context, FilmDbContext db) =>
        {
            // Restituire lista proiezioni salvate dall'utente
        });
        
        // POST /user/proiezioni-salvate
        group.MapPost("/proiezioni-salvate", [Authorize] async (HttpContext context, SalvaProiezioneRequestDTO request, FilmDbContext db) =>
        {
            // Salvare proiezione nell'area personale
        });
        
        // DELETE /user/proiezioni-salvate/{id}
        group.MapDelete("/proiezioni-salvate/{id}", [Authorize] async (int id, HttpContext context, FilmDbContext db) =>
        {
            // Rimuovere proiezione salvata
        });
        
        // POST /user/prenota
        group.MapPost("/prenota", [Authorize] async (HttpContext context, PrenotazioneRequestDTO request, FilmDbContext db) =>
        {
            // Creare prenotazione virtuale per proiezione salvata
        });
        
        return app;
    }
}
```

### 4.3 AdminEndpoints (Gestione Utenti)

Creare `Endpoints/AdminEndpoints.cs`:

```csharp
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin").RequireAuthorization("AdminOnly");
        
        // GET /admin/users
        group.MapGet("/users", async (FilmDbContext db) =>
        {
            // Lista tutti gli utenti
        });
        
        // GET /admin/users/{id}
        group.MapGet("/users/{id}", async (int id, FilmDbContext db) =>
        {
            // Dettagli utente specifico
        });
        
        // PUT /admin/users/{id}/roles
        group.MapPut("/users/{id}/roles", async (int id, UpdateRuoliRequestDTO request, FilmDbContext db) =>
        {
            // Aggiornare ruoli utente
        });
        
        // DELETE /admin/users/{id}
        group.MapDelete("/users/{id}", async (int id, FilmDbContext db) =>
        {
            // Disattivare utente (soft delete)
        });
        
        return app;
    }
}
```

---

## Fase 5: Protezione API Esistenti

### 5.1 Proteggere Endpoints Esistenti

Modificare gli endpoint esistenti:

#### RegistiEndpoints:
```csharp
// GET /registi - [AllowAnonymous] (visibile a tutti)
// POST /registi - [Authorize(Roles = "Admin,PowerUser")]
// PUT /registi/{id} - [Authorize(Roles = "Admin,PowerUser")]
// DELETE /registi/{id} - [Authorize(Roles = "Admin,PowerUser")]
```

#### FilmsEndpoints:
```csharp
// GET /films - [AllowAnonymous] (visibile a tutti)
// GET /films/{id} - [AllowAnonymous]
// POST /films - [Authorize(Roles = "Admin,PowerUser")]
// PUT /films/{id} - [Authorize(Roles = "Admin,PowerUser")]
// DELETE /films/{id} - [Authorize(Roles = "Admin,PowerUser")]
```

#### CinemasEndpoints:
```csharp
// GET /cinemas - [AllowAnonymous]
// GET /cinemas/{id} - [AllowAnonymous]
// POST /cinemas - [Authorize(Roles = "Admin")] // Solo Admin!
// PUT /cinemas/{id} - [Authorize(Roles = "Admin")]
// DELETE /cinemas/{id} - [Authorize(Roles = "Admin")]
```

#### ProiezioniEndpoints:
```csharp
// GET /proiezioni - [AllowAnonymous] (proiezioni in corso visibili a tutti)
// GET /proiezioni/{id} - [AllowAnonymous]
// POST /proiezioni - [Authorize(Roles = "Admin,PowerUser")]
// PUT /proiezioni/{id} - [Authorize(Roles = "Admin,PowerUser")]
// DELETE /proiezioni/{id} - [Authorize(Roles = "Admin,PowerUser")]
```

---

## Fase 6: Implementazione Categorie

### 6.1 DTO Categorie

Creare `DTO/CategoriaDTO.cs`:
```csharp
public class CategoriaDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
}

public class CategoriaCreateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
}

public class FilmWithCategorieDTO : FilmDTO
{
    public List<CategoriaDTO> Categorie { get; set; } = new();
}

public class FilmCreateWithCategorieDTO
{
    // Tutti i campi di FilmCreateDTO
    public List<int> CategoriaIds { get; set; } = new();
}
```

### 6.2 CategorieEndpoints

Creare `Endpoints/CategorieEndpoints.cs`:
```csharp
public static class CategorieEndpoints
{
    public static IEndpointRouteBuilder MapCategorieEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categorie");
        
        // GET /categorie - Lista categorie [AllowAnonymous]
        // GET /categorie/{id} - Dettaglio categoria [AllowAnonymous]
        // POST /categorie - Crea categoria [Authorize(Roles = "Admin")]
        // PUT /categorie/{id} - Aggiorna categoria [Authorize(Roles = "Admin")]
        // DELETE /categorie/{id} - Elimina categoria [Authorize(Roles = "Admin")]
        // GET /categorie/{id}/films - Film di una categoria [AllowAnonymous]
        
        return app;
    }
}
```

### 6.3 Aggiornare FilmsEndpoints

Modificare gli endpoint Film per gestire le categorie:
```csharp
// POST /films - Accettare anche lista di CategoriaIds
// PUT /films/{id} - Aggiornare anche le categorie associate
```

---

## Fase 7: Frontend - Autenticazione

### 7.1 Nuove Pagine HTML

#### login.html
- Form di login (username/email, password)
- Link a registrazione
- Validazione client-side
- Reindirizzamento dopo login

#### register.html
- Form di registrazione (username, email, password, conferma password, nome, cognome)
- Validazione password (min 8 caratteri, maiuscole, minuscole, numeri)
- Link a login

#### profilo.html (Area Personale)
- Dati utente (nome, cognome, email, telefono)
- Form per modifica profilo
- Lista proiezioni salvate
- Prenotazioni effettuate
- Possibilità di rimuovere proiezioni salvate
- Prenotazione virtuale (selezione numero posti)

#### proiezioni-pubblico.html
- Visualizzazione proiezioni in corso (solo lettura)
- Pulsante "Prenota" che redireziona a login se non autenticato
- Se autenticato: redireziona a pagina di prenotazione

### 7.2 Moduli JavaScript Autenticazione

#### js/auth.js
```javascript
const Auth = {
    // Gestione token in localStorage/sessionStorage
    accessToken: null,
    refreshToken: null,
    tokenExpiry: null,
    
    async login(username, password) {
        // Chiamata POST /auth/login
        // Salvare token in localStorage
        // Salvare dati utente
        // Avviare timer per refresh automatico
    },
    
    async refresh() {
        // Chiamata POST /auth/refresh
        // Aggiornare token
    },
    
    logout() {
        // Chiamata POST /auth/logout
        // Rimovere token da localStorage
        // Redirect a login
    },
    
    isAuthenticated() {
        // Verificare se token esiste e non è scaduto
    },
    
    getUser() {
        // Restituire dati utente decodificati dal token
    },
    
    hasRole(role) {
        // Verificare se utente ha un ruolo specifico
    },
    
    getAuthHeaders() {
        // Restituire headers con Authorization: Bearer {token}
    }
};
```

#### Aggiornamento api-client.js
Modificare tutte le chiamate API per includere header Authorization:
```javascript
async get(endpoint) {
    const headers = Auth.getAuthHeaders();
    const response = await fetch(`${this.baseUrl}${endpoint}`, { headers });
    
    if (response.status === 401) {
        // Tentare refresh token
        // Se refresh fallisce, redirect a login
    }
    // ...
}
```

### 7.3 Middleware Protezione Rotte

Creare `js/route-guard.js`:
```javascript
const RouteGuard = {
    checkAuth(requiredRoles = []) {
        if (!Auth.isAuthenticated()) {
            window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname);
            return false;
        }
        
        if (requiredRoles.length > 0) {
            const hasRole = requiredRoles.some(role => Auth.hasRole(role));
            if (!hasRole) {
                window.location.href = '/index.html';
                return false;
            }
        }
        
        return true;
    },
    
    hideAdminElements() {
        // Nascondere elementi admin se utente non è admin
        const user = Auth.getUser();
        if (!user || !user.ruoli.includes('Admin')) {
            document.querySelectorAll('[data-role="admin"]').forEach(el => el.style.display = 'none');
        }
    }
};

// Eseguire su ogni pagina:
document.addEventListener('DOMContentLoaded', () => {
    // Nascondere elementi in base ai ruoli
    RouteGuard.hideAdminElements();
});
```

### 7.4 Aggiornamento Componenti Esistenti

#### sidebar.html
- Aggiungere voce "Area Personale" (visibile solo a utenti autenticati)
- Nascondere voci admin se utente non ha ruolo Admin/PowerUser
- Mostrare logout se autenticato

#### navbar.html
- Mostrare nome utente e avatar se autenticato
- Mostrare link a login se non autenticato
- Dropdown con logout
- Nascondere link admin se non autorizzato

---

## Fase 8: Configurazione Ambiente

### 8.1 Aggiornamento .env
```
# JWT Configuration
JWT_SECRET_KEY=your-super-secret-key-min-32-characters
JWT_ISSUER=FilmAPI
JWT_AUDIENCE=FilmFrontend
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7
```

### 8.2 appsettings.json
```json
{
  "Jwt": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "${JWT_ISSUER}",
    "Audience": "${JWT_AUDIENCE}",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

---

## Fase 9: Task Sequenziali di Sviluppo

### Task 9.1: Database e Migration
- [ ] Creare entità Utente, Ruolo, UtenteRuolo, Categoria, FilmCategoria, ProiezioneSalvata
- [ ] Aggiornare FilmDbContext con nuove entità
- [ ] Creare migration `AddAuthAndCategories`
- [ ] Implementare seed dati (ruoli, admin, categorie)

### Task 9.2: Backend - JWT Service
- [ ] Installare pacchetto JwtBearer
- [ ] Implementare JwtService
- [ ] Configurare JWT in Program.cs
- [ ] Aggiungere UseAuthentication e UseAuthorization

### Task 9.3: Backend - Auth Endpoints
- [ ] Implementare AuthEndpoints (login, logout, refresh, register, me)
- [ ] Implementare password hashing con BCrypt
- [ ] Testare endpoint con Swagger

### Task 9.4: Backend - RBAC
- [ ] Definire policies di autorizzazione
- [ ] Proteggere endpoint esistenti con RequireAuthorization
- [ ] Implementare AdminEndpoints
- [ ] Implementare UserEndpoints (area personale)

### Task 9.5: Backend - Categorie
- [ ] Implementare CategorieEndpoints
- [ ] Aggiornare FilmsEndpoints per gestire categorie
- [ ] Aggiornare DTO Film per includere categorie

### Task 9.6: Frontend - Auth Module
- [ ] Creare auth.js con gestione token
- [ ] Creare login.html e register.html
- [ ] Aggiornare api-client.js con headers Authorization
- [ ] Implementare auto-refresh token

### Task 9.7: Frontend - Area Personale
- [ ] Creare profilo.html
- [ ] Implementare visualizzazione/modifica dati utente
- [ ] Implementare salvataggio proiezioni
- [ ] Implementare lista proiezioni salvate
- [ ] Implementare prenotazione virtuale

### Task 9.8: Frontend - Protezione Rotte
- [ ] Creare route-guard.js
- [ ] Aggiornare sidebar.html (nascondere voci in base ai ruoli)
- [ ] Aggiornare navbar.html (login/logout, nome utente)
- [ ] Aggiungere protezione alle pagine admin

### Task 9.9: Frontend - Categorie
- [ ] Aggiornare forms Film per selezione categorie
- [ ] Visualizzare categorie nelle card film
- [ ] Implementare filtro per categoria

### Task 9.10: Testing e Verifica
- [ ] Testare tutti i ruoli (admin, power user, user)
- [ ] Verificare protezione API
- [ ] Verificare redirect frontend
- [ ] Testare refresh token
- [ ] Testare prenotazioni virtuali

---

## Fase 10: Verifica Finale

### Checklist Sicurezza:
- [ ] API protette con JWT
- [ ] RBAC funzionante (admin, power user, user)
- [ ] Password hashate con BCrypt
- [ ] Refresh token implementato
- [ ] CORS configurato correttamente
- [ ] Endpoint sensibili protetti

### Checklist Frontend:
- [ ] Login/logout funzionante
- [ ] Token salvati in localStorage
- [ ] Auto-refresh token implementato
- [ ] Redirect automatico se non autorizzato
- [ ] Area personale accessibile solo a utenti autenticati
- [ ] Area admin nascosta a utenti non admin
- [ ] Pagine cinemas.html, dashboard.html, proiezioni.html, registi.html protette
- [ ] Categorie visualizzabili e filtrabili
- [ ] Prenotazioni virtuali funzionanti

### Checklist Database:
- [ ] Tabelle Utenti, Ruoli, Categorie create
- [ ] Relazioni molti-a-molti funzionanti
- [ ] Seed dati presenti (admin, categorie)
- [ ] Indici unique su Username e Email

---

## Note Tecniche

### Sicurezza Password:
- Usare BCrypt per hashing password
- Minimo 8 caratteri
- Richiedere: maiuscole, minuscole, numeri
- Non loggare mai password in chiaro

### Gestione Token:
- Access Token: 15 minuti di durata
- Refresh Token: 7 giorni di durata
- Salvare in localStorage (o httpOnly cookie per maggiore sicurezza in futuro)
- Implementare auto-refresh prima della scadenza

### Frontend Security:
- Validare input lato client e server
- Sanitizzare dati prima di visualizzarli (XSS prevention)
- Non memorizzare password in localStorage
- Usare HTTPS in produzione

### Database:
- Soft delete per utenti (campo Attivo)
- Cascade delete per proiezioni salvate quando eliminato utente
- Indici su campi di ricerca frequenti (Email, Username)

---

## Diagramma dei Ruoli

```
┌─────────────────────────────────────────────────────────────────┐
│                          ADMIN                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  - CRUD completo su tutte le entità                    │   │
│  │  - Gestione utenti e ruoli                             │   │
│  │  - Accesso a tutte le pagine admin                     │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       POWER USER                                │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  - CRUD su Film, Registi, Proiezioni                   │   │
│  │  - READ ONLY su Cinema                                 │   │
│  │  - Accesso area admin (senza gestione utenti)          │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     UTENTE AUTENTICATO                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  - Area personale con dati e proiezioni salvate        │   │
│  │  - Prenotazioni virtuali                               │   │
│  │  - Visualizza proiezioni in corso                      │   │
│  │  - NO accesso area admin                               │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    NON AUTENTICATO                              │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  - Visualizza index.html                                │   │
│  │  - Visualizza proiezioni (solo lettura)                │   │
│  │  - Redirect a login se tenta prenotazione              │   │
│  │  - Redirect a login se tenta accesso aree protette       │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## File da Creare/Modificare

### Backend:
- [ ] `Model/Utente.cs`
- [ ] `Model/Ruolo.cs`
- [ ] `Model/UtenteRuolo.cs`
- [ ] `Model/Categoria.cs`
- [ ] `Model/FilmCategoria.cs`
- [ ] `Model/ProiezioneSalvata.cs`
- [ ] `Services/JwtService.cs`
- [ ] `DTO/Auth/AuthDTOs.cs`
- [ ] `DTO/CategoriaDTO.cs`
- [ ] `DTO/UtenteDTO.cs`
- [ ] `Endpoints/AuthEndpoints.cs`
- [ ] `Endpoints/UserEndpoints.cs`
- [ ] `Endpoints/AdminEndpoints.cs`
- [ ] `Endpoints/CategorieEndpoints.cs`
- [ ] `Data/Migrations/*` (migration)
- [ ] `Program.cs` (aggiornare)

### Frontend:
- [ ] `wwwroot/login.html`
- [ ] `wwwroot/register.html`
- [ ] `wwwroot/profilo.html`
- [ ] `wwwroot/proiezioni-pubblico.html`
- [ ] `wwwroot/js/auth.js`
- [ ] `wwwroot/js/route-guard.js`
- [ ] `wwwroot/js/pages/profilo.js`
- [ ] `wwwroot/js/pages/login.js`
- [ ] `wwwroot/js/pages/register.js`
- [ ] `wwwroot/components/sidebar.html` (aggiornare)
- [ ] `wwwroot/components/navbar.html` (aggiornare)
- [ ] `wwwroot/js/api-client.js` (aggiornare)
- [ ] `wwwroot/films.html` (aggiornare per categorie)
- [ ] `wwwroot/index.html` (aggiornare)

### Configurazione:
- [ ] `.env` (aggiornare con JWT config)
- [ ] `appsettings.json` (aggiornare)