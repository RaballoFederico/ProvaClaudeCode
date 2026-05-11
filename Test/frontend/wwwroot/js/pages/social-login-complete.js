(async () => {
    const statusEl = document.getElementById('status');
    const url = new URL(window.location.href);
    const provider = url.searchParams.get('provider');
    const authCode = url.searchParams.get('extAuthCode');
    const extAuthError = url.searchParams.get('extAuthError');

    if (extAuthError) {
        statusEl.textContent = `Errore login esterno: ${extAuthError}`;
        return;
    }

    if (!provider || !authCode) {
        statusEl.textContent = 'Parametri mancanti per completare il login social.';
        return;
    }

    try {
        const result = await Auth.completeExternalLogin(provider, authCode);
        if (!result.success) {
            statusEl.textContent = result.error || 'Completamento login esterno fallito.';
            return;
        }

        statusEl.textContent = 'Login completato. Reindirizzamento...';
        window.location.href = 'index.html';
    } catch (error) {
        statusEl.textContent = error?.message || 'Errore durante il completamento del login esterno.';
    }
})();
