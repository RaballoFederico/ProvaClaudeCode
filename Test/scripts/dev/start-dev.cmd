@echo off
setlocal

set "AZURE_FRONTEND=https://filmhub-frontend.delightfuldune-f7916078.francecentral.azurecontainerapps.io/home.html"
set "AZURE_API=https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io"

echo.
echo Ambiente configurato su Azure.
echo Frontend: %AZURE_FRONTEND%
echo Backend:  %AZURE_API%
echo.
echo Premi un tasto per chiudere questa finestra.
pause >nul
