#!/usr/bin/env bash
# Pokreće testove: ./run-tests.sh [dotnet|python|flutter|all ...] [-- dodatni argumenti za alat]
# Primjeri:
#   ./run-tests.sh                       # sve
#   ./run-tests.sh dotnet                # samo .NET
#   ./run-tests.sh python flutter        # dva dijela
#   ./run-tests.sh python -- -k linucb   # filter (pytest -k)
#   ./run-tests.sh dotnet -- --filter "FullyQualifiedName~Bandit"
#   ./run-tests.sh flutter -- test/common
set -u
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND="$ROOT/HealthyPaws.Backend"
AI="$BACKEND/healthy_paws.ai"
UI="$ROOT/HealthyPaws.UI/healthy_paws"

usage() { sed -n '2,10p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; }

targets=()
extra=()
while [ $# -gt 0 ]; do
  case "$1" in
    -h|--help) usage; exit 0 ;;
    --) shift; extra=("$@"); break ;;
    dotnet|.net|net) targets+=(dotnet) ;;
    python|py|ai) targets+=(python) ;;
    flutter|dart|ui) targets+=(flutter) ;;
    all) targets+=(dotnet python flutter) ;;
    *) echo "Nepoznat argument: $1" >&2; usage >&2; exit 2 ;;
  esac
  shift
done
[ ${#targets[@]} -eq 0 ] && targets=(dotnet python flutter)
if [ ${#extra[@]} -gt 0 ] && [ "$(printf '%s\n' "${targets[@]}" | sort -u | wc -l)" -gt 1 ]; then
  echo "Dodatni argumenti (--) se mogu koristiti samo uz jedan odabrani dio." >&2; exit 2
fi

run_dotnet() { (cd "$BACKEND" && dotnet test HealthyPaws.Tests --nologo -v q "${extra[@]}"); }
run_python() {
  local py="$AI/.venv/bin/python"
  [ -x "$py" ] || { echo "Nema $py (kreiraj venv i pip install -r requirements-dev.txt)" >&2; return 1; }
  (cd "$AI" && "$py" -m pytest "${extra[@]}")
}
run_flutter() { (cd "$UI" && flutter test "${extra[@]}"); }

declare -A result
for t in $(printf '%s\n' "${targets[@]}" | awk '!s[$0]++'); do
  echo; echo "=============== $t ==============="
  if "run_$t"; then result[$t]=OK; else result[$t]=FAIL; fi
done

echo; echo "=============== SAŽETAK ==============="
rc=0
for t in "${!result[@]}"; do printf '%-8s %s\n' "$t" "${result[$t]}"; [ "${result[$t]}" = OK ] || rc=1; done
exit $rc
