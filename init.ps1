# init.ps1 — Initialize unityccworkflow for a Unity project
# Usage: .\init.ps1 -ProjectPath "D:\path\to\unity\project"
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath
)

$ErrorActionPreference = "Stop"

# Verify Unity project
if (-not (Test-Path "$ProjectPath/Assets") -or -not (Test-Path "$ProjectPath/ProjectSettings")) {
    Write-Error "Not a Unity project root: $ProjectPath (missing Assets/ or ProjectSettings/)"
    exit 1
}

$pkg = $PSScriptRoot  # location of unityccworkflow/

Write-Host "Initializing unityccworkflow for: $ProjectPath"
Write-Host ""

# 1. Copy Unity C# files
Write-Host "1. Copying Unity C# files..."
New-Item -ItemType Directory -Force "$ProjectPath/Assets/Editor/AgentMirror" | Out-Null
New-Item -ItemType Directory -Force "$ProjectPath/Assets/Scripts/Core" | Out-Null
Copy-Item "$pkg/unity/Editor/AgentMirror/*" "$ProjectPath/Assets/Editor/AgentMirror/" -Recurse -Force
Copy-Item "$pkg/unity/Runtime/StableId.cs" "$ProjectPath/Assets/Scripts/Core/StableId.cs" -Force
Write-Host "   ✓ Editor/AgentMirror/*.cs, Scripts/Core/StableId.cs"

# 2. Copy CC hook scripts
Write-Host "2. Copying hook scripts..."
New-Item -ItemType Directory -Force "$ProjectPath/.claude/hooks" | Out-Null
Copy-Item "$pkg/cc/hooks/*" "$ProjectPath/.claude/hooks/" -Force
Write-Host "   ✓ .claude/hooks/*.sh"

# 3. Copy skill
Write-Host "3. Copying discipline skill..."
New-Item -ItemType Directory -Force "$ProjectPath/.claude/skills/unity-mcp-discipline" | Out-Null
Copy-Item "$pkg/cc/skills/unity-mcp-discipline/skill.md" "$ProjectPath/.claude/skills/unity-mcp-discipline/skill.md" -Force
Write-Host "   ✓ .claude/skills/unity-mcp-discipline/skill.md"

# 4. Copy slash commands
Write-Host "4. Copying slash commands..."
New-Item -ItemType Directory -Force "$ProjectPath/.claude/commands" | Out-Null
Copy-Item "$pkg/cc/commands/*" "$ProjectPath/.claude/commands/" -Force
Write-Host "   ✓ .claude/commands/unity-*.md, unityccworkflow-init.md"

# 5. Copy toggle config (only if not already present — preserve project state) and write init history entry
$modeJsonPath = "$ProjectPath/.claude/unity-mode.json"
if (Test-Path $modeJsonPath) {
    Write-Host "5. unity-mode.json already exists — skipping (project-owned, not overwritten)"
} else {
    Copy-Item "$pkg/cc/unity-mode.json" $modeJsonPath -Force
    Write-Host "   ✓ .claude/unity-mode.json (enabled: true)"
}
$historyPath = "$ProjectPath/.claude/unity-mode-history.jsonl"
$ts = (Get-Date -Format "o")
Add-Content $historyPath "{`"ts`": `"$ts`", `"action`": `"master-on`", `"reason`": `"init`", `"session`": `"unityccworkflow-init`"}"
Write-Host "   ✓ .claude/unity-mode-history.jsonl (init entry written)"

# 6. Merge hooks into .claude/settings.json
Write-Host "6. Configuring settings.json..."
$settingsPath = "$ProjectPath/.claude/settings.json"
if (Test-Path $settingsPath) {
    Write-Warning "settings.json already exists at $settingsPath"
    Write-Warning "Manually merge the hooks block from: $pkg/cc/settings.json.template"
    Write-Warning "See README.md for merge instructions."
} else {
    New-Item -ItemType Directory -Force "$ProjectPath/.claude" | Out-Null
    Copy-Item "$pkg/cc/settings.json.template" $settingsPath -Force
    Write-Host "   ✓ .claude/settings.json (from template)"
}

# 7. Create Assets/Docs/ and copy DESIGN.md template (only if not already present — human-authored)
$docsDir = "$ProjectPath/Assets/Docs"
$designPath = "$docsDir/DESIGN.md"
New-Item -ItemType Directory -Force $docsDir | Out-Null
if (Test-Path $designPath) {
    Write-Host "7. Assets/Docs/DESIGN.md already exists — skipping (human-authored, not overwritten)"
} else {
    Copy-Item "$pkg/templates/DESIGN.md.template" $designPath -Force
    Write-Host "   ✓ Assets/Docs/DESIGN.md (from template — fill in your game's intent)"
}

# 8. Create CLAUDE.md game-director declaration (only if not already present — project-authored)
$claudeMdPath = "$ProjectPath/CLAUDE.md"
if (Test-Path $claudeMdPath) {
    Write-Host "8. CLAUDE.md already exists — skipping (project-authored, not overwritten)"
} else {
    Copy-Item "$pkg/templates/CLAUDE.md.template" $claudeMdPath -Force
    Write-Host "   ✓ CLAUDE.md (game-director declaration — fill in project description)"
}

# 9. Create .claude/teams/game-dev/index.md (routing + non-negotiables — always refreshed from template)
$teamDir = "$ProjectPath/.claude/teams/game-dev"
New-Item -ItemType Directory -Force $teamDir | Out-Null
$indexPath = "$teamDir/index.md"
if (Test-Path $indexPath) {
    Write-Host "9. .claude/teams/game-dev/index.md already exists — skipping (may have project customisations)"
} else {
    Copy-Item "$pkg/templates/game-dev-index.md.template" $indexPath -Force
    Write-Host "   ✓ .claude/teams/game-dev/index.md (routing table + non-negotiables)"
}

# 9b. Copy team domain pattern files (always refresh — these are the general knowledge base)
Write-Host "9b. Copying team domain pattern files..."
$packTeamDir = "$pkg/cc/teams/game-dev"
if (Test-Path $packTeamDir) {
    $skipCount = 0
    $copyCount = 0
    foreach ($file in Get-ChildItem "$packTeamDir/*.md" -Recurse) {
        $target = "$teamDir/$($file.Name)"
        if ((Test-Path $target) -and (Get-Item $target).Length -gt 100) {
            $skipCount++
        } else {
            Copy-Item $file.FullName $target -Force
            $copyCount++
        }
    }
    Write-Host "   ✓ $copyCount files copied, $skipCount existing files skipped (keep project customisations)"
} else {
    Write-Host "   - No team domain files in pack yet"
}

# 10. Create Assets/Docs/ProjectKnow/ directory for project-specific knowledge
$projectKnowDir = "$ProjectPath/Assets/Docs/ProjectKnow"
New-Item -ItemType Directory -Force $projectKnowDir | Out-Null
Write-Host "   ✓ Assets/Docs/ProjectKnow/ (add domain .md files here as the project grows)"

Write-Host ""
Write-Host "=========================================="
Write-Host "unityccworkflow initialized successfully."
Write-Host "=========================================="
Write-Host ""
Write-Host "Next steps (manual — ~15 minutes):"
Write-Host ""
Write-Host "  Step 1 — Open Unity"
Write-Host "           AgentMirror emitters compile and run automatically."
Write-Host "           Check: Library/AgentMirror/SceneMirror.json exists."
Write-Host ""
Write-Host "  Step 2 — StableId bootstrap"
Write-Host "           In Unity: select scene root GameObject"
Write-Host "           Tools → AgentMirror → Add StableId to selection (recursive)"
Write-Host "           Repeat for each prefab root folder in Project window."
Write-Host ""
Write-Host "  Step 3 — Author DESIGN.md"
Write-Host "           Open Assets/Docs/DESIGN.md."
Write-Host "           Fill in: core loop, game principles, unit behaviors, non-goals."
Write-Host "           Mark sections with stability tiers."
Write-Host ""
Write-Host "  Step 4 — Fill in CLAUDE.md project description"
Write-Host "           Open CLAUDE.md and replace [PROJECT_NAME] and the project description."
Write-Host "           This is what tells Claude what the game is about each session automatically."
Write-Host ""
Write-Host "  Step 5 — Smoke test"
Write-Host "           /unity-status → should show 🛡️ 9/9"
Write-Host "           Ask: 'what entities are in the scene?' → reads SceneMirror, no MCP list calls"
Write-Host "           Edit a .cs file → compile-stop hook fires"
