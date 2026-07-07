<#
.SYNOPSIS
    Empaqueta gphoto2.exe y todas sus dependencias MSYS2 (DLLs + drivers de
    cámara/puerto) dentro de ObservApp\Tools\gphoto2\, con la estructura que
    espera GPhoto2CameraService.cs.

.NOTES
    Ejecutar desde una PowerShell normal de Windows (no hace falta la
    terminal MSYS2). Requiere que MSYS2 y el paquete
    mingw-w64-x86_64-gphoto2 ya estén instalados.
#>

$ErrorActionPreference = "Stop"

# ── Rutas de origen y destino ────────────────────────────────────────────
$Msys2Bin = "C:\msys64\mingw64\bin"
$Msys2Lib = "C:\msys64\mingw64\lib"
$DestRoot = "C:\Proyectos\XTV\ObservApp\ObservApp\Tools\gphoto2"

# Únicamente las DLLs que cuelgan de /mingw64/bin/ en tu salida de `ldd`.
# Deliberadamente EXCLUIDAS: ntdll.dll, KERNEL32.DLL, KERNELBASE.dll,
# ADVAPI32.dll, msvcrt.dll, sechost.dll, RPCRT4.dll, SHLWAPI.dll,
# ucrtbase.dll, SHELL32.dll, msvcp_win.dll, USER32.dll, win32u.dll,
# GDI32.dll, gdi32full.dll (todas son DLLs del propio Windows, ya presentes
# en cualquier instalación) e InProcessClient64.dll (inyectada en runtime
# por el agente SentinelOne — no es una dependencia real del binario).
$RequiredDlls = @(
    "libgphoto2-6.dll",
    "libgphoto2_port-12.dll",
    "libintl-8.dll",
    "libexif-12.dll",
    "libpopt-0.dll",
    "libreadline8.dll",
    "libwinpthread-1.dll",
    "libltdl-7.dll",
    "libiconv-2.dll",
    "libsystre-0.dll",
    "libtermcap-0.dll",
    "libtre-5.dll"
)

Write-Host "== Empaquetando gphoto2 para ObservApp ==" -ForegroundColor Cyan

if (-not (Test-Path "$Msys2Bin\gphoto2.exe")) {
    throw "No se encuentra $Msys2Bin\gphoto2.exe. Verifica que MSYS2 y el paquete mingw-w64-x86_64-gphoto2 están instalados."
}

New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null

# ── 1. Ejecutable + DLLs ──────────────────────────────────────────────────
Write-Host "Copiando gphoto2.exe..."
Copy-Item "$Msys2Bin\gphoto2.exe" -Destination $DestRoot -Force

foreach ($dll in $RequiredDlls) {
    $src = Join-Path $Msys2Bin $dll
    if (Test-Path $src) {
        Copy-Item $src -Destination $DestRoot -Force
        Write-Host "  + $dll"
    } else {
        Write-Warning "No encontrada: $dll (revisa si tu build tiene un nombre/versión distinta)"
    }
}

# ── 2. Drivers de cámara (camlibs) ───────────────────────────────────────
# libgphoto2 busca aquí los .so/.dll de cada modelo de cámara soportado
# (ptp2.dll, canon.dll, etc.). La carpeta de origen incluye el número de
# versión de libgphoto2 (p. ej. lib\libgphoto2\2.5.33), así que se localiza
# dinámicamente en vez de asumir un valor fijo.
$camlibsSrc = Get-ChildItem -Path (Join-Path $Msys2Lib "libgphoto2") -Directory |
              Sort-Object Name -Descending | Select-Object -First 1

if ($null -eq $camlibsSrc) {
    throw "No se encontró la carpeta de camlibs en $Msys2Lib\libgphoto2\<version>\"
}

$camlibsDest = Join-Path $DestRoot "camlibs"
Write-Host "Copiando camlibs desde $($camlibsSrc.FullName)..."
New-Item -ItemType Directory -Force -Path $camlibsDest | Out-Null
Copy-Item "$($camlibsSrc.FullName)\*" -Destination $camlibsDest -Recurse -Force

# ── 3. Drivers de puerto (iolibs) ────────────────────────────────────────
# Equivalente para los backends de transporte (usb1.dll, serial.dll...).
$iolibsSrc = Get-ChildItem -Path (Join-Path $Msys2Lib "libgphoto2_port") -Directory |
             Sort-Object Name -Descending | Select-Object -First 1

if ($null -eq $iolibsSrc) {
    throw "No se encontró la carpeta de iolibs en $Msys2Lib\libgphoto2_port\<version>\"
}

$iolibsDest = Join-Path $DestRoot "iolibs"
Write-Host "Copiando iolibs desde $($iolibsSrc.FullName)..."
New-Item -ItemType Directory -Force -Path $iolibsDest | Out-Null
Copy-Item "$($iolibsSrc.FullName)\*" -Destination $iolibsDest -Recurse -Force

Write-Host ""
Write-Host "== Completado ==" -ForegroundColor Green
Write-Host "Estructura generada en: $DestRoot"
Get-ChildItem $DestRoot -Recurse | Select-Object FullName

Write-Host ""
Write-Host "Prueba manual recomendada:" -ForegroundColor Yellow
Write-Host "  cd `"$DestRoot`""
Write-Host "  `$env:CAMLIBS = `"$DestRoot\camlibs`""
Write-Host "  `$env:IOLIBS  = `"$DestRoot\iolibs`""
Write-Host "  .\gphoto2.exe --auto-detect"
