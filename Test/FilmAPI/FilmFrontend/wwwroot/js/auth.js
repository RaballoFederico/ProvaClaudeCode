const AZURE_API_BASE_URL = 'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io';

function getApiCandidates() {
    const stored = window.localStorage.getItem('apiBaseUrl');
    const defaults = [AZURE_API_BASE_URL, 'http://localhost:5000', 'https://localhost:7217'];
    if (!stored || stored === 'undefined' || stored === 'null') {
        return defaults;
    }
    return [stored, ...defaults.filter(url => url !== stored)];
}

let API_URL = getApiCandidates()[0];

const Auth = {
    accessToken: localStorage.getItem('accessToken'),
    refreshToken: localStorage.getItem('refreshToken'),
    tokenExpiry: localStorage.getItem('tokenExpiry') ? new Date(localStorage.getItem('tokenExpiry')) : null,
    user: JSON.parse(localStorage.getItem('user') || 'null'),

    async login(username, password) {
        try {
            let response = null;
            let lastError = null;

            for (const candidate of getApiCandidates()) {
                try {
                    response = await fetch(`${candidate}/auth/login`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ username, password })
                    });
                    API_URL = candidate;
                    localStorage.setItem('apiBaseUrl', candidate);
                    break;
                } catch (err) {
                    lastError = err;
                }
            }

            if (!response) {
                throw lastError || new Error('Impossibile raggiungere il server API');
            }

            if (!response.ok) {
                let message = 'Credenziali non valide';
                try {
                    const error = await response.json();
                    message = error.message || message;
                } catch {
                    // fallback su messaggio di default
                }
                throw new Error(message);
            }

            const data = await response.json();

            // Salva token
            this.accessToken = data.accessToken;
            this.refreshToken = data.refreshToken;
            this.tokenExpiry = new Date(data.expiresAt);
            this.user = data.utente;

            localStorage.setItem('accessToken', data.accessToken);
            localStorage.setItem('refreshToken', data.refreshToken);
            localStorage.setItem('tokenExpiry', data.expiresAt);
            localStorage.setItem('user', JSON.stringify(data.utente));

            return { success: true, user: data.utente };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async register(userData) {
        try {
            const response = await fetch(`${API_URL}/auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(userData)
            });

            if (!response.ok) {
                let message = 'Errore durante la registrazione';
                try {
                    const error = await response.json();
                    message = error.message || message;
                } catch {
                    // fallback su messaggio di default
                }
                throw new Error(message);
            }

            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async refresh() {
        try {
            if (!this.refreshToken) {
                throw new Error('Nessun refresh token');
            }

            const response = await fetch(`${API_URL}/auth/refresh`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: this.refreshToken })
            });

            if (!response.ok) {
                this.logout();
                throw new Error('Sessione scaduta');
            }

            const data = await response.json();

            this.accessToken = data.accessToken;
            this.refreshToken = data.refreshToken;
            this.tokenExpiry = new Date(data.expiresAt);
            this.user = data.utente;

            localStorage.setItem('accessToken', data.accessToken);
            localStorage.setItem('refreshToken', data.refreshToken);
            localStorage.setItem('tokenExpiry', data.expiresAt);
            localStorage.setItem('user', JSON.stringify(data.utente));

            return true;
        } catch (error) {
            this.logout();
            return false;
        }
    },

    async logout() {
        try {
            if (this.accessToken) {
                await fetch(`${API_URL}/auth/logout`, {
                    method: 'POST',
                    headers: { 'Authorization': `Bearer ${this.accessToken}` }
                });
            }
        } catch (e) {
            console.log('Errore logout:', e);
        }

        this.accessToken = null;
        this.refreshToken = null;
        this.tokenExpiry = null;
        this.user = null;

        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('tokenExpiry');
        localStorage.removeItem('user');

        window.location.href = '/login.html';
    },

    isAuthenticated() {
        if (!this.accessToken || !this.tokenExpiry) return false;
        return new Date() < new Date(this.tokenExpiry);
    },

    async ensureToken() {
        if (!this.isAuthenticated() && this.refreshToken) {
            return await this.refresh();
        }
        return this.isAuthenticated();
    },

    getAuthHeaders() {
        const headers = {
            'Content-Type': 'application/json'
        };
        if (this.accessToken) {
            headers['Authorization'] = `Bearer ${this.accessToken}`;
        }
        return headers;
    },

    hasRole(role) {
        if (!this.user || !this.user.ruoli) return false;
        return this.user.ruoli.includes(role);
    },

    isAdmin() {
        return this.hasRole('Admin');
    },

    isPowerUser() {
        return this.hasRole('Admin') || this.hasRole('PowerUser');
    },

    canManageCatalog() {
        return this.hasRole('Admin') || this.hasRole('PowerUser');
    },

    getUser() {
        return this.user;
    }
};

// Inizializza controllo token all'avvio
document.addEventListener('DOMContentLoaded', () => {
    // Aggiungi event listener per logout
    const logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            Auth.logout();
        });
    }

    // Setup auto-refresh token (5 minuti prima della scadenza)
    if (Auth.tokenExpiry) {
        const expiryTime = new Date(Auth.tokenExpiry).getTime();
        const now = Date.now();
        const timeUntilRefresh = expiryTime - now - (5 * 60 * 1000); // 5 minuti prima

        if (timeUntilRefresh > 0) {
            setTimeout(() => {
                Auth.refresh();
            }, timeUntilRefresh);
        }
    }
});
