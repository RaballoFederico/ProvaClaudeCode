# Scheda tecnica generale - Spostamento di un progetto locale su Azure

## Introduzione
Quando un'applicazione funziona in locale, significa che viene eseguita direttamente sul computer dello sviluppatore. In questa fase e normale usare percorsi locali, file di configurazione nella cartella del progetto, database avviati manualmente o tramite Docker e indirizzi come `localhost`.

Il passaggio ad Azure serve a rendere l'applicazione disponibile online. Per farlo in modo ordinato conviene containerizzare il progetto con Docker, pubblicare le immagini su Azure Container Registry e poi avviarle tramite Azure Container Apps.

In questo modo il codice non viene semplicemente copiato su un server, ma viene impacchettato in un'immagine Docker. L'immagine contiene tutto cio che serve per eseguire l'applicazione, mentre Azure si occupa di avviare il container e renderlo raggiungibile tramite HTTPS.

## Architettura generale
Una soluzione tipica e composta da questi elementi:

- Un Resource Group, che raccoglie tutte le risorse Azure del progetto.
- Un Azure Container Registry, usato per salvare le immagini Docker.
- Un Container Apps Environment, cioe l'ambiente dove vengono eseguite le app containerizzate.
- Una o piu Container App, per esempio una per il backend e una per il frontend.
- Eventuali servizi esterni, come database, sistemi di pagamento, email o autenticazione.

Il Resource Group serve per organizzare il progetto. Azure Container Registry serve per archiviare le immagini Docker. Azure Container Apps serve invece per eseguire quelle immagini come applicazioni online.

## Preparazione del progetto
Prima del deploy bisogna fare in modo che l'applicazione non dipenda troppo dall'ambiente locale.

Per esempio, se nel codice e scritto direttamente `localhost`, l'applicazione funzionera sul computer dello sviluppatore ma non su Azure. Lo stesso vale per password, stringhe di connessione e URL di servizi esterni.

La soluzione corretta e usare variabili d'ambiente. Una variabile d'ambiente e un valore configurato fuori dal codice, letto dall'applicazione quando parte. Per esempio:

```text
DB_HOST
DB_PORT
DB_NAME
DB_USER
DB_PASSWORD
JWT_SECRET_KEY
```

Questo permette di usare lo stesso codice in ambienti diversi. In locale posso usare certi valori, mentre su Azure posso impostarne altri senza modificare il sorgente.

## Creazione del Dockerfile
Per pubblicare l'applicazione su Azure Container Apps serve prima creare un'immagine Docker. L'immagine viene descritta tramite un file chiamato `Dockerfile`.

Nel caso di un'applicazione .NET, un Dockerfile puo essere costruito in due fasi: una fase di build e una fase di runtime.

Esempio generale:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NomeProgetto.csproj ./
RUN dotnet restore NomeProgetto.csproj

COPY . ./
RUN dotnet publish NomeProgetto.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NomeProgetto.dll"]
```

La prima immagine, `sdk`, contiene gli strumenti per compilare. La seconda, `aspnet`, contiene solo cio che serve per eseguire. Questa separazione rende l'immagine finale piu pulita e piu adatta alla pubblicazione.

La riga:

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
```

indica all'applicazione di ascoltare sulla porta `8080`. Questa porta dovra poi essere indicata anche ad Azure con `--target-port 8080`.

## Build dell'immagine Docker
Dopo aver scritto il Dockerfile si costruisce l'immagine in locale.

La forma generale del comando e:

```powershell
docker build -f <percorso-dockerfile> -t <nome-immagine>:<tag> <contesto-build>
```

Ogni parte ha uno scopo preciso.

`docker build` avvia la costruzione dell'immagine.

`-f <percorso-dockerfile>` indica quale Dockerfile usare.

`-t <nome-immagine>:<tag>` assegna un nome e una versione all'immagine.

`<contesto-build>` indica la cartella da cui Docker puo leggere i file.

Esempio:

```powershell
docker build -f ".\backend\Dockerfile" -t "mia-api:v1.0.0" ".\backend"
```

In questo esempio `mia-api` e il nome dell'immagine e `v1.0.0` e il tag. Il tag serve per distinguere una versione dall'altra.

## Creazione dell'Azure Container Registry
Azure Container Registry e il servizio in cui vengono caricate le immagini Docker.

Prima si crea un Resource Group:

```powershell
az group create --name <nome-resource-group> --location <area-azure>
```

Esempio:

```powershell
az group create --name rg-mio-progetto --location francecentral
```

Il comando si compone cosi:

`az group create` crea un nuovo gruppo di risorse.

`--name` indica il nome del gruppo.

`--location` indica l'area geografica Azure.

Poi si crea il registry:

```powershell
az acr create --resource-group <nome-resource-group> --name <nome-acr> --sku Basic --admin-enabled true
```

Esempio:

```powershell
az acr create --resource-group rg-mio-progetto --name acrmioprogetto123 --sku Basic --admin-enabled true
```

`--sku Basic` indica il piano base, adatto a progetti piccoli o scolastici.

`--admin-enabled true` abilita le credenziali amministrative del registry, utili per permettere alla Container App di scaricare immagini private.

## Pubblicazione dell'immagine su ACR
Una volta creato il registry, bisogna fare login:

```powershell
az acr login --name <nome-acr>
```

Esempio:

```powershell
az acr login --name acrmioprogetto123
```

Poi bisogna recuperare il login server:

```powershell
az acr show --name <nome-acr> --query loginServer -o tsv
```

Esempio di risultato:

```text
acrmioprogetto123.azurecr.io
```

A questo punto l'immagine locale deve essere taggata con il nome remoto.

La forma generale e:

```powershell
docker tag <immagine-locale>:<tag> <acr-login-server>/<immagine-remota>:<tag>
```

Esempio:

```powershell
docker tag mia-api:v1.0.0 acrmioprogetto123.azurecr.io/mia-api:v1.0.0
```

Infine si fa il push:

```powershell
docker push acrmioprogetto123.azurecr.io/mia-api:v1.0.0
```

Da questo momento l'immagine non e piu solo locale, ma si trova anche su Azure Container Registry.

## Creazione dell'ambiente Container Apps
Prima di creare una Container App serve un ambiente Container Apps.

Comando generale:

```powershell
az containerapp env create --name <nome-ambiente> --resource-group <nome-resource-group> --location <area-azure>
```

Esempio:

```powershell
az containerapp env create --name acaenv-mio-progetto --resource-group rg-mio-progetto --location francecentral
```

L'ambiente rappresenta il contesto in cui gireranno le Container App. Piu applicazioni dello stesso progetto possono stare nello stesso ambiente.

## Creazione della Container App
Ora si puo creare la Container App usando l'immagine pubblicata su ACR.

Forma generale:

```powershell
az containerapp create `
  --resource-group <nome-resource-group> `
  --name <nome-container-app> `
  --environment <nome-ambiente> `
  --image <acr-login-server>/<nome-immagine>:<tag> `
  --target-port <porta-container> `
  --ingress external `
  --registry-server <acr-login-server> `
  --registry-username <utente-acr> `
  --registry-password <password-acr>
```

Esempio:

```powershell
az containerapp create `
  --resource-group rg-mio-progetto `
  --name mia-api `
  --environment acaenv-mio-progetto `
  --image acrmioprogetto123.azurecr.io/mia-api:v1.0.0 `
  --target-port 8080 `
  --ingress external `
  --registry-server acrmioprogetto123.azurecr.io `
  --registry-username <utente-acr> `
  --registry-password <password-acr>
```

`--target-port 8080` deve corrispondere alla porta su cui ascolta il container.

`--ingress external` rende l'app raggiungibile da internet.

I parametri `--registry-server`, `--registry-username` e `--registry-password` servono per permettere ad Azure di scaricare l'immagine dal registry privato.

## Configurazione delle variabili d'ambiente
Dopo la creazione della Container App, si configurano le variabili d'ambiente.

Forma generale:

```powershell
az containerapp update `
  --resource-group <nome-resource-group> `
  --name <nome-container-app> `
  --set-env-vars CHIAVE1=VALORE1 CHIAVE2=VALORE2
```

Esempio:

```powershell
az containerapp update `
  --resource-group rg-mio-progetto `
  --name mia-api `
  --set-env-vars DB_HOST=mio-db DB_PORT=3306 DB_NAME=app_db DB_USER=admin JWT_ISSUER=MiaAPI JWT_AUDIENCE=MioFrontend
```

Le variabili sensibili, come password del database o chiavi private, non dovrebbero essere scritte in chiaro negli script. Conviene gestirle come secret o configurazioni protette.

## Esempi pratici di variabili d'ambiente
Le variabili d'ambiente servono per dare all'applicazione i valori necessari per funzionare senza scriverli direttamente nel codice. Ogni variabile ha un nome e un valore, nel formato:

```text
NOME_VARIABILE=valore
```

Per esempio:

```text
DB_PORT=3306
```

Significa che la variabile si chiama `DB_PORT` e contiene il valore `3306`.

### Variabili per il database
`DB_HOST` indica l'indirizzo del server database. In locale potrebbe essere `localhost`, mentre su Azure potrebbe essere il nome di un servizio database o un hostname interno.

Esempi:

```text
DB_HOST=localhost
DB_HOST=mio-database.mysql.database.azure.com
DB_HOST=db-interno
```

`DB_PORT` indica la porta su cui ascolta il database. Per MySQL di solito si usa `3306`, mentre per PostgreSQL di solito si usa `5432`.

Esempi:

```text
DB_PORT=3306
DB_PORT=5432
```

`DB_NAME` indica il nome del database usato dall'applicazione. Non e il nome del server, ma il nome dello schema/database dentro al server.

Esempi:

```text
DB_NAME=app_db
DB_NAME=cinema_db
DB_NAME=gestionale_produzione
```

`DB_USER` indica l'utente con cui l'applicazione accede al database.

Esempi:

```text
DB_USER=admin
DB_USER=app_user
DB_USER=utente_api
```

`DB_PASSWORD` contiene la password dell'utente database. Questa e una variabile sensibile, quindi non dovrebbe essere salvata in chiaro dentro Git o dentro file pubblici.

Esempi:

```text
DB_PASSWORD=PasswordMoltoSicura123!
DB_PASSWORD=<valore-salvato-come-secret>
```

Una configurazione database tramite Azure CLI potrebbe essere:

```powershell
az containerapp update `
  --resource-group rg-mio-progetto `
  --name mia-api `
  --set-env-vars DB_HOST=mio-database.mysql.database.azure.com DB_PORT=3306 DB_NAME=app_db DB_USER=app_user
```

La password e meglio impostarla come secret o tramite una configurazione protetta del portale Azure.

### Variabili per JWT e autenticazione
`JWT_SECRET_KEY` e la chiave usata dal backend per firmare i token JWT. Deve essere lunga e difficile da indovinare. Una chiave troppo corta o banale rende il sistema insicuro.

Esempi:

```text
JWT_SECRET_KEY=QuestaEUnaChiaveMoltoLungaDiAlmeno32Caratteri
JWT_SECRET_KEY=<chiave-generata-e-salvata-come-secret>
```

`JWT_ISSUER` indica chi emette il token. Di solito si mette il nome del backend o della API.

Esempi:

```text
JWT_ISSUER=MiaAPI
JWT_ISSUER=GestionaleBackend
JWT_ISSUER=AuthServer
```

`JWT_AUDIENCE` indica per chi e valido il token. Di solito si mette il nome del frontend o del client che usera la API.

Esempi:

```text
JWT_AUDIENCE=MioFrontend
JWT_AUDIENCE=WebAppClient
JWT_AUDIENCE=MobileApp
```

`JWT_ACCESS_TOKEN_EXPIRY_MINUTES` indica per quanti minuti e valido l'access token.

Esempi:

```text
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15
JWT_ACCESS_TOKEN_EXPIRY_MINUTES=30
```

`JWT_REFRESH_TOKEN_EXPIRY_DAYS` indica per quanti giorni e valido il refresh token.

Esempi:

```text
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7
JWT_REFRESH_TOKEN_EXPIRY_DAYS=30
```

Esempio di configurazione JWT:

```powershell
az containerapp update `
  --resource-group rg-mio-progetto `
  --name mia-api `
  --set-env-vars JWT_ISSUER=MiaAPI JWT_AUDIENCE=MioFrontend JWT_ACCESS_TOKEN_EXPIRY_MINUTES=15 JWT_REFRESH_TOKEN_EXPIRY_DAYS=7
```

Anche `JWT_SECRET_KEY` e meglio trattarla come valore sensibile.

### Variabili per CORS
`CORS_ALLOWED_ORIGINS` indica quali frontend possono chiamare la API dal browser. Questo serve per evitare che qualsiasi sito esterno possa fare richieste alla API usando il browser dell'utente.

Il valore di solito e un URL completo, oppure piu URL separati da virgola.

Esempi:

```text
CORS_ALLOWED_ORIGINS=https://mio-frontend.azurecontainerapps.io
CORS_ALLOWED_ORIGINS=https://www.miosito.it,https://app.miosito.it
```

Se il frontend gira in locale durante lo sviluppo, si puo usare:

```text
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173
```

In produzione, invece, e meglio usare solo URL HTTPS reali.

Esempio comando:

```powershell
az containerapp update `
  --resource-group rg-mio-progetto `
  --name mia-api `
  --set-env-vars CORS_ALLOWED_ORIGINS=https://mio-frontend.azurecontainerapps.io
```

### Variabili per URL pubblici e redirect
`APP_PUBLIC_URL` puo essere usata per indicare l'URL pubblico dell'applicazione.

Esempi:

```text
APP_PUBLIC_URL=https://mio-frontend.azurecontainerapps.io
APP_PUBLIC_URL=https://www.miosito.it
```

`API_BASE_URL` puo essere usata dal frontend per sapere dove si trova il backend.

Esempi:

```text
API_BASE_URL=https://mia-api.azurecontainerapps.io
API_BASE_URL=https://api.miosito.it
```

`EXTERNAL_AUTH_BACKEND_BASE_URL` viene usata nei sistemi di autenticazione esterna, per esempio login con Google, GitHub o Microsoft. Deve contenere l'URL pubblico del backend, perche i provider esterni devono sapere dove rimandare l'utente dopo il login.

Esempi:

```text
EXTERNAL_AUTH_BACKEND_BASE_URL=https://mia-api.azurecontainerapps.io
EXTERNAL_AUTH_BACKEND_BASE_URL=https://api.miosito.it
```

### Variabili per email SMTP
Se l'applicazione invia email, per esempio per recupero password o notifiche, servono le variabili SMTP.

`SMTP_HOST` indica il server SMTP.

Esempi:

```text
SMTP_HOST=smtp.gmail.com
SMTP_HOST=smtp.office365.com
```

`SMTP_PORT` indica la porta. Spesso si usa `587` per invio con TLS.

Esempi:

```text
SMTP_PORT=587
SMTP_PORT=465
```

`SMTP_USER` indica l'utente/email usata per autenticarsi.

Esempio:

```text
SMTP_USER=noreply@miosito.it
```

`SMTP_PASSWORD` contiene la password o app password dell'account email. Anche questa e sensibile.

Esempio:

```text
SMTP_PASSWORD=<password-salvata-come-secret>
```

`SMTP_FROM` indica il mittente mostrato nelle email inviate dall'app.

Esempi:

```text
SMTP_FROM=noreply@miosito.it
SMTP_FROM=assistenza@miosito.it
```

### Variabili per Stripe o pagamenti
Se il progetto usa Stripe, normalmente servono queste variabili.

`STRIPE_SECRET_KEY` e la chiave privata usata dal backend. Deve restare segreta.

Esempio:

```text
STRIPE_SECRET_KEY=sk_test_xxxxxxxxxxxxxxxxx
```

`STRIPE_PUBLISHABLE_KEY` e la chiave pubblica usata dal frontend.

Esempio:

```text
STRIPE_PUBLISHABLE_KEY=pk_test_xxxxxxxxxxxxxxxxx
```

`STRIPE_WEBHOOK_SECRET` serve per verificare che gli eventi ricevuti dal webhook arrivino davvero da Stripe.

Esempio:

```text
STRIPE_WEBHOOK_SECRET=whsec_xxxxxxxxxxxxxxxxx
```

In produzione bisogna usare chiavi live, mentre in sviluppo si usano chiavi test.

### Variabili per servizi esterni
Molte applicazioni usano API esterne, per esempio mappe, immagini, notifiche o sistemi di login.

Esempi generici:

```text
GOOGLE_MAPS_API_KEY=AIza...
TMDB_API_KEY=xxxxxxxxxxxxxxxx
GITHUB_CLIENT_ID=xxxxxxxx
GITHUB_CLIENT_SECRET=<secret>
```

La regola generale e semplice: se il valore identifica un servizio, una password o una chiave privata, non va scritto nel codice. Va configurato come variabile d'ambiente o secret.

### Esempio completo realistico
Un backend pubblicato su Azure potrebbe avere una configurazione simile:

```text
DB_HOST=mio-database.mysql.database.azure.com
DB_PORT=3306
DB_NAME=app_db
DB_USER=app_user
DB_PASSWORD=<secret>
JWT_SECRET_KEY=<secret>
JWT_ISSUER=MiaAPI
JWT_AUDIENCE=MioFrontend
CORS_ALLOWED_ORIGINS=https://mio-frontend.azurecontainerapps.io
API_BASE_URL=https://mia-api.azurecontainerapps.io
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=noreply@miosito.it
SMTP_PASSWORD=<secret>
SMTP_FROM=noreply@miosito.it
```

Questa configurazione permette all'applicazione di sapere dove si trova il database, come generare i token, quali frontend autorizzare e come inviare email.

## Aggiornamento di una nuova versione
Quando si modifica il codice, bisogna creare una nuova immagine, pubblicarla su ACR e aggiornare la Container App.

Il flusso e:

1. Build nuova immagine.
2. Tag con versione nuova.
3. Push su ACR.
4. Update della Container App.

Esempio:

```powershell
docker build -f ".\backend\Dockerfile" -t "mia-api:v1.0.1" ".\backend"
docker tag mia-api:v1.0.1 acrmioprogetto123.azurecr.io/mia-api:v1.0.1
docker push acrmioprogetto123.azurecr.io/mia-api:v1.0.1
az containerapp update --resource-group rg-mio-progetto --name mia-api --image acrmioprogetto123.azurecr.io/mia-api:v1.0.1
```

In questo modo Azure vede chiaramente che la versione e cambiata.

## Problema comune: uso del tag `latest`
Un errore frequente durante il deploy e usare sempre il tag `latest`.

Per esempio:

```text
acrmioprogetto123.azurecr.io/mia-api:latest
```

Il problema e che `latest` non forza automaticamente Azure a scaricare sempre l'ultima immagine. E solo un'etichetta. Se la Container App era gia configurata con:

```text
acrmioprogetto123.azurecr.io/mia-api:latest
```

e dopo una modifica viene caricata un'altra immagine con lo stesso nome e lo stesso tag, la configurazione della Container App resta identica. Azure puo quindi continuare a usare la versione precedente o non aggiornarsi come previsto.

Per questo motivo puo sembrare che il deploy non sincronizzi le modifiche fatte. In realta il problema e che non c'e un cambio visibile nel nome dell'immagine.

La soluzione migliore e usare tag versionati:

```powershell
docker build -f ".\backend\Dockerfile" -t "mia-api:v1.0.1" ".\backend"
docker tag mia-api:v1.0.1 acrmioprogetto123.azurecr.io/mia-api:v1.0.1
docker push acrmioprogetto123.azurecr.io/mia-api:v1.0.1
az containerapp update --resource-group rg-mio-progetto --name mia-api --image acrmioprogetto123.azurecr.io/mia-api:v1.0.1
```

Alla modifica successiva si usera:

```text
v1.0.2
```

oppure un tag basato sulla data, ad esempio:

```text
2026-05-21-01
```

Questo rende il deploy piu affidabile e permette anche il rollback. Se la versione `v1.0.2` ha un problema, si puo tornare alla versione precedente:

```powershell
az containerapp update --resource-group rg-mio-progetto --name mia-api --image acrmioprogetto123.azurecr.io/mia-api:v1.0.1
```

## Recupero dell'URL pubblico
Dopo la creazione o l'aggiornamento della Container App si puo leggere l'URL pubblico.

Comando generale:

```powershell
az containerapp show --resource-group <nome-resource-group> --name <nome-container-app> --query properties.configuration.ingress.fqdn -o tsv
```

Esempio:

```powershell
az containerapp show --resource-group rg-mio-progetto --name mia-api --query properties.configuration.ingress.fqdn -o tsv
```

Il risultato sara un dominio generato da Azure. Aggiungendo `https://` davanti si ottiene l'URL pubblico dell'applicazione.

## Controlli dopo il deploy
Dopo il deploy bisogna verificare che l'applicazione risponda correttamente.

Se il backend espone un endpoint di controllo come `/health`, si puo usare:

```powershell
curl https://<url-pubblico>/health
```

Se la risposta indica che il servizio e attivo, il deploy e andato a buon fine. Se invece ci sono errori, le cause piu comuni sono:

- porta sbagliata tra Dockerfile e `--target-port`;
- variabili d'ambiente mancanti;
- immagine non aggiornata;
- credenziali ACR errate;
- database non raggiungibile.

## Conclusione
Spostare un progetto da locale ad Azure significa prepararlo per funzionare in un ambiente esterno e ripetibile. Il passaggio principale e la containerizzazione con Docker, seguita dalla pubblicazione su Azure Container Registry e dall'esecuzione tramite Azure Container Apps.

La parte piu importante e separare codice e configurazione: il codice resta uguale, mentre valori come database, password, URL e chiavi vengono gestiti tramite variabili d'ambiente.

Per evitare problemi di sincronizzazione durante il deploy e consigliabile usare tag versionati invece di affidarsi sempre a `latest`. Questo rende piu chiaro quale versione e online e permette di tornare facilmente a una versione precedente in caso di errore.
