#!/bin/bash
# pre-git-commit-guard.sh — Blocks Bash git commands during Unity Play Mode.
# Part of UCCPack (Unity Co-op Creation Kit).
#
# This hook runs before EVERY Bash tool call but only blocks on git commit/push.
# It also checks whether the active scene was saved.

PM_FILE="Library/AgentMirror/PlayModeState.json"
MIRROR_FILE="Library/AgentMirror/SceneMirror.meta.json"

# ── Check Play Mode ─────────────────────────────────────────────────
if [ -f "$PM_FILE" ]; then
  IS_PLAYING=$(jq -r '.isPlaying // false' "$PM_FILE" 2>/dev/null)
  if [ "$IS_PLAYING" == "true" ]; then
    echo ""
    echo "══════════════════════════════════════════════════════════════"
    echo "🛑 BLOCKED: Unity is in Play Mode!"
    echo ""
    echo "   Stop the game in Unity Editor before using git commands."
    echo "══════════════════════════════════════════════════════════════"
    echo ""
    exit 1
  fi
fi

# ── Reminder to save scene (non-blocking) ───────────────────────────
# This only shows when there's a scene-mirror timestamp to compare
if [ -f "$MIRROR_FILE" ]; then
  EMITTED_AT=$(jq -r '.emittedAt // empty' "$MIRROR_FILE" 2>/dev/null)
  if [ -n "$EMITTED_AT" ]; then
    MIRROR_TS=$(date -d "$EMITTED_AT" +%s 2>/dev/null || echo 0)
    NOW_TS=$(date +%s)
    AGE=$(( NOW_TS - MIRROR_TS ))
    if [ "$AGE" -gt 120 ]; then
      echo "NOTE: Scene mirror is ${AGE}s old. Make sure the scene was saved before committing."
    fi
  fi
fi

exit 0
