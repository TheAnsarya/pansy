# Integrated Pipeline Master Plan

**Created:** 2026-03-08
**Status:** Planning
**Scope:** Cross-project — Pansy, Peony, Poppy, Nexen

---

## Executive Summary

This plan defines a **complete integrated pipeline** across all four projects for ROM hacking and reverse engineering:

```
Play → Debug → Export → Disassemble → Edit → Build → Verify → Merge → Play Again → Debug Again
```

The pipeline enables a **continuous refinement loop** where each iteration enriches the metadata, improving code understanding with every pass.

---

## The Pipeline

```
    ┌─────────────────────────────────────────────────────────────────────┐
    │                  THE INTEGRATED PIPELINE                            │
    │                                                                     │
    │   ┌─────────┐     ┌─────────┐     ┌──────────┐                    │
    │   │ 1. PLAY │────▶│ 2. DEBUG│────▶│ 3. EXPORT│                    │
    │   │ (Nexen) │     │ (Nexen) │     │(Nex→Pan) │                    │
    │   └─────────┘     └─────────┘     └────┬─────┘                    │
    │                                         │ .pansy                   │
    │                                         ▼                          │
    │   ┌───────────┐    ┌──────────┐    ┌──────────┐                   │
    │   │ 5. EDIT   │◀───│ 4. DISA- │◀───│  .pansy  │                   │
    │   │(VS Code + │    │  SSEMBLE │    │  + ROM   │                   │
    │   │  Poppy    │    │ (Peony)  │    └──────────┘                   │
    │   │Extension) │    └──────────┘                                    │
    │   └─────┬─────┘         │                                          │
    │         │ .pasm         │ enriched .pansy                          │
    │         ▼               ▼                                          │
    │   ┌──────────┐    ┌──────────┐    ┌──────────┐                    │
    │   │ 6. BUILD │───▶│ 7. VERI- │───▶│ 8. MERGE │                    │
    │   │ (Poppy)  │    │    FY    │    │ (Pansy   │                    │
    │   └──────────┘    │ (Peony)  │    │  Tools)  │                    │
    │         │         └──────────┘    └────┬─────┘                    │
    │         │ ROM + .pansy                 │ merged .pansy             │
    │         │                              │                           │
    │         ▼                              ▼                           │
    │   ┌────────────┐    ┌─────────────────────┐                       │
    │   │ 9. PLAY    │◀───│ Import merged .pansy │                       │
    │   │   AGAIN    │    │ (symbols, comments,  │                       │
    │   │  (Nexen)   │    │  CDL, cross-refs)    │                       │
    │   └─────┬──────┘    └─────────────────────┘                       │
    │         │                                                          │
    │         ▼                                                          │
    │   ┌───────────┐                                                    │
    │   │ 10. DEBUG │──────────────────────▶ GOTO 3 (refine)            │
    │   │   AGAIN   │                                                    │
    │   │  (Nexen)  │                                                    │
    │   └───────────┘                                                    │
    │                                                                     │
    └─────────────────────────────────────────────────────────────────────┘
```

---

## Pipeline Stages — Detailed

### Stage 1: Play (Nexen)

**Tool:** Nexen emulator
**Input:** ROM file
**Output:** Runtime execution data

- Load ROM, play the game to exercise code paths
- Background CDL recording captures which bytes are code vs data
- CPU state snapshots record register/flag state at key points
- Save states preserve execution context

**Current State:** ✅ Fully functional

### Stage 2: Debug (Nexen)

**Tool:** Nexen debugger
**Input:** Running ROM + imported .pansy metadata (optional)
**Output:** Breakpoint/trace data, user-added labels/comments

- Import .pansy symbols to see meaningful labels instead of raw addresses
- Set breakpoints, step through code, inspect CPU state
- Add labels and comments during analysis
- Trace execution to discover code paths

**Current State:** ✅ Functional. Pansy import works for symbols, comments, CDL, memory regions, cross-refs.

### Stage 3: Export (Nexen → Pansy)

**Tool:** Nexen PansyExporter + BackgroundPansyExporter
**Input:** Nexen debug state (CDL, labels, comments, CPU state)
**Output:** `.pansy` file

- Auto-export on 5-minute timer (BackgroundPansyExporter)
- Manual export from Debug menu
- Export on ROM unload
- Sections exported: CDL map, symbols, comments, memory regions, cross-refs, metadata, CPU state

**Current State:** ✅ Fully functional (8 sections). Missing: Data Types, Source Map.

### Stage 4: Disassemble (Peony)

**Tool:** Peony disassembler
**Input:** ROM + `.pansy` file (optional)
**Output:** `.pasm` source files + enriched `.pansy`

- Load ROM binary
- Import .pansy hints (CDL, symbols, cross-refs) for better analysis
- Perform static analysis with code/data classification
- Generate human-readable `.pasm` assembly source
- Export enriched `.pansy` with discovered symbols, cross-refs, comments

**Current State:** ⚠️ Partial. Pansy import works but doesn't preserve symbol/comment types, doesn't queue jump targets or sub-entry-points from CDL hints. (See Epic 2)

### Stage 5: Edit (VS Code + Poppy Extension)

**Tool:** VS Code with Poppy extension
**Input:** `.pasm` source files
**Output:** Modified `.pasm` source files

- Syntax highlighting for Poppy Assembly
- Add/modify code, labels, comments
- Organize source into multiple files with includes
- Document findings with block comments and TODOs

**Current State:** ✅ Extension functional with syntax highlighting and diagnostics.

### Stage 6: Build (Poppy)

**Tool:** Poppy compiler
**Input:** `.pasm` source files
**Output:** ROM binary + `.pansy` file

- Compile `.pasm` files to ROM binary
- Generate `.pansy` with all metadata from source:
  - Symbols from label definitions
  - CDL flags from code/data classification
  - Cross-refs from JSR/JMP/branch analysis
  - Memory regions from segment definitions
  - Comments from source comments
  - Metadata from project configuration

**Current State:** ✅ Fully functional. PansyGenerator exports all sections. Missing: Source Map generation.

### Stage 7: Verify (Peony)

**Tool:** Peony RoundtripVerifier
**Input:** Original ROM + assembled ROM
**Output:** Verification result (pass/fail + byte-by-byte diff)

- Compare assembled ROM byte-for-byte against original
- Report any differences with detailed diff
- Guarantee roundtrip: disassemble → assemble → identical ROM

**Current State:** ✅ Fully functional.

### Stage 8: Merge (Pansy Tools)

**Tool:** Pansy CLI `merge` command / Pansy.Core.PansyMerger
**Input:** Multiple `.pansy` files (Nexen export + Peony export + Poppy export)
**Output:** Single merged `.pansy` file

- Merge symbols, comments from multiple sources
- Union code/data flags
- Combine cross-references
- Deduplicate and order entries
- Parallel merge for performance

**Current State:** ✅ Functional. CLI command and API both work.

### Stage 9: Play Again (Nexen)

**Tool:** Nexen emulator
**Input:** Rebuilt ROM + merged `.pansy`
**Output:** Rich debugging session

- Load rebuilt ROM (should be identical to original)
- Auto-import merged `.pansy` for full metadata context
- All symbols, comments, CDL data, cross-refs available in debugger

**Current State:** ✅ Functional via PansyImporter.

### Stage 10: Debug Again (Nexen)

**Tool:** Nexen debugger
**Input:** Running ROM with full metadata
**Output:** Updated analysis, new labels/comments

- Debug with rich context from all previous iterations
- Add new labels/comments discovered during analysis
- Export updated .pansy → GOTO Stage 3
- Each iteration enriches the metadata further

**Current State:** ✅ The loop works end-to-end, though with noted gaps.

---

## Gap Analysis

### A. Pansy Format Gaps

| ID | Gap | Section | Impact | Priority |
|----|-----|---------|--------|----------|
| A1 | Data Types section not implemented | 0x0005 | Can't annotate structured data (arrays, structs, pointers) | Medium |
| A2 | Source Map section not implemented | 0x0007 | Can't map ROM offsets back to source files/lines | Low |

### B. Nexen Gaps

| ID | Gap | Impact | Priority |
|----|-----|--------|----------|
| B1 | No hot reload of .pansy files | Must restart debug session to pick up external changes | Medium |
| B2 | No folder-based per-ROM storage | All data in single file, no versioning | Medium |
| B3 | No incremental .pansy updates | Full re-export each time, even for minor changes | Low |
| B4 | No Data Types export | Structured data not annotated in export | Low |
| B5 | No Source Map usage | Can't jump from debugger to source file:line | Low |
| B6 | No progress UI for import/export | Large files show no progress feedback | Low |

### C. Peony Gaps

| ID | Gap | Impact | Priority |
|----|-----|--------|----------|
| C1 | Symbol types not preserved in import | Function/Constant type info lost when loading .pansy | High |
| C2 | Comment types not preserved in import | Block/Todo/Inline distinction lost | High |
| C3 | Jump target hints from CDL not queued | Pansy can provide code entry points but Peony ignores them | High |
| C4 | Sub-entry-point hints from CDL not queued | Pansy subroutine hints ignored during analysis | High |
| C5 | No bookmark support | User navigation bookmarks not persisted | Low |
| C6 | No data type consumption | Structured data hints from Pansy ignored | Low |

### D. Poppy Gaps

| ID | Gap | Impact | Priority |
|----|-----|--------|----------|
| D1 | No Pansy input (read) capability | Can't import symbols/hints from existing analysis | Medium |

---

## Channel F Integration Update (2026-03-26)

This session added Fairchild Channel F as a first-class platform identity in Pansy core so Nexen and Poppy can share a stable metadata target.

- Platform id added: `0x1f` (`PLATFORM_CHANNEL_F`)
- Platform name mapping added: `Fairchild Channel F`
- Default memory regions added:
  - Cartridge ROM (`$0000-$17ff`)
  - System RAM (`$2800-$2fff`)
  - Video RAM (`$3000-$37ff`)
  - I/O registers (`$3800-$38ff`)
- Default Channel F symbol entries added (baseline I/O and reset vector)
- Test coverage extended in:
  - `PansyLoaderTests`
  - `PlatformDefaultsTests`
  - `BugFixTests`

This closes the immediate platform-id gap for pipeline stage handoff between Nexen export and Pansy ingestion.
| D2 | No Source Map generation | Can't trace assembled bytes back to source lines | Medium |

### E. Cross-Project Gaps (tracked in game-garden repo)

| ID | Gap | Impact | Priority |
|----|-----|--------|----------|
| E1 | No unified project configuration | Each tool configured independently | Low |
| E2 | No automated pipeline scripts | Manual invocation of each stage | Low |
| E3 | No cross-project version compatibility checks | Format changes could break consumers | Medium |
| E4 | Merge workflow not documented end-to-end | Users don't know how to combine outputs | Medium |

---

## Epic Structure

### Epic 1: Pansy Format Completion (pansy repo)

Complete the two reserved sections in the Pansy format specification.

- **1.1** Data Types section (0x0005) — define schema for struct/array/pointer annotations
- **1.2** Source Map section (0x0007) — define schema for ROM-offset → source-file:line mapping
- **1.3** Update FILE-FORMAT.md documentation
- **1.4** Add PansyWriter/PansyLoader support for new sections
- **1.5** Add tests and benchmarks

### Epic 2: Peony Pansy Import Enhancement (peony repo)

Complete the Pansy consumption pipeline in Peony (Epic 8 continuation).

- **2.1** Import symbol types from Pansy (preserve Function/Constant/Label)
- **2.2** Import comment types from Pansy (preserve Inline/Block/Todo)
- **2.3** Queue jump targets from Pansy CDL as code entry points
- **2.4** Queue sub-entry-points from Pansy CDL as code entry points
- **2.5** Add tests for type-preserving roundtrip

### Epic 3: Poppy Pansy Integration (poppy repo)

Add Pansy consumption and Source Map generation to Poppy.

- **3.1** Add PansyReader for importing symbols during assembly
- **3.2** Generate Source Map section in PansyGenerator
- **3.3** Add `--pansy-input` CLI flag for symbol import
- **3.4** Add tests for Pansy import and Source Map

### Epic 4: Nexen Pipeline Enhancements (Nexen repo)

Enhance Nexen's role in the integrated pipeline.

- **4.1** Pansy hot reload (detect external .pansy changes, re-import)
- **4.2** Folder-based per-ROM debug data storage
- **4.3** Data Types section export
- **4.4** Import progress UI for large .pansy files

### Epic 5: Pipeline Documentation & Automation (game-garden repo)

**Repo:** game-garden — the meta-project proving bidirectionality of the Flower Toolchain.
**Issue:** game-garden#12

Document and automate the end-to-end pipeline.

- **5.1** Pipeline user guide (PIPELINE-GUIDE.md) — game-garden#13
- **5.2** Pipeline CLI workflow scripts (PowerShell + bash) — game-garden#14
- **5.3** Cross-project README updates linking to pipeline docs — game-garden#15
- **5.4** Merge workflow documentation with examples — game-garden#16
- **5.5** End-to-end integration test script — game-garden#17
- **5.6** Unified project configuration format — game-garden#18
- **5.7** Cross-project version compatibility checks — game-garden#19

---

## Implementation Priority

### Phase 1 — High Priority (Pipeline Correctness)

1. **Epic 2: Peony Import Enhancement** — Without type preservation and CDL hint consumption, the roundtrip degrades metadata quality
2. **Epic 5.1-5.4: Documentation** — Users need to understand the pipeline

### Phase 2 — Medium Priority (Pipeline Features)

3. **Epic 3: Poppy Pansy Integration** — Enables reading existing analysis during assembly
4. **Epic 4.1: Nexen Hot Reload** — Enables seamless iteration without restart
5. **Epic 1: Pansy Format Completion** — Enables structured data and source map features

### Phase 3 — Lower Priority (Pipeline Polish)

6. **Epic 4.2: Folder Storage** — Better organization and versioning
7. **Epic 4.3-4.4: Nexen UI** — Polish and convenience features
8. **Epic 5.5: E2E Test** — Automated verification of the full pipeline

---

## Data Flow Map

```
                    ┌─────────────────────────────┐
                    │         .pansy file          │
                    │                              │
                    │  Sections:                   │
                    │  ├─ Code/Data Map (0x0001)   │
                    │  ├─ Symbols (0x0002)         │
                    │  ├─ Comments (0x0003)        │
                    │  ├─ Memory Regions (0x0004)  │
                    │  ├─ Data Types (0x0005) ⏳   │
                    │  ├─ Cross-Refs (0x0006)      │
                    │  ├─ Source Map (0x0007) ⏳   │
                    │  ├─ Metadata (0x0008)        │
                    │  ├─ CPU State (0x0009) ✅    │
                    │  └─ Bookmarks (0x000A) ⏳    │
                    └───────┬────────┬────────┬────┘
                            │        │        │
              ┌─────────────┘        │        └──────────────┐
              ▼                      ▼                       ▼
      ┌───────────────┐    ┌──────────────┐     ┌─────────────────┐
      │    Nexen       │    │    Peony     │     │     Poppy       │
      │  (Emulator)    │    │(Disassembler)│     │   (Assembler)   │
      │                │    │              │     │                 │
      │ WRITES:        │    │ WRITES:      │     │ WRITES:         │
      │ ✅ CDL         │    │ ✅ Symbols   │     │ ✅ Symbols      │
      │ ✅ Symbols     │    │ ✅ Comments  │     │ ✅ Comments     │
      │ ✅ Comments    │    │ ✅ CDL       │     │ ✅ CDL          │
      │ ✅ Mem Regions │    │ ✅ Cross-Refs│     │ ✅ Cross-Refs   │
      │ ✅ Cross-Refs  │    │ ✅ Mem Reg.  │     │ ✅ Mem Regions  │
      │ ✅ Metadata    │    │ ✅ Metadata  │     │ ✅ Metadata     │
      │ ✅ CPU State   │    │              │     │                 │
      │                │    │ READS:       │     │ READS:          │
      │ READS:         │    │ ⚠️ Symbols  │     │ ❌ Nothing      │
      │ ✅ Symbols     │    │   (no types) │     │   (one-way)     │
      │ ✅ Comments    │    │ ⚠️ Comments │     │                 │
      │ ✅ CDL         │    │   (no types) │     │                 │
      │ ✅ Mem Regions │    │ ✅ CDL       │     │                 │
      │ ✅ Cross-Refs  │    │ ✅ Mem Reg.  │     │                 │
      │ ✅ Metadata    │    │ ❌ Jump Tgt  │     │                 │
      │                │    │ ❌ Sub Entry │     │                 │
      └───────────────┘    └──────────────┘     └─────────────────┘
```

---

## Test Strategy

### Per-Project Tests

| Project | Current Tests | Target Coverage |
|---------|--------------|----------------|
| Pansy | 328 | +Data Types, +Source Map section tests |
| Peony | 991 | +Type-preserving roundtrip tests |
| Poppy | 1,837 | +Pansy import tests, +Source Map tests |
| Nexen | 2,006 (1,545 C++ + 461 .NET) | +Hot reload tests, +Folder storage tests |

### Cross-Project Integration Tests

| Test | Description | Tools |
|------|-------------|-------|
| Nexen→Peony roundtrip | Export .pansy from Nexen, import in Peony, verify analysis quality | Nexen + Peony |
| Peony→Poppy roundtrip | Disassemble → assemble → verify identical ROM | Peony + Poppy |
| Full pipeline | Nexen export → Peony disassemble → Poppy build → verify → Nexen import | All |
| Merge integration | Merge Nexen + Peony exports, verify no data loss | Pansy merge |
| Type preservation | Export with types → import → re-export → verify types preserved | Peony/Poppy/Pansy |

---

## GitHub Issue Tracker

| Epic | Repo | Issue | Description |
|------|------|-------|-------------|
| 1 | pansy | #55 | Pansy Format Completion — Data Types + Source Map |
| 2 | peony | #101 | Peony Pansy Import Enhancement — Type Preservation + CDL Hints |
| 3 | poppy | #170 | Poppy Pansy Integration — Import Symbols + Source Map |
| 4 | Nexen | #581 | Nexen Pipeline Enhancements — Hot Reload + Folder Storage |
| 5 | game-garden | #12 | Pipeline Documentation & Automation — Scripts + Guides |

### Sub-Issues

**pansy#55:** #57 (Data Types), #58 (Source Map), #59 (Docs)
**peony#101:** #102 (Symbol Types), #103 (Comment Types), #104 (Jump Targets), #105 (Sub-Entry), #106 (Roundtrip Tests)
**poppy#170:** #171 (PansyReader), #172 (Source Map Gen), #173 (CLI Flag), #174 (Tests)
**Nexen#581:** #582 (Hot Reload), #583 (Folder Storage), #584 (Data Types Export), #585 (Progress UI)
**game-garden#12:** #13 (Guide), #14 (Scripts), #15 (README Updates), #16 (Merge Docs), #17 (E2E Test), #18 (Project Config), #19 (Version Compat)

---

## Related Documents

- [Nexen Pansy Roadmap](../../Nexen/~docs/pansy-roadmap.md)
- [Nexen Phase 7.5 Pansy Sync](../../Nexen/~docs/phase-7.5-pansy-sync.md)
- [Peony Pansy Deep Integration Plan](../../peony/~Plans/pansy-deep-integration-plan.md)
- [Peony Type-Preserving Import Code Plan](../../peony/~Plans/code-plan-type-preserving-import.md)
- [Poppy Long-Term Plan](../../poppy/~plans/long-term-plan.md)
- [Pansy FILE-FORMAT.md](../../pansy/docs/FILE-FORMAT.md)
- [Game Garden Roadmap](../../game-garden/~plans/roadmap.md)
- [Game Garden Pipeline Tools](../../game-garden/tools/README.md)
