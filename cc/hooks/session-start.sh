#!/bin/bash
# session-start.sh — injects project context at session start

# ── jq availability check ──────────────────────────────────────────
if ! command -v jq &> /dev/null && ! command -v jq.exe &> /dev/null; then
  echo "⚠️  jq is not installed. UCCPack hooks (Play Mode guard, EditorSnapshot diff,"
  echo "   pre-commit checks) will not work. Install: winget install jqlang.jq"
  echo ""
fi

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

# Pattern index — available knowledge files injected here so Claude always knows what exists
echo "## Available patterns (read on demand from .claude/teams/game-dev/*.md)"
echo ""
echo "  | File | Key patterns |"
echo "  |------|-------------|"
echo "  | \`animation.md\` | Spider-Verse frame-rate, Mixamo FBX import, clip end detection, RootT.y binding, per-state FPS override, FBX clip extraction"
echo "  | \`cinemachine.md\` | CM3 Follow/LookAt properties, priority takeover, MCP set_property limits"
echo "  | \`prefabs.md\` | Never unpack, non-destructive overrides, asset modification"
echo "  | \`shaders.md\` | Texel size (never _ScreenParams), Sobel kernel, noise wiggle, FPS quantization"
echo "  | \`systems.md\` | InputAction HTML-escape fix, Canvas↔world conversion, Awake/Start/OnEnable discipline, no name-based lookups, editor-vs-script values, new input system, no ?? on Unity Objs, script-active-state"
echo "  | \`tools.md\` | Editor scripts, custom inspectors, build pipelines, wire refs via MCP"
echo "  | \`blind-spots.md\` | Known AI limitations — read before debugging"
echo "  | \`monobehaviour-lifecycle.md\` | Execution order, message timing, coroutine edge cases"
echo ""

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

# Write throttle timestamp (for post-compaction.sh)
echo "$(date +%s)" > "Library/AgentMirror/.last-session-inject"

# Log to HookAudit
mkdir -p Library/AgentMirror
echo "{\"ts\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"hook\":\"session-start\",\"fired\":true}" >> Library/AgentMirror/HookAudit.jsonl
