# CPU State Metadata

## Overview

The Pansy CPU-state section (`0x0009`) stores per-address execution state that affects instruction decoding but does not belong in the code/data map.

This section exists for architectures where the active processor mode changes how bytes should be interpreted. Two current examples are:

- SNES / 65816: accumulator and index register width depend on the `M` and `X` flags.
- GBA / ARM7TDMI: opcode width and decoder selection depend on ARM versus THUMB mode.

## Why This Is Separate From CODE_DATA_MAP

The code/data map answers questions like:

- Is this byte code or data?
- Is this byte an opcode?
- Was this byte read, drawn, or reached indirectly?

CPU-state metadata answers a different question:

- Under what processor state should code at this address be decoded?

Do not overload code/data-map bits to store CPU-width or mode information. CPU-state entries are the canonical place for that metadata.

## Section Layout

Each CPU-state entry is 9 bytes:

```text
Address:    uint32
Flags:      uint8
DataBank:   uint8
DirectPage: uint16
CpuMode:    uint8
```

## Field Semantics

### Address

CPU address where the recorded state is known to apply. Consumers should treat this as a seed point for decoding at that address, then propagate state according to architecture rules while control flow continues.

### Flags

Current flag meanings are architecture-specific.

For SNES / 65816:

- Bit 0: `X` flag
- Bit 1: `M` flag
- Bits 2-7: reserved, write as zero

For 65816 width tracking, the meaning is:

- `X = 1` means index registers are 8-bit
- `X = 0` means index registers are 16-bit
- `M = 1` means accumulator is 8-bit
- `M = 0` means accumulator is 16-bit

These bits intentionally mirror the live CPU flags, so `1` means 8-bit for both register groups.

### DataBank

65816 data bank register snapshot. Write zero for architectures that do not use it.

### DirectPage

65816 direct-page register snapshot. Write zero for architectures that do not use it.

### CpuMode

Current standardized values:

- `0`: `Native65816`
- `1`: `Emulation6502`
- `2`: `ARM`
- `3`: `THUMB`

Reserved for future platform-specific expansion:

- `4`: `M68000` (proposed)
- `5`: `Z80` (proposed)

## Producer Guidance

- Emit CPU-state entries only when the state is known.
- Prefer exact seed points over inferred blanket coverage.
- Sort entries by address.
- Keep reserved bits cleared.
- Preserve code/data-map flags independently; do not merge CPU-state information into that section.

## Consumer Guidance

- Prefer CPU-state metadata over lossy fallback sources when both exist.
- Use entries to seed decoding state at matching addresses.
- Continue to propagate architecture state through decoded instructions and control flow.
- Fall back to CDL or platform defaults only when no authoritative CPU-state entry exists.

Analyzer and CLI consumers should present mode-aware interpretations instead of raw flag bytes where possible.

- `PansyAnalyzer.GetCpuModeName(...)` provides stable mode labels for reports.
- `PansyAnalyzer.DescribeCpuState(...)` provides mode-specific flag decoding used by CLI summaries.

## Current Canonical Uses

- Nexen exports SNES X/M and GBA ARM/THUMB state through CPU-state entries.
- Peony uses Pansy CPU-state entries as the preferred SNES M/X source during 65816 decoding, with CDL retained as fallback.

## Genesis / Mega Drive (Proposed Conventions)

Genesis has two interacting CPUs in the cartridge execution model:

- Motorola 68000 main CPU
- Z80 sound CPU

To keep CPU-state metadata architecture-agnostic while still useful for Genesis analysis, use the following proposed conventions for future producer support.

Proposed `CpuMode` values:

- `4`: `M68000`
- `5`: `Z80`

Proposed `Flags` bit layout when `CpuMode = M68000`:

- Bit 0: `S` (Supervisor state)
- Bit 1: `T` (Trace enabled)
- Bits 2-4: `IPL` (interrupt priority level, 0-7)
- Bits 5-7: reserved (write zero)

Proposed `Flags` bit layout when `CpuMode = Z80`:

- Bit 0: `IFF1` (interrupt enable state)
- Bit 1: `IFF2` (secondary interrupt enable state)
- Bit 2: `IM` low bit (interrupt mode)
- Bit 3: `IM` high bit (interrupt mode)
- Bits 4-7: reserved (write zero)

`DataBank` and `DirectPage` remain zero for Genesis unless a future extension explicitly assigns meaning.

These values are documentation-level conventions only in this phase. They are intended to guide upcoming format and API implementation slices without changing current binary compatibility.

Current analyzer/CLI interpretation behavior for Genesis uses the proposed layouts above when `CpuMode` is `M68000` or `Z80`.

## Benchmark Guidance

CPU_STATE benchmark coverage includes SNES, GBA, and Genesis mixed-mode scenarios in `CpuStateBenchmarks`.

Run focused CPU_STATE benchmarks:

```powershell
dotnet run --project tests/Pansy.Core.Benchmarks -c Release -- --filter "*CpuStateBenchmarks*" --warmupCount 3 --iterationCount 5
```

Key comparison scenarios:

- `Write CPU state SNES`
- `Write CPU state GBA`
- `Write CPU state Genesis mixed-mode`
- `Load CPU state Genesis mixed-mode`
- `Query CPU state Genesis mixed x1000`

When reporting results, include both timing and allocation deltas so mixed M68000/Z80 mode-switch density can be compared against SNES and GBA baselines.

## Related Documentation

- [File Format Specification](FILE-FORMAT.md)
- [Examples](EXAMPLES.md)
