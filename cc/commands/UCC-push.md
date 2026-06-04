# UCC-push

Push the unityccworkflow pack to GitHub. Backports project divergences first, then versions and pushes.

## Steps

1. **Backport project divergences** — Run `UCC-backport` flow:
   - Check divergences on all tracked paths (hooks, commands, AgentMirror, settings.json hooks section, CLAUDE.md rules)
   - Present diff summary and ask approval per file
   - Copy approved files from project → pack
   - Commit to local pack: `git add -A && git commit -m "backport: $(basename $(git rev-parse --show-toplevel 2>/dev/null || echo 'project')) — $(date +%Y-%m-%d)"`

2. **Check local pack git status** — In `D:\Files\Desktop\Claude\Projects\UnityCCWorkflow\`:
   - Read `pack-info.json` for current version.
   - Run `git log --oneline -5` to see recent commits.
   - Run `git status --short` to check for uncommitted changes.
   - Run `git log --oneline HEAD..origin/master` to see unpulled remote commits (if any).
   - Run `git log --oneline origin/master..HEAD` to see unpushed local commits.

3. **Present summary** — Show:
   - What was backported from the project
   - Local pack version
   - Unpushed commits vs origin

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
