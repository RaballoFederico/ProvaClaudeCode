(function () {
    let clientReadyPromise = null;

    function getFallbackBaseUrl() {
        const currentHost = (window.location.hostname || '').toLowerCase();
        return currentHost === '127.0.0.1' ? 'http://127.0.0.1:5001' : 'http://localhost:5001';
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
        const currentHost = (window.location.hostname || '').toLowerCase();
        if (currentHost === '127.0.0.1') {
            return ['http://127.0.0.1:5001', 'https://127.0.0.1:7217'];
        }

        return ['http://localhost:5001', 'https://localhost:7217'];
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
            const isLocal = parsed.hostname === 'localhost' || parsed.hostname === '127.0.0.1';
            if (!isLocal) {
                return true;
            }

            return parsed.port === '5001' || parsed.port === '7217';
        } catch {
            return false;
        }
    }

    function getStoredBaseUrl() {
        const normalized = normalizeUrl((window.localStorage.getItem('apiBaseUrl') || '').trim());
        if (!normalized || !isAcceptedLocalOrigin(normalized)) {
            return null;
        }

        const currentHost = (window.location.hostname || '').toLowerCase();
        if (currentHost === 'localhost' && normalized.includes('127.0.0.1')) {
            return null;
        }
        if (currentHost === '127.0.0.1' && normalized.includes('localhost')) {
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
