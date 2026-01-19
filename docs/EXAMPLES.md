# Pansy Example Workflows

This guide demonstrates common workflows and use cases for Pansy in ROM hacking and retro game development.

## Table of Contents

1. [Basic Disassembly Workflow](#basic-disassembly-workflow)
2. [Collaborative Analysis](#collaborative-analysis)
3. [ROM Hack Development](#rom-hack-development)
4. [Comparing ROM Versions](#comparing-rom-versions)
5. [Integration with Tools](#integration-with-tools)
6. [Advanced Workflows](#advanced-workflows)

---

## Basic Disassembly Workflow

### Scenario: Analyzing a new ROM

**Goal:** Create comprehensive metadata for a ROM you're disassembling.

**Steps:**

1. **Initial Analysis** - Use Peony to generate initial Pansy file:
```bash
# Disassemble ROM with Peony
peony disasm game.nes --output game.pasm --pansy game.pansy
```

2. **Review Metadata** - Check what was automatically detected:
```bash
# View file info
dotnet run --project src/Pansy.Cli -- info game.pansy

# List all auto-detected symbols
dotnet run --project src/Pansy.Cli -- symbols game.pansy > symbols.txt
```

3. **Manual Enhancement** - Add/improve symbols using code:
```csharp
using Pansy.Core;

// Load existing analysis
var data = File.ReadAllBytes("game.pansy");
var existing = new PansyLoader(data);

// Create new writer with existing data
var writer = new PansyWriter {
	Platform = existing.Platform,
	RomSize = existing.RomSize,
	RomCrc32 = existing.RomCrc32,
	ProjectName = "My Analysis",
	Author = "Your Name"
};

// Copy existing symbols
foreach (var (addr, name) in existing.Symbols) {
	writer.AddSymbol(addr, name);
}

// Add new symbols based on your findings
writer.AddSymbol(0x8200, "CheckCollision");
writer.AddSymbol(0x8250, "UpdatePosition");
writer.AddComment(0x8200, "Returns 1 in A if collision detected");

// Add cross-references
writer.AddCrossReference(new CrossReference(0x8100, 0x8200, CrossRefType.Jsr));

// Save enhanced analysis
var enhanced = writer.Generate();
File.WriteAllBytes("game-enhanced.pansy", enhanced);
```

4. **Verify Changes** - Compare to original:
```bash
dotnet run --project src/Pansy.Cli -- diff game.pansy game-enhanced.pansy
```

5. **Reassemble** - Use with Poppy to rebuild:
```bash
poppy game.pasm --pansy game-enhanced.pansy --output game-rebuild.nes
```

---

## Collaborative Analysis

### Scenario: Multiple people analyzing the same ROM

**Goal:** Share analysis work without conflicts.

**Workflow:**

1. **Initial Setup** - Person A starts analysis:
```csharp
var writer = new PansyWriter {
	Platform = PansyLoader.PLATFORM_NES,
	RomSize = 0x20000,
	RomCrc32 = CalculateCRC32("game.nes"),
	ProjectName = "Game Analysis Project",
	Author = "Team"
};

// Person A analyzes first bank ($8000-$BFFF)
writer.AddSymbol(0x8000, "Reset_Vector");
writer.AddSymbol(0x8100, "Init_PPU");
writer.AddMemoryRegion(new MemoryRegion(
	0x8000, 0xBFFF, 1, 0, "PRG Bank 0"
));

File.WriteAllBytes("bank0-analysis.pansy", writer.Generate());
```

2. **Parallel Work** - Person B works on second bank:
```csharp
var writer = new PansyWriter {
	Platform = PansyLoader.PLATFORM_NES,
	RomSize = 0x20000,
	RomCrc32 = CalculateCRC32("game.nes"),
	ProjectName = "Game Analysis Project",
	Author = "Team"
};

// Person B analyzes second bank ($C000-$FFFF)
writer.AddSymbol(0xC000, "Music_Engine");
writer.AddSymbol(0xC100, "Play_Sound");
writer.AddMemoryRegion(new MemoryRegion(
	0xC000, 0xFFFF, 1, 1, "PRG Bank 1"
));

File.WriteAllBytes("bank1-analysis.pansy", writer.Generate());
```

3. **Merge Analyses** - Combine work:
```csharp
// Load both analyses
var bank0 = new PansyLoader(File.ReadAllBytes("bank0-analysis.pansy"));
var bank1 = new PansyLoader(File.ReadAllBytes("bank1-analysis.pansy"));

// Create merged writer
var merged = new PansyWriter {
	Platform = bank0.Platform,
	RomSize = bank0.RomSize,
	RomCrc32 = bank0.RomCrc32,
	ProjectName = "Game Analysis Project (Merged)",
	Author = "Team"
};

// Merge symbols from both
foreach (var (addr, name) in bank0.Symbols) {
	merged.AddSymbol(addr, name);
}
foreach (var (addr, name) in bank1.Symbols) {
	merged.AddSymbol(addr, name);
}

// Merge memory regions
foreach (var region in bank0.MemoryRegions) {
	merged.AddMemoryRegion(region);
}
foreach (var region in bank1.MemoryRegions) {
	merged.AddMemoryRegion(region);
}

File.WriteAllBytes("complete-analysis.pansy", merged.Generate());
```

4. **Verify Merge** - Check for conflicts:
```bash
# Compare merged with originals
dotnet run --project src/Pansy.Cli -- diff bank0-analysis.pansy complete-analysis.pansy
dotnet run --project src/Pansy.Cli -- diff bank1-analysis.pansy complete-analysis.pansy
```

---

## ROM Hack Development

### Scenario: Building a ROM hack with new code

**Goal:** Track symbols and metadata as you develop.

**Workflow:**

1. **Set Up Project** - Initialize Pansy alongside source:
```
my-rom-hack/
├── src/
│   ├── main.pasm
│   ├── graphics.pasm
│   └── sound.pasm
├── assets/
│   └── *.chr, *.pal
├── build/
│   ├── game.nes
│   └── game.pansy
└── docs/
	└── memory-map.md
```

2. **Development Cycle**:

```bash
# 1. Edit source code
vim src/main.pasm

# 2. Assemble with Pansy generation
poppy src/main.pasm \
	--output build/game.nes \
	--pansy build/game.pansy \
	--symbols build/symbols.sym

# 3. Test in emulator
mesen build/game.nes

# 4. Review generated symbols
dotnet run --project src/Pansy.Cli -- symbols build/game.pansy

# 5. Find specific symbols during debugging
dotnet run --project src/Pansy.Cli -- find build/game.pansy "PlayerState"
```

3. **Cross-Reference Analysis** - Find who calls a function:
```bash
# Convert symbol name to address first
dotnet run --project src/Pansy.Cli -- symbols build/game.pansy | grep UpdatePlayer
# Output: $8450   UpdatePlayer

# Find cross-references (32848 = $8450 in decimal)
dotnet run --project src/Pansy.Cli -- xrefs build/game.pansy 33872
```

4. **Version Tracking** - Compare builds:
```bash
# After making changes
poppy src/main.pasm --output build/game-v2.nes --pansy build/game-v2.pansy

# See what changed
dotnet run --project src/Pansy.Cli -- diff build/game.pansy build/game-v2.pansy
```

---

## Comparing ROM Versions

### Scenario: Analyzing different regional versions

**Goal:** Identify differences between USA and Japan versions.

**Workflow:**

1. **Analyze Both Versions**:
```bash
# Disassemble USA version
peony disasm game-usa.nes --output usa.pasm --pansy game-usa.pansy

# Disassemble Japan version
peony disasm game-jpn.nes --output jpn.pasm --pansy game-jpn.pansy
```

2. **Compare Analyses**:
```bash
dotnet run --project src/Pansy.Cli -- diff game-usa.pansy game-jpn.pansy > differences.txt
```

3. **Identify Changes Programmatically**:
```csharp
var usa = new PansyLoader(File.ReadAllBytes("game-usa.pansy"));
var jpn = new PansyLoader(File.ReadAllBytes("game-jpn.pansy"));

// Find symbols unique to each version
var usaOnlySymbols = usa.Symbols.Keys.Except(jpn.Symbols.Keys);
var jpnOnlySymbols = jpn.Symbols.Keys.Except(usa.Symbols.Keys);

Console.WriteLine("USA-only symbols:");
foreach (var addr in usaOnlySymbols) {
	Console.WriteLine($"  ${addr:X4}: {usa.GetSymbol(addr)}");
}

Console.WriteLine("\nJapan-only symbols:");
foreach (var addr in jpnOnlySymbols) {
	Console.WriteLine($"  ${addr:X4}: {jpn.GetSymbol(addr)}");
}

// Compare code coverage
var (usaCode, usaData, usaTotal, usaCoverage) = usa.GetCoverageStats();
var (jpnCode, jpnData, jpnTotal, jpnCoverage) = jpn.GetCoverageStats();

Console.WriteLine($"\nUSA Coverage: {usaCoverage:F1}%");
Console.WriteLine($"Japan Coverage: {jpnCoverage:F1}%");
```

---

## Integration with Tools

### Mesen Integration (Future)

Track analysis alongside debugging:

```bash
# Export Pansy to Mesen label file
dotnet run --project src/Pansy.Cli -- convert game.pansy --to mesen --output game.mlb

# Import Mesen labels to Pansy
dotnet run --project src/Pansy.Cli -- convert game.mlb --from mesen --output game.pansy
```

### GitHub Integration

Use in CI/CD for ROM hack projects:

```yaml
# .github/workflows/build.yml
name: Build ROM Hack

on: [push, pull_request]

jobs:
  build:
	runs-on: ubuntu-latest
	steps:
	  - uses: actions/checkout@v3
	  
	  - name: Setup .NET
		uses: actions/setup-dotnet@v3
		with:
		  dotnet-version: '10.0.x'
	  
	  - name: Build ROM
		run: poppy src/main.pasm --output build/game.nes --pansy build/game.pansy
	  
	  - name: Verify Pansy metadata
		run: |
		  dotnet run --project tools/Pansy.Cli -- info build/game.pansy
		  
		  # Check symbol count meets minimum
		  SYMBOL_COUNT=$(dotnet run --project tools/Pansy.Cli -- symbols build/game.pansy | grep "Total:" | awk '{print $2}')
		  if [ "$SYMBOL_COUNT" -lt 100 ]; then
			echo "Error: Expected at least 100 symbols, found $SYMBOL_COUNT"
			exit 1
		  fi
	  
	  - name: Upload artifacts
		uses: actions/upload-artifact@v3
		with:
		  name: rom-with-metadata
		  path: |
			build/game.nes
			build/game.pansy
```

---

## Advanced Workflows

### Automated Symbol Detection

Use pattern matching to auto-generate symbols:

```csharp
var rom = File.ReadAllBytes("game.nes");
var writer = new PansyWriter {
	Platform = PansyLoader.PLATFORM_NES,
	RomSize = (uint)rom.Length,
	RomCrc32 = CalculateCRC32(rom)
};

// Scan for common patterns
for (int i = 0; i < rom.Length - 3; i++) {
	// Find JSR instructions ($20)
	if (rom[i] == 0x20) {
		ushort target = (ushort)(rom[i + 1] | (rom[i + 2] << 8));
		writer.MarkAsSubroutine((uint)target);
		writer.AddCrossReference(new CrossReference(
			(uint)i, (uint)target, CrossRefType.Jsr
		));
	}
	
	// Find JMP instructions ($4C)
	if (rom[i] == 0x4C) {
		ushort target = (ushort)(rom[i + 1] | (rom[i + 2] << 8));
		writer.MarkAsJumpTarget((uint)target);
		writer.AddCrossReference(new CrossReference(
			(uint)i, (uint)target, CrossRefType.Jmp
		));
	}
}

File.WriteAllBytes("auto-detected.pansy", writer.Generate());
```

### Incremental Analysis

Update existing analysis without losing work:

```csharp
// Load existing
var existing = new PansyLoader(File.ReadAllBytes("game.pansy"));

// Create updated writer
var writer = new PansyWriter {
	Platform = existing.Platform,
	RomSize = existing.RomSize,
	RomCrc32 = existing.RomCrc32,
	ProjectName = existing.ProjectName,
	Author = existing.Author,
	ProjectVersion = IncrementVersion(existing.ProjectVersion)
};

// Copy all existing data
CopyAllData(existing, writer);

// Add only new findings
writer.AddSymbol(0x9000, "NewFunction");
writer.AddComment(0x9000, "Discovered during analysis");

// Save update
File.WriteAllBytes("game.pansy", writer.Generate());
```

### Data-Driven Documentation

Generate memory map docs from Pansy:

```csharp
var pansy = new PansyLoader(File.ReadAllBytes("game.pansy"));

using var md = File.CreateText("memory-map.md");
md.WriteLine("# Memory Map");
md.WriteLine();

foreach (var region in pansy.MemoryRegions.OrderBy(r => r.Start)) {
	md.WriteLine($"## {region.Name}");
	md.WriteLine($"**Range:** ${region.Start:X4}-${region.End:X4}");
	md.WriteLine($"**Bank:** {region.Bank}");
	md.WriteLine();
	
	// List symbols in this region
	var symbolsInRegion = pansy.Symbols
		.Where(kv => kv.Key >= region.Start && kv.Key <= region.End)
		.OrderBy(kv => kv.Key);
	
	if (symbolsInRegion.Any()) {
		md.WriteLine("### Symbols:");
		md.WriteLine();
		md.WriteLine("| Address | Name |");
		md.WriteLine("|---------|------|");
		foreach (var (addr, name) in symbolsInRegion) {
			md.WriteLine($"| ${addr:X4} | {name} |");
		}
		md.WriteLine();
	}
}
```

---

## Best Practices

1. **Version Control** - Track `.pansy` files alongside source code
2. **Consistent Naming** - Use clear, descriptive symbol names
3. **Comments** - Add comments for non-obvious code sections
4. **Regular Backups** - Save incremental versions during analysis
5. **Team Coordination** - Divide work by memory regions to avoid conflicts
6. **Automated Checks** - Use CI to verify metadata quality
7. **Documentation** - Keep memory maps and function lists up to date

## See Also

- [CLI Reference](CLI-REFERENCE.md) - Detailed command documentation
- [File Format](FILE-FORMAT.md) - Format specification
- [API Reference](../src/Pansy.Core/README.md) - Library usage
