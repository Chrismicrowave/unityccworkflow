# UCC-push

Push the unityccworkflow pack to GitHub. Checks both the local pack and the current Unity project for the latest state, presents a diff summary, and confirms before pushing.

## Steps

1. **Check local pack git status** — In `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\`:
   - Read `pack-info.json` for current version.
   - Run `git log --oneline -5` to see recent commits.
   - Run `git status --short` to check for uncommitted changes.
   - Run `git log --oneline HEAD..origin/master` to see unpulled remote commits (if any).
   - Run `git log --oneline origin/master..HEAD` to see unpushed local commits.

2. **Check divergences** — For each synced path pair, run `git diff --no-index` to detect changes:
   - `.claude/hooks/` ↔ `cc/hooks/`
   - `.claude/skills/unity-mcp-discipline/` ↔ `cc/skills/unity-mcp-discipline/`
   - `.claude/commands/UCC-*.md` ↔ `cc/commands/UCC-*.md`
   - `Assets/Editor/AgentMirror/` ↔ `unity/Editor/AgentMirror/`
   - `Assets/Scripts/Core/StableId.cs` ↔ `unity/Runtime/StableId.cs`

3. **Auto-capture project changes** — If any divergences are found, copy changed files **from project → pack**. This ensures project improvements are saved to UCCPack automatically.
   - Copy divergent files from each project path to its pack counterpart.
   - Report which files were captured.

4. **Present summary** — Show:
   - Local pack version (from `pack-info.json`)
   - Local pack unpushed commits vs origin
   - Any files captured from the project (or note that none diverged)

5. **Determine new version** — Parse current version from `pack-info.json`. Propose bump:
   - Patch (default — usually the right choice)
   - Minor
   - Major
   - Custom
   - Ask user which bump to use (plain text question).

6. **Update pack-info.json** — Write the new version into `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\pack-info.json`. Also stamp with source project and timestamp:
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
