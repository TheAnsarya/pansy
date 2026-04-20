# Pansy Jump Graph Contract Plan (2026-04-20)

## Context

Pansy is the canonical metadata format. Producers and consumers need a clear, scalable contract for jump metadata.
This plan aligns with epic #98 and sub-issues #99, #100, #101.

## Objectives

- Define exact semantics of CDM flags versus CROSS_REFS edges.
- Add validation for graph integrity and address sanity.
- Benchmark load and analysis performance at large graph sizes.

## Work Breakdown

1. Document producer contract and examples (#99).
2. Implement validator checks for graph integrity (#100).
3. Benchmark parsing and memory for large CROSS_REFS datasets (#101).

## Contract Direction

- CDM flags indicate per-byte classification and hinting.
- CROSS_REFS encode source-to-target relationships and edge types.
- Multi-destination control flow is represented by multiple edges from one source.

## Success Criteria

- Contract is explicit and referenced by Nexen and Peony work.
- Validator emits actionable diagnostics.
- Benchmarks provide practical limits and default guidance.
