#!/bin/bash
# post-compaction.sh — re-injects context after CC compaction
# Registered on UserPromptSubmit (every user message) but throttled:
# only re-injects if > 30 minutes since last injection.
#
# This ensures that after compaction destroys the injected context,
# the next user message triggers a fresh injection.
# Without compaction, the throttle prevents wasteful re-injection.
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."post-compaction-reinject"' .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

# Throttle: only re-inject if > 30 min since last session-start ran
THROTTLE_FILE="Library/AgentMirror/.last-session-inject"
NOW=$(date +%s)
if [ -f "$THROTTLE_FILE" ]; then
  LAST=$(cat "$THROTTLE_FILE" 2>/dev/null || echo 0)
  ELAPSED=$((NOW - LAST))
  [ "$ELAPSED" -lt 1800 ] && exit 0
fi

# Re-inject same content as session-start
bash "$(dirname "$0")/session-start.sh"
