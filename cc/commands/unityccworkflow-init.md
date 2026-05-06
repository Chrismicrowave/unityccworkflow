# unityccworkflow-init

Initialize the unityccworkflow solution pack for the current Unity project.

## Steps

1. Verify CWD is a Unity project root: check for `Assets/` and `ProjectSettings/` directories. If not found, stop and report the error.

2. Ask: "Where is the unityccworkflow pack located?" Default suggestion: `C:\Users\Administrator\Downloads\unityccworkflow` (Windows) or `~/unityccworkflow` (Mac/Linux).

3. Run `.\init.ps1 -ProjectPath "<CWD>"` (Windows) or `bash init.sh "<CWD>"` (Mac/Linux) from the pack path.

4. Report what was created or copied: C# files, hook scripts, commands, skill, Assets/Docs/DESIGN.md template.

5. Print the 5 manual steps remaining:
   - Open Unity — AgentMirror emitters compile and populate Library/AgentMirror/
   - In Unity: Tools → AgentMirror → Add StableId to selection (recursive) on scene root and prefab roots
   - Fill in Assets/Docs/DESIGN.md — core loop, principles, at least one unit behavior, non-goals
   - Open CC in project root and run `/unity-on`
   - Verify with `/unity-status` — should show 🛡️ 9/9

6. Offer: "Shall I also run the StableId bootstrap via MCP now?" If yes: call `execute_script` to run `StableIdBootstrap.AddToSelection()` on all root scene objects.
