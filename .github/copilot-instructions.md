# 🌼 Pansy - AI Copilot Directives

> **Pansy** - Program ANalysis SYstem format for comprehensive assembly metadata exchange

## Project Overview

**🌼 Pansy** is a universal disassembly metadata format and toolkit. It provides a standardized way to store, share, and edit disassembly analysis data across different platforms and tools.

**Purpose:**
- Binary file format for disassembly metadata (symbols, comments, cross-refs, memory regions)
- C# library for reading/writing Pansy files
- Cross-platform UI for viewing and editing metadata
- CLI tools for inspection and manipulation
- Integration with Poppy (assembler) and Peony (disassembler)

## GitHub Issue Management

### ⚠️ CRITICAL: Always Create Issues on GitHub Directly

**NEVER just document issues in markdown files.** Always create actual GitHub issues using the `gh` CLI:

```powershell
# Create an issue
gh issue create --repo TheAnsarya/pansy --title "Issue Title" --body "Description" --label "label1,label2"

# Add labels
gh issue edit <number> --repo TheAnsarya/pansy --add-label "label"

# Close issue
gh issue close <number> --repo TheAnsarya/pansy --comment "Completed in commit abc123"
```

### Required Labels
- `enhancement` - New features
- `bug` - Bug fixes
- `documentation` - Docs work
- `performance` - Performance related
- `testing` - Test related
- Priority: `high-priority`, `medium-priority`, `low-priority`

### ⚠️ MANDATORY: Issue-First Workflow

**Always create GitHub issues BEFORE starting implementation work.** This is non-negotiable.

1. **Before Implementation:**
   - Create a GitHub issue describing the planned work
   - Include scope, approach, and acceptance criteria
   - Add appropriate labels
   - Create plans/code-plans in `~Plans/` for non-trivial work

2. **During Implementation:**
   - Reference issue number in commits: `git commit -m "Fix loader parsing - #247"`
   - Update issue with progress comments if work spans multiple sessions
   - Add sub-issues for discovered work

3. **After Implementation:**
   - Close issue with completion comment including commit hash
   - Link related issues if applicable

**Workflow Pattern:**
```powershell
# 1. Create issue FIRST
gh issue create --repo TheAnsarya/pansy --title "Description" --body "Details" --label "label"

# 2. Add prompt tracking comment (for AI-created issues)
gh issue comment <number> --repo TheAnsarya/pansy --body "Prompt for work:`n{original user prompt}"

# 3. Implement the fix/feature

# 4. Commit with issue reference
git add .
git commit -m "Brief description - #<issue-number>"

# 5. Close issue with summary
gh issue close <number> --repo TheAnsarya/pansy --comment "Completed in <commit-hash>"
```

### ⚠️ MANDATORY: Prompt Tracking for AI-Created Issues

When creating GitHub issues from AI prompts, **IMMEDIATELY** add the original user prompt as the **FIRST comment** right after creating the issue — before doing any implementation work:

```powershell
# Create issue
$issueUrl = gh issue create --repo TheAnsarya/pansy --title "Description" --body "Details" --label "label"
$issueNum = ($issueUrl -split '/')[-1]

# IMMEDIATELY add prompt as first comment (before any other work)
gh issue comment $issueNum --repo TheAnsarya/pansy --body "Prompt for work:
<original user prompt that triggered this work>"
```

## Coding Standards

### Indentation
- **TABS for indentation** — enforced by `.editorconfig`
- **Tab width: 4 spaces** — ALWAYS use 4-space-equivalent tabs
- **Applies to all file types** — C#, JSON, YAML, Markdown, scripts, and config files
- NEVER use spaces for indentation — only tabs
- Inside code blocks in markdown, use spaces for alignment of ASCII art/diagrams

### Brace Style — K&R (One True Brace)
- **Opening braces on the SAME LINE** as the statement — ALWAYS
- This applies to ALL constructs: `if`, `else`, `for`, `while`, `switch`, `try`, `catch`, functions, methods, classes, structs, namespaces, lambdas, properties, enum declarations, etc.
- `else` and `else if` go on the same line as the closing brace: `} else {`
- `catch` goes on the same line as the closing brace: `} catch (...) {`
- **NEVER use Allman style** (brace on its own line)
- **NEVER put an opening brace on a new line** — not even for long parameter lists

#### C# Examples

```csharp
// ✅ CORRECT — K&R style
if (condition) {
	DoSomething();
} else if (other) {
	DoOther();
} else {
	DoFallback();
}

for (int i = 0; i < count; i++) {
	Process(i);
}

public void Execute(int param) {
	// body
}

public class MyClass : Base {
	public string Name { get; set; }

	public void Method() {
		// body
	}
}

// ❌ WRONG — Allman style (DO NOT USE)
if (condition)
{
	DoSomething();
}
```

### Hexadecimal Values
- **Always lowercase**: `0xff00`, not `0xFF00`
- **Format specifiers lowercase**: `:x4`, not `:X4`
- Use `$` for addresses in documentation: `$ff00`

### C# Standard
- **.NET 10** with latest C# features
- File-scoped namespaces where applicable
- Nullable reference types enabled
- Modern pattern matching

### Encoding & Line Endings
- **UTF-8** encoding with BOM for all files
- **CRLF** line endings (Windows style)
- Support for Unicode and emojis

### ⚠️ Comment Safety Rule
**When adding or modifying comments, NEVER change the actual code.**

- Changes to comments must not alter code logic, structure, or formatting
- When adding XML documentation or inline comments, preserve all existing code exactly
- Verify code integrity after adding documentation

## Performance Guidelines

### ⚠️ MANDATORY: Before/After Benchmarks

**EVERY performance-related change MUST include before/after benchmarks.** This is non-negotiable.

1. **Before any code change:** Run the full benchmark suite and record results
2. **After the change:** Run the same benchmarks and compare
3. **Include results** in the issue/commit comments
4. **Reject changes** that regress performance without justification

```powershell
# Run benchmarks (dry-run to verify, then full)
dotnet run --project tests/Pansy.Core.Benchmarks -c Release -- --filter "*" --warmupCount 3 --iterationCount 5

# For quick validation
dotnet run --project tests/Pansy.Core.Benchmarks -c Release -- --job dry
```

### Data Structure Selection
- **Always research and choose the most efficient data structures** for each use case
- Profile before committing to a data structure change
- Prefer `HashSet<T>` for membership tests, `Dictionary<TKey, TValue>` for lookups
- Use `FrozenDictionary`/`FrozenSet` for read-heavy immutable data
- Consider `Span<T>`, `Memory<T>` for hot paths
- Avoid LINQ in hot paths — use manual loops

### Safe Optimizations
- `readonly` on structs and fields
- `sealed` on classes not designed for inheritance
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for small hot methods
- `Span<byte>` over `byte[]` where possible
- `stackalloc` for small temporary buffers

## Testing Guidelines

### ⚠️ MANDATORY: Before/After Testing

**EVERY code change MUST include before/after test runs.** This is non-negotiable.

1. **Before any code change:** Run the full test suite and record pass/fail counts
2. **After the change:** Run the full test suite — ALL tests must pass
3. **Add new tests** for any new functionality or bug fixes
4. **Never commit with failing tests**

```powershell
# Run all tests
dotnet test tests/Pansy.Core.Tests -c Release --nologo -v m

# Run specific test class
dotnet test tests/Pansy.Core.Tests -c Release --filter "ClassName=IntegrationTests"
```

### Test Categories
- **Unit tests** — Individual method behavior (PansyWriterTests, PansyLoaderTests)
- **Roundtrip tests** — Write → Read → Verify (RoundtripTests)
- **Bug fix tests** — Regression tests for fixed bugs (BugFixTests)
- **Integration tests** — Full workflow scenarios (IntegrationTests)
- **Typed data tests** — SymbolEntry/CommentEntry preservation (TypedDataTests)
- **Code/data map tests** — Flag roundtrip (CodeDataMapTests)
- **Platform defaults tests** — PlatformDefaults correctness (PlatformDefaultsTests)

### Verification Checklist (for EVERY code change):
1. ✅ All tests pass (`dotnet test tests/Pansy.Core.Tests -c Release`)
2. ✅ Build succeeds for all projects (`dotnet build Pansy.sln -c Release`)
3. ✅ New tests added for new/changed functionality
4. ✅ Benchmarks show no regression (for performance changes)
5. ✅ No new warnings in build output
6. ✅ Code formatted (tabs, K&R braces, lowercase hex)

## Documentation-First Development

### ⚠️ MANDATORY: Document Before Implement

For any non-trivial work:

1. **Create a plan** in `~Plans/` describing the approach
2. **Create a GitHub issue** with scope and acceptance criteria
3. **Create code-plans** for complex changes (pseudocode, data flow, API design)
4. **Then implement** — with the plan as your guide

### Required Documentation
- `~Plans/` — Technical plans and code-plans
- `~docs/session-logs/` — Session summaries
- `docs/` — User-facing documentation (FILE-FORMAT.md, CLI-REFERENCE.md, etc.)
- All docs should be reachable from `README.md`

### ⚠️ MANDATORY: Session Logs

**Always create a session log at the end of every conversation that involves code changes, issue creation, or significant research.** This is non-negotiable.

- File: `~docs/session-logs/YYYY-MM-DD-session-NN.md`
- Increment `NN` if a log already exists for that date
- Include: summary of work done, issues created/closed, commits made, files changed, and next steps
- Commit the session log as part of the final commit

### Log Files

- Session logs: `~docs/session-logs/YYYY-MM-DD-session-NN.md`
- **NEVER edit** `~docs/pansy-manual-prompts-log.txt` (user-maintained)

## Git Workflow

### ⚠️ MANDATORY: Format Before Every Commit

Before EVERY commit:
1. Verify tab indentation (no spaces)
2. Verify K&R brace style (no Allman)
3. Verify lowercase hex (`0xff`, `:x4` not `0xFF`, `:X4`)
4. Run build to check for warnings
5. Run tests to verify correctness

### Commit Messages
- **Always reference issue numbers**: `Brief description - #<issue-number>`
- Logical, atomic commits — one concern per commit
- Use conventional prefixes: `feat:`, `fix:`, `test:`, `docs:`, `perf:`, `refactor:`

### Branching
- Create feature branches for significant work
- Branch naming: `feature/description`, `fix/description`
- Merge back to `main` when complete

## 📁 Project Structure

```
/                     # Root
├── .github/          # GitHub configuration
├── docs/             # User documentation (linked from README)
├── src/              # Source code
│   ├── Pansy.Core/   # Core library (format I/O)
│   ├── Pansy.UI/     # Avalonia cross-platform UI
│   └── Pansy.Cli/    # CLI tools
├── tests/            # xUnit tests & benchmarks
│   ├── Pansy.Core.Tests/       # xUnit test project
│   └── Pansy.Core.Benchmarks/  # BenchmarkDotNet project
├── ~docs/            # Project creation documentation
│   ├── chat-logs/    # AI conversation logs
│   └── session-logs/ # Session summaries
├── ~Plans/           # Short/long term plans & code-plans
├── ~manual-testing/  # Manual test files
└── ~reference-files/ # Reference materials
```

## Technology Stack

### C# .NET 10
- **Core Library** — Pansy format reading/writing
- **Avalonia UI** — Cross-platform desktop application (Windows/Linux/macOS)
- **CLI** — Command-line tools using System.CommandLine
- **xUnit** — Testing framework
- **BenchmarkDotNet** — Performance benchmarks
- **Spectre.Console** — Rich CLI output

### Build Commands

```powershell
# Build entire solution
dotnet build Pansy.sln -c Release

# Run tests
dotnet test tests/Pansy.Core.Tests -c Release

# Run benchmarks
dotnet run --project tests/Pansy.Core.Benchmarks -c Release

# Run CLI
dotnet run --project src/Pansy.Cli -- <command>

# Run UI
dotnet run --project src/Pansy.UI
```

## Problem-Solving Philosophy

### ⚠️ NEVER GIVE UP on Hard Problems

When a task is complex or seems difficult:

1. **NEVER declare something "too hard" or "not worth it"** and close the issue
2. **Break it down** — Create multiple smaller sub-issues for research, prototyping, and incremental progress
3. **Research first** — Create research issues to investigate approaches, alternatives, and prior art
4. **Document everything** — Create docs, code-plans, and analysis documents in `~Plans/`
5. **Prototype** — Create spike/prototype branches to test approaches before committing
6. **Incremental progress** — Even partial progress (e.g., replacing 3 of 15 usages) is valuable
7. **Create issues for future work** — If something can't be done now, create well-documented issues for later

### Issue Decomposition Pattern
When facing a large task:
- `Research/Investigation` — Analyze scope, dependencies, alternatives
- `Document findings` — Create technical analysis docs
- `Create prototype` — Spike branch to test feasibility
- `Implement Phase 1` — Lowest-risk subset first
- `Implement Phase 2` — Next batch of changes
- `Final cleanup` — Remove old code, update docs

## Related Projects

- **Poppy** — Assembly compiler (uses Pansy for symbols/metadata)
- **Peony** — Disassembler (generates Pansy files)
- **Nexen** — Multi-system emulator (exports Pansy metadata)
- **GameInfo** — ROM hacking toolkit
- **BPS-Patch** — Binary patching system

## ⚠️ Important Notes

1. **Never use spaces for indentation** — TABS ONLY
2. **Never use uppercase hex** — always lowercase (`0xff`, `:x4`)
3. **Never modify** the manual prompts log file
4. **Always** add BOM to UTF-8 files
5. **Always** ensure documentation is linked from README
6. **Always use `.pansy` file extension** for metadata files
7. **Always** create GitHub issues before starting work
8. **Always** run tests before and after code changes
9. **Always** run benchmarks before and after performance changes
10. **Always** format code before committing (tabs, K&R, lowercase hex)
11. **Always** tie commits to issues with `#<number>` references
12. **Always** document plans before implementing

## Pansy File Format

### Header
- Magic: "PANSY\0\0\0" (8 bytes)
- Version: uint16 (current: 0x0100)
- Flags: uint16
- Platform: byte
- Reserved: 3 bytes
- ROM size: uint32
- ROM CRC32: uint32
- Section count: uint32
- Reserved: 4 bytes

### Content Sections
1. **Code/Data Map** (0x0001) — Per-byte flags: CODE, DATA, JUMP_TARGET, SUB_ENTRY, OPCODE, DRAWN, READ, INDIRECT
2. **Symbols** (0x0002) — Address → Name + Type (Label, Constant, Enum, Struct, Macro, Local, Anonymous, InterruptVector, Function)
3. **Comments** (0x0003) — Address → Text + Type (Inline, Block, Todo)
4. **Memory Regions** (0x0004) — Named memory regions with types and banks
5. **Data Types** (0x0005) — *Reserved, not yet implemented*
6. **Cross-References** (0x0006) — From/To address pairs with type (Jsr, Jmp, Branch, Read, Write)
7. **Source Map** (0x0007) — *Reserved, not yet implemented*
8. **Metadata** (0x0008) — Project name, author, version, timestamps

### Platform IDs
- 0x01: NES
- 0x02: SNES
- 0x03: Game Boy
- 0x04: Game Boy Advance
- 0x05: Sega Genesis
- 0x06: Sega Master System
- 0x07: PC Engine
- 0x08: Atari 2600
- 0x09: Atari Lynx
- 0x0a: WonderSwan
- 0xff: Custom

## Markdown Formatting

### ⚠️ MANDATORY: Fix Markdownlint Warnings

**Always fix markdownlint warnings when editing or creating markdown files.** This is non-negotiable.

Key rules to enforce:

- **MD022** — Blank lines above and below headings
- **MD031** — Blank lines around fenced code blocks
- **MD032** — Blank lines around lists (ordered and unordered)
- **MD047** — Files must end with a single newline character
- **MD007** — Disabled (tab indentation is 1 character, not 4)
- **MD010** — Disabled (hard tabs are REQUIRED per our indentation rules)

When generating new markdown content, **always include proper blank line spacing** around headings, lists, and code blocks.

### ⚠️ MANDATORY: Documentation Link-Tree

**Every markdown file in the repository must be reachable from `README.md` through a hierarchical link structure.**

- The main `README.md` must link to all documentation directories and key files
- Subdirectory docs should link back to parent and to sibling docs
- No orphan markdown files — if a `.md` file exists, it must be discoverable from the root README
- When adding new documentation, always update `README.md` with a link to it
- Internal docs (`~docs/`) should have their own index linked from the main README

