#!/bin/bash
# post-edit-script.sh — fires after any .cs file edit; signals compile-once discipline
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."post-edit-script-stop"' .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

mkdir -p Library/AgentMirror
echo "{\"ts\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"hook\":\"post-edit-script\",\"fired\":true}" >> Library/AgentMirror/HookAudit.jsonl

echo "HOOK: .cs file edited. Run check_compile_errors EXACTLY ONCE. If errors: STOP and surface to user. Do NOT loop. Do NOT attempt auto-fix without user confirmation."
