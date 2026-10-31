#!/usr/bin/env bash
# Builds the mod and assembles the r2modman/Thunderstore zip in out/.
# Needs the game and BepInEx DLLs in libs/ first (see README, "Building from source").
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build -c Release -v q
P=dist/plugins/ror2-plus
cp bin/Release/netstandard2.1/ror2-plus.dll "$P/"
rm -rf "$P/models" && cp -r art/models "$P/models"
VER=$(python3 -c "import json;print(json.load(open('dist/manifest.json'))['version_number'])")
mkdir -p out && rm -f "out/ror2-plus-$VER.zip"
(cd dist && zip -qr "../out/ror2-plus-$VER.zip" manifest.json README.md CHANGELOG.md icon.png plugins)
echo "out/ror2-plus-$VER.zip"
