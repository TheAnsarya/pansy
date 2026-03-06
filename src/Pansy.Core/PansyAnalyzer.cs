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
	/// <summary>Table of ROM/RAM addresses.</summary>
	PointerTable,
	/// <summary>Platform-specific graphics tile data.</summary>
	TileData,
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
			? DetectPatternsInGaps(gaps, romData, loader.Platform, loader)
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
		IReadOnlyList<GapRegion> gaps, byte[] romData,
		byte platform = 0, PansyLoader? loader = null) {
		var patterns = new List<DetectedPattern>();

		foreach (var gap in gaps) {
			if (gap.Offset + gap.Length > romData.Length) continue;
			var span = romData.AsSpan(gap.Offset, gap.Length);

			if (TryDetectFill(span, gap.Offset, out var fill)) {
				patterns.Add(fill);
			} else if (TryDetectAsciiString(span, gap.Offset, out var str)) {
				patterns.Add(str);
			} else if (TryDetectPointerTable(span, gap.Offset, platform, loader, out var ptrs)) {
				patterns.Add(ptrs);
			} else if (TryDetectTileData(span, gap.Offset, platform, out var tile)) {
				patterns.Add(tile);
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

	// ========================================================================
	// Phase 3: Platform-specific pattern detection
	// ========================================================================

	/// <summary>
	/// Detect pointer/address tables — sequences of valid addresses for the platform.
	/// Requires at least 3 consecutive valid addresses (6+ bytes for 16-bit platforms).
	/// </summary>
	public static bool TryDetectPointerTable(
		ReadOnlySpan<byte> data, int offset, byte platform,
		PansyLoader? loader, out DetectedPattern pattern) {
		int addrSize = GetAddressSize(platform);
		int minEntries = 3;

		if (data.Length < addrSize * minEntries) {
			pattern = default!;
			return false;
		}

		int entryCount = data.Length / addrSize;
		int validCount = 0;

		for (int i = 0; i < entryCount; i++) {
			var entry = data.Slice(i * addrSize, addrSize);
			uint addr = ReadAddress(entry, platform);

			if (IsValidAddress(addr, platform, loader)) {
				validCount++;
			}
		}

		double ratio = (double)validCount / entryCount;
		if (ratio >= 0.75 && validCount >= minEntries) {
			int tableBytes = entryCount * addrSize;
			pattern = new DetectedPattern {
				Offset = offset,
				Length = tableBytes,
				Kind = PatternKind.PointerTable,
				Confidence = ratio,
				Description = $"Pointer table: {validCount}/{entryCount} valid {addrSize * 8}-bit addresses",
			};
			return true;
		}

		pattern = default!;
		return false;
	}

	/// <summary>
	/// Detect platform-specific graphics tile data.
	/// NES: 2bpp planar tiles (16 bytes/tile)
	/// GB: 2bpp interleaved tiles (16 bytes/tile)
	/// SNES: 2bpp/4bpp tiles (16 or 32 bytes/tile)
	/// </summary>
	public static bool TryDetectTileData(
		ReadOnlySpan<byte> data, int offset, byte platform,
		out DetectedPattern pattern) {
		int tileSize = GetTileSize(platform);

		// Need at least 4 complete tiles
		if (tileSize == 0 || data.Length < tileSize * 4) {
			pattern = default!;
			return false;
		}

		int tileCount = data.Length / tileSize;
		int validTiles = 0;

		for (int i = 0; i < tileCount; i++) {
			var tile = data.Slice(i * tileSize, tileSize);
			if (IsValidTile(tile, platform)) {
				validTiles++;
			}
		}

		double ratio = (double)validTiles / tileCount;
		if (ratio >= 0.60 && validTiles >= 4) {
			int totalBytes = tileCount * tileSize;
			string bpp = platform switch {
				PansyLoader.PLATFORM_NES => "2bpp",
				PansyLoader.PLATFORM_GB => "2bpp",
				PansyLoader.PLATFORM_SNES => tileSize == 32 ? "4bpp" : "2bpp",
				_ => $"{tileSize * 8 / 64}bpp",
			};
			pattern = new DetectedPattern {
				Offset = offset,
				Length = totalBytes,
				Kind = PatternKind.TileData,
				Confidence = ratio,
				Description = $"{bpp} tile data: {validTiles}/{tileCount} valid tiles ({totalBytes} bytes)",
			};
			return true;
		}

		pattern = default!;
		return false;
	}

	/// <summary>
	/// Get the native address size in bytes for a platform.
	/// </summary>
	public static int GetAddressSize(byte platform) => platform switch {
		PansyLoader.PLATFORM_NES => 2,
		PansyLoader.PLATFORM_GB => 2,
		PansyLoader.PLATFORM_ATARI_2600 => 2,
		PansyLoader.PLATFORM_SMS => 2,
		PansyLoader.PLATFORM_PCE => 2,
		PansyLoader.PLATFORM_SNES => 3, // 24-bit
		PansyLoader.PLATFORM_GBA => 4, // 32-bit ARM
		PansyLoader.PLATFORM_GENESIS => 4, // 32-bit 68k
		_ => 2, // default to 16-bit
	};

	/// <summary>
	/// Read an address from a byte span in the platform's native format.
	/// </summary>
	public static uint ReadAddress(ReadOnlySpan<byte> data, byte platform) {
		if (platform == PansyLoader.PLATFORM_GENESIS) {
			// M68000: big-endian 32-bit
			if (data.Length < 4) return 0xffffffff;
			return ((uint)data[0] << 24) | ((uint)data[1] << 16) |
				   ((uint)data[2] << 8) | data[3];
		}
		if (platform == PansyLoader.PLATFORM_GBA) {
			// ARM7TDMI: little-endian 32-bit
			if (data.Length < 4) return 0xffffffff;
			return data[0] | ((uint)data[1] << 8) |
				   ((uint)data[2] << 16) | ((uint)data[3] << 24);
		}
		if (platform == PansyLoader.PLATFORM_SNES) {
			// 65816: little-endian 24-bit
			if (data.Length < 3) return 0xffffffff;
			return data[0] | ((uint)data[1] << 8) | ((uint)data[2] << 16);
		}
		// Default: little-endian 16-bit
		if (data.Length < 2) return 0xffffffff;
		return (uint)(data[0] | (data[1] << 8));
	}

	/// <summary>
	/// Check if an address is valid for the platform's address space.
	/// Optionally validates against known symbols/code in the loader.
	/// </summary>
	public static bool IsValidAddress(uint addr, byte platform, PansyLoader? loader) {
		// Platform-specific address range validation
		bool inRange = platform switch {
			PansyLoader.PLATFORM_NES => addr is (>= 0x0000 and <= 0xffff),
			PansyLoader.PLATFORM_GB => addr is (>= 0x0000 and <= 0xffff),
			PansyLoader.PLATFORM_SNES => addr is (>= 0x000000 and <= 0xffffff),
			PansyLoader.PLATFORM_GBA => addr is (>= 0x08000000 and <= 0x0e00ffff),
			PansyLoader.PLATFORM_GENESIS => addr is (>= 0x000000 and <= 0xffffff),
			PansyLoader.PLATFORM_ATARI_2600 => addr is (>= 0xf000 and <= 0xffff),
			_ => addr <= 0xffff,
		};

		if (!inRange) return false;

		// If loader available, check if address points to known code/data
		if (loader != null) {
			int intAddr = (int)addr;
			if (loader.IsCode(intAddr) || loader.IsData(intAddr) ||
				loader.GetSymbol(intAddr) != null) {
				return true;
			}
		}

		return inRange;
	}

	/// <summary>
	/// Get the tile size in bytes for a platform's native tile format.
	/// Returns 0 for platforms without standard tile formats.
	/// </summary>
	public static int GetTileSize(byte platform) => platform switch {
		PansyLoader.PLATFORM_NES => 16,   // 2bpp planar, 8x8
		PansyLoader.PLATFORM_GB => 16,    // 2bpp interleaved, 8x8
		PansyLoader.PLATFORM_SNES => 32,  // 4bpp planar, 8x8 (most common)
		PansyLoader.PLATFORM_SMS => 32,   // 4bpp planar, 8x8
		PansyLoader.PLATFORM_PCE => 32,   // 4bpp, 8x8
		_ => 0,
	};

	/// <summary>
	/// Validate tile data structure for a platform.
	/// Checks that the tile has realistic bit patterns (not all 0x00 or 0xff,
	/// reasonable bit distribution suggesting actual graphics).
	/// </summary>
	public static bool IsValidTile(ReadOnlySpan<byte> tile, byte platform) {
		int tileSize = GetTileSize(platform);
		if (tile.Length < tileSize) return false;

		// Reject tiles that are entirely zero or entirely 0xff (likely padding)
		bool allZero = true;
		bool allOnes = true;
		for (int i = 0; i < tileSize; i++) {
			if (tile[i] != 0x00) allZero = false;
			if (tile[i] != 0xff) allOnes = false;
			if (!allZero && !allOnes) break;
		}
		if (allZero || allOnes) return false;

		// Check for reasonable byte variety (at least 2 distinct values)
		int distinctValues = 0;
		Span<bool> seen = stackalloc bool[256];
		seen.Clear();
		for (int i = 0; i < tileSize; i++) {
			if (!seen[tile[i]]) {
				seen[tile[i]] = true;
				distinctValues++;
				if (distinctValues >= 2) return true;
			}
		}

		return distinctValues >= 2;
	}

	// ========================================================================
	// Phase 3: Auto-Annotation Generator (#42)
	// ========================================================================

	/// <summary>
	/// Generate annotations from analysis results and write an enriched Pansy file.
	/// Adds symbols, comments, and memory regions for detected patterns.
	/// </summary>
	public static byte[] GenerateAnnotations(
		PansyLoader source, AnalysisResult analysis) {
		var writer = new PansyWriter {
			Platform = source.Platform,
			RomSize = source.RomSize,
			RomCrc32 = source.RomCrc32,
			ProjectName = source.ProjectName,
			Author = source.Author,
			ProjectVersion = source.ProjectVersion,
		};

		// Copy existing symbols
		foreach (var kvp in source.AllSymbolEntries) {
			foreach (var entry in kvp.Value) {
				writer.AddSymbol((uint)kvp.Key, entry.Name, entry.Type);
			}
		}

		// Copy existing comments
		foreach (var kvp in source.AllCommentEntries) {
			foreach (var entry in kvp.Value) {
				writer.AddComment((uint)kvp.Key, entry.Text, (byte)entry.Type);
			}
		}

		// Copy existing cross-references
		foreach (var xref in source.CrossReferences) {
			writer.AddCrossReference(xref);
		}

		// Copy existing memory regions
		foreach (var region in source.MemoryRegions) {
			writer.AddMemoryRegion(region);
		}

		// Add annotations from detected patterns
		foreach (var pattern in analysis.Patterns) {
			uint addr = (uint)pattern.Offset;
			switch (pattern.Kind) {
				case PatternKind.Fill:
					writer.AddComment(addr, pattern.Description ?? $"Padding ({pattern.Length} bytes)", (byte)CommentType.Inline);
					for (int i = 0; i < pattern.Length; i++) {
						writer.MarkAsData((uint)(pattern.Offset + i));
					}
					break;

				case PatternKind.AsciiString:
					writer.AddSymbol(addr, $"str_{addr:x6}", SymbolType.Label);
					writer.AddComment(addr, pattern.Description ?? "ASCII string", (byte)CommentType.Inline);
					for (int i = 0; i < pattern.Length; i++) {
						writer.MarkAsData((uint)(pattern.Offset + i));
					}
					break;

				case PatternKind.PointerTable:
					writer.AddSymbol(addr, $"ptrtbl_{addr:x6}", SymbolType.Label);
					writer.AddComment(addr, pattern.Description ?? "Pointer table", (byte)CommentType.Inline);
					for (int i = 0; i < pattern.Length; i++) {
						writer.MarkAsData((uint)(pattern.Offset + i));
					}
					break;

				case PatternKind.TileData:
					writer.AddSymbol(addr, $"tiles_{addr:x6}", SymbolType.Label);
					writer.AddComment(addr, pattern.Description ?? "Tile data", (byte)CommentType.Inline);
					for (int i = 0; i < pattern.Length; i++) {
						writer.MarkAsDrawn((uint)(pattern.Offset + i));
					}
					break;
			}
		}

		// Add annotations for gaps without patterns
		foreach (var gap in analysis.Gaps) {
			bool hasPattern = false;
			foreach (var p in analysis.Patterns) {
				if (p.Offset == gap.Offset) {
					hasPattern = true;
					break;
				}
			}
			if (!hasPattern && gap.Length >= 16) {
				writer.AddComment((uint)gap.Offset,
					$"Unclassified gap: {gap.Length} bytes", (byte)CommentType.Todo);
			}
		}

		return writer.Generate();
	}
}
