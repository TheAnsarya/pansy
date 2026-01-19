# � Pansy - Universal Disassembly Metadata Format

**🌼 Pansy** is a standardized binary format for storing and sharing disassembly analysis data across retro gaming platforms and tools.

## 📢 Status

**Version:** 1.0  
**Status:** Beta - Format stable, core functionality complete

### Recent Updates
- ✅ **UI Editing Complete** - Full CRUD operations for symbols, comments, and memory regions
- ✅ **Format Compatibility Fixed** - PansyWriter and PansyLoader now fully compatible with roundtrip support
- ✅ **17 Tests Passing** - Comprehensive test coverage including roundtrip verification
- ✅ **CLI Commands Working** - info, symbols, find, xrefs, diff all functional
- ✅ **Documentation Complete** - File format spec, CLI reference, examples guide
- 📝 **GitHub Issues** - 10 enhancement tasks tracked on project board

## 🎯 Purpose

Disassembly is more than just converting bytes to instructions. It involves:
- **Symbols** - Meaningful names for addresses (subroutines, variables, labels)
- **Comments** - Explanations of what code does
- **Cross-References** - Understanding who calls what
- **Memory Regions** - Organizing code/data/RAM/ROM
- **Type Information** - Distinguishing code from data

Pansy provides a universal, efficient format to store all this metadata independently from the disassembled code itself.

## ✨ Features

### 🗃️ Binary Format
- Compact, versioned binary format (`.pansy` files)
- Platform-agnostic (NES, SNES, GB, GBA, Genesis, Atari 2600, Custom)
- Fast loading and saving
- Supports large ROMs efficiently

### 📚 C# Library
- `Pansy.Core` - Read/write Pansy files
- Modern .NET 10 / C# 14
- Cross-platform (Windows, Linux, macOS)
- Comprehensive xUnit test coverage

### 🖥️ Cross-Platform UI
- Built with Avalonia UI (Windows, Linux, macOS)
- **Add/Edit/Delete** symbols, comments, and memory regions
- **Save/Save As** with dirty state tracking
- **Real-time Search** - Filter symbols and comments
- Memory map visualization with type information
- Cross-reference browser with navigation
- Double-click to edit, toolbar buttons for all operations
- Input dialogs for all editing tasks

### 🛠️ CLI Tools
- Inspect Pansy files from command line
- Convert to/from other formats (Mesen, FCEUX, No$GBA, etc.)
- Merge multiple analysis files
- Search and query metadata
- Diff and compare files

## 🚀 Quick Start

### Installation

```bash
# Clone the repository
git clone https://github.com/TheAnsarya/pansy.git
cd pansy

# Build the solution
dotnet build Pansy.sln

# Run tests
dotnet test Pansy.sln
```

### Usage Examples

**CLI:**
```bash
# View Pansy file info
dotnet run --project src/Pansy.Cli -- info game.pansy

# List all symbols
dotnet run --project src/Pansy.Cli -- symbols game.pansy

# Search for symbols or comments
dotnet run --project src/Pansy.Cli -- find game.pansy "Handler"

# Show cross-references for an address (decimal)
dotnet run --project src/Pansy.Cli -- xrefs game.pansy 32784

# Diff two files
dotnet run --project src/Pansy.Cli -- diff original.pansy modified.pansy
```

**UI:**
```bash
# Launch the UI application
dotnet run --project src/Pansy.UI

# Open a Pansy file: File → Open (Ctrl+O)
# Edit symbols: Click Symbols tab → Add/Edit/Delete buttons
# Search: Use search box to filter symbols/comments
# Save changes: File → Save (Ctrl+S) or Save As (Ctrl+Shift+S)
```

**Library:**
```csharp
using Pansy.Core;

// Create a new Pansy file
var writer = new PansyWriter {
	Platform = PansyLoader.PLATFORM_NES,
	RomSize = 0x20000,
	RomCrc32 = 0x12345678,
	ProjectName = "My ROM Hack",
	Author = "Your Name",
	ProjectVersion = "1.0.0"
};

// Add metadata
writer.AddSymbol(0x8000, "Reset");
writer.AddSymbol(0x8100, "Main_Loop");
writer.AddComment(0x8000, "Reset vector entry point");
writer.MarkAsCode(0x8000);
writer.MarkAsJumpTarget(0x8100);
writer.AddCrossReference(new CrossReference(0x8050, 0x8100, CrossRefType.Jmp));

// Generate file
var data = writer.Generate();
File.WriteAllBytes("game.pansy", data);

// Load a Pansy file
var pansy = new PansyLoader(data);

// Access symbols
foreach (var (address, name) in pansy.Symbols) {
	Console.WriteLine($"${address:X4}: {name}");
}

// Get cross-references to a specific address
var refsTo = pansy.CrossReferences
	.Where(x => x.To == 0x8100)
	.ToList();

Console.WriteLine($"References to $8100: {refsTo.Count}");
```

## 📖 Documentation

- [File Format Specification](docs/FILE-FORMAT.md) - Complete format documentation with platform-specific details
- [CLI Reference](docs/CLI-REFERENCE.md) - Comprehensive command-line tool guide
- [Examples](docs/EXAMPLES.md) - Workflow guides and use cases
- API examples and integration guides

## 🔗 Integration

Pansy integrates with:
- **[Peony](https://github.com/TheAnsarya/peony)** - Disassembler (generates Pansy files)
- **[Poppy](https://github.com/TheAnsarya/poppy)** - Assembler (uses Pansy for symbols)
- **[GameInfo](https://github.com/TheAnsarya/GameInfo)** - ROM hacking toolkit
- **Mesen 2** - NES/SNES emulator (import/export)
- **FCEUX** - NES emulator (import/export)
- **No$GBA** - GBA emulator (import/export)

## 🌟 Platform Support

| Platform | ID | Support |
|----------|-----|---------|
| NES | 0x01 | ✅ Full |
| SNES | 0x02 | ✅ Full |
| Game Boy | 0x03 | ✅ Full |
| Game Boy Advance | 0x04 | ✅ Full |
| Sega Genesis | 0x05 | ✅ Full |
| Atari 2600 | 0x06 | ✅ Full |
| Custom | 0xFF | ✅ Full |

## 🏗️ Architecture

```
Pansy/
├── src/
│   ├── Pansy.Core/          # Core library (format I/O)
│   ├── Pansy.UI/            # Avalonia desktop app
│   └── Pansy.Cli/           # Command-line tools
├── tests/
│   ├── Pansy.Core.Tests/    # Core library tests
│   ├── Pansy.UI.Tests/      # UI tests
│   └── Pansy.Cli.Tests/     # CLI tests
└── docs/                    # Documentation
```

## 🤝 Contributing

Contributions welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📜 License

This is free and unencumbered software released into the public domain. See [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- Inspired by Mesen's MLB format and FCEUX's NL files
- Built with love for the ROM hacking and retro gaming community
- Special thanks to contributors and testers

## 🔗 Related Projects

- **[Peony](https://github.com/TheAnsarya/peony)** - Multi-system disassembler
- **[Poppy](https://github.com/TheAnsarya/poppy)** - Multi-system assembler
- **[GameInfo](https://github.com/TheAnsarya/GameInfo)** - ROM hacking toolkit
- **[BPS-Patch](https://github.com/TheAnsarya/bps-patch)** - Binary patching system
