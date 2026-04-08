const ApiClient = {
    baseUrl: window.localStorage.getItem('apiBaseUrl') || 'http://localhost:5000',

    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        
        // Aggiungi header Authorization se l'utente è autenticato
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };
        
        if (Auth.accessToken) {
            headers['Authorization'] = `Bearer ${Auth.accessToken}`;
        }
        
        const config = {
            headers,
            ...options
        };

        if (config.body && typeof config.body === 'object') {
            config.body = JSON.stringify(config.body);
        }

        try {
            let response = await fetch(url, config);

            // Se token scaduto, prova a fare refresh
            if (response.status === 401 && Auth.refreshToken) {
                const refreshed = await Auth.refresh();
                if (refreshed) {
                    // Riprova la richiesta con il nuovo token
                    headers['Authorization'] = `Bearer ${Auth.accessToken}`;
                    response = await fetch(url, config);
                } else {
                    // Refresh fallito, redirect a login
                    window.location.href = '/login.html';
                    throw new Error('Sessione scaduta');
                }
            }

            if (response.status === 401) {
                // Non autenticato, redirect a login se necessario
                if (!endpoint.includes('/auth/login') && !endpoint.includes('/auth/register')) {
                    window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname);
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
