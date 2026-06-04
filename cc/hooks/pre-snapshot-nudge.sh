#!/bin/bash
# pre-snapshot-nudge.sh — Blocks get_game_object_info MCP calls.
# Read EditorSnapshot.json instead: it has every GO, component, field value.
#
#   jq -r '.entities[] | select(.path=="<path>")' Library/AgentMirror/EditorSnapshot.json
#   jq -r '.entities[] | select(.path=="<path>") | .components[] | select(.type=="<Component>") | .fields' Library/AgentMirror/EditorSnapshot.json
#
# Override: /unity-rule-off pre-snapshot-nudge
echo ""
echo "══════════════════════════════════════════════════════════════"
echo "🛑 Use EditorSnapshot.json instead of get_game_object_info"
echo ""
echo "   The snapshot has every GameObject, component, and field."
echo "   It's faster (no MCP round-trip) and more complete."
echo ""
echo "   jq -r '.entities[] | select(.path==\"<path>\") |"
echo "        .components[] | select(.type==\"<Component>\") |"
echo "        .fields' Library/AgentMirror/EditorSnapshot.json"
echo ""
echo "   Override: /unity-rule-off pre-snapshot-nudge"
echo "══════════════════════════════════════════════════════════════"
echo ""
exit 1
