using Xunit;

namespace Pansy.Core.Tests;

public class AnalyzerTests {
	// ========================================================================
	// Coverage Analysis (#31)
	// ========================================================================

	[Fact]
	public void AnalyzeCoverage_EmptyFile_ReturnsZeroCoverage() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.AnalyzeCoverage(loader, 256);

		Assert.Equal(256, result.TotalBytes);
		Assert.Equal(0, result.ClassifiedBytes);
		Assert.Equal(0.0, result.CoveragePercent);
		Assert.Single(result.Gaps);
		Assert.Equal(0, result.Gaps[0].Offset);
		Assert.Equal(256, result.Gaps[0].Length);
	}

	[Fact]
	public void AnalyzeCoverage_AllCodeMarked_Returns100Percent() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 8,
		};
		for (uint i = 0; i < 8; i++) {
			writer.MarkAsCode(i);
		}
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.AnalyzeCoverage(loader, 8);

		Assert.Equal(8, result.TotalBytes);
		Assert.Equal(8, result.ClassifiedBytes);
		Assert.Equal(100.0, result.CoveragePercent);
		Assert.Empty(result.Gaps);
	}

	[Fact]
	public void AnalyzeCoverage_MixedCodeAndData_CountsBoth() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 16,
		};
		// Mark first 4 as code, next 4 as data, rest unclassified
		for (uint i = 0; i < 4; i++) writer.MarkAsCode(i);
		for (uint i = 4; i < 8; i++) writer.MarkAsData(i);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.AnalyzeCoverage(loader, 16);

		Assert.Equal(16, result.TotalBytes);
		Assert.Equal(8, result.ClassifiedBytes);
		Assert.Equal(50.0, result.CoveragePercent);
	}

	[Fact]
	public void AnalyzeCoverage_DrawnAndReadFlags_Counted() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 4,
		};
		writer.MarkAsDrawn(0);
		writer.MarkAsRead(1);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.AnalyzeCoverage(loader, 4);

		Assert.Equal(2, result.ClassifiedBytes);
	}

	// ========================================================================
	// Gap Finder (#32)
	// ========================================================================

	[Fact]
	public void AnalyzeCoverage_SingleGapInMiddle_DetectsCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 16,
		};
		// Mark 0-3 and 12-15 as code, leaving 4-11 as gap
		for (uint i = 0; i < 4; i++) writer.MarkAsCode(i);
		for (uint i = 12; i < 16; i++) writer.MarkAsCode(i);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.AnalyzeCoverage(loader, 16);

		Assert.Single(result.Gaps);
		Assert.Equal(4, result.Gaps[0].Offset);
		Assert.Equal(8, result.Gaps[0].Length);
		Assert.Equal(12, result.Gaps[0].End);
	}

	[Fact]
	public void AnalyzeCoverage_MultipleGaps_DetectsAll() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 20,
		};
		// Mark bytes 0-3, 8-11, 16-19 → gaps at 4-7 and 12-15
		for (uint i = 0; i < 4; i++) writer.MarkAsCode(i);
		for (uint i = 8; i < 12; i++) writer.MarkAsCode(i);
		for (uint i = 16; i < 20; i++) writer.MarkAsCode(i);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.AnalyzeCoverage(loader, 20);

		Assert.Equal(2, result.Gaps.Count);
		Assert.Equal(4, result.Gaps[0].Offset);
		Assert.Equal(4, result.Gaps[0].Length);
		Assert.Equal(12, result.Gaps[1].Offset);
		Assert.Equal(4, result.Gaps[1].Length);
	}

	[Fact]
	public void AnalyzeCoverage_GapAtEnd_DetectsTrailingGap() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 16,
		};
		for (uint i = 0; i < 8; i++) writer.MarkAsCode(i);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.AnalyzeCoverage(loader, 16);

		Assert.Single(result.Gaps);
		Assert.Equal(8, result.Gaps[0].Offset);
		Assert.Equal(8, result.Gaps[0].Length);
	}

	[Fact]
	public void GapRegion_EndProperty_IsCorrect() {
		var gap = new GapRegion(10, 5);
		Assert.Equal(10, gap.Offset);
		Assert.Equal(5, gap.Length);
		Assert.Equal(15, gap.End);
	}

	// ========================================================================
	// Full Analysis (Analyze method)
	// ========================================================================

	[Fact]
	public void Analyze_WithCdlAndSymbols_CombinesSources() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 32,
		};
		// CDL: mark bytes 0-7 as code
		for (uint i = 0; i < 8; i++) writer.MarkAsCode(i);
		// Symbol at byte 16 (not covered by CDL)
		writer.AddSymbol(16, "DataTable");

		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var rom = new byte[32];

		var result = PansyAnalyzer.Analyze(loader, rom);

		Assert.Equal(32, result.TotalBytes);
		Assert.Equal(9, result.ClassifiedBytes); // 8 CDL + 1 symbol
		Assert.Equal(8, result.CdlClassifiedBytes);
		Assert.Equal(1, result.SymbolCoveredBytes);
	}

	[Fact]
	public void Analyze_WithCrossRefs_CountsReferenceAddresses() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 32,
		};
		writer.AddCrossReference(new CrossReference(4, 20, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(8, 24, CrossRefType.Branch));

		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var rom = new byte[32];

		var result = PansyAnalyzer.Analyze(loader, rom);

		Assert.Equal(4, result.CrossRefCoveredBytes); // 4 unique addresses: 4, 20, 8, 24
	}

	[Fact]
	public void Analyze_CdlAndXrefOverlap_DoesNotDoubleCount() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 32,
		};
		// CDL marks byte 4 as code
		writer.MarkAsCode(4);
		// Cross-ref also references byte 4
		writer.AddCrossReference(new CrossReference(4, 20, CrossRefType.Jsr));

		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var rom = new byte[32];

		var result = PansyAnalyzer.Analyze(loader, rom);

		// Byte 4 counted as CDL, not double-counted. Byte 20 counted as xref.
		Assert.Equal(1, result.CdlClassifiedBytes);
		Assert.Equal(1, result.CrossRefCoveredBytes);
		Assert.Equal(2, result.ClassifiedBytes);
	}

	// ========================================================================
	// Symbol Boundary Analyzer (#34)
	// ========================================================================

	[Fact]
	public void BuildSymbolBoundaries_NoSymbols_ReturnsEmpty() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var boundaries = PansyAnalyzer.BuildSymbolBoundaries(loader, 256);

		Assert.Empty(boundaries);
	}

	[Fact]
	public void BuildSymbolBoundaries_MultipleSymbols_SpansBetweenThem() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x100,
		};
		writer.AddSymbol(0x00, "Reset", SymbolType.Function);
		writer.AddSymbol(0x40, "Nmi", SymbolType.Function);
		writer.AddSymbol(0x80, "DataTable", SymbolType.Label);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var boundaries = PansyAnalyzer.BuildSymbolBoundaries(loader, 0x100);

		Assert.Equal(3, boundaries.Count);

		Assert.Equal(0x00, boundaries[0].StartAddress);
		Assert.Equal(0x40, boundaries[0].EndAddress);
		Assert.Equal("Reset", boundaries[0].Name);
		Assert.Equal(0x40, boundaries[0].Length);

		Assert.Equal(0x40, boundaries[1].StartAddress);
		Assert.Equal(0x80, boundaries[1].EndAddress);
		Assert.Equal("Nmi", boundaries[1].Name);

		Assert.Equal(0x80, boundaries[2].StartAddress);
		Assert.Equal(0x100, boundaries[2].EndAddress);
		Assert.Equal("DataTable", boundaries[2].Name);
		Assert.Equal(0x80, boundaries[2].Length);
	}

	[Fact]
	public void BuildSymbolBoundaries_SingleSymbol_SpansToEnd() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 64,
		};
		writer.AddSymbol(16, "OnlySymbol");

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var boundaries = PansyAnalyzer.BuildSymbolBoundaries(loader, 64);

		Assert.Single(boundaries);
		Assert.Equal(16, boundaries[0].StartAddress);
		Assert.Equal(64, boundaries[0].EndAddress);
		Assert.Equal(48, boundaries[0].Length);
	}

	// ========================================================================
	// Cross-Reference Graph Stats (#33)
	// ========================================================================

	[Fact]
	public void BuildCrossRefStats_NoXrefs_ReturnsZeros() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var stats = PansyAnalyzer.BuildCrossRefStats(loader);

		Assert.Equal(0, stats.TotalCrossRefs);
		Assert.Equal(0, stats.JsrCount);
		Assert.Equal(0, stats.JmpCount);
		Assert.Equal(0, stats.BranchCount);
		Assert.Equal(0, stats.ReadCount);
		Assert.Equal(0, stats.WriteCount);
	}

	[Fact]
	public void BuildCrossRefStats_MixedXrefs_BreaksDownByType() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		writer.AddCrossReference(new CrossReference(0x00, 0x40, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x10, 0x50, CrossRefType.Jmp));
		writer.AddCrossReference(new CrossReference(0x20, 0x60, CrossRefType.Branch));
		writer.AddCrossReference(new CrossReference(0x30, 0x70, CrossRefType.Read));
		writer.AddCrossReference(new CrossReference(0x40, 0x80, CrossRefType.Write));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var stats = PansyAnalyzer.BuildCrossRefStats(loader);

		Assert.Equal(5, stats.TotalCrossRefs);
		Assert.Equal(1, stats.JsrCount);
		Assert.Equal(1, stats.JmpCount);
		Assert.Equal(1, stats.BranchCount);
		Assert.Equal(1, stats.ReadCount);
		Assert.Equal(1, stats.WriteCount);
	}

	[Fact]
	public void BuildCrossRefStats_MultipleXrefsToSameTarget_CountsUnique() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		writer.AddCrossReference(new CrossReference(0x00, 0x40, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x10, 0x40, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x20, 0x40, CrossRefType.Jsr));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var stats = PansyAnalyzer.BuildCrossRefStats(loader);

		Assert.Equal(3, stats.TotalCrossRefs);
		Assert.Equal(3, stats.UniqueSourceAddresses);
		Assert.Equal(1, stats.UniqueTargetAddresses);
	}

	[Fact]
	public void BuildCrossRefStats_MostReferenced_IncludesTopTargets() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		// Address 0x40 referenced 3 times, 0x50 once
		writer.AddCrossReference(new CrossReference(0x00, 0x40, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x10, 0x40, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x20, 0x40, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x30, 0x50, CrossRefType.Jsr));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var stats = PansyAnalyzer.BuildCrossRefStats(loader);

		Assert.True(stats.MostReferenced.Count > 0);
		Assert.Equal(0x40, stats.MostReferenced[0].Address);
		Assert.Equal(3, stats.MostReferenced[0].Count);
	}

	// ========================================================================
	// Pattern Detection (Phase 2)
	// ========================================================================

	[Fact]
	public void Analyze_WithPatternDetection_DetectsFillInGap() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 48,
		};
		// Mark first 16 bytes as code, leave rest as gap
		for (uint i = 0; i < 16; i++) writer.MarkAsCode(i);

		var pansyData = writer.Generate();
		var loader = new PansyLoader(pansyData);

		// ROM: 16 code bytes + 32 bytes of 0xff padding
		var rom = new byte[48];
		for (uint i = 16; i < 48; i++) rom[i] = 0xff;

		var result = PansyAnalyzer.Analyze(loader, rom, detectPatterns: true);

		Assert.Single(result.Patterns);
		Assert.Equal(PatternKind.Fill, result.Patterns[0].Kind);
		Assert.Equal(16, result.Patterns[0].Offset);
		Assert.Equal(32, result.Patterns[0].Length);
		Assert.Equal(1.0, result.Patterns[0].Confidence);
	}

	[Fact]
	public void Analyze_WithPatternDetection_DetectsAsciiString() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 32,
		};
		// Mark first 8 bytes as code
		for (uint i = 0; i < 8; i++) writer.MarkAsCode(i);

		var pansyData = writer.Generate();
		var loader = new PansyLoader(pansyData);

		// ROM: 8 code bytes + ASCII text
		var rom = new byte[32];
		var text = "Hello, World! Test123"u8;
		text.CopyTo(rom.AsSpan(8));

		var result = PansyAnalyzer.Analyze(loader, rom, detectPatterns: true);

		Assert.Single(result.Patterns);
		Assert.Equal(PatternKind.AsciiString, result.Patterns[0].Kind);
		Assert.Equal(8, result.Patterns[0].Offset);
	}

	[Fact]
	public void Analyze_WithoutPatternDetection_ReturnsNoPatterns() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 32,
		};
		for (uint i = 0; i < 16; i++) writer.MarkAsCode(i);

		var pansyData = writer.Generate();
		var loader = new PansyLoader(pansyData);
		var rom = new byte[32];
		for (uint i = 16; i < 32; i++) rom[i] = 0xff;

		var result = PansyAnalyzer.Analyze(loader, rom, detectPatterns: false);

		Assert.Empty(result.Patterns);
	}

	[Fact]
	public void TryDetectFill_AllSameByte_DetectsFill() {
		var data = new byte[32];
		Array.Fill(data, (byte)0xff);

		bool result = PansyAnalyzer.TryDetectFill(data, 0, out var pattern);

		Assert.True(result);
		Assert.Equal(PatternKind.Fill, pattern.Kind);
		Assert.Equal(32, pattern.Length);
		Assert.Equal(1.0, pattern.Confidence);
		Assert.Contains("0xff", pattern.Description);
	}

	[Fact]
	public void TryDetectFill_MixedBytes_ReturnsFalse() {
		var data = new byte[] { 0xff, 0xff, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };

		bool result = PansyAnalyzer.TryDetectFill(data, 0, out _);

		Assert.False(result);
	}

	[Fact]
	public void TryDetectFill_TooSmall_ReturnsFalse() {
		var data = new byte[] { 0xff, 0xff, 0xff };

		bool result = PansyAnalyzer.TryDetectFill(data, 0, out _);

		Assert.False(result);
	}

	[Fact]
	public void TryDetectAsciiString_PrintableText_DetectsString() {
		var data = "Test string data!"u8.ToArray();

		bool result = PansyAnalyzer.TryDetectAsciiString(data, 0, out var pattern);

		Assert.True(result);
		Assert.Equal(PatternKind.AsciiString, pattern.Kind);
		Assert.True(pattern.Confidence >= 0.85);
	}

	[Fact]
	public void TryDetectAsciiString_BinaryData_ReturnsFalse() {
		var data = new byte[] { 0x80, 0x90, 0xa0, 0xb0, 0xc0, 0xd0, 0xe0, 0xf0 };

		bool result = PansyAnalyzer.TryDetectAsciiString(data, 0, out _);

		Assert.False(result);
	}

	[Fact]
	public void TryDetectAsciiString_TooShort_ReturnsFalse() {
		var data = "Hi"u8.ToArray();

		bool result = PansyAnalyzer.TryDetectAsciiString(data, 0, out _);

		Assert.False(result);
	}

	// ========================================================================
	// Edge Cases
	// ========================================================================

	[Fact]
	public void Analyze_ZeroLengthRom_ReturnsZeros() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.Analyze(loader, [], detectPatterns: true);

		Assert.Equal(0, result.TotalBytes);
		Assert.Equal(0, result.ClassifiedBytes);
		Assert.Equal(0.0, result.CoveragePercent);
		Assert.Empty(result.Gaps);
		Assert.Empty(result.Patterns);
	}

	[Fact]
	public void Analyze_FullyCovered_NoGaps() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 8,
		};
		for (uint i = 0; i < 8; i++) writer.MarkAsData(i);

		var pansyData = writer.Generate();
		var loader = new PansyLoader(pansyData);
		var rom = new byte[8];

		var result = PansyAnalyzer.Analyze(loader, rom);

		Assert.Equal(100.0, result.CoveragePercent);
		Assert.Empty(result.Gaps);
	}

	[Fact]
	public void Analyze_LargeRom_PerformsCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000,
		};
		// Mark first 0x10000 bytes as code
		for (uint i = 0; i < 0x10000; i++) writer.MarkAsCode(i);

		var pansyData = writer.Generate();
		var loader = new PansyLoader(pansyData);
		var rom = new byte[0x20000];

		var result = PansyAnalyzer.Analyze(loader, rom);

		Assert.Equal(0x20000, result.TotalBytes);
		Assert.Equal(0x10000, result.ClassifiedBytes);
		Assert.Equal(50.0, result.CoveragePercent);
		Assert.Single(result.Gaps);
		Assert.Equal(0x10000, result.Gaps[0].Offset);
		Assert.Equal(0x10000, result.Gaps[0].Length);
	}
}
