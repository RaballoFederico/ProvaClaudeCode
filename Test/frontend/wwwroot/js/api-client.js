const ApiConfigAdapter = window.ApiConfig || {
    getCandidates() {
        return ['http://localhost:5001', 'http://127.0.0.1:5001', 'https://localhost:7217'];
    },
    persistBaseUrl(value) {
        if (!value) return;
        window.localStorage.setItem('apiBaseUrl', value);
    },
    get defaultBaseUrl() {
        return this.getCandidates()[0];
    }
};

const ApiClient = {
    baseUrl: ApiConfigAdapter.defaultBaseUrl,

    getFallbackBaseUrls() {
        const fallbacks = ApiConfigAdapter.getCandidates();
        return [this.baseUrl, ...fallbacks.filter(url => url !== this.baseUrl)];
    },

    async request(endpoint, options = {}) {
        const auth = typeof window !== 'undefined' ? window.Auth : undefined;

        const isAuthEndpoint = endpoint.startsWith('/auth/');
        if (!isAuthEndpoint && auth && typeof auth.ensureToken === 'function') {
            await auth.ensureToken();
        }

        // Aggiungi header Authorization se l'utente è autenticato
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };
        
        if (auth && auth.accessToken) {
            headers['Authorization'] = `Bearer ${auth.accessToken}`;
        }
        
        const config = {
            credentials: 'include',
            headers,
            ...options
        };

        if (config.body && typeof config.body === 'object') {
            config.body = JSON.stringify(config.body);
        }

        try {
            let response = null;
            let activeBaseUrl = this.baseUrl;
            let fetchError = null;

            for (const candidate of this.getFallbackBaseUrls()) {
                try {
                    response = await fetch(`${candidate}${endpoint}`, config);
                    activeBaseUrl = candidate;
                    if (this.baseUrl !== candidate) {
                        this.baseUrl = candidate;
                        ApiConfigAdapter.persistBaseUrl(candidate);
                    }
                    break;
                } catch (err) {
                    fetchError = err;
                }
            }

            if (!response) {
                throw fetchError || new Error('Failed to fetch');
            }

            // Se token scaduto, prova a fare refresh (refresh token in cookie HttpOnly)
            if (response.status === 401 && !isAuthEndpoint && auth && typeof auth.refresh === 'function') {
                const refreshed = await auth.refresh({ silent: true });
                if (refreshed) {
                    // Riprova la richiesta con il nuovo token
                    headers['Authorization'] = auth.accessToken ? `Bearer ${auth.accessToken}` : headers['Authorization'];
                    response = await fetch(`${activeBaseUrl}${endpoint}`, config);
                } else {
                    // Refresh fallito, redirect a login
                    window.location.href = '/login.html';
                    throw new Error('Sessione scaduta');
                }
            }

            if (response.status === 401) {
                // Non autenticato, redirect a login se necessario
                if (!endpoint.includes('/auth/login') && !endpoint.includes('/auth/register')) {
                    window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search);
                }
                throw new Error('Non autenticato');
            }

            if (response.status === 403) {
                throw new Error('Accesso negato - Permessi insufficienti');
            }

            if (response.status === 204) {
                return null;
            }

            let data = null;
            const contentType = response.headers.get('content-type') || '';
            if (contentType.includes('application/json')) {
                data = await response.json();
            }

            if (!response.ok) {
                throw new Error(data?.message || `HTTP ${response.status}`);
            }

            return data;
        } catch (error) {
            console.error('API Error:', error);
            if (error instanceof TypeError && error.message && error.message.includes('Failed to fetch')) {
                throw new Error(`Impossibile raggiungere API (${this.baseUrl}). Verifica che il backend sia avviato e CORS configurato.`);
            }
            throw error;
        }
    },

    async get(endpoint) {
        return this.request(endpoint, { method: 'GET' });
    },

    async post(endpoint, data) {
        return this.request(endpoint, { method: 'POST', body: data });
    },

    async put(endpoint, data) {
        return this.request(endpoint, { method: 'PUT', body: data });
    },

    async delete(endpoint) {
        return this.request(endpoint, { method: 'DELETE' });
    }
};

window.ApiClient = ApiClient;
window.APIClient = ApiClient;
window.dispatchEvent(new CustomEvent('apiclient:ready'));
