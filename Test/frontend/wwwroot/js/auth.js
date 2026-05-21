// DOC: auth - file del progetto; contiene logica specifica della feature/modulo.
/* DOC: Modulo JS 'auth': utility/comportamenti condivisi per autenticazione, routing, tema e API client. */
const ApiConfigAdapter = window.ApiConfig || {
    /* DOC-FN: 'getCandidates' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getCandidates() {
        return [
            'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io'
        ];
    },
    /* DOC-FN: 'persistBaseUrl' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    persistBaseUrl(value) {
        if (!value) return;
        window.localStorage.setItem('apiBaseUrl', value);
    }
};

let API_URL = ApiConfigAdapter.getCandidates()[0];

/* DOC-FN: 'getApiCandidates' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function getApiCandidates() {
    const all = ApiConfigAdapter.getCandidates();
    return [API_URL, ...all.filter(x => x !== API_URL)];
}

/* DOC-FN: 'fetchWithApiFallback' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function fetchWithApiFallback(path, options = {}) {
    let response = null;
    let lastError = null;

    /* DOC-FN: 'for' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    for (const candidate of getApiCandidates()) {
        try {
            response = await fetch(`${candidate}${path}`, options);
            API_URL = candidate;
            ApiConfigAdapter.persistBaseUrl(candidate);
            break;
        } catch (err) {
            lastError = err;
        }
    }

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!response) {
        throw lastError || new Error('Impossibile raggiungere il server API');
    }

    return response;
}

const Auth = {
    sessionUserKey: 'authUserState',
    sessionAuthKey: 'authSessionState',
    accessToken: null,
    refreshToken: null,
    tokenExpiry: null,
    user: null,
    initPromise: null,
    autoRefreshTimeoutId: null,
    refreshPromise: null,
    sessionVersion: 0,

    /* DOC-FN: 'hasStoredSessionHint' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    hasStoredSessionHint() {
        return !!(
            window.localStorage.getItem(this.sessionAuthKey) ||
            window.localStorage.getItem(this.sessionUserKey) ||
            window.sessionStorage.getItem(this.sessionAuthKey) ||
            window.sessionStorage.getItem(this.sessionUserKey)
        );
    },

    /* DOC-FN: 'saveSession' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    saveSession(data) {
        this.sessionVersion += 1;
        this.accessToken = data.accessToken;
        this.refreshToken = null;
        this.tokenExpiry = new Date(data.expiresAt);
        this.user = data.utente;
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (typeof window !== 'undefined' && window.ApiClient && typeof window.ApiClient.clearCache === 'function') {
            window.ApiClient.clearCache();
        }
        this.persistAuthSession();
        this.persistUserSession();

        this.scheduleAutoRefresh();
    },

    /* DOC-FN: 'clearSession' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    clearSession() {
        this.sessionVersion += 1;
        this.accessToken = null;
        this.refreshToken = null;
        this.tokenExpiry = null;
        this.user = null;
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (typeof window !== 'undefined' && window.ApiClient && typeof window.ApiClient.clearCache === 'function') {
            window.ApiClient.clearCache();
        }
        this.clearAuthSession();
        this.clearUserSession();

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (this.autoRefreshTimeoutId) {
            clearTimeout(this.autoRefreshTimeoutId);
            this.autoRefreshTimeoutId = null;
        }

    },

    /* DOC-FN: 'hydrateUserSession' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    hydrateUserSession() {
        if (this.user) return;

        try {
            const raw = window.localStorage.getItem(this.sessionUserKey) || window.sessionStorage.getItem(this.sessionUserKey);
            if (!raw) return;

            const parsed = JSON.parse(raw);
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (parsed && typeof parsed === 'object') {
                this.user = parsed;
            }
        } catch {
            this.clearUserSession();
        }
    },

    /* DOC-FN: 'hydrateAuthSession' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    hydrateAuthSession() {
        if (this.accessToken && this.tokenExpiry) return;

        try {
            const raw = window.localStorage.getItem(this.sessionAuthKey) || window.sessionStorage.getItem(this.sessionAuthKey);
            if (!raw) return;
            const parsed = JSON.parse(raw);
            if (!parsed || typeof parsed !== 'object') return;

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (typeof parsed.accessToken === 'string' && parsed.accessToken.trim()) {
                this.accessToken = parsed.accessToken;
            }
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (typeof parsed.tokenExpiry === 'string' && parsed.tokenExpiry.trim()) {
                this.tokenExpiry = new Date(parsed.tokenExpiry);
            }
        } catch {
            this.clearAuthSession();
        }
    },

    /* DOC-FN: 'persistUserSession' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    persistUserSession() {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!this.user) {
            this.clearUserSession();
            return;
        }

        window.localStorage.setItem(this.sessionUserKey, JSON.stringify(this.user));
    },

    /* DOC-FN: 'persistAuthSession' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    persistAuthSession() {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!this.accessToken || !this.tokenExpiry) {
            this.clearAuthSession();
            return;
        }

        window.localStorage.setItem(this.sessionAuthKey, JSON.stringify({
            accessToken: this.accessToken,
            tokenExpiry: this.tokenExpiry instanceof Date ? this.tokenExpiry.toISOString() : this.tokenExpiry
        }));
    },

    /* DOC-FN: 'clearUserSession' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    clearUserSession() {
        window.localStorage.removeItem(this.sessionUserKey);
    },

    /* DOC-FN: 'clearAuthSession' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    clearAuthSession() {
        window.localStorage.removeItem(this.sessionAuthKey);
    },

    /* DOC-FN: 'scheduleAutoRefresh' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    scheduleAutoRefresh() {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (this.autoRefreshTimeoutId) {
            clearTimeout(this.autoRefreshTimeoutId);
            this.autoRefreshTimeoutId = null;
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!this.tokenExpiry) {
            return;
        }

        const expiryTime = new Date(this.tokenExpiry).getTime();
        const now = Date.now();
        const timeUntilRefresh = expiryTime - now - (60 * 1000);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (timeUntilRefresh <= 0) {
            return;
        }

        this.autoRefreshTimeoutId = setTimeout(() => {
            this.refresh({ silent: true });
        }, timeUntilRefresh);
    },

    async login(username, password) {
        try {
            const response = await fetchWithApiFallback('/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password }),
                credentials: 'include'
            });

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
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
            this.saveSession(data);

            return { success: true, user: data.utente };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async register(userData) {
        try {
            const response = await fetchWithApiFallback('/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(userData)
            });

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
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

    async forgotPassword(email, returnUrl) {
        try {
            const response = await fetchWithApiFallback('/auth/forgot-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, returnUrl })
            });

            const payload = await response.json().catch(() => ({}));
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (!response.ok) {
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (response.status === 404) {
                    throw new Error('Funzione reset password non disponibile sul server attuale: riavvia il backend aggiornato.');
                }
                throw new Error(payload.message || 'Errore durante la richiesta reset password');
            }

            return { success: true, message: payload.message || 'Controlla la tua email per il link di reset.' };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async recoverAccount(email, returnUrl) {
        try {
            const response = await fetchWithApiFallback('/auth/recover-account', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, returnUrl })
            });

            const payload = await response.json().catch(() => ({}));
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (!response.ok) {
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (response.status === 404) {
                    throw new Error('Funzione recupero account non disponibile sul server attuale: riavvia il backend aggiornato.');
                }
                throw new Error(payload.message || 'Errore durante la richiesta recupero account');
            }

            return { success: true, message: payload.message || 'Controlla la tua email per recuperare l\'account.' };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async completeAccountRecovery(token, newPassword) {
        try {
            const response = await fetchWithApiFallback('/auth/recover-account/complete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ token, newPassword })
            });

            const payload = await response.json().catch(() => ({}));
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (!response.ok) {
                throw new Error(payload.message || 'Errore durante il recupero account');
            }

            return { success: true, message: payload.message, username: payload.username };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async resetPassword(token, newPassword) {
        try {
            const response = await fetchWithApiFallback('/auth/reset-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ token, newPassword })
            });

            const payload = await response.json().catch(() => ({}));
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (!response.ok) {
                throw new Error(payload.message || 'Errore durante il reset password');
            }

            return { success: true, message: payload.message || 'Password aggiornata con successo' };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async changePassword(currentPassword, newPassword) {
        try {
            const response = await fetchWithApiFallback('/auth/change-password', {
                method: 'POST',
                headers: this.getAuthHeaders(),
                body: JSON.stringify({ currentPassword, newPassword }),
                credentials: 'include'
            });

            const payload = await response.json().catch(() => ({}));
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (!response.ok) {
                throw new Error(payload.message || 'Errore durante il cambio password');
            }

            return { success: true, message: payload.message || 'Password aggiornata con successo' };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async getExternalProviders() {
        const response = await fetchWithApiFallback('/auth/external/providers', {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        });

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!response.ok) {
            throw new Error('Impossibile caricare i provider di accesso');
        }

        return await response.json();
    },

    async startExternalLogin(provider, returnUrl) {
        const response = await fetchWithApiFallback(
            `/auth/external/${encodeURIComponent(provider)}/start?returnUrl=${encodeURIComponent(returnUrl)}`,
            {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            }
        );

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!response.ok) {
            let message = 'Provider esterno non disponibile';
            try {
                const error = await response.json();
                message = error.message || message;
            } catch {
                // fallback su messaggio di default
            }
            throw new Error(message);
        }

        const payload = await response.json();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!payload.redirectUrl) {
            throw new Error('Redirect OAuth non valido');
        }

        window.location.href = payload.redirectUrl;
    },

    async completeExternalLogin(provider, authCode) {
        const response = await fetchWithApiFallback('/auth/external/complete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ provider, authCode }),
            credentials: 'include'
        });

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!response.ok) {
            let message = 'Completamento login esterno fallito';
            try {
                const error = await response.json();
                message = error.message || message;
            } catch {
                // fallback su messaggio di default
            }
            throw new Error(message);
        }

        const data = await response.json();
        this.saveSession(data);
        return { success: true, user: data.utente };
    },

    async refresh(options = {}) {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (this.refreshPromise) {
            return this.refreshPromise;
        }

        const silent = !!options.silent;
        const startedSessionVersion = this.sessionVersion;
        this.refreshPromise = (async () => {
            try {
                const response = await fetchWithApiFallback('/auth/refresh', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ refreshToken: '' }),
                    credentials: 'include'
                });

                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (!response.ok) {
                    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                    if (this.sessionVersion === startedSessionVersion) {
                        this.clearSession();
                        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                        if (!silent) {
                            window.location.href = '/login.html';
                        }
                    }
                    throw new Error('Sessione scaduta');
                }

                const data = await response.json();
                this.saveSession(data);
                return true;
            } catch (error) {
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (this.sessionVersion === startedSessionVersion) {
                    this.clearSession();
                    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                    if (!silent) {
                        window.location.href = '/login.html';
                    }
                }
                return false;
            }
        })();

        try {
            return await this.refreshPromise;
        } finally {
            this.refreshPromise = null;
        }
    },

    /* DOC-FN: 'ensureInitialized' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    ensureInitialized() {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!this.initPromise) {
            this.hydrateAuthSession();
            this.hydrateUserSession();
            const hasValidToken = this.isAuthenticated() && !!this.user;
            const hasStoredSessionHint = this.hasStoredSessionHint();

            // Evita un refresh di rete all'avvio per utenti anonimi:
            // riduce attese percepite su pagine pubbliche.
            const initAction = hasValidToken
                ? Promise.resolve(true)
                : (hasStoredSessionHint ? this.refresh({ silent: true }) : Promise.resolve(false));

            this.initPromise = initAction.finally(() => {
                window.dispatchEvent(new CustomEvent('auth:ready', {
                    detail: {
                        authenticated: this.isAuthenticated()
                    }
                }));
            });
        }

        return this.initPromise;
    },

    async logout() {
        try {
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (this.accessToken) {
                await fetchWithApiFallback('/auth/logout', {
                    method: 'POST',
                    headers: { 'Authorization': `Bearer ${this.accessToken}` },
                    credentials: 'include'
                });
            }
        } catch (e) {
            console.log('Errore logout:', e);
        }

        this.clearSession();

        window.location.href = '/login.html';
    },

    /* DOC-FN: 'isAuthenticated' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    isAuthenticated() {
        if (!this.accessToken || !this.tokenExpiry) return false;
        return new Date() < new Date(this.tokenExpiry);
    },

    async ensureToken() {
        this.hydrateAuthSession();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!this.isAuthenticated()) {
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (!this.hasStoredSessionHint()) {
                return false;
            }
            return await this.refresh({ silent: true });
        }
        return this.isAuthenticated();
    },

    /* DOC-FN: 'getAuthHeaders' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getAuthHeaders() {
        const headers = {
            'Content-Type': 'application/json'
        };
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (this.accessToken) {
            headers['Authorization'] = `Bearer ${this.accessToken}`;
        }
        return headers;
    },

    /* DOC-FN: 'hasRole' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    hasRole(role) {
        if (!this.user || !this.user.ruoli) return false;
        return this.user.ruoli.includes(role);
    },

    /* DOC-FN: 'isAdmin' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    isAdmin() {
        return this.hasRole('Admin');
    },

    /* DOC-FN: 'isPowerUser' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    isPowerUser() {
        return this.hasRole('Admin') || this.hasRole('PowerUser');
    },

    /* DOC-FN: 'canManageCatalog' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    canManageCatalog() {
        return this.hasRole('Admin') || this.hasRole('PowerUser');
    },

    /* DOC-FN: 'getUser' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getUser() {
        return this.user;
    }
};

window.Auth = Auth;

// Inizializza controllo token all'avvio
document.addEventListener('DOMContentLoaded', () => {
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (Auth.tokenExpiry && new Date(Auth.tokenExpiry) <= new Date()) {
        Auth.clearSession();
    }

    // Aggiungi event listener per logout
    const logoutBtn = document.getElementById('logout-btn');
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            Auth.logout();
        });
    }

    Auth.ensureInitialized();
});


