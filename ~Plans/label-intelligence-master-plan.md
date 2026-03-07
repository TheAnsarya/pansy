# 🌼 Label Intelligence Master Plan

> **Epic**: Intelligent Label Generation & Analysis for the Flower Toolchain
>
> **Scope**: Pansy (core), Peony (disassembler), Poppy (assembler), Nexen (emulator), Game Garden (games)
>
> **Goal**: Transform bare addresses into meaningful, human-readable labels across the entire pipeline
> using hardware register databases, ROM pattern analysis, AI-assisted naming, and internet data sources.

## Problem Statement

Currently, disassembled code is full of raw hex addresses like `$2000`, `$4014`, `$ff40`.
A developer reading `sta $2000` must look up that `$2000` is `PPUCTRL` on NES — this mental
overhead slows reverse engineering dramatically.

### Current State

| Component | Labels Exist? | Details |
|-----------|---------------|---------|
| **Pansy PlatformDefaults** | Lynx only | ~40 hardware register symbols; NES/SNES/GB/GBA/Atari 2600 have regions but **zero** symbol tables |
| **Nexen DefaultLabelHelper** | All 8 platforms | ~350+ hardware register labels with names AND descriptions — the richest source |
| **Peony Disassembler** | Basic | Uses Pansy symbols if present, but has no built-in register knowledge |
| **Poppy Assembler** | None | Imports Pansy symbols via PansyImporter, no built-in knowledge |
| **PansyAnalyzer** | Pattern detection | Detects Fill, AsciiString, PointerTable, TileData in ROM gaps — no label generation |

### Gap Analysis

1. **Pansy library has no register labels for 7 of 8 platforms** — only Lynx
2. **Nexen's rich register data is trapped in the emulator** — not shared with Peony/Poppy
3. **No automatic label generation** from ROM structure analysis
4. **No internet data integration** — DataCrystal, NESdev Wiki, etc. not leveraged
5. **PansyAnalyzer detects patterns but doesn't name them** — "PointerTable at $8000" but no label

## Architecture Overview

```
Internet Sources                    ROM Binary
(DataCrystal, NESdev Wiki)             |
        |                              v
        v                    +-----------------+
+------------------+         | PansyAnalyzer   |
| WebLabelProvider |         | (pattern detect)|
| (scrape/cache)   |         +---------+-------+
+--------+---------+                   |
         |                             v
         |              +---------------------------+
         +----------->  | LabelIntelligenceEngine   |
                        | (merge, deduplicate,      |
         +----------->  |  resolve conflicts,       |
         |              |  generate names)           |
+--------+---------+    +-------------+-------------+
| PlatformDefaults |                  |
| (hw registers)   |                  v
+------------------+         +------------------+
                             | .pansy file      |
                             | (enriched labels)|
                             +--------+---------+
                                      |
                    +-----------------+------------------+
                    |                 |                   |
                    v                 v                   v
              Peony (disasm)   Poppy (build)      Nexen (debug)
              Better .pasm     Richer symbols     Label overlay
```

## Work Breakdown

### Phase 1: Hardware Register Foundation (pansy)
**Port Nexen's register databases to Pansy PlatformDefaults**

This is the highest-value, lowest-risk work. Nexen's `DefaultLabelHelper.cs` already has
~350+ register definitions with names and descriptions. Port them to `PlatformDefaults.cs`
so ALL toolchain components benefit.

| Platform | Registers to Add | Source |
|----------|-----------------|--------|
| NES | PPU ($2000-$2007), APU ($4000-$4017), Controller ($4016-$4017) | DefaultLabelHelper.cs |
| SNES | PPU ($2100-$213F), CPU ($4016-$421F), DMA (8 channels), SPC ($F0-$FF) | DefaultLabelHelper.cs |
| Game Boy | LCD ($FF40-$FF4B), APU ($FF10-$FF26), System ($FF00-$FFFF) | DefaultLabelHelper.cs |
| GBA | Display ($4000000+), Sound, DMA, Timers, I/O, Interrupts | DefaultLabelHelper.cs |
| PC Engine | VDC, VCE, PSG, Timer, Joypad, IRQ | DefaultLabelHelper.cs |
| Master System | VDP, PSG, Memory Control, Joystick | DefaultLabelHelper.cs |
| WonderSwan | Display, Palettes, DMA, Sound, System, IRQ, Cartridge | DefaultLabelHelper.cs |
| Atari 2600 | TIA (read/write), RIOT, vectors | DefaultLabelHelper.cs |

**Sub-issues:**
- `[pansy] Add NES hardware register symbols to PlatformDefaults`
- `[pansy] Add SNES hardware register symbols to PlatformDefaults`
- `[pansy] Add Game Boy hardware register symbols to PlatformDefaults`
- `[pansy] Add GBA hardware register symbols to PlatformDefaults`
- `[pansy] Add PCE/SMS/WS hardware register symbols to PlatformDefaults`
- `[pansy] Add Atari 2600 hardware register symbols to PlatformDefaults`
- `[pansy] Add unit tests for all platform default symbols`

### Phase 2: Enhanced PlatformDefaults API (pansy)
**Extend the API to carry descriptions and symbol types**

Currently `GetDefaultSymbols()` returns `Dictionary<uint, string>` — address → name only.
Need to carry descriptions and proper SymbolType (Constant for registers).

**Changes:**
- New return type: `Dictionary<uint, DefaultSymbol>` where `DefaultSymbol` has Name, Description, SymbolType
- Or add `GetDefaultSymbolsWithDescriptions()` alongside existing API
- Add interrupt vector detection per platform (NMI, RESET, IRQ addresses)

**Sub-issues:**
- `[pansy] Design enhanced DefaultSymbol type with descriptions`
- `[pansy] Update GetDefaultSymbols to return rich symbol data`
- `[pansy] Add interrupt vector symbols per platform`

### Phase 3: ROM Pattern Analysis & Auto-Labeling (pansy)
**Extend PansyAnalyzer to generate labels from detected patterns**

PansyAnalyzer already detects Fill, AsciiString, PointerTable, TileData patterns in gaps.
Extend it to generate meaningful label names from these detections.

**Auto-label rules:**
- Interrupt vectors → `NMI_HANDLER`, `RESET_HANDLER`, `IRQ_HANDLER` (read from vector table)
- Pointer tables → `PTR_TABLE_$XXXX` (address-based)
- ASCII strings → `STR_$XXXX` or first N chars if readable
- Fill/padding → `PADDING_$XXXX`
- Jump targets with many refs → `SUB_$XXXX` or `FUNC_$XXXX`
- Data blocks → `DATA_$XXXX`

**Sub-issues:**
- `[pansy] Add auto-label generation to PansyAnalyzer`
- `[pansy] Platform-specific vector table label generation`
- `[pansy] Pointer table label generation from detected patterns`
- `[pansy] String literal label generation`

### Phase 4: Label Merge Engine (pansy)
**Resolve conflicts when multiple sources suggest different labels for the same address**

Priority order:
1. User-defined labels (highest — never overwrite)
2. Hardware register names (authoritative)
3. Internet/database-sourced labels
4. Auto-generated pattern labels (lowest)

**Sub-issues:**
- `[pansy] Design label merge/priority system`
- `[pansy] Implement LabelMergeEngine with conflict resolution`

### Phase 5: Internet Data Integration (pansy/game-garden)
**Scrape and cache label data from community sources**

Sources:
- **DataCrystal** (datacrystal.romhacking.net) — ROM maps, RAM maps per game
- **NESdev Wiki** (nesdev.org) — NES/SNES register reference
- **GBDEV Wiki** (gbdev.io) — Game Boy reference
- **GBAtek** (problemkaputt.de) — GBA/NDS technical reference
- **Romhacking.net** — Various disassembly projects

**Approach:**
- CLI tool or library that fetches + parses + caches label data
- Cache as `.json` files per game (CRC32/SHA1 indexed)
- Import into .pansy files on demand
- Respect rate limits, cache aggressively

**Sub-issues:**
- `[pansy] Design internet label data schema`
- `[pansy] Implement DataCrystal ROM map parser`
- `[pansy] Implement label cache system`
- `[game-garden] Integrate cached labels into game projects`

### Phase 6: AI-Assisted Label Naming (future)
**Use LLM analysis to suggest meaningful names for unlabeled code**

This is the most speculative phase. Ideas:
- Feed disassembled code context to an LLM
- Ask for suggested function/label names based on instruction patterns
- Human reviews and approves suggestions
- Store approved labels in .pansy file

**Approach considerations:**
- Local model (Ollama/llama.cpp) for privacy
- VS Code Copilot integration for inline suggestions
- Batch mode for processing entire ROMs
- Confidence scoring to highlight uncertain suggestions

**Sub-issues:**
- `[pansy] Research AI label suggestion approaches`
- `[peony] AI-assisted function naming prototype`
- `[Nexen] AI label suggestion UI integration`

### Phase 7: Pipeline Integration (all repos)
**Ensure enriched labels flow through the entire toolchain**

- **Peony**: Use Pansy PlatformDefaults + auto-labels when disassembling
- **Poppy**: Import enriched .pansy symbols for better build output
- **Nexen**: Show auto-generated labels in debugger, allow user override
- **Game Garden**: Pre-populate label databases per game project

**Sub-issues:**
- `[peony] Auto-populate hardware register labels during disassembly`
- `[poppy] Display label source (hw/auto/user/internet) in listings`
- `[Nexen] Show label source indicators in debugger`
- `[game-garden] Pre-populate per-game label databases`

## Data Sources Reference

### NES Hardware Registers
| Address | Name | Description |
|---------|------|-------------|
| $2000 | PPUCTRL | PPU Control Register 1 |
| $2001 | PPUMASK | PPU Control Register 2 |
| $2002 | PPUSTATUS | PPU Status Register |
| $2003 | OAMADDR | OAM Address |
| $2004 | OAMDATA | OAM Data |
| $2005 | PPUSCROLL | PPU Scroll Position |
| $2006 | PPUADDR | PPU Address |
| $2007 | PPUDATA | PPU Data |
| $4000-$4003 | SQ1_* | Square Wave 1 |
| $4004-$4007 | SQ2_* | Square Wave 2 |
| $4008-$400B | TRI_* | Triangle |
| $400C-$400F | NOISE_* | Noise |
| $4010-$4013 | DMC_* | Delta Modulation |
| $4014 | OAMDMA | OAM DMA |
| $4015 | SND_CHN | Sound Channel Control |
| $4016 | JOY1 | Joypad 1 |
| $4017 | JOY2/FRAMECTR | Joypad 2 / Frame Counter |
| $FFFA | NMI_VECTOR | NMI Vector |
| $FFFC | RESET_VECTOR | Reset Vector |
| $FFFE | IRQ_VECTOR | IRQ/BRK Vector |

### SNES Hardware Registers (partial — see DefaultLabelHelper.cs for full list)
- PPU: $2100-$213F (INIDISP through STAT78)
- CPU: $4016-$421F (JOYSER0 through MEMSEL)
- DMA: $4300-$437B (8 channels × 11 registers)
- SPC: $F0-$FF (TEST through IPL_ROM_ENABLE)

### Game Boy I/O Registers
- Joypad: $FF00 (JOYP)
- Serial: $FF01-$FF02 (SB, SC)
- Timer: $FF04-$FF07 (DIV, TIMA, TMA, TAC)
- Interrupts: $FF0F (IF), $FFFF (IE)
- Sound: $FF10-$FF26 (NR channels)
- LCD: $FF40-$FF4B (LCDC, STAT, SCY, SCX, LY, LYC, DMA, BGP, OBP0, OBP1, WY, WX)

## Implementation Timeline

1. **Phase 1** (hardware registers) — Immediate, high-value
2. **Phase 2** (enhanced API) — With Phase 1
3. **Phase 3** (auto-labeling) — After Phase 1+2
4. **Phase 4** (merge engine) — After Phase 3
5. **Phase 5** (internet data) — Parallel with Phase 3-4
6. **Phase 6** (AI naming) — Future exploration
7. **Phase 7** (pipeline) — Rolling integration

## Success Criteria

- [ ] All 8 platforms have hardware register symbols in PlatformDefaults
- [ ] Peony disassembly output uses register names instead of raw addresses
- [ ] Poppy builds display register names in listings
- [ ] Nexen debugger shows enriched labels from .pansy files
- [ ] PansyAnalyzer generates auto-labels for detected patterns
- [ ] DataCrystal integration provides per-game RAM/ROM labels
- [ ] Label merge engine resolves conflicts with clear priority
- [ ] Unit tests cover all new label data and analysis
