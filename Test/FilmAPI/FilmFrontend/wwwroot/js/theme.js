(function () {
    const STORAGE_KEY = 'siteTheme';
    let volatileTheme = 'dark';

    function safeGetStoredTheme() {
        try {
            return localStorage.getItem(STORAGE_KEY);
        } catch (_) {
            return volatileTheme;
        }
    }

    function safeSetStoredTheme(theme) {
        volatileTheme = theme;
        try {
            localStorage.setItem(STORAGE_KEY, theme);
        } catch (_) {
            // Ignore storage errors (private mode / blocked storage)
        }
    }

    function getPreferredTheme() {
        const stored = safeGetStoredTheme();
        if (stored === 'light' || stored === 'dark') {
            return stored;
        }

        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        return prefersDark ? 'dark' : 'light';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.classList.toggle('dark', theme === 'dark');
        document.documentElement.classList.toggle('light', theme === 'light');
        safeSetStoredTheme(theme);

        const icon = document.getElementById('theme-toggle-icon');
        if (icon) {
            icon.textContent = theme === 'dark' ? 'light_mode' : 'dark_mode';
        }

        document.querySelectorAll('[data-theme-toggle-icon]').forEach((el) => {
            el.textContent = theme === 'dark' ? 'light_mode' : 'dark_mode';
        });

        window.dispatchEvent(new CustomEvent('theme:changed', { detail: { theme } }));
    }

    window.Theme = {
        init() {
            applyTheme(getPreferredTheme());
        },
        toggle() {
            const current = document.documentElement.getAttribute('data-theme') || 'dark';
            applyTheme(current === 'dark' ? 'light' : 'dark');
        }
    };

    window.Theme.init();

    if (window.matchMedia) {
        const media = window.matchMedia('(prefers-color-scheme: dark)');
        const handleSystemThemeChange = (event) => {
            const stored = safeGetStoredTheme();
            if (stored !== 'light' && stored !== 'dark') {
                applyTheme(event.matches ? 'dark' : 'light');
            }
        };

        if (typeof media.addEventListener === 'function') {
            media.addEventListener('change', handleSystemThemeChange);
        } else if (typeof media.addListener === 'function') {
            media.addListener(handleSystemThemeChange);
        }
    }
})();
