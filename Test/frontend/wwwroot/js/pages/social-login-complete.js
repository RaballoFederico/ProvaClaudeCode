/* DOC: Script pagina 'social-login-complete': gestisce eventi UI, chiamate API e rendering dinamico della pagina. */
(async () => {
    const statusEl = document.getElementById('status');
    const url = new URL(window.location.href);
    const provider = url.searchParams.get('provider');
    const authCode = url.searchParams.get('extAuthCode');
    const extAuthError = url.searchParams.get('extAuthError');

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (extAuthError) {
        statusEl.textContent = `Errore login esterno: ${extAuthError}`;
        return;
    }

    /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
    if (!provider || !authCode) {
        statusEl.textContent = 'Parametri mancanti per completare il login social.';
        return;
    }

    try {
        const result = await Auth.completeExternalLogin(provider, authCode);
        /* DOC-FN: 'if' gestisce logica applicativa locale (input, stato UI, chiamate API o trasformazioni dati). */
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

