#!/usr/bin/env bash
set -euo pipefail

export JAVA_HOME='/Library/Java/JavaVirtualMachines/jdk-1.8.jdk/Contents/Home'

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$ROOT_DIR/../../Server"

login_zip_src="$ROOT_DIR/AL-Login/build/AL-Login.zip"
chat_zip_src="$ROOT_DIR/AL-Chat/build/AL-Chat.zip"
game_zip_src="$ROOT_DIR/AL-Game/build/AL-Game.zip"
commons_zip_src="$ROOT_DIR/AL-Commons/build/AL-Commons.zip"

mkdir -p "$SERVER_DIR"

echo
echo "Copying built server ZIPs into: $SERVER_DIR"
cp -f "$login_zip_src" "$SERVER_DIR/AL-Login.zip"
cp -f "$chat_zip_src" "$SERVER_DIR/AL-Chat.zip"
cp -f "$game_zip_src" "$SERVER_DIR/AL-Game.zip"
cp -f "$commons_zip_src" "$SERVER_DIR/AL-Commons.zip"

extract() {
  local zip_file="$1"
  local out_dir="$2"

  mkdir -p "$out_dir"

  if command -v 7z >/dev/null 2>&1; then
    7z x -y "$zip_file" -o"$out_dir" >/dev/null
  else
    unzip -o "$zip_file" -d "$out_dir" >/dev/null
  fi
}

echo
echo "Extracting ZIPs into Server/* directories..."
extract "$SERVER_DIR/AL-Login.zip" "$SERVER_DIR/AuthServer"
extract "$SERVER_DIR/AL-Chat.zip" "$SERVER_DIR/ChatServer"
extract "$SERVER_DIR/AL-Game.zip" "$SERVER_DIR/WorldServer"
extract "$SERVER_DIR/AL-Commons.zip" "$SERVER_DIR/WorldServer"

echo
echo "Cleaning up build directories..."
rm -rf "$ROOT_DIR/AL-Login/build" \
       "$ROOT_DIR/AL-Chat/build" \
       "$ROOT_DIR/AL-Game/build" \
       "$ROOT_DIR/AL-Commons/build"

echo
echo "Done."
