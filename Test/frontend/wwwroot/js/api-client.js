(() => {
if (window.__apiClientInitialized) {
    return;
}
window.__apiClientInitialized = true;

const ApiConfigAdapter = window.ApiConfig || {
    getCandidates() {
        return [
            'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io'
        ];
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

    getFallbackBaseUrls() {
        const fallbacks = ApiConfigAdapter.getCandidates();
        return [this.baseUrl, ...fallbacks.filter(url => url !== this.baseUrl)];
    },

    getCacheNamespace() {
        const userId = (typeof window !== 'undefined' && window.Auth && typeof window.Auth.getUser === 'function' && window.Auth.getUser())
            ? (window.Auth.getUser().id ?? 'auth')
            : 'anon';
        return `apicache:${this.cacheVersion}:${userId}:`;
    },

    normalizeEndpoint(endpoint) {
        if (!endpoint) return '/';
        return endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
    },

    getCacheKey(endpoint) {
        return `${this.getCacheNamespace()}${this.baseUrl}${this.normalizeEndpoint(endpoint)}`;
    },

    resolveCacheTtlMs(endpoint) {
        const normalized = this.normalizeEndpoint(endpoint);
        const match = this.cacheTtlByPrefixMs.find(x => normalized.startsWith(x.prefix));
        return match?.ttlMs ?? this.defaultGetCacheTtlMs;
    },

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

    clearCache() {
        try {
            const namespaces = [
                this.getCacheNamespace(),
                `apicache:${this.cacheVersion}:anon:`,
                `apicache:${this.cacheVersion}:auth:`
            ];
            const keys = Object.keys(window.localStorage);
            for (const key of keys) {
                if (namespaces.some(prefix => key.startsWith(prefix))) {
                    window.localStorage.removeItem(key);
                }
            }
        } catch {
            // ignore storage errors
        }
    },

    absolutizeMediaUrl(value, baseUrl) {
        if (!value || typeof value !== 'string') return value;
        if (/^https?:\/\//i.test(value)) return value;
        const normalizedValue = value.startsWith('/') ? value : `/${value}`;
        if (!/^\/media\//i.test(normalizedValue)) return value;
        return `${String(baseUrl || this.baseUrl).replace(/\/+$/, '')}${normalizedValue}`;
    },

    normalizeMediaUrls(payload, baseUrl) {
        if (!payload) return payload;
        if (Array.isArray(payload)) {
            return payload.map(item => this.normalizeMediaUrls(item, baseUrl));
        }
        if (typeof payload !== 'object') return payload;

        const out = { ...payload };
        for (const key of Object.keys(out)) {
            const value = out[key];
            if (value && typeof value === 'object') {
                out[key] = this.normalizeMediaUrls(value, baseUrl);
                continue;
            }

            const normalizedKey = key.toLowerCase();
            const isMediaField =
                normalizedKey === 'copertinapath' ||
                normalizedKey === 'imageurl' ||
                normalizedKey === 'profile';
            if (isMediaField) {
                out[key] = this.absolutizeMediaUrl(value, baseUrl);
            }
        }
        return out;
    },

    normalizeImageElementSrc(img, baseUrl) {
        if (!img) return;
        const rawSrc = img.getAttribute('src');
        if (!rawSrc) return;
        const normalized = this.absolutizeMediaUrl(rawSrc, baseUrl);
        if (normalized && normalized !== rawSrc) {
            img.setAttribute('src', normalized);
        }
    },

    normalizeMediaImagesInDom(baseUrl) {
        if (typeof document === 'undefined') return;
        document.querySelectorAll('img[src]').forEach((img) => this.normalizeImageElementSrc(img, baseUrl));
    },

    installDomMediaNormalizer() {
        if (typeof document === 'undefined') return;
        this.normalizeMediaImagesInDom(this.baseUrl);

        const observer = new MutationObserver((mutations) => {
            for (const mutation of mutations) {
                if (mutation.type === 'attributes' && mutation.target instanceof HTMLImageElement) {
                    this.normalizeImageElementSrc(mutation.target, this.baseUrl);
                    continue;
                }

                mutation.addedNodes.forEach((node) => {
                    if (!(node instanceof Element)) return;
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

    async request(endpoint, options = {}) {
        const auth = typeof window !== 'undefined' ? window.Auth : undefined;
        const method = String(options.method || 'GET').toUpperCase();
        const canUseGetCache = method === 'GET' && !endpoint.startsWith('/auth/');
        const cacheTtlMs = options.cacheTtlMs ?? this.resolveCacheTtlMs(endpoint);
        const staleMaxAgeMs = options.staleMaxAgeMs ?? this.defaultGetStaleMaxAgeMs;
        const bypassCache = !!options.bypassCache;
        const { cacheTtlMs: _cacheTtlMs, staleMaxAgeMs: _staleMaxAgeMs, bypassCache: _bypassCache, ...requestOptions } = options;

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
            ...requestOptions
        };

        if (config.body && typeof config.body === 'object') {
            config.body = JSON.stringify(config.body);
        }

        if (canUseGetCache && !bypassCache) {
            const cached = this.readCachedEnvelope(endpoint);
            if (cached && cached.data !== null) {
                const ageMs = Date.now() - cached.ts;
                const isFresh = ageMs <= cacheTtlMs;
                const isStillUsable = ageMs <= staleMaxAgeMs;

                if (isFresh || isStillUsable) {
                    // Serve immediatamente da cache per rendere la pagina istantanea.
                    // Se stale, aggiorna in background senza bloccare la UI.
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
                data = this.normalizeMediaUrls(data, activeBaseUrl);
            }

            if (!response.ok) {
                throw new Error(data?.message || `HTTP ${response.status}`);
            }

            if (canUseGetCache) {
                this.writeCachedResponse(endpoint, data);
            } else if (method === 'POST' || method === 'PUT' || method === 'DELETE' || method === 'PATCH') {
                this.clearCache();
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
ApiClient.installDomMediaNormalizer();
window.dispatchEvent(new CustomEvent('apiclient:ready'));
})();
