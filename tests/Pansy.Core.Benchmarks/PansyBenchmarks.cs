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
