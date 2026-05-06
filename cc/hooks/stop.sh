#!/bin/bash
# stop.sh — session-end digest and CorrectionLedger append
if [ "$(jq -r .enabled .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi
if [ "$(jq -r '.rules."stop-session-digest"' .claude/unity-mode.json 2>/dev/null)" != "true" ]; then exit 0; fi

TS=$(date -u +%Y-%m-%dT%H:%M:%SZ)
DATE=$(date -u +%Y-%m-%d)

MODIFIED_COUNT=0
if [ -f "Library/AgentMirror/SessionLedger.jsonl" ]; then
  MODIFIED_COUNT=$(tail -100 Library/AgentMirror/SessionLedger.jsonl | jq -s 'length' 2>/dev/null || echo 0)
fi

echo ""
echo "Session digest (${TS}):"
echo "  Entities modified this session: ${MODIFIED_COUNT}"
echo "  See Library/AgentMirror/SessionLedger.jsonl for details."

# Ensure CorrectionLedger exists
CORRECTIONS_FILE="Library/AgentMirror/CorrectionLedger.md"
mkdir -p Library/AgentMirror
if [ ! -f "$CORRECTIONS_FILE" ]; then
  {
    echo "# Correction Ledger"
    echo "Auto-appended corrections from user sessions. Injected at session start."
    echo ""
  } > "$CORRECTIONS_FILE"
fi

{
  echo "## Session ${DATE} ${TS}"
  echo "(Review session transcript and append corrections manually, or implement keyword scan via CC session output)"
  echo ""
} >> "$CORRECTIONS_FILE"

echo "{\"ts\":\"${TS}\",\"hook\":\"stop\",\"modifiedCount\":${MODIFIED_COUNT}}" >> Library/AgentMirror/HookAudit.jsonl
