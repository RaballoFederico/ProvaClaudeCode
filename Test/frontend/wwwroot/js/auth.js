const ApiConfigAdapter = window.ApiConfig || {
    getCandidates() {
        return ['http://localhost:5001', 'http://127.0.0.1:5001', 'https://localhost:7217'];
    },
    persistBaseUrl(value) {
        if (!value) return;
        window.localStorage.setItem('apiBaseUrl', value);
    }
};

let API_URL = ApiConfigAdapter.getCandidates()[0];

function getApiCandidates() {
    const all = ApiConfigAdapter.getCandidates();
    return [API_URL, ...all.filter(x => x !== API_URL)];
}

async function fetchWithApiFallback(path, options = {}) {
    let response = null;
    let lastError = null;

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

    saveSession(data) {
        this.sessionVersion += 1;
        this.accessToken = data.accessToken;
        this.refreshToken = null;
        this.tokenExpiry = new Date(data.expiresAt);
        this.user = data.utente;
        this.persistAuthSession();
        this.persistUserSession();

        this.scheduleAutoRefresh();
    },

    clearSession() {
        this.sessionVersion += 1;
        this.accessToken = null;
        this.refreshToken = null;
        this.tokenExpiry = null;
        this.user = null;
        this.clearAuthSession();
        this.clearUserSession();

        if (this.autoRefreshTimeoutId) {
            clearTimeout(this.autoRefreshTimeoutId);
            this.autoRefreshTimeoutId = null;
        }

    },

    hydrateUserSession() {
        if (this.user) return;

        try {
            const raw = window.localStorage.getItem(this.sessionUserKey) || window.sessionStorage.getItem(this.sessionUserKey);
            if (!raw) return;

            const parsed = JSON.parse(raw);
            if (parsed && typeof parsed === 'object') {
                this.user = parsed;
            }
        } catch {
            this.clearUserSession();
        }
    },

    hydrateAuthSession() {
        if (this.accessToken && this.tokenExpiry) return;

        try {
            const raw = window.localStorage.getItem(this.sessionAuthKey) || window.sessionStorage.getItem(this.sessionAuthKey);
            if (!raw) return;
            const parsed = JSON.parse(raw);
            if (!parsed || typeof parsed !== 'object') return;

            if (typeof parsed.accessToken === 'string' && parsed.accessToken.trim()) {
                this.accessToken = parsed.accessToken;
            }
            if (typeof parsed.tokenExpiry === 'string' && parsed.tokenExpiry.trim()) {
                this.tokenExpiry = new Date(parsed.tokenExpiry);
            }
        } catch {
            this.clearAuthSession();
        }
    },

    persistUserSession() {
        if (!this.user) {
            this.clearUserSession();
            return;
        }

        window.localStorage.setItem(this.sessionUserKey, JSON.stringify(this.user));
    },

    persistAuthSession() {
        if (!this.accessToken || !this.tokenExpiry) {
            this.clearAuthSession();
            return;
        }

        window.localStorage.setItem(this.sessionAuthKey, JSON.stringify({
            accessToken: this.accessToken,
            tokenExpiry: this.tokenExpiry instanceof Date ? this.tokenExpiry.toISOString() : this.tokenExpiry
        }));
    },

    clearUserSession() {
        window.localStorage.removeItem(this.sessionUserKey);
    },

    clearAuthSession() {
        window.localStorage.removeItem(this.sessionAuthKey);
    },

    scheduleAutoRefresh() {
        if (this.autoRefreshTimeoutId) {
            clearTimeout(this.autoRefreshTimeoutId);
            this.autoRefreshTimeoutId = null;
        }

        if (!this.tokenExpiry) {
            return;
        }

        const expiryTime = new Date(this.tokenExpiry).getTime();
        const now = Date.now();
        const timeUntilRefresh = expiryTime - now - (60 * 1000);
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
            if (!response.ok) {
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
            if (!response.ok) {
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

                if (!response.ok) {
                    if (this.sessionVersion === startedSessionVersion) {
                        this.clearSession();
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
                if (this.sessionVersion === startedSessionVersion) {
                    this.clearSession();
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

    ensureInitialized() {
        if (!this.initPromise) {
            this.hydrateAuthSession();
            this.hydrateUserSession();
            const hasValidToken = this.isAuthenticated() && !!this.user;
            const initAction = hasValidToken
                ? Promise.resolve(true)
                : this.refresh({ silent: true });

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

    isAuthenticated() {
        if (!this.accessToken || !this.tokenExpiry) return false;
        return new Date() < new Date(this.tokenExpiry);
    },

    async ensureToken() {
        if (!this.isAuthenticated()) {
            return await this.refresh({ silent: true });
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

window.Auth = Auth;

// Inizializza controllo token all'avvio
document.addEventListener('DOMContentLoaded', () => {
    if (Auth.tokenExpiry && new Date(Auth.tokenExpiry) <= new Date()) {
        Auth.clearSession();
    }

    // Aggiungi event listener per logout
    const logoutBtn = document.getElementById('logout-btn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            Auth.logout();
        });
    }

    Auth.ensureInitialized();
});
