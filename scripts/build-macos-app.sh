#!/usr/bin/env bash
# Packages QrScanner.Desktop as a real .app bundle (with Dock icon), for local testing.
# Usage: scripts/build-macos-app.sh [osx-arm64|osx-x64]
set -euo pipefail

cd "$(dirname "$0")/.."

RID="${1:-osx-arm64}"
CONFIG="Release"
APP_NAME="QR Scanner"
BUNDLE_ID="nl.theredhead.qrscanner"
EXECUTABLE_NAME="QrScanner.Desktop"

PUBLISH_DIR="QrScanner.Desktop/bin/$CONFIG/net10.0/$RID/publish"
APP_DIR="dist/$APP_NAME.app"

echo "Publishing self-contained build for $RID..."
dotnet publish QrScanner.Desktop/QrScanner.Desktop.csproj -c "$CONFIG" -r "$RID" --self-contained -p:UseAppHost=true

echo "Assembling $APP_NAME.app..."
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"

cp -R "$PUBLISH_DIR/." "$APP_DIR/Contents/MacOS/"
cp "QrScanner.Desktop/Resources/AppIcon.icns" "$APP_DIR/Contents/Resources/AppIcon.icns"

cat > "$APP_DIR/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleName</key>
	<string>$APP_NAME</string>
	<key>CFBundleDisplayName</key>
	<string>$APP_NAME</string>
	<key>CFBundleIdentifier</key>
	<string>$BUNDLE_ID</string>
	<key>CFBundleExecutable</key>
	<string>$EXECUTABLE_NAME</string>
	<key>CFBundleIconFile</key>
	<string>AppIcon</string>
	<key>CFBundlePackageType</key>
	<string>APPL</string>
	<key>CFBundleShortVersionString</key>
	<string>1.0</string>
	<key>CFBundleVersion</key>
	<string>1</string>
	<key>NSCameraUsageDescription</key>
	<string>QR Scanner uses the camera to scan QR codes.</string>
	<key>NSHighResolutionCapable</key>
	<true/>
</dict>
</plist>
PLIST

chmod +x "$APP_DIR/Contents/MacOS/$EXECUTABLE_NAME"

echo ""
echo "Done: $APP_DIR"
echo "Open it with: open \"$APP_DIR\""
