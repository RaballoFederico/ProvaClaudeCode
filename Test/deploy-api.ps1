param(
  [string]$ResourceGroup = "rgfilmhubenvfrancecentral237",
  [string]$AcrName = "acrfilmhub5497",
  [string]$ContainerAppEnv = "acaenv-filmhub",
  [string]$ApiAppName = "filmhub-api",
  [string]$ImageName = "filmhub-api",
  [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"
$root = "D:\Scuola\5IA\INFO\ClaudeCode\Test"

Write-Host "[1/6] Resolve ACR info..."
$acrLoginServer = az acr show -n $AcrName --query loginServer -o tsv
$acrUser = az acr credential show -n $AcrName --query username -o tsv
$acrPass = az acr credential show -n $AcrName --query "passwords[0].value" -o tsv

Write-Host "[2/6] Build API image..."
docker build -f "$root\backend\Dockerfile" -t "$ImageName`:$Tag" "$root\backend"

Write-Host "[3/6] Push image to ACR..."
az acr login -n $AcrName
$fullImage = "$acrLoginServer/$ImageName`:$Tag"
docker tag "$ImageName`:$Tag" $fullImage
docker push $fullImage

Write-Host "[4/6] Create or update Container App..."
$exists = az containerapp show -g $ResourceGroup -n $ApiAppName --query name -o tsv 2>$null
if ($exists) {
  az containerapp registry set `
    -g $ResourceGroup -n $ApiAppName `
    --server $acrLoginServer `
    --username $acrUser `
    --password $acrPass | Out-Null

  az containerapp update `
    -g $ResourceGroup -n $ApiAppName `
    --image $fullImage | Out-Null
} else {
  az containerapp create `
    -g $ResourceGroup -n $ApiAppName --environment $ContainerAppEnv `
    --image $fullImage `
    --target-port 8080 `
    --ingress external `
    --registry-server $acrLoginServer `
    --registry-username $acrUser `
    --registry-password $acrPass `
    --min-replicas 1 --max-replicas 1 | Out-Null
}

Write-Host "[5/6] Ensure API env vars..."
az containerapp update `
  -g $ResourceGroup -n $ApiAppName `
  --set-env-vars DB_HOST=filmhub-db DB_PORT=3306 DB_NAME=filmapi_db DB_USER=root JWT_ISSUER=FilmAPI JWT_AUDIENCE=FilmFrontend | Out-Null

Write-Host "[6/6] Done"
$apiFqdn = az containerapp show -g $ResourceGroup -n $ApiAppName --query properties.configuration.ingress.fqdn -o tsv
Write-Host "API URL: https://$apiFqdn"
