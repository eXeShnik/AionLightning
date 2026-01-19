#!/usr/bin/env bash
set -euo pipefail

export JAVA_HOME='/Library/Java/JavaVirtualMachines/jdk-1.8.jdk/Contents/Home'

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

ant_cmd=()
if command -v ant >/dev/null 2>&1; then
  ant_cmd=(ant)
else
  echo "ERROR: Apache Ant not found. Install ant (e.g. 'brew install ant') and ensure it's on PATH." >&2
  exit 1
fi

cd "$ROOT_DIR/AL-Game"
"${ant_cmd[@]}" "$@"
