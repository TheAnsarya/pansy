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
			writer.AddSymbol(0x8000 + i, $"Symbol_{i:X4}");
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
			writer.AddComment(0x8000 + i, $"Comment for address ${0x8000 + i:X4}");
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
			writer.AddSymbol(0x8000 + i, $"Sym_{i:X4}");
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
			writer.AddSymbol(0x8000 + i, $"Symbol_{i:X4}");
			writer.MarkAsCode(i);
		}
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

	[GlobalSetup]
	public void Setup() {
		_smallFile = GenerateFile(100);
		_mediumFile = GenerateFile(1000);
		_largeFile = GenerateFile(10000);
		_compressedFile = GenerateFile(10000, compressed: true);
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
			writer.AddSymbol(0x8000 + i, $"Sym_{i:X4}");
			writer.AddComment(0x8000 + i, $"Comment {i}");
			writer.MarkAsCode(i);
			if (i % 5 == 0) writer.MarkAsOpcode(i);
			if (i % 10 == 0) writer.MarkAsJumpTarget(i);
			if (i % 50 == 0) writer.MarkAsSubroutine(i);
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
}
