# UCC-push

Push the unityccworkflow pack to GitHub. Checks both the local pack and the current Unity project for the latest state, presents a diff summary, and confirms before pushing.

## Steps

1. **Check local pack git status** — In `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\v0.1\`:
   - Read `pack-info.json` for current version.
   - Run `git log --oneline -5` to see recent commits.
   - Run `git status --short` to check for uncommitted changes.
   - Run `git log --oneline HEAD..origin/master` to see unpulled remote commits (if any).
   - Run `git log --oneline origin/master..HEAD` to see unpushed local commits.

2. **Check session project for divergences** — Compare the project's `.claude/commands/*.md` files (the pack-related ones: `UCC-*`, `unity-*`, `unityccworkflow-init`) against the local pack's `cc/commands/`:
   - Use `git diff --no-index` to compare corresponding files.
   - Also check `.claude/hooks/`, `.claude/skills/unity-mcp-discipline/`, `Assets/Editor/AgentMirror/`, `Assets/Scripts/Core/StableId.cs`.

3. **Present summary to user** — Show:
   - Local pack version (from `pack-info.json`)
   - Local pack unpushed commits vs origin
   - Any divergence between session project pack files and local pack (which files differ)
   - **Ask the user to confirm**: which source to push from (local pack, or sync project→pack first then push, or cancel)
   - Use plain text question — do NOT use AskUserQuestion tool.

4. **Sync if needed** — If user wants to push from project changes:
   - Copy the divergent files from project → pack source (`cc/commands/`, etc.)
   - Commit in the pack repo: `git add -A && git commit -m "sync: update from project"`
   - If user wants to push local pack directly, skip this step.

5. **Determine new version** — Parse current version from `pack-info.json`. Propose bump:
   - Patch (default — usually the right choice)
   - Minor
   - Major
   - Custom
   - Ask user which bump to use (plain text question).

6. **Update pack-info.json** — Write the new version into `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\v0.1\pack-info.json`. Also stamp with source project and timestamp:
   - Add `"lastPushedFrom": "<ProjectName>"` — e.g. `"TechLead (GDS5-P3)"`
   - Add `"lastPushedAt": "<iso-timestamp>"` — use `$(date -Iseconds)` or PowerShell `(Get-Date -Format "o")`

7. **Commit** — `git add -A && git commit -m "release: v{new_version} from <ProjectName>"`

8. **Tag and push** — `git tag v{new_version}` && `git push origin master --tags`

9. **Update project version** — Write `.claude/pack-version.json` in the project:
   - `version`: the new version
   - `sourceCommit`: `git log --oneline -1 --format=%h`
   - `installedAt`: current timestamp

10. **Report result** — Show:
   - Version bumped from → to
   - Commit log (last 3 entries)
   - Tag pushed
   - Confirmation the pack is live at `https://github.com/Chrismicrowave/unityccworkflow`
