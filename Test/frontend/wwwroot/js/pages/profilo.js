// js/pages/profilo.js
// Gestione area personale utente

let profiloData = null;
let proiezioneDaPrenotare = null;

function getProfilePictureStorageKey(userId) {
    return `profile_picture_user_${userId}`;
}

function getStoredProfilePicture(userId) {
    if (!userId) return null;
    return localStorage.getItem(getProfilePictureStorageKey(userId));
}

function setStoredProfilePicture(userId, dataUrl) {
    if (!userId) return;
    localStorage.setItem(getProfilePictureStorageKey(userId), dataUrl);
}

function applyProfilePictureToUI() {
    if (!profiloData?.id) return;

    const profileAvatarImg = document.getElementById('profile-avatar-img');
    const initialsSpan = document.getElementById('user-initials');
    if (!profileAvatarImg || !initialsSpan) return;

    const picture = getStoredProfilePicture(profiloData.id);
    const hasPicture = typeof picture === 'string' && picture.startsWith('data:image/');
    profileAvatarImg.classList.toggle('hidden', !hasPicture);
    initialsSpan.classList.toggle('hidden', hasPicture);

    if (hasPicture) {
        profileAvatarImg.src = picture;
    } else {
        profileAvatarImg.removeAttribute('src');
    }

    const navAvatar = document.getElementById('nav-user-avatar-image');
    const navInitials = document.getElementById('nav-user-initials');
    if (navAvatar && navInitials) {
        navAvatar.classList.toggle('hidden', !hasPicture);
        navInitials.classList.toggle('hidden', hasPicture);
        if (hasPicture) {
            navAvatar.src = picture;
        } else {
            navAvatar.removeAttribute('src');
        }
    }
}

// Carica il profilo
async function loadProfile() {
    try {
        showLoading(true);
        
        profiloData = await ApiClient.get('/user/profile');
        
        showLoading(false);
        renderProfile();
        await loadNewsletterPreference();
        await loadStoricoAcquistiRimborsi();
        renderProiezioni();
        handlePrenotaQueryParam();
    } catch (error) {
        showLoading(false);
        showError(error.message);
    }
}

async function loadNewsletterPreference() {
    const checkbox = document.getElementById('newsletter-consent');
    if (!checkbox) return;
    try {
        const res = await ApiClient.get('/newsletter/preference');
        checkbox.checked = !!res?.consenso;
    } catch {
        checkbox.checked = false;
    }
}

function formatMoney(value) {
    const amount = Number(value ?? 0);
    return `${amount.toFixed(2)} EUR`;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text || '';
    return div.innerHTML;
}

async function loadStoricoAcquistiRimborsi() {
    const acquistiList = document.getElementById('acquisti-list');
    const acquistiEmpty = document.getElementById('acquisti-empty');
    const rimborsiList = document.getElementById('rimborsi-list');
    const rimborsiEmpty = document.getElementById('rimborsi-empty');

    if (!acquistiList || !acquistiEmpty || !rimborsiList || !rimborsiEmpty) return;

    acquistiList.innerHTML = '';
    rimborsiList.innerHTML = '';
    acquistiEmpty.classList.add('hidden');
    rimborsiEmpty.classList.add('hidden');

    try {
        const [acquisti, storicoCredito] = await Promise.all([
            ApiClient.get('/user/acquisti'),
            ApiClient.get('/user/credito/storico')
        ]);

        const acquistiSafe = Array.isArray(acquisti) ? acquisti : [];
        const creditoSafe = Array.isArray(storicoCredito) ? storicoCredito : [];

        if (!acquistiSafe.length) {
            acquistiEmpty.classList.remove('hidden');
        } else {
            acquistiList.innerHTML = acquistiSafe.slice(0, 8).map((a) => {
                const stato = String(a.stato || '').toUpperCase();
                const statoClass = stato === 'REFUNDED'
                    ? 'text-sky-300 bg-sky-500/15 border-sky-500/30'
                    : stato === 'CANCELLED'
                        ? 'text-red-300 bg-red-500/15 border-red-500/30'
                        : 'text-emerald-300 bg-emerald-500/15 border-emerald-500/30';

                const metodo = a.metodoPagamentoEtichetta || a.metodoPagamento || 'N/D';

                return `
                    <div class="rounded-xl border border-outline-variant/20 bg-surface-container-high px-3 py-2 text-sm">
                        <div class="flex flex-wrap items-center justify-between gap-2">
                            <span class="font-medium text-on-surface">Acquisto #${a.id}</span>
                            <span class="inline-flex items-center rounded-full border px-2 py-0.5 text-xs ${statoClass}">
                                ${escapeHtml(stato || 'N/D')}
                            </span>
                        </div>
                        <div class="mt-1 text-on-surface-variant text-xs">${new Date(a.dataAcquisto).toLocaleString('it-IT')}</div>
                        <div class="mt-1 text-on-surface-variant text-xs">Importo: <span class="text-on-surface">${formatMoney(a.importoTotale)}</span></div>
                        <div class="text-on-surface-variant text-xs">Credito usato: <span class="text-on-surface">${formatMoney(a.creditoUsato)}</span></div>
                        <div class="text-on-surface-variant text-xs">Metodo: <span class="text-on-surface">${escapeHtml(metodo)}</span></div>
                    </div>
                `;
            }).join('');
        }

        const rimborsiOMovimenti = creditoSafe
            .filter((t) => {
                const tipo = String(t.tipo || '').toUpperCase();
                return tipo === 'RIMBORSO' || tipo === 'RICARICA' || tipo === 'ACQUISTO';
            })
            .slice(0, 10);

        if (!rimborsiOMovimenti.length) {
            rimborsiEmpty.classList.remove('hidden');
        } else {
            rimborsiList.innerHTML = rimborsiOMovimenti.map((t) => {
                const tipo = String(t.tipo || '').toUpperCase();
                const isRefund = tipo === 'RIMBORSO';
                const amount = Number(t.importo || 0);
                const amountClass = amount >= 0 ? 'text-emerald-300' : 'text-red-300';
                const badgeClass = isRefund
                    ? 'text-sky-300 bg-sky-500/15 border-sky-500/30'
                    : (tipo === 'RICARICA'
                        ? 'text-emerald-300 bg-emerald-500/15 border-emerald-500/30'
                        : 'text-amber-300 bg-amber-500/15 border-amber-500/30');

                return `
                    <div class="rounded-xl border border-outline-variant/20 bg-surface-container-high px-3 py-2 text-sm">
                        <div class="flex flex-wrap items-center justify-between gap-2">
                            <span class="inline-flex items-center rounded-full border px-2 py-0.5 text-xs ${badgeClass}">
                                ${escapeHtml(tipo)}
                            </span>
                            <span class="font-medium ${amountClass}">${formatMoney(amount)}</span>
                        </div>
                        <div class="mt-1 text-on-surface-variant text-xs">${new Date(t.dataTransazione).toLocaleString('it-IT')}</div>
                        <div class="mt-1 text-on-surface-variant text-xs">Saldo dopo operazione: <span class="text-on-surface">${formatMoney(t.saldoSuccessivo)}</span></div>
                        ${t.descrizione ? `<div class="mt-1 text-on-surface-variant text-xs">${escapeHtml(t.descrizione)}</div>` : ''}
                    </div>
                `;
            }).join('');
        }
    } catch {
        acquistiEmpty.textContent = 'Impossibile caricare lo storico acquisti.';
        rimborsiEmpty.textContent = 'Impossibile caricare lo storico rimborsi.';
        acquistiEmpty.classList.remove('hidden');
        rimborsiEmpty.classList.remove('hidden');
    }
}

function handlePrenotaQueryParam() {
    const params = new URLSearchParams(window.location.search);
    const prenotaParam = params.get('prenota');
    if (!prenotaParam) return;

    const proiezioneId = parseInt(prenotaParam);
    if (Number.isNaN(proiezioneId)) return;

    const target = (profiloData.proiezioniSalvate || []).find(p => p.proiezioneId === proiezioneId && !p.prenotato);
    if (!target) return;

    const dataProiezione = new Date(target.dataProiezione);
    const dataFormattata = dataProiezione.toLocaleDateString('it-IT', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
        year: 'numeric'
    });

    apriPrenotazione(target.id, target.filmTitolo, target.cinemaNome, dataFormattata);
    params.delete('prenota');
    const query = params.toString();
    const nextUrl = query ? `${window.location.pathname}?${query}` : window.location.pathname;
    window.history.replaceState({}, '', nextUrl);
}

function showLoading(show) {
    document.getElementById('loading').classList.toggle('hidden', !show);
    document.getElementById('content').classList.toggle('hidden', show);
    document.getElementById('error').classList.add('hidden');
}

function showError(message) {
    document.getElementById('loading').classList.add('hidden');
    document.getElementById('content').classList.add('hidden');
    document.getElementById('error').classList.remove('hidden');
    document.getElementById('error-text').textContent = message;
}

function renderProfile() {
    const user = profiloData;
    
    // Avatar iniziali
    const initials = (user.nome?.[0] || '') + (user.cognome?.[0] || '') || user.username[0].toUpperCase();
    document.getElementById('user-initials').textContent = initials.toUpperCase();
    
    // Nome completo
    const fullName = user.nome && user.cognome ? `${user.nome} ${user.cognome}` : user.username;
    document.getElementById('user-fullname').textContent = fullName;
    document.getElementById('user-username').textContent = '@' + user.username;
    
    // Ruoli
    const rolesContainer = document.getElementById('user-roles');
    rolesContainer.innerHTML = user.ruoli.map(ruolo => {
        const color = ruolo === 'Admin' ? 'bg-red-500/20 text-red-400' : 
                     ruolo === 'PowerUser' ? 'bg-yellow-500/20 text-yellow-400' : 
                     'bg-blue-500/20 text-blue-400';
        return `<span class="px-2 py-1 rounded-full text-xs font-medium ${color}">${ruolo}</span>`;
    }).join('');
    
    // Info profilo
    document.getElementById('profile-email').textContent = user.email;
    document.getElementById('profile-telefono').textContent = user.telefono || '-';
    document.getElementById('profile-data-reg').textContent = new Date(user.dataRegistrazione).toLocaleDateString('it-IT');
    document.getElementById('profile-payment-method').textContent = user.metodoPagamentoPreferitoEtichetta || user.metodoPagamentoPreferito || '-';
    
    // Popola form modifica
    document.getElementById('edit-email').value = user.email;
    document.getElementById('edit-nome').value = user.nome || '';
    document.getElementById('edit-cognome').value = user.cognome || '';
    document.getElementById('edit-telefono').value = user.telefono || '';
    applyProfilePictureToUI();
}

function renderProiezioni() {
    const list = document.getElementById('proiezioni-list');
    const noProiezioni = document.getElementById('no-proiezioni');
    
    if (!profiloData.proiezioniSalvate || profiloData.proiezioniSalvate.length === 0) {
        list.innerHTML = '';
        noProiezioni.classList.remove('hidden');
        return;
    }
    
    noProiezioni.classList.add('hidden');
    
    list.innerHTML = profiloData.proiezioniSalvate.map(p => {
        const isPrenotato = p.prenotato;
        const dataProiezione = new Date(p.dataProiezione);
        const dataFormattata = dataProiezione.toLocaleDateString('it-IT', { 
            weekday: 'long', 
            day: 'numeric', 
            month: 'long',
            year: 'numeric'
        });
        
        return `
            <div class="bg-surface-container rounded-xl p-4 border border-outline-variant/20 hover:border-outline-variant/40 transition-colors hover-lift reveal-up" style="animation-delay:${(p.id % 8) * 0.03}s">
                <div class="flex flex-col md:flex-row md:items-center gap-4">
                    <div class="flex-1">
                        <h4 class="font-bold text-on-surface">${p.filmTitolo}</h4>
                        <p class="text-sm text-on-surface-variant mt-1">
                            <span class="material-symbols-outlined text-xs align-middle">theaters</span>
                            ${p.cinemaNome}
                        </p>
                        <p class="text-sm text-on-surface-variant">
                            <span class="material-symbols-outlined text-xs align-middle">schedule</span>
                            ${dataFormattata} alle ${p.oraProiezione.substring(0, 5)}
                        </p>
                        <p class="text-xs text-on-surface-variant/60 mt-2">
                            Salvata il ${new Date(p.dataSalvataggio).toLocaleDateString('it-IT')}
                        </p>
                    </div>
                    
                    <div class="flex items-center gap-3">
                        ${isPrenotato ? `
                            <div class="flex items-center gap-2 px-3 py-2 bg-green-500/20 rounded-lg">
                                <span class="material-symbols-outlined text-green-400 text-sm">check_circle</span>
                                <span class="text-sm text-green-400">${p.numeroPosti} posti prenotati</span>
                            </div>
                            <button onclick="vaiABiglietti(${p.showId || 0})" class="px-4 py-2 bg-surface-container-high border border-outline-variant/30 rounded-lg text-sm text-on-surface transition-colors flex items-center gap-2 hover-lift" title="Apri biglietti">
                                <span class="material-symbols-outlined text-sm">confirmation_number</span>
                                Biglietti
                            </button>
                            <button onclick="annullaPrenotazione(${p.id})" class="p-2 text-on-surface-variant hover:text-red-400 transition-colors" title="Annulla prenotazione">
                                <span class="material-symbols-outlined">cancel</span>
                            </button>
                        ` : `
                            <button onclick="vaiAPrenotazione(${p.id}, ${p.showId || 0}, ${p.proiezioneId || 0})" 
                                class="px-4 py-2 bg-primary-container text-white rounded-lg text-sm transition-colors flex items-center gap-2 hover-lift">
                                <span class="material-symbols-outlined text-sm">confirmation_number</span>
                                Prenota
                            </button>
                        `}
                        <button onclick="rimuoviProiezione(${p.id})" class="p-2 text-on-surface-variant hover:text-red-400 transition-colors" title="Rimuovi dai salvati">
                            <span class="material-symbols-outlined">delete</span>
                        </button>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

// Modifica profilo
document.getElementById('edit-profile-btn').addEventListener('click', () => {
    document.getElementById('edit-modal').classList.remove('hidden');
});

document.getElementById('close-modal').addEventListener('click', () => {
    document.getElementById('edit-modal').classList.add('hidden');
    document.getElementById('edit-error').classList.add('hidden');
});

document.getElementById('cancel-edit').addEventListener('click', () => {
    document.getElementById('edit-modal').classList.add('hidden');
    document.getElementById('edit-error').classList.add('hidden');
});

document.getElementById('change-picture-btn').addEventListener('click', () => {
    const input = document.getElementById('profile-picture-input');
    if (input) {
        input.value = '';
        input.click();
    }
});

document.getElementById('profile-picture-input').addEventListener('change', async (event) => {
    const file = event.target.files?.[0];
    if (!file || !profiloData?.id) return;

    if (!file.type.startsWith('image/')) {
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Seleziona un file immagine valido', 'error');
        }
        return;
    }

    if (file.size > 2 * 1024 * 1024) {
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Immagine troppo grande (max 2MB)', 'error');
        }
        return;
    }

    const dataUrl = await new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = () => reject(new Error('Lettura immagine non riuscita'));
        reader.readAsDataURL(file);
    });

    if (typeof dataUrl !== 'string') return;
    setStoredProfilePicture(profiloData.id, dataUrl);
    applyProfilePictureToUI();
    if (typeof updateNavbar === 'function') {
        updateNavbar();
    }

    if (typeof Utils !== 'undefined') {
        Utils.showNotification('Foto profilo aggiornata', 'success');
    }
});

document.getElementById('open-security-btn').addEventListener('click', () => {
    document.getElementById('security-modal').classList.remove('hidden');
});

document.getElementById('close-security-modal').addEventListener('click', closeSecurityModal);
document.getElementById('cancel-security').addEventListener('click', closeSecurityModal);

function closeSecurityModal() {
    document.getElementById('security-modal').classList.add('hidden');
    document.getElementById('security-error').classList.add('hidden');
    document.getElementById('security-form').reset();
}

document.getElementById('security-form').addEventListener('submit', async (e) => {
    e.preventDefault();

    const currentPassword = document.getElementById('current-password').value;
    const newPassword = document.getElementById('new-password').value;
    const confirmPassword = document.getElementById('confirm-password').value;

    const errorDiv = document.getElementById('security-error');
    const saveBtn = document.getElementById('security-save-btn');
    const saveText = document.getElementById('security-save-text');
    const saveLoading = document.getElementById('security-save-loading');

    errorDiv.classList.add('hidden');

    if (newPassword.length < 8) {
        errorDiv.textContent = 'La nuova password deve contenere almeno 8 caratteri';
        errorDiv.classList.remove('hidden');
        return;
    }

    if (newPassword !== confirmPassword) {
        errorDiv.textContent = 'La conferma password non coincide';
        errorDiv.classList.remove('hidden');
        return;
    }

    if (newPassword === currentPassword) {
        errorDiv.textContent = 'La nuova password deve essere diversa da quella attuale';
        errorDiv.classList.remove('hidden');
        return;
    }

    saveBtn.disabled = true;
    saveText.textContent = 'Aggiornamento...';
    saveLoading.classList.remove('hidden');

    try {
        await ApiClient.put('/user/change-password', { currentPassword, newPassword });
        closeSecurityModal();
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Password aggiornata con successo', 'success');
        }
    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.classList.remove('hidden');
    } finally {
        saveBtn.disabled = false;
        saveText.textContent = 'Aggiorna';
        saveLoading.classList.add('hidden');
    }
});

document.getElementById('edit-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const email = document.getElementById('edit-email').value;
    const nome = document.getElementById('edit-nome').value;
    const cognome = document.getElementById('edit-cognome').value;
    const telefono = document.getElementById('edit-telefono').value;
    
    const errorDiv = document.getElementById('edit-error');
    const saveBtn = document.getElementById('save-btn');
    const saveText = document.getElementById('save-text');
    const saveLoading = document.getElementById('save-loading');
    
    errorDiv.classList.add('hidden');
    
    // Validazione
    if (!email.includes('@')) {
        errorDiv.textContent = 'Email non valida';
        errorDiv.classList.remove('hidden');
        return;
    }
    
    saveBtn.disabled = true;
    saveText.textContent = 'Salvataggio...';
    saveLoading.classList.remove('hidden');
    
    try {
        await ApiClient.put('/user/profile', { email, nome, cognome, telefono });
        
        // Chiudi modal e ricarica
        document.getElementById('edit-modal').classList.add('hidden');
        
        // Mostra notifica di successo
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Profilo aggiornato con successo', 'success');
        }
        
        // Ricarica profilo
        await loadProfile();
    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.classList.remove('hidden');
    } finally {
        saveBtn.disabled = false;
        saveText.textContent = 'Salva';
        saveLoading.classList.add('hidden');
    }
});

// Prenotazione
function apriPrenotazione(id, filmTitolo, cinemaNome, data) {
    proiezioneDaPrenotare = id;
    document.getElementById('prenota-film').textContent = filmTitolo;
    document.getElementById('prenota-cinema').textContent = cinemaNome;
    document.getElementById('prenota-data').textContent = data;
    document.getElementById('prenota-modal').classList.remove('hidden');
}

document.getElementById('close-prenota-modal').addEventListener('click', () => {
    document.getElementById('prenota-modal').classList.add('hidden');
    document.getElementById('prenota-error').classList.add('hidden');
    proiezioneDaPrenotare = null;
});

document.getElementById('cancel-prenota').addEventListener('click', () => {
    document.getElementById('prenota-modal').classList.add('hidden');
    document.getElementById('prenota-error').classList.add('hidden');
    proiezioneDaPrenotare = null;
});

document.getElementById('prenota-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    if (!proiezioneDaPrenotare) return;
    
    const numeroPosti = parseInt(document.getElementById('prenota-posti').value);
    const errorDiv = document.getElementById('prenota-error');
    const prenotaBtn = document.getElementById('prenota-btn');
    const prenotaText = document.getElementById('prenota-text');
    const prenotaLoading = document.getElementById('prenota-loading');
    
    errorDiv.classList.add('hidden');
    
    if (numeroPosti < 1 || numeroPosti > 10) {
        errorDiv.textContent = 'Il numero di posti deve essere tra 1 e 10';
        errorDiv.classList.remove('hidden');
        return;
    }
    
    prenotaBtn.disabled = true;
    prenotaText.textContent = 'Prenotazione...';
    prenotaLoading.classList.remove('hidden');
    
    try {
        await ApiClient.post('/user/prenota', { 
            proiezioneSalvataId: proiezioneDaPrenotare, 
            numeroPosti 
        });
        
        document.getElementById('prenota-modal').classList.add('hidden');
        
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Prenotazione effettuata con successo!', 'success');
        }
        
        // Ricarica profilo
        await loadProfile();
    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.classList.remove('hidden');
    } finally {
        prenotaBtn.disabled = false;
        prenotaText.textContent = 'Prenota';
        prenotaLoading.classList.add('hidden');
    }
});

function vaiABiglietti(showId) {
    const url = showId ? `/user-biglietti.html?showId=${showId}` : '/user-biglietti.html';
    window.location.href = url;
}

async function vaiAPrenotazione(proiezioneSalvataId, showId, proiezioneId) {
    if (showId) {
        try {
            const show = await ApiClient.get(`/shows/${showId}`);
            const params = new URLSearchParams({
                IdShow: String(showId),
                IdFilm: String(show.filmId || 0),
                IdSala: String(show.salaId || 0),
                IdCinema: String(show.cinemaId || 0)
            });
            window.location.href = `/acquista.html?${params.toString()}`;
            return;
        } catch {
        }
    }

    const target = (profiloData?.proiezioniSalvate || []).find(p => p.id === proiezioneSalvataId || p.proiezioneId === proiezioneId);
    if (!target) return;

    const dataProiezione = new Date(target.dataProiezione);
    const dataFormattata = dataProiezione.toLocaleDateString('it-IT', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
        year: 'numeric'
    });

    apriPrenotazione(target.id, target.filmTitolo, target.cinemaNome, dataFormattata);
}

async function rimuoviProiezione(id) {
    if (!confirm('Sei sicuro di voler rimuovere questa proiezione dai salvati?')) return;
    
    try {
        await ApiClient.delete(`/user/proiezioni-salvate/${id}`);
        
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Proiezione rimossa', 'info');
        }
        
        await loadProfile();
    } catch (error) {
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Errore: ' + error.message, 'error');
        }
    }
}

async function annullaPrenotazione(id) {
    if (!confirm('Sei sicuro di voler annullare questa prenotazione?')) return;
    
    try {
        await ApiClient.delete(`/user/prenota/${id}`);
        
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Prenotazione annullata', 'info');
        }
        
        await loadProfile();
    } catch (error) {
        if (typeof Utils !== 'undefined') {
            Utils.showNotification('Errore: ' + error.message, 'error');
        }
    }
}

// Inizializza
document.addEventListener('DOMContentLoaded', async () => {
    const ok = await Auth.ensureInitialized();
    if (!ok || !Auth.isAuthenticated()) {
        window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname);
        return;
    }

    if (typeof loadAllComponents === 'function') {
        await loadAllComponents();
    }

    await loadProfile();

    document.getElementById('newsletter-consent')?.addEventListener('change', async (event) => {
        const checked = !!event.target.checked;
        try {
            await ApiClient.put('/newsletter/preference', { consenso: checked });
            if (typeof Utils !== 'undefined') {
                Utils.showNotification(checked ? 'Newsletter attivata' : 'Newsletter disattivata', 'success');
            }
        } catch (error) {
            event.target.checked = !checked;
            if (typeof Utils !== 'undefined') {
                Utils.showNotification(error.message || 'Errore aggiornamento preferenza newsletter', 'error');
            }
        }
    });

    const params = new URLSearchParams(window.location.search);
    if (params.get('edit') === '1') {
        const openEdit = () => {
            const modal = document.getElementById('edit-modal');
            if (modal) {
                modal.classList.remove('hidden');
                params.delete('edit');
                const qs = params.toString();
                const nextUrl = qs ? `${window.location.pathname}?${qs}` : window.location.pathname;
                window.history.replaceState({}, '', nextUrl);
            }
        };
        setTimeout(openEdit, 250);
    }
});
