(function () {
    let clientReadyPromise = null;
    const AZURE_API_BASE_URL = 'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io';

    function getFallbackBaseUrl() {
        return AZURE_API_BASE_URL;
    }

    function createInPlaceFallbackClient() {
        const fallback = {
            baseUrl: getFallbackBaseUrl(),
            async request(endpoint, options = {}) {
                const headers = {
                    'Content-Type': 'application/json',
                    ...(options.headers || {})
                };

                const auth = typeof window.Auth !== 'undefined' ? window.Auth : null;
                if (auth && auth.accessToken) {
                    headers.Authorization = `Bearer ${auth.accessToken}`;
                }

                const config = {
                    credentials: 'include',
                    ...options,
                    headers
                };

                if (config.body && typeof config.body === 'object') {
                    config.body = JSON.stringify(config.body);
                }

                const response = await fetch(`${fallback.baseUrl}${endpoint}`, config);
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
            },
            get(endpoint) { return fallback.request(endpoint, { method: 'GET' }); },
            post(endpoint, data) { return fallback.request(endpoint, { method: 'POST', body: data }); },
            put(endpoint, data) { return fallback.request(endpoint, { method: 'PUT', body: data }); },
            delete(endpoint) { return fallback.request(endpoint, { method: 'DELETE' }); }
        };

        return fallback;
    }

    function ensureClientScriptInjected() {
        if (document.querySelector('script[data-api-client-loader="1"]')) {
            return;
        }

        const script = document.createElement('script');
        script.src = '/js/api-client.js';
        script.async = false;
        script.setAttribute('data-api-client-loader', '1');
        (document.head || document.body || document.documentElement).appendChild(script);
    }

    function waitForClientReady(timeoutMs) {
        const current = resolveClientOrNull();
        if (current) {
            return Promise.resolve(current);
        }

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
            if (maybeReady) {
                onReady();
            }
        });

        return clientReadyPromise;
    }

    function resolveClientOrNull() {
        const current = window.ApiClient;
        if (!current || current.__isBootstrapClient) {
            return null;
        }

        return current;
    }

    function callWhenReady(method, args) {
        const realClient = resolveClientOrNull();
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

    function getDefaults() {
        return [AZURE_API_BASE_URL];
    }

    function uniq(urls) {
        return [...new Set(urls)];
    }

    function normalizeUrl(raw) {
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

    function isAcceptedLocalOrigin(origin) {
        try {
            const parsed = new URL(origin);
            if (parsed.protocol !== 'https:') {
                return false;
            }
            if (parsed.hostname.includes('filmhub-frontend')) {
                return false;
            }
            return true;
        } catch {
            return false;
        }
    }

    function getStoredBaseUrl() {
        const normalized = normalizeUrl((window.localStorage.getItem('apiBaseUrl') || '').trim());
        if (!normalized || !isAcceptedLocalOrigin(normalized)) {
            return null;
        }
        try {
            const parsed = new URL(normalized);
            const currentHost = (window.location.hostname || '').toLowerCase();
            if (parsed.hostname.toLowerCase() === currentHost) {
                return null;
            }
        } catch {
            return null;
        }

        return normalized;
    }

    function getCandidates() {
        const defaults = getDefaults();
        const stored = getStoredBaseUrl();
        if (!stored) {
            return uniq(defaults);
        }

        return uniq([stored, ...defaults]);
    }

    function persistBaseUrl(value) {
        const normalized = normalizeUrl(value);
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
