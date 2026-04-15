// js/pages/profilo.js
// Gestione area personale utente

let profiloData = null;
let proiezioneDaPrenotare = null;

// Verifica autenticazione
if (!Auth.isAuthenticated()) {
    window.location.href = '/login.html?redirect=' + encodeURIComponent(window.location.pathname);
}

// Carica il profilo
async function loadProfile() {
    try {
        showLoading(true);
        
        profiloData = await ApiClient.get('/user/profile');
        
        showLoading(false);
        renderProfile();
        renderProiezioni();
        handlePrenotaQueryParam();
    } catch (error) {
        showLoading(false);
        showError(error.message);
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
    
    // Popola form modifica
    document.getElementById('edit-email').value = user.email;
    document.getElementById('edit-nome').value = user.nome || '';
    document.getElementById('edit-cognome').value = user.cognome || '';
    document.getElementById('edit-telefono').value = user.telefono || '';
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
    if (typeof loadAllComponents === 'function') {
        await loadAllComponents();
    }

    await loadProfile();

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
