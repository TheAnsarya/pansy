# Atari 2600 Phase 2 Design — Pansy Metadata Enhancements

## Goal

Complete the Atari 2600 register symbol table and add rich metadata for tools.

## Current State

- Platform ID `0x08` defined in `PlatformId` enum
- `GetAtari2600DefaultRegions()` returns 4 memory regions (TIA, RAM, RIOT, ROM)
- `GetAtari2600DefaultSymbolEntries()` returns 40+ TIA write registers and RIOT registers
- Interrupt vector symbols defined

## Phase 2 Work Items

### 1. TIA Read Registers (#82)

Add 14 missing read registers to `GetAtari2600DefaultSymbolEntries()`:

| Address | Name | Purpose |
|---------|------|---------|
| `$00` | CXM0P | Collision M0-P1, M0-P0 |
| `$01` | CXM1P | Collision M1-P0, M1-P1 |
| `$02` | CXP0FB | Collision P0-PF, P0-BL |
| `$03` | CXP1FB | Collision P1-PF, P1-BL |
| `$04` | CXM0FB | Collision M0-PF, M0-BL |
| `$05` | CXM1FB | Collision M1-PF, M1-BL |
| `$06` | CXBLPF | Collision BL-PF |
| `$07` | CXPPMM | Collision P0-P1, M0-M1 |
| `$08` | INPT0 | Paddle 0 input |
| `$09` | INPT1 | Paddle 1 input |
| `$0a` | INPT2 | Paddle 2 input |
| `$0b` | INPT3 | Paddle 3 input |
| `$0c` | INPT4 | Player 0 fire button |
| `$0d` | INPT5 | Player 1 fire button |

**Note:** TIA read and write registers share the same address range but are distinguished by read vs write access. The symbol type should indicate read-only access.

### 2. Comprehensive Tests (#83)

Create `Atari2600PlatformDefaultsTests.cs` with:

- Verify all 44+ TIA write register symbols present
- Verify all 14 TIA read register symbols present
- Verify RIOT register symbols (SWCHA through T1024T)
- Verify interrupt vector symbols
- Verify memory region boundaries
- Verify no duplicate addresses

### 3. Bit Field Metadata (#84)

Future enhancement — add structured metadata for bit-level register documentation:

- NUSIZ0/1: bits 0-2 missile size, bits 4-5 player copies/size
- CTRLPF: bit 0 reflect, bit 1 score, bit 2 priority, bits 4-5 ball size
- SWCHB: bit 0 RESET, bit 1 SELECT, bit 3 B/W, bit 6 P0 difficulty, bit 7 P1 difficulty

## File Changes

- `src/Pansy.Core/PlatformDefaults.cs` — Add read registers
- `tests/Pansy.Core.Tests/Atari2600PlatformDefaultsTests.cs` — New test file
