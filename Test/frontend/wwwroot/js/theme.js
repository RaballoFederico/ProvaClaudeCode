// DOC: theme - file del progetto; contiene logica specifica della feature/modulo.
/* DOC: Modulo JS 'theme': utility/comportamenti condivisi per autenticazione, routing, tema e API client. */
(function () {
    const STORAGE_KEY = 'siteTheme';
    let volatileTheme = 'dark';

    /* DOC-FN: 'safeGetStoredTheme' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function safeGetStoredTheme() {
        try {
            return localStorage.getItem(STORAGE_KEY);
        } catch (_) {
            return volatileTheme;
        }
    }

    /* DOC-FN: 'safeSetStoredTheme' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function safeSetStoredTheme(theme) {
        volatileTheme = theme;
        try {
            localStorage.setItem(STORAGE_KEY, theme);
        } catch (_) {
            // Ignore storage errors (private mode / blocked storage)
        }
    }

    /* DOC-FN: 'getPreferredTheme' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function getPreferredTheme() {
        const stored = safeGetStoredTheme();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (stored === 'light' || stored === 'dark') {
            return stored;
        }

        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        return prefersDark ? 'dark' : 'light';
    }

    /* DOC-FN: 'applyTheme' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.classList.toggle('dark', theme === 'dark');
        document.documentElement.classList.toggle('light', theme === 'light');
        safeSetStoredTheme(theme);

        const icon = document.getElementById('theme-toggle-icon');
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (icon) {
            icon.textContent = theme === 'dark' ? 'light_mode' : 'dark_mode';
        }

        document.querySelectorAll('[data-theme-toggle-icon]').forEach((el) => {
            el.textContent = theme === 'dark' ? 'light_mode' : 'dark_mode';
        });

        window.dispatchEvent(new CustomEvent('theme:changed', { detail: { theme } }));
    }

    /* DOC-FN: 'applyMotionProfile' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function applyMotionProfile() {
        const path = (window.location.pathname.split('/').pop() || 'home.html').toLowerCase();
        const cinematicPages = new Set([
            'home.html',
            'abbonamenti.html',
            'login.html',
            'register.html',
            'programmazione.html',
            'scheda-film.html'
        ]);
        const focusedPages = new Set([
            'index.html',
            'films.html',
            'registi.html',
            'cinemas.html',
            'sale.html',
            'shows.html',
            'proiezioni.html',
            'utenti.html',
            'newsletter-admin.html',
            'validazione.html',
            'check-in.html'
        ]);

        const profile = cinematicPages.has(path) ? 'cinematic' : focusedPages.has(path) ? 'focused' : 'balanced';
        document.documentElement.setAttribute('data-motion-profile', profile);
    }

    window.Theme = {
        /* DOC-FN: 'init' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        init() {
            applyTheme(getPreferredTheme());
        },
        /* DOC-FN: 'toggle' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        toggle() {
            const current = document.documentElement.getAttribute('data-theme') || 'dark';
            applyTheme(current === 'dark' ? 'light' : 'dark');
        }
    };

    window.Theme.init();
    applyMotionProfile();

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (window.matchMedia) {
        const media = window.matchMedia('(prefers-color-scheme: dark)');
        const handleSystemThemeChange = (event) => {
            const stored = safeGetStoredTheme();
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (stored !== 'light' && stored !== 'dark') {
                applyTheme(event.matches ? 'dark' : 'light');
            }
        };

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (typeof media.addEventListener === 'function') {
            media.addEventListener('change', handleSystemThemeChange);
        } else if (typeof media.addListener === 'function') {
            media.addListener(handleSystemThemeChange);
        }
    }

    /* DOC-FN: 'initMotionEnhancements' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function initMotionEnhancements() {
        const reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (reduceMotion) return;

        const profile = document.documentElement.getAttribute('data-motion-profile') || 'balanced';
        const revealStep = profile === 'cinematic' ? 56 : profile === 'focused' ? 24 : 38;
        const revealThreshold = profile === 'cinematic' ? 0.1 : profile === 'focused' ? 0.2 : 0.12;
        const revealTargets = document.querySelectorAll('section, article, .app-card, .hover-lift, .hero-banner, .auth-card, .bg-surface-container, .bg-surface-container-high');
        let revealIndex = 0;

        revealTargets.forEach((el) => {
            if (el.classList.contains('reveal-up') || el.classList.contains('motion-inview')) return;
            const rect = el.getBoundingClientRect();
            if ((rect.width < 180 && rect.height < 80) || el.closest('#navbar, #sidebar, #mobile-bottom-nav')) return;

            el.classList.add('motion-inview');
            el.style.setProperty('--motion-delay', `${(revealIndex % 6) * revealStep}ms`);
            revealIndex += 1;
        });

        const popTargets = document.querySelectorAll('.app-card, .app-btn, .film-card, .badge-soft');
        popTargets.forEach((el) => {
            el.classList.add('motion-pop');
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (el.classList.contains('app-card') || el.classList.contains('film-card')) {
                el.classList.add('motion-glow');
            }
        });

        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('motion-show');
                observer.unobserve(entry.target);
            });
        }, {
            threshold: revealThreshold,
            rootMargin: '0px 0px -6% 0px'
        });

        document.querySelectorAll('.motion-inview').forEach((el) => observer.observe(el));
    }

    let motionRefreshScheduled = false;
    /* DOC-FN: 'scheduleMotionRefresh' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    function scheduleMotionRefresh() {
        if (motionRefreshScheduled) return;
        motionRefreshScheduled = true;
        window.requestAnimationFrame(() => {
            motionRefreshScheduled = false;
            initMotionEnhancements();
        });
    }

    window.Theme.refreshMotion = scheduleMotionRefresh;

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initMotionEnhancements);
    } else {
        initMotionEnhancements();
    }

    window.addEventListener('load', () => {
        // Covers late-rendered chunks/components.
        setTimeout(initMotionEnhancements, 120);
    });

    const mutationObserver = new MutationObserver((mutations) => {
        const hasAddedNodes = mutations.some((m) => m.addedNodes && m.addedNodes.length > 0);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (hasAddedNodes) {
            scheduleMotionRefresh();
        }
    });

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (document.body) {
        mutationObserver.observe(document.body, { childList: true, subtree: true });
    } else {
        document.addEventListener('DOMContentLoaded', () => {
            mutationObserver.observe(document.body, { childList: true, subtree: true });
        });
    }
})();


