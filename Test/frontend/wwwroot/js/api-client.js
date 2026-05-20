/* DOC: Modulo JS 'api-client': utility/comportamenti condivisi per autenticazione, routing, tema e API client. */
(() => {
/* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
if (window.__apiClientInitialized) {
    return;
}
window.__apiClientInitialized = true;

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
    },
    get defaultBaseUrl() {
        return this.getCandidates()[0];
    }
};

const ApiClient = {
    baseUrl: ApiConfigAdapter.defaultBaseUrl,
    cacheVersion: 'v1',
    defaultGetCacheTtlMs: 3 * 60 * 1000,
    defaultGetStaleMaxAgeMs: 24 * 60 * 60 * 1000,
    cacheTtlByPrefixMs: [
        { prefix: '/dashboard/overview', ttlMs: 30 * 1000 },
        { prefix: '/proiezioni', ttlMs: 45 * 1000 },
        { prefix: '/shows', ttlMs: 45 * 1000 },
        { prefix: '/films', ttlMs: 5 * 60 * 1000 },
        { prefix: '/registi', ttlMs: 5 * 60 * 1000 },
        { prefix: '/cinemas', ttlMs: 5 * 60 * 1000 },
        { prefix: '/categorie', ttlMs: 10 * 60 * 1000 }
    ],

    // Restituisce la lista di base URL in ordine di tentativo:
    // prima quello corrente, poi i candidati alternativi.
    /* DOC-FN: 'getFallbackBaseUrls' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getFallbackBaseUrls() {
        const fallbacks = ApiConfigAdapter.getCandidates();
        return [this.baseUrl, ...fallbacks.filter(url => url !== this.baseUrl)];
    },

    // Costruisce un namespace cache separato per utente autenticato/anonimo,
    // così i dati di utenti diversi non si mescolano in localStorage.
    /* DOC-FN: 'getCacheNamespace' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getCacheNamespace() {
        const userId = (typeof window !== 'undefined' && window.Auth && typeof window.Auth.getUser === 'function' && window.Auth.getUser())
            ? (window.Auth.getUser().id ?? 'auth')
            : 'anon';
        return `apicache:${this.cacheVersion}:${userId}:`;
    },

    // Uniforma gli endpoint per evitare chiavi cache duplicate (con/senza slash).
    /* DOC-FN: 'normalizeEndpoint' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    normalizeEndpoint(endpoint) {
        if (!endpoint) return '/';
        return endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
    },

    /* DOC-FN: 'getCacheKey' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getCacheKey(endpoint) {
        return `${this.getCacheNamespace()}${this.baseUrl}${this.normalizeEndpoint(endpoint)}`;
    },

    // TTL dinamico per endpoint: dashboard/proiezioni scadono prima,
    // anagrafiche (film/registi/cinema) restano in cache più a lungo.
    /* DOC-FN: 'resolveCacheTtlMs' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    resolveCacheTtlMs(endpoint) {
        const normalized = this.normalizeEndpoint(endpoint);
        const match = this.cacheTtlByPrefixMs.find(x => normalized.startsWith(x.prefix));
        return match?.ttlMs ?? this.defaultGetCacheTtlMs;
    },

    // Legge dalla cache locale solo payload validi (shape + timestamp).
    /* DOC-FN: 'readCachedEnvelope' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    readCachedEnvelope(endpoint) {
        try {
            const key = this.getCacheKey(endpoint);
            const raw = window.localStorage.getItem(key);
            if (!raw) return null;
            const parsed = JSON.parse(raw);
            if (!parsed || typeof parsed !== 'object') return null;
            if (typeof parsed.ts !== 'number') return null;
            return {
                ts: parsed.ts,
                data: parsed.data ?? null
            };
        } catch {
            return null;
        }
    },

    // Salva risposta GET in cache con timestamp per strategia stale-while-revalidate.
    /* DOC-FN: 'writeCachedResponse' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    writeCachedResponse(endpoint, data) {
        try {
            const key = this.getCacheKey(endpoint);
            window.localStorage.setItem(key, JSON.stringify({
                ts: Date.now(),
                data
            }));
        } catch {
            // ignore storage quota errors
        }
    },

    // Invalida cache dell'utente corrente e namespace comuni dopo mutazioni (POST/PUT/DELETE/PATCH).
    /* DOC-FN: 'clearCache' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    clearCache() {
        try {
            const namespaces = [
                this.getCacheNamespace(),
                `apicache:${this.cacheVersion}:anon:`,
                `apicache:${this.cacheVersion}:auth:`
            ];
            const keys = Object.keys(window.localStorage);
            /* DOC-FN: 'for' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            for (const key of keys) {
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (namespaces.some(prefix => key.startsWith(prefix))) {
                    window.localStorage.removeItem(key);
                }
            }
        } catch {
            // ignore storage errors
        }
    },

    /* DOC-FN: 'absolutizeMediaUrl' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    absolutizeMediaUrl(value, baseUrl) {
        if (!value || typeof value !== 'string') return value;
        if (/^https?:\/\//i.test(value)) return value;
        const normalizedValue = value.startsWith('/') ? value : `/${value}`;
        if (!/^\/media\//i.test(normalizedValue)) return value;
        return `${String(baseUrl || this.baseUrl).replace(/\/+$/, '')}${normalizedValue}`;
    },

    // Normalizza URL media relativi (/media/...) in URL assoluti verso il backend attivo.
    /* DOC-FN: 'normalizeMediaUrls' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    normalizeMediaUrls(payload, baseUrl) {
        if (!payload) return payload;
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (Array.isArray(payload)) {
            return payload.map(item => this.normalizeMediaUrls(item, baseUrl));
        }
        if (typeof payload !== 'object') return payload;

        const out = { ...payload };
        /* DOC-FN: 'for' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        for (const key of Object.keys(out)) {
            const value = out[key];
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (value && typeof value === 'object') {
                out[key] = this.normalizeMediaUrls(value, baseUrl);
                continue;
            }

            const normalizedKey = key.toLowerCase();
            const isMediaField =
                normalizedKey === 'copertinapath' ||
                normalizedKey === 'imageurl' ||
                normalizedKey === 'profile';
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (isMediaField) {
                out[key] = this.absolutizeMediaUrl(value, baseUrl);
            }
        }
        return out;
    },

    /* DOC-FN: 'normalizeImageElementSrc' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    normalizeImageElementSrc(img, baseUrl) {
        if (!img) return;
        const rawSrc = img.getAttribute('src');
        if (!rawSrc) return;
        const normalized = this.absolutizeMediaUrl(rawSrc, baseUrl);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (normalized && normalized !== rawSrc) {
            img.setAttribute('src', normalized);
        }
    },

    /* DOC-FN: 'normalizeMediaImagesInDom' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    normalizeMediaImagesInDom(baseUrl) {
        if (typeof document === 'undefined') return;
        document.querySelectorAll('img[src]').forEach((img) => this.normalizeImageElementSrc(img, baseUrl));
    },

    // Osserva il DOM e corregge automaticamente src immagini media relative anche quando
    // il contenuto viene iniettato dinamicamente dopo il render iniziale.
    /* DOC-FN: 'installDomMediaNormalizer' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    installDomMediaNormalizer() {
        if (typeof document === 'undefined') return;
        this.normalizeMediaImagesInDom(this.baseUrl);

        const observer = new MutationObserver((mutations) => {
            /* DOC-FN: 'for' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            for (const mutation of mutations) {
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (mutation.type === 'attributes' && mutation.target instanceof HTMLImageElement) {
                    this.normalizeImageElementSrc(mutation.target, this.baseUrl);
                    continue;
                }

                mutation.addedNodes.forEach((node) => {
                    if (!(node instanceof Element)) return;
                    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                    if (node instanceof HTMLImageElement) {
                        this.normalizeImageElementSrc(node, this.baseUrl);
                    }
                    node.querySelectorAll?.('img[src]').forEach((img) => this.normalizeImageElementSrc(img, this.baseUrl));
                });
            }
        });

        observer.observe(document.documentElement, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['src']
        });
    },

    // Metodo centrale per tutte le chiamate API:
    // - gestione token e refresh
    // - fallback tra più base URL
    // - cache GET con stale-while-revalidate
    // - normalizzazione URL media
    async request(endpoint, options = {}) {
        const auth = typeof window !== 'undefined' ? window.Auth : undefined;
        const method = String(options.method || 'GET').toUpperCase();
        const canUseGetCache = method === 'GET' && !endpoint.startsWith('/auth/');
        const cacheTtlMs = options.cacheTtlMs ?? this.resolveCacheTtlMs(endpoint);
        const staleMaxAgeMs = options.staleMaxAgeMs ?? this.defaultGetStaleMaxAgeMs;
        const bypassCache = !!options.bypassCache;
        const { cacheTtlMs: _cacheTtlMs, staleMaxAgeMs: _staleMaxAgeMs, bypassCache: _bypassCache, ...requestOptions } = options;

        const isAuthEndpoint = endpoint.startsWith('/auth/');
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!isAuthEndpoint && auth && typeof auth.ensureToken === 'function') {
            await auth.ensureToken();
        }

        // Aggiungi header Authorization se l'utente e autenticato
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };
        
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (auth && auth.accessToken) {
            headers['Authorization'] = `Bearer ${auth.accessToken}`;
        }
        
        const config = {
            credentials: 'include',
            headers,
            ...requestOptions
        };

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (config.body && typeof config.body === 'object') {
            config.body = JSON.stringify(config.body);
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (canUseGetCache && !bypassCache) {
            const cached = this.readCachedEnvelope(endpoint);
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (cached && cached.data !== null) {
                const ageMs = Date.now() - cached.ts;
                const isFresh = ageMs <= cacheTtlMs;
                const isStillUsable = ageMs <= staleMaxAgeMs;

                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (isFresh || isStillUsable) {
                    // Serve immediatamente da cache per rendere la pagina istantanea.
                    // Se stale, aggiorna in background senza bloccare la UI.
                    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                    if (!isFresh) {
                        setTimeout(() => {
                            this.request(endpoint, { ...requestOptions, method, bypassCache: true }).catch(() => {});
                        }, 0);
                    }
                    return this.normalizeMediaUrls(cached.data, this.baseUrl);
                }
            }
        }

        try {
            let response = null;
            let activeBaseUrl = this.baseUrl;
            let fetchError = null;

            /* DOC-FN: 'for' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            for (const candidate of this.getFallbackBaseUrls()) {
                try {
                    const abortController = new AbortController();
                    const timeoutId = setTimeout(() => abortController.abort(), 4000);
                    try {
                        response = await fetch(`${candidate}${endpoint}`, { ...config, signal: abortController.signal });
                    } finally {
                        clearTimeout(timeoutId);
                    }
                    activeBaseUrl = candidate;
                    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                    if (this.baseUrl !== candidate) {
                        this.baseUrl = candidate;
                        ApiConfigAdapter.persistBaseUrl(candidate);
                    }
                    break;
                } catch (err) {
                    fetchError = err;
                }
            }

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (!response) {
                throw fetchError || new Error('Failed to fetch');
            }

            // Se token scaduto, prova a fare refresh (refresh token in cookie HttpOnly)
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (response.status === 401 && !isAuthEndpoint && auth && typeof auth.refresh === 'function') {
                const refreshed = await auth.refresh({ silent: true });
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
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

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (response.status === 401) {
                // Non autenticato, redirect a login se necessario
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (!endpoint.includes('/auth/login') && !endpoint.includes('/auth/register')) {
                    window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname + window.location.search);
                }
                throw new Error('Non autenticato');
            }

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (response.status === 403) {
                throw new Error('Accesso negato - Permessi insufficienti');
            }

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (response.status === 204) {
                return null;
            }

            let data = null;
            const contentType = response.headers.get('content-type') || '';
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (contentType.includes('application/json')) {
                data = await response.json();
                data = this.normalizeMediaUrls(data, activeBaseUrl);
            }

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (!response.ok) {
                throw new Error(data?.message || `HTTP ${response.status}`);
            }

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (canUseGetCache) {
                this.writeCachedResponse(endpoint, data);
            } else if (method === 'POST' || method === 'PUT' || method === 'DELETE' || method === 'PATCH') {
                this.clearCache();
            }

            return data;
        } catch (error) {
            console.error('API Error:', error);
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
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
ApiClient.installDomMediaNormalizer();
window.dispatchEvent(new CustomEvent('apiclient:ready'));
})();

