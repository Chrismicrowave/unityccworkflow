#!/bin/bash
# post-compaction.sh — re-injects context after CC compaction
# Register as PostCompaction hook if available; otherwise wire to UserPromptSubmit as a guard.
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."post-compaction-reinject"' .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

# Re-inject same content as session-start
bash "$(dirname "$0")/session-start.sh"
