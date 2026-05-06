Read `.claude/unity-mode.json`. Read last 3 entries of `.claude/unity-mode-history.jsonl` (if it exists). Report:
- Master enabled state (true/false)
- Rule states: list each rule name and its value, count how many are enabled vs total (e.g. "9/9 rules enabled")
- Last changed timestamp and who changed it
- Any rules currently disabled — list them by name

Format clearly so the user can see the full discipline state at a glance.
