// DOC: utenti - file del progetto; contiene logica specifica della feature/modulo.
/* DOC: Script pagina 'utenti': gestisce eventi UI, chiamate API e rendering dinamico della pagina. */
const utentiState = {
    users: [],
    rolesByName: {},
    confirmResolver: null,
    canManageRoles: false
};

/* DOC-FN: 'setMessage' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function setMessage(text, isError = false) {
    const message = document.getElementById('message');
    if (!message) return;
    message.textContent = text || '';
    message.className = isError ? 'mt-4 text-sm text-red-400' : 'mt-4 text-sm text-on-surface-variant';
}

/* DOC-FN: 'fmtDate' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function fmtDate(value) {
    if (!value) return '-';
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return '-';
    return d.toLocaleString('it-IT');
}

/* DOC-FN: 'fmtMoney' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function fmtMoney(value) {
    const n = Number(value ?? 0);
    return `${n.toFixed(2)} EUR`;
}

/* DOC-FN: 'countRole' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function countRole(roleName) {
    return utentiState.users.filter((u) => Array.isArray(u.ruoli) && u.ruoli.includes(roleName)).length;
}

/* DOC-FN: 'canAssignTargetRole' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function canAssignTargetRole(user, targetRoleName) {
    const roles = Array.isArray(user.ruoli) ? user.ruoli : [];
    const isAdmin = roles.includes('Admin');
    const isPowerUser = roles.includes('PowerUser');

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (isAdmin && targetRoleName !== 'Admin' && countRole('Admin') <= 1) {
        return {
            ok: false,
            reason: "Operazione bloccata: non puoi modificare il ruolo dell'ultimo Admin."
        };
    }

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (isPowerUser && targetRoleName !== 'PowerUser' && countRole('PowerUser') <= 1) {
        return {
            ok: false,
            reason: "Operazione bloccata: non puoi modificare il ruolo dell'ultimo PowerUser."
        };
    }

    return { ok: true, reason: '' };
}

/* DOC-FN: 'showConfirm' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function showConfirm(message) {
    const modal = document.getElementById('confirm-role-modal');
    const msg = document.getElementById('confirm-role-message');
    if (!modal || !msg) return await Utils.confirmDialog(message);
    msg.textContent = message;
    modal.classList.remove('hidden');

    return await new Promise((resolve) => {
        utentiState.confirmResolver = resolve;
    });
}

/* DOC-FN: 'closeConfirm' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function closeConfirm(result) {
    const modal = document.getElementById('confirm-role-modal');
    if (modal) modal.classList.add('hidden');
    const resolver = utentiState.confirmResolver;
    utentiState.confirmResolver = null;
    if (resolver) resolver(result);
}

/* DOC-FN: 'loadRoles' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function loadRoles() {
    try {
        const roles = await ApiClient.get('/admin/ruoli');
        const map = {};
        (Array.isArray(roles) ? roles : []).forEach((role) => {
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (role?.nome && Number.isFinite(role?.id)) {
                map[role.nome] = role.id;
            }
        });
        utentiState.rolesByName = map;
        return;
    } catch {
        // Fallback compatibilita con backend precedente
    }

    utentiState.rolesByName = {
        Admin: 1,
        PowerUser: 2,
        User: 3
    };
}

/* DOC-FN: 'renderUsersTable' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function renderUsersTable(users) {
    const tbody = document.getElementById('users-tbody');
    if (!tbody) return;
    tbody.innerHTML = '';

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!users.length) {
        tbody.innerHTML = `<tr><td colspan="5" class="px-3 py-4 text-center text-on-surface-variant">Nessun utente registrato.</td></tr>`;
        return;
    }

    users.forEach((user, index) => {
        const roles = Array.isArray(user.ruoli) ? user.ruoli.join(', ') : '-';
        const guardToUser = canAssignTargetRole(user, 'User');
        const canManageRoles = !!utentiState.canManageRoles;

        const row = document.createElement('tr');
        row.innerHTML = `
            <td class="px-3 py-3">${index + 1}</td>
            <td class="px-3 py-3">
                <div class="font-medium">${user.username || '-'}</div>
                <div class="text-xs text-on-surface-variant">${(user.nome || '')} ${(user.cognome || '')}</div>
            </td>
            <td class="px-3 py-3">${user.email || '-'}</td>
            <td class="px-3 py-3">${roles}</td>
            <td class="px-3 py-3">
                <div class="flex flex-wrap gap-2">
                    <button data-action="set-user" data-user-id="${user.id}" class="rounded-lg border border-outline-variant/30 px-2 py-1 text-xs hover:bg-surface-container-high ${(canManageRoles && guardToUser.ok) ? '' : 'opacity-50 cursor-not-allowed'}" ${(canManageRoles && guardToUser.ok) ? '' : 'disabled'} title="${!canManageRoles ? 'Solo Admin' : (!guardToUser.ok ? guardToUser.reason : '')}">User</button>
                    <button data-action="set-poweruser" data-user-id="${user.id}" class="rounded-lg border border-outline-variant/30 px-2 py-1 text-xs hover:bg-surface-container-high ${canManageRoles ? '' : 'opacity-50 cursor-not-allowed'}" ${canManageRoles ? '' : 'disabled'} title="${canManageRoles ? '' : 'Solo Admin'}">PowerUser</button>
                    <button data-action="set-admin" data-user-id="${user.id}" class="rounded-lg border border-outline-variant/30 px-2 py-1 text-xs hover:bg-surface-container-high ${canManageRoles ? '' : 'opacity-50 cursor-not-allowed'}" ${canManageRoles ? '' : 'disabled'} title="${canManageRoles ? '' : 'Solo Admin'}">Admin</button>
                    <button data-action="edit-profile" data-user-id="${user.id}" class="rounded-lg border border-outline-variant/30 px-2 py-1 text-xs hover:bg-surface-container-high">Modifica profilo</button>
                    <button data-action="deactivate" data-user-id="${user.id}" class="rounded-lg border border-outline-variant/30 px-2 py-1 text-xs hover:bg-surface-container-high ${canManageRoles ? '' : 'opacity-50 cursor-not-allowed'}" ${canManageRoles ? '' : 'disabled'} title="${canManageRoles ? '' : 'Solo Admin'}">Elimina account</button>
                    <button data-action="view-transactions" data-user-id="${user.id}" class="rounded-lg border border-outline-variant/30 px-2 py-1 text-xs hover:bg-surface-container-high">Storico</button>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

/* DOC-FN: 'loadUsers' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function loadUsers() {
    setMessage('Caricamento utenti...');
    try {
        const users = await ApiClient.get('/admin/utenti');
        utentiState.users = (Array.isArray(users) ? users : []).filter((u) => u.attivo !== false);
        renderUsersTable(utentiState.users);
        setMessage(`Utenti caricati: ${utentiState.users.length}`);
    } catch (error) {
        setMessage(error.message || 'Errore caricamento utenti', true);
    }
}

/* DOC-FN: 'updateRole' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function updateRole(userId, targetRoleName) {
    const me = await ApiClient.get('/auth/me');
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!me?.ruoli?.includes('Admin')) {
        throw new Error('Operazione consentita solo agli Admin.');
    }

    const user = utentiState.users.find((u) => u.id === userId);
    if (!user) throw new Error('Utente non trovato');

    const guard = canAssignTargetRole(user, targetRoleName);
    if (!guard.ok) throw new Error(guard.reason);

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!utentiState.rolesByName.Admin || !utentiState.rolesByName.PowerUser || !utentiState.rolesByName.User) {
        await loadRoles();
    }

    const roleId = utentiState.rolesByName[targetRoleName];
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!roleId) {
        throw new Error(`Ruolo ${targetRoleName} non disponibile`);
    }

    try {
        await ApiClient.put(`/admin/utenti/${userId}/ruoli`, { ruoloIds: [roleId] });
    } catch (error) {
        const msg = String(error?.message || '').toLowerCase();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (msg.includes('http 404') || msg.includes('not found')) {
            await ApiClient.put(`/admin/users/${userId}/roles`, { ruoloIds: [roleId] });
            return;
        }
        throw error;
    }
}

/* DOC-FN: 'setUserActivation' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function setUserActivation(userId, activate) {
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (activate) {
        try {
            await ApiClient.post(`/admin/users/${userId}/activate`, {});
        } catch (error) {
            const msg = String(error?.message || '').toLowerCase();
            /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
            if (msg.includes('http 404') || msg.includes('not found')) {
                // Compatibilita con backend eventualmente senza route activate legacy
                throw new Error('Riattivazione non disponibile su questa versione backend.');
            }
            throw error;
        }
        return;
    }
    try {
        await ApiClient.delete(`/admin/utenti/${userId}`);
    } catch (error) {
        const msg = String(error?.message || '').toLowerCase();
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (msg.includes('http 404') || msg.includes('not found')) {
            // Fallback backend legacy: soft delete/disattivazione
            await ApiClient.delete(`/admin/users/${userId}`);
            return;
        }
        throw error;
    }
}

/* DOC-FN: 'renderCreditHistory' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function renderCreditHistory(items) {
    const tbody = document.getElementById('credit-history-tbody');
    if (!tbody) return;
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!items.length) {
        tbody.innerHTML = `<tr><td colspan="4" class="px-3 py-3 text-on-surface-variant">Nessun movimento disponibile.</td></tr>`;
        return;
    }
    tbody.innerHTML = items.map((item) => `
        <tr>
            <td class="px-3 py-2">${fmtDate(item.dataTransazione)}</td>
            <td class="px-3 py-2">${item.tipo || '-'}</td>
            <td class="px-3 py-2">${fmtMoney(item.importo)}</td>
            <td class="px-3 py-2">${fmtMoney(item.saldoSuccessivo)}</td>
        </tr>
    `).join('');
}

/* DOC-FN: 'renderOrdersHistory' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function renderOrdersHistory(items) {
    const tbody = document.getElementById('orders-history-tbody');
    if (!tbody) return;
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!items.length) {
        tbody.innerHTML = `<tr><td colspan="4" class="px-3 py-3 text-on-surface-variant">Nessun acquisto disponibile.</td></tr>`;
        return;
    }
    tbody.innerHTML = items.map((item) => `
        <tr>
            <td class="px-3 py-2">${fmtDate(item.dataAcquisto)}</td>
            <td class="px-3 py-2">${fmtMoney(item.importoTotale)}</td>
            <td class="px-3 py-2">${item.metodoPagamentoEtichetta || item.metodoPagamento || '-'}</td>
            <td class="px-3 py-2">${item.stato || '-'}</td>
        </tr>
    `).join('');
}

/* DOC-FN: 'renderTicketsHistory' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function renderTicketsHistory(items) {
    const tbody = document.getElementById('tickets-history-tbody');
    if (!tbody) return;
    if (!items.length) {
        tbody.innerHTML = `<tr><td colspan="4" class="px-3 py-3 text-on-surface-variant">Nessun biglietto disponibile.</td></tr>`;
        return;
    }

    tbody.innerHTML = items.map((item) => `
        <tr>
            <td class="px-3 py-2">${fmtDate(item.dataShow)}</td>
            <td class="px-3 py-2">${item.filmTitolo || '-'}</td>
            <td class="px-3 py-2">${item.posto || '-'}</td>
            <td class="px-3 py-2">${item.validato ? 'Validato' : 'Non validato'}</td>
        </tr>
    `).join('');
}

/* DOC-FN: 'renderTransactionsSummary' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function renderTransactionsSummary(summary) {
    const box = document.getElementById('transactions-summary');
    if (!box) return;

    const acquistiTotali = Number(summary?.acquistiTotali ?? 0);
    const spesoTotale = Number(summary?.spesoTotale ?? 0);
    const bigliettiTotali = Number(summary?.bigliettiTotali ?? 0);
    const creditoMovimenti = Number(summary?.movimentiCreditoTotali ?? 0);

    box.innerHTML = `
        <div class="rounded-xl border border-outline-variant/20 bg-surface-container-high/45 p-3">
            <div class="text-xs text-on-surface-variant">Acquisti totali</div>
            <div class="mt-1 text-lg font-semibold">${acquistiTotali}</div>
        </div>
        <div class="rounded-xl border border-outline-variant/20 bg-surface-container-high/45 p-3">
            <div class="text-xs text-on-surface-variant">Spesa totale</div>
            <div class="mt-1 text-lg font-semibold">${fmtMoney(spesoTotale)}</div>
        </div>
        <div class="rounded-xl border border-outline-variant/20 bg-surface-container-high/45 p-3">
            <div class="text-xs text-on-surface-variant">Biglietti emessi</div>
            <div class="mt-1 text-lg font-semibold">${bigliettiTotali}</div>
        </div>
        <div class="rounded-xl border border-outline-variant/20 bg-surface-container-high/45 p-3">
            <div class="text-xs text-on-surface-variant">Movimenti credito</div>
            <div class="mt-1 text-lg font-semibold">${creditoMovimenti}</div>
        </div>
    `;
}

/* DOC-FN: 'loadUserTransactions' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
async function loadUserTransactions(userId) {
    const panel = document.getElementById('transactions-panel');
    const title = document.getElementById('transactions-user');
    if (!panel || !title) return;

    panel.classList.remove('hidden');
    title.textContent = 'Caricamento storico transazioni...';

    try {
        let response;
        try {
            response = await ApiClient.get(`/admin/utenti/${userId}/transazioni`);
        } catch {
            const fallbackCredito = await ApiClient.get(`/admin/credito/storico/${userId}`);
            response = {
                utente: utentiState.users.find((u) => u.id === userId) || null,
                storicoCredito: Array.isArray(fallbackCredito) ? fallbackCredito : [],
                storicoAcquisti: []
            };
        }

        const user = response.utente || {};
        const fullName = `${user.nome || ''} ${user.cognome || ''}`.trim();
        title.textContent = `${fullName || user.username || '-'} (${user.email || '-'})`;
        renderTransactionsSummary(response.summary || {});
        renderCreditHistory(Array.isArray(response.storicoCredito) ? response.storicoCredito : []);
        renderOrdersHistory(Array.isArray(response.storicoAcquisti) ? response.storicoAcquisti : []);
        renderTicketsHistory(Array.isArray(response.biglietti) ? response.biglietti : []);
    } catch (error) {
        title.textContent = error.message || 'Errore caricamento storico transazioni.';
        renderTransactionsSummary({});
        renderCreditHistory([]);
        renderOrdersHistory([]);
        renderTicketsHistory([]);
    }
}

/* DOC-FN: 'openEditUserModal' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function openEditUserModal(userId) {
    const user = utentiState.users.find((u) => u.id === userId);
    if (!user) return;
    document.getElementById('edit-user-id').value = String(user.id);
    document.getElementById('edit-user-email').value = user.email || '';
    document.getElementById('edit-user-nome').value = user.nome || '';
    document.getElementById('edit-user-cognome').value = user.cognome || '';
    document.getElementById('edit-user-telefono').value = user.telefono || '';
    document.getElementById('edit-user-modal').classList.remove('hidden');
}

/* DOC-FN: 'closeEditUserModal' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
function closeEditUserModal() {
    document.getElementById('edit-user-modal')?.classList.add('hidden');
}

document.addEventListener('click', async (event) => {
    const button = event.target.closest('button[data-action][data-user-id]');
    if (!button) return;

    const userId = Number(button.getAttribute('data-user-id'));
    const action = button.getAttribute('data-action');
    if (!Number.isFinite(userId) || !action) return;

    try {
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (action === 'set-user') {
            const ok = await showConfirm('Confermi di riportare questo account al ruolo User?');
            if (!ok) return;
            await updateRole(userId, 'User');
            setMessage('Ruolo aggiornato a User.');
            await loadUsers();
            return;
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (action === 'set-poweruser') {
            const ok = await showConfirm('Confermi di impostare questo account come PowerUser?');
            if (!ok) return;
            await updateRole(userId, 'PowerUser');
            setMessage('Ruolo aggiornato a PowerUser.');
            await loadUsers();
            return;
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (action === 'set-admin') {
            const ok = await showConfirm('Confermi di impostare questo account come Admin?');
            if (!ok) return;
            await updateRole(userId, 'Admin');
            setMessage('Ruolo aggiornato a Admin.');
            await loadUsers();
            return;
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (action === 'deactivate') {
            const ok = await showConfirm('Confermi eliminazione definitiva dell\'account? Questa operazione non e annullabile.');
            if (!ok) return;
            await setUserActivation(userId, false);
            setMessage('Account eliminato.');
            await loadUsers();
            return;
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (action === 'view-transactions') {
            await loadUserTransactions(userId);
            return;
        }

        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
        if (action === 'edit-profile') {
            openEditUserModal(userId);
        }
    } catch (error) {
        setMessage(error.message || 'Operazione non riuscita', true);
    }
});

document.addEventListener('DOMContentLoaded', async () => {
    const ok = await Auth.ensureInitialized();
    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!ok || !Auth.isAuthenticated() || (!Auth.hasRole('Admin') && !Auth.hasRole('PowerUser'))) {
        window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname);
        return;
    }

    utentiState.canManageRoles = Auth.hasRole('Admin');

    document.getElementById('reload-users-btn')?.addEventListener('click', loadUsers);
    document.getElementById('close-transactions-btn')?.addEventListener('click', () => {
        document.getElementById('transactions-panel')?.classList.add('hidden');
    });

    document.getElementById('close-edit-user-modal')?.addEventListener('click', closeEditUserModal);
    document.getElementById('cancel-edit-user-modal')?.addEventListener('click', closeEditUserModal);
    document.getElementById('edit-user-form')?.addEventListener('submit', async (event) => {
        event.preventDefault();
        const id = Number(document.getElementById('edit-user-id').value);
        const email = document.getElementById('edit-user-email').value.trim();
        const nome = document.getElementById('edit-user-nome').value.trim();
        const cognome = document.getElementById('edit-user-cognome').value.trim();
        const telefono = document.getElementById('edit-user-telefono').value.trim();

        try {
            await ApiClient.put(`/admin/utenti/${id}/profilo`, { email, nome, cognome, telefono });
            closeEditUserModal();
            setMessage('Profilo utente aggiornato.');
            await loadUsers();
        } catch (error) {
            setMessage(error.message || 'Errore aggiornamento profilo', true);
        }
    });

    document.getElementById('confirm-role-cancel')?.addEventListener('click', () => closeConfirm(false));
    document.getElementById('confirm-role-ok')?.addEventListener('click', () => closeConfirm(true));

    await loadRoles();
    await loadUsers();
});


