#!/usr/bin/env pwsh
<#
.SYNOPSIS
	Script para obtener la versión actual de GitVersion localmente

.DESCRIPTION
	Ejecuta GitVersion y muestra la versión calculada.
	Útil para verificar qué versión se usará en la próxima compilación.

.EXAMPLE
	.\Get-Version.ps1
#>

Write-Host "📦 Obteniendo versión con GitVersion..." -ForegroundColor Cyan

# Verificar si GitVersion está instalado
$gitversion = Get-Command dotnet-gitversion -ErrorAction SilentlyContinue
if (-not $gitversion) {
	Write-Host "⚠️  GitVersion no está instalado como herramienta global." -ForegroundColor Yellow
	Write-Host "Instalando: dotnet tool install -g GitVersion.Tool" -ForegroundColor Yellow
	dotnet tool install -g GitVersion.Tool
}

# Obtener versión
$output = dotnet-gitversion /output json /nofetch 2>$null | ConvertFrom-Json

Write-Host "✓ Versión actual:" -ForegroundColor Green
Write-Host "  SemVer (uso: Assembly.dll): $($output.SemVer)" -ForegroundColor Cyan
Write-Host "  Major.Minor: $($output.Major).$($output.Minor)" -ForegroundColor Cyan
Write-Host "  Full Version: $($output.FullSemVer)" -ForegroundColor Cyan
Write-Host "  Informational: $($output.InformationalVersion)" -ForegroundColor Cyan
Write-Host "  Branch: $($output.BranchName)" -ForegroundColor Cyan
Write-Host "  Commits: $($output.CommitsSinceVersionSource)" -ForegroundColor Cyan

Write-Host "`n💡 Para actualizar la versión, crea un nuevo tag:" -ForegroundColor Yellow
Write-Host "   git tag v$($output.Major).$($output.Minor).$($output.Patch + 1)" -ForegroundColor White
Write-Host "   git push origin --tags" -ForegroundColor White
