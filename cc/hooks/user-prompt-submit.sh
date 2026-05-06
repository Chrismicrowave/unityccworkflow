#!/bin/bash
# user-prompt-submit.sh — injects SceneMirror matches and flags intent-change signals
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."user-prompt-submit-injection"' .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

USER_MSG="${CLAUDE_USER_PROMPT:-}"

# Intent-change detection
if echo "$USER_MSG" | grep -qiE "(actually|from now on|i want .* to|let.?s change|change the|should now|update .* to)"; then
  echo "[INTENT-CHANGE-CANDIDATE] User message may signal intent change. Check DESIGN.md for affected section. Propose AMEND before coding."
fi

# GameObject name injection — match entity names from SceneMirror against user message
if [ -f "Library/AgentMirror/SceneMirror.json" ]; then
  MATCHES=$(jq -r '.[].name' Library/AgentMirror/SceneMirror.json 2>/dev/null | while IFS= read -r name; do
    if [ -n "$name" ] && echo "$USER_MSG" | grep -qiF "$name"; then
      jq -r --arg n "$name" \
        'to_entries[] | select(.value.name == $n) | "  StableId: \(.key) | Name: \(.value.name) | Path: \(.value.path) | Components: \(.value.components | join(", "))"' \
        Library/AgentMirror/SceneMirror.json 2>/dev/null
    fi
  done)
  if [ -n "$MATCHES" ]; then
    echo "SceneMirror matches for entities mentioned in prompt:"
    echo "$MATCHES"
  fi
fi

mkdir -p Library/AgentMirror
echo "{\"ts\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"hook\":\"user-prompt-submit\",\"fired\":true}" >> Library/AgentMirror/HookAudit.jsonl
