#!/bin/bash
# pre-tool-use-write-guard.sh — Save scene + hard-block writes during Unity Play Mode
#
# 1. Saves the scene before any write operation (via AgentMirror SceneSaver signal).
# 2. Blocks writes if Unity is in Play Mode.
#
# Override: /unity-rule-off pre-tool-use-write-guard

MODE_JSON=".claude/unity-mode.json"
[ -f "$MODE_JSON" ] || exit 0
[ "$(jq -r .enabled "$MODE_JSON" 2>/dev/null)" = "true" ] || exit 0
[ "$(jq -r '.rules."pre-tool-use-write-guard"' "$MODE_JSON" 2>/dev/null)" = "true" ] || exit 0

# ── Write tools list ─────────────────────────────────────────────────
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

IS_WRITE=0
for WT in $WRITE_TOOLS; do
  [ "$MCP_TOOL" = "$WT" ] && IS_WRITE=1
done

# ── Read PlayModeState ───────────────────────────────────────────────
PM_FILE="Library/AgentMirror/PlayModeState.json"
IS_PLAYING=false
if [ -f "$PM_FILE" ]; then
  IS_PLAYING=$(jq -r '.isPlaying // false' "$PM_FILE" 2>/dev/null)
fi

if [ "$IS_PLAYING" = "true" ] && [ "$IS_WRITE" = "1" ]; then
  echo ""
  echo "══════════════════════════════════════════════════════════════"
  echo "🛑 BLOCKED: Unity is in Play Mode!"
  echo "   Tool: $MCP_TOOL"
  echo ""
  echo "   Cannot edit the scene while the game is running."
  echo "   Changes made during Play Mode are discarded on exit."
  echo ""
  echo "   Options:"
  echo "     1. Stop the game first (coplay Stop Game)"
  echo "     2. Ask user to stop the game manually"
  echo "     3. Override: /unity-rule-off pre-tool-use-write-guard"
  echo "══════════════════════════════════════════════════════════════"
  echo ""
  exit 1
fi

# ── Save scene before write (if not in Play Mode) ───────────────────
if [ "$IS_WRITE" = "1" ] && [ "$IS_PLAYING" != "true" ]; then
  SIGNAL="Library/AgentMirror/.save-scene-signal"
  DONE="Library/AgentMirror/.save-scene-done"

  # Clean up any stale files
  rm -f "$DONE"

  # Write signal → SceneSaver picks it up, saves scene, writes done file
  mkdir -p "$(dirname "$SIGNAL")"
  echo "save" > "$SIGNAL"

  # Wait for done file (up to 10 seconds)
  for i in $(seq 1 20); do
    if [ -f "$DONE" ]; then
      break
    fi
    sleep 0.5
  done

  # Clean up
  rm -f "$SIGNAL" "$DONE"
fi

# ── StableId reminder (only for set_property) ────────────────────────
if [ "$MCP_TOOL" = "mcp__coplay-mcp__set_property" ]; then
  if [ "$(jq -r '.rules."pre-set-property-name-refusal"' "$MODE_JSON" 2>/dev/null)" = "true" ]; then
    echo "HOOK: Resolve target by stableId before set_property."
    echo "       Check Library/AgentMirror/SceneMirror.json for the entity's stableId."
  fi
fi

exit 0
