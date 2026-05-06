#!/bin/bash
# init.sh — Initialize unityccworkflow for a Unity project (Mac/Linux)
# Usage: bash init.sh /path/to/unity/project

set -e

PROJECT_PATH="$1"

if [ -z "$PROJECT_PATH" ]; then
  echo "Usage: bash init.sh <unity-project-root>"
  exit 1
fi

if [ ! -d "$PROJECT_PATH/Assets" ] || [ ! -d "$PROJECT_PATH/ProjectSettings" ]; then
  echo "Error: Not a Unity project root: $PROJECT_PATH (missing Assets/ or ProjectSettings/)"
  exit 1
fi

PKG="$(cd "$(dirname "$0")" && pwd)"

echo "Initializing unityccworkflow for: $PROJECT_PATH"
echo ""

# 1. Unity C# files
echo "1. Copying Unity C# files..."
mkdir -p "$PROJECT_PATH/Assets/Editor/AgentMirror"
mkdir -p "$PROJECT_PATH/Assets/Scripts/Core"
cp -f "$PKG/unity/Editor/AgentMirror/"*.cs "$PROJECT_PATH/Assets/Editor/AgentMirror/"
cp -f "$PKG/unity/Runtime/StableId.cs" "$PROJECT_PATH/Assets/Scripts/Core/StableId.cs"
echo "   ✓ Editor/AgentMirror/*.cs, Scripts/Core/StableId.cs"

# 2. Hook scripts
echo "2. Copying hook scripts..."
mkdir -p "$PROJECT_PATH/.claude/hooks"
cp -f "$PKG/cc/hooks/"*.sh "$PROJECT_PATH/.claude/hooks/"
chmod +x "$PROJECT_PATH/.claude/hooks/"*.sh
echo "   ✓ .claude/hooks/*.sh"

# 3. Skill
echo "3. Copying discipline skill..."
mkdir -p "$PROJECT_PATH/.claude/skills/unity-mcp-discipline"
cp -f "$PKG/cc/skills/unity-mcp-discipline/skill.md" "$PROJECT_PATH/.claude/skills/unity-mcp-discipline/skill.md"
echo "   ✓ .claude/skills/unity-mcp-discipline/skill.md"

# 4. Slash commands
echo "4. Copying slash commands..."
mkdir -p "$PROJECT_PATH/.claude/commands"
cp -f "$PKG/cc/commands/"*.md "$PROJECT_PATH/.claude/commands/"
echo "   ✓ .claude/commands/unity-*.md, unityccworkflow-init.md"

# 5. Toggle config (preserve if already present)
if [ -f "$PROJECT_PATH/.claude/unity-mode.json" ]; then
  echo "5. unity-mode.json already exists — skipping (project-owned)"
else
  mkdir -p "$PROJECT_PATH/.claude"
  cp -f "$PKG/cc/unity-mode.json" "$PROJECT_PATH/.claude/unity-mode.json"
  echo "   ✓ .claude/unity-mode.json"
fi

# 6. settings.json
if [ -f "$PROJECT_PATH/.claude/settings.json" ]; then
  echo "6. settings.json already exists — manually merge hooks from: $PKG/cc/settings.json.template"
else
  mkdir -p "$PROJECT_PATH/.claude"
  cp -f "$PKG/cc/settings.json.template" "$PROJECT_PATH/.claude/settings.json"
  echo "   ✓ .claude/settings.json"
fi

# 7. Assets/Docs/DESIGN.md (preserve if already present)
mkdir -p "$PROJECT_PATH/Assets/Docs"
if [ -f "$PROJECT_PATH/Assets/Docs/DESIGN.md" ]; then
  echo "7. Assets/Docs/DESIGN.md already exists — skipping (human-authored)"
else
  cp -f "$PKG/templates/DESIGN.md.template" "$PROJECT_PATH/Assets/Docs/DESIGN.md"
  echo "   ✓ Assets/Docs/DESIGN.md (from template)"
fi

echo ""
echo "=========================================="
echo "unityccworkflow initialized successfully."
echo "=========================================="
echo ""
echo "Next steps:"
echo "  1. Open Unity — AgentMirror emitters run on compile"
echo "  2. Tools → AgentMirror → Add StableId to selection (recursive)"
echo "  3. Fill in Assets/Docs/DESIGN.md"
echo "  4. Open CC in project root → /unity-on"
echo "  5. /unity-status → should show 🛡️ 9/9"
