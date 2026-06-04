#!/bin/bash
# pre-tool-use-write-guard.sh — Hard-block writes during Unity Play Mode
#
# Replaces the older pre-set-property.sh (which only printed a soft warning).
# Reads Library/AgentMirror/PlayModeState.json (written by PlayModeTracker.cs)
# and exits with code 1 if Unity is in Play Mode AND the tool is a write tool.
#
# Also preserves the stableId name-resolution reminder for set_property.
#
# Read operations (state queries, screenshots, logs) are ALLOWED during play mode.
# Write operations (property edits, transforms, component changes) are BLOCKED.
#
# Override: /unity-rule-off pre-tool-use-write-guard

MODE_JSON=".claude/unity-mode.json"
if [ ! -f "$MODE_JSON" ]; then exit 0; fi
if [ "$(jq -r .enabled "$MODE_JSON" 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."pre-tool-use-write-guard"' "$MODE_JSON" 2>/dev/null)" != "true" ]; then exit 0; fi

# ── Read PlayModeState ───────────────────────────────────────────────────
PM_FILE="Library/AgentMirror/PlayModeState.json"
if [ ! -f "$PM_FILE" ]; then
  # PlayModeTracker hasn't emitted yet — allow and warn
  echo "HOOK: PlayModeState.json not found (Unity may not have compiled yet). Skipping play-mode check."
  exit 0
fi

IS_PLAYING=$(jq -r '.isPlaying // false' "$PM_FILE" 2>/dev/null)
if [ "$IS_PLAYING" != "true" ]; then exit 0; fi

# ── We are in Play Mode — determine if this tool is read or write ────────
# Write tools (blocked):
WRITE_TOOLS="mcp__coplay-mcp__set_property
mcp__coplay-mcp__set_transform
mcp__coplay-mcp__set_rect_transform
mcp__coplay-mcp__add_component
mcp__coplay-mcp__remove_component
mcp__coplay-mcp__execute_script
mcp__coplay-mcp__create_game_object
mcp__coplay-mcp__delete_game_object
mcp__coplay-mcp__duplicate_game_object
mcp__coplay-mcp__parent_game_object
mcp__coplay-mcp__rename_game_object
mcp__coplay-mcp__set_layer
mcp__coplay-mcp__set_tag
mcp__coplay-mcp__assign_material
mcp__coplay-mcp__set_ui_text
mcp__coplay-mcp__set_ui_layout
mcp__coplay-mcp__assign_material_to_fbx
mcp__coplay-mcp__remove_persistent_listener
mcp__coplay-mcp__add_persistent_listener
mcp__coplay-mcp__create_animation_clip
mcp__coplay-mcp__set_animation_curves
mcp__coplay-mcp__set_sprite_animation_curve
mcp__coplay-mcp__set_animation_clip_settings
mcp__coplay-mcp__modify_animator_controller"

for WT in $WRITE_TOOLS; do
  if [ "$MCP_TOOL" = "$WT" ]; then
    echo ""
    echo "══════════════════════════════════════════════════════════════"
    echo "🛑 BLOCKED: Unity is in Play Mode ($(jq -r '.isPlaying' "$PM_FILE"))"
    echo "   Tool: $MCP_TOOL"
    echo ""
    echo "   Cannot edit the scene while the game is running."
    echo "   Changes made during Play Mode are lost on exit."
    echo ""
    echo "   Options:"
    echo "     1. Stop the game first (coplay Stop Game)"
    echo "     2. Ask user to stop the game manually"
    echo "     3. Override: /unity-rule-off pre-tool-use-write-guard"
    echo "══════════════════════════════════════════════════════════════"
    echo ""
    exit 1
  fi
done

# ── Read-only tools allowed in play mode ─────────────────────────────────
# If we reach here, the tool is a read operation or not in our write list.
# Allowed: get_*, list_*, check_*, capture_*, search_*, read_*, import_*, install_*

# ── StableId resolution reminder (only for set_property, not play-mode) ──
if [ "$MCP_TOOL" = "mcp__coplay-mcp__set_property" ]; then
  if [ "$(jq -r '.rules."pre-set-property-name-refusal"' "$MODE_JSON" 2>/dev/null)" == "true" ]; then
    echo "HOOK: Resolve target by stableId before set_property."
    echo "       Check Library/AgentMirror/SceneMirror.json for the entity's stableId."
  fi
fi

exit 0
