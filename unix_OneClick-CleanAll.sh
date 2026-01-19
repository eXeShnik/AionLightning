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

echo "############################################"
echo "########## Cleaning all files ... ##########"
echo "########## Execute from repo root ##########"
echo "############################################"

for module in AL-Chat AL-Commons AL-Login AL-Game; do
  echo
  echo "==> Cleaning $module"
  (cd "$ROOT_DIR/$module" && "${ant_cmd[@]}" clean)
done

echo
echo "############################################"
echo "################# Completed ################"
echo "############################################"
