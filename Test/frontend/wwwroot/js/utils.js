const Utils = {
    formatDate(dateString) {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleDateString('it-IT', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    },

    formatDateShort(dateString) {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleDateString('it-IT', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    },

    formatTime(timeString) {
        if (!timeString) return '';
        return timeString.substring(0, 5);
    },

    showNotification(message, type = 'info') {
        let container = document.querySelector('.toast-container');
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

    confirmDialog(message) {
        return window.confirm(message);
    },

    showLoading() {
        let overlay = document.querySelector('.loading-overlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'loading-overlay';
            overlay.innerHTML = '<div class="spinner"></div>';
            document.body.appendChild(overlay);
        }
        overlay.style.display = 'flex';
    },

    hideLoading() {
        const overlay = document.querySelector('.loading-overlay');
        if (overlay) {
            overlay.style.display = 'none';
        }
    },

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

    formatId(prefix, id) {
        return `${prefix}-${String(id).padStart(3, '0')}`;
    },

    _notificationStoreKey() {
        const user = (typeof Auth !== 'undefined' && Auth.getUser) ? Auth.getUser() : null;
        const userKey = user?.id || user?.username || 'guest';
        return `appNotifications:${userKey}`;
    },

    _notificationCatalogKey() {
        const user = (typeof Auth !== 'undefined' && Auth.getUser) ? Auth.getUser() : null;
        const userKey = user?.id || user?.username || 'guest';
        return `appKnownFilms:${userKey}`;
    },

    _readNotifications() {
        try {
            const raw = localStorage.getItem(this._notificationStoreKey());
            const parsed = raw ? JSON.parse(raw) : [];
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    },

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

    addAppNotification(payload) {
        if (!payload || !payload.title) return;

        const current = this._readNotifications();
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

    listAppNotifications(filterType = 'all') {
        const list = this._readNotifications().sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
        if (!filterType || filterType === 'all') return list;
        return list.filter((n) => n.type === filterType);
    },

    getUnreadNotificationCount(filterType = 'all') {
        const list = this._readNotifications();
        return list.filter((n) => !n.read && (filterType === 'all' || n.type === filterType)).length;
    },

    markNotificationRead(id) {
        if (!id) return;
        const current = this._readNotifications();
        const updated = current.map((n) => n.id === id ? { ...n, read: true } : n);
        this._saveNotifications(updated);

        if (this._hasServerNotificationSupport()) {
            ApiClient.put(`/notifiche/mine/${id}/read`).catch(() => {});
        }
    },

    markAllNotificationsRead() {
        const current = this._readNotifications();
        const updated = current.map((n) => ({ ...n, read: true }));
        this._saveNotifications(updated);

        if (this._hasServerNotificationSupport()) {
            ApiClient.put('/notifiche/mine/read-all').catch(() => {});
        }
    },

    clearAllNotifications() {
        this._saveNotifications([]);
        if (this._hasServerNotificationSupport()) {
            ApiClient.delete('/notifiche/mine').catch(() => {});
        }
    },

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
        if (!previousSet.size) {
            localStorage.setItem(key, JSON.stringify(ids));
            return;
        }

        const newFilms = films.filter((f) => {
            const id = Number(f?.id || 0);
            return id > 0 && !previousSet.has(id);
        });

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
