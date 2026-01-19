# 🌼 Pansy File Format Specification

> **Pansy** - Program ANalysis SYstem format for comprehensive assembly metadata exchange

**Version:** 1.0  
**Status:** Draft  
**Created:** 2026-01-19  
**Last Updated:** 2026-01-24

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

```
┌──────────────────────────────────────┐
│           Header (32 bytes)          │
├──────────────────────────────────────┤
│         Section Count (uint32)       │
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
├──────────────────────────────────────┤
│          Footer (12 bytes)           │
│  - ROM CRC32     (4 bytes)           │
│  - Metadata CRC32 (4 bytes)          │
│  - File CRC32    (4 bytes)           │
└──────────────────────────────────────┘
```

## Header Format

**Size:** 32 bytes  
**Offset:** 0x0000

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0x00 | 8 | char[8] | Magic: "PANSY\0\0\0" |
| 0x08 | 2 | uint16 | Version (0x0100 = v1.0) |
| 0x0A | 1 | uint8 | Platform ID |
| 0x0B | 1 | uint8 | Flags |
| 0x0C | 4 | uint32 | ROM size |
| 0x10 | 4 | uint32 | ROM CRC32 |
| 0x14 | 8 | uint64 | Creation timestamp (Unix epoch) |
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
| 0x09 | Atari 7800 | 6502C | |
| 0x0A | Atari Lynx | 65C02 | Handheld |
| 0x0B | WonderSwan | V30MZ | Bandai |
| 0x0C | Neo Geo | 68000 + Z80 | MVS/AES |
| 0x0D | SPC700 | SPC700 | SNES audio |
| 0x0E | Famicom Disk System | 6502 | FDS |
| 0x0F | VirtualBoy | V810 | Nintendo VB |
| 0x10 | N64 | MIPS R4300i | Ultra 64 |
| 0x11 | PSX | MIPS R3000A | PlayStation |
| 0x12 | Saturn | SH-2 | Sega Saturn |
| 0x13 | Dreamcast | SH-4 | Sega Dreamcast |
| 0x14 | GameCube | PowerPC | Nintendo GC |
| 0x15 | PS2 | MIPS R5900 | PlayStation 2 |
| 0x16 | Xbox | Pentium III | Microsoft Xbox |
| 0x17 | Wii | PowerPC | Nintendo Wii |
| 0x18 | PS3 | Cell | PlayStation 3 |
| 0x19 | Xbox 360 | PowerPC | Microsoft 360 |
| 0x1A | Nintendo DS | ARM9 + ARM7 | Dual-screen |
| 0x1B | Nintendo 3DS | ARM11 | Stereoscopic |
| 0x1C | PSP | MIPS R4000 | PlayStation Portable |
| 0x1D | PS Vita | ARM Cortex-A9 | |
| 0x1E | Switch | ARM Cortex-A57 | Nintendo Switch |
| 0xFF | Custom | Varies | User-defined platform |

## Flags

| Bit | Flag | Description |
|-----|------|-------------|
| 0 | COMPRESSED | Sections use zstd compression |
| 1 | HAS_SOURCE_MAP | Includes source file mapping |
| 2 | HAS_DEBUG_INFO | Extended debug information |
| 3 | MULTI_BANK | Multiple ROM banks/segments |
| 4-7 | Reserved | Must be 0 |

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

```
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

### COMMENTS (0x0003)

Per-address comments.

```
Comment Entry:
  Address: uint32
  Type: uint8 (inline=1, block=2, todo=3)
  Length: uint16
  Text: char[Length]
```

### MEMORY_REGIONS (0x0004)

Memory segment definitions.

```
Region Entry:
  StartAddress: uint32
  EndAddress: uint32
  Type: uint8 (code=1, data=2, bss=3, rodata=4)
  Bank: uint8
  Flags: uint16
  NameLength: uint16
  Name: char[NameLength]
```

### DATA_TYPES (0x0005)

Data structure definitions for tables, arrays, etc.

```
DataType Entry:
  Address: uint32
  Length: uint32
  ElementSize: uint16
  ElementCount: uint16
  Type: uint8 (byte=1, word=2, long=3, ptr=4, string=5)
  NameLength: uint16
  Name: char[NameLength]
```

### CROSS_REFS (0x0006)

Cross-reference data for jump/call targets.

```
CrossRef Entry:
  FromAddress: uint32
  ToAddress: uint32
  Type: uint8 (jsr=1, jmp=2, branch=3, read=4, write=5)
```

### SOURCE_MAP (0x0007)

Maps ROM addresses back to original source files.

```
SourceMap Entry:
  RomAddress: uint32
  FileIndex: uint16
  Line: uint16
  Column: uint16

SourceFile Entry:
  PathLength: uint16
  Path: char[PathLength]
```

### METADATA (0x0008)

Project metadata.

```
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

## Compression

Section data is compressed using **zstd** (Zstandard) with compression level 3 by default.
Uncompressed data is stored if COMPRESSED flag is not set or if compression increases size.

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
| Multi-system | Limited | SNES only | ✅ |
| Compression | ❌ | gzip | zstd |
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
5. Strings are UTF-8 encoded without null terminator

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
