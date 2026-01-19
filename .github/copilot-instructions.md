# � Pansy - AI Copilot Directives

## Project Overview

**🌼 Pansy** is a universal disassembly metadata format and toolkit. It provides a standardized way to store, share, and edit disassembly analysis data across different platforms and tools.

**Purpose:**
- Binary file format for disassembly metadata (symbols, comments, cross-refs, memory regions)
- C# library for reading/writing Pansy files
- Cross-platform UI for viewing and editing metadata
- CLI tools for inspection and manipulation
- Integration with Poppy (assembler) and Peony (disassembler)

## Coding Standards

### Indentation & Whitespace
- **ALWAYS use TABS for indentation** - Never spaces, in any file type
- Tab width: 4 spaces
- Remove trailing whitespace from all lines
- Include a blank line at the end of every file

### Brace Style
- **K&R style** - Opening braces on the SAME line as the statement
- Example:
  ```csharp
  if (condition) {
      // code
  } else {
      // code
  }
  ```

### Hexadecimal Values
- **Always lowercase** for all hex values
- Use `$` for addresses in documentation: `$ff00`
- Use `0x` for C# hex literals: `0xff00`

### C# Style
- **K&R brace style** - opening brace on same line
- .NET 10 / C# 14 features
- File-scoped namespaces
- Nullable reference types enabled
- Modern pattern matching

### Encoding & Line Endings
- **UTF-8** encoding with BOM for all files
- **CRLF** line endings (Windows style)
- Support for Unicode and emojis

## 📁 Project Structure

```
/                     # Root
├── .github/          # GitHub configuration
├── docs/             # User documentation (linked from README)
├── src/              # Source code
│   ├── Pansy.Core/   # Core library (format I/O)
│   ├── Pansy.UI/     # Avalonia cross-platform UI
│   └── Pansy.Cli/    # CLI tools
├── tests/            # xUnit tests
├── ~docs/            # Project creation documentation
│   ├── chat-logs/    # AI conversation logs
│   └── session-logs/ # Session summaries
├── ~Plans/           # Short/long term plans
├── ~manual-testing/  # Manual test files
└── ~reference-files/ # Reference materials
```

## Technology Stack

### C# .NET 10
- **Core Library** - Pansy format reading/writing
- **Avalonia UI** - Cross-platform desktop application (Windows/Linux/macOS)
- **CLI** - Command-line tools using System.CommandLine
- **xUnit** - Testing framework
- **Spectre.Console** - Rich CLI output

### File Format
- **Binary format** - Efficient storage, versioned
- **Platform support** - NES, SNES, GB, GBA, Genesis, Atari 2600, Custom
- **Content types** - Symbols, comments, cross-refs, memory regions, code/data maps

## 📝 Documentation Requirements

### Code Comments
- Comment ALL code thoroughly
- XML documentation for public APIs
- Document file format specifications
- Include examples where helpful

### Documentation Files
- All docs should be reachable from `README.md`
- Use emojis and formatting for readability
- Keep markdown files in `/docs/` or inline with code

### Log Files
- Chat logs: `~docs/chat-logs/YYYY-MM-DD-chat-NN.md`
- Session logs: `~docs/session-logs/YYYY-MM-DD-session-NN.md`
- **NEVER edit** `~docs/pansy-manual-prompts-log.txt` (user-maintained)

## 🔀 Git Workflow

### Branching
- Create feature branches for significant work
- Branch naming: `feature/description`, `fix/description`
- Merge back to `main` when complete

### Commits
- Logical, atomic commits
- Always reference GitHub issues in commit messages
- Format: `Brief description (#issue-number)`

### Issues
- Create GitHub issues for all planned work
- Use Kanban board for project management
- Link all commits to relevant issues

## Related Projects

- **Poppy** - Assembly compiler (uses Pansy for symbols/metadata)
- **Peony** - Disassembler (generates Pansy files)
- **GameInfo** - ROM hacking toolkit
- **BPS-Patch** - Binary patching system

## ⚠️ Important Notes

1. **Never use spaces for indentation** - TABS ONLY
2. **Never use uppercase hex** - always lowercase
3. **Never modify** the manual prompts log file
4. **Always** add BOM to UTF-8 files
5. **Always** ensure documentation is linked from README
6. **Always use `.pansy` file extension** for metadata files

## Pansy File Format

### Header
- Magic: "PNSY" (4 bytes)
- Version: uint16 (current: 0x0100)
- Platform: byte (NES=0x01, SNES=0x02, GB=0x03, etc.)
- Flags: byte
- ROM size: uint32
- ROM CRC32: uint32

### Content Sections
1. **Symbols** - Address → Name mappings
2. **Comments** - Address → Comment text
3. **Code Offsets** - Addresses marked as code
4. **Data Offsets** - Addresses marked as data
5. **Jump Targets** - Branch/jump destinations
6. **Sub Entry Points** - Subroutine addresses
7. **Memory Regions** - Named memory regions with types
8. **Cross-References** - Who references what (type, from, to)

### Platform IDs
- 0x01: NES
- 0x02: SNES
- 0x03: Game Boy
- 0x04: Game Boy Advance
- 0x05: Sega Genesis
- 0x06: Atari 2600
- 0xFF: Custom

## Build Commands

```bash
# Build entire solution
dotnet build Pansy.sln

# Run tests
dotnet test Pansy.sln

# Run CLI
dotnet run --project src/Pansy.Cli -- <command>

# Run UI
dotnet run --project src/Pansy.UI
```

## UI Features

- **File Viewer** - Inspect Pansy files with rich formatting
- **Symbol Editor** - Add/edit/remove symbols
- **Comment Editor** - Annotate addresses
- **Cross-Reference Browser** - Navigate relationships
- **Memory Map View** - Visualize code/data regions
- **Export/Import** - Convert to other formats (Mesen, FCEUX, etc.)
- **Diff View** - Compare two Pansy files

## CLI Commands

```bash
# View file info
pansy info file.pansy

# List symbols
pansy symbols file.pansy

# Search symbols
pansy find file.pansy "subroutine"

# Show cross-references
pansy xrefs file.pansy $8000

# Diff two files
pansy diff file1.pansy file2.pansy

# Convert formats
pansy convert file.pansy --to mesen --output symbols.txt

# Merge files
pansy merge base.pansy overlay.pansy --output merged.pansy
```

