@echo off
SETLOCAL

REM Asegúrate de que el script se ejecuta en el directorio del repositorio
cd /d "%~dp0"

REM Agregar todos los cambios al staging area
git add .

REM Hacer un commit con la fecha actual como mensaje
FOR /F "tokens=2-4 delims=/ " %%i IN ('date /t') DO SET fecha=%%k-%%i-%%j
git commit -m "Ed%fecha%"

REM Empujar los cambios al repositorio remoto
git push

ENDLOCAL
