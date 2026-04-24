# 🌼 Pansy File Format Specification

> **Pansy** - Program ANalysis SYstem format for comprehensive assembly metadata exchange

**Version:** 1.0  
**Status:** Draft  
**Created:** 2026-01-19  
**Last Updated:** 2026-07-09

## Overview

🌼 Pansy is a binary metadata format for storing disassembly analysis data, designed for retro game development and ROM hacking. It provides a standardized way to share labels, comments, code/data classifications, and cross-reference information across different tools.

## Design Goals

- **Platform-agnostic**: Support all retro systems (NES, SNES, GB, GBA, Genesis, etc.)
- **Compact**: Binary format with optional compression
- **Fast**: Optimized for reading during assembly/disassembly
- **Extensible**: Version-tolerant with forward/backward compatibility
- **Complete**: Capture all analysis data (symbols, comments, xrefs, memory maps)
- **Tool-neutral**: No assumptions about specific assemblers or disassemblers

## File Structure

```text
┌──────────────────────────────────────┐
│           Header (32 bytes)          │
├──────────────────────────────────────┤
│                                      │
│          Section Table               │
│  ┌────────────────────────────────┐  │
│  │ Type, Offset, CompSize, Size   │  │ (16 bytes × N)
│  └────────────────────────────────┘  │
│               ...                    │
├──────────────────────────────────────┤
│                                      │
│         Section Data                 │
│  ┌────────────────────────────────┐  │
│  │   CODE_DATA_MAP (compressed)   │  │
│  ├────────────────────────────────┤  │
│  │   SYMBOLS (compressed)         │  │
│  ├────────────────────────────────┤  │
│  │   COMMENTS (uncompressed)      │  │
│  └────────────────────────────────┘  │
│               ...                    │
└──────────────────────────────────────┘
```

> **Note:** Section count is embedded in the header at offset 0x18. There is no footer;
> integrity checks should be performed at the application level.

## Header Format

**Size:** 32 bytes  
**Offset:** 0x0000

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0x00 | 8 | char[8] | Magic: "PANSY\0\0\0" |
| 0x08 | 2 | uint16 | Version (0x0100 = v1.0) |
| 0x0A | 2 | uint16 | Flags (see Flags section) |
| 0x0C | 1 | uint8 | Platform ID |
| 0x0D | 1 | uint8 | Reserved (must be 0) |
| 0x0E | 2 | uint16 | Reserved (must be 0) |
| 0x10 | 4 | uint32 | ROM size |
| 0x14 | 4 | uint32 | ROM CRC32 |
| 0x18 | 4 | uint32 | Section count |
| 0x1C | 4 | uint32 | Reserved (must be 0) |

## Platform IDs

| ID | Platform | CPU | Notes |
|----|----------|-----|-------|
| 0x01 | NES | 6502 | NTSC/PAL |
| 0x02 | SNES | 65816 | LoROM/HiROM/ExHiROM |
| 0x03 | Game Boy | Z80-like | GB/GBC/SGB |
| 0x04 | Game Boy Advance | ARM7TDMI | 32-bit |
| 0x05 | Sega Genesis | 68000 + Z80 | Mega Drive |
| 0x06 | Sega Master System | Z80 | Mark III |
| 0x07 | PC Engine | 6280 | TurboGrafx-16 |
| 0x08 | Atari 2600 | 6507 | VCS |
| 0x09 | Atari Lynx | 65C02 | Handheld |
| 0x0a | WonderSwan | V30MZ | Bandai |
| 0x0b | Neo Geo | 68000 + Z80 | MVS/AES |
| 0x0c | SPC700 | SPC700 | SNES audio |
| 0x0d | Commodore 64 | 6510 | C64 |
| 0x0e | MSX | Z80 | |
| 0x0f | Atari 7800 | 6502C | |
| 0x10 | Atari 8-bit | 6502 | 400/800/XL/XE |
| 0x11 | Apple II | 6502 | |
| 0x12 | ZX Spectrum | Z80 | Sinclair |
| 0x13 | ColecoVision | Z80 | |
| 0x14 | Intellivision | CP1610 | |
| 0x15 | Vectrex | 6809 | |
| 0x16 | Sega Game Gear | Z80 | |
| 0x17 | Sega 32X | SH-2 | |
| 0x18 | Sega CD | 68000 | |
| 0x19 | Virtual Boy | V810 | Nintendo VB |
| 0x1a | Amstrad CPC | Z80 | |
| 0x1b | BBC Micro | 6502 | |
| 0x1c | Commodore VIC-20 | 6502 | |
| 0x1d | Commodore Plus/4 | 7501 | |
| 0x1e | Commodore 128 | 8502 | |
| 0x1f | Fairchild Channel F | F8 | Video Entertainment System |
| 0xff | Custom | Varies | User-defined platform |

## Flags

**Type:** uint16 (2 bytes)

| Bit | Flag | Description |
|-----|------|-------------|
| 0 | COMPRESSED | Sections use DEFLATE compression |
| 1 | HAS_SOURCE_MAP | Includes source file mapping |
| 2 | HAS_CROSS_REFS | Contains cross-references section |
| 3 | DETAILED_CDL | Has detailed CDL data |
| 4 | HAS_CPU_STATE | Contains CPU state section (0x0009) |
| 5-15 | Reserved | Must be 0 |

## Section Types

### CODE_DATA_MAP (0x0001)

Per-byte classification flags, similar to CDL but extended.

**Byte flags:**

| Bit | Flag | Description |
|-----|------|-------------|
| 0 | CODE | Byte is code (opcode or operand) |
| 1 | DATA | Byte is data |
| 2 | JUMP_TARGET | Byte is a JMP destination |
| 3 | SUB_ENTRY | Byte is a JSR/CALL destination |
| 4 | OPCODE | Byte is an opcode (not operand) |
| 5 | DRAWN | Byte was rendered (graphics) |
| 6 | READ | Byte was read as data |
| 7 | INDIRECT | Accessed via indirect addressing |

### SYMBOLS (0x0002)

Label and constant definitions.

```text
Symbol Entry:
  Address: uint32 (24-bit address + 8-bit bank)
  Type: uint8 (label=1, constant=2, enum=3, struct=4)
  Flags: uint8
  NameLength: uint16
  Name: char[NameLength]
  ValueLength: uint16 (for constants)
  Value: int64 (for constants)
```

**Symbol Types:**

| Value | Type | Description |
|-------|------|-------------|
| 1 | LABEL | Code or data label |
| 2 | CONSTANT | Named constant value |
| 3 | ENUM | Enumeration member |
| 4 | STRUCT | Structure definition |
| 5 | MACRO | Macro definition |
| 6 | LOCAL | Local label (within scope) |
| 7 | ANONYMOUS | Anonymous label (+/-) |
| 8 | INTERRUPT_VECTOR | Interrupt/exception vector (NMI, IRQ, RESET, etc.) |
| 9 | FUNCTION | Subroutine/function entry point |

### COMMENTS (0x0003)

Per-address comments.

```text
Comment Entry:
  Address: uint32
  Type: uint8 (inline=1, block=2, todo=3)
  Length: uint16
  Text: char[Length]
```

### MEMORY_REGIONS (0x0004)

Memory segment definitions.

```text
Region Entry:
  StartAddress: uint32
  EndAddress: uint32
  Type: uint8 (see table below)
  Bank: uint8
  Flags: uint16
  NameLength: uint16
  Name: char[NameLength]
```

**Memory Region Types:**

| Value | Type | Description |
|-------|------|-------------|
| 0 | UNKNOWN | Unknown or unspecified region |
| 1 | ROM | Read-only memory |
| 2 | RAM | Random access memory |
| 3 | VRAM | Video RAM |
| 4 | IO | I/O registers |
| 5 | SRAM | Save RAM (battery-backed) |
| 6 | WRAM | Work RAM |
| 7 | OPEN_BUS | Open bus / unmapped |
| 8 | MIRROR | Mirror of another region |

### DATA_TYPES (0x0005)

Data structure definitions for tables, arrays, and other typed data regions.

**Entry layout** (variable size, minimum 15 bytes):

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| +0 | 4 | uint32 | Address (ROM offset where data starts) |
| +4 | 4 | uint32 | Length (total byte count of data region) |
| +8 | 2 | uint16 | ElementSize (size of each element in bytes) |
| +10 | 2 | uint16 | ElementCount (number of elements) |
| +12 | 1 | uint8 | Type (see Data Element Types below) |
| +13 | 2 | uint16 | NameLength (byte count, UTF-8) |
| +15 | N | char[] | Name (UTF-8, no null terminator) |

**Parsing:** No entry count header — entries repeat until section data is exhausted.

**Data Element Types:**

| Value | Type | Description |
|-------|------|-------------|
| 1 | BYTE | Single byte (uint8) |
| 2 | WORD | 16-bit word (uint16, little-endian) |
| 3 | LONG | 32-bit long (uint32, little-endian) |
| 4 | POINTER | Address/pointer (uint32, platform endianness) |
| 5 | STRING | Text/string data (variable length) |

**Example:** A 256-byte lookup table of uint16 values at `$c000`:

```text
Address:      00 c0 00 00       ($c000)
Length:        00 02 00 00       (512 bytes total)
ElementSize:  02 00             (2 bytes per element)
ElementCount: 00 01             (256 elements)
Type:         02                (WORD)
NameLength:   0c 00             (12 bytes)
Name:         "SinLookupTbl"   (UTF-8)
```

### CROSS_REFS (0x0006)

Cross-reference data for jump/call targets.

```text
CrossRef Entry:
  FromAddress: uint32
  ToAddress: uint32
  Type: uint8 (jsr=1, jmp=2, branch=3, read=4, write=5)
```

#### CDM vs CROSS_REFS Producer Contract

`CODE_DATA_MAP` and `CROSS_REFS` encode different guarantees and should both be emitted when available.

- `CODE_DATA_MAP` answers byte-local classification questions (`CODE`, `DATA`, `JUMP_TARGET`, `SUB_ENTRY`, `OPCODE`, etc.).
- `CROSS_REFS` answers graph questions (`from -> to`) for control flow and memory access edges.
- Producers should not treat one section as a lossless replacement for the other.

Required producer behavior:

1. Emit `CODE_DATA_MAP` flags for all known code/data bytes.
2. Emit `CROSS_REFS` edges for all known resolved source-target relationships.
3. For one-source-many-target control flow (jump tables, indirect dispatch), emit one `CrossRef Entry` per resolved target.
4. Keep output deterministic by sorting edges by `(FromAddress, ToAddress, Type)` and deduplicating exact duplicates.
5. Preserve backward compatibility by continuing to emit legacy `CROSS_REFS` entries even when richer grouped metadata is also available in tool-specific pipelines.

Binary example (single-target branch):

```text
CrossRef Entry bytes:
  FromAddress: 00 80 00 00    ($8000)
  ToAddress:   20 80 00 00    ($8020)
  Type:        03             (branch)
```

Binary example (one source, three jump-table targets):

```text
Entry A:
  FromAddress: 00 90 00 00    ($9000)
  ToAddress:   00 a0 00 00    ($a000)
  Type:        02             (jmp)

Entry B:
  FromAddress: 00 90 00 00    ($9000)
  ToAddress:   20 a0 00 00    ($a020)
  Type:        02             (jmp)

Entry C:
  FromAddress: 00 90 00 00    ($9000)
  ToAddress:   40 a0 00 00    ($a040)
  Type:        02             (jmp)
```

Compatibility notes:

- Nexen producers should continue exporting CDM flags and full per-target `CROSS_REFS` edges so legacy consumers remain correct.
- Peony consumers should seed disassembly entry points primarily from control-flow `CROSS_REFS` (`jsr`, `jmp`, `branch`) while using CDM flags for byte classification and non-graph hints.
- When sections disagree, treat `CROSS_REFS` as authoritative for explicit graph edges and `CODE_DATA_MAP` as authoritative for per-byte classification.

Consumer fallback guidance:

- If `CROSS_REFS` is missing, use `JUMP_TARGET` and `SUB_ENTRY` CDM flags as entry seeding hints.
- If `CODE_DATA_MAP` is missing, do not infer byte-level `CODE`/`DATA` flags from edges alone.
- If both are present, merge them without discarding information from either section.

### SOURCE_MAP (0x0007)

Maps ROM addresses to original source file locations. The section contains two parts:
a file path table followed by per-address mapping entries.

**Part 1 — Source File Table** (must come first):

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| +0 | 2 | uint16 | FileCount (number of source files) |

For each file (repeated `FileCount` times):

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| +0 | 2 | uint16 | PathLength (byte count, UTF-8) |
| +2 | N | char[] | Path (UTF-8, no null terminator) |

**Part 2 — Source Map Entries** (follow file table, fixed 10 bytes each):

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| +0 | 4 | uint32 | RomAddress (ROM byte offset) |
| +4 | 2 | uint16 | FileIndex (0-based index into file table) |
| +6 | 2 | uint16 | Line (1-based line number) |
| +8 | 2 | uint16 | Column (1-based column number) |

**Parsing:** Entries repeat after the file table until section data is exhausted.

**Example:** Two source files, one mapping entry:

```text
File table:
  FileCount:    02 00                     (2 files)
  PathLength:   08 00                     (8 bytes)
  Path:         "main.pasm"
  PathLength:   0e 00                     (14 bytes)
  Path:         "includes/io.pasm"

Mapping entry:
  RomAddress:   00 80 00 00               ($8000)
  FileIndex:    00 00                     (file 0 = main.pasm)
  Line:         0a 00                     (line 10)
  Column:       01 00                     (column 1)
```

**Usage:** Poppy generates source map entries during assembly. Peony can consume them
to annotate disassembly with original source locations. Useful for debugging rebuilt
ROMs — jump from a ROM address back to the `.pasm` source line that produced it.

### METADATA (0x0008)

Project metadata.

```text
Metadata:
  ProjectNameLength: uint16
  ProjectName: char[Length]
  AuthorLength: uint16
  Author: char[Length]
  VersionLength: uint16
  Version: char[Length]
  CreatedTimestamp: int64
  ModifiedTimestamp: int64
```

### BOOKMARKS (0x000a)

User-defined bookmarks for quick navigation.

```text
Bookmark Entry:
  Address: uint32
  Color: uint8
  NameLength: uint16
  Name: char[NameLength]
```

**Color:** Application-defined palette index (0 = default).

### CPU_STATE (0x0009)

Per-address CPU state snapshots for architectures with mode-dependent instruction decoding.
Essential for SNES (65816) where the M and X flags change instruction widths, and GBA where ARM/THUMB mode changes the instruction set.

This section is separate from `CODE_DATA_MAP`. Do not store CPU-width or CPU-mode metadata by overloading code/data-map bits.
See [CPU State Metadata](CPU-STATE.md) for canonical producer and consumer guidance.

```text
CPU State Entry (9 bytes, repeating):
  Address:    uint32  — CPU address where this state applies
  Flags:      uint8   — Bit 0: XFlag (1 = 8-bit index registers)
                        Bit 1: MFlag (1 = 8-bit accumulator)
                        Bits 2–7: reserved (0)
  DataBank:   uint8   — Data bank register (DBR, 65816-specific, 0 for other CPUs)
  DirectPage: uint16  — Direct page offset (65816-specific, 0 for other CPUs)
  CpuMode:    uint8   — 0 = Native65816, 1 = Emulation6502, 2 = ARM, 3 = THUMB
                        4 = M68000 (proposed), 5 = Z80 (proposed)
```

**Usage notes:**

- Only addresses where CPU state differs from the default need entries
- SNES: M/X flags determine whether `lda`, `ldx`, etc. read 1 or 2 bytes of immediate data
- GBA: ARM mode uses 32-bit instructions, THUMB uses 16-bit instructions
- Genesis: planned conventions use CpuMode `4` (M68000) and `5` (Z80) with platform-specific flag semantics documented in `CPU-STATE.md`
- Entries should be sorted by address for efficient lookup

## Compression

Section data is compressed using **DEFLATE** (System.IO.Compression.DeflateStream) with `CompressionLevel.Optimal`.
Uncompressed data is stored if the COMPRESSED flag is not set, or if compression increases the section size.
Each section is compressed independently — a section whose compressed size equals or exceeds its uncompressed size is stored raw.

## Platform-Specific Details

### NES (0x01)

**Memory Map:**

- $0000-$07FF: RAM (2KB, mirrored to $0800-$1FFF)
- $2000-$2007: PPU registers (mirrored to $2008-$3FFF)
- $4000-$4017: APU and I/O registers
- $4020-$FFFF: Cartridge space (PRG ROM/RAM)

**Special Considerations:**

- Bank switching via mappers (use bank byte in addresses)
- CHR data stored separately from PRG ROM
- Use `MEMORY_REGIONS` to define mapper-specific banks

### SNES (0x02)

**Memory Map:**

- $00:0000-$00:1FFF: RAM (8KB, mirrored)
- $00:2000-$00:7FFF: Hardware registers
- $00:8000-$FF:FFFF: ROM (varies by mapping mode)

**Mapping Modes:**

- **LoROM**: Banks $00-$7D/$80-$FD, $8000-$FFFF per bank
- **HiROM**: Banks $00-$7D/$80-$FD, $0000-$FFFF per bank
- **ExHiROM**: Banks $00-$FF with extended addressing

**Special Considerations:**

- Use 24-bit addresses (bank:offset)
- SPC700 audio coprocessor (separate Platform ID 0x0D)

### Game Boy (0x03)

**Memory Map:**

- $0000-$3FFF: ROM Bank 0 (16KB fixed)
- $4000-$7FFF: ROM Bank 1-N (16KB switchable)
- $8000-$9FFF: VRAM (8KB)
- $A000-$BFFF: External RAM (8KB, optional)
- $C000-$DFFF: Work RAM (8KB)
- $E000-$FDFF: Echo RAM (mirror of $C000-$DDFF)
- $FE00-$FE9F: OAM (sprite attributes)
- $FF00-$FF7F: I/O registers
- $FF80-$FFFE: High RAM (127 bytes)
- $FFFF: Interrupt Enable register

**Special Considerations:**

- Bank numbers stored in bank byte (up to 512 banks)
- GBC extended features (VRAM bank 1, extended palettes)

### Game Boy Advance (0x04)

**Memory Map:**

- $00000000-$00003FFF: BIOS ROM (16KB)
- $02000000-$0203FFFF: On-board WRAM (256KB)
- $03000000-$03007FFF: On-chip WRAM (32KB)
- $04000000-$040003FE: I/O registers
- $05000000-$050003FF: Palette RAM (1KB)
- $06000000-$06017FFF: VRAM (96KB)
- $07000000-$070003FF: OAM (1KB)
- $08000000-$09FFFFFF: Game ROM (32MB max)

**Special Considerations:**

- 32-bit ARM and 16-bit Thumb instructions
- Use CODE flags to distinguish instruction types

### Sega Genesis (0x05)

**Memory Map:**

- $000000-$3FFFFF: Cartridge ROM (4MB max)
- $400000-$7FFFFF: Reserved
- $800000-$9FFFFF: Reserved (used for SRAM mapping)
- $A00000-$A0FFFF: Z80 address space
- $C00000-$DFFFFF: VDP ports and RAM
- $E00000-$FFFFFF: RAM (64KB, mirrored)

**Special Considerations:**

- 68000 main CPU (16/32-bit instructions)
- Z80 sound CPU (separate address space, use Platform ID 0x06 or embed)
- VDP graphics data

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-19 | Initial specification |
| 1.0.1 | 2026-01-24 | Added platform-specific details |
| 1.0.2 | 2026-07-09 | Synced spec with implementation: fixed header layout (flags is uint16, section count in header at 0x18), corrected platform IDs to match PansyLoader constants, changed compression from zstd to DEFLATE, marked DATA_TYPES and SOURCE_MAP as reserved/unimplemented, removed footer (integrity checks at application level) |
| 1.0.3 | 2026-07-12 | Added INTERRUPT_VECTOR (8) and FUNCTION (9) symbol types; typed SymbolEntry/CommentEntry records preserve full type info through roundtrip; DRAWN/READ/INDIRECT flags now fully implemented in writer and loader |
| 1.0.4 | 2026-07-19 | DATA_TYPES and SOURCE_MAP sections now fully implemented; added BOOKMARKS section (0x000a); batch insert APIs; expanded platform table (0x0b–0x1e); 328 tests, 70 benchmarks |
| 1.0.5 | 2026-07-20 | Added CPU_STATE section (0x0009) for per-address processor state (M/X flags, data bank, direct page, CPU mode); HAS_CPU_STATE header flag (bit 4); supports SNES 65816 mode tracking and GBA ARM/THUMB switching; 332 tests |

## Comparison with Existing Formats

| Feature | CDL | DIZ | Pansy |
|---------|-----|-----|-------|
| Code/data flags | ✅ | ✅ | ✅ |
| Jump targets | Mesen only | ❌ | ✅ |
| Sub entry points | ✅ | ✅ | ✅ |
| Labels | ❌ | ✅ | ✅ |
| Comments | ❌ | ✅ | ✅ |
| Source mapping | ❌ | ❌ | ✅ |
| Cross-references | ❌ | ❌ | ✅ |
| Data types | ❌ | Limited | ✅ |
| CPU state (M/X, DBR) | ❌ | ✅ | ✅ |
| Bookmarks | ❌ | ❌ | ✅ |
| Multi-system | Limited | SNES only | ✅ |
| Compression | ❌ | gzip | DEFLATE |
| Binary format | ✅ | JSON | ✅ |

## File Extension

- Primary: `.pansy`
- Alternative: `.psy`

## Related Tools

- **Poppy** - `--pansy output.pansy` flag for generation
- **Peony** - `peony disasm --pansy input.pansy rom.nes` for consumption
- **GameInfo** - Import/export utilities

## Example Usage

### Poppy (Assembly)

```bash
# Generate ROM with Pansy metadata
poppy game.pasm --output game.nes --pansy game.pansy

# Full build with all outputs
poppy game.pasm \
	--output game.nes \
	--pansy game.pansy \
	--symbols game.sym \
	--listing game.lst
```

### Peony (Disassembly)

```bash
# Disassemble with Pansy metadata
peony disasm game.nes --pansy game.pansy --output game.pasm

# Use Pansy for improved accuracy
peony disasm game.nes \
	--pansy game.pansy \
	--labels game.sym \
	--output src/
```

## Implementation Notes

1. Sections can appear in any order
2. Unknown section types should be preserved but ignored
3. CRC32 uses IEEE polynomial (same as PNG/ZIP)
4. All multi-byte values are little-endian
5. Strings are length-prefixed (uint16 byte count) followed by UTF-8 data, no null terminator
6. String length fields represent **byte count**, not character count
7. Empty strings are valid (length = 0, no data bytes follow)
8. Sections with no entry count header parse entries until the section data is exhausted
9. Maximum string length: 65,535 bytes (uint16 max)

## Future Enhancements

### Planned for Version 1.1

- Additional section types for graphics metadata
- Improved compression with dictionary sharing
- Incremental update support
- Embedded source code snippets

### Under Consideration

- SQLite-based format option for large projects
- Differential/patch format for version control
- Encrypted sections for commercial projects
