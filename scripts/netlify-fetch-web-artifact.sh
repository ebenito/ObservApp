#!/usr/bin/env bash
# Descarga el último artefacto "ObservApp-Web" que publica el job build-web
# (.github/workflows/build.yml) y lo deja listo en publish/web/wwwroot para que
# Netlify lo publique. Netlify no puede compilar .NET 10, así que reutiliza la
# compilación de GitHub Actions.
#
# Los artefactos de GitHub solo se descargan con autenticación; nightly.link
# hace de proxy público para repositorios públicos.
set -euo pipefail

REPO="ebenito/ObservApp"
WORKFLOW="build"
ARTIFACT="ObservApp-Web"

# Rama de la que tomar el artefacto: master para master, develop para el resto
# (deploy previews y ramas sin build-web propio).
case "${BRANCH:-}" in
  master) ARTIFACT_BRANCH="master" ;;
  *)      ARTIFACT_BRANCH="${ARTIFACT_BRANCH:-develop}" ;;
esac

URL="https://nightly.link/${REPO}/workflows/${WORKFLOW}/${ARTIFACT_BRANCH}/${ARTIFACT}.zip"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "Descargando ${ARTIFACT} (rama ${ARTIFACT_BRANCH})..."
curl -fsSL --retry 3 -o "$TMP/web.zip" "$URL"

rm -rf publish/web
mkdir -p publish/web
unzip -q "$TMP/web.zip" -d publish/web

cd publish/web/wwwroot

# Netlify comprime en el CDN: sobran las versiones precomprimidas.
find . \( -name '*.gz' -o -name '*.br' \) -delete

# La app solo usa el tema material3-dark de Syncfusion (index.html).
if [ -d _content/Syncfusion.Blazor.Themes ]; then
  find _content/Syncfusion.Blazor.Themes -type f ! -name 'material3-dark.css' -delete
  find _content/Syncfusion.Blazor.Themes -type d -empty -delete
fi

# Configuración exclusiva de Azure Static Web Apps.
rm -f staticwebapp.config.json

echo "Publish listo: $(du -sh . | cut -f1), $(find . -type f | wc -l) ficheros"