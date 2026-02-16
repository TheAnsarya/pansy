# Atari Lynx Platform Support for Pansy

**Created:** February 16, 2026
**Status:** Planning

## Overview

Add Atari Lynx (65SC02) platform support to Pansy metadata format.

## Current Status

### Already Implemented ✅

1. **PLATFORM_LYNX constant** - Platform ID `0x09` is already defined in `PansyLoader.cs`

```csharp
/// <summary>Platform ID for Atari Lynx.</summary>
public const byte PLATFORM_LYNX = 0x09;
```

### To Be Implemented 🔄

#### 1. Lynx-Specific Memory Regions

Define default memory regions for Lynx ROMs:
- Zero Page: $0000-$00ff
- Stack: $0100-$01ff
- Work RAM: $0200-$fbff
- Suzy Registers: $fc00-$fcff
- Mikey Registers: $fd00-$fdff
- Boot ROM: $fe00-$ffff

#### 2. Lynx Symbol Defaults

Common symbols that should be auto-recognized:
- Suzy register names
- Mikey register names
- Timer names
- Audio channel names
- Vector addresses

#### 3. Documentation

- Document the 65SC02 CPU characteristics
- Memory map reference
- Register definitions
- Integration with Poppy's Lynx support

## Implementation Plan

### Phase 1: Memory Region Templates

Add predefined memory regions for Lynx in the Pansy writer:

```csharp
public static MemoryRegion[] GetLynxDefaultRegions() => [
	new MemoryRegion(0x0000, 0x00ff, MemoryType.RAM, 0, "Zero Page"),
	new MemoryRegion(0x0100, 0x01ff, MemoryType.RAM, 0, "Stack"),
	new MemoryRegion(0x0200, 0xfbff, MemoryType.RAM, 0, "Work RAM"),
	new MemoryRegion(0xfc00, 0xfcff, MemoryType.IO, 0, "Suzy Registers"),
	new MemoryRegion(0xfd00, 0xfdff, MemoryType.IO, 0, "Mikey Registers"),
	new MemoryRegion(0xfe00, 0xffff, MemoryType.ROM, 0, "Boot ROM"),
];
```

### Phase 2: Symbol Templates

Add well-known Lynx symbols for automatic recognition:
- Suzy hardware registers
- Mikey hardware registers
- Vector addresses

### Phase 3: Integration Tests

- Create test Pansy files with Lynx platform ID
- Verify correct loading/saving
- Test memory region handling

## GitHub Issues

### Epic: Atari Lynx Support in Pansy

**Labels:** `epic`, `enhancement`, `lynx`

### Sub-Issues:

1. **Add Lynx memory region templates**
   - Labels: `enhancement`, `lynx`
   - Pre-defined regions for Lynx memory map

2. **Add Lynx symbol recognition**
   - Labels: `enhancement`, `lynx`
   - Common register symbols

3. **Documentation and tests**
   - Labels: `documentation`, `lynx`
   - Examples and integration tests

## Architecture Reference

### WDC 65SC02 CPU

- 8-bit CPU with 16-bit address bus
- Enhanced 65C02 with new instructions
- 4 MHz clock on Lynx

### Memory Organization

| Region | Address Range | Size | Purpose |
|--------|---------------|------|---------|
| Zero Page | $0000-$00ff | 256 bytes | Fast access |
| Stack | $0100-$01ff | 256 bytes | Stack |
| Work RAM | $0200-$fbff | 64256 bytes | Program/Data |
| Suzy | $fc00-$fcff | 256 bytes | I/O Registers |
| Mikey | $fd00-$fdff | 256 bytes | I/O Registers |
| Boot ROM | $fe00-$ffff | 512 bytes | Boot code |

## External References

- [Lynx Dev Resources](https://www.monlynx.de/lynx/)
- [65C02 Datasheet](https://www.westerndesigncenter.com/wdc/documentation/w65c02s.pdf)
- [Poppy Lynx Guide](../poppy/docs/atari-lynx-guide.md)

