document.getElementById('reset-form').addEventListener('submit', async (event) => {
    event.preventDefault();

    const params = new URLSearchParams(window.location.search);
    const token = (params.get('token') || '').trim();
    const newPassword = document.getElementById('new-password').value;
    const confirmPassword = document.getElementById('confirm-password').value;
    const message = document.getElementById('message');
    message.textContent = '';

    if (!token) {
        message.textContent = 'Token mancante o non valido.';
        return;
    }

    if (newPassword !== confirmPassword) {
        message.textContent = 'Le password non coincidono.';
        return;
    }

    const result = await Auth.resetPassword(token, newPassword);
    message.textContent = result.success
        ? (result.message || 'Password aggiornata con successo.')
        : (result.error || 'Errore durante il reset password.');
});
