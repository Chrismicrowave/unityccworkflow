#!/bin/bash
# pre-snapshot-nudge.sh — Reminds the AI to read UnitySnapshot before scene MCP reads.
# Soft warning only — does not block.
echo "HOOK: Consider reading EditorSnapshot.json or SceneTreeSnapshot.json instead of MCP."
echo "       jq query: jq -r '.entities[] | select(.path==\"<path>\")' Library/AgentMirror/EditorSnapshot.json"
exit 0
