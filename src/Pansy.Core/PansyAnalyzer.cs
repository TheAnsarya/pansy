// ============================================================================
// PansyAnalyzer.cs - ROM Coverage Analysis and Gap Detection
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using System.Collections;

namespace Pansy.Core;

/// <summary>
/// Result of analyzing ROM coverage using Pansy metadata.
/// </summary>
public sealed class AnalysisResult {
	/// <summary>Total ROM bytes analyzed.</summary>
	public int TotalBytes { get; init; }

	/// <summary>Bytes classified by at least one metadata source.</summary>
	public int ClassifiedBytes { get; init; }

	/// <summary>Bytes classified specifically by CDL flags.</summary>
	public int CdlClassifiedBytes { get; init; }

	/// <summary>Bytes covered by symbol addresses.</summary>
	public int SymbolCoveredBytes { get; init; }

	/// <summary>Bytes referenced by cross-references.</summary>
	public int CrossRefCoveredBytes { get; init; }

	/// <summary>Coverage percentage (0.0 to 100.0).</summary>
	public double CoveragePercent => TotalBytes > 0
		? (double)ClassifiedBytes / TotalBytes * 100.0
		: 0.0;

	/// <summary>Unclassified regions of the ROM.</summary>
	public IReadOnlyList<GapRegion> Gaps { get; init; } = [];

	/// <summary>Detected data patterns within gaps.</summary>
	public IReadOnlyList<DetectedPattern> Patterns { get; init; } = [];

	/// <summary>Symbol boundary regions found.</summary>
	public IReadOnlyList<SymbolBoundary> SymbolBoundaries { get; init; } = [];

	/// <summary>Cross-reference graph statistics.</summary>
	public CrossRefGraphStats? GraphStats { get; init; }
}

/// <summary>
/// An unclassified region of the ROM (a gap in metadata coverage).
/// </summary>
public readonly record struct GapRegion(int Offset, int Length) {
	/// <summary>End offset (exclusive).</summary>
	public int End => Offset + Length;
}

/// <summary>
/// A detected data pattern in an unclassified gap.
/// </summary>
public sealed class DetectedPattern {
	/// <summary>Byte offset in the ROM.</summary>
	public required int Offset { get; init; }

	/// <summary>Length in bytes.</summary>
	public required int Length { get; init; }

	/// <summary>Type of pattern detected.</summary>
	public required PatternKind Kind { get; init; }

	/// <summary>Confidence level (0.0 to 1.0).</summary>
	public required double Confidence { get; init; }

	/// <summary>Human-readable description.</summary>
	public string? Description { get; init; }
}

/// <summary>
/// Types of data patterns that can be detected in ROM gaps.
/// </summary>
public enum PatternKind : byte {
	/// <summary>Repeated single byte (padding/unused).</summary>
	Fill,
	/// <summary>Printable ASCII text.</summary>
	AsciiString,
	/// <summary>Unknown/unidentifiable data.</summary>
	Unknown,
}

/// <summary>
/// A region bounded by two consecutive symbols.
/// </summary>
public readonly record struct SymbolBoundary(
	int StartAddress,
	int EndAddress,
	string Name,
	SymbolType Type) {
	/// <summary>Length of the region in bytes.</summary>
	public int Length => EndAddress - StartAddress;
}

/// <summary>
/// Statistics about the cross-reference graph.
/// </summary>
public sealed class CrossRefGraphStats {
	/// <summary>Total number of cross-references.</summary>
	public int TotalCrossRefs { get; init; }

	/// <summary>Number of unique source addresses.</summary>
	public int UniqueSourceAddresses { get; init; }

	/// <summary>Number of unique target addresses.</summary>
	public int UniqueTargetAddresses { get; init; }

	/// <summary>Breakdown by type.</summary>
	public int JsrCount { get; init; }
	/// <summary>Number of jump cross-references.</summary>
	public int JmpCount { get; init; }
	/// <summary>Number of branch cross-references.</summary>
	public int BranchCount { get; init; }
	/// <summary>Number of read cross-references.</summary>
	public int ReadCount { get; init; }
	/// <summary>Number of write cross-references.</summary>
	public int WriteCount { get; init; }

	/// <summary>Addresses with the most incoming references.</summary>
	public IReadOnlyList<(int Address, int Count)> MostReferenced { get; init; } = [];

	/// <summary>Subroutines with no incoming references.</summary>
	public IReadOnlyList<int> UnreferencedSubroutines { get; init; } = [];
}

/// <summary>
/// Analyzes ROM coverage using existing Pansy metadata sections.
/// Follows a "hard data first, heuristics second" approach:
/// Phase 1 uses CDL, symbols, and cross-references for definitive classification.
/// Phase 2 applies pattern detection only to unclassified gaps.
/// </summary>
public static class PansyAnalyzer {
	/// <summary>
	/// Perform full analysis: CDL coverage, gap detection, symbol boundaries,
	/// cross-reference graph, and optional pattern detection on gaps.
	/// </summary>
	/// <param name="loader">A loaded Pansy file with metadata.</param>
	/// <param name="romData">The raw ROM data bytes.</param>
	/// <param name="detectPatterns">Whether to run pattern detection on gaps.</param>
	public static AnalysisResult Analyze(
		PansyLoader loader,
		byte[] romData,
		bool detectPatterns = false) {
		int totalBytes = romData.Length;

		// Phase 1: Build classification bitmap from all metadata sources
		var classified = new BitArray(totalBytes);
		int cdlCount = 0;
		int symbolCount = 0;
		int xrefCount = 0;

		// CDL flags — most authoritative source
		for (int i = 0; i < totalBytes; i++) {
			if (loader.IsCode(i) || loader.IsData(i) || loader.IsDrawn(i) ||
				loader.IsRead(i) || loader.IsJumpTarget(i) || loader.IsSubEntryPoint(i)) {
				classified[i] = true;
				cdlCount++;
			}
		}

		// Symbol addresses
		var allSymbols = loader.AllSymbolEntries;
		foreach (var kvp in allSymbols) {
			int addr = kvp.Key;
			if (addr >= 0 && addr < totalBytes && !classified[addr]) {
				classified[addr] = true;
				symbolCount++;
			}
		}

		// Cross-reference targets and sources
		foreach (var xref in loader.CrossReferences) {
			int from = (int)xref.From;
			int to = (int)xref.To;
			if (from >= 0 && from < totalBytes && !classified[from]) {
				classified[from] = true;
				xrefCount++;
			}
			if (to >= 0 && to < totalBytes && !classified[to]) {
				classified[to] = true;
				xrefCount++;
			}
		}

		// Find gaps
		int classifiedTotal = 0;
		var gaps = new List<GapRegion>();
		int gapStart = -1;

		for (int i = 0; i < totalBytes; i++) {
			if (classified[i]) {
				classifiedTotal++;
				if (gapStart >= 0) {
					gaps.Add(new GapRegion(gapStart, i - gapStart));
					gapStart = -1;
				}
			} else {
				if (gapStart < 0) gapStart = i;
			}
		}
		if (gapStart >= 0) {
			gaps.Add(new GapRegion(gapStart, totalBytes - gapStart));
		}

		// Symbol boundaries
		var boundaries = BuildSymbolBoundaries(loader, totalBytes);

		// Cross-reference graph stats
		var graphStats = BuildCrossRefStats(loader);

		// Pattern detection (Phase 2) — only on gaps
		var patterns = detectPatterns
			? DetectPatternsInGaps(gaps, romData)
			: [];

		return new AnalysisResult {
			TotalBytes = totalBytes,
			ClassifiedBytes = classifiedTotal,
			CdlClassifiedBytes = cdlCount,
			SymbolCoveredBytes = symbolCount,
			CrossRefCoveredBytes = xrefCount,
			Gaps = gaps,
			Patterns = patterns,
			SymbolBoundaries = boundaries,
			GraphStats = graphStats,
		};
	}

	/// <summary>
	/// Analyze only CDL coverage (fast, no pattern detection).
	/// </summary>
	public static AnalysisResult AnalyzeCoverage(PansyLoader loader, int romSize) {
		var classified = new BitArray(romSize);
		int count = 0;

		for (int i = 0; i < romSize; i++) {
			if (loader.IsCode(i) || loader.IsData(i) || loader.IsDrawn(i) ||
				loader.IsRead(i)) {
				classified[i] = true;
				count++;
			}
		}

		var gaps = new List<GapRegion>();
		int gapStart = -1;
		for (int i = 0; i < romSize; i++) {
			if (classified[i]) {
				if (gapStart >= 0) {
					gaps.Add(new GapRegion(gapStart, i - gapStart));
					gapStart = -1;
				}
			} else {
				if (gapStart < 0) gapStart = i;
			}
		}
		if (gapStart >= 0) {
			gaps.Add(new GapRegion(gapStart, romSize - gapStart));
		}

		return new AnalysisResult {
			TotalBytes = romSize,
			ClassifiedBytes = count,
			CdlClassifiedBytes = count,
			Gaps = gaps,
		};
	}

	/// <summary>
	/// Build symbol boundary regions from sorted symbol addresses.
	/// Each boundary spans from one symbol to the next.
	/// </summary>
	public static IReadOnlyList<SymbolBoundary> BuildSymbolBoundaries(
		PansyLoader loader, int romSize) {
		var allSymbols = loader.AllSymbolEntries;
		if (allSymbols.Count == 0) return [];

		// Sort symbol addresses
		var sorted = allSymbols
			.Select(kvp => (Address: kvp.Key, Entry: kvp.Value[0]))
			.OrderBy(x => x.Address)
			.ToList();

		var boundaries = new List<SymbolBoundary>(sorted.Count);
		for (int i = 0; i < sorted.Count; i++) {
			int start = sorted[i].Address;
			int end = (i + 1 < sorted.Count) ? sorted[i + 1].Address : romSize;
			boundaries.Add(new SymbolBoundary(start, end, sorted[i].Entry.Name, sorted[i].Entry.Type));
		}

		return boundaries;
	}

	/// <summary>
	/// Build cross-reference graph statistics from the loaded Pansy file.
	/// </summary>
	public static CrossRefGraphStats BuildCrossRefStats(PansyLoader loader) {
		var stats = loader.GetCrossRefStats();
		var mostReferenced = loader.GetMostReferencedAddresses(20).ToList();
		var unreferenced = loader.GetUnreferencedSubroutines().ToList();

		return new CrossRefGraphStats {
			TotalCrossRefs = stats.TotalXrefs,
			JsrCount = stats.JsrCount,
			JmpCount = stats.JmpCount,
			BranchCount = stats.BranchCount,
			ReadCount = stats.ReadCount,
			WriteCount = stats.WriteCount,
			UniqueSourceAddresses = loader.CrossReferences
				.Select(x => (int)x.From).Distinct().Count(),
			UniqueTargetAddresses = loader.CrossReferences
				.Select(x => (int)x.To).Distinct().Count(),
			MostReferenced = mostReferenced,
			UnreferencedSubroutines = unreferenced,
		};
	}

	// ========================================================================
	// Phase 2: Pattern Detection (for gaps only)
	// ========================================================================

	/// <summary>
	/// Detect data patterns in unclassified gap regions.
	/// </summary>
	public static List<DetectedPattern> DetectPatternsInGaps(
		IReadOnlyList<GapRegion> gaps, byte[] romData) {
		var patterns = new List<DetectedPattern>();

		foreach (var gap in gaps) {
			if (gap.Offset + gap.Length > romData.Length) continue;
			var span = romData.AsSpan(gap.Offset, gap.Length);

			if (TryDetectFill(span, gap.Offset, out var fill)) {
				patterns.Add(fill);
			} else if (TryDetectAsciiString(span, gap.Offset, out var str)) {
				patterns.Add(str);
			}
		}

		return patterns;
	}

	/// <summary>
	/// Detect fill regions (repeated single byte — typically 0x00 or 0xff padding).
	/// </summary>
	public static bool TryDetectFill(
		ReadOnlySpan<byte> data, int offset, out DetectedPattern pattern) {
		if (data.Length < 16) {
			pattern = default!;
			return false;
		}

		byte first = data[0];
		for (int i = 1; i < data.Length; i++) {
			if (data[i] != first) {
				pattern = default!;
				return false;
			}
		}

		pattern = new DetectedPattern {
			Offset = offset,
			Length = data.Length,
			Kind = PatternKind.Fill,
			Confidence = 1.0,
			Description = $"Fill: 0x{first:x2} x {data.Length}",
		};
		return true;
	}

	/// <summary>
	/// Detect ASCII string regions (>= 85% printable characters, min 4 bytes).
	/// </summary>
	public static bool TryDetectAsciiString(
		ReadOnlySpan<byte> data, int offset, out DetectedPattern pattern) {
		if (data.Length < 4) {
			pattern = default!;
			return false;
		}

		int printable = 0;
		for (int i = 0; i < data.Length; i++) {
			byte b = data[i];
			if ((b >= 0x20 && b <= 0x7e) || b == 0x0a || b == 0x0d || b == 0x00) {
				printable++;
			}
		}

		double ratio = (double)printable / data.Length;
		if (ratio >= 0.85) {
			pattern = new DetectedPattern {
				Offset = offset,
				Length = data.Length,
				Kind = PatternKind.AsciiString,
				Confidence = ratio,
				Description = $"ASCII text ({ratio:P0} printable)",
			};
			return true;
		}

		pattern = default!;
		return false;
	}
}
