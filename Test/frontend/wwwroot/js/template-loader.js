const componentHtmlCache = new Map();
const COMPONENT_CACHE_VERSION = 'v3';

function getComponentCacheKey(componentPath) {
    return `component-cache:${COMPONENT_CACHE_VERSION}:${componentPath}`;
}

function getCachedComponentHtml(componentPath) {
    if (componentHtmlCache.has(componentPath)) {
        return componentHtmlCache.get(componentPath);
    }

    try {
        const cached = window.sessionStorage.getItem(getComponentCacheKey(componentPath));
        if (cached) {
            componentHtmlCache.set(componentPath, cached);
            return cached;
        }
    } catch {
        // ignore storage errors
    }

    return null;
}

function setCachedComponentHtml(componentPath, html) {
    componentHtmlCache.set(componentPath, html);
    try {
        window.sessionStorage.setItem(getComponentCacheKey(componentPath), html);
    } catch {
        // ignore storage errors
    }
}

function injectComponentHtml(elementId, html) {
    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }

    element.innerHTML = html;

    const scripts = Array.from(element.querySelectorAll('script'));
    for (const oldScript of scripts) {
        const newScript = document.createElement('script');
        if (oldScript.src) {
            newScript.src = oldScript.src;
        } else {
            newScript.textContent = oldScript.textContent;
        }
        document.body.appendChild(newScript);
        oldScript.remove();
    }
}

async function loadComponent(elementId, componentPath) {
    try {
        const cachedHtml = getCachedComponentHtml(componentPath);
        if (cachedHtml) {
            injectComponentHtml(elementId, cachedHtml);
            return;
        }

        const response = await fetch(componentPath);
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

async function loadAllComponents() {
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
    if (window.Theme && typeof window.Theme.refreshMotion === 'function') {
        window.Theme.refreshMotion();
    }
}

function getCurrentUser() {
    if (typeof Auth === 'undefined') return null;
    return Auth.getUser();
}

function isManagerUser() {
    if (typeof Auth === 'undefined' || !Auth.isAuthenticated()) return false;
    return Auth.canManageCatalog();
}

function isAuthenticatedUser() {
    return typeof Auth !== 'undefined' && Auth.isAuthenticated();
}

function applyPageAccessControl() {
    const currentPath = window.location.pathname.split('/').pop() || 'home.html';
    if (typeof AccessControl !== 'undefined' && AccessControl.pageRules) {
        const rule = AccessControl.pageRules[currentPath];
        if (rule !== undefined && !AccessControl.canAccess(rule)) {
            window.location.href = AccessControl.getDefaultRoute();
            return;
        }
    }
}

function initNavigation() {
    const currentPath = window.location.pathname.split('/').pop() || 'home.html';
    
    document.querySelectorAll('nav a').forEach(link => {
        const href = link.getAttribute('href');
        if (href === currentPath || (currentPath === '' && href === 'home.html')) {
            link.classList.add('bg-surface-container-high', 'text-primary-container');
            link.classList.remove('text-on-surface-variant');
        }
    });
}

const COOKIE_CONSENT_KEY = 'filmapi_cookie_consent_v1';

function getCookieConsent() {
    try {
        return localStorage.getItem(COOKIE_CONSENT_KEY);
    } catch {
        return null;
    }
}

function setCookieConsent(value) {
    try {
        localStorage.setItem(COOKIE_CONSENT_KEY, value);
    } catch {
        // ignore storage errors
    }
}

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
    `;
    document.head.appendChild(style);
}

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
        <div class="cookie-banner-actions">
            <button type="button" id="cookie-accept-btn" class="cookie-btn cookie-btn-accept">Accetta</button>
            <button type="button" id="cookie-reject-btn" class="cookie-btn cookie-btn-reject">Rifiuta</button>
        </div>
    `;

    document.body.appendChild(banner);

    const acceptBtn = document.getElementById('cookie-accept-btn');
    const rejectBtn = document.getElementById('cookie-reject-btn');

    if (acceptBtn) {
        acceptBtn.addEventListener('click', () => {
            setCookieConsent('accepted');
            banner.remove();
        });
    }

    if (rejectBtn) {
        rejectBtn.addEventListener('click', () => {
            setCookieConsent('rejected');
            banner.remove();
        });
    }
}
