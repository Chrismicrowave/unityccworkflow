#!/usr/bin/env bash
# pre-write-plan-format.sh
# When writing a plan file, reads the CLAUDE.md plan format rules
# (both global and project) into the tool output so Claude sees them.
#
# Trigger: pre-write on Assets/Docs/Plans/*.md or *task--*.md

set -euo pipefail

path="$1"

# Only fire for plan files
case "$path" in
  *Plans/*.md|*plans/*.md|*task--*.md) ;;
  *) exit 0 ;;
esac

cat <<'RULES'

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 PLAN FORMAT — from CLAUDE.md rules
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Every plan MUST:
1. Save as: task--YYYY-MM-DD--HHMM.md
2. Include self-cleanup: "**Cleanup:** 🧹 Delete this file once ..."
3. Include an execution step asking user to confirm deletion
4. Include "delete after use" in its own checklist
5. Use - [ ] checkboxes per step
6. Each step has verification criteria

Project CLAUDE.md also says:
  - Temp plans → Assets/Docs/Plans/
  - Include date + context (what triggered it, what it supersedes)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

RULES
