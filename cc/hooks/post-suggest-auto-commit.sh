#!/bin/bash
# post-suggest-auto-commit.sh — Automatically commits Unity file changes
#
# Fires after every Claude response (PostSuggest hook).
# Checks git diff for meaningful Unity project changes and commits them
# with a descriptive message.
#
# Only fires for:
#   - Unity projects (has Assets/ and ProjectSettings/)
#   - When relevant files changed (.unity, .prefab, .asset, .cs, etc.)
#   - When a git repo exists
#
# Debounce: skips if less than 60 seconds since last auto-commit.

# ── Guard: Unity project only ───────────────────────────────────────────
[ -d "Assets" ] || exit 0
[ -d "ProjectSettings" ] || exit 0
[ -d ".git" ] || exit 0

# ── Guard: rule enabled? ────────────────────────────────────────────────
MODE_JSON=".claude/unity-mode.json"
if [ -f "$MODE_JSON" ]; then
  if [ "$(jq -r .enabled "$MODE_JSON" 2>/dev/null)" != "true" ]; then exit 0; fi
  if [ "$(jq -r '.rules."post-suggest-auto-commit"' "$MODE_JSON" 2>/dev/null)" != "true" ]; then exit 0; fi
fi

# ── Debounce file ───────────────────────────────────────────────────────
COMMIT_LOCK=".claude/.last-auto-commit"
NOW=$(date +%s)
if [ -f "$COMMIT_LOCK" ]; then
  LAST=$(cat "$COMMIT_LOCK")
  ELAPSED=$((NOW - LAST))
  [ "$ELAPSED" -lt 60 ] && exit 0  # minimum 60s between auto-commits
fi

# ── Check for meaningful changes ────────────────────────────────────────
CHANGES=$(git diff --name-only 2>/dev/null)
UNSTAGED=$(git diff --cached --name-only 2>/dev/null)
ALL_CHANGES=$(printf "%s\n%s" "$CHANGES" "$UNSTAGED" | sort -u)

# Interesting file patterns for a Unity project
HAS_ASSET_CHANGE=false
SCENES=""
PREFABS=""
SCRIPTS=""
ASSETS=""
CONFIGS=""

while IFS= read -r f; do
  [ -z "$f" ] && continue
  # Only consider tracked or addable files
  case "$f" in
    *.unity)
      HAS_ASSET_CHANGE=true
      s=$(basename "$f" .unity)
      SCENES="${SCENES}${SCENES:+,}$s"
      ;;
    *.prefab)
      HAS_ASSET_CHANGE=true
      p=$(basename "$f" .prefab)
      PREFABS="${PREFABS}${PREFABS:+,}$p"
      ;;
    *.cs|*.shader)
      HAS_ASSET_CHANGE=true
      c=$(basename "$f")
      SCRIPTS="${SCRIPTS}${SCRIPTS:+,}$c"
      ;;
    *.asset|*.mat|*.controller|*.anim|*.fbx|*.glb|*.png|*.jpg)
      HAS_ASSET_CHANGE=true
      a=$(basename "$f")
      ASSETS="${ASSETS}${ASSETS:+,}$a"
      ;;
    *.json|*.md|*.ps1|*.sh)
      CONFIGS="${CONFIGS}${CONFIGS:+,}$(basename "$f")"
      HAS_ASSET_CHANGE=true
      ;;
    .claude/*|CLAUDE.md)
      HAS_ASSET_CHANGE=true
      CONFIGS="${CONFIGS}${CONFIGS:+,}$(basename "$f")"
      ;;
  esac
done <<< "$ALL_CHANGES"

$HAS_ASSET_CHANGE || exit 0
# Skip if only .meta files changed (they're noise)
ONLY_META=true
while IFS= read -r f; do
  [ -z "$f" ] && continue
  case "$f" in *.meta) ;; *) ONLY_META=false; break ;; esac
done <<< "$ALL_CHANGES"
$ONLY_META && exit 0

# ── Build commit message ────────────────────────────────────────────────
MSG=""
[ -n "$SCENES" ]  && MSG="scene($SCENES)"
[ -n "$PREFABS" ] && MSG="${MSG}${MSG:+, }prefab($PREFABS)"
[ -n "$SCRIPTS" ] && MSG="${MSG}${MSG:+, }script($SCRIPTS)"
[ -n "$ASSETS" ]  && MSG="${MSG}${MSG:+, }asset($ASSETS)"
[ -n "$CONFIGS" ] && MSG="${MSG}${MSG:+, }config($CONFIGS)"
[ -z "$MSG" ]     && MSG="auto: unity changes"

MSG="auto: $MSG"

# ── Commit ──────────────────────────────────────────────────────────────
git add -A 2>/dev/null
git commit -m "$MSG" --no-verify 2>/dev/null
echo "$NOW" > "$COMMIT_LOCK"

exit 0
