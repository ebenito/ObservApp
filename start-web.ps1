#!/usr/bin/env pwsh
# Script para iniciar la versión Web de ObservApp
# Uso: .\start-web.ps1

Write-Host "🚀 Iniciando ObservApp Web..." -ForegroundColor Cyan
Write-Host ""

# Navegar al directorio del proyecto web
Set-Location -Path "$PSScriptRoot\ObservApp.Web"

# Verificar que estamos en el directorio correcto
if (-not (Test-Path "ObservApp.Web.csproj")) {
	Write-Host "❌ Error: No se encuentra ObservApp.Web.csproj" -ForegroundColor Red
	Write-Host "   Asegúrate de ejecutar este script desde la raíz del proyecto." -ForegroundColor Yellow
	exit 1
}

Write-Host "📦 Compilando proyecto..." -ForegroundColor Yellow
dotnet build --configuration Debug

if ($LASTEXITCODE -ne 0) {
	Write-Host "❌ Error de compilación. Revisa los errores anteriores." -ForegroundColor Red
	exit 1
}

Write-Host ""
Write-Host "✅ Compilación exitosa" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Iniciando servidor web..." -ForegroundColor Cyan
Write-Host "   URL: https://localhost:7294" -ForegroundColor White
Write-Host "   Presiona Ctrl+C para detener el servidor" -ForegroundColor Gray
Write-Host ""

# Ejecutar con watch para hot reload
dotnet watch run --launch-profile https

# Si el comando anterior falla, intentar sin watch
if ($LASTEXITCODE -ne 0) {
	Write-Host "⚠️  'dotnet watch' falló, intentando sin watch..." -ForegroundColor Yellow
	dotnet run --launch-profile https
}
