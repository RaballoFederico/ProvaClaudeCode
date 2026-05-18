/* DOC: Modulo JS 'utils': utility/comportamenti condivisi per autenticazione, routing, tema e API client. */
const Utils = {
    /* DOC-FN: 'formatDate' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    formatDate(dateString) {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleDateString('it-IT', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    },

    /* DOC-FN: 'formatDateShort' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    formatDateShort(dateString) {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleDateString('it-IT', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    },

    /* DOC-FN: 'formatTime' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    formatTime(timeString) {
        if (!timeString) return '';
        return timeString.substring(0, 5);
    },

    /* DOC-FN: 'showNotification' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    showNotification(message, type = 'info') {
        let container = document.querySelector('.toast-container');
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!container) {
            container = document.createElement('div');
            container.className = 'toast-container';
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.textContent = message;
        
        container.appendChild(toast);

        setTimeout(() => {
            toast.style.animation = 'slideIn 0.3s ease-out reverse';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    },

    /* DOC-FN: 'confirmDialog' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    confirmDialog(message) {
        return window.confirm(message);
    },

    /* DOC-FN: 'showLoading' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    showLoading() {
        let overlay = document.querySelector('.loading-overlay');
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'loading-overlay';
            overlay.innerHTML = '<div class="spinner"></div>';
            document.body.appendChild(overlay);
        }
        overlay.style.display = 'flex';
    },

    /* DOC-FN: 'hideLoading' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    hideLoading() {
        const overlay = document.querySelector('.loading-overlay');
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (overlay) {
            overlay.style.display = 'none';
        }
    },

    /* DOC-FN: 'debounce' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    /* DOC-FN: 'formatId' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    formatId(prefix, id) {
        return `${prefix}-${String(id).padStart(3, '0')}`;
    },

    /* DOC-FN: '_notificationStoreKey' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    _notificationStoreKey() {
        const user = (typeof Auth !== 'undefined' && Auth.getUser) ? Auth.getUser() : null;
        const userKey = user?.id || user?.username || 'guest';
        return `appNotifications:${userKey}`;
    },

    /* DOC-FN: '_notificationCatalogKey' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    _notificationCatalogKey() {
        const user = (typeof Auth !== 'undefined' && Auth.getUser) ? Auth.getUser() : null;
        const userKey = user?.id || user?.username || 'guest';
        return `appKnownFilms:${userKey}`;
    },

    /* DOC-FN: '_readNotifications' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    _readNotifications() {
        try {
            const raw = localStorage.getItem(this._notificationStoreKey());
            const parsed = raw ? JSON.parse(raw) : [];
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    },

    /* DOC-FN: '_saveNotifications' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    _saveNotifications(items) {
        const normalized = (items || []).slice(0, 80);
        localStorage.setItem(this._notificationStoreKey(), JSON.stringify(normalized));
        window.dispatchEvent(new CustomEvent('notifications:updated', {
            detail: {
                unread: normalized.filter((n) => !n.read).length,
                count: normalized.length
            }
        }));
    },

    /* DOC-FN: '_hasServerNotificationSupport' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    _hasServerNotificationSupport() {
        return typeof ApiClient !== 'undefined' && typeof Auth !== 'undefined' && Auth.isAuthenticated();
    },

    async syncNotificationsFromServer() {
        if (!this._hasServerNotificationSupport()) return;

        try {
            const items = await ApiClient.get('/notifiche/mine');
            if (!Array.isArray(items)) return;

            const normalized = items.map((n) => ({
                id: String(n.id),
                type: n.tipo || 'info',
                title: n.titolo || 'Notifica',
                message: n.messaggio || '',
                url: n.url || '',
                read: !!n.letta,
                createdAt: n.dataCreazione || new Date().toISOString(),
                dedupeKey: n.dedupeKey || null
            }));
            this._saveNotifications(normalized);
        } catch {
        }
    },

    async _pushNotificationToServer(payload) {
        if (!this._hasServerNotificationSupport()) return;

        try {
            await ApiClient.post('/notifiche/mine', {
                tipo: payload.type || 'info',
                titolo: payload.title || 'Notifica',
                messaggio: payload.message || '',
                url: payload.url || '',
                dedupeKey: payload.dedupeKey || null
            });
        } catch {
        }
    },

    /* DOC-FN: 'addAppNotification' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    addAppNotification(payload) {
        if (!payload || !payload.title) return;

        const current = this._readNotifications();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (payload.dedupeKey && current.some((n) => n.dedupeKey === payload.dedupeKey)) {
            return;
        }

        const item = {
            id: payload.id || `n_${Date.now()}_${Math.random().toString(36).slice(2, 7)}`,
            type: payload.type || 'info',
            title: payload.title,
            message: payload.message || '',
            url: payload.url || '',
            read: false,
            createdAt: payload.createdAt || new Date().toISOString(),
            dedupeKey: payload.dedupeKey || null
        };

        this._saveNotifications([item, ...current]);
        this._pushNotificationToServer(item);
    },

    /* DOC-FN: 'listAppNotifications' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    listAppNotifications(filterType = 'all') {
        const list = this._readNotifications().sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
        if (!filterType || filterType === 'all') return list;
        return list.filter((n) => n.type === filterType);
    },

    /* DOC-FN: 'getUnreadNotificationCount' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getUnreadNotificationCount(filterType = 'all') {
        const list = this._readNotifications();
        return list.filter((n) => !n.read && (filterType === 'all' || n.type === filterType)).length;
    },

    /* DOC-FN: 'markNotificationRead' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    markNotificationRead(id) {
        if (!id) return;
        const current = this._readNotifications();
        const updated = current.map((n) => n.id === id ? { ...n, read: true } : n);
        this._saveNotifications(updated);

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (this._hasServerNotificationSupport()) {
            ApiClient.put(`/notifiche/mine/${id}/read`).catch(() => {});
        }
    },

    /* DOC-FN: 'markAllNotificationsRead' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    markAllNotificationsRead() {
        const current = this._readNotifications();
        const updated = current.map((n) => ({ ...n, read: true }));
        this._saveNotifications(updated);

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (this._hasServerNotificationSupport()) {
            ApiClient.put('/notifiche/mine/read-all').catch(() => {});
        }
    },

    /* DOC-FN: 'clearAllNotifications' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    clearAllNotifications() {
        this._saveNotifications([]);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (this._hasServerNotificationSupport()) {
            ApiClient.delete('/notifiche/mine').catch(() => {});
        }
    },

    /* DOC-FN: 'registerFilmCatalogSnapshot' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    registerFilmCatalogSnapshot(films) {
        if (!Array.isArray(films)) return;

        const ids = films.map((f) => Number(f?.id || 0)).filter((id) => id > 0);
        if (!ids.length) return;

        const key = this._notificationCatalogKey();
        let previousIds = [];
        try {
            previousIds = JSON.parse(localStorage.getItem(key) || '[]');
        } catch {
            previousIds = [];
        }

        const previousSet = new Set((Array.isArray(previousIds) ? previousIds : []).map(Number));
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!previousSet.size) {
            localStorage.setItem(key, JSON.stringify(ids));
            return;
        }

        const newFilms = films.filter((f) => {
            const id = Number(f?.id || 0);
            return id > 0 && !previousSet.has(id);
        });

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (newFilms.length) {
            const firstTitle = newFilms[0]?.titolo || 'Nuovo film';
            const title = newFilms.length === 1 ? 'Nuovo film in programmazione' : `${newFilms.length} nuovi film disponibili`;
            const message = newFilms.length === 1
                ? `${firstTitle} e ora disponibile in programmazione.`
                : `${firstTitle}${newFilms.length > 1 ? ' e altri titoli' : ''} sono stati aggiunti.`;
            this.addAppNotification({
                type: 'new-film',
                title,
                message,
                url: '/programmazione.html?tag=featured',
                dedupeKey: `film-batch-${newFilms.map((f) => f.id).join('-')}`
            });
        }

        const merged = [...new Set([...previousSet, ...ids])];
        localStorage.setItem(key, JSON.stringify(merged));
    }
};

