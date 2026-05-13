const AccessControl = {
    roleMap: {
        admin: ['Admin'],
        manager: ['Admin', 'PowerUser'],
        auth: ['Admin', 'PowerUser', 'User'],
        user: ['User']
    },

    pageRules: {
        'home.html': 'guest',
        'index.html': 'manager',
        'films.html': 'auth',
        'proiezioni.html': 'manager',
        'shows.html': 'manager',
        'sale.html': 'admin',
        'cinemas.html': 'auth',
        'registi.html': 'manager',
        'categorie.html': 'admin',
        'profilo.html': 'auth',
        'programmazione.html': null,
        'scheda-film.html': null,
        'my-cinemas.html': null,
        'conferma-acquisto.html': 'auth',
        'validazione.html': 'manager',
        'ricarica-credito.html': 'manager',
        'ricarica-credito-utente.html': 'user',
        'acquista.html': 'auth',
        'pagamento.html': 'auth',
        'user-biglietti.html': 'auth',
        'check-in.html': 'auth',
        'proiezioni-pubblico.html': null,
        'supporto.html': null,
        'privacy.html': null,
        'termini.html': null,
        '404.html': null,
        'login.html': 'guest',
        'register.html': 'guest',
        'recupera-password.html': 'guest',
        'reimposta-password.html': 'guest',
        'social-login-complete.html': 'guest',
        'utenti.html': 'admin'
        ,
        'abbonamenti.html': 'auth',
        'newsletter-admin.html': 'admin'
    },

    getUser() {
        if (typeof Auth === 'undefined' || !Auth.isAuthenticated()) {
            return null;
        }

        return Auth.getUser() || null;
    },

    getRoles(user) {
        if (!user || !Array.isArray(user.ruoli)) {
            return [];
        }

        return user.ruoli
            .map(r => String(r || '').trim())
            .filter(Boolean);
    },

    hasAnyRole(user, rolesToMatch) {
        const roles = this.getRoles(user).map(r => r.toLowerCase());
        return rolesToMatch.some(role => roles.includes(String(role || '').toLowerCase()));
    },

    canAccess(rule) {
        if (rule === null) {
            return true;
        }

        const user = this.getUser();
        if (rule === 'guest') {
            return !user;
        }

        if (!user) {
            return false;
        }

        const allowedRoles = this.roleMap[rule] || [];
        return this.hasAnyRole(user, allowedRoles);
    },

    canAccessByRule(rule, user) {
        if (rule === 'public' || rule === null) return true;
        if (rule === 'guest') return !user;
        if (!user) return false;

        if (rule === 'auth') return true;
        if (rule === 'manager') return this.hasAnyRole(user, ['Admin', 'PowerUser']);
        if (rule === 'admin') return this.hasAnyRole(user, ['Admin']);
        if (rule === 'user') {
            const isManager = this.hasAnyRole(user, ['Admin', 'PowerUser']);
            return !isManager && this.hasAnyRole(user, ['User']);
        }

        return false;
    },

    getDefaultRoute() {
        if (!Auth.isAuthenticated()) {
            return '/home.html';
        }

        if (Auth.hasRole('Admin') || Auth.hasRole('PowerUser')) {
            return '/index.html';
        }

        return '/profilo.html';
    },

    enforceCurrentPage() {
        const page = window.location.pathname.split('/').pop() || 'index.html';
        const rule = this.pageRules[page];

        if (rule === undefined) {
            return;
        }

        if (!this.canAccess(rule)) {
            if (rule === 'guest') {
                window.location.href = this.getDefaultRoute();
                return;
            }

            if (!Auth.isAuthenticated()) {
                window.location.href = `/login.html?redirect=${encodeURIComponent(window.location.pathname + window.location.search)}`;
                return;
            }

            window.location.href = this.getDefaultRoute();
        }
    }
};

window.AccessControl = AccessControl;

document.addEventListener('DOMContentLoaded', async () => {
    if (typeof Auth !== 'undefined') {
        await Auth.ensureInitialized();
        AccessControl.enforceCurrentPage();
    }
});
