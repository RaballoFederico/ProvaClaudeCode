@echo off
setlocal

echo Arresto processi progetto...
taskkill /IM FilmAPI.exe /F >nul 2>&1
taskkill /IM FilmFrontend.exe /F >nul 2>&1

echo Fatto.
echo Premi un tasto per chiudere questa finestra.
pause >nul

