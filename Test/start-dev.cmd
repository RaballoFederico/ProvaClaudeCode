@echo off
setlocal

set "ROOT=%~dp0"
set "BACKEND_DIR=%ROOT%backend"
set "FRONTEND_DIR=%ROOT%frontend"
set "RUNTIME_DIR=%ROOT%.runtime"
set "BACKEND_PID_FILE=%RUNTIME_DIR%\backend.pid"
set "FRONTEND_PID_FILE=%RUNTIME_DIR%\frontend.pid"

if not exist "%RUNTIME_DIR%" mkdir "%RUNTIME_DIR%" >nul 2>&1

netstat -ano | findstr /R /C:":5001 .*LISTENING" >nul
if %ERRORLEVEL%==0 (
    echo Backend gia in ascolto sulla porta 5001: http://localhost:5001
    powershell -NoProfile -Command "$line = netstat -ano | Select-String ':5001' | Select-String 'LISTENING' | Select-Object -First 1; if ($line) { $p = ($line -split '\s+')[-1]; Set-Content -Path '%BACKEND_PID_FILE%' -Value $p }"
) else (
    echo Avvio backend su http://localhost:5001 ...
    start "FilmHub Backend :5001" /min cmd /k "cd /d ""%BACKEND_DIR%"" && dotnet run --project ""%BACKEND_DIR%\FilmAPI.csproj"" --urls http://localhost:5001"
    powershell -NoProfile -Command "Start-Sleep -Milliseconds 1200; $line = netstat -ano | Select-String ':5001' | Select-String 'LISTENING' | Select-Object -First 1; if ($line) { $p = ($line -split '\s+')[-1]; Set-Content -Path '%BACKEND_PID_FILE%' -Value $p } else { if (Test-Path '%BACKEND_PID_FILE%') { Remove-Item '%BACKEND_PID_FILE%' -Force } }"
)

netstat -ano | findstr /R /C:":5285 .*LISTENING" >nul
if %ERRORLEVEL%==0 (
    echo Frontend gia in ascolto sulla porta 5285: http://localhost:5285
    powershell -NoProfile -Command "$line = netstat -ano | Select-String ':5285' | Select-String 'LISTENING' | Select-Object -First 1; if ($line) { $p = ($line -split '\s+')[-1]; Set-Content -Path '%FRONTEND_PID_FILE%' -Value $p }"
) else (
    echo Avvio frontend su http://localhost:5285 ...
    start "FilmHub Frontend :5285" /min cmd /k "cd /d ""%FRONTEND_DIR%"" && dotnet run --project ""%FRONTEND_DIR%\FilmFrontend.csproj"" --urls http://localhost:5285"
    powershell -NoProfile -Command "Start-Sleep -Milliseconds 1200; $line = netstat -ano | Select-String ':5285' | Select-String 'LISTENING' | Select-Object -First 1; if ($line) { $p = ($line -split '\s+')[-1]; Set-Content -Path '%FRONTEND_PID_FILE%' -Value $p } else { if (Test-Path '%FRONTEND_PID_FILE%') { Remove-Item '%FRONTEND_PID_FILE%' -Force } }"
)

echo.
echo Frontend: http://localhost:5285/home.html
echo Backend:  http://localhost:5001
echo.
echo Per fermare tutto: esegui stop-dev.cmd
echo.
echo Premi un tasto per chiudere questa finestra.
pause >nul
