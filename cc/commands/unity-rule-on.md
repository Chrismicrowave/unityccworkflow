Parse `$args` as `<rule-name>`. Set `.rules.<rule-name>` to `true` in `.claude/unity-mode.json`. Update `lastChanged` and `changedBy` fields. Append to `.claude/unity-mode-history.jsonl`: `{"ts": "<iso-timestamp>", "action": "rule-on", "rule": "<rule-name>", "session": "<session-id>"}`.

Confirm: "Rule `<rule-name>` enabled."
