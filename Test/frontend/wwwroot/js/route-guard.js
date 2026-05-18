/* DOC: Modulo JS 'route-guard': utility/comportamenti condivisi per autenticazione, routing, tema e API client. */
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

    /* DOC-FN: 'getUser' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getUser() {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (typeof Auth === 'undefined' || !Auth.isAuthenticated()) {
            return null;
        }

        return Auth.getUser() || null;
    },

    /* DOC-FN: 'getRoles' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getRoles(user) {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!user || !Array.isArray(user.ruoli)) {
            return [];
        }

        return user.ruoli
            .map(r => String(r || '').trim())
            .filter(Boolean);
    },

    /* DOC-FN: 'hasAnyRole' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    hasAnyRole(user, rolesToMatch) {
        const roles = this.getRoles(user).map(r => r.toLowerCase());
        return rolesToMatch.some(role => roles.includes(String(role || '').toLowerCase()));
    },

    /* DOC-FN: 'canAccess' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    canAccess(rule) {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (rule === null) {
            return true;
        }

        const user = this.getUser();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (rule === 'guest') {
            return !user;
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!user) {
            return false;
        }

        const allowedRoles = this.roleMap[rule] || [];
        return this.hasAnyRole(user, allowedRoles);
    },

    /* DOC-FN: 'canAccessByRule' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    canAccessByRule(rule, user) {
        if (rule === 'public' || rule === null) return true;
        if (rule === 'guest') return !user;
        if (!user) return false;

        if (rule === 'auth') return true;
        if (rule === 'manager') return this.hasAnyRole(user, ['Admin', 'PowerUser']);
        if (rule === 'admin') return this.hasAnyRole(user, ['Admin']);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (rule === 'user') {
            const isManager = this.hasAnyRole(user, ['Admin', 'PowerUser']);
            return !isManager && this.hasAnyRole(user, ['User']);
        }

        return false;
    },

    /* DOC-FN: 'getDefaultRoute' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    getDefaultRoute() {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!Auth.isAuthenticated()) {
            return '/home.html';
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (Auth.hasRole('Admin') || Auth.hasRole('PowerUser')) {
            return '/index.html';
        }

        return '/profilo.html';
    },

    /* DOC-FN: 'enforceCurrentPage' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    enforceCurrentPage() {
        const page = window.location.pathname.split('/').pop() || 'index.html';
        const rule = this.pageRules[page];

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (rule === undefined) {
            return;
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (!this.canAccess(rule)) {
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (rule === 'guest') {
                window.location.href = this.getDefaultRoute();
                return;
            }

            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
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
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (typeof Auth !== 'undefined') {
        await Auth.ensureInitialized();
        AccessControl.enforceCurrentPage();
    }
});

