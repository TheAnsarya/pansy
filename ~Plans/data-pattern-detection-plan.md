# Pansy Data Pattern Detection — Plan

## Status: DEFERRED — Use Hard Data First

## Key Insight

**Statistical pattern detection is premature.** Pansy already has rich structured metadata
that provides hard data about ROM contents:

- **CDL flags** — CODE, DATA, JUMP_TARGET, SUB_ENTRY, OPCODE, DRAWN, READ, INDIRECT
- **Symbols** — Label, Constant, Enum, Struct, Function, InterruptVector, etc.
- **Comments** — Inline, Block, Todo (human annotations)
- **Memory regions** — Named typed regions (ROM, RAM, VRAM, IO, SRAM, WRAM)
- **Cross-references** — Jsr, Jmp, Branch, Read, Write with from/to addresses
- **Source map** — File/line mappings back to assembly source
- **Data types** — Typed data annotations

The correct approach is to **leverage this existing structured metadata** before resorting
to entropy analysis and statistical heuristics.

## Correct Priority Order

### Phase 1: Metadata-Driven Analysis (Use What We Have)

Use existing Pansy sections to derive insights:

1. **CDL-driven classification** — Bytes marked CODE vs DATA vs DRAWN vs READ
   give definitive type information. No heuristics needed.
2. **Symbol-informed regions** — Symbols with types (Function, Struct, Enum) define
   data structure boundaries.
3. **Cross-reference graphs** — Build call graphs, data dependency graphs from xrefs.
4. **Memory region boundaries** — Named regions define ROM layout definitively.
5. **Comment mining** — Extract TODO items, known issues, documentation from comments.

### Phase 2: Gap Analysis (What CDL Doesn't Cover)

After Phase 1, identify **unclassified bytes** — ROM regions with no CDL flags, no symbols,
no cross-references. These are the candidates for statistical analysis.

Typical gaps in CDL coverage:
- Lookup tables (data arrays accessed via indexed addressing)
- Graphics tiles (CHR data, sprite data, tilemaps)
- Text strings (game dialogue, menu text)
- Compressed data blocks (Huffman, LZ, RLE)
- Audio data (samples, sequence data)
- Unused/padding regions (0x00 or 0xFF fill)

### Phase 3: Statistical Detection (Only for Gaps)

Apply heuristics **only** to unclassified regions:

1. **Fill detection** — Regions of repeated single byte (0x00, 0xFF padding)
2. **String detection** — ASCII/shift-JIS text in data regions
3. **Tile detection** — 2bpp/4bpp tile patterns (NES CHR, SNES tiles)
4. **Pointer table detection** — Sequences of valid addresses for the platform
5. **Compressed block detection** — High-entropy regions after known data
6. **Repeated array detection** — Fixed-stride patterns (stat tables, level data)

### Phase 4: Platform-Specific Patterns

Each platform has known data layout conventions:

- **NES**: CHR-ROM is always tiles, PRG-ROM 16K banks, vectors at $FFFA-$FFFF
- **SNES**: LoROM/HiROM layout, DMA tables for VRAM/CGRAM/OAM transfers
- **GB**: Tile data at $8000-$97FF, OAM at $FE00-$FE9F, fixed bank layout
- **GBA**: ARM/Thumb code sections, LZ77 compressed data marked by header bytes

## Architecture

### Recommended Design

```
PansyAnalyzer (new class)
├── AnalyzeFromMetadata(PansyFile)     ← Phase 1: uses existing sections
│   ├── ClassifyFromCdl()              ← CODE/DATA/DRAWN classification
│   ├── BuildSymbolMap()               ← Symbol-informed boundaries
│   ├── BuildCrossRefGraph()           ← Call graph / data flow
│   └── IdentifyGaps()                 ← Find unclassified regions
│
├── AnalyzeGaps(gaps, romData)         ← Phase 2-3: statistical only for gaps
│   ├── DetectFills()
│   ├── DetectStrings()
│   ├── DetectTiles()
│   ├── DetectPointerTables()
│   ├── DetectCompressedBlocks()
│   └── DetectRepeatedArrays()
│
└── MergeResults()                     ← Combine all findings
    └── GenerateAnnotations()          ← Add to Pansy file as symbols/comments
```

### Integration Points

1. **Input**: `PansyFile` loaded via `PansyLoader` + raw ROM data
2. **Output**: Enriched `PansyFile` with additional symbols, comments, data types
3. **CLI**: `pansy analyze --input file.pansy --rom game.nes --output enriched.pansy`
4. **UI**: Analysis panel showing coverage statistics and detected patterns
5. **API**: `PansyAnalyzer.Analyze(PansyFile, byte[] rom)` returns `AnalysisResult`

## Metrics

### Coverage Score

Given a ROM of N bytes, the **coverage score** measures how much is classified:

```
CoverageScore = (ClassifiedBytes / TotalBytes) × 100%

Where ClassifiedBytes = Σ(bytes with CDL flags OR symbols OR data type annotations)
```

### Gap Report

```
GapReport:
- Total ROM: 524,288 bytes (512 KB)
- CDL classified: 412,000 bytes (78.6%)
- Symbol-covered: 380,000 bytes (72.5%)
- Unclassified gaps: 112,288 bytes (21.4%)
  - 0x20000-0x2FFFF: 64 KB gap (likely CHR tile data)
  - 0x3F000-0x3FFFF: 4 KB gap (likely lookup tables)
  - 0x7FFF0-0x7FFFF: 16 bytes (vectors — should be labeled)
```

## Sub-Issues for Implementation

### Phase 1 Sub-Issues (Priority: HIGH — use hard data)

- **P1.1** Coverage analyzer — Calculate CDL coverage statistics
- **P1.2** Gap finder — Identify unclassified ROM regions
- **P1.3** Cross-reference graph builder — Build call/data flow graphs
- **P1.4** Symbol boundary analyzer — Infer data structure sizes from symbol gaps
- **P1.5** CLI `analyze` command — Basic analysis with coverage report

### Phase 2 Sub-Issues (Priority: MEDIUM — fill gaps)

- **P2.1** Fill detection — Padding/unused region identification
- **P2.2** String detection — ASCII/Shift-JIS text finder
- **P2.3** Platform tile detection — NES CHR, SNES tiles, GB tiles
- **P2.4** Pointer table detection — Platform-aware address arrays

### Phase 3 Sub-Issues (Priority: LOW — advanced heuristics)

- **P3.1** Compressed block detection — Entropy-based with platform LZ headers
- **P3.2** Repeated array detection — Fixed-stride pattern finder
- **P3.3** Auto-annotation — Generate symbols/comments from detected patterns
- **P3.4** UI integration — Analysis panel in Pansy.UI

## Dependencies

- Requires: PansyLoader, PansyWriter (existing)
- Requires: Platform-specific knowledge (NES/SNES/GB memory maps)
- Benefits from: Nexen CDL export, Peony disassembly output
- Produces: Enriched Pansy files for Peony re-analysis

## Decision: Why Not Now?

1. **No real ROM test data** — Can't validate pattern detection without actual game ROMs
2. **CDL data is more reliable** — Emulator-generated CDL from Nexen is ground truth
3. **Peony already classifies** — Disassembler output provides CODE/DATA classification
4. **Statistical methods need tuning per platform** — High investment for uncertain returns
5. **Better to improve CDL export** — Make Nexen's CDL more comprehensive first

## Future Trigger

Implement this when:
- Multiple Pansy files exist with CDL gaps > 20%
- Users request "what's in these gaps?" functionality
- Peony needs better initialization hints for unanalyzed regions
- UI can visualize coverage heat maps
