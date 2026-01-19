#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

bash "$ROOT_DIR/unix_build_commons.sh"

cp -R "$ROOT_DIR/AL-Commons/build/al-commons.jar" "$ROOT_DIR/AL-Login/libs/"
cp -R "$ROOT_DIR/AL-Commons/build/al-commons.jar" "$ROOT_DIR/AL-Chat/libs/"
cp -R "$ROOT_DIR/AL-Commons/build/al-commons.jar" "$ROOT_DIR/AL-Game/libs/"

bash "$ROOT_DIR/unix_build_loginserver.sh"
bash "$ROOT_DIR/unix_build_chatserver.sh"
bash "$ROOT_DIR/unix_build_gameserver.sh"