#!/bin/bash
# pre-set-property.sh — guards against play-mode edits and name-only addressing
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

# Guard A: play mode refusal
if [ "$(jq -r '.rules."pre-set-property-playmode-refusal"' .claude/unity-mode.json 2>/dev/null)" == "true" ]; then
  echo "HOOK: Before set_property, verify EditorApplication.isPlaying == false. If game is running, stop it first or call /unity-rule-off pre-set-property-playmode-refusal to override."
fi

# Guard B: name-based addressing refusal
if [ "$(jq -r '.rules."pre-set-property-name-refusal"' .claude/unity-mode.json 2>/dev/null)" == "true" ]; then
  echo "HOOK: Resolve target by stableId before set_property. Check Library/AgentMirror/SceneMirror.json for the entity's stableId. Use execute_script to call resolve_entity if unsure."
fi

mkdir -p Library/AgentMirror
echo "{\"ts\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"hook\":\"pre-set-property\",\"fired\":true}" >> Library/AgentMirror/HookAudit.jsonl
