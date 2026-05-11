document.getElementById('recover-form').addEventListener('submit', async (event) => {
    event.preventDefault();

    const email = document.getElementById('email').value.trim();
    const message = document.getElementById('message');
    message.textContent = '';

    const result = await Auth.forgotPassword(email, window.location.origin + '/reimposta-password.html');
    if (!result.success) {
        message.textContent = result.error || 'Errore durante la richiesta';
        return;
    }

    message.textContent = result.message || 'Se esiste un account associato, riceverai una email con le istruzioni.';
});
