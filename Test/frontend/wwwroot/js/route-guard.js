const RouteGuard = {
    roleMap: {
        admin: ['Admin'],
        manager: ['Admin', 'PowerUser'],
        auth: ['Admin', 'PowerUser', 'User']
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
        'acquista.html': 'auth',
        'pagamento.html': 'auth',
        'user-biglietti.html': 'auth',
        'check-in.html': 'auth',
        'proiezioni-pubblico.html': 'auth',
        '404.html': null,
        'login.html': 'guest',
        'register.html': 'guest'
    },

    canAccess(rule) {
        if (rule === null) {
            return true;
        }

        if (rule === 'guest') {
            return !Auth.isAuthenticated();
        }

        if (!Auth.isAuthenticated()) {
            return false;
        }

        const allowedRoles = this.roleMap[rule] || [];
        return allowedRoles.some(role => Auth.hasRole(role));
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

document.addEventListener('DOMContentLoaded', async () => {
    if (typeof Auth !== 'undefined') {
        await Auth.ensureInitialized();
        RouteGuard.enforceCurrentPage();
    }
});
