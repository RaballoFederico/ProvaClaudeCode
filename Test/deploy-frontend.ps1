param(
  [string]$ResourceGroup = "rgfilmhubenvfrancecentral237",
  [string]$AcrName = "acrfilmhub5497",
  [string]$ContainerAppEnv = "acaenv-filmhub",
  [string]$FrontendAppName = "filmhub-frontend",
  [string]$ImageName = "filmhub-frontend",
  [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"
$root = "D:\Scuola\5IA\INFO\ClaudeCode\Test"

Write-Host "[1/5] Resolve ACR info..."
$acrLoginServer = az acr show -n $AcrName --query loginServer -o tsv
$acrUser = az acr credential show -n $AcrName --query username -o tsv
$acrPass = az acr credential show -n $AcrName --query "passwords[0].value" -o tsv

Write-Host "[2/5] Build frontend image..."
docker build -f "$root\Dockerfile.frontend" -t "$ImageName`:$Tag" "$root"

Write-Host "[3/5] Push image to ACR..."
az acr login -n $AcrName
$fullImage = "$acrLoginServer/$ImageName`:$Tag"
docker tag "$ImageName`:$Tag" $fullImage
docker push $fullImage

Write-Host "[4/5] Create or update Container App..."
$exists = az containerapp show -g $ResourceGroup -n $FrontendAppName --query name -o tsv 2>$null
if ($exists) {
  az containerapp registry set `
    -g $ResourceGroup -n $FrontendAppName `
    --server $acrLoginServer `
    --username $acrUser `
    --password $acrPass | Out-Null

  az containerapp update `
    -g $ResourceGroup -n $FrontendAppName `
    --image $fullImage | Out-Null
} else {
  az containerapp create `
    -g $ResourceGroup -n $FrontendAppName --environment $ContainerAppEnv `
    --image $fullImage `
    --target-port 8080 `
    --ingress external `
    --registry-server $acrLoginServer `
    --registry-username $acrUser `
    --registry-password $acrPass `
    --min-replicas 1 --max-replicas 1 | Out-Null
}

Write-Host "[5/5] Done"
$frontFqdn = az containerapp show -g $ResourceGroup -n $FrontendAppName --query properties.configuration.ingress.fqdn -o tsv
Write-Host "Frontend URL: https://$frontFqdn"
