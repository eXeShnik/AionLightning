#!/usr/bin/env bash
set -euo pipefail

export JAVA_HOME='/Library/Java/JavaVirtualMachines/jdk-1.8.jdk/Contents/Home'

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

ant_cmd=()
if [[ -f "$ROOT_DIR/Tools/Ant/bin/ant" ]]; then
  ant_cmd=(sh "$ROOT_DIR/Tools/Ant/bin/ant")
elif command -v ant >/dev/null 2>&1; then
  ant_cmd=(ant)
else
  echo "ERROR: Apache Ant not found. Install ant (e.g. 'brew install ant') or ensure Tools/Ant exists." >&2
  exit 1
fi

mode="build" # build | install | debug

run_ant() {
  local module="$1"; shift
  local -a args=()

  case "$mode" in
    build)
      # mirrors MODE=clean package in the .bat menu
      args=(clean package)
      ;;
    install)
      args=(clean install)
      ;;
    debug)
      # mirrors MODE=clean package -e -X in the .bat menu
      args=(-e -X clean package)
      ;;
    *)
      args=()
      ;;
  esac

  echo
  echo "==> $mode: $module"
  (cd "$ROOT_DIR/$module" && "${ant_cmd[@]}" "${args[@]}" "$@")
}

while true; do
  title="Build"
  [[ "$mode" == "install" ]] && title="Install"
  [[ "$mode" == "debug" ]] && title="Debug"

  clear || true
  cat <<EOF

*--------------------------------------------------------------------------*
|                    Aion Lightning Project - $title Panel                  |
*--------------------------------------------------------------------------*
|                                                                          |
|    1 - $title Login server                         6 - Build mode        |
|    2 - $title Game server                          7 - Install mode      |
|    3 - $title Chat server                          8 - Debug mode        |
|    4 - $title Commons                              9 - Quit              |
|    5 - $title All                                                         |
|                                                                          |
*--------------------------------------------------------------------------*

EOF

  read -r -p "Type your option and press ENTER: " option

  case "$option" in
    1) run_ant AL-Login ;;
    2) run_ant AL-Game ;;
    3) run_ant AL-Chat ;;
    4) run_ant AL-Commons ;;
    5)
      run_ant AL-Login
      run_ant AL-Game
      run_ant AL-Chat
      run_ant AL-Commons
      ;;
    6) mode="build" ;;
    7) mode="install" ;;
    8) mode="debug" ;;
    9) exit 0 ;;
    *) continue ;;
  esac

  read -r -p "Press ENTER to continue..." _
done
