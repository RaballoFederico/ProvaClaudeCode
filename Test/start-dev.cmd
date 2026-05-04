@echo off
setlocal

set "ROOT=%~dp0"
set "BACKEND_DIR=%ROOT%backend"
set "FRONTEND_DIR=%ROOT%frontend"

tasklist /FI "IMAGENAME eq FilmAPI.exe" | find /I "FilmAPI.exe" >nul
if %ERRORLEVEL%==0 (
    echo Backend gia attivo: http://127.0.0.1:5001
) else (
    echo Avvio backend su http://127.0.0.1:5001 ...
    start "FilmAPI Backend" cmd /k "cd /d "%BACKEND_DIR%" && dotnet run --urls http://127.0.0.1:5001"
)

tasklist /FI "IMAGENAME eq FilmFrontend.exe" | find /I "FilmFrontend.exe" >nul
if %ERRORLEVEL%==0 (
    echo Frontend gia attivo: http://127.0.0.1:5285
) else (
    echo Avvio frontend su http://127.0.0.1:5285 ...
    start "FilmFrontend" cmd /k "cd /d "%FRONTEND_DIR%" && dotnet run --urls http://127.0.0.1:5285"
)

echo.
echo Frontend: http://127.0.0.1:5285/home.html
echo Backend:  http://127.0.0.1:5001
echo.
echo Se vuoi ripartire pulito: esegui prima stop-dev.cmd e poi start-dev.cmd
echo.
echo Premi un tasto per chiudere questa finestra.
pause >nul
