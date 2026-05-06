#!/bin/bash
# session-start.sh — injects project context at session start
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."session-start-injection"' .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

echo "# Agent context — injected by session-start"
echo ""

# ProjectDigest
if [ -f "Library/AgentMirror/ProjectDigest.md" ]; then
  echo "## Project Digest"
  cat "Library/AgentMirror/ProjectDigest.md"
  echo ""
fi

# SceneMirror summary
if [ -f "Library/AgentMirror/SceneMirror.meta.json" ]; then
  echo "## SceneMirror status"
  jq -r '"Entities: \(.entityCount) | Scenes: \(.sceneCount) | Last updated: \(.emittedAt)"' Library/AgentMirror/SceneMirror.meta.json
  echo ""
fi

# CorrectionLedger
if [ -f "Library/AgentMirror/CorrectionLedger.md" ]; then
  echo "## Prior corrections (read carefully)"
  tail -30 "Library/AgentMirror/CorrectionLedger.md"
  echo ""
fi

# RefactorEvent — if recent (within 24h)
if [ -f "Library/AgentMirror/RefactorEvent.json" ]; then
  EMITTED_AT=$(jq -r '.ts // empty' Library/AgentMirror/RefactorEvent.json 2>/dev/null)
  if [ -n "$EMITTED_AT" ]; then
    EVENT_AGE=$(( $(date +%s) - $(date -d "$EMITTED_AT" +%s 2>/dev/null || echo 0) ))
    if [ "$EVENT_AGE" -lt 86400 ]; then
      echo "## ⚠️ Recent refactor event"
      cat "Library/AgentMirror/RefactorEvent.json"
      echo ""
    fi
  fi
fi

# Log to HookAudit
mkdir -p Library/AgentMirror
echo "{\"ts\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"hook\":\"session-start\",\"fired\":true}" >> Library/AgentMirror/HookAudit.jsonl
