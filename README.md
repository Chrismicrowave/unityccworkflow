# unityccworkflow v0.1

A solution pack that adds a determinism layer between Claude Code and Unity MCP. Addresses 20 categories of CC+Unity pain identified across 26 sessions.

## What it does

- **AgentMirror** — Unity Editor module that auto-emits JSON artifacts to `Library/AgentMirror/` on every scene/prefab/script change. CC reads these instead of making live MCP calls.
- **StableId** — GUID-per-GameObject that survives rename and reparent. Ends name-based addressing errors.
- **CC hooks** — 8 bash scripts (session-start, stop, user-prompt-submit, pre-list-hierarchy, pre-set-property, post-edit-script, post-compaction, pre-commit) that enforce discipline and inject context automatically.
- **unity-mcp-discipline skill** — 9 in-context rules covering compile discipline, orientation, identity, CHALLENGE/AMEND flow, play-mode guard, and Animator debugging.
- **Master toggle** — `unity-mode.json` with per-rule enable/disable. `/unity-on`, `/unity-off`, `/unity-rule-off <rule>` slash commands.
- **DESIGN.md contract** — Template for human-authored intent. CHALLENGE fires when a `locked` or `settled` section would be violated. AMEND fires on intent-change signals.

## Quick start

```powershell
# Windows
.\init.ps1 -ProjectPath "D:\path\to\your\unity\project"
```

```bash
# Mac/Linux
bash init.sh /path/to/your/unity/project
```

Then follow the 5-step checklist printed by the init script.

## Package structure

```
unityccworkflow/
├── unity/
│   ├── Editor/AgentMirror/
│   │   ├── AgentMirrorConfig.cs        — shared paths/constants
│   │   ├── StableIdBootstrap.cs        — menu item: add StableId recursively
│   │   ├── SceneMirrorEmitter.cs       — hierarchy → SceneMirror.json
│   │   ├── AsmdefGraphEmitter.cs       — assemblies → AsmdefGraph.json
│   │   ├── AnimatorDumpEmitter.cs      — animators → AnimatorDump.json
│   │   ├── UndoGroupWrapper.cs         — named undo groups for agent edits
│   │   ├── ProjectDigestEmitter.cs     — git log + session tail → ProjectDigest.md
│   │   ├── RefactorEventEmitter.cs     — mass-rename detector → RefactorEvent.json
│   │   ├── PrefabGraphEmitter.cs       — prefab instances + variants → PrefabGraph.json
│   │   └── InspectorRefsEmitter.cs     — UnityEvent wiring → InspectorRefs.json
│   └── Runtime/
│       └── StableId.cs                 — GUID component, survives rename/reparent
├── cc/
│   ├── hooks/
│   │   ├── session-start.sh            — inject ProjectDigest + SceneMirror + CorrectionLedger
│   │   ├── stop.sh                     — session digest + CorrectionLedger append
│   │   ├── user-prompt-submit.sh       — intent-change detection + SceneMirror name injection
│   │   ├── post-edit-script.sh         — compile-once signal after .cs edit
│   │   ├── pre-list-hierarchy.sh       — rate-limit MCP hierarchy calls when mirror is fresh
│   │   ├── pre-set-property.sh         — playmode guard + stableId reminder
│   │   ├── post-compaction.sh          — re-inject context after CC compaction
│   │   └── pre-commit.sh              — nudge when behavior scripts change without DESIGN.md
│   ├── skills/unity-mcp-discipline/
│   │   └── skill.md                   — 9 in-context rules + CHALLENGE/AMEND flows
│   ├── commands/
│   │   ├── unity-on.md
│   │   ├── unity-off.md
│   │   ├── unity-status.md
│   │   ├── unity-rule-off.md
│   │   ├── unity-rule-on.md
│   │   ├── unity-rule-status.md
│   │   ├── unity-rule-reset.md
│   │   └── unityccworkflow-init.md
│   ├── unity-mode.json                 — master toggle (all enabled by default)
│   └── settings.json.template          — CC hooks registration
├── templates/
│   └── DESIGN.md.template              — blank design doc with stability tier stubs
├── init.ps1                            — Windows init script
├── init.sh                             — Mac/Linux init script
└── README.md
```

## Artifacts emitted to Library/AgentMirror/

| File | Contents | When updated |
|------|----------|-------------|
| `SceneMirror.json` | All GameObjects: stableId, name, path, kind, components | hierarchy change, prefab import |
| `SceneMirror.meta.json` | entityCount, sceneCount, emittedAt | same |
| `AsmdefGraph.json` | Assembly names, file lists, references | .asmdef import |
| `AnimatorDump.json` | All controllers: layers, states, transitions, parameters | .controller import |
| `ProjectDigest.md` | git log, changed files, uncommitted, last session | Unity editor launch |
| `RefactorEvent.json` | Mass-rename/reparent events (>10 objects) | mass hierarchy change |
| `PrefabGraph.json` | Prefab assets: instanceCount, variantOf, scenesUsedIn | .prefab import |
| `InspectorRefs.json` | UnityEvent wiring: source, target, method | .unity/.prefab import |
| `SessionLedger.jsonl` | Per-entity modification log | agent edits (via UndoGroupWrapper) |
| `CorrectionLedger.md` | User corrections — injected at session start | session stop hook |
| `HookAudit.jsonl` | Hook fire log | every hook execution |

## Slash commands

| Command | Action |
|---------|--------|
| `/unity-on` | Enable all discipline hooks |
| `/unity-off [reason]` | Disable all (emitters keep running) |
| `/unity-status` | Show enabled state + all rule states |
| `/unity-rule-off <rule>` | Disable one rule by name |
| `/unity-rule-on <rule>` | Re-enable one rule |
| `/unity-rule-status` | List all rules with N/9 count |
| `/unity-rule-reset` | Reset all rules to enabled |
| `/unityccworkflow-init` | Init pack from inside CC session |

## What is NOT overwritten by re-running init

| File | Why |
|------|-----|
| `DESIGN.md` | Human-authored intent — project-unique |
| `.claude/unity-mode.json` | Toggle state — project-specific |
| `.claude/unity-mode-history.jsonl` | Audit trail |
| `Library/AgentMirror/*.json` | Machine-generated from project |

## Merging hooks into an existing settings.json

If your project already has `.claude/settings.json`, copy the `hooks` block from `cc/settings.json.template` and merge it manually. Do not overwrite — you may have other hooks configured.

## Design document (DESIGN.md)

DESIGN.md is the human-owned source of truth for game intent. The CC discipline skill enforces it:

- `<!-- stability: locked -->` — CHALLENGE fires, must explicitly choose (a)/(b)/(c)
- `<!-- stability: settled -->` — CHALLENGE fires, options offered
- `<!-- stability: in-flux -->` — conflict noted, agent proceeds with best interpretation
- `<!-- stability: TBD -->` — agent asks before implementing

Minimum viable DESIGN.md: Core loop (locked) + one principle + Non-goals (locked).

## Validation target (Phase 6)

After install, run a comparable session to the Apr 28 baseline (180 MCP errors, 115 orientation calls, 29 compile_errors calls). Pass threshold: ≥40% reduction on all three metrics.
