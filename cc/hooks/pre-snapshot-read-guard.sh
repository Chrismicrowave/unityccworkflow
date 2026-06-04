#!/bin/bash
# pre-snapshot-read-guard.sh — Blocks MCP scene reads that have snapshot equivalents.
# Use the snapshot files (EditorSnapshot, SceneTreeSnapshot, FolderSnapshot) instead.
#
# Blocked tools and their snapshot replacement:
#   get_game_object_info           → EditorSnapshot.json (jq query)
#   list_game_objects_in_hierarchy → SceneTreeSnapshot.json (all GOs by path)
#   list_all_prefabs               → EditorSnapshot.json (entities with kind=prefab-asset)
#   list_files                     → FolderSnapshot.json (file tree under Assets/)
#   Glob/search_files              → FolderSnapshot.json (search by extension/name)
#
# Override: /unity-rule-off pre-snapshot-read-guard

# Map blocked tools to their snapshot replacement
case "$MCP_TOOL" in
  mcp__coplay-mcp__get_game_object_info)
    REPLACEMENT="EditorSnapshot.json"
    JQ_EXAMPLE="jq -r '.entities[] | select(.path==\"<path>\")' Library/AgentMirror/EditorSnapshot.json"
    ;;
  mcp__coplay-mcp__list_game_objects_in_hierarchy)
    REPLACEMENT="SceneTreeSnapshot.json"
    JQ_EXAMPLE="jq -r '.[] | \"\\(.path) (\\(.name)) — \\(.components | length) components\"' Library/AgentMirror/SceneTreeSnapshot.json"
    ;;
  mcp__coplay-mcp__list_all_prefabs*)
    REPLACEMENT="EditorSnapshot.json (prefab-asset entities)"
    JQ_EXAMPLE="jq -r '.entities[] | select(.kind==\"prefab-asset\") | .path' Library/AgentMirror/EditorSnapshot.json"
    ;;
  mcp__coplay-mcp__list_files|Glob)
    REPLACEMENT="FolderSnapshot.json"
    JQ_EXAMPLE="jq -r '.files[] | select(.ext==\".cs\") | .path' Library/AgentMirror/FolderSnapshot.json"
    ;;
  *)
    exit 0  # not a blocked tool
    ;;
esac

echo ""
echo "══════════════════════════════════════════════════════════════"
echo "🛑 Use $REPLACEMENT instead of $MCP_TOOL"
echo ""
echo "   The snapshot has the same data with no MCP round-trip."
echo ""
echo "   $JQ_EXAMPLE"
echo ""
echo "   Override: /unity-rule-off pre-snapshot-read-guard"
echo "══════════════════════════════════════════════════════════════"
echo ""
exit 1
