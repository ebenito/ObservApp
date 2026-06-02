@echo off
REM Script para iniciar la versión Web de ObservApp
REM Uso: start-web.bat

echo.
echo ======================================
echo   Iniciando ObservApp Web
echo ======================================
echo.

cd /d "%~dp0ObservApp.Web"

if not exist "ObservApp.Web.csproj" (
	echo ERROR: No se encuentra ObservApp.Web.csproj
	echo Asegurate de ejecutar este script desde la raiz del proyecto.
	pause
	exit /b 1
)

echo Compilando proyecto...
dotnet build --configuration Debug

if %errorlevel% neq 0 (
	echo.
	echo ERROR de compilacion. Revisa los errores anteriores.
	pause
	exit /b 1
)

echo.
echo Compilacion exitosa!
echo.
echo Iniciando servidor web...
echo URL: https://localhost:7294
echo Presiona Ctrl+C para detener el servidor
echo.

dotnet watch run --launch-profile https

if %errorlevel% neq 0 (
	echo.
	echo 'dotnet watch' fallo, intentando sin watch...
	dotnet run --launch-profile https
)

pause
