async function loadUsers() {
    const message = document.getElementById('message');
    const tbody = document.querySelector('#users-table tbody');
    message.textContent = '';
    tbody.innerHTML = '';

    try {
        const users = await ApiClient.get('/admin/utenti');
        users.forEach((user) => {
            const tr = document.createElement('tr');
            const roles = (user.ruoli || []).join(', ');
            tr.innerHTML = `
                <td>${user.id}</td>
                <td>${user.username}</td>
                <td>${user.email}</td>
                <td>${roles}</td>
                <td>
                    <button data-user-id="${user.id}" data-promote="power">PowerUser</button>
                    <button data-user-id="${user.id}" data-promote="admin">Admin</button>
                </td>
            `;
            tbody.appendChild(tr);
        });
    } catch (error) {
        message.textContent = error.message || 'Errore caricamento utenti';
    }
}

async function promoteUser(userId, targetRole) {
    const message = document.getElementById('message');
    message.textContent = '';

    try {
        const rolesResp = await ApiClient.get('/auth/me');
        const currentUser = rolesResp;
        if (!currentUser?.ruoli?.includes('Admin')) {
            message.textContent = 'Operazione consentita solo agli Admin.';
            return;
        }

        const roleIds = targetRole === 'admin' ? [2] : [1];
        await ApiClient.put(`/admin/utenti/${userId}/ruoli`, { ruoloIds: roleIds });
        message.textContent = 'Ruolo aggiornato con successo.';
        await loadUsers();
    } catch (error) {
        message.textContent = error.message || 'Errore aggiornamento ruolo';
    }
}

document.addEventListener('click', async (event) => {
    const button = event.target.closest('button[data-user-id]');
    if (!button) return;
    const userId = Number(button.getAttribute('data-user-id'));
    const targetRole = button.getAttribute('data-promote');
    if (!Number.isFinite(userId) || !targetRole) return;
    await promoteUser(userId, targetRole);
});

document.addEventListener('DOMContentLoaded', async () => {
    const ok = await Auth.ensureInitialized();
    if (!ok || !Auth.isAuthenticated() || !Auth.hasRole('Admin')) {
        window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname);
        return;
    }

    await loadUsers();
});
