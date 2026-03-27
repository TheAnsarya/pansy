# Channel F Metadata Conventions

This guide defines practical conventions for Channel F metadata files in the Pansy ecosystem (Nexen, Peony, Poppy).

## Purpose

- Keep Channel F symbol/comment/cross-reference data interoperable across tools.
- Standardize memory region naming and boundaries.
- Reduce ambiguity when sharing `.pansy` files between emulator, disassembler, and assembler workflows.

## Platform ID

Use platform id `0x1f` for Fairchild Channel F metadata files.

## Baseline Memory Regions

Recommended default memory region map:

- `bios`: `$0000-$07ff` (`ROM`)
- `cart`: `$0800-$17ff` (`ROM`)
- `ram`: `$2800-$2fff` (`RAM`)
- `vram`: `$3000-$37ff` (`VRAM`)
- `io`: `$3800-$38ff` (`IO`)

If a project uses extended cartridge space, add additional ROM regions with explicit names.

## Symbol Naming

Use stable, tool-neutral names:

- Entry point: `CartEntry`
- BIOS routines: `Bios_*`
- Interrupt and vector-style labels: `Vec_*` where applicable
- I/O labels: `Port_*` or hardware-specific names
- Data tables: `Tbl_*`

Avoid tool-generated temporary prefixes in canonical shared files.

## Comment Style

Prefer concise intent comments:

- Explain why a routine exists.
- Document non-obvious register/port usage.
- Mark uncertain reverse-engineering assumptions with `TODO` comment type.

## Code/Data Map Guidance

When possible, emit full code/data map flags for Channel F regions:

- Mark executable bytes as `CODE` and opcode starts as `OPCODE`.
- Mark jump/call targets with `JUMP_TARGET` and `SUB_ENTRY`.
- Mark rendered/graphics accesses in VRAM-related traces using `DRAWN` where available.
- Mark read-heavy table lookups with `READ` and indirect-driven accesses with `INDIRECT`.

## Cross-Reference Guidance

For Channel F projects, use cross-references consistently:

- `Jsr`/call-like flow for `pi` patterns.
- `Jmp` for absolute control transfers (`jmp`/equivalent patterns).
- `Branch` for conditional and short branch flow (`bt`/`bf`/`br` patterns).
- `Read` and `Write` for data and port access relationships when the producer supports it.

## Interop Workflow

Typical roundtrip path:

1. Nexen exports Channel F `.pansy` during debug/analysis.
2. Peony consumes metadata and enriches disassembly context.
3. Poppy consumes labels/comments where relevant during build workflows.
4. Updated metadata is revalidated in Nexen.

## Validation Checklist

- Platform id is `0x1f`.
- Memory regions include at least BIOS, cart, RAM, VRAM, and I/O.
- Entry symbols resolve to valid ROM addresses.
- Cross-reference targets are within mapped regions.
- Code/data map length matches expected ROM size.

## Related Documents

- `docs/FILE-FORMAT.md`
- `docs/EXAMPLES.md`
- `README.md`
