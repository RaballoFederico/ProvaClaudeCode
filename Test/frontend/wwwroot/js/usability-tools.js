(() => {
    const STORAGE_KEY = 'filmhub_usability_settings_v1';
    const TOUR_PENDING_EMAIL_KEY = 'filmhub_tour_pending_email';
    const TOUR_SEEN_PREFIX = 'filmhub_tour_seen_user_';
    const DEFAULTS = {
        highContrast: false,
        largeText: false,
        readableFont: false,
        reduceMotion: false
    };

    const state = { ...DEFAULTS };

    function readSettings() {
        try {
            const parsed = JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}');
            return { ...DEFAULTS, ...parsed };
        } catch {
            return { ...DEFAULTS };
        }
    }

    function saveSettings() {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    }

    function applySettings() {
        const html = document.documentElement;
        html.classList.toggle('a11y-high-contrast', !!state.highContrast);
        html.classList.toggle('a11y-large-text', !!state.largeText);
        html.classList.toggle('a11y-readable-font', !!state.readableFont);
        html.classList.toggle('a11y-reduce-motion', !!state.reduceMotion);
    }

    function escapeHtml(value) {
        const div = document.createElement('div');
        div.textContent = value || '';
        return div.innerHTML;
    }

    function getTourSteps() {
        const page = (window.location.pathname.split('/').pop() || 'home.html').toLowerCase();
        const base = [
            {
                selector: '#navbar',
                title: 'Barra in alto',
                text: 'Qui trovi ricerca rapida, notifiche, menu account e tema. Suggerimento: usa Ctrl+K per cercare una pagina.'
            },
            {
                selector: '#sidebar',
                title: 'Menu laterale',
                text: 'Questo menu e il punto principale di navigazione tra prenotazioni, biglietti, profilo e backoffice.'
            },
            {
                selector: '#nav-user-toggle',
                title: 'Menu utente',
                text: 'Da qui apri le impostazioni personali, il pannello accessibilita e il tour guidato in qualsiasi momento.'
            }
        ];

        const map = {
            'programmazione.html': [
                ...base,
                { selector: '#open-cinema-modal', title: '1) Scegli il cinema', text: 'Inizia da qui: seleziona il cinema corretto per vedere orari e disponibilita reali.' },
                { selector: '#search-input', title: '2) Cerca il film', text: 'Scrivi un titolo per filtrare il catalogo in tempo reale e trovare subito quello che vuoi.' },
                { selector: '#films-grid', title: '3) Apri il film', text: 'Clicca una card per vedere spettacoli, sala e orari disponibili per la prenotazione.' }
            ],
            'profilo.html': [
                ...base,
                { selector: '#profilo-form', title: 'Dati account', text: 'Qui puoi aggiornare i dati personali.' },
                { selector: '#ordini-list', title: 'Ordini', text: 'In questa sezione trovi storico acquisti e dettagli.' },
                { selector: '#biglietti-list', title: 'Biglietti', text: 'Qui trovi i biglietti acquistati e il loro stato.' }
            ],
            'index.html': [
                ...base
            ]
        };

        return map[page] || [
            ...base,
            { selector: 'main', title: 'Contenuto pagina', text: 'Qui trovi le funzionalita principali della schermata.' }
        ];
    }

    function buildPanel() {
        let root = document.getElementById('a11y-tools-root');
        if (root) return root;

        root = document.createElement('div');
        root.id = 'a11y-tools-root';
        root.className = 'a11y-tools-root';
        root.innerHTML = `
            <button id="a11y-fab" class="a11y-fab" title="Accessibilita" aria-label="Apri accessibilita">
                <span class="material-symbols-outlined">accessibility_new</span>
            </button>
            <div id="a11y-panel" class="a11y-panel hidden" role="dialog" aria-label="Pannello accessibilita">
                <div class="a11y-panel-header">
                    <strong>Accessibilita</strong>
                    <button id="a11y-close" class="a11y-icon-btn" aria-label="Chiudi">✕</button>
                </div>
                <label class="a11y-toggle"><input id="a11y-high-contrast" type="checkbox"> Contrasto alto</label>
                <label class="a11y-toggle"><input id="a11y-large-text" type="checkbox"> Testo piu grande</label>
                <label class="a11y-toggle"><input id="a11y-readable-font" type="checkbox"> Font leggibile</label>
                <label class="a11y-toggle"><input id="a11y-reduce-motion" type="checkbox"> Riduci animazioni</label>
                <div class="a11y-actions">
                    <button id="a11y-start-tour" class="a11y-btn">Avvia tour guidato</button>
                    <button id="a11y-reset" class="a11y-btn a11y-btn-muted">Reset</button>
                </div>
            </div>
        `;

        document.body.appendChild(root);
        return root;
    }

    function syncPanelControls() {
        const map = [
            ['a11y-high-contrast', 'highContrast'],
            ['a11y-large-text', 'largeText'],
            ['a11y-readable-font', 'readableFont'],
            ['a11y-reduce-motion', 'reduceMotion']
        ];
        map.forEach(([id, key]) => {
            const input = document.getElementById(id);
            if (input) {
                input.checked = !!state[key];
                input.closest('.a11y-toggle')?.classList.toggle('is-checked', !!state[key]);
            }
        });
    }

    function bindPanel() {
        const fab = document.getElementById('a11y-fab');
        const panel = document.getElementById('a11y-panel');
        const close = document.getElementById('a11y-close');
        if (!fab || !panel || !close) return;

        const openPanel = () => panel.classList.remove('hidden');
        const closePanel = () => panel.classList.add('hidden');

        fab.addEventListener('click', openPanel);
        close.addEventListener('click', closePanel);

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') closePanel();
        });

        const bindToggle = (id, key) => {
            const el = document.getElementById(id);
            if (!el) return;
            el.addEventListener('change', () => {
                state[key] = !!el.checked;
                el.closest('.a11y-toggle')?.classList.toggle('is-checked', !!el.checked);
                saveSettings();
                applySettings();
            });
        };

        bindToggle('a11y-high-contrast', 'highContrast');
        bindToggle('a11y-large-text', 'largeText');
        bindToggle('a11y-readable-font', 'readableFont');
        bindToggle('a11y-reduce-motion', 'reduceMotion');

        document.getElementById('a11y-reset')?.addEventListener('click', () => {
            Object.assign(state, DEFAULTS);
            saveSettings();
            applySettings();
            syncPanelControls();
        });

        document.getElementById('a11y-start-tour')?.addEventListener('click', () => {
            closePanel();
            window.UsabilityTools.startTour();
        });
    }

    function waitForTourTargets(steps, timeoutMs = 2500) {
        const startAt = Date.now();
        return new Promise((resolve) => {
            const check = () => {
                const foundCount = steps.filter((step) => !!resolveStepElement(step)).length;
                if (foundCount > 0 || (Date.now() - startAt) >= timeoutMs) {
                    resolve();
                    return;
                }
                setTimeout(check, 120);
            };
            check();
        });
    }

    function resolveStepElement(step) {
        if (!step || !step.selector) return null;
        if (typeof step.selector === 'string') {
            return document.querySelector(step.selector);
        }
        if (Array.isArray(step.selector)) {
            for (const selector of step.selector) {
                const match = document.querySelector(selector);
                if (match) return match;
            }
        }
        return null;
    }

    function startTour() {
        const steps = getTourSteps();
        if (!steps.length) return;

        let current = 0;
        let overlay = document.getElementById('tour-overlay');
        if (overlay) overlay.remove();

        overlay = document.createElement('div');
        overlay.id = 'tour-overlay';
        overlay.className = 'tour-overlay';
        document.body.appendChild(overlay);

        function getRenderableSteps() {
            const renderable = [];
            for (let idx = 0; idx < steps.length; idx += 1) {
                const el = resolveStepElement(steps[idx]);
                if (el) renderable.push({ index: idx, element: el });
            }
            return renderable;
        }

        function positionCard(rect, cardEl) {
            const spacing = 12;
            const vw = window.innerWidth;
            const vh = window.innerHeight;
            const cardRect = cardEl.getBoundingClientRect();
            let left = rect.left;
            const maxLeft = Math.max(12, vw - cardRect.width - 12);
            left = Math.min(Math.max(12, left), maxLeft);

            let top = rect.bottom + spacing;
            if (top + cardRect.height > vh - 12) {
                top = rect.top - cardRect.height - spacing;
            }
            top = Math.min(Math.max(12, top), Math.max(12, vh - cardRect.height - 12));

            cardEl.style.left = `${left + window.scrollX}px`;
            cardEl.style.top = `${top + window.scrollY}px`;
        }

        const render = (shouldScroll = false) => {
            const renderableSteps = getRenderableSteps();
            if (!renderableSteps.length) {
                stopTour();
                return;
            }

            let currentStep = renderableSteps.find((entry) => entry.index === current);
            if (!currentStep) {
                currentStep = renderableSteps.find((entry) => entry.index > current) || renderableSteps[renderableSteps.length - 1];
            }
            if (!currentStep) {
                stopTour();
                return;
            }

            current = currentStep.index;
            const step = steps[current];
            const element = currentStep.element;
            const currentPosition = renderableSteps.findIndex((entry) => entry.index === current);
            const totalSteps = renderableSteps.length;

            if (shouldScroll) {
                element.scrollIntoView({ block: 'center', behavior: 'smooth' });
            }
            const rect = element.getBoundingClientRect();
            const top = Math.max(12, rect.top + window.scrollY - 8);
            const left = Math.max(12, rect.left + window.scrollX - 8);
            const width = Math.max(24, rect.width + 16);
            const height = Math.max(24, rect.height + 16);

            overlay.innerHTML = `
                <div class="tour-highlight" style="top:${top}px;left:${left}px;width:${width}px;height:${height}px;"></div>
                <div class="tour-card">
                    <div class="tour-meta">Passo ${currentPosition + 1} di ${totalSteps}</div>
                    <div class="tour-title">${escapeHtml(step.title)}</div>
                    <div class="tour-text">${escapeHtml(step.text)}</div>
                    <div class="tour-actions">
                        <button class="tour-btn tour-btn-muted" id="tour-skip">Chiudi</button>
                        <button class="tour-btn tour-btn-muted" id="tour-prev" ${currentPosition <= 0 ? 'disabled' : ''}>Indietro</button>
                        <button class="tour-btn" id="tour-next">${currentPosition >= totalSteps - 1 ? 'Fine' : 'Avanti'}</button>
                    </div>
                </div>
            `;

            const card = overlay.querySelector('.tour-card');
            if (card) {
                positionCard(rect, card);
            }

            document.getElementById('tour-skip')?.addEventListener('click', stopTour);
            document.getElementById('tour-prev')?.addEventListener('click', () => {
                const liveRenderable = getRenderableSteps();
                const pos = liveRenderable.findIndex((entry) => entry.index === current);
                if (pos <= 0) return;
                current = liveRenderable[pos - 1].index;
                render(true);
            });
            document.getElementById('tour-next')?.addEventListener('click', () => {
                const liveRenderable = getRenderableSteps();
                const pos = liveRenderable.findIndex((entry) => entry.index === current);
                if (pos < 0 || pos >= liveRenderable.length - 1) {
                    stopTour();
                    return;
                }
                current = liveRenderable[pos + 1].index;
                render(true);
            });
        };

        const onEsc = (e) => {
            if (e.key === 'Escape') stopTour();
        };

        function stopTour() {
            document.removeEventListener('keydown', onEsc);
            window.removeEventListener('resize', onResize);
            window.removeEventListener('scroll', onScroll, true);
            overlay.remove();
        }

        const onResize = () => render(false);
        const onScroll = () => render(false);

        document.addEventListener('keydown', onEsc);
        window.addEventListener('resize', onResize);
        window.addEventListener('scroll', onScroll, true);

        waitForTourTargets(steps).then(() => {
            if (!document.body.contains(overlay)) return;
            render(true);
        });
    }

    function getCurrentUser() {
        if (typeof window === 'undefined' || typeof window.Auth === 'undefined') return null;
        if (!Auth.isAuthenticated || !Auth.isAuthenticated()) return null;
        return Auth.getUser ? Auth.getUser() : null;
    }

    function getTourSeenKey(user) {
        const userId = user?.id;
        if (!userId) return null;
        return `${TOUR_SEEN_PREFIX}${userId}`;
    }

    function maybeStartTutorialForNewUser() {
        const user = getCurrentUser();
        if (!user) return;

        const email = String(user.email || '').trim().toLowerCase();
        const pendingEmail = String(localStorage.getItem(TOUR_PENDING_EMAIL_KEY) || '').trim().toLowerCase();
        if (!email || !pendingEmail || email !== pendingEmail) return;

        const seenKey = getTourSeenKey(user);
        if (!seenKey) return;
        if (localStorage.getItem(seenKey) === '1') return;

        localStorage.setItem(seenKey, '1');
        localStorage.removeItem(TOUR_PENDING_EMAIL_KEY);

        setTimeout(() => {
            startTour();
        }, 900);
    }

    function ensureInit() {
        Object.assign(state, readSettings());
        applySettings();
        buildPanel();
        syncPanelControls();
        bindPanel();
        maybeStartTutorialForNewUser();

        window.addEventListener('auth:ready', () => {
            maybeStartTutorialForNewUser();
        });
    }

    window.UsabilityTools = {
        startTour,
        openAccessibility() {
            document.getElementById('a11y-panel')?.classList.remove('hidden');
        },
        resetAccessibility() {
            Object.assign(state, DEFAULTS);
            saveSettings();
            applySettings();
            syncPanelControls();
        }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', ensureInit);
    } else {
        ensureInit();
    }
})();
