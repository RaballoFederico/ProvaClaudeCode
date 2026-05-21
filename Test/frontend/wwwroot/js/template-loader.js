// DOC: template-loader - file del progetto; contiene logica specifica della feature/modulo.
/* DOC: Modulo JS 'template-loader': utility/comportamenti condivisi per autenticazione, routing, tema e API client. */
const componentHtmlCache = new Map();
const COMPONENT_CACHE_VERSION = 'v3';

/* DOC-FN: 'getComponentCacheKey' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function getComponentCacheKey(componentPath) {
    return `component-cache:${COMPONENT_CACHE_VERSION}:${componentPath}`;
}

/* DOC-FN: 'getCachedComponentHtml' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function getCachedComponentHtml(componentPath) {
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (componentHtmlCache.has(componentPath)) {
        return componentHtmlCache.get(componentPath);
    }

    try {
        const cached = window.sessionStorage.getItem(getComponentCacheKey(componentPath));
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (cached) {
            componentHtmlCache.set(componentPath, cached);
            return cached;
        }
    } catch {
        // ignore storage errors
    }

    return null;
}

/* DOC-FN: 'setCachedComponentHtml' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function setCachedComponentHtml(componentPath, html) {
    componentHtmlCache.set(componentPath, html);
    try {
        window.sessionStorage.setItem(getComponentCacheKey(componentPath), html);
    } catch {
        // ignore storage errors
    }
}

/* DOC-FN: 'injectComponentHtml' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function injectComponentHtml(elementId, html) {
    const element = document.getElementById(elementId);
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!element) {
        return;
    }

    element.innerHTML = html;

    const scripts = Array.from(element.querySelectorAll('script'));
    /* DOC-FN: 'for' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    for (const oldScript of scripts) {
        const newScript = document.createElement('script');
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (oldScript.src) {
            newScript.src = oldScript.src;
        } else {
            newScript.textContent = oldScript.textContent;
        }
        document.body.appendChild(newScript);
        oldScript.remove();
    }
}

/* DOC-FN: 'loadComponent' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function loadComponent(elementId, componentPath) {
    try {
        const cachedHtml = getCachedComponentHtml(componentPath);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (cachedHtml) {
            injectComponentHtml(elementId, cachedHtml);
            return;
        }

        const response = await fetch(componentPath);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!response.ok) {
            throw new Error(`Failed to load ${componentPath}`);
        }
        const html = await response.text();
        setCachedComponentHtml(componentPath, html);
        injectComponentHtml(elementId, html);
    } catch (error) {
        console.error('Error loading component:', error);
    }
}

/* DOC-FN: 'loadAllComponents' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function loadAllComponents() {
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (typeof Auth !== 'undefined' && typeof Auth.ensureInitialized === 'function') {
        // Avvia inizializzazione auth in background senza bloccare il rendering layout.
        Auth.ensureInitialized().catch(() => {});
    }

    const components = [
        { id: 'sidebar-container', path: '/components/sidebar.html' },
        { id: 'navbar-container', path: '/components/navbar.html' },
        { id: 'footer-container', path: '/components/footer.html' }
    ];

    await Promise.all(
        components.map(c => loadComponent(c.id, c.path))
    );

    initNavigation();
    applyPageAccessControl();
    renderCookieBanner();
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (window.Theme && typeof window.Theme.refreshMotion === 'function') {
        window.Theme.refreshMotion();
    }
}

/* DOC-FN: 'getCurrentUser' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function getCurrentUser() {
    if (typeof Auth === 'undefined') return null;
    return Auth.getUser();
}

/* DOC-FN: 'isManagerUser' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function isManagerUser() {
    if (typeof Auth === 'undefined' || !Auth.isAuthenticated()) return false;
    return Auth.canManageCatalog();
}

/* DOC-FN: 'isAuthenticatedUser' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function isAuthenticatedUser() {
    return typeof Auth !== 'undefined' && Auth.isAuthenticated();
}

/* DOC-FN: 'applyPageAccessControl' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function applyPageAccessControl() {
    const currentPath = window.location.pathname.split('/').pop() || 'home.html';
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (typeof AccessControl !== 'undefined' && AccessControl.pageRules) {
        const rule = AccessControl.pageRules[currentPath];
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (rule !== undefined && !AccessControl.canAccess(rule)) {
            window.location.href = AccessControl.getDefaultRoute();
            return;
        }
    }
}

/* DOC-FN: 'initNavigation' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function initNavigation() {
    const currentPath = window.location.pathname.split('/').pop() || 'home.html';
    
    document.querySelectorAll('nav a').forEach(link => {
        const href = link.getAttribute('href');
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (href === currentPath || (currentPath === '' && href === 'home.html')) {
            link.classList.add('bg-surface-container-high', 'text-primary-container');
            link.classList.remove('text-on-surface-variant');
        }
    });
}

const COOKIE_CONSENT_KEY = 'filmapi_cookie_consent_v2';

/* DOC-FN: 'getCookieConsent' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function getCookieConsent() {
    try {
        return localStorage.getItem(COOKIE_CONSENT_KEY);
    } catch {
        return null;
    }
}

/* DOC-FN: 'setCookieConsent' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function setCookieConsent(value) {
    try {
        localStorage.setItem(COOKIE_CONSENT_KEY, value);
    } catch {
        // ignore storage errors
    }
}

/* DOC-FN: 'getCookiePreferences' espone preferenze cookie in forma strutturata. */
function getCookiePreferences() {
    const raw = getCookieConsent();
    if (!raw) return null;

    if (raw === 'accepted') {
        return {
            necessary: true,
            analytics: true,
            marketing: true
        };
    }

    if (raw === 'rejected') {
        return {
            necessary: true,
            analytics: false,
            marketing: false
        };
    }

    try {
        const parsed = JSON.parse(raw);
        return {
            necessary: true,
            analytics: !!parsed?.analytics,
            marketing: !!parsed?.marketing
        };
    } catch {
        return null;
    }
}

/* DOC-FN: 'hasCookieCategoryConsent' verifica il consenso per categoria. */
function hasCookieCategoryConsent(category) {
    if (category === 'necessary') return true;
    const prefs = getCookiePreferences();
    if (!prefs) return false;
    return !!prefs[category];
}

/* DOC-FN: 'applyCookieConsentState' salva consenso e notifica il resto della UI. */
function applyCookieConsentState(state) {
    setCookieConsent(state);
    window.dispatchEvent(new CustomEvent('cookie-consent-updated', {
        detail: {
            preferences: getCookiePreferences()
        }
    }));
}

window.CookieConsent = {
    getPreferences: getCookiePreferences,
    hasConsent: hasCookieCategoryConsent
};

/* DOC-FN: 'ensureCookieBannerStyles' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function ensureCookieBannerStyles() {
    if (document.getElementById('cookie-banner-styles')) return;

    const style = document.createElement('style');
    style.id = 'cookie-banner-styles';
    style.textContent = `
        .cookie-banner {
            position: fixed;
            left: 50%;
            bottom: 1rem;
            transform: translateX(-50%);
            width: min(94vw, 940px);
            z-index: 9999;
            background: #111827;
            color: #f9fafb;
            border: 1px solid rgba(255,255,255,0.15);
            border-radius: 12px;
            box-shadow: 0 16px 40px rgba(0,0,0,0.45);
            padding: 14px 16px;
            display: grid;
            gap: 10px;
        }
        .cookie-banner-title {
            font-weight: 700;
            font-size: 0.98rem;
        }
        .cookie-banner-text {
            font-size: 0.9rem;
            line-height: 1.35rem;
            color: #c7ced7;
        }
        .cookie-banner-actions {
            display: flex;
            gap: 8px;
            flex-wrap: wrap;
        }
        .cookie-btn {
            border: 1px solid transparent;
            border-radius: 9px;
            padding: 8px 12px;
            font-size: 0.88rem;
            font-weight: 600;
            cursor: pointer;
            transition: all .16s ease;
        }
        .cookie-btn-accept {
            background: #e50914;
            color: #fff;
        }
        .cookie-btn-accept:hover {
            filter: brightness(1.05);
        }
        .cookie-btn-reject {
            background: transparent;
            color: #e5e7eb;
            border-color: rgba(255,255,255,0.25);
        }
        .cookie-btn-reject:hover {
            background: rgba(255,255,255,0.08);
        }
        .cookie-link {
            color: #93c5fd;
            text-decoration: underline;
        }
        .cookie-switch {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 10px;
            padding: 8px 0;
            border-bottom: 1px solid rgba(255,255,255,0.08);
        }
        .cookie-switch:last-of-type {
            border-bottom: none;
        }
        .cookie-switch input {
            width: 18px;
            height: 18px;
        }
        .cookie-prefs {
            display: none;
            margin-top: 2px;
            padding-top: 2px;
        }
        .cookie-prefs-open {
            display: block;
        }
    `;
    document.head.appendChild(style);
}

/* DOC-FN: 'renderCookieBanner' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function renderCookieBanner() {
    if (document.getElementById('cookie-banner')) return;
    if (getCookieConsent()) return;

    ensureCookieBannerStyles();

    const banner = document.createElement('div');
    banner.id = 'cookie-banner';
    banner.className = 'cookie-banner';
    banner.innerHTML = `
        <div class="cookie-banner-title">Usiamo i cookie</div>
        <div class="cookie-banner-text">
            Utilizziamo cookie tecnici per il funzionamento del sito e, con il tuo consenso,
            cookie aggiuntivi per migliorare l'esperienza. Leggi la nostra
            <a class="cookie-link" href="/privacy.html">Privacy Policy</a>.
        </div>
        <div id="cookie-prefs-panel" class="cookie-prefs" aria-hidden="true">
            <label class="cookie-switch">
                <span>Cookie necessari (sempre attivi)</span>
                <input type="checkbox" checked disabled>
            </label>
            <label class="cookie-switch">
                <span>Cookie analytics</span>
                <input type="checkbox" id="cookie-analytics-toggle">
            </label>
            <label class="cookie-switch">
                <span>Cookie marketing</span>
                <input type="checkbox" id="cookie-marketing-toggle">
            </label>
        </div>
        <div class="cookie-banner-actions">
            <button type="button" id="cookie-customize-btn" class="cookie-btn cookie-btn-reject">Personalizza</button>
            <button type="button" id="cookie-accept-btn" class="cookie-btn cookie-btn-accept">Accetta</button>
            <button type="button" id="cookie-reject-btn" class="cookie-btn cookie-btn-reject">Rifiuta</button>
            <button type="button" id="cookie-save-btn" class="cookie-btn cookie-btn-accept" style="display:none;">Salva preferenze</button>
        </div>
    `;

    document.body.appendChild(banner);

    const acceptBtn = document.getElementById('cookie-accept-btn');
    const rejectBtn = document.getElementById('cookie-reject-btn');
    const customizeBtn = document.getElementById('cookie-customize-btn');
    const saveBtn = document.getElementById('cookie-save-btn');
    const prefsPanel = document.getElementById('cookie-prefs-panel');
    const analyticsToggle = document.getElementById('cookie-analytics-toggle');
    const marketingToggle = document.getElementById('cookie-marketing-toggle');

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (acceptBtn) {
        acceptBtn.addEventListener('click', () => {
            applyCookieConsentState('accepted');
            banner.remove();
        });
    }

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (rejectBtn) {
        rejectBtn.addEventListener('click', () => {
            applyCookieConsentState('rejected');
            banner.remove();
        });
    }

    if (customizeBtn && saveBtn && prefsPanel && analyticsToggle && marketingToggle) {
        customizeBtn.addEventListener('click', () => {
            const isOpen = prefsPanel.classList.toggle('cookie-prefs-open');
            prefsPanel.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
            saveBtn.style.display = isOpen ? 'inline-block' : 'none';
        });

        saveBtn.addEventListener('click', () => {
            const payload = {
                necessary: true,
                analytics: !!analyticsToggle.checked,
                marketing: !!marketingToggle.checked
            };
            applyCookieConsentState(JSON.stringify(payload));
            banner.remove();
        });
    }
}


