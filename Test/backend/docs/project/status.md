# Stato Avanzamento - Iterazione 4

## Stato generale

| Area | Stato | Note |
|---|---|---|
| Modello dati multi-sala | Completata | Entita `Sala`, `Show`, `Acquisto`, `Biglietto`, `CreditoUtente`, `TransazioneCredito`, `PrenotazioneTemporanea` con indici e relazioni |
| Programmazione pubblica | Completata | `programmazione.html`, `scheda-film.html`, `my-cinemas.html` con selezione cinema e filtri |
| Acquisto e lock posti | Completata | Lock race-safe con scadenza, rinnovo e rilascio; conferma acquisto e biglietti |
| Pagamento misto | Completata | Mix credito + Stripe con `PaymentIntent` reale e verifica server-side |
| Validazione ticket | Completata | Flusso manuale e QR per PowerUser/Admin con controllo cinema |
| Credito piattaforma | Completata | Ricarica da backoffice e storico transazioni |
| Email e PDF | Completata | Invio conferma via SMTP con allegato PDF ticket |
| Seed dati demo | Completata | Dataset coerente per cinema/sale/film/show/acquisti/biglietti/crediti |

## Checklist sintetica Iterazione 4

- [x] Gestione cinema multi-sala con tipologie (ISENSE, XL, 3D, 2D)
- [x] Nuova programmazione per film (card uniche per titolo)
- [x] Tag programmazione: in evidenza, in uscita, tutti
- [x] Selezione cinema con persistenza localStorage e profilo utente
- [x] Scheda film con date orizzontali e show raggruppati per tipologia sala
- [x] Nuova gestione admin sale e show
- [x] Validazione overlap show per sala/data/orario
- [x] Pagina acquisto con piantina posti e max 10 ticket
- [x] Lock temporaneo anti race condition con cleanup background
- [x] Pagamento con credito, carta o mix credito+carta
- [x] Ricarica credito operatore con tracciamento
- [x] Validazione ticket con marcatura data/ora
- [x] Endpoint PDF ticket e invio email conferma

## TODO non bloccanti

- [ ] Rifinitura UI/UX ulteriore per allineamento visuale completo a benchmark UCI
- [ ] Webhook Stripe per riconciliazione asincrona avanzata (attuale: verifica sincrona PaymentIntent)
