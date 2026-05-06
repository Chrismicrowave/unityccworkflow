#!/bin/bash
# pre-list-hierarchy.sh — rate-limits list_game_objects_in_hierarchy calls when mirror is fresh
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."pre-list-hierarchy-rate-limit"' .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

# Track call count per session using PID-scoped temp file
COUNT_FILE="/tmp/unity_list_hierarchy_count_${PPID}"
CALL_COUNT=0
if [ -f "$COUNT_FILE" ]; then CALL_COUNT=$(cat "$COUNT_FILE"); fi
CALL_COUNT=$((CALL_COUNT + 1))
echo "$CALL_COUNT" > "$COUNT_FILE"

# Check mirror freshness
MIRROR_META="Library/AgentMirror/SceneMirror.meta.json"
if [ -f "$MIRROR_META" ]; then
  EMITTED_AT=$(jq -r '.emittedAt // empty' "$MIRROR_META" 2>/dev/null)
  if [ -n "$EMITTED_AT" ]; then
    MIRROR_AGE=$(( $(date +%s) - $(date -d "$EMITTED_AT" +%s 2>/dev/null || echo 0) ))
    # Block if mirror < 5 min old and this is the 3rd+ call
    if [ "$MIRROR_AGE" -lt 300 ] && [ "$CALL_COUNT" -gt 2 ]; then
      echo "BLOCKED: SceneMirror.json is fresh (${MIRROR_AGE}s old, ${CALL_COUNT} calls this session). Read Library/AgentMirror/SceneMirror.json instead of calling list_game_objects_in_hierarchy." >&2
      exit 1
    fi
  fi
fi

mkdir -p Library/AgentMirror
echo "{\"ts\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"hook\":\"pre-list-hierarchy\",\"callCount\":$CALL_COUNT}" >> Library/AgentMirror/HookAudit.jsonl
