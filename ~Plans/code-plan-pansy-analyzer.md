# Code Plan: PansyAnalyzer — Metadata-Driven Data Classification

## Overview

`PansyAnalyzer` is a new class in `Pansy.Core` that analyzes ROM coverage using
existing Pansy metadata (CDL, symbols, cross-refs, memory regions) before applying
any statistical heuristics to unclassified gaps.

## Core Principle

**Hard data first, heuristics second.**

```csharp
Input: PansyFile (loaded from .pansy) + byte[] romData
Output: AnalysisResult containing coverage stats, gap list, detected patterns
```

## API Design

```csharp
namespace Pansy.Core;

/// Result of analyzing a ROM with Pansy metadata
public sealed class AnalysisResult {
	public int TotalBytes { get; init; }
	public int ClassifiedBytes { get; init; }
	public double CoveragePercent => TotalBytes > 0
		? (double)ClassifiedBytes / TotalBytes * 100.0
		: 0.0;
	public IReadOnlyList<GapRegion> Gaps { get; init; } = [];
	public IReadOnlyList<DetectedPattern> Patterns { get; init; } = [];
}

/// An unclassified region of the ROM
public readonly record struct GapRegion(int Offset, int Length) {
	public int End => Offset + Length;
}

/// A detected data pattern in an unclassified gap
public sealed class DetectedPattern {
	public required int Offset { get; init; }
	public required int Length { get; init; }
	public required PatternKind Kind { get; init; }
	public required double Confidence { get; init; } // 0.0 to 1.0
	public string? Description { get; init; }
}

public enum PatternKind {
	Fill,           // Repeated single byte (padding)
	AsciiString,    // Printable ASCII text
	ShiftJisString, // Japanese text
	TileData,       // 2bpp/4bpp graphics tiles
	PointerTable,   // Array of valid addresses
	CompressedBlock, // High-entropy data with known header
	RepeatedArray,  // Fixed-stride repeated structure
	Unknown         // Unidentifiable
}
```

## Phase 1 Implementation: CDL Coverage Analysis

```csharp
public static class PansyAnalyzer {
	/// Analyze ROM coverage from CDL (Code/Data Log) flags
	public static AnalysisResult AnalyzeCoverage(PansyFile pansy, byte[] romData) {
		var classified = new BitArray(romData.Length);

		// Mark bytes classified by CDL flags
		if (pansy.CodeDataMap is { Length: > 0 } cdl) {
			for (int i = 0; i < Math.Min(cdl.Length, romData.Length); i++) {
				if (cdl[i] != 0) {
					classified[i] = true;
				}
			}
		}

		// Mark bytes covered by symbols (approximate: mark the symbol address)
		foreach (var symbol in pansy.Symbols) {
			int addr = MapAddressToRomOffset(symbol.Address, pansy.Platform);
			if (addr >= 0 && addr < romData.Length) {
				classified[addr] = true;
			}
		}

		// Count classified and find gaps
		int classifiedCount = 0;
		var gaps = new List<GapRegion>();
		int gapStart = -1;

		for (int i = 0; i < romData.Length; i++) {
			if (classified[i]) {
				classifiedCount++;
				if (gapStart >= 0) {
					gaps.Add(new GapRegion(gapStart, i - gapStart));
					gapStart = -1;
				}
			} else {
				if (gapStart < 0) gapStart = i;
			}
		}
		if (gapStart >= 0) {
			gaps.Add(new GapRegion(gapStart, romData.Length - gapStart));
		}

		return new AnalysisResult {
			TotalBytes = romData.Length,
			ClassifiedBytes = classifiedCount,
			Gaps = gaps,
			Patterns = [] // Phase 1 doesn't detect patterns
		};
	}

	/// Platform-specific address-to-ROM-offset mapping
	/// (Stub — needs per-platform implementation)
	private static int MapAddressToRomOffset(uint address, PlatformId platform) {
		// Simple identity for now — Phase 1.4 will add proper mapping
		return (int)address;
	}
}
```

## Phase 2 Implementation: Gap Pattern Detection

Only runs on GapRegions from Phase 1:

```csharp
public static List<DetectedPattern> AnalyzeGaps(
	IReadOnlyList<GapRegion> gaps,
	byte[] romData,
	PlatformId platform) {

	var patterns = new List<DetectedPattern>();

	foreach (var gap in gaps) {
		var span = romData.AsSpan(gap.Offset, gap.Length);

		// Try detectors in priority order (cheapest first)
		if (TryDetectFill(span, gap.Offset, out var fill)) {
			patterns.Add(fill);
		} else if (TryDetectString(span, gap.Offset, out var str)) {
			patterns.Add(str);
		} else if (TryDetectPointerTable(span, gap.Offset, platform, out var ptrs)) {
			patterns.Add(ptrs);
		} else if (TryDetectTiles(span, gap.Offset, platform, out var tiles)) {
			patterns.Add(tiles);
		}
		// ... more detectors
	}

	return patterns;
}
```

## Fill Detection (simplest, highest confidence)

```csharp
private static bool TryDetectFill(
	ReadOnlySpan<byte> data,
	int offset,
	out DetectedPattern pattern) {

	if (data.Length < 16) { // Too small to be meaningful fill
		pattern = default!;
		return false;
	}

	byte first = data[0];
	bool allSame = true;
	for (int i = 1; i < data.Length; i++) {
		if (data[i] != first) {
			allSame = false;
			break;
		}
	}

	if (allSame) {
		pattern = new DetectedPattern {
			Offset = offset,
			Length = data.Length,
			Kind = PatternKind.Fill,
			Confidence = 1.0,
			Description = $"Fill: 0x{first:x2} × {data.Length}"
		};
		return true;
	}

	pattern = default!;
	return false;
}
```

## String Detection

```csharp
private static bool TryDetectString(
	ReadOnlySpan<byte> data,
	int offset,
	out DetectedPattern pattern) {

	// Count printable ASCII bytes
	int printable = 0;
	for (int i = 0; i < data.Length; i++) {
		byte b = data[i];
		if ((b >= 0x20 && b <= 0x7e) || b == 0x0a || b == 0x0d || b == 0x00) {
			printable++;
		}
	}

	double ratio = (double)printable / data.Length;
	if (ratio >= 0.85 && data.Length >= 4) {
		pattern = new DetectedPattern {
			Offset = offset,
			Length = data.Length,
			Kind = PatternKind.AsciiString,
			Confidence = ratio,
			Description = $"ASCII text ({ratio:P0} printable)"
		};
		return true;
	}

	pattern = default!;
	return false;
}
```

## Test Strategy

### Phase 1 Tests (Coverage Analysis)

```csharp
[Fact]
public void AnalyzeCoverage_FullCdlCoverage_Returns100Percent() {
	var pansy = new PansyFile {
		CodeDataMap = new byte[] { 0x01, 0x01, 0x02, 0x02 } // All marked
	};
	var rom = new byte[] { 0x4c, 0x00, 0x80, 0xff };

	var result = PansyAnalyzer.AnalyzeCoverage(pansy, rom);

	Assert.Equal(100.0, result.CoveragePercent);
	Assert.Empty(result.Gaps);
}

[Fact]
public void AnalyzeCoverage_PartialCdl_FindsGaps() {
	var pansy = new PansyFile {
		CodeDataMap = new byte[] { 0x01, 0x00, 0x00, 0x02 }
	};
	var rom = new byte[4];

	var result = PansyAnalyzer.AnalyzeCoverage(pansy, rom);

	Assert.Equal(50.0, result.CoveragePercent);
	Assert.Single(result.Gaps);
	Assert.Equal(1, result.Gaps[0].Offset);
	Assert.Equal(2, result.Gaps[0].Length);
}
```

### Phase 2 Tests (Pattern Detection)

```csharp
[Fact]
public void DetectFill_AllZeros_DetectsWithFullConfidence() { ... }

[Fact]
public void DetectString_AsciiText_DetectsCorrectly() { ... }

[Fact]
public void DetectPointerTable_NesAddresses_DetectsCorrectly() { ... }
```

## File Locations

```text
src/Pansy.Core/
├── PansyAnalyzer.cs          ← New file (Phase 1)
├── AnalysisResult.cs         ← New types file
├── PatternDetectors/         ← New folder (Phase 2+)
│   ├── FillDetector.cs
│   ├── StringDetector.cs
│   ├── TileDetector.cs
│   ├── PointerTableDetector.cs
│   └── CompressedBlockDetector.cs

tests/Pansy.Core.Tests/
├── PansyAnalyzerTests.cs     ← Coverage analysis tests
├── PatternDetectorTests/     ← Per-detector test files
│   ├── FillDetectorTests.cs
│   ├── StringDetectorTests.cs
│   └── ...
```

## Dependencies / Blockers

- Phase 1: No dependencies — can start immediately
- Phase 2: Needs Phase 1 gap data as input
- Phase 3 (tiles): Needs platform-specific tile format knowledge
- Phase 4 (compressed): Needs known compression format headers per platform
- All phases: Benefit from real ROM + Pansy file pairs for validation
