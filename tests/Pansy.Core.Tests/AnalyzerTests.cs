using System.Linq;
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
	// Jump Graph Validation (#100)
	// ========================================================================

	[Fact]
	public void ValidateJumpGraph_OrphanJumpTarget_ReportsDiagnostic() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x100,
		};
		writer.MarkAsJumpTarget(0x40);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.ValidateJumpGraph(loader, 0x100);

		Assert.Contains(result.Diagnostics,
			d => d.Kind == JumpGraphDiagnosticKind.OrphanJumpTarget && d.Address == 0x40);
	}

	[Fact]
	public void ValidateJumpGraph_UnresolvedXrefTarget_ReportsError() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x100,
		};
		writer.AddCrossReference(new CrossReference(0x10, 0x80, CrossRefType.Jsr));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.ValidateJumpGraph(loader, 0x100);

		Assert.Contains(result.Diagnostics,
			d => d.Kind == JumpGraphDiagnosticKind.UnresolvedXrefTarget && d.Address == 0x80 && d.Severity == JumpGraphDiagnosticSeverity.Error);
		Assert.False(result.IsValid);
	}

	[Fact]
	public void ValidateJumpGraph_MalformedTypeDistribution_ReportsWarning() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x400,
		};

		for (uint i = 0; i < 12; i++) {
			writer.AddCrossReference(new CrossReference(0x20 + i, 0x80 + i, CrossRefType.Read));
		}

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.ValidateJumpGraph(loader, 0x400);

		Assert.Contains(result.Diagnostics,
			d => d.Kind == JumpGraphDiagnosticKind.MalformedTypeDistribution && d.Severity == JumpGraphDiagnosticSeverity.Warning);
	}

	[Fact]
	public void ValidateJumpGraph_ImpossibleAddress_ReportsError() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x200,
		};
		writer.AddCrossReference(new CrossReference(0x10, 0x1_0000, CrossRefType.Jmp));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var result = PansyAnalyzer.ValidateJumpGraph(loader, 0x200);

		Assert.Contains(result.Diagnostics,
			d => d.Kind == JumpGraphDiagnosticKind.ImpossibleAddress && d.Address == 0x1_0000 && d.Severity == JumpGraphDiagnosticSeverity.Error);
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

	[Fact]
	public void DescribeCpuState_GenesisM68000_InterpretsSupervisorTraceAndIplBits() {
		var entry = new CpuStateEntry(0x000200, 0x17, 0x00, 0x0000, CpuMode.M68000);
		var description = PansyAnalyzer.DescribeCpuState(entry, PansyLoader.PLATFORM_GENESIS);

		Assert.Equal("mode=M68000, s=1, t=1, ipl=5", description);
	}

	[Fact]
	public void DescribeCpuState_GenesisZ80_InterpretsInterruptModeBits() {
		var entry = new CpuStateEntry(0x00a000, 0x0f, 0x00, 0x0000, CpuMode.Z80);
		var description = PansyAnalyzer.DescribeCpuState(entry, PansyLoader.PLATFORM_GENESIS);

		Assert.Equal("mode=Z80, iff1=1, iff2=1, im=3", description);
	}

	[Fact]
	public void DescribeCpuState_Snes_InterpretsMXAndBankingState() {
		var entry = new CpuStateEntry(0x008000, 0x03, 0x7e, 0x2100, CpuMode.Native65816);
		var description = PansyAnalyzer.DescribeCpuState(entry, PansyLoader.PLATFORM_SNES);

		Assert.Equal("mode=65816 Native, x=8, m=8, db=$7e, dp=$2100", description);
	}

	[Fact]
	public void DescribeCpuState_ArmAndThumb_ReturnExpectedWidthDescriptions() {
		var armDescription = PansyAnalyzer.DescribeCpuState(
			new CpuStateEntry(0x08000000, 0x00, 0x00, 0x0000, CpuMode.ARM),
			PansyLoader.PLATFORM_GBA);
		var thumbDescription = PansyAnalyzer.DescribeCpuState(
			new CpuStateEntry(0x08001000, 0x00, 0x00, 0x0000, CpuMode.THUMB),
			PansyLoader.PLATFORM_GBA);

		Assert.Equal("mode=ARM, width=32-bit", armDescription);
		Assert.Equal("mode=THUMB, width=16-bit", thumbDescription);
	}

	[Theory]
	[InlineData(CpuMode.Native65816, "65816 Native")]
	[InlineData(CpuMode.Emulation6502, "65816 Emulation")]
	[InlineData(CpuMode.ARM, "ARM")]
	[InlineData(CpuMode.THUMB, "THUMB")]
	[InlineData(CpuMode.M68000, "M68000")]
	[InlineData(CpuMode.Z80, "Z80")]
	public void GetCpuModeName_ReturnsStableLabels(CpuMode mode, string expected) {
		Assert.Equal(expected, PansyAnalyzer.GetCpuModeName(mode));
	}

	// ========================================================================
	// Phase 3: Platform-Specific Address Helpers (#41)
	// ========================================================================

	[Theory]
	[InlineData(PansyLoader.PLATFORM_NES, 2)]
	[InlineData(PansyLoader.PLATFORM_GB, 2)]
	[InlineData(PansyLoader.PLATFORM_ATARI_2600, 2)]
	[InlineData(PansyLoader.PLATFORM_SMS, 2)]
	[InlineData(PansyLoader.PLATFORM_PCE, 2)]
	[InlineData(PansyLoader.PLATFORM_SNES, 3)]
	[InlineData(PansyLoader.PLATFORM_GBA, 4)]
	[InlineData(PansyLoader.PLATFORM_GENESIS, 4)]
	public void GetAddressSize_ReturnsCorrectSize(byte platform, int expected) {
		Assert.Equal(expected, PansyAnalyzer.GetAddressSize(platform));
	}

	[Fact]
	public void GetAddressSize_UnknownPlatform_Defaults2() {
		Assert.Equal(2, PansyAnalyzer.GetAddressSize(0xff));
	}

	[Fact]
	public void ReadAddress_NES_LittleEndian16() {
		byte[] data = [0x34, 0x12];
		uint addr = PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_NES);
		Assert.Equal(0x1234u, addr);
	}

	[Fact]
	public void ReadAddress_SNES_LittleEndian24() {
		byte[] data = [0x56, 0x34, 0x12];
		uint addr = PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_SNES);
		Assert.Equal(0x123456u, addr);
	}

	[Fact]
	public void ReadAddress_GBA_LittleEndian32() {
		byte[] data = [0x00, 0x00, 0x00, 0x08];
		uint addr = PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_GBA);
		Assert.Equal(0x08000000u, addr);
	}

	[Fact]
	public void ReadAddress_Genesis_BigEndian32() {
		byte[] data = [0x00, 0x12, 0x34, 0x56];
		uint addr = PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_GENESIS);
		Assert.Equal(0x00123456u, addr);
	}

	[Fact]
	public void ReadAddress_TooShort_ReturnsMaxValue() {
		byte[] data = [0x34];
		Assert.Equal(0xffffffffu, PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_NES));
		Assert.Equal(0xffffffffu, PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_SNES));
		Assert.Equal(0xffffffffu, PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_GBA));
		Assert.Equal(0xffffffffu, PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_GENESIS));
	}

	[Theory]
	[InlineData(PansyLoader.PLATFORM_NES, 0x8000u, true)]
	[InlineData(PansyLoader.PLATFORM_NES, 0x0000u, true)]
	[InlineData(PansyLoader.PLATFORM_GB, 0x0150u, true)]
	[InlineData(PansyLoader.PLATFORM_SNES, 0x808000u, true)]
	[InlineData(PansyLoader.PLATFORM_GBA, 0x08000000u, true)]
	[InlineData(PansyLoader.PLATFORM_GBA, 0x07ffffffu, false)]
	[InlineData(PansyLoader.PLATFORM_GBA, 0x0e010000u, false)]
	[InlineData(PansyLoader.PLATFORM_GENESIS, 0x000000u, true)]
	[InlineData(PansyLoader.PLATFORM_GENESIS, 0x01000000u, false)]
	[InlineData(PansyLoader.PLATFORM_ATARI_2600, 0xf000u, true)]
	[InlineData(PansyLoader.PLATFORM_ATARI_2600, 0xefffu, false)]
	public void IsValidAddress_RangeChecks(byte platform, uint addr, bool expected) {
		Assert.Equal(expected, PansyAnalyzer.IsValidAddress(addr, platform, null));
	}

	// ========================================================================
	// Phase 3: Pointer Table Detection (#41)
	// ========================================================================

	[Fact]
	public void TryDetectPointerTable_NES_ValidTable_Detects() {
		// 4 valid NES addresses (16-bit LE): $8000, $8100, $8200, $8300
		byte[] data = [
			0x00, 0x80, // $8000
			0x00, 0x81, // $8100
			0x00, 0x82, // $8200
			0x00, 0x83, // $8300
		];

		bool result = PansyAnalyzer.TryDetectPointerTable(
			data, 0, PansyLoader.PLATFORM_NES, null, out var pattern);

		Assert.True(result);
		Assert.Equal(PatternKind.PointerTable, pattern.Kind);
		Assert.Equal(8, pattern.Length);
		Assert.True(pattern.Confidence >= 0.75);
		Assert.Contains("16-bit", pattern.Description);
	}

	[Fact]
	public void TryDetectPointerTable_SNES_ValidTable_Detects() {
		// 3 valid SNES 24-bit addresses (LE): $808000, $808100, $818000
		byte[] data = [
			0x00, 0x80, 0x80, // $808000
			0x00, 0x81, 0x80, // $808100
			0x00, 0x80, 0x81, // $818000
		];

		bool result = PansyAnalyzer.TryDetectPointerTable(
			data, 0, PansyLoader.PLATFORM_SNES, null, out var pattern);

		Assert.True(result);
		Assert.Equal(PatternKind.PointerTable, pattern.Kind);
		Assert.Equal(9, pattern.Length);
		Assert.Contains("24-bit", pattern.Description);
	}

	[Fact]
	public void TryDetectPointerTable_GBA_ValidTable_Detects() {
		// 3 valid GBA 32-bit addresses (LE): $08000000, $08001000, $08002000
		byte[] data = [
			0x00, 0x00, 0x00, 0x08, // $08000000
			0x00, 0x10, 0x00, 0x08, // $08001000
			0x00, 0x20, 0x00, 0x08, // $08002000
		];

		bool result = PansyAnalyzer.TryDetectPointerTable(
			data, 0, PansyLoader.PLATFORM_GBA, null, out var pattern);

		Assert.True(result);
		Assert.Equal(PatternKind.PointerTable, pattern.Kind);
		Assert.Equal(12, pattern.Length);
		Assert.Contains("32-bit", pattern.Description);
	}

	[Fact]
	public void TryDetectPointerTable_Genesis_BigEndian_Detects() {
		// 3 valid Genesis 32-bit big-endian addresses
		byte[] data = [
			0x00, 0x00, 0x80, 0x00, // $00008000
			0x00, 0x00, 0x90, 0x00, // $00009000
			0x00, 0x00, 0xa0, 0x00, // $0000a000
		];

		bool result = PansyAnalyzer.TryDetectPointerTable(
			data, 0, PansyLoader.PLATFORM_GENESIS, null, out var pattern);

		Assert.True(result);
		Assert.Equal(PatternKind.PointerTable, pattern.Kind);
	}

	[Fact]
	public void TryDetectPointerTable_TooFewEntries_ReturnsFalse() {
		// Only 2 entries — need at least 3
		byte[] data = [0x00, 0x80, 0x00, 0x81];

		bool result = PansyAnalyzer.TryDetectPointerTable(
			data, 0, PansyLoader.PLATFORM_NES, null, out _);

		Assert.False(result);
	}

	[Fact]
	public void TryDetectPointerTable_TooShort_ReturnsFalse() {
		// Less than minEntries * addrSize
		byte[] data = [0x00, 0x80, 0x00];

		bool result = PansyAnalyzer.TryDetectPointerTable(
			data, 0, PansyLoader.PLATFORM_NES, null, out _);

		Assert.False(result);
	}

	[Fact]
	public void TryDetectPointerTable_GBA_InvalidAddresses_ReturnsFalse() {
		// Addresses outside GBA ROM range
		byte[] data = [
			0x00, 0x00, 0x00, 0x00, // $00000000 — invalid for GBA
			0x01, 0x00, 0x00, 0x00, // $00000001 — invalid
			0x02, 0x00, 0x00, 0x00, // $00000002 — invalid
		];

		bool result = PansyAnalyzer.TryDetectPointerTable(
			data, 0, PansyLoader.PLATFORM_GBA, null, out _);

		Assert.False(result);
	}

	// ========================================================================
	// Phase 3: Platform Tile Size Helpers (#40)
	// ========================================================================

	[Theory]
	[InlineData(PansyLoader.PLATFORM_NES, 16)]
	[InlineData(PansyLoader.PLATFORM_GB, 16)]
	[InlineData(PansyLoader.PLATFORM_SNES, 32)]
	[InlineData(PansyLoader.PLATFORM_SMS, 32)]
	[InlineData(PansyLoader.PLATFORM_PCE, 32)]
	public void GetTileSize_ReturnsCorrectSize(byte platform, int expected) {
		Assert.Equal(expected, PansyAnalyzer.GetTileSize(platform));
	}

	[Theory]
	[InlineData(PansyLoader.PLATFORM_GBA)]
	[InlineData(PansyLoader.PLATFORM_GENESIS)]
	[InlineData(PansyLoader.PLATFORM_ATARI_2600)]
	public void GetTileSize_UnsupportedPlatform_ReturnsZero(byte platform) {
		Assert.Equal(0, PansyAnalyzer.GetTileSize(platform));
	}

	// ========================================================================
	// Phase 3: Tile Data Detection (#40)
	// ========================================================================

	[Fact]
	public void TryDetectTileData_NES_ValidTiles_Detects() {
		// 4 NES 2bpp tiles (16 bytes each = 64 bytes)
		// Each tile has varied data (not all-zero or all-ones)
		var data = new byte[64];
		var rng = new Random(42);
		rng.NextBytes(data);
		// Ensure no tile is all-zero or all-ones
		for (int t = 0; t < 4; t++) {
			data[t * 16] = 0x55; // ensure variation
			data[t * 16 + 1] = 0xaa;
		}

		bool result = PansyAnalyzer.TryDetectTileData(
			data, 0, PansyLoader.PLATFORM_NES, out var pattern);

		Assert.True(result);
		Assert.Equal(PatternKind.TileData, pattern.Kind);
		Assert.Equal(64, pattern.Length);
		Assert.True(pattern.Confidence >= 0.60);
		Assert.Contains("2bpp", pattern.Description);
	}

	[Fact]
	public void TryDetectTileData_SNES_ValidTiles_Detects() {
		// 4 SNES 4bpp tiles (32 bytes each = 128 bytes)
		var data = new byte[128];
		var rng = new Random(42);
		rng.NextBytes(data);
		for (int t = 0; t < 4; t++) {
			data[t * 32] = 0x55;
			data[t * 32 + 1] = 0xaa;
		}

		bool result = PansyAnalyzer.TryDetectTileData(
			data, 0, PansyLoader.PLATFORM_SNES, out var pattern);

		Assert.True(result);
		Assert.Equal(PatternKind.TileData, pattern.Kind);
		Assert.Equal(128, pattern.Length);
		Assert.Contains("4bpp", pattern.Description);
	}

	[Fact]
	public void TryDetectTileData_AllZeroTiles_ReturnsFalse() {
		// All-zero tiles should be rejected as "padding"
		var data = new byte[64]; // 4 NES tiles, all zero

		bool result = PansyAnalyzer.TryDetectTileData(
			data, 0, PansyLoader.PLATFORM_NES, out _);

		Assert.False(result);
	}

	[Fact]
	public void TryDetectTileData_AllOnesTiles_ReturnsFalse() {
		var data = new byte[64];
		Array.Fill(data, (byte)0xff);

		bool result = PansyAnalyzer.TryDetectTileData(
			data, 0, PansyLoader.PLATFORM_NES, out _);

		Assert.False(result);
	}

	[Fact]
	public void TryDetectTileData_TooFewTiles_ReturnsFalse() {
		// Only 3 tiles — need at least 4
		var data = new byte[48];
		var rng = new Random(42);
		rng.NextBytes(data);

		bool result = PansyAnalyzer.TryDetectTileData(
			data, 0, PansyLoader.PLATFORM_NES, out _);

		Assert.False(result);
	}

	[Fact]
	public void TryDetectTileData_UnsupportedPlatform_ReturnsFalse() {
		var data = new byte[64];
		var rng = new Random(42);
		rng.NextBytes(data);

		bool result = PansyAnalyzer.TryDetectTileData(
			data, 0, PansyLoader.PLATFORM_GBA, out _);

		Assert.False(result);
	}

	[Fact]
	public void IsValidTile_NES_ValidTile_ReturnsTrue() {
		var tile = new byte[16];
		tile[0] = 0x55;
		tile[1] = 0xaa;

		Assert.True(PansyAnalyzer.IsValidTile(tile, PansyLoader.PLATFORM_NES));
	}

	[Fact]
	public void IsValidTile_AllZero_ReturnsFalse() {
		var tile = new byte[16];

		Assert.False(PansyAnalyzer.IsValidTile(tile, PansyLoader.PLATFORM_NES));
	}

	[Fact]
	public void IsValidTile_AllOnes_ReturnsFalse() {
		var tile = new byte[16];
		Array.Fill(tile, (byte)0xff);

		Assert.False(PansyAnalyzer.IsValidTile(tile, PansyLoader.PLATFORM_NES));
	}

	[Fact]
	public void IsValidTile_TooShort_ReturnsFalse() {
		var tile = new byte[8]; // NES needs 16
		tile[0] = 0x55;

		Assert.False(PansyAnalyzer.IsValidTile(tile, PansyLoader.PLATFORM_NES));
	}

	// ========================================================================
	// Phase 3: Integrated Pattern Detection in Gaps (#40, #41)
	// ========================================================================

	[Fact]
	public void DetectPatternsInGaps_PointerTableInGap_Detects() {
		// NES pointer table in a gap
		byte[] rom = new byte[32];
		// 4 valid NES addresses at offset 0
		rom[0] = 0x00; rom[1] = 0x80; // $8000
		rom[2] = 0x00; rom[3] = 0x81; // $8100
		rom[4] = 0x00; rom[5] = 0x82; // $8200
		rom[6] = 0x00; rom[7] = 0x83; // $8300

		var gaps = new List<GapRegion> { new(0, 8) };

		var patterns = PansyAnalyzer.DetectPatternsInGaps(
			gaps, rom, PansyLoader.PLATFORM_NES);

		Assert.Single(patterns);
		Assert.Equal(PatternKind.PointerTable, patterns[0].Kind);
	}

	[Fact]
	public void DetectPatternsInGaps_TileDataInGap_NES_PointerTableWins() {
		// For NES (full 16-bit address space), pointer table detection has
		// priority over tile data since all byte pairs are valid addresses.
		var rom = new byte[80];
		var rng = new Random(42);
		rng.NextBytes(rom);
		for (int t = 0; t < 4; t++) {
			rom[t * 16] = 0x55;
			rom[t * 16 + 1] = 0xaa;
		}

		var gaps = new List<GapRegion> { new(0, 64) };

		var patterns = PansyAnalyzer.DetectPatternsInGaps(
			gaps, rom, PansyLoader.PLATFORM_NES);

		Assert.Single(patterns);
		// Pointer table wins because all 16-bit values are valid NES addresses
		Assert.Equal(PatternKind.PointerTable, patterns[0].Kind);
	}

	[Fact]
	public void DetectPatternsInGaps_TileDataInGap_GBA_InvalidPointers() {
		// For GBA (restricted address range 0x08000000-0x0e00ffff),
		// random data won't form valid pointers, so tile detection can be
		// tested indirectly — but GBA has no tile format, so nothing detected.
		var rom = new byte[80];
		var rng = new Random(42);
		rng.NextBytes(rom);

		var gaps = new List<GapRegion> { new(0, 64) };

		var patterns = PansyAnalyzer.DetectPatternsInGaps(
			gaps, rom, PansyLoader.PLATFORM_GBA);

		// GBA: pointers invalid (random data not in 0x080xxxxx range),
		// no tile format defined — no patterns detected
		Assert.Empty(patterns);
	}

	[Fact]
	public void DetectPatternsInGaps_FillHasPriority_OverPointerTable() {
		// Fill pattern should be detected before pointer tables
		var rom = new byte[32];
		Array.Fill(rom, (byte)0xff);

		var gaps = new List<GapRegion> { new(0, 32) };

		var patterns = PansyAnalyzer.DetectPatternsInGaps(
			gaps, rom, PansyLoader.PLATFORM_NES);

		Assert.Single(patterns);
		Assert.Equal(PatternKind.Fill, patterns[0].Kind);
	}

	[Fact]
	public void Analyze_NES_PointerTableInGap_EndToEnd() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 24,
		};
		// Mark first 8 bytes as code
		for (uint i = 0; i < 8; i++) writer.MarkAsCode(i);

		var pansyData = writer.Generate();
		var loader = new PansyLoader(pansyData);

		// ROM: 8 code bytes + pointer table with 4 valid addresses
		var rom = new byte[24];
		rom[8] = 0x00; rom[9] = 0x80;
		rom[10] = 0x00; rom[11] = 0x81;
		rom[12] = 0x00; rom[13] = 0x82;
		rom[14] = 0x00; rom[15] = 0x83;
		rom[16] = 0x00; rom[17] = 0x84;
		rom[18] = 0x00; rom[19] = 0x85;
		rom[20] = 0x00; rom[21] = 0x86;
		rom[22] = 0x00; rom[23] = 0x87;

		var result = PansyAnalyzer.Analyze(loader, rom, detectPatterns: true);

		Assert.Single(result.Patterns);
		Assert.Equal(PatternKind.PointerTable, result.Patterns[0].Kind);
		Assert.Equal(8, result.Patterns[0].Offset);
	}

	// ========================================================================
	// Phase 3: Auto-Annotation Generator (#42)
	// ========================================================================

	[Fact]
	public void GenerateAnnotations_EmptyAnalysis_PreservesExistingData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
			RomCrc32 = 0xdeadbeef,
			ProjectName = "TestProject",
			Author = "TestAuthor",
		};
		writer.AddSymbol(0x10, "Reset", SymbolType.Function);
		writer.AddComment(0x10, "Entry point", (byte)CommentType.Inline);
		writer.AddCrossReference(new CrossReference(0x00, 0x10, CrossRefType.Jsr));

		var data = writer.Generate();
		var source = new PansyLoader(data);

		var emptyAnalysis = new AnalysisResult {
			TotalBytes = 256,
			ClassifiedBytes = 0,
			Gaps = [],
			Patterns = [],
		};

		var output = PansyAnalyzer.GenerateAnnotations(source, emptyAnalysis);
		var result = new PansyLoader(output);

		Assert.Equal(PansyLoader.PLATFORM_NES, result.Platform);
		Assert.Equal(256u, result.RomSize);
		Assert.Equal(0xdeadbeefu, result.RomCrc32);
		Assert.Equal("TestProject", result.ProjectName);
		Assert.Equal("TestAuthor", result.Author);
		Assert.Equal("Reset", result.GetSymbol(0x10));
		Assert.NotNull(result.GetComment(0x10));
		Assert.Single(result.CrossReferences);
	}

	[Fact]
	public void GenerateAnnotations_FillPattern_AddsCommentAndMarkData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 64,
		};
		var data = writer.Generate();
		var source = new PansyLoader(data);

		var analysis = new AnalysisResult {
			TotalBytes = 64,
			Gaps = [new GapRegion(0, 32)],
			Patterns = [new DetectedPattern {
				Offset = 0,
				Length = 32,
				Kind = PatternKind.Fill,
				Confidence = 1.0,
				Description = "Fill: 0xff x 32",
			}],
		};

		var output = PansyAnalyzer.GenerateAnnotations(source, analysis);
		var result = new PansyLoader(output);

		// Should have a comment at offset 0
		Assert.NotNull(result.GetComment(0));
		// Bytes should be marked as data
		Assert.True(result.IsData(0));
		Assert.True(result.IsData(31));
	}

	[Fact]
	public void GenerateAnnotations_AsciiPattern_AddsSymbolAndComment() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 64,
		};
		var data = writer.Generate();
		var source = new PansyLoader(data);

		var analysis = new AnalysisResult {
			TotalBytes = 64,
			Gaps = [new GapRegion(0x10, 16)],
			Patterns = [new DetectedPattern {
				Offset = 0x10,
				Length = 16,
				Kind = PatternKind.AsciiString,
				Confidence = 0.95,
				Description = "ASCII text (95% printable)",
			}],
		};

		var output = PansyAnalyzer.GenerateAnnotations(source, analysis);
		var result = new PansyLoader(output);

		Assert.Equal("str_000010", result.GetSymbol(0x10));
		Assert.NotNull(result.GetComment(0x10));
		Assert.True(result.IsData(0x10));
	}

	[Fact]
	public void GenerateAnnotations_PointerTablePattern_AddsSymbolAndComment() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 64,
		};
		var data = writer.Generate();
		var source = new PansyLoader(data);

		var analysis = new AnalysisResult {
			TotalBytes = 64,
			Gaps = [new GapRegion(0x20, 8)],
			Patterns = [new DetectedPattern {
				Offset = 0x20,
				Length = 8,
				Kind = PatternKind.PointerTable,
				Confidence = 1.0,
				Description = "Pointer table: 4/4 valid 16-bit addresses",
			}],
		};

		var output = PansyAnalyzer.GenerateAnnotations(source, analysis);
		var result = new PansyLoader(output);

		Assert.Equal("ptrtbl_000020", result.GetSymbol(0x20));
		Assert.NotNull(result.GetComment(0x20));
		Assert.True(result.IsData(0x20));
	}

	[Fact]
	public void GenerateAnnotations_TileDataPattern_AddsSymbolAndMarksDrawn() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 128,
		};
		var data = writer.Generate();
		var source = new PansyLoader(data);

		var analysis = new AnalysisResult {
			TotalBytes = 128,
			Gaps = [new GapRegion(0x00, 64)],
			Patterns = [new DetectedPattern {
				Offset = 0x00,
				Length = 64,
				Kind = PatternKind.TileData,
				Confidence = 0.80,
				Description = "2bpp tile data: 4/4 valid tiles (64 bytes)",
			}],
		};

		var output = PansyAnalyzer.GenerateAnnotations(source, analysis);
		var result = new PansyLoader(output);

		Assert.Equal("tiles_000000", result.GetSymbol(0x00));
		Assert.NotNull(result.GetComment(0x00));
		// Tile data marked as Drawn (not Data)
		Assert.True(result.IsDrawn(0));
		Assert.True(result.IsDrawn(63));
	}

	[Fact]
	public void GenerateAnnotations_UnclassifiedGap_AddsTodoComment() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 64,
		};
		var data = writer.Generate();
		var source = new PansyLoader(data);

		// Gap at offset 0x10, length 32 — no pattern matches it
		var analysis = new AnalysisResult {
			TotalBytes = 64,
			Gaps = [new GapRegion(0x10, 32)],
			Patterns = [], // No patterns detected
		};

		var output = PansyAnalyzer.GenerateAnnotations(source, analysis);
		var result = new PansyLoader(output);

		var comment = result.GetComment(0x10);
		Assert.NotNull(comment);
		Assert.Contains("Unclassified gap", comment);
	}

	[Fact]
	public void GenerateAnnotations_SmallGapWithoutPattern_NoTodoComment() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 32,
		};
		var data = writer.Generate();
		var source = new PansyLoader(data);

		// Gap smaller than 16 bytes — should not get a todo comment
		var analysis = new AnalysisResult {
			TotalBytes = 32,
			Gaps = [new GapRegion(0x00, 8)],
			Patterns = [],
		};

		var output = PansyAnalyzer.GenerateAnnotations(source, analysis);
		var result = new PansyLoader(output);

		Assert.Null(result.GetComment(0x00));
	}

	[Fact]
	public void GenerateAnnotations_Roundtrip_OutputIsValidPansy() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 256,
			RomCrc32 = 0x12345678,
			ProjectName = "RoundtripTest",
		};
		writer.AddSymbol(0x00, "Reset", SymbolType.Function);
		writer.MarkAsCode(0);

		var data = writer.Generate();
		var source = new PansyLoader(data);

		var analysis = new AnalysisResult {
			TotalBytes = 256,
			Gaps = [new GapRegion(0x80, 128)],
			Patterns = [
				new DetectedPattern {
					Offset = 0x80,
					Length = 64,
					Kind = PatternKind.Fill,
					Confidence = 1.0,
				},
			],
		};

		var output = PansyAnalyzer.GenerateAnnotations(source, analysis);

		// Output should be loadable as valid Pansy
		var result = new PansyLoader(output);
		Assert.Equal(PansyLoader.PLATFORM_SNES, result.Platform);
		Assert.Equal(256u, result.RomSize);
		Assert.Equal("RoundtripTest", result.ProjectName);
		// Original symbol preserved
		Assert.Equal("Reset", result.GetSymbol(0x00));
		// New annotation added
		Assert.NotNull(result.GetComment(0x80));
	}

	// ========================================================================
	// Auto-Label Generation (#70)
	// ========================================================================

	[Fact]
	public void GenerateAutoLabels_SubEntryPoints_LabeledAsSub() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		writer.MarkAsSubroutine(0x10);
		writer.MarkAsSubroutine(0x20);
		writer.MarkAsCode(0x10);
		writer.MarkAsCode(0x20);
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "sub_0010" && l.Address == 0x10 && l.Type == SymbolType.Function);
		Assert.Contains(labels, l => l.Name == "sub_0020" && l.Address == 0x20 && l.Type == SymbolType.Function);
	}

	[Fact]
	public void GenerateAutoLabels_JumpTargets_LabeledAsLoc() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		writer.MarkAsJumpTarget(0x30);
		writer.MarkAsCode(0x30);
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "loc_0030" && l.Address == 0x30 && l.Type == SymbolType.Label);
	}

	[Fact]
	public void GenerateAutoLabels_ExistingSymbols_NotOverwritten() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		writer.MarkAsSubroutine(0x10);
		writer.MarkAsCode(0x10);
		writer.AddSymbol(0x10, "MyRoutine", SymbolType.Function);
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.DoesNotContain(labels, l => l.Address == 0x10);
	}

	[Fact]
	public void GenerateAutoLabels_NesHardwareRegisters_Included() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "PPUCTRL" && l.Address == 0x2000);
		Assert.Contains(labels, l => l.Name == "PPUMASK" && l.Address == 0x2001);
		Assert.Contains(labels, l => l.Name == "OAMDMA" && l.Address == 0x4014);
	}

	[Fact]
	public void GenerateAutoLabels_SnesHardwareRegisters_Included() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "INIDISP" && l.Address == 0x2100);
		Assert.Contains(labels, l => l.Name == "VMADDL" && l.Address == 0x2116);
	}

	[Fact]
	public void GenerateAutoLabels_GbHardwareRegisters_Included() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_GB,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "LCDC" && l.Address == 0xff40);
		Assert.Contains(labels, l => l.Name == "IF" && l.Address == 0xff0f);
	}

	[Fact]
	public void GenerateAutoLabels_HwRegisterNotOverriddenByUserSymbol() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		// User already named $2000 something else
		writer.AddSymbol(0x2000, "MyPPUCtrl", SymbolType.Label);
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.DoesNotContain(labels, l => l.Address == 0x2000);
	}

	[Fact]
	public void GenerateAutoLabels_JumpTargetOverlappingHwRegister_NotDuplicated() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		// Jump target at a hardware register address
		writer.MarkAsJumpTarget(0x2000);
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		// Should have the hw register name, not loc_2000
		var at2000 = labels.Where(l => l.Address == 0x2000).ToList();
		Assert.Single(at2000);
		Assert.Equal("PPUCTRL", at2000[0].Name);
	}

	[Fact]
	public void GetInterruptVectorLabels_Nes_ReadsVectors() {
		// Create NES-sized ROM with vectors at $FFFA-$FFFF
		var romData = new byte[0x10000];
		// NMI = $8000
		romData[0xfffa] = 0x00;
		romData[0xfffb] = 0x80;
		// RESET = $C000
		romData[0xfffc] = 0x00;
		romData[0xfffd] = 0xc0;
		// IRQ = $E000
		romData[0xfffe] = 0x00;
		romData[0xffff] = 0xe0;

		var labels = PansyAnalyzer.GetInterruptVectorLabels(PansyLoader.PLATFORM_NES, romData);

		Assert.Contains(labels, l => l.Name == "nmi_handler" && l.Address == 0x8000 && l.Type == SymbolType.Function);
		Assert.Contains(labels, l => l.Name == "reset" && l.Address == 0xc000 && l.Type == SymbolType.Function);
		Assert.Contains(labels, l => l.Name == "irq_handler" && l.Address == 0xe000 && l.Type == SymbolType.Function);
	}

	[Fact]
	public void GetInterruptVectorLabels_Snes_ReadsNativeAndEmuVectors() {
		var romData = new byte[0x10000];
		// Native NMI = $0180
		romData[0xffea] = 0x80;
		romData[0xffeb] = 0x01;
		// Emulation RESET = $8000
		romData[0xfffc] = 0x00;
		romData[0xfffd] = 0x80;

		var labels = PansyAnalyzer.GetInterruptVectorLabels(PansyLoader.PLATFORM_SNES, romData);

		Assert.Contains(labels, l => l.Name == "native_nmi_handler" && l.Address == 0x0180);
		Assert.Contains(labels, l => l.Name == "emu_reset" && l.Address == 0x8000);
	}

	[Fact]
	public void GetInterruptVectorLabels_Gb_ReadsHandlersAndEntry() {
		var romData = new byte[0x200];

		var labels = PansyAnalyzer.GetInterruptVectorLabels(PansyLoader.PLATFORM_GB, romData);

		Assert.Contains(labels, l => l.Name == "vblank_handler" && l.Address == 0x0040);
		Assert.Contains(labels, l => l.Name == "stat_handler" && l.Address == 0x0048);
		Assert.Contains(labels, l => l.Name == "timer_handler" && l.Address == 0x0050);
		Assert.Contains(labels, l => l.Name == "serial_handler" && l.Address == 0x0058);
		Assert.Contains(labels, l => l.Name == "joypad_handler" && l.Address == 0x0060);
		Assert.Contains(labels, l => l.Name == "entry_point" && l.Address == 0x0100);
	}

	[Fact]
	public void GetInterruptVectorLabels_ZeroTarget_Skipped() {
		var romData = new byte[0x10000];
		// NMI points to $0000 — should be skipped
		romData[0xfffa] = 0x00;
		romData[0xfffb] = 0x00;
		// RESET = $C000 — should be included
		romData[0xfffc] = 0x00;
		romData[0xfffd] = 0xc0;

		var labels = PansyAnalyzer.GetInterruptVectorLabels(PansyLoader.PLATFORM_NES, romData);

		Assert.DoesNotContain(labels, l => l.Name == "nmi_handler");
		Assert.Contains(labels, l => l.Name == "reset" && l.Address == 0xc000);
	}

	[Fact]
	public void GetInterruptVectorLabels_UnknownPlatform_ReturnsEmpty() {
		var romData = new byte[0x10000];
		var labels = PansyAnalyzer.GetInterruptVectorLabels(0xfe, romData);
		Assert.Empty(labels);
	}

	[Fact]
	public void GenerateAutoLabels_VectorTargets_IncludedForNes() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x10000,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var romData = new byte[0x10000];
		romData[0xfffc] = 0x00;
		romData[0xfffd] = 0xc0;

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "reset" && l.Address == 0xc000);
	}

	[Fact]
	public void GenerateAutoLabels_VectorTarget_SkippedIfUserSymbolExists() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x10000,
		};
		writer.AddSymbol(0xc000, "MainEntry", SymbolType.Function);
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var romData = new byte[0x10000];
		romData[0xfffc] = 0x00;
		romData[0xfffd] = 0xc0;

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.DoesNotContain(labels, l => l.Address == 0xc000);
	}

	[Fact]
	public void GenerateAnnotations_WithRomData_InjectsAutoLabels() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x10000,
		};
		writer.MarkAsSubroutine(0x10);
		writer.MarkAsCode(0x10);
		var data = writer.Generate();
		var source = new PansyLoader(data);

		var analysis = new AnalysisResult {
			TotalBytes = 0x10000,
			Gaps = [],
			Patterns = [],
		};

		var romData = new byte[0x10000];
		romData[0xfffc] = 0x00;
		romData[0xfffd] = 0xc0;

		var output = PansyAnalyzer.GenerateAnnotations(source, analysis, romData);
		var result = new PansyLoader(output);

		// Auto-generated sub label
		Assert.Equal("sub_0010", result.GetSymbol(0x10));
		// Vector target label
		Assert.Equal("reset", result.GetSymbol(0xc000));
		// Hardware register label
		Assert.Equal("PPUCTRL", result.GetSymbol(0x2000));
	}

	[Fact]
	public void GenerateAnnotations_WithoutRomData_NoAutoLabels() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 256,
		};
		writer.MarkAsSubroutine(0x10);
		writer.MarkAsCode(0x10);
		var data = writer.Generate();
		var source = new PansyLoader(data);

		var analysis = new AnalysisResult {
			TotalBytes = 256,
			Gaps = [],
			Patterns = [],
		};

		// No romData argument → no auto-labels
		var output = PansyAnalyzer.GenerateAnnotations(source, analysis);
		var result = new PansyLoader(output);

		Assert.Null(result.GetSymbol(0x10));
		Assert.Null(result.GetSymbol(0x2000));
	}

	[Fact]
	public void GenerateAutoLabels_GbaHardwareRegisters_Included() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_GBA,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "DISPCNT" && l.Address == 0x04000000);
		Assert.Contains(labels, l => l.Name == "IE" && l.Address == 0x04000200);
	}

	[Fact]
	public void GenerateAutoLabels_Atari2600HardwareRegisters_Included() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_ATARI_2600,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "VSYNC");
		Assert.Contains(labels, l => l.Name == "VBLANK");
	}

	[Fact]
	public void GenerateAutoLabels_PceHardwareRegisters_Included() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_PCE,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "VDC_AR");
	}

	[Fact]
	public void GenerateAutoLabels_SmsMasterSystem_Included() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SMS,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "VDP_DATA");
	}

	[Fact]
	public void GenerateAutoLabels_WonderSwanRegisters_Included() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_WONDERSWAN,
			RomSize = 256,
		};
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		var romData = new byte[256];

		var labels = PansyAnalyzer.GenerateAutoLabels(loader, romData);

		Assert.Contains(labels, l => l.Name == "DISPLAY_CTRL");
	}
}
