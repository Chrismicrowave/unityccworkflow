#!/bin/bash
# pre-commit.sh — nudges designer when behavior scripts change without DESIGN.md update
# Install as .git/hooks/pre-commit in the Unity project repo
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."pre-commit-design-nudge"' .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

BEHAVIOR_CHANGED=$(git diff --cached --name-only 2>/dev/null | grep -E "Scripts/(Behavior|AI|NPC|Enemy)/")
DESIGN_CHANGED=$(git diff --cached --name-only 2>/dev/null | grep "Assets/Docs/DESIGN.md")

if [ -n "$BEHAVIOR_CHANGED" ] && [ -z "$DESIGN_CHANGED" ]; then
  echo "⚠️  Behavior scripts changed but DESIGN.md was not updated."
  echo "    Consider: does this change alter a unit behavior, game principle, or scene contract?"
  echo "    Files changed:"
  echo "$BEHAVIOR_CHANGED" | sed 's/^/      /'
  # Soft — does not block commit
fi
