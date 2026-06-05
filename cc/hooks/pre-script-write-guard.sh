#!/bin/bash
# pre-script-write-guard.sh — Block script edits during Unity Play Mode
#
# Blocks Edit/Write tool calls on .cs files when Unity is in Play Mode.
# Editing scripts during Play Mode is bad practice — changes compile into
# the running game state and can cause confusing behavior.
#
# Override: /unity-rule-off pre-script-write-guard

MODE_JSON=".claude/unity-mode.json"
[ -f "$MODE_JSON" ] || exit 0
[ "$(jq -r .enabled "$MODE_JSON" 2>/dev/null)" = "true" ] || exit 0
[ "$(jq -r '.rules."pre-script-write-guard"' "$MODE_JSON" 2>/dev/null)" = "true" ] || exit 0

# ── Read PlayModeState ───────────────────────────────────────────────
PM_FILE="Library/AgentMirror/PlayModeState.json"
IS_PLAYING=false
if [ -f "$PM_FILE" ]; then
  IS_PLAYING=$(jq -r '.isPlaying // false' "$PM_FILE" 2>/dev/null)
fi

[ "$IS_PLAYING" = "true" ] || exit 0

# ── Read stdin for tool details ─────────────────────────────────────
ARGS=$(cat /dev/stdin 2>/dev/null || echo "")
TOOL=$(echo "$ARGS" | jq -r '.tool // ""' 2>/dev/null)

# Only block Edit and Write tools
[ "$TOOL" = "Edit" ] || [ "$TOOL" = "Write" ] || exit 0

# Only block .cs files
FILE_PATH=$(echo "$ARGS" | jq -r '.args.file_path // .args.path // ""' 2>/dev/null)
case "$FILE_PATH" in
  *.cs) ;;  # continue to block
  *) exit 0 ;;  # not a .cs file, allow
esac

# ── Block ────────────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════════════"
echo "🛑 BLOCKED: Cannot edit scripts while Unity is in Play Mode!"
echo "   Tool: $TOOL"
echo "   File: $FILE_PATH"
echo ""
echo "   Script edits during Play Mode compile into the running game"
echo "   and can cause confusing behavior."
echo ""
echo "   Options:"
echo "     1. Stop the game first (coplay Stop Game)"
echo "     2. Override: /unity-rule-off pre-script-write-guard"
echo "══════════════════════════════════════════════════════════════"
echo ""
exit 1
