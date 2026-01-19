# � Pansy - Universal Disassembly Metadata Format

**🌼 Pansy** is a standardized binary format for storing and sharing disassembly analysis data across retro gaming platforms and tools.

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
- Built with Avalonia UI
- View and edit symbols, comments, cross-refs
- Memory map visualization
- Symbol browser with search
- Cross-reference navigation
- Diff viewer for comparing analysis versions

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
pansy info game.pansy

# List all symbols
pansy symbols game.pansy

# Search for symbols
pansy find game.pansy "subroutine"

# Show cross-references for an address
pansy xrefs game.pansy $8000

# Diff two files
pansy diff original.pansy modified.pansy

# Convert to Mesen format
pansy convert game.pansy --to mesen --output symbols.mlb
```

**Library:**
```csharp
using Pansy.Core;

// Load a Pansy file
var data = File.ReadAllBytes("game.pansy");
var pansy = new PansyLoader(data);

// Access symbols
foreach (var (address, name) in pansy.Symbols) {
	Console.WriteLine($"${address:x4}: {name}");
}

// Get cross-references
var refs = pansy.CrossReferences
	.Where(x => x.To == 0x8000)
	.ToList();
```

## 📖 Documentation

- [File Format Specification](docs/format-specification.md)
- [Library API Documentation](docs/api-reference.md)
- [CLI Command Reference](docs/cli-reference.md)
- [UI User Guide](docs/ui-guide.md)
- [Integration Guide](docs/integration.md)

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

MIT License - see [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- Inspired by Mesen's MLB format and FCEUX's NL files
- Built with love for the ROM hacking and retro gaming community
- Special thanks to contributors and testers

## 🔗 Related Projects

- **[Peony](https://github.com/TheAnsarya/peony)** - Multi-system disassembler
- **[Poppy](https://github.com/TheAnsarya/poppy)** - Multi-system assembler
- **[GameInfo](https://github.com/TheAnsarya/GameInfo)** - ROM hacking toolkit
- **[BPS-Patch](https://github.com/TheAnsarya/bps-patch)** - Binary patching system
