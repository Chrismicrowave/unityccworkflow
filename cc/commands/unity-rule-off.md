Parse `$args` as `<rule-name> [reason]`. Valid rule names: `session-start-injection`, `post-edit-script-stop`, `pre-list-hierarchy-rate-limit`, `pre-set-property-name-refusal`, `pre-set-property-playmode-refusal`, `user-prompt-submit-injection`, `stop-session-digest`, `post-compaction-reinject`, `pre-commit-design-nudge`.

Set `.rules.<rule-name>` to `false` in `.claude/unity-mode.json`. Update `lastChanged` and `changedBy` fields. Append to `.claude/unity-mode-history.jsonl`: `{"ts": "<iso-timestamp>", "action": "rule-off", "rule": "<rule-name>", "reason": "<reason or empty>", "session": "<session-id>"}`.

Confirm: "Rule `<rule-name>` disabled. Use `/unity-rule-on <rule-name>` to re-enable."
