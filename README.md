# 🌼 Pansy - Universal Disassembly Metadata Format

> **Pansy** - Program ANalysis SYstem format for comprehensive assembly metadata exchange

**🌼 Pansy** is a standardized binary format for storing and sharing disassembly analysis data across retro gaming platforms and tools.

## 📢 Status

**Version:** 1.0.0  
**Status:** Stable - Format finalized, core functionality complete, NuGet package available

### Recent Updates

- ✅ **Data Pattern Detection** - Automated fill, ASCII, pointer table, and tile data detection
- ✅ **Auto-Annotation** - Generate enriched Pansy files with detected pattern metadata
- ✅ **File Merging** - Combine base + overlay Pansy files with intelligent deduplication
- ✅ **Parallel Processing** - Parallel gap detection, section decompression, and merge operations
- ✅ **328 Tests Passing** - Comprehensive coverage including analyzer, merger, roundtrip, batch API, format compatibility, and edge case tests
- ✅ **Label Intelligence** - LabelMergeEngine, LabelDataCache, ILabelSuggester, auto-label generation from CDL + hardware registers
- ✅ **433 Total Tests** - Including Label Intelligence, platform defaults, and integration tests
- ✅ **70 Benchmarks** - Writer, loader, analyzer, merger, batch API, cross-ref query, graph export, and bookmark/data type performance tracking
- ✅ **Batch APIs** - High-performance batch insert for symbols, comments, cross-references, and memory regions
- ✅ **Performance Optimized** - FrozenDictionary/FrozenSet for zero-allocation lookups, LINQ-free hot paths
- ✅ **Typed Metadata** - SymbolEntry/CommentEntry records with type preservation
- ✅ **Extended Flags** - DRAWN, READ, INDIRECT code/data map flags
- ✅ **UI Editing Complete** - Full CRUD operations for symbols, comments, and memory regions
- ✅ **CLI Commands Working** - info, symbols, find, xrefs, diff, analyze, merge all functional
- ✅ **Documentation Complete** - File format spec, CLI reference, examples guide

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
- `PansyAnalyzer` - Coverage analysis, gap detection, pattern detection, auto-annotation
- `PansyMerger` - Merge base + overlay files with parallel operations
- Modern .NET 10 / C# 14
- Cross-platform (Windows, Linux, macOS)
- Comprehensive xUnit test coverage (328 tests)
- Batch insert APIs for high-throughput metadata ingestion

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

# Analyze coverage and detect patterns
dotnet run --project src/Pansy.Cli -- analyze game.pansy game.nes --patterns

# Merge two Pansy files (base + overlay)
dotnet run --project src/Pansy.Cli -- merge base.pansy overlay.pansy -o merged.pansy
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
	Console.WriteLine($"${address:x4}: {name}");
}

// Get cross-references to a specific address
var refsTo = pansy.CrossReferences
	.Where(x => x.To == 0x8100)
	.ToList();

Console.WriteLine($"References to $8100: {refsTo.Count}");

// Analyze coverage and detect patterns
var romData = File.ReadAllBytes("game.nes");
var analysis = PansyAnalyzer.Analyze(pansy, romData, detectPatterns: true);
Console.WriteLine($"Coverage: {analysis.CoveragePercent:F1}%");
Console.WriteLine($"Patterns found: {analysis.DetectedPatterns.Count}");

// Generate enriched annotations from analysis
var annotated = PansyAnalyzer.GenerateAnnotations(pansy, analysis);
File.WriteAllBytes("game-annotated.pansy", annotated);

// Merge two Pansy files
var basePansy = new PansyLoader(File.ReadAllBytes("base.pansy"));
var overlay = new PansyLoader(File.ReadAllBytes("overlay.pansy"));
var merged = PansyMerger.Merge(basePansy, overlay);
File.WriteAllBytes("merged.pansy", merged.Generate());
```

## 📖 Documentation

- [File Format Specification](docs/FILE-FORMAT.md) - Complete format documentation with platform-specific details
- [CLI Reference](docs/CLI-REFERENCE.md) - Comprehensive command-line tool guide
- [Examples](docs/EXAMPLES.md) - Workflow guides and use cases
- [CI/CD Policy](docs/CI-CD-POLICY.md) - Continuous integration and deployment guidelines
- [Contributing Guide](CONTRIBUTING.md) - Contribution guidelines and development workflow

### Internal Documentation

- [Development Docs](~docs/) - Integration summaries, session logs, and chat logs
- [Planning](~Plans/) - Technical plans, roadmaps, and research documents

## 🔗 Integration

Pansy integrates with:

- **[Peony](https://github.com/TheAnsarya/peony)** - Disassembler (generates Pansy files)
- **[Poppy](https://github.com/TheAnsarya/poppy)** - Assembler (uses Pansy for symbols)
- **[GameInfo](https://github.com/TheAnsarya/GameInfo)** - ROM hacking toolkit
- **[Nexen](https://github.com/TheAnsarya/Nexen)** - Multi-system emulator (exports Pansy metadata)
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
| Sega Master System | 0x06 | ✅ Full |
| PC Engine | 0x07 | ✅ Full |
| Atari 2600 | 0x08 | ✅ Full |
| Atari Lynx | 0x09 | ✅ Full |
| WonderSwan | 0x0a | ✅ Full |
| Neo Geo | 0x0b | ✅ Full |
| SPC700 | 0x0c | ✅ Full |
| Commodore 64 | 0x0d | ✅ Full |
| MSX | 0x0e | ✅ Full |
| Atari 7800 | 0x0f | ✅ Full |
| Atari 8-bit | 0x10 | ✅ Full |
| Apple II | 0x11 | ✅ Full |
| ZX Spectrum | 0x12 | ✅ Full |
| ColecoVision | 0x13 | ✅ Full |
| Intellivision | 0x14 | ✅ Full |
| Vectrex | 0x15 | ✅ Full |
| Sega Game Gear | 0x16 | ✅ Full |
| Sega 32X | 0x17 | ✅ Full |
| Sega CD | 0x18 | ✅ Full |
| Virtual Boy | 0x19 | ✅ Full |
| Amstrad CPC | 0x1a | ✅ Full |
| BBC Micro | 0x1b | ✅ Full |
| Commodore VIC-20 | 0x1c | ✅ Full |
| Commodore Plus/4 | 0x1d | ✅ Full |
| Commodore 128 | 0x1e | ✅ Full |
| Custom | 0xff | ✅ Full |

## 🏗️ Architecture

```text
Pansy/
├── src/
│   ├── Pansy.Core/              # Core library (format I/O)
│   ├── Pansy.UI/                # Avalonia desktop app
│   └── Pansy.Cli/               # Command-line tools
├── tests/
│   ├── Pansy.Core.Tests/        # xUnit tests (328 tests)
│   └── Pansy.Core.Benchmarks/   # BenchmarkDotNet suite (70 benchmarks)
└── docs/                        # Documentation
```

## 🤝 Contributing

Contributions welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📜 License

This is free and unencumbered software released into the public domain. See [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- Inspired by Mesen's MLB format and FCEUX's NL files
- Built with love for the ROM hacking and retro gaming community
- Special thanks to contributors and testers

## 🌷 Integrated Pipeline

Pansy is the metadata backbone of the **Flower Toolchain** — an integrated pipeline for playing, debugging, disassembling, editing, and rebuilding retro games:

| Stage | Tool | Pansy Role |
|-------|------|------------|
| 1. Play & Debug | [Nexen](https://github.com/TheAnsarya/Nexen) | Export CDL, symbols, CPU state |
| 2. Disassemble | [Peony](https://github.com/TheAnsarya/peony) | Import hints, export analysis |
| 3. Edit & Document | Editor + Pansy UI | View/edit all metadata |
| 4. Build | [Poppy](https://github.com/TheAnsarya/poppy) | Import symbols, export CDL |
| 5. Verify | [Game Garden](https://github.com/TheAnsarya/game-garden) | Roundtrip validation |

See the [Integrated Pipeline Master Plan](~Plans/integrated-pipeline-master-plan.md) for architecture details.

## 🔗 Related Projects

- **[Nexen](https://github.com/TheAnsarya/Nexen)** - Multi-system emulator & debugger
- **[Peony](https://github.com/TheAnsarya/peony)** - Multi-system disassembler
- **[Poppy](https://github.com/TheAnsarya/poppy)** - Multi-system assembler
- **[Game Garden](https://github.com/TheAnsarya/game-garden)** - Games disassembly & recompilation
- **[GameInfo](https://github.com/TheAnsarya/GameInfo)** - ROM hacking toolkit
- **[BPS-Patch](https://github.com/TheAnsarya/bps-patch)** - Binary patching system
