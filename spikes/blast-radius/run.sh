#!/usr/bin/env bash
# Predicts the blast radius of a change (naive and improved), applies the real change, and compares with build and test results.
# Usage: run.sh <count> [seed]        random members
#        run.sh --targets <file>      one "Type Member" per line
# Run from spikes/blast-radius after "dotnet build".
set -u
HERE=$(cd "$(dirname "$0")" && pwd)
WORK=${WORK:-$HERE/.work}
radius() { dotnet run --no-build --project "$HERE/tools/Radius" -- "$@" | tr -d '\r'; }
field() { echo "$1" | sed -n "s/^$2: //p"; }

rm -rf "$HERE/.graph" && mkdir -p "$HERE/.graph" && for f in "$HERE"/tests/*/bin/Debug/net10.0/Foo.*.dll; do cp -f "$f" "$HERE/.graph/"; done
if [ "$1" = --targets ]; then
  mapfile -t PICKS < <(tr -d '\r' < "$2")
else
  SEED=${2:-$RANDOM}; echo "seed=$SEED"
  mapfile -t PICKS < <(radius list "$HERE" | shuf -n "$1" --random-source=<(yes "$SEED"))
fi

for pick in "${PICKS[@]}"; do
  read -r TYPE MEMBER <<< "$pick"
  NAIVE=$(radius predict "$HERE/.graph" "$HERE" "$TYPE" "$MEMBER" --naive)
  FULL=$(radius predict "$HERE/.graph" "$HERE" "$TYPE" "$MEMBER")
  for KIND in rename body; do
    rm -rf "$WORK" && mkdir -p "$WORK"
    cp -r "$HERE/src" "$HERE/tests" "$HERE/BlastRadius.slnx" "$WORK/"
    find "$WORK" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
    radius mutate "$WORK/src" "$TYPE" "$MEMBER" "$KIND"
    echo "=== $TYPE.$MEMBER [$KIND]   $(echo "$FULL" | head -1 | sed -E 's/^target: [^ ]+ +//; s/ +mode=.*//')"
    if [ "$KIND" = rename ]; then
      ACTUAL=$(dotnet build "$WORK/BlastRadius.slnx" 2>&1 | grep -oE '[A-Za-z]+\.cs\([0-9]+,[0-9]+\): error' | sed -E 's/\.cs.*//' | sort -u | paste -sd, - | sed 's/,/, /g')
      echo "  naive:    $(field "$NAIVE" compile)"
      echo "  improved: $(field "$FULL" compile)"
      echo "  actual:   ${ACTUAL:-<builds>}"
    else
      ACTUAL=$(dotnet test "$WORK/BlastRadius.slnx" 2>&1 | grep -oE '[A-Za-z]+ServiceTests\.[A-Za-z_]+ \[FAIL\]|Failed Foo\.[A-Za-z.]+Tests\.[A-Za-z_]+' | sed -E 's/^Failed //; s/ \[FAIL\]//; s/^Foo\.[A-Za-z]+\.Tests\.//' | sort -u | paste -sd, - | sed 's/,/, /g')
      echo "  naive:    $(field "$NAIVE" behavior-tests)"
      echo "  improved: $(field "$FULL" behavior-tests)"
      echo "  actual:   ${ACTUAL:-<no test fails>}"
    fi
  done
done
