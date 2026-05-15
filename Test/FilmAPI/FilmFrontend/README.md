# FilmAPI - Documentazione Tecnica

## Panoramica dell'Architettura

L'applicazione FilmAPI è composta da due componenti principali che comunicano tramite HTTP REST:

```
┌─────────────────────┐         HTTP/REST          ┌─────────────────────┐
│                     │  ◄──────────────────────►  │                     │
│   FilmFrontend      │       Fetch API            │     FilmAPI         │
│   (ASP.NET Core)    │                            │   (ASP.NET Core)     │
│   Porta 5001       │                            │   Porta 5000         │
│                     │                            │                     │
│   - HTML/CSS/JS     │                            │   - Minimal API      │
│   - Tailwind CSS    │                            │   - MariaDB          │
│   - File Statici    │                            │   - Entity Framework │
└─────────────────────┘                            └─────────────────────┘
```

---

## 1. Struttura del Progetto

### 1.1 Backend (FilmAPI)
```
FilmAPI/
├── Program.cs              # Configurazione app e endpoints
├── Model/                  # Entità EF Core
│   ├── Regista.cs
│   ├── Film.cs
│   ├── Cinema.cs
│   └── Proiezione.cs
├── Data/                  # DbContext
│   └── FilmDbContext.cs
├── DTO/                   # Data Transfer Objects
├── Endpoints/             # Endpoint API REST
│   ├── RegistiEndpoints.cs
│   ├── FilmsEndpoints.cs
│   ├── CinemasEndpoints.cs
│   └── ProiezioniEndpoints.cs
└── .env                   # Configurazione database
```

### 1.2 Frontend (FilmFrontend)
```
FilmFrontend/
├── Program.cs              # Server static files
├── wwwroot/
│   ├── index.html          # Dashboard
│   ├── films.html          # Gestione Film
│   ├── registi.html       # Gestione Registi
│   ├── cinemas.html        # Gestione Cinema
│   ├── proiezioni.html     # Gestione Proiezioni
│   ├── components/        # Componenti riutilizzabili
│   │   ├── navbar.html
│   │   ├── sidebar.html
│   │   └── footer.html
│   ├── css/
│   │   └── styles.css     # Stili personalizzati
│   └── js/
│       ├── api-client.js  # Client API
│       ├── utils.js       # Utility
│       └── template-loader.js  # Caricamento componenti
└── FilmFrontend.csproj
```

---

## 2. Comunicazione Backend-Frontend

### 2.1 Configurazione CORS

Il backend deve permettere richieste dal frontend tramite CORS.

**File: `FilmAPI/Program.cs`**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowFrontend");
```

### 2.2 Client API (Frontend)

Il frontend utilizza `api-client.js` per le chiamate HTTP.

**File: `FilmFrontend/wwwroot/js/api-client.js`**
```javascript
const ApiClient = {
    // URL base del backend
    baseUrl: 'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io',

    // Metodi CRUD generici
    async get(endpoint) { ... },
    async post(endpoint, data) { ... },
    async put(endpoint, data) { ... },
    async delete(endpoint) { ... }
};
```

### 2.3 Esempio di Chiamata

```javascript
// Ottenere tutti i film
const films = await ApiClient.get('/films');

// Creare un nuovo film
await ApiClient.post('/films', {
    titolo: "Inception",
    registaId: 1,
    dataProduzione: "2010-07-16",
    durata: 148
});

// Aggiornare un film
await ApiClient.put('/films/1', {
    titolo: "Inception",
    durata: 150
});

// Eliminare un film
await ApiClient.delete('/films/1');
```

---

## 3. Endpoint API REST

### 3.1 Registi
| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| GET | `/registi` | Lista tutti i registi |
| GET | `/registi/{id}` | Ottiene un regista specifico |
| POST | `/registi` | Crea un nuovo regista |
| PUT | `/registi/{id}` | Aggiorna un regista |
| DELETE | `/registi/{id}` | Elimina un regista |

### 3.2 Film
| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| GET | `/films` | Lista tutti i film |
| GET | `/films/{id}` | Ottiene un film specifico |
| POST | `/films` | Crea un nuovo film |
| PUT | `/films/{id}` | Aggiorna un film |
| DELETE | `/films/{id}` | Elimina un film |

### 3.3 Cinema
| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| GET | `/cinemas` | Lista tutti i cinema |
| GET | `/cinemas/{id}` | Ottiene un cinema specifico |
| POST | `/cinemas` | Crea un nuovo cinema |
| PUT | `/cinemas/{id}` | Aggiorna un cinema |
| DELETE | `/cinemas/{id}` | Elimina un cinema |

### 3.4 Proiezioni
| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| GET | `/proiezioni` | Lista tutte le proiezioni |
| GET | `/proiezioni/{id}` | Ottiene una proiezione specifica |
| POST | `/proiezioni` | Crea una nuova proiezione |
| PUT | `/proiezioni/{id}` | Aggiorna una proiezione |
| DELETE | `/proiezioni/{id}` | Elimina una proiezione |

---

## 4. Formato dei Dati

### 4.1 Request (POST/PUT)
```json
// POST /registi
{
    "nome": "Christopher",
    "cognome": "Nolan",
    "nazionalita": "Regno Unito"
}

// POST /films
{
    "titolo": "Inception",
    "dataProduzione": "2010-07-16",
    "registaId": 1,
    "durata": 148,
    "copertinaPath": "/media/inception.jpg",
    "filmatoPath": null
}

// POST /cinemas
{
    "nome": "Cinema Odeon",
    "indirizzo": "Via Roma 10",
    "citta": "Milano"
}

// POST /proiezioni
{
    "cinemaId": 1,
    "filmId": 1,
    "data": "2026-03-20",
    "ora": "20:00"
}
```

### 4.2 Response
```json
// GET /registi
[
    {
        "id": 1,
        "nome": "Christopher",
        "cognome": "Nolan",
        "nazionalita": "Regno Unito"
    }
]

// GET /films
[
    {
        "id": 1,
        "titolo": "Inception",
        "dataProduzione": "2010-07-16T00:00:00",
        "registaId": 1,
        "durata": 148,
        "copertinaPath": "/media/inception.jpg",
        "filmatoPath": null
    }
]
```

---

## 5. Gestione Errori

### 5.1 Codici HTTP

| Codice | Significato |
|--------|-------------|
| 200 | OK - Richiesta completata |
| 201 | Created - Risorsa creata |
| 204 | No Content - Eliminazione completata |
| 400 | Bad Request - Dati non validi |
| 404 | Not Found - Risorsa non trovata |
| 409 | Conflict - Violazione vincoli (es. proiezione duplicata) |

### 5.2 Gestione Errori nel Frontend

```javascript
async function loadData() {
    try {
        const films = await ApiClient.get('/films');
        renderFilms(films);
    } catch (error) {
        // Mostra notifica errore
        Utils.showNotification('Errore: ' + error.message, 'error');
    }
}
```

---

## 6. Avvio dell'Applicazione

### 6.1 Prerequisiti
- .NET 9 SDK
- MariaDB (locale o Docker)

### 6.2 Avvio Backend
```bash
cd FilmAPI
dotnet run --urls "https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io"
```

### 6.3 Avvio Frontend
```bash
cd FilmFrontend
dotnet run --urls "https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io"
```

### 6.4 Avvio con Docker (Database)
```bash
cd FilmAPI
docker-compose up -d  # Avvia MariaDB
dotnet ef database update  # Applica migrazioni
dotnet run
```

---

## 7. Configurazione

### 7.1 Backend (.env)
```
DB_HOST=filmhub-db.internal.delightfuldune-f7916078.francecentral.azurecontainerapps.io
DB_PORT=3306
DB_NAME=filmapi_db
DB_USER=root
DB_PASSWORD=password
DEFAULT_COVER_IMAGE_PATH=/media/defaults/cover-default.jpg
```

### 7.2 Frontend (api-client.js)
```javascript
const ApiClient = {
    baseUrl: 'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io',  // URL del backend
    // ...
};
```

---

## 8. Dipendenze

### 8.1 Backend (NuGet)
- `Pomelo.EntityFrameworkCore.MySql` - Provider MySQL/MariaDB
- `Microsoft.AspNetCore.OpenApi` - Swagger/OpenAPI
- `NSwag.AspNetCore` - Documentazione API
- `DotNetEnv` - Caricamento variabili ambiente

### 8.2 Frontend (CDN)
- **Tailwind CSS**: Framework CSS utility-first
- **Google Fonts**: Manrope (titoli), Inter (corpo)
- **Material Symbols**: Icone

---

## 9. Struttura Database

### 9.1 Schema ER
```
Registi (1) ──────► (N) Film (1) ◄────── (N) Proiezioni (N) ►───── (1) Cinema
```

### 9.2 Tabelle
- **Registi**: id, nome, cognome, nazionalita
- **Film**: id, titolo, dataProduzione, registaId, durata, copertinaPath, filmatoPath
- **Cinema**: id, nome, indirizzo, citta
- **Proiezioni**: id, cinemaId, filmId, data, ora

### 9.3 Vincoli
- PK autoincrementale su tutte le tabelle
- FK: Regista → Film, Film → Proiezioni, Cinema → Proiezioni
- UNIQUE: (cinemaId, filmId, data, ora) su Proiezioni

---

## 10. Sicurezza

### 10.1 CORS
Il backend accetta richieste solo da `https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io` (frontend).

### 10.2 Best Practices
- Non memorizzare dati sensibili in localStorage
- Validazione sempre lato backend
- Prepared statements per query SQL (EF Core)

---

## 11. Troubleshooting

### 11.1 Problema: CORS Error
**Sintomo**: `Access-Control-Allow-Origin` missing
**Soluzione**: Verificare che `app.UseCors()` sia configurato in Program.cs

### 11.2 Problema: Connection Refused
**Sintomo**: Errore di connessione al backend
**Soluzione**: Verificare che il backend sia in esecuzione su porta 5000

### 11.3 Problema: Database Connection Failed
**Sintomo**: Errore connessione MariaDB
**Soluzione**: Verificare che MariaDB sia in esecuzione e le credenziali siano corrette nel .env

---

## 12. Sviluppo Futuro

### 12.1 Autenticazione
Prossime iterazioni potranno includere:
- Sistema di login
- JWT token
- Ruoli e permessi

### 12.2 Upload File
Possibilità di caricare immagini di copertina e filmati.

### 12.3 Ottimizzazioni
- Lazy loading per immagini
- Paginazione lato server
- Cache locale

