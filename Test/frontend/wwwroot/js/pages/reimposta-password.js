// DOC: reimposta-password - file del progetto; contiene logica specifica della feature/modulo.
/* DOC: Script pagina 'reimposta-password': gestisce eventi UI, chiamate API e rendering dinamico della pagina. */
document.getElementById('reset-form').addEventListener('submit', async (event) => {
    event.preventDefault();

    const params = new URLSearchParams(window.location.search);
    const token = (params.get('token') || '').trim();
    const newPassword = document.getElementById('new-password').value;
    const confirmPassword = document.getElementById('confirm-password').value;
    const message = document.getElementById('message');
    message.textContent = '';

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!token) {
        message.textContent = 'Token mancante o non valido.';
        return;
    }

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (newPassword !== confirmPassword) {
        message.textContent = 'Le password non coincidono.';
        return;
    }

    const result = await Auth.resetPassword(token, newPassword);
    message.textContent = result.success
        ? (result.message || 'Password aggiornata con successo.')
        : (result.error || 'Errore durante il reset password.');
});


