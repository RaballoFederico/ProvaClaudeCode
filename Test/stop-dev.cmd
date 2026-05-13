@echo off
setlocal

set "ROOT=%~dp0"
set "RUNTIME_DIR=%ROOT%.runtime"
set "BACKEND_PID_FILE=%RUNTIME_DIR%\backend.pid"
set "FRONTEND_PID_FILE=%RUNTIME_DIR%\frontend.pid"

echo Arresto processi progetto...

if exist "%BACKEND_PID_FILE%" (
    set /p BACKEND_PID=<"%BACKEND_PID_FILE%"
    if not "%BACKEND_PID%"=="" (
        powershell -NoProfile -Command "$p = Get-Process -Id %BACKEND_PID% -ErrorAction SilentlyContinue; if ($p -and $p.ProcessName -eq 'dotnet') { Stop-Process -Id %BACKEND_PID% -Force }"
    )
    del "%BACKEND_PID_FILE%" >nul 2>&1
)

if exist "%FRONTEND_PID_FILE%" (
    set /p FRONTEND_PID=<"%FRONTEND_PID_FILE%"
    if not "%FRONTEND_PID%"=="" (
        powershell -NoProfile -Command "$p = Get-Process -Id %FRONTEND_PID% -ErrorAction SilentlyContinue; if ($p -and $p.ProcessName -eq 'dotnet') { Stop-Process -Id %FRONTEND_PID% -Force }"
    )
    del "%FRONTEND_PID_FILE%" >nul 2>&1
)

for /f "tokens=5" %%P in ('netstat -ano ^| findstr /R /C:":5001 .*LISTENING"') do taskkill /PID %%P /F >nul 2>&1
for /f "tokens=5" %%P in ('netstat -ano ^| findstr /R /C:":5285 .*LISTENING"') do taskkill /PID %%P /F >nul 2>&1

echo Fatto.
echo Premi un tasto per chiudere questa finestra.
pause >nul

