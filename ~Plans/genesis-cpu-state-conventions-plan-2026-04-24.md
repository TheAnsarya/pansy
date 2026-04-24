# Genesis CPU-State Conventions Plan (2026-04-24)

## Context

Pansy CPU-state metadata currently documents canonical usage for SNES (M/X width flags) and GBA (ARM/THUMB mode), but Sega Genesis / Mega Drive requires additional conventions due to dual-CPU execution context (68000 + Z80).

Tracking issues:

- Epic: #111
- Planning/docs slice: #112

## Research Summary

External references reviewed:

- Motorola 68000 addressing/state behavior overview:
	- https://en.wikibooks.org/wiki/68000_Assembly/Addressing_Modes
- Genesis hardware bus and CPU interaction notes:
	- https://plutiedev.com/mirror/kabuto-hardware-notes
- Additional 68k addressing/alignment tutorial context:
	- https://mrjester.hapisan.com/04_MC68/Sect01Part06/Index.html

Observed constraints from references:

- Genesis runtime commonly involves both 68000 and Z80 execution domains.
- State that influences decoding/analysis differs by CPU family and cannot be represented safely via current SNES-only flag semantics.
- Existing CPU_STATE payload shape can carry this data without binary layout changes if semantics are platform/mode-scoped.

## Proposed Metadata Conventions

### CpuMode values (forward-compatible)

- 4: `M68000` (proposed)
- 5: `Z80` (proposed)

### Flags semantics (mode-scoped)

When `CpuMode = M68000`:

- bit 0: Supervisor (`S`)
- bit 1: Trace (`T`)
- bits 2-4: Interrupt Priority Level (`IPL`, 0-7)
- bits 5-7: reserved

When `CpuMode = Z80`:

- bit 0: `IFF1`
- bit 1: `IFF2`
- bits 2-3: Interrupt mode (`IM`)
- bits 4-7: reserved

`DataBank` and `DirectPage` remain zero for Genesis modes in v1 scope.

## Design Goals

- Preserve binary compatibility of current section layout (`9` bytes per entry).
- Keep semantics explicit and mode-dependent rather than overloading global flag meaning.
- Avoid breaking existing SNES/GBA consumers by treating new CpuMode values as additive.

## Risks and Mitigations

- Risk: Consumers assume only modes 0-3 exist.
	- Mitigation: update parser/consumer guidance to tolerate unknown mode values.
- Risk: Producers emit inconsistent Genesis flags.
	- Mitigation: provide strict mode-specific flag tables and validation tests.
- Risk: Overfitting to one toolchain.
	- Mitigation: keep conventions generic and avoid requiring emulator-specific internal state.

## Implementation Follow-Ups

1. Add `CpuMode` enum values for `M68000` and `Z80` in `Pansy.Core` with compatibility tests.
2. Add CPU-state roundtrip tests validating Genesis mode values and flag bytes are preserved.
3. Add analyzer support for Genesis mode-aware reporting (optional phase).
4. Add CLI display formatting for Genesis CPU-state entries.

## Acceptance Criteria for Follow-Up Implementation Ticket

- New CpuMode values serialize/deserialize losslessly.
- Existing SNES/GBA CPU-state tests remain green.
- New Genesis CPU-state tests added and passing.
- Documentation remains synchronized with implementation.
