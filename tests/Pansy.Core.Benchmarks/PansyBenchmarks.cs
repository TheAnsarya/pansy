// ============================================================================
// PansyBenchmarks.cs - Performance benchmarks for Pansy.Core
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Pansy.Core;

BenchmarkSwitcher.FromAssembly(typeof(WriterBenchmarks).Assembly).Run(args);

/// <summary>
/// Benchmarks for PansyWriter generation performance at various scales.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class WriterBenchmarks {
	[Params(100, 1000, 10000)]
	public int SymbolCount { get; set; }

	[Benchmark]
	public byte[] WriteSymbols() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			writer.AddSymbol(0x8000 + i, $"Symbol_{i:x4}");
		}
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteCodeDataMap() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			writer.MarkAsCode(i);
			if (i % 10 == 0) writer.MarkAsJumpTarget(i);
			if (i % 50 == 0) writer.MarkAsSubroutine(i);
			if (i % 3 == 0) writer.MarkAsOpcode(i);
		}
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteComments() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			writer.AddComment(0x8000 + i, $"Comment for address ${0x8000 + i:x4}");
		}
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteCrossRefs() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			writer.AddCrossReference(new CrossReference(0x8000 + i, 0x9000 + i, CrossRefType.Jsr));
		}
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteFullFile() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000,
			RomCrc32 = 0x12345678,
			ProjectName = "Benchmark Project",
			Author = "Benchmark",
			ProjectVersion = "1.0.0"
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			writer.AddSymbol(0x8000 + i, $"Sym_{i:x4}");
			writer.AddComment(0x8000 + i, $"Comment {i}");
			writer.MarkAsCode(i);
			if (i % 5 == 0) writer.MarkAsOpcode(i);
			writer.AddCrossReference(new CrossReference(0x8000 + i, 0x9000 + i, CrossRefType.Jsr));
		}
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, 1, 0, "ROM"));
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteCompressed() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000,
			EnableCompression = true
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			writer.AddSymbol(0x8000 + i, $"Symbol_{i:x4}");
			writer.MarkAsCode(i);
		}
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteTypedSymbols() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			var type = (SymbolType)((i % 9) + 1);
			writer.AddSymbol(0x8000 + i, $"TypedSym_{i:x4}", type);
		}
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteTypedComments() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			var ct = (byte)((i % 3) + 1);
			writer.AddComment(0x8000 + i, $"Typed comment {i}", ct);
		}
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteExtendedFlags() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			writer.MarkAsData(i);
			if (i % 3 == 0) writer.MarkAsDrawn(i);
			if (i % 5 == 0) writer.MarkAsRead(i);
			if (i % 7 == 0) writer.MarkAsIndirect(i);
		}
		return writer.Generate();
	}

	[Benchmark]
	public byte[] WriteFullFileExtended() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000,
			RomCrc32 = 0x12345678,
			ProjectName = "Extended Benchmark",
			Author = "Benchmark",
			ProjectVersion = "2.0.0",
			EnableCompression = true
		};
		for (uint i = 0; i < (uint)SymbolCount; i++) {
			var symType = (SymbolType)((i % 9) + 1);
			writer.AddSymbol(0x8000 + i, $"Sym_{i:x4}", symType);
			var cmtType = (byte)((i % 3) + 1);
			writer.AddComment(0x8000 + i, $"Comment {i}", cmtType);
			writer.MarkAsCode(i);
			if (i % 5 == 0) writer.MarkAsOpcode(i);
			if (i % 3 == 0) writer.MarkAsDrawn(i);
			if (i % 7 == 0) writer.MarkAsIndirect(i);
			writer.AddCrossReference(new CrossReference(0x8000 + i, 0x9000 + i, CrossRefType.Jsr));
		}
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, 1, 0, "ROM"));
		return writer.Generate();
	}
}

/// <summary>
/// Benchmarks for PansyLoader parsing performance at various scales.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class LoaderBenchmarks {
	private byte[]? _smallFile;
	private byte[]? _mediumFile;
	private byte[]? _largeFile;
	private byte[]? _compressedFile;

	// Pre-loaded loaders for pure lookup benchmarks (no loading cost)
	private PansyLoader? _mediumLoader;
	private PansyLoader? _largeLoader;

	[GlobalSetup]
	public void Setup() {
		_smallFile = GenerateFile(100);
		_mediumFile = GenerateFile(1000);
		_largeFile = GenerateFile(10000);
		_compressedFile = GenerateFile(10000, compressed: true);

		// Pre-load for pure lookup benchmarks
		_mediumLoader = new PansyLoader(_mediumFile);
		_largeLoader = new PansyLoader(_largeFile);
	}

	private static byte[] GenerateFile(int count, bool compressed = false) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000,
			RomCrc32 = 0xdeadbeef,
			EnableCompression = compressed,
			ProjectName = "Bench",
			Author = "Test"
		};
		for (uint i = 0; i < (uint)count; i++) {
			var symType = (SymbolType)((i % 9) + 1);
			writer.AddSymbol(0x8000 + i, $"Sym_{i:x4}", symType);
			var cmtType = (byte)((i % 3) + 1);
			writer.AddComment(0x8000 + i, $"Comment {i}", cmtType);
			writer.MarkAsCode(i);
			if (i % 5 == 0) writer.MarkAsOpcode(i);
			if (i % 10 == 0) writer.MarkAsJumpTarget(i);
			if (i % 50 == 0) writer.MarkAsSubroutine(i);
			if (i % 3 == 0) writer.MarkAsDrawn(i);
			if (i % 7 == 0) writer.MarkAsIndirect(i);
			writer.AddCrossReference(new CrossReference(0x8000 + i, 0x9000 + i, CrossRefType.Jsr));
		}
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, 1, 0, "ROM"));
		return writer.Generate();
	}

	[Benchmark]
	public PansyLoader LoadSmallFile() => new(_smallFile!);

	[Benchmark]
	public PansyLoader LoadMediumFile() => new(_mediumFile!);

	[Benchmark]
	public PansyLoader LoadLargeFile() => new(_largeFile!);

	[Benchmark]
	public PansyLoader LoadCompressedFile() => new(_compressedFile!);

	[Benchmark]
	public bool LookupSymbol() {
		var loader = new PansyLoader(_mediumFile!);
		var found = false;
		for (int i = 0; i < 1000; i++) {
			found |= loader.GetSymbol(0x8000 + i) != null;
		}
		return found;
	}

	[Benchmark]
	public bool LookupCodeOffsets() {
		var loader = new PansyLoader(_mediumFile!);
		var count = 0;
		for (int i = 0; i < 1000; i++) {
			if (loader.IsCode(i)) count++;
		}
		return count > 0;
	}

	[Benchmark]
	public int LookupSymbolTypes() {
		var loader = new PansyLoader(_mediumFile!);
		var count = 0;
		for (int i = 0; i < 1000; i++) {
			if (loader.GetSymbolType(0x8000 + i) != null) count++;
		}
		return count;
	}

	[Benchmark]
	public int LookupCommentTypes() {
		var loader = new PansyLoader(_mediumFile!);
		var count = 0;
		for (int i = 0; i < 1000; i++) {
			if (loader.GetCommentType(0x8000 + i) != null) count++;
		}
		return count;
	}

	[Benchmark]
	public int LookupExtendedFlags() {
		var loader = new PansyLoader(_largeFile!);
		var count = 0;
		for (int i = 0; i < 10000; i++) {
			if (loader.IsDrawn(i)) count++;
			if (loader.IsRead(i)) count++;
			if (loader.IsIndirect(i)) count++;
		}
		return count;
	}

	[Benchmark]
	public PansyLoader LoadLargeExtendedCompressed() => new(_compressedFile!);

	// --- Pure lookup benchmarks (pre-loaded, no parsing cost) ---

	[Benchmark]
	public int PureLookupSymbols() {
		var count = 0;
		for (int i = 0; i < 1000; i++) {
			if (_mediumLoader!.GetSymbol(0x8000 + i) != null) count++;
		}
		return count;
	}

	[Benchmark]
	public int PureLookupCodeOffsets() {
		var count = 0;
		for (int i = 0; i < 1000; i++) {
			if (_mediumLoader!.IsCode(i)) count++;
		}
		return count;
	}

	[Benchmark]
	public int PureAccessSymbolsProperty() {
		var total = 0;
		for (int i = 0; i < 100; i++) {
			total += _mediumLoader!.Symbols.Count;
		}
		return total;
	}

	[Benchmark]
	public int PureAccessCommentsProperty() {
		var total = 0;
		for (int i = 0; i < 100; i++) {
			total += _mediumLoader!.Comments.Count;
		}
		return total;
	}

	[Benchmark]
	public int PureLookupExtendedFlags() {
		var count = 0;
		for (int i = 0; i < 10000; i++) {
			if (_largeLoader!.IsDrawn(i)) count++;
			if (_largeLoader!.IsRead(i)) count++;
			if (_largeLoader!.IsIndirect(i)) count++;
		}
		return count;
	}
}

/// <summary>
/// Benchmarks for PansyAnalyzer — coverage analysis, pattern detection, and auto-annotation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class AnalyzerBenchmarks {
	private PansyLoader _sparseLoader = null!;
	private PansyLoader _denseLoader = null!;
	private byte[] _sparseRom = null!;
	private byte[] _denseRom = null!;
	private byte[] _fillRom = null!;
	private byte[] _pointerTableRom = null!;

	[GlobalSetup]
	public void Setup() {
		// Sparse: ~25% coverage (gaps available for pattern detection)
		var sparseWriter = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
		};
		for (uint i = 0; i < 0x2000; i++) sparseWriter.MarkAsCode(i);
		sparseWriter.AddSymbol(0x0000, "Reset", SymbolType.Function);
		sparseWriter.AddSymbol(0x1000, "Nmi", SymbolType.Function);
		sparseWriter.AddCrossReference(new CrossReference(0x0000, 0x1000, CrossRefType.Jsr));
		_sparseLoader = new PansyLoader(sparseWriter.Generate());
		_sparseRom = new byte[0x8000];
		// Fill the gap with 0xff padding
		Array.Fill(_sparseRom, (byte)0xff, 0x2000, 0x6000);

		// Dense: ~80% coverage (fewer gaps)
		var denseWriter = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
		};
		for (uint i = 0; i < 0x6000; i++) denseWriter.MarkAsCode(i);
		for (uint i = 0; i < 100; i++) {
			denseWriter.AddSymbol(i * 0x100, $"Sub_{i:x2}", SymbolType.Function);
		}
		_denseLoader = new PansyLoader(denseWriter.Generate());
		_denseRom = new byte[0x8000];

		// ROM with pointer tables for detection benchmarks
		_pointerTableRom = new byte[0x8000];
		for (int i = 0x2000; i < 0x4000; i += 2) {
			_pointerTableRom[i] = (byte)(0x8000 + i);
			_pointerTableRom[i + 1] = 0x80;
		}

		// ROM with fill region
		_fillRom = new byte[0x8000];
		Array.Fill(_fillRom, (byte)0xff, 0x2000, 0x4000);
	}

	[Benchmark(Description = "AnalyzeCoverage (32KB sparse)")]
	public AnalysisResult AnalyzeCoverageSparse() {
		return PansyAnalyzer.AnalyzeCoverage(_sparseLoader, 0x8000);
	}

	[Benchmark(Description = "AnalyzeCoverage (32KB dense)")]
	public AnalysisResult AnalyzeCoverageDense() {
		return PansyAnalyzer.AnalyzeCoverage(_denseLoader, 0x8000);
	}

	[Benchmark(Description = "FullAnalyze no patterns (32KB)")]
	public AnalysisResult FullAnalyzeNoPatterns() {
		return PansyAnalyzer.Analyze(_sparseLoader, _sparseRom, detectPatterns: false);
	}

	[Benchmark(Description = "FullAnalyze with patterns (32KB)")]
	public AnalysisResult FullAnalyzeWithPatterns() {
		return PansyAnalyzer.Analyze(_sparseLoader, _sparseRom, detectPatterns: true);
	}

	[Benchmark(Description = "BuildSymbolBoundaries")]
	public IReadOnlyList<SymbolBoundary> BuildBoundaries() {
		return PansyAnalyzer.BuildSymbolBoundaries(_denseLoader, 0x8000);
	}

	[Benchmark(Description = "BuildCrossRefStats")]
	public CrossRefGraphStats BuildXrefStats() {
		return PansyAnalyzer.BuildCrossRefStats(_sparseLoader);
	}

	[Benchmark(Description = "DetectFill (24KB span)")]
	public bool DetectFill() {
		return PansyAnalyzer.TryDetectFill(
			_fillRom.AsSpan(0x2000, 0x4000), 0x2000, out _);
	}

	[Benchmark(Description = "DetectPointerTable (8KB NES)")]
	public bool DetectPointerTable() {
		return PansyAnalyzer.TryDetectPointerTable(
			_pointerTableRom.AsSpan(0x2000, 0x2000), 0x2000,
			PansyLoader.PLATFORM_NES, null, out _);
	}

	[Benchmark(Description = "DetectTileData (1KB NES)")]
	public bool DetectTileData() {
		// 64 NES tiles = 1024 bytes
		var data = new byte[1024];
		var rng = new Random(42);
		rng.NextBytes(data);
		return PansyAnalyzer.TryDetectTileData(data, 0, PansyLoader.PLATFORM_NES, out _);
	}

	[Benchmark(Description = "GenerateAnnotations (32KB)")]
	public byte[] GenerateAnnotations() {
		var analysis = PansyAnalyzer.Analyze(_sparseLoader, _sparseRom, detectPatterns: true);
		return PansyAnalyzer.GenerateAnnotations(_sparseLoader, analysis);
	}

	[Benchmark(Description = "ReadAddress NES x10000")]
	public uint ReadAddressBench() {
		uint sum = 0;
		ReadOnlySpan<byte> data = [0x00, 0x80];
		for (int i = 0; i < 10000; i++) {
			sum += PansyAnalyzer.ReadAddress(data, PansyLoader.PLATFORM_NES);
		}
		return sum;
	}

	[Benchmark(Description = "IsValidAddress NES x10000")]
	public int ValidateAddresses() {
		int count = 0;
		for (uint i = 0; i < 10000; i++) {
			if (PansyAnalyzer.IsValidAddress(0x8000 + i, PansyLoader.PLATFORM_NES, null))
				count++;
		}
		return count;
	}
}

/// <summary>
/// Benchmarks large CROSS_REFS ingestion/analysis scenarios (100k+ edges) with mixed CDM density.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class LargeCrossRefBenchmarks {
	private const int RomSize = 0x20_0000;
	private const int DenseXrefCount = 120_000;
	private const int VeryDenseXrefCount = 220_000;

	private byte[] _denseUncompressed = null!;
	private byte[] _denseCompressed = null!;
	private byte[] _veryDenseUncompressed = null!;
	private byte[] _rom = null!;

	private PansyLoader _denseLoader = null!;
	private PansyLoader _veryDenseLoader = null!;

	[GlobalSetup]
	public void Setup() {
		_denseUncompressed = GenerateLargeXrefFile(DenseXrefCount, cdmStride: 16, compressed: false);
		_denseCompressed = GenerateLargeXrefFile(DenseXrefCount, cdmStride: 16, compressed: true);
		_veryDenseUncompressed = GenerateLargeXrefFile(VeryDenseXrefCount, cdmStride: 4, compressed: false);

		_denseLoader = new PansyLoader(_denseUncompressed);
		_veryDenseLoader = new PansyLoader(_veryDenseUncompressed);
		_rom = new byte[RomSize];
	}

	private static byte[] GenerateLargeXrefFile(int xrefCount, int cdmStride, bool compressed) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = RomSize,
			EnableCompression = compressed,
			ProjectName = "LargeXrefBench",
			Author = "Benchmark",
			ProjectVersion = "1.0",
		};

		for (uint i = 0; i < (uint)xrefCount; i++) {
			uint from = (i * 3) % 0x1f_0000;
			uint to = ((i * 7) + 0x2000) % 0x1f_0000;
			writer.AddCrossReference(new CrossReference(
				from,
				to,
				(CrossRefType)((i % 5) + 1)));

			if ((i % cdmStride) == 0) {
				writer.MarkAsCode(to);
				writer.MarkAsJumpTarget(to);
			}
			if ((i % (cdmStride * 2)) == 0) {
				writer.MarkAsSubroutine(to);
			}
		}

		return writer.Generate();
	}

	[Benchmark(Description = "Load 120k xrefs (uncompressed)")]
	public PansyLoader LoadDenseUncompressed() => new(_denseUncompressed);

	[Benchmark(Description = "Load 120k xrefs (compressed)")]
	public PansyLoader LoadDenseCompressed() => new(_denseCompressed);

	[Benchmark(Description = "Analyze 120k xrefs (mixed CDM)")]
	public AnalysisResult AnalyzeDenseGraph() {
		return PansyAnalyzer.Analyze(_denseLoader, _rom, detectPatterns: false);
	}

	[Benchmark(Description = "ValidateJumpGraph 120k xrefs")]
	public JumpGraphValidationResult ValidateDenseGraph() {
		return PansyAnalyzer.ValidateJumpGraph(_denseLoader, RomSize);
	}

	[Benchmark(Description = "Analyze 220k xrefs (high density)")]
	public AnalysisResult AnalyzeVeryDenseGraph() {
		return PansyAnalyzer.Analyze(_veryDenseLoader, _rom, detectPatterns: false);
	}
}

/// <summary>
/// Benchmarks for PansyMerger — merging two Pansy files with parallel operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class MergerBenchmarks {
	private PansyLoader _baseSmall = null!;
	private PansyLoader _overlaySmall = null!;
	private PansyLoader _baseLarge = null!;
	private PansyLoader _overlayLarge = null!;

	[GlobalSetup]
	public void Setup() {
		_baseSmall = new PansyLoader(GenerateMergeFile(100, "base"));
		_overlaySmall = new PansyLoader(GenerateMergeFile(100, "overlay", addressOffset: 50));
		_baseLarge = new PansyLoader(GenerateMergeFile(5000, "base"));
		_overlayLarge = new PansyLoader(GenerateMergeFile(5000, "overlay", addressOffset: 2500));
	}

	private static byte[] GenerateMergeFile(int count, string prefix, uint addressOffset = 0) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000,
			RomCrc32 = 0xdeadbeef,
			ProjectName = $"{prefix} project",
		};
		for (uint i = 0; i < (uint)count; i++) {
			writer.AddSymbol(0x8000 + addressOffset + i, $"{prefix}_{i:x4}");
			writer.AddComment(0x8000 + addressOffset + i, $"{prefix} comment {i}");
			writer.MarkAsCode(addressOffset + i);
			if (i % 5 == 0) writer.MarkAsOpcode(addressOffset + i);
			writer.AddCrossReference(new CrossReference(
				0x8000 + addressOffset + i, 0x9000 + addressOffset + i, CrossRefType.Jsr));
		}
		writer.AddMemoryRegion(new MemoryRegion(
			0x8000 + addressOffset, 0x8000 + addressOffset + (uint)count, 1, 0, $"{prefix}_ROM"));
		return writer.Generate();
	}

	[Benchmark(Description = "Merge small (100 entries)")]
	public byte[] MergeSmall() {
		return PansyMerger.Merge(_baseSmall, _overlaySmall).Generate();
	}

	[Benchmark(Description = "Merge large (5000 entries)")]
	public byte[] MergeLarge() {
		return PansyMerger.Merge(_baseLarge, _overlayLarge).Generate();
	}

	[Benchmark(Description = "Merge small no-overlap")]
	public byte[] MergeSmallNoOverlap() {
		var overlay = new PansyLoader(GenerateMergeFile(100, "other", addressOffset: 200));
		return PansyMerger.Merge(_baseSmall, overlay).Generate();
	}

	[Benchmark(Description = "Merge large full-overlap")]
	public byte[] MergeLargeFullOverlap() {
		return PansyMerger.Merge(_baseLarge, _baseLarge).Generate();
	}
}

/// <summary>
/// Benchmarks for batch API vs individual insertion performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class BatchApiBenchmarks {
	[Params(100, 1000, 5000)]
	public int Count { get; set; }

	[Benchmark(Baseline = true, Description = "AddSymbol individual")]
	public byte[] AddSymbolIndividual() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddSymbol(0x8000 + i, $"Sym_{i:x4}", SymbolType.Label);
		}
		return writer.Generate();
	}

	[Benchmark(Description = "AddSymbols batch")]
	public byte[] AddSymbolsBatch() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		var symbols = new (uint Address, string Name, SymbolType Type)[Count];
		for (int i = 0; i < Count; i++) {
			symbols[i] = (0x8000 + (uint)i, $"Sym_{i:x4}", SymbolType.Label);
		}
		writer.AddSymbols(symbols);
		return writer.Generate();
	}

	[Benchmark(Description = "AddComment individual")]
	public byte[] AddCommentIndividual() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddComment(0x8000 + i, $"Comment {i}", CommentType.Inline);
		}
		return writer.Generate();
	}

	[Benchmark(Description = "AddComments batch")]
	public byte[] AddCommentsBatch() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		var comments = new (uint Address, string Text, CommentType Type)[Count];
		for (int i = 0; i < Count; i++) {
			comments[i] = (0x8000 + (uint)i, $"Comment {i}", CommentType.Inline);
		}
		writer.AddComments(comments);
		return writer.Generate();
	}

	[Benchmark(Description = "AddCrossReference individual")]
	public byte[] AddCrossRefIndividual() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddCrossReference(new CrossReference(0x8000 + i, 0x9000 + i, CrossRefType.Jsr));
		}
		return writer.Generate();
	}

	[Benchmark(Description = "AddCrossReferences batch")]
	public byte[] AddCrossRefsBatch() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		var xrefs = new CrossReference[Count];
		for (int i = 0; i < Count; i++) {
			xrefs[i] = new CrossReference(0x8000 + (uint)i, 0x9000 + (uint)i, CrossRefType.Jsr);
		}
		writer.AddCrossReferences(xrefs);
		return writer.Generate();
	}
}

/// <summary>
/// Benchmarks for cross-reference query operations at various scales.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CrossRefQueryBenchmarks {
	private PansyLoader _smallLoader = null!;
	private PansyLoader _largeLoader = null!;

	[GlobalSetup]
	public void Setup() {
		_smallLoader = new PansyLoader(GenerateXrefFile(200));
		_largeLoader = new PansyLoader(GenerateXrefFile(5000));
	}

	private static byte[] GenerateXrefFile(int count) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)count; i++) {
			writer.AddCrossReference(new CrossReference(
				0x8000 + i * 3,
				0x8000 + (i % 100) * 4,
				(CrossRefType)(i % 5 + 1)));
			writer.MarkAsSubroutine(0x8000 + (i % 100) * 4);
		}
		for (uint i = 0; i < (uint)count; i++) {
			writer.AddSymbol(0x8000 + i * 3, $"Src_{i:x4}");
		}
		return writer.Generate();
	}

	[Benchmark(Description = "GetCrossRefsTo (200 xrefs)")]
	public int QueryTo_Small() {
		var count = 0;
		for (uint i = 0; i < 100; i++) {
			count += _smallLoader.GetCrossRefsTo((int)(0x8000 + i * 4)).Count;
		}
		return count;
	}

	[Benchmark(Description = "GetCrossRefsTo (5000 xrefs)")]
	public int QueryTo_Large() {
		var count = 0;
		for (uint i = 0; i < 100; i++) {
			count += _largeLoader.GetCrossRefsTo((int)(0x8000 + i * 4)).Count;
		}
		return count;
	}

	[Benchmark(Description = "GetCrossRefsFrom (200 xrefs)")]
	public int QueryFrom_Small() {
		var count = 0;
		for (uint i = 0; i < 100; i++) {
			count += _smallLoader.GetCrossRefsFrom((int)(0x8000 + i * 3)).Count;
		}
		return count;
	}

	[Benchmark(Description = "GetCrossRefsByType (5000 xrefs)")]
	public int QueryByType() {
		var count = 0;
		count += _largeLoader.GetCrossRefsByType(CrossRefType.Jsr).Count();
		count += _largeLoader.GetCrossRefsByType(CrossRefType.Jmp).Count();
		count += _largeLoader.GetCrossRefsByType(CrossRefType.Branch).Count();
		return count;
	}

	[Benchmark(Description = "GetCrossRefsFromRange (5000 xrefs)")]
	public int QueryFromRange() {
		return _largeLoader.GetCrossRefsFromRange(0x8000, 0x9000).Count();
	}

	[Benchmark(Description = "GetCrossRefsToRange (5000 xrefs)")]
	public int QueryToRange() {
		return _largeLoader.GetCrossRefsToRange(0x8000, 0x8200).Count();
	}

	[Benchmark(Description = "GetReferenceCount x100 (5000 xrefs)")]
	public int ReferenceCount() {
		var count = 0;
		for (uint i = 0; i < 100; i++) {
			count += _largeLoader.GetReferenceCount((int)(0x8000 + i * 4));
		}
		return count;
	}

	[Benchmark(Description = "GetUnreferencedSubroutines (5000 xrefs)")]
	public int UnreferencedSubs() {
		return _largeLoader.GetUnreferencedSubroutines().Count();
	}

	[Benchmark(Description = "GetMostReferenced top 10 (5000 xrefs)")]
	public int MostReferenced() {
		return _largeLoader.GetMostReferencedAddresses(10).Count();
	}

	[Benchmark(Description = "GetCrossRefStats (5000 xrefs)")]
	public int XrefStats() {
		var s = _largeLoader.GetCrossRefStats();
		return s.TotalXrefs;
	}
}

/// <summary>
/// Benchmarks for graph export operations (DOT, GraphML, JSON).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class GraphExportBenchmarks {
	private PansyLoader _smallLoader = null!;
	private PansyLoader _largeLoader = null!;

	[GlobalSetup]
	public void Setup() {
		_smallLoader = new PansyLoader(GenerateGraphFile(50));
		_largeLoader = new PansyLoader(GenerateGraphFile(500));
	}

	private static byte[] GenerateGraphFile(int count) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)count; i++) {
			writer.AddSymbol(0x8000 + i * 4, $"Sub_{i:x4}", SymbolType.Function);
			writer.AddCrossReference(new CrossReference(
				0x8000 + i * 4,
				0x8000 + ((i + 1) % (uint)count) * 4,
				(CrossRefType)(i % 5 + 1)));
		}
		return writer.Generate();
	}

	[Benchmark(Description = "ToDot small (50 nodes)")]
	public string ToDotSmall() => PansyGraphExporter.ToDot(_smallLoader);

	[Benchmark(Description = "ToDot large (500 nodes)")]
	public string ToDotLarge() => PansyGraphExporter.ToDot(_largeLoader);

	[Benchmark(Description = "ToGraphML small (50 nodes)")]
	public string ToGraphMLSmall() => PansyGraphExporter.ToGraphML(_smallLoader);

	[Benchmark(Description = "ToGraphML large (500 nodes)")]
	public string ToGraphMLLarge() => PansyGraphExporter.ToGraphML(_largeLoader);

	[Benchmark(Description = "ToJson small (50 nodes)")]
	public string ToJsonSmall() => PansyGraphExporter.ToJson(_smallLoader);

	[Benchmark(Description = "ToJson large (500 nodes)")]
	public string ToJsonLarge() => PansyGraphExporter.ToJson(_largeLoader);

	[Benchmark(Description = "ToDot filtered Jsr (500 nodes)")]
	public string ToDotFiltered() => PansyGraphExporter.ToDot(_largeLoader, CrossRefType.Jsr);

	[Benchmark(Description = "ToJson filtered Jmp (500 nodes)")]
	public string ToJsonFiltered() => PansyGraphExporter.ToJson(_largeLoader, CrossRefType.Jmp);
}

/// <summary>
/// Benchmarks for bookmark and data type section performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class BookmarkDataTypeBenchmarks {
	[Params(100, 1000)]
	public int Count { get; set; }

	[Benchmark(Description = "Write bookmarks")]
	public byte[] WriteBookmarks() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddBookmark(new Bookmark(0x8000 + i, $"BM_{i:x4}", (byte)(i % 8)));
		}
		return writer.Generate();
	}

	[Benchmark(Description = "Write data types")]
	public byte[] WriteDataTypes() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddDataType(new DataTypeEntry(
				0x8000 + i * 0x10,
				16, 2, 8,
				(DataElementType)(i % 4 + 1),
				$"Type_{i:x4}"));
		}
		return writer.Generate();
	}

	[Benchmark(Description = "Load bookmarks")]
	public int LoadBookmarks() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddBookmark(new Bookmark(0x8000 + i, $"BM_{i:x4}", (byte)(i % 8)));
		}
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		return loader.Bookmarks.Count;
	}

	[Benchmark(Description = "Load data types")]
	public int LoadDataTypes() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddDataType(new DataTypeEntry(
				0x8000 + i * 0x10,
				16, 2, 8,
				(DataElementType)(i % 4 + 1),
				$"Type_{i:x4}"));
		}
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		return loader.DataTypes.Count;
	}

	[Benchmark(Description = "Write source map")]
	public byte[] WriteSourceMap() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x80000
		};
		var fileIdx = writer.AddSourceFile("main.pasm");
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddSourceMapping(new SourceMapEntry(0x8000 + i, fileIdx, (ushort)(i + 1), 0));
		}
		return writer.Generate();
	}
}

/// <summary>
/// Benchmarks for CPU state section (0x0009) performance — canonical M/X tracking for SNES and ARM/THUMB for GBA.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CpuStateBenchmarks {
	[Params(100, 1000, 5000)]
	public int Count { get; set; }

	[Benchmark(Description = "Write CPU state SNES")]
	public byte[] WriteCpuStateSnes() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			// Alternate M and X flags every 16 bytes
			var flags = (byte)(((i / 16) % 3) switch {
				0 => 0x00, // 16-bit A, 16-bit X
				1 => 0x03, // 8-bit A, 8-bit X
				_ => 0x01  // 16-bit A, 8-bit X
			});
			writer.AddCpuState(new CpuStateEntry(0x8000 + i, flags, 0x00, 0x0000, CpuMode.Native65816));
		}
		return writer.Generate();
	}

	[Benchmark(Description = "Write CPU state GBA")]
	public byte[] WriteCpuStateGba() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_GBA,
			RomSize = 0x2000000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			// Alternate ARM and THUMB mode every 4 bytes
			var mode = ((i / 4) % 2 == 0) ? CpuMode.ARM : CpuMode.THUMB;
			writer.AddCpuState(new CpuStateEntry(i, 0x00, 0x00, 0x0000, mode));
		}
		return writer.Generate();
	}

	[Benchmark(Description = "Write CPU state with other data")]
	public byte[] WriteMixedCpuState() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000
		};
		// Interleave CPU-state with symbols, comments, code/data map
		for (uint i = 0; i < (uint)Count; i++) {
			if (i % 2 == 0) {
				writer.AddSymbol(0x8000 + i, $"Sym_{i:x4}");
			}
			if (i % 3 == 0) {
				writer.AddComment(0x8000 + i, $"Comment {i}");
			}
			if (i % 5 == 0) {
				writer.MarkAsCode(i);
			}
			if (i % 10 == 0) {
				writer.AddCpuState(new CpuStateEntry(0x8000 + i, (byte)(i % 4), 0x00, 0x0000, CpuMode.Native65816));
			}
		}
		return writer.Generate();
	}

	[Benchmark(Description = "Load CPU state SNES")]
	public PansyLoader LoadCpuStateSnes() {
		var data = WriteCpuStateSnes();
		return new PansyLoader(data);
	}

	[Benchmark(Description = "Load CPU state GBA")]
	public PansyLoader LoadCpuStateGba() {
		var data = WriteCpuStateGba();
		return new PansyLoader(data);
	}

	[Benchmark(Description = "Query CPU state x1000")]
	public int QueryCpuState() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < 1000; i++) {
			writer.AddCpuState(new CpuStateEntry(0x8000 + i, (byte)(i % 4), 0x00, 0x0000, CpuMode.Native65816));
		}
		var loader = new PansyLoader(writer.Generate());

		// Query random CPU states
		int count = 0;
		for (int i = 0; i < 1000; i++) {
			var addr = 0x8000 + (i % 1000);
			// Simulate accessing CPU-state for decode-width decisions
			_ = loader.CpuStateEntries.FirstOrDefault(cs => cs.Address == addr);
			count++;
		}
		return count;
	}

	[Benchmark(Description = "Roundtrip CPU state (write + load)")]
	public int RoundtripCpuState() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddCpuState(new CpuStateEntry(0x8000 + i, (byte)(i % 4), 0x00, 0x0000, CpuMode.Native65816));
		}
		var data = writer.Generate();
		var loader = new PansyLoader(data);
		return loader.CpuStateEntries.Count;
	}

	[Benchmark(Description = "CPU state + compression")]
	public byte[] CpuStateCompressed() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000,
			EnableCompression = true
		};
		for (uint i = 0; i < (uint)Count; i++) {
			writer.AddCpuState(new CpuStateEntry(0x8000 + i, (byte)(i % 4), 0x00, 0x0000, CpuMode.Native65816));
			writer.AddSymbol(0x8000 + i, $"S_{i:x4}");
		}
		return writer.Generate();
	}
}
