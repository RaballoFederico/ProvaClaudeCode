/* DOC: Modulo JS 'api-config': utility/comportamenti condivisi per autenticazione, routing, tema e API client. */
(function () {
    let clientReadyPromise = null;
    const AZURE_API_BASE_URL = 'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io';

    /* DOC-FN: 'getFallbackBaseUrl' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function getFallbackBaseUrl() {
        return AZURE_API_BASE_URL;
    }

    /* DOC-FN: 'createInPlaceFallbackClient' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function createInPlaceFallbackClient() {
        const fallback = {
            baseUrl: getFallbackBaseUrl(),
            async request(endpoint, options = {}) {
                const headers = {
                    'Content-Type': 'application/json',
                    ...(options.headers || {})
                };

                const auth = typeof window.Auth !== 'undefined' ? window.Auth : null;
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (auth && auth.accessToken) {
                    headers.Authorization = `Bearer ${auth.accessToken}`;
                }

                const config = {
                    credentials: 'include',
                    ...options,
                    headers
                };

                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (config.body && typeof config.body === 'object') {
                    config.body = JSON.stringify(config.body);
                }

                const response = await fetch(`${fallback.baseUrl}${endpoint}`, config);
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (response.status === 204) {
                    return null;
                }

                let data = null;
                const contentType = response.headers.get('content-type') || '';
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (contentType.includes('application/json')) {
                    data = await response.json();
                }

                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (!response.ok) {
                    throw new Error(data?.message || `HTTP ${response.status}`);
                }

                return data;
            },
            get(endpoint) { return fallback.request(endpoint, { method: 'GET' }); },
            post(endpoint, data) { return fallback.request(endpoint, { method: 'POST', body: data }); },
            put(endpoint, data) { return fallback.request(endpoint, { method: 'PUT', body: data }); },
            delete(endpoint) { return fallback.request(endpoint, { method: 'DELETE' }); }
        };

        return fallback;
    }

    /* DOC-FN: 'ensureClientScriptInjected' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function ensureClientScriptInjected() {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (document.querySelector('script[data-api-client-loader="1"]')) {
            return;
        }

        const script = document.createElement('script');
        script.src = '/js/api-client.js';
        script.async = false;
        script.setAttribute('data-api-client-loader', '1');
        (document.head || document.body || document.documentElement).appendChild(script);
    }

    /* DOC-FN: 'waitForClientReady' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function waitForClientReady(timeoutMs) {
        const current = resolveClientOrNull();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (current) {
            return Promise.resolve(current);
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (clientReadyPromise) {
            return clientReadyPromise;
        }

        ensureClientScriptInjected();

        clientReadyPromise = new Promise((resolve) => {
            const timer = window.setTimeout(() => {
                window.removeEventListener('apiclient:ready', onReady);
                clientReadyPromise = null;
                const fallbackClient = createInPlaceFallbackClient();
                window.ApiClient = fallbackClient;
                window.APIClient = fallbackClient;
                resolve(fallbackClient);
            }, timeoutMs);

            const onReady = () => {
                const readyClient = resolveClientOrNull();
                /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
                if (!readyClient) {
                    return;
                }

                window.clearTimeout(timer);
                window.removeEventListener('apiclient:ready', onReady);
                clientReadyPromise = null;
                resolve(readyClient);
            };

            window.addEventListener('apiclient:ready', onReady);

            const maybeReady = resolveClientOrNull();
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (maybeReady) {
                onReady();
            }
        });

        return clientReadyPromise;
    }

    /* DOC-FN: 'resolveClientOrNull' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function resolveClientOrNull() {
        const current = window.ApiClient;
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!current || current.__isBootstrapClient) {
            return null;
        }

        return current;
    }

    /* DOC-FN: 'callWhenReady' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function callWhenReady(method, args) {
        const realClient = resolveClientOrNull();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (realClient && typeof realClient[method] === 'function') {
            return realClient[method](...args);
        }

        return waitForClientReady(15000).then((client) => {
            const selectedClient = client || createInPlaceFallbackClient();
            const fallbackMethod = typeof selectedClient[method] === 'function'
                ? selectedClient[method].bind(selectedClient)
                : selectedClient.request.bind(selectedClient);
            return fallbackMethod(...args);
        });
    }

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (typeof window.ApiClient === 'undefined') {
        window.ApiClient = {
            __isBootstrapClient: true,
            baseUrl: '',
            request(...args) { return callWhenReady('request', args); },
            get(...args) { return callWhenReady('get', args); },
            post(...args) { return callWhenReady('post', args); },
            put(...args) { return callWhenReady('put', args); },
            delete(...args) { return callWhenReady('delete', args); }
        };
        window.APIClient = window.ApiClient;
    }

    /* DOC-FN: 'getDefaults' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function getDefaults() {
        return [AZURE_API_BASE_URL];
    }

    /* DOC-FN: 'uniq' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function uniq(urls) {
        return [...new Set(urls)];
    }

    /* DOC-FN: 'normalizeUrl' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function normalizeUrl(raw) {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!raw || raw === 'undefined' || raw === 'null') {
            return null;
        }

        try {
            const parsed = new URL(raw);
            return parsed.origin;
        } catch {
            return null;
        }
    }

    /* DOC-FN: 'isAcceptedLocalOrigin' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function isAcceptedLocalOrigin(origin) {
        try {
            const parsed = new URL(origin);
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (parsed.protocol !== 'https:') {
                return false;
            }
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (parsed.hostname.includes('filmhub-frontend')) {
                return false;
            }
            return true;
        } catch {
            return false;
        }
    }

    /* DOC-FN: 'getStoredBaseUrl' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function getStoredBaseUrl() {
        const normalized = normalizeUrl((window.localStorage.getItem('apiBaseUrl') || '').trim());
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!normalized || !isAcceptedLocalOrigin(normalized)) {
            return null;
        }
        try {
            const parsed = new URL(normalized);
            const currentHost = (window.location.hostname || '').toLowerCase();
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (parsed.hostname.toLowerCase() === currentHost) {
                return null;
            }
        } catch {
            return null;
        }

        return normalized;
    }

    /* DOC-FN: 'getCandidates' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function getCandidates() {
        const defaults = getDefaults();
        const stored = getStoredBaseUrl();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!stored) {
            return uniq(defaults);
        }

        return uniq([stored, ...defaults]);
    }

    /* DOC-FN: 'persistBaseUrl' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function persistBaseUrl(value) {
        const normalized = normalizeUrl(value);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!normalized || !isAcceptedLocalOrigin(normalized)) {
            return;
        }

        window.localStorage.setItem('apiBaseUrl', normalized);
    }

    window.ApiConfig = {
        get defaultBaseUrl() {
            return getCandidates()[0];
        },
        getCandidates,
        getStoredBaseUrl,
        persistBaseUrl,
        normalizeUrl
    };
})();

