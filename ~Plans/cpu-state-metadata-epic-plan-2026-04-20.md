# CPU State Metadata Epic Plan (2026-04-20)

## Epic

Pansy #104 - Canonical CPU state metadata pipeline

## Goal

Document and harden Pansy section `0x0009` as the canonical storage for per-address CPU execution state such as SNES X/M width flags, separate from code/data-map analysis flags.

## Workstreams

1. Documentation
   - Describe CPU-state record semantics and SNES X/M bit usage.
   - Clarify why CPU-state is stored separately from CDM flags.
2. Validation
   - Keep roundtrip tests proving CPU-state preservation.
   - Add validation guidance for producers/consumers.
3. Performance
   - Benchmark loader and lookup overhead on CPU-state-heavy files.

## Cross-Repo Dependencies

- Nexen produces CPU-state entries.
- Peony consumes CPU-state entries during decode.
- Pansy defines and validates the contract.

## Deliverables

- Pansy #105 documentation updates
- Pansy #106 benchmark coverage
- Cross-repo notes aligned with Nexen/Peony implementation
