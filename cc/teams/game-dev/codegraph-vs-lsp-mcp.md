# codegraph vs lsp-mcp (OmniSharp)

**Test date:** 2026-06-05  
**Context:** Two sub-agents given identical tasks — analyze and explain the StrokeSnap Unity C# codebase — each using one tool exclusively.

## raw data

| Metric | codegraph | lsp-mcp |
|---|---|---|
| Wall clock | 2 min 26 sec | 24 min 33 sec |
| Tool calls | 58 | 67 |
| Tokens | 115,601 | 113,913 |
| Tool-specific queries | 6 `codegraph` commands | 7 lsp-mcp tool calls + init |
| Supplemented by Read/Grep | Selective (relied on index) | Yes (read ~30 files) |
| Code issues found | 0 | 7 |
| Call graph | Built-in (`callers`/`callees`) | Manual (references) |

## verdict

**codegraph — use for speed and structure:**
- Pre-built SQLite index → sub-second queries
- CLI-first → works in any context (CI, headless, Bash)
- `callers`/`callees`/`impact` commands are unique to it
- 10× faster than lsp-mcp for the same breadth of analysis

**lsp-mcp — use for semantic depth:**
- Roslyn-level accuracy (resolved types, not AST guesses)
- `lsp_diagnostics` catches actual code quality bugs
- `lsp_definition`/`lsp_references` for precise navigation
- Slower boot but richer per-finding data

**Both together (recommended):**
1. `codegraph query` → instant class/method find
2. `lsp-mcp lsp_definition` or `lsp_references` → precise semantics
3. `lsp_diagnostics` → code quality check on changed files

## practical note

codegraph has a CLI (`codegraph query/files/callers`). lsp-mcp is an MCP server only (requires an MCP client to load it). This makes codegraph more accessible from sub-agents, scripts, and CI — lsp-mcp only works when Claude Code natively loads it as an MCP server, which requires a session restart after adding it to `.claude/settings.json`.
