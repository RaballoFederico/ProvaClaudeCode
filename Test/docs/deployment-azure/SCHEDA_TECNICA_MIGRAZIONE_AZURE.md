# Scheda tecnica - Spostamento del progetto FilmHub da locale ad Azure

## Introduzione
Il progetto FilmHub inizialmente funzionava in locale, cioe direttamente sul computer di sviluppo. In questa configurazione il backend, il frontend e il database venivano eseguiti in un ambiente controllato dal programmatore, con percorsi locali, porte locali e file di configurazione presenti nella cartella del progetto.

L'obiettivo dello spostamento su Azure e stato rendere l'applicazione disponibile online, quindi raggiungibile tramite URL pubblici e non piu soltanto dalla macchina locale. Per farlo non e stato copiato semplicemente il codice su un server, ma e stata scelta una soluzione basata su container Docker e Azure Container Apps. In questo modo backend e frontend vengono impacchettati in immagini Docker, pubblicati su Azure Container Registry e poi eseguiti come servizi cloud.

Questa scelta rende il deploy piu ordinato: l'ambiente locale serve per costruire e testare, mentre Azure esegue la stessa applicazione dentro container standardizzati.

## Architettura finale
L'architettura finale e composta da piu elementi Azure che collaborano tra loro.

Il primo elemento e il Resource Group `rgfilmhubenvfrancecentral237`. Un Resource Group e un contenitore logico di Azure: serve a raggruppare tutte le risorse dello stesso progetto, cosi e piu semplice gestirle, monitorarle ed eventualmente eliminarle.

Il secondo elemento e Azure Container Registry, chiamato `acrfilmhub5497`. Questo servizio funziona come un archivio privato di immagini Docker. Dopo aver creato l'immagine del backend o del frontend in locale, l'immagine viene caricata su questo registry. Azure Container Apps poi scarica l'immagine da li per eseguirla.

Il terzo elemento e l'ambiente Azure Container Apps `acaenv-filmhub`. Questo ambiente rappresenta il contesto in cui vengono eseguite le Container App. Dentro questo ambiente sono state create due applicazioni:

- `filmhub-api`, che contiene il backend .NET.
- `filmhub-frontend`, che contiene il frontend .NET che serve i file statici.

Entrambe le applicazioni ascoltano sulla porta `8080`, perche nei Dockerfile e impostata la variabile `ASPNETCORE_URLS=http://+:8080` e negli script Azure viene usato `--target-port 8080`.

## Preparazione del progetto per il cloud
Prima di fare il deploy su Azure e stato necessario preparare il progetto in modo che potesse funzionare anche fuori dall'ambiente locale.

In locale e normale usare file come `.env`, impostazioni di sviluppo o URL tipo `localhost`. Su Azure, invece, questi valori devono essere configurabili dall'esterno. Per questo nel backend molte impostazioni vengono lette da variabili d'ambiente, come `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` e `JWT_SECRET_KEY`.

Questo significa che il codice non deve essere modificato ogni volta che cambia l'ambiente. La stessa immagine Docker puo essere usata in locale, staging o produzione, cambiando solo le variabili assegnate al container.

## Containerizzazione del backend
Il backend usa il file [backend/Dockerfile](D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Dockerfile). Questo Dockerfile usa una build multi-stage.

Nel primo stage viene usata l'immagine `mcr.microsoft.com/dotnet/sdk:10.0`. Questa immagine contiene tutti gli strumenti necessari per compilare il progetto .NET. Qui vengono eseguiti `dotnet restore` e `dotnet publish`.

Nel secondo stage viene usata l'immagine `mcr.microsoft.com/dotnet/aspnet:10.0`. Questa immagine e piu leggera, perche contiene solo il runtime necessario per eseguire l'applicazione gia compilata. Il risultato pubblicato viene copiato nella cartella `/app` e poi avviato con:

```dockerfile
ENTRYPOINT ["dotnet", "FilmAPI.dll"]
```

La riga:

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
```

indica ad ASP.NET di ascoltare su tutte le interfacce di rete del container, usando la porta `8080`. Questa impostazione e fondamentale su Azure Container Apps, perche Azure deve sapere su quale porta inoltrare il traffico HTTP.

## Containerizzazione del frontend
Il frontend usa il file [Dockerfile.frontend](D:\Scuola\5IA\INFO\ClaudeCode\Test\Dockerfile.frontend). Anche qui viene usata una build multi-stage.

Il comando `dotnet publish` crea una versione pronta per la pubblicazione del progetto `FilmFrontend.csproj`. Nel container finale viene poi eseguito:

```dockerfile
ENTRYPOINT ["dotnet","FilmFrontend.dll"]
```

Il frontend non e solo una cartella di file HTML aperta dal browser, ma viene servito da un'applicazione ASP.NET. Questo permette anche di gestire alcune rotte, come il redirect verso `/home.html` e il proxy/redirect per alcune risorse media.

## Deploy automatizzato con PowerShell
Per rendere il deploy ripetibile sono stati creati due script:

- [deploy-api.ps1](D:\Scuola\5IA\INFO\ClaudeCode\Test\scripts\deployment\deploy-api.ps1)
- [deploy-frontend.ps1](D:\Scuola\5IA\INFO\ClaudeCode\Test\scripts\deployment\deploy-frontend.ps1)

Gli script seguono la stessa logica: recuperano le informazioni del registry, costruiscono l'immagine Docker, la pubblicano su Azure Container Registry e poi creano o aggiornano la Container App.

Questa struttura e utile perche evita di dover riscrivere ogni volta tutti i comandi a mano. Inoltre gli script sono idempotenti: se la Container App esiste gia, viene aggiornata; se non esiste, viene creata.

## Parametri principali degli script
All'inizio degli script ci sono parametri come:

```powershell
[string]$ResourceGroup = "rgfilmhubenvfrancecentral237"
[string]$AcrName = "acrfilmhub5497"
[string]$ContainerAppEnv = "acaenv-filmhub"
[string]$Tag = "latest"
```

`ResourceGroup` indica il gruppo di risorse Azure in cui si trovano le risorse del progetto.

`AcrName` e il nome dell'Azure Container Registry. Serve per fare login, recuperare il server del registry e caricare le immagini Docker.

`ContainerAppEnv` indica l'ambiente Azure Container Apps in cui vengono eseguite le app.

`Tag` rappresenta la versione dell'immagine Docker. In questo progetto viene usato `latest`, che indica l'ultima versione disponibile. In un ambiente piu strutturato sarebbe meglio usare tag versionati, ad esempio `v1.0.3`, per facilitare rollback e tracciamento.

Nel deploy API e presente anche:

```powershell
[string]$ApiAppName = "filmhub-api"
[string]$ImageName = "filmhub-api"
```

`ApiAppName` e il nome della Container App su Azure, mentre `ImageName` e il nome dell'immagine Docker. In questo caso coincidono per chiarezza.

Nel deploy frontend ci sono valori equivalenti:

```powershell
[string]$FrontendAppName = "filmhub-frontend"
[string]$ImageName = "filmhub-frontend"
```

## Recupero delle informazioni dell'ACR
La prima fase dello script recupera le informazioni necessarie per usare Azure Container Registry.

Il comando:

```powershell
$acrLoginServer = az acr show -n $AcrName --query loginServer -o tsv --only-show-errors
```

si compone cosi:

- `az acr show` chiede ad Azure i dettagli di un Container Registry.
- `-n $AcrName` specifica quale registry leggere.
- `--query loginServer` estrae solo il campo `loginServer`.
- `-o tsv` restituisce un output semplice, senza JSON.

Il risultato e un valore simile a:

```text
acrfilmhub5497.azurecr.io
```

Questo valore serve per comporre il nome completo dell'immagine da pubblicare.

Subito dopo lo script recupera anche utente e password del registry:

```powershell
$acrUser = az acr credential show -n $AcrName --query username -o tsv --only-show-errors
$acrPass = az acr credential show -n $AcrName --query "passwords[0].value" -o tsv --only-show-errors
```

Queste credenziali servono soprattutto alla Container App per scaricare l'immagine dal registry privato.

## Build dell'immagine Docker
Per il backend lo script esegue:

```powershell
docker build -f "$root\backend\Dockerfile" -t "$ImageName`:$Tag" "$root\backend"
```

Il comando si compone cosi:

- `docker build` avvia la costruzione di un'immagine Docker.
- `-f "$root\backend\Dockerfile"` indica quale Dockerfile usare.
- `-t "$ImageName`:$Tag"` assegna nome e tag all'immagine.
- `"$root\backend"` e il contesto di build, cioe la cartella da cui Docker puo leggere i file.

Con i valori reali diventa:

```powershell
docker build -f "D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Dockerfile" -t "filmhub-api:latest" "D:\Scuola\5IA\INFO\ClaudeCode\Test\backend"
```

Per il frontend la logica e uguale, ma cambia il Dockerfile e il contesto:

```powershell
docker build -f "$root\Dockerfile.frontend" -t "$ImageName`:$Tag" "$root"
```

Qui il contesto e la root del progetto, perche il Dockerfile deve accedere alla cartella `frontend`.

## Pubblicazione dell'immagine su Azure Container Registry
Dopo la build, l'immagine esiste solo in locale. Per farla usare ad Azure bisogna pubblicarla su ACR.

Prima si effettua il login:

```powershell
az acr login -n $AcrName --only-show-errors
```

Poi si compone il nome completo dell'immagine:

```powershell
$fullImage = "$acrLoginServer/$ImageName`:$Tag"
```

Con i valori del backend diventa:

```text
acrfilmhub5497.azurecr.io/filmhub-api:latest
```

A questo punto l'immagine locale viene rinominata/taggata con il percorso remoto:

```powershell
docker tag "filmhub-api:latest" "acrfilmhub5497.azurecr.io/filmhub-api:latest"
```

Infine viene caricata sul registry:

```powershell
docker push "acrfilmhub5497.azurecr.io/filmhub-api:latest"
```

Lo stesso procedimento viene applicato anche al frontend, usando `filmhub-frontend`.

## Creazione o aggiornamento della Container App
La parte piu importante dello script e quella che controlla se la Container App esiste gia.

```powershell
$exists = az containerapp show -g $ResourceGroup -n $ApiAppName --query name -o tsv --only-show-errors 2>$null
```

Questo comando prova a leggere la Container App. Se esiste, restituisce il nome. Se non esiste, l'errore viene mandato a `2>$null`, cosi non sporca l'output.

Se l'app esiste, lo script aggiorna registry e immagine:

```powershell
az containerapp registry set `
  -g $ResourceGroup -n $ApiAppName `
  --server $acrLoginServer `
  --username $acrUser `
  --password $acrPass
```

Poi aggiorna l'immagine:

```powershell
az containerapp update `
  -g $ResourceGroup -n $ApiAppName `
  --image $fullImage
```

Se invece l'app non esiste, viene creata:

```powershell
az containerapp create `
  -g $ResourceGroup -n $ApiAppName --environment $ContainerAppEnv `
  --image $fullImage `
  --target-port 8080 `
  --ingress external `
  --registry-server $acrLoginServer `
  --registry-username $acrUser `
  --registry-password $acrPass `
  --min-replicas 1 --max-replicas 1
```

`--target-port 8080` dice ad Azure che il container ascolta sulla porta `8080`.

`--ingress external` rende l'app raggiungibile dall'esterno tramite HTTPS.

`--min-replicas 1` mantiene sempre almeno una istanza attiva.

`--max-replicas 1` limita il numero massimo di istanze a una. Questo aiuta a controllare costi e comportamento durante il progetto scolastico.

## Configurazione delle variabili d'ambiente su Azure
Nel deploy del backend e presente anche una fase dedicata alle variabili d'ambiente:

```powershell
az containerapp update `
  -g $ResourceGroup -n $ApiAppName `
  --set-env-vars DB_HOST=filmhub-db DB_PORT=3306 DB_NAME=filmapi_db DB_USER=root JWT_ISSUER=FilmAPI JWT_AUDIENCE=FilmFrontend
```

Questo comando aggiorna la configurazione runtime del container. Non modifica il codice e non ricostruisce l'immagine: cambia solo i valori letti dall'applicazione quando parte.

`DB_HOST` indica dove si trova il database. Nel codice esiste anche un valore di fallback verso l'host interno completo Azure.

`DB_PORT=3306` indica la porta standard di MySQL.

`DB_NAME=filmapi_db` indica il database usato dal progetto.

`DB_USER=root` indica l'utente usato per collegarsi al database.

`JWT_ISSUER=FilmAPI` identifica chi emette i token JWT.

`JWT_AUDIENCE=FilmFrontend` indica per quale client sono pensati i token.

Alcune variabili non sono inserite direttamente nello script perche sono sensibili. Per esempio `DB_PASSWORD`, `JWT_SECRET_KEY`, `STRIPE_SECRET_KEY` e `SMTP_PASSWORD` non dovrebbero essere scritte in chiaro nel codice o negli script pubblici. Vanno configurate come variabili sicure o secret su Azure.

## Collegamento del frontend alla API
Nel file [frontend/wwwroot/js/api-config.js](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\wwwroot\js\api-config.js) e presente:

```javascript
const AZURE_API_BASE_URL = 'https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io';
```

Questo valore rappresenta l'indirizzo della API pubblicata su Azure. Quando il frontend deve chiamare il backend, usa questo URL come base.

Nel file [frontend/Program.cs](D:\Scuola\5IA\INFO\ClaudeCode\Test\frontend\Program.cs) viene usata anche la variabile:

```csharp
EXTERNAL_AUTH_BACKEND_BASE_URL
```

Se questa variabile e impostata, il frontend la usa come URL del backend; altrimenti usa il valore Azure gia presente come fallback. Questo rende il frontend piu flessibile, perche puo essere spostato su altri ambienti senza cambiare codice.

## Controllo finale degli URL pubblici
Alla fine degli script viene recuperato il dominio pubblico della Container App:

```powershell
$apiFqdn = az containerapp show -g $ResourceGroup -n $ApiAppName --query properties.configuration.ingress.fqdn -o tsv --only-show-errors
Write-Host "API URL: https://$apiFqdn"
```

Il comando `az containerapp show` legge i dettagli della Container App.

`--query properties.configuration.ingress.fqdn` estrae solo il Fully Qualified Domain Name, cioe il dominio pubblico generato da Azure.

Il risultato viene stampato come URL HTTPS, per esempio:

```text
https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io
```

Lo stesso viene fatto per il frontend.

## Esempi pratici di composizione dei comandi

### Leggere il dominio pubblico della API
Per leggere il dominio pubblico bisogna sapere tre cose: il comando base, il Resource Group e il nome della app.

La forma generale e:

```powershell
az containerapp show -g <resource-group> -n <nome-app> --query properties.configuration.ingress.fqdn -o tsv
```

Nel progetto FilmHub i valori sono:

- `<resource-group>` = `rgfilmhubenvfrancecentral237`
- `<nome-app>` = `filmhub-api`

Quindi il comando completo diventa:

```powershell
az containerapp show -g rgfilmhubenvfrancecentral237 -n filmhub-api --query properties.configuration.ingress.fqdn -o tsv
```

### Aggiornare una variabile d'ambiente
La forma generale e:

```powershell
az containerapp update -g <resource-group> -n <nome-app> --set-env-vars CHIAVE=VALORE
```

Se voglio aggiornare l'emittente dei token JWT, la chiave e `JWT_ISSUER` e il valore e `FilmAPI`.

Il comando diventa:

```powershell
az containerapp update -g rgfilmhubenvfrancecentral237 -n filmhub-api --set-env-vars JWT_ISSUER=FilmAPI
```

Se devo aggiornare piu variabili insieme, le scrivo una dopo l'altra:

```powershell
az containerapp update -g rgfilmhubenvfrancecentral237 -n filmhub-api --set-env-vars DB_HOST=filmhub-db DB_PORT=3306 DB_NAME=filmapi_db DB_USER=root
```

### Creare una nuova versione immagine
Per creare una immagine Docker servono tre elementi: il Dockerfile, il nome dell'immagine e il contesto di build.

Per il backend:

```powershell
docker build -f "D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Dockerfile" -t "filmhub-api:latest" "D:\Scuola\5IA\INFO\ClaudeCode\Test\backend"
```

Qui `filmhub-api` e il nome dell'immagine e `latest` e il tag. Il tag puo essere cambiato con una versione, ad esempio:

```powershell
docker build -f "D:\Scuola\5IA\INFO\ClaudeCode\Test\backend\Dockerfile" -t "filmhub-api:v1.0.0" "D:\Scuola\5IA\INFO\ClaudeCode\Test\backend"
```

### Pubblicare l'immagine sul registry
Dopo la build locale, l'immagine deve essere associata al registry remoto.

Formato generale:

```powershell
docker tag <immagine-locale>:<tag> <acr-login-server>/<immagine-remota>:<tag>
```

Nel caso della API:

```powershell
docker tag filmhub-api:latest acrfilmhub5497.azurecr.io/filmhub-api:latest
```

Poi si carica:

```powershell
docker push acrfilmhub5497.azurecr.io/filmhub-api:latest
```

### Aggiornare la Container App con una nuova immagine
Quando l'immagine e gia su ACR, Azure puo essere aggiornato con:

```powershell
az containerapp update -g rgfilmhubenvfrancecentral237 -n filmhub-api --image acrfilmhub5497.azurecr.io/filmhub-api:latest
```

Questo comando dice ad Azure: usa questa nuova immagine per la Container App `filmhub-api`.

## Controlli dopo il deploy
Dopo il deploy e importante verificare che il backend sia realmente partito e che riesca a collegarsi al database.

Il backend espone l'endpoint:

```text
/health
```

Quindi si puo testare con:

```powershell
curl https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io/health
```

Se la risposta contiene `Healthy`, significa che l'app e attiva e il controllo principale e andato bene. Se invece compare `Unhealthy`, la causa piu probabile e un problema con il database o con qualche variabile d'ambiente mancante.

Bisogna poi verificare anche il frontend aprendo:

```text
https://filmhub-frontend.delightfuldune-f7916078.francecentral.azurecontainerapps.io
```

Da li si controlla che le pagine si carichino e che le chiamate alla API funzionino.

## Problema possibile: modifiche non sincronizzate con `latest`
Durante il deploy puo capitare un problema abbastanza comune: si modifica il codice, si ricostruisce l'immagine, si fa il push su Azure Container Registry, ma su Azure sembra continuare a girare la versione precedente dell'applicazione.

Questo succede spesso quando si usa sempre lo stesso tag Docker, per esempio:

```text
latest
```

Il tag `latest` non significa davvero "forza sempre l'ultima versione". In pratica e solo un'etichetta. Se prima pubblico:

```text
acrfilmhub5497.azurecr.io/filmhub-api:latest
```

e poi pubblico di nuovo un'altra immagine con lo stesso identico nome e lo stesso identico tag, Azure Container Apps puo non capire immediatamente che deve scaricare una nuova immagine. Dal suo punto di vista l'immagine configurata e ancora:

```text
acrfilmhub5497.azurecr.io/filmhub-api:latest
```

Quindi il valore della configurazione non e cambiato. Anche se dentro ACR il contenuto dell'immagine e stato aggiornato, la Container App puo continuare a usare l'immagine gia presente o non fare un nuovo pull come ci si aspetta.

Questo e il motivo per cui a volte sembrava che il deploy non sincronizzasse le modifiche fatte: non era necessariamente un errore del codice, ma un problema di tracciamento della versione dell'immagine.

La soluzione piu corretta e usare tag versionati invece di usare sempre `latest`. Per esempio, invece di:

```powershell
.\scripts\deployment\deploy-api.ps1
```

si puo lanciare lo script passando un tag specifico:

```powershell
.\scripts\deployment\deploy-api.ps1 -Tag v1.0.1
```

In questo modo l'immagine pubblicata diventa:

```text
acrfilmhub5497.azurecr.io/filmhub-api:v1.0.1
```

Alla modifica successiva si usa un altro tag:

```powershell
.\scripts\deployment\deploy-api.ps1 -Tag v1.0.2
```

Azure vede chiaramente che l'immagine e cambiata, perche passa da `v1.0.1` a `v1.0.2`. Questo rende il deploy piu affidabile e permette anche di capire quale versione e attualmente online.

La stessa cosa vale per il frontend:

```powershell
.\scripts\deployment\deploy-frontend.ps1 -Tag v1.0.1
```

e poi:

```powershell
.\scripts\deployment\deploy-frontend.ps1 -Tag v1.0.2
```

Un altro vantaggio dei tag versionati e il rollback. Se una versione nuova ha un problema, si puo tornare alla precedente aggiornando la Container App con l'immagine vecchia:

```powershell
az containerapp update -g rgfilmhubenvfrancecentral237 -n filmhub-api --image acrfilmhub5497.azurecr.io/filmhub-api:v1.0.1
```

In sintesi, `latest` va bene per prove veloci, ma per un deploy piu chiaro e affidabile e meglio usare tag diversi a ogni pubblicazione, ad esempio `v1.0.1`, `v1.0.2`, oppure un tag basato sulla data come `2026-05-21-01`.

## Variabili d'ambiente principali
Le variabili d'ambiente sono fondamentali per separare codice e configurazione.

`DB_HOST` indica l'host del database. In locale puo essere un nome Docker o `localhost`; su Azure diventa un nome raggiungibile dalla Container App.

`DB_PORT` indica la porta del database. Per MySQL normalmente e `3306`.

`DB_NAME` indica il nome del database usato dall'applicazione.

`DB_USER` e `DB_PASSWORD` sono le credenziali di accesso al database.

`JWT_SECRET_KEY` e la chiave usata per firmare i token JWT. Deve essere lunga almeno 32 caratteri e non deve essere un placeholder. Se questa variabile manca, il backend non dovrebbe partire in modo sicuro.

`JWT_ISSUER` indica chi genera il token, in questo caso `FilmAPI`.

`JWT_AUDIENCE` indica chi deve usare il token, in questo caso `FilmFrontend`.

`CORS_ALLOWED_ORIGINS` contiene gli URL frontend autorizzati a chiamare la API. In produzione e molto importante, perche impedisce a domini non autorizzati di usare direttamente il backend dal browser.

`EXTERNAL_AUTH_BACKEND_BASE_URL` indica l'URL pubblico del backend da usare per autenticazioni esterne e redirect.

`STRIPE_SECRET_KEY`, `STRIPE_PUBLISHABLE_KEY` e `STRIPE_WEBHOOK_SECRET` servono per i pagamenti Stripe.

`SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD` e `SMTP_FROM` servono per l'invio delle email.

## Conclusione
Lo spostamento da locale ad Azure e stato realizzato trasformando backend e frontend in immagini Docker, pubblicandole su Azure Container Registry e distribuendole tramite Azure Container Apps.

La parte piu importante non e solo il deploy in se, ma il modo in cui il progetto e stato reso configurabile: dati come database, token JWT, CORS, Stripe e SMTP vengono gestiti tramite variabili d'ambiente. Questo rende l'applicazione piu adatta al cloud, perche lo stesso codice puo funzionare in ambienti diversi senza modifiche dirette.

Il risultato finale e una struttura piu professionale: il progetto resta sviluppabile in locale, ma puo essere pubblicato online in modo ripetibile attraverso gli script `scripts/deployment/deploy-api.ps1` e `scripts/deployment/deploy-frontend.ps1`.
