# Piano di lavoro - FilmAPI Frontend (Iterazione 2)

## Panoramica

Questa iterazione implementa il frontend dell'applicazione FilmAPI. Il frontend sarà una Web App sviluppata con HTML, CSS e JavaScript vanilla con Tailwind CSS, servita da un'applicazione ASP.NET Core Minimal API dedicata che espone i file statici.

### Stack Tecnologico
- **Frontend**: HTML5, CSS3, JavaScript (ES6+), Tailwind CSS (CDN)
- **Server Frontend**: ASP.NET Core Minimal API
- **Comunicazione**: Fetch API verso il Backend API
- **Font**: Google Fonts (Manrope, Inter)
- **Icone**: Material Symbols Outlined (Google)

### Design System
Il design è ispirato ai mock presenti nella cartella `stitch/stitch/` e include:
- **Tema**: Dark mode con sfondo `#131313`
- **Colori primari**: Rosso cinema `#E50914` (primary-container), Oro `#E9C176` (secondary)
- **Font**: Manrope per titoli, Inter per corpo testo
- **Layout**: Sidebar sinistra + TopBar + Content area

---

## 1) Struttura Progetto Frontend

### 1.1 Creazione Progetto
- Creare un nuovo progetto ASP.NET Core Empty chiamato `FilmFrontend` nella soluzione
- Posizionarlo nella stessa solution di `FilmAPI`
- Configurare `.NET 9`

### 1.2 Struttura Cartelle wwwroot
```
wwwroot/
├── assets/                       # Immagini, favicon, etc.
│   ├── favicon.ico
│   └── images/                    # Immagini di default e placeholder
├── components/                   # Componenti HTML riutilizzabili
│   ├── navbar.html               # TopNavBar con search e profilo
│   ├── sidebar.html              # SideNavBar con navigazione
│   └── footer.html                # Footer comune
├── css/                          # Fogli di stile CSS
│   └── styles.css                 # Stili personalizzati (se necessari)
├── js/                           # Script JavaScript
│   ├── api-client.js             # Client per chiamate API backend
│   ├── template-loader.js        # Script per caricare componenti HTML
│   ├── utils.js                  # Utility comuni (formattazione, notifiche)
│   ├── navbar.js                 # Logica comune navbar e sidebar
│   └── pages/                    # Script specifici per pagina
│       ├── index.js
│       ├── registi.js
│       ├── films.js
│       ├── cinemas.js
│       └── proiezioni.js
├── index.html                    # Home/Dashboard
├── registi.html                  # Gestione registi
├── films.html                    # Gestione film
├── cinemas.html                  # Gestione cinema
└── proiezioni.html               # Gestione proiezioni
```

---

## 2) Configurazione Server Frontend

### 2.1 Program.cs
- Configurare `UseFileServer` per servire file statici
- Abilitare `defaultFiles` per servire `index.html` come default
- Configurare CORS per permettere chiamate al Backend API
- Abilitare fallback alle route SPA (`FallbackToFile("index.html")`)

### 2.2 appsettings.json
Configurare:
- `BackendApiUrl`: URL del backend API (es. `https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io`)
- `FrontendPort`: porta del server frontend (es. `5001`)

### 2.3 CORS Policy
- Configurare policy CORS con nome `"AllowFrontend"`
- Permettere origin specifiche del frontend
- Permettere metodi HTTP: GET, POST, PUT, DELETE
- Permettere headers: Content-Type, Authorization

---

## 3) Design System e Layout

### 3.1 Colori (Tailwind Config)
```javascript
tailwind.config = {
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        "primary-container": "#e50914",      // Rosso cinema
        "secondary": "#e9c176",              // Oro
        "background": "#131313",              // Sfondo principale
        "surface": "#131313",
        "surface-container": "#201f1f",
        "surface-container-low": "#1c1b1b",
        "surface-container-high": "#2a2a2a",
        "on-surface": "#e5e2e1",              // Testo principale
        "on-surface-variant": "#e9bcb6",     // Testo secondario
        "outline-variant": "#5e3f3b",
        "tertiary-container": "#0076c5",
      },
      fontFamily: {
        "headline": ["Manrope"],
        "body": ["Inter"],
      }
    }
  }
}
```

### 3.2 Layout Base
Ogni pagina segue la struttura:
```
┌─────────────────────────────────────────────────────┐
│ TopNavBar (search, notifiche, profilo)              │
├──────────┬──────────────────────────────────────────┤
│          │                                          │
│ SideNav  │           Main Content                  │
│ (64px)   │                                          │
│          │   - Hero Section (solo home)             │
│ - Home   │   - Statistiche Card                     │
│ - Film   │   - Film in programmazione               │
│ - Registi│   - Tabelle CRUD                         │
│ - Cinema │                                          │
│ - Proiez.│                                          │
│          │                                          │
├──────────┴──────────────────────────────────────────┤
│ Footer                                              │
└─────────────────────────────────────────────────────┘
```

### 3.3 Componenti Riutilizzabili

#### SideNavBar (sidebar.html)
```html
<aside class="h-screen w-64 sticky top-0 bg-[#201F1F]">
  <!-- Logo/Titolo -->
  <div>
    <h1>Director's Suite</h1>
    <p>Backstage Access</p>
  </div>
  
  <!-- Navigation -->
  <nav>
    <a href="index.html">Dashboard</a>
    <a href="films.html">Film</a>
    <a href="registi.html">Registi</a>
    <a href="cinemas.html">Cinema</a>
    <a href="proiezioni.html">Proiezioni</a>
  </nav>
  
  <!-- Footer Nav -->
  <div>
    <a href="#">Help</a>
    <a href="#">Logout</a> <!-- Mock per ora -->
  </div>
</aside>
```

#### TopNavBar (navbar.html)
```html
<header class="bg-[#1C1B1B] px-8 py-4 sticky top-0 z-50">
  <div>
    <span>The Cinematic Director's Suite</span>
  </div>
  <div>
    <!-- Search Bar -->
    <input placeholder="Search..." />
    
    <!-- Actions -->
    <button>notifications</button>
    <button>settings</button>
    <img src="avatar-placeholder.jpg" /> <!-- Mock avatar -->
  </div>
</header>
```

#### Footer (footer.html)
```html
<footer class="bg-[#131313] py-6 px-12 border-t">
  <p>© 2024 Cinematic Director's Suite. All rights reserved.</p>
  <div>
    <a href="#">Support</a>
    <a href="#">Privacy Policy</a>
    <a href="#">Terms of Service</a>
  </div>
</footer>
```

---

## 4) Pagine Principali

### 4.1 index.html - Home/Dashboard

**Struttura** (basata su `dashboard_director_s_suite/code.html`):

```
┌─────────────────────────────────────────────────────┐
│ TopNavBar                                           │
├──────────┬──────────────────────────────────────────┤
│          │ Hero Section                             │
│ SideNav  │ "Director's Dashboard"                   │
│          │ + sottotitolo                            │
│          ├──────────────────────────────────────────┤
│          │ Statistics Grid (Bento Style)            │
│          │ ┌────────┐ ┌────────┐ ┌────────┐ ┌────┐  │
│          │ │ Films  │ │Registi │ │ Cinema │ │Pro.│  │
│          │ │ 1,482  │ │  342   │ │   56   │ │124 │  │
│          │ └────────┘ └────────┘ └────────┘ └────┘  │
│          ├──────────────────────────────────────────┤
│          │ Film in Programmazione (Card Grid)       │
│          │ ┌──────────┐ ┌──────────┐                │
│          │ │ Film 1   │ │ Film 2   │                │
│          │ │ Copertina│ │ Copertina│                │
│          │ │ Info     │ │ Info     │                │
│          │ └──────────┘ └──────────┘                │
│          ├──────────────────────────────────────────┤
│          │ Quick Actions (sidebar destra)           │
│          └──────────────────────────────────────────┤
│ Footer                                              │
└─────────────────────────────────────────────────────┘
```

**Elementi:**
- **Hero Section**: Titolo "Director's Dashboard" con sottotitolo descrittivo
- **Statistiche Grid (Bento)**: 
  - Card "Total Films" - conteggio da `GET /films`
  - Card "Total Directors" - conteggio da `GET /registi`
  - Card "Total Cinemas" - conteggio da `GET /cinemas`
  - Card "Today's Projections" (evidenziata rosso) - conteggio da `GET /proiezioni`
- **Film in Programmazione**: Grid di card con:
  - Immagine copertina
  - Genere (badge colorato)
  - Durata
  - Titolo film
  - Nome regista
  - Hover effect con zoom immagine
- **Quick Actions** (sidebar destra):
  - "Add new Film"
  - "Schedule Projection"
  - "Register Director"
- **Upcoming Projections**: Tabella con orari

**Endpoint utilizzati:**
- `GET /films` - lista film con copertine
- `GET /registi` - conteggio e nomi
- `GET /cinemas` - conteggio
- `GET /proiezioni` - conteggio e lista prossime

### 4.2 registi.html - Gestione Registi

**Struttura** (basata su `gestione_registi/code.html`):

```
Header con titolo "Director Registry" + pulsante "Add New Director"

Bento Grid Layout:
┌────────────┬─────────────────────────────────────────┐
│ Stats      │ Data Table                                 │
│ ┌────────┐ │ ID | Nome | Cognome | Nazionalità | Azioni │
│ │ Total  │ │ DIR-001 | Quentin | Tarantino | US   │ ... │
│ │ Auteurs│ │ DIR-042 | Christopher| Nolan | UK    │ ... │
│ │ 142    │ │ ...     │ ...     │ ...      │ ...  │ ... │
│ └────────┘ │                                         │
│ ┌────────┐ │ Pagination                              │
│ │ Global │ │ < 1 2 3 ... >                          │
│ │Diversity│└─────────────────────────────────────────┘
│ │ Nations│
│ └────────┘│
└────────────┴─────────────────────────────────────────┘

Director Spotlight (sezione opzionale con film in evidenza)
Footer
```

**Funzionalità:**
- Tabella con:
  - Colonna ID formattato (es. "DIR-001")
  - Badge per nazionalità
  - Azioni (edit, delete) visibili su hover
- Pagination
- Form creazione/modifica in modal
- Eliminazione con conferma dialog

**Endpoint utilizzati:**
- `GET /registi` - lista con paginazione
- `POST /registi` - creazione
- `PUT /registi/{id}` - modifica
- `DELETE /registi/{id}` - eliminazione

### 4.3 films.html - Gestione Film

**Struttura** (basata su `gestione_film/code.html`):

```
Header con titolo "Film Management" + pulsante "Add New Film"

Filters Section:
┌─────────────────────────────────────────────────────┐
│ [🔍 Search by Title or ID...] [Director ▼] [Year ▼]│
└─────────────────────────────────────────────────────┘

Data Table:
┌─────────────────────────────────────────────────────┐
│ ID | Titolo | DataProduzione | RegistaId | Durata | Actions │
│ #FLM-9021 | Oppenheimer | July 21, 2023 | CN-001 | 180 min │
│ #FLM-4432 | Dune: Part Two | Mar 1, 2024 | DV-022 │ 166 min │
└─────────────────────────────────────────────────────┘

Pagination
Footer
```

**Funzionalità:**
- Tabella con:
  - ID formattato (es. "#FLM-9021")
  - Titolo con badge produttore
  - DataProduzione formattata
  - RegistaId con badge colorato
  - Durata formattata (es. "180 min")
- Filtri: ricerca testo, dropdown regista, dropdown anno
- Form creazione/modifica con:
  - Dropdown Regista (popolato da API)
  - DatePicker per DataProduzione
  - Input numerico per Durata
  - Input path Copertina (opzionale con default)
  - Input path Filmato (opzionale)
- Modal per edit/delete

**Endpoint utilizzati:**
- `GET /films` - lista con filtri opzionali
- `GET /registi` - per dropdown
- `POST /films`
- `PUT /films/{id}`
- `DELETE /films/{id}`

### 4.4 cinemas.html - Gestione Cinema

**Struttura** (basata su `cinema_e_proiezioni/code.html`):

```
Header "Operations Control"
├── Section: Cinemas Inventory
│   ├── Titolo + pulsante "Add New Cinema"
│   └── Cinema List (Table-less layout)
│       ID | Nome | Indirizzo | Città | Actions
│       #C-801 | Grand Rex Luminaire | Via delle Rose, 12 | Milan, IT
│
├── Section: Proiezioni Associate (opzionale)
│
Footer
```

**Funzionalità:**
- Layout tabella con hover effects
- Form creazione/modifica cinema
- Eliminazione con verifica proiezioni associate

**Endpoint utilizzati:**
- `GET /cinemas`
- `POST /cinemas`
- `PUT /cinemas/{id}`
- `DELETE /cinemas/{id}`

### 4.5 proiezioni.html - Gestione Proiezioni

**Struttura** (basata su `cinema_e_proiezioni/code.html`):

```
Header "Operations Control"
├── Section: Active Projections
│   ├── Titolo + pulsante "Schedule Projection"
│   └── Projection Cards (Grid)
│       ┌──────────────────┐ ┌──────────────────┐
│       │ Now Showing       │ │ Scheduled        │
│       │ Film: Interstellar│ │ Film: Budapest   │
│       │ Cinema: Grand Rex │ │ Cinema: Onyx     │
│       │ Date: Dec 24      │ │ Date: Dec 25     │
│       │ Time: 20:30       │ │ Time: 18:00      │
│       └──────────────────┘ └──────────────────┘
│
Footer
```

**Funzionalità:**
- Card view per proiezioni con:
  - Badge stato (Now Showing / Scheduled / Premiere)
  - Nome Film
  - Nome Cinema
  - Data e Ora
  - Azioni su hover
- Form creazione/modifica con:
  - Dropdown Film (popolato da API)
  - Dropdown Cinema (popolato da API)
  - DatePicker per data
  - TimePicker per ora
- Verifica conflitti orari (vincolo unique)

**Endpoint utilizzati:**
- `GET /proiezioni` - con informazioni film e cinema
- `GET /films` - per dropdown
- `GET /cinemas` - per dropdown
- `POST /proiezioni`
- `PUT /proiezioni/{id}`
- `DELETE /proiezioni/{id}`

---

## 5) Moduli JavaScript

### 5.1 api-client.js
```javascript
const ApiClient = {
    baseUrl: 'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io',
    
    async get(endpoint) {
        const response = await fetch(`${this.baseUrl}${endpoint}`);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.json();
    },
    
    async post(endpoint, data) {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.json();
    },
    
    async put(endpoint, data) {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.json();
    },
    
    async delete(endpoint) {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'DELETE'
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.status === 204 ? null : response.json();
    }
};
```

### 5.2 utils.js
```javascript
const Utils = {
    formatDate(dateString) {
        return new Date(dateString).toLocaleDateString('it-IT', {
            year: 'numeric', month: 'long', day: 'numeric'
        });
    },
    
    formatTime(timeString) {
        return timeString; // Già nel formato HH:mm
    },
    
    showNotification(message, type = 'info') {
        // Toast notification con Tailwind
        const toast = document.createElement('div');
        toast.className = `fixed bottom-4 right-4 px-6 py-3 rounded-xl shadow-xl z-50
            ${type === 'error' ? 'bg-error-container text-white' : 
              type === 'success' ? 'bg-tertiary-container text-white' : 
              'bg-surface-container-high text-on-surface'}`;
        toast.textContent = message;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 3000);
    },
    
    confirmDialog(message) {
        return window.confirm(message);
    }
};
```

### 5.3 template-loader.js
```javascript
async function loadComponent(elementId, componentPath) {
    try {
        const response = await fetch(componentPath);
        if (!response.ok) throw new Error(`Failed to load ${componentPath}`);
        const html = await response.text();
        document.getElementById(elementId).innerHTML = html;
    } catch (error) {
        console.error('Error loading component:', error);
    }
}

// Esempio utilizzo in ogni pagina:
// document.addEventListener('DOMContentLoaded', () => {
//     loadComponent('sidebar-container', '/components/sidebar.html');
//     loadComponent('navbar-container', '/components/navbar.html');
//     loadComponent('footer-container', '/components/footer.html');
// });
```

### 5.4 navbar.js
```javascript
// Gestione sidebar e navbar comune
function initNavigation() {
    // Highlight voce menu attiva
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('nav a');
    
    navLinks.forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('text-[#E50914]', 'bg-[#2A2A2A]');
            link.classList.remove('text-on-surface-variant');
        }
    });
    
    // Mobile menu toggle
    const menuToggle = document.getElementById('mobile-menu-toggle');
    const sidebar = document.getElementById('sidebar');
    
    if (menuToggle) {
        menuToggle.addEventListener('click', () => {
            sidebar.classList.toggle('hidden');
        });
    }
}

// Mock logout (non implementato)
function logout() {
    Utils.showNotification('Logout non implementato', 'info');
}
```

---

## 6) Stili CSS e Tailwind

### 6.1 Setup Tailwind (CDN)
Ogni pagina include nell'`<head>`:
```html
<script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
<link href="https://fonts.googleapis.com/css2?family=Manrope:wght@200;400;500;700;800&family=Inter:wght@300;400;500;600&display=swap" rel="stylesheet"/>
<link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&display=swap" rel="stylesheet"/>
```

### 6.2 Config Tailwind Inline
```html
<script>
tailwind.config = {
    darkMode: "class",
    theme: {
        extend: {
            colors: {
                "primary-container": "#e50914",
                "secondary": "#e9c176",
                "background": "#131313",
                "surface": "#131313",
                "surface-container": "#201f1f",
                // ... altri colori
            },
            fontFamily: {
                "headline": ["Manrope"],
                "body": ["Inter"],
            }
        }
    }
}
</script>
```

### 6.3 Stili Globali Personalizzati (styles.css)
```css
/* Classes personalizzate non coperte da Tailwind */
.material-symbols-outlined {
    font-variation-settings: 'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}

body {
    background-color: #131313;
    color: #e5e2e1;
    font-family: 'Inter', sans-serif;
}

.glass-panel {
    background: rgba(53, 53, 52, 0.6);
    backdrop-filter: blur(20px);
}

.primary-gradient {
    background: linear-gradient(135deg, #e50914 0%, #c0000c 100%);
}

/* Animazioni */
.group:hover .group-hover\:opacity-100 {
    opacity: 1;
}

/* Scrollbar personalizzata */
::-webkit-scrollbar {
    width: 8px;
}
::-webkit-scrollbar-track {
    background: #201f1f;
}
::-webkit-scrollbar-thumb {
    background: #5e3f3b;
    border-radius: 4px;
}
```

---

## 7) Mockup di Riferimento

I file di riferimento per l'aspetto grafico si trovano in:
- `stitch/stitch/dashboard_director_s_suite/` - Home/Dashboard
- `stitch/stitch/gestione_registi/` - Pagina registi
- `stitch/stitch/gestione_film/` - Pagina film
- `stitch/stitch/cinema_e_proiezioni/` - Pagina cinema e proiezioni

Ogni file contiene:
- `code.html` - Codice HTML completo del mock
- `screen.png` - Anteprima visuale

---

## 8) Autenticazione (Posticipata)

### 8.1 Stato Attuale
- **Login non implementato** in questa iterazione
- Possibilità di:
  - Mock login (avatar placeholder statico)
  - Nessun login (UI senza autenticazione)

### 8.2 Preparazione per Future Implementazioni
- Placeholder per pulsante "Logout" che mostra notification di funzionalità non disponibile
- Avatar placeholder statico nella TopNavBar
- Struttura preparata per aggiungere:
  - Pagina `login.html`
  - Modulo `auth.js` per gestione token
  - Intercettori fetch per Authorization header

---

## 9) Gestione Errori e Validazioni

### 9.1 Validazioni Frontend
- Campi obbligatori con indicazione visiva
- Formato date (DatePicker nativo HTML5)
- Formato orari (TimePicker nativo HTML5)
- Verifica FK esistenti (dropdown popolati da API)
- Messaggi di errore inline sotto i campi

### 9.2 Gestione Errori API
- Toast notification per errori (rosso per errori, verde per successo, blu per info)
- Loading states durante chiamate (spinner o skeleton)
- Retry automatico per errori di rete (max 3 tentativi)
- Gestione codici HTTP:
  - 200/201: Successo
  - 400: Errore validazione → mostrare messaggio
  - 404: Non trovato → redirect o messaggio
  - 409: Conflitto (es. proiezione duplicata)

---

## 10) Configurazione Ambiente

### 10.1 Variabili Ambiente
Creare file `.env` nel progetto Frontend:
```
BACKEND_API_URL=https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io
FRONTEND_PORT=5001
```

### 10.2 Configurazione CORS Backend
Il backend deve permettere richieste dal frontend:
```csharp
// In Program.cs del Backend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("AllowFrontend");
```

---

## 11) Attività di Sviluppo Sequenziali

### Fase 1: Setup Progetto
- [ ] Creare progetto FilmFrontend nella solution
- [ ] Configurare Program.cs con middleware file statici
- [ ] Configurare CORS
- [ ] Creare struttura cartelle wwwroot

### Fase 2: Design System Base
- [ ] Creare template base con Tailwind CDN
- [ ] Definire variabili colori e font
- [ ] Creare `styles.css` con classi personalizzate

### Fase 3: Componenti HTML
- [ ] Creare `components/sidebar.html`
- [ ] Creare `components/navbar.html`
- [ ] Creare `components/footer.html`
- [ ] Implementare `template-loader.js`
- [ ] Implementare `navbar.js`

### Fase 4: Moduli JavaScript Core
- [ ] Implementare `api-client.js`
- [ ] Implementare `utils.js`
- [ ] Testare connessione Backend

### Fase 5: Pagine CRUD
- [ ] Implementare `index.html` (Dashboard)
- [ ] Implementare `registi.html`
- [ ] Implementare `films.html`
- [ ] Implementare `cinemas.html`
- [ ] Implementare `proiezioni.html`

### Fase 6: Gestione State e UX
- [ ] Implementare notifiche toast
- [ ] Implementare modals per create/edit
- [ ] Implementare conferma eliminazione
- [ ] Implementare loading states

### Fase 7: Testing e Rifinitura
- [ ] Test manuale tutte le pagine
- [ ] Test responsive design (mobile, tablet, desktop)
- [ ] Test cross-browser (Chrome, Firefox, Edge)
- [ ] Ottimizzazione performance

---

## 12) Verifica Finale

- [ ] Avvio Backend API su porta 5000
- [ ] Avvio Frontend Server su porta 5001
- [ ] Verifica comunicazione CORS
- [ ] Test completo CRUD su tutte le pagine
- [ ] Verifica validazioni client-side
- [ ] Verifica messaggi di errore
- [ ] Verifica responsive design
- [ ] Verifica consistenza con mock grafici

---

## 13) Note Tecniche

### 13.1 Tailwind CSS
- Utilizzato via CDN per semplicità
- Config inline in ogni pagina
- Possibilità di migrazione a Tailwind compilato in futuro

### 13.2 Material Symbols
- Icone caricate da Google Fonts
- Utilizzo: `<span class="material-symbols-outlined">icon_name</span>`
- Icone comuni: dashboard, movie, theaters, schedule, person, edit, delete, add, search

### 13.3 Mobile Navigation
- Sidebar nascosta su mobile (< 768px)
- Bottom navigation bar visibile su mobile
- Hamburger menu per sidebar toggle

### 13.4 Performance
- Lazy loading immagini copertina
- Paginazione client-side per tabelle grandi
- Debounce su search input
- Cache locale per dropdown FK (registi, cinema)
