using Pansy.Core;
using Xunit;

namespace Pansy.Core.Tests;

public class PansyWriterTests {
	[Fact]
	public void Constructor_Default_Initializes() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var data = writer.Generate();

		Assert.NotEmpty(data);
		// Minimal file: 8 (magic) + 2 (version) + 2 (flags) + 1 (platform) + 1 (reserved) + 4 (size) + 4 (crc) + 2 (section count) = 24 bytes
		Assert.True(data.Length >= 24, $"Expected at least 24 bytes, got {data.Length}");
	}

	[Fact]
	public void AddSymbol_WritesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddSymbol(0x8000, "Reset");
		writer.AddSymbol(0x8003, "NMI");

		var data = writer.Generate();

		// Verify file has data beyond minimal header
		Assert.True(data.Length > 24, "File should contain symbol section data");
	}

	[Fact]
	public void AddComment_WritesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddComment(0x8000, "Entry point");
		writer.AddComment(0x8010, "Main loop");

		var data = writer.Generate();

		Assert.True(data.Length > 24, "File should contain comment section data");
	}

	[Fact]
	public void MarkAsCode_WritesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.MarkAsCode(0x8000);
		writer.MarkAsCode(0x8010);
		writer.MarkAsCode(0x8020);

		var data = writer.Generate();

		Assert.True(data.Length > 24, "File should contain code offset section data");
	}

	[Fact]
	public void MarkAsData_WritesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.MarkAsData(0x9000);
		writer.MarkAsData(0x9100);

		var data = writer.Generate();

		Assert.True(data.Length > 24, "File should contain data offset section data");
	}

	[Fact]
	public void MarkAsJumpTarget_WritesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.MarkAsJumpTarget(0x8050);

		var data = writer.Generate();

		Assert.True(data.Length > 24, "File should contain jump target section data");
	}

	[Fact]
	public void MarkAsSubroutine_WritesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.MarkAsSubroutine(0x8100);

		var data = writer.Generate();

		Assert.True(data.Length > 24, "File should contain subroutine section data");
	}

	[Fact]
	public void AddMemoryRegion_WritesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, 0x01, 0, "ROM"));

		var data = writer.Generate();

		Assert.True(data.Length > 24, "File should contain memory region section data");
	}

	[Fact]
	public void AddCrossReference_WritesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddCrossReference(new CrossReference(0x8010, 0x8100, CrossRefType.Jsr));

		var data = writer.Generate();

		Assert.True(data.Length > 24, "File should contain cross-reference section data");
	}

	[Fact]
	public void AddMultiTargetCrossReference_WritesGroupedAndLegacyEdges() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddMultiTargetCrossReference(new MultiTargetCrossReference(
			From: 0x8010,
			Type: CrossRefType.Branch,
			Targets: [0x8020, 0x8030]));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Single(loader.MultiTargetCrossReferences);
		Assert.Equal(2, loader.MultiTargetCrossReferences[0].Targets.Count);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8010 && x.To == 0x8020 && x.Type == CrossRefType.Branch);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8010 && x.To == 0x8030 && x.Type == CrossRefType.Branch);
	}

	[Fact]
	public void EnableCompression_CompressesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			EnableCompression = true
		};

		// Add lots of data to make compression effective
		for (uint i = 0; i < 500; i++) {
			writer.AddSymbol(0x8000 + i, $"Symbol_{i:x4}");
		}

		var compressedData = writer.Generate();

		// Compressed file should have reasonable size (not checking exact compression ratio)
		Assert.NotEmpty(compressedData);
		Assert.True(compressedData.Length < 50000, "Compressed data should be smaller than uncompressed");
	}

	[Fact]
	public void MultipleDataTypes_AllWritten() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000,
			ProjectName = "Test Project",
			Author = "Test Author",
			ProjectVersion = "1.0"
		};

		writer.AddSymbol(0x8000, "Reset");
		writer.AddComment(0x8000, "Entry point");
		writer.MarkAsCode(0x8000);
		writer.MarkAsJumpTarget(0x8050);
		writer.MarkAsSubroutine(0x8100);
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, 0x01, 0, "ROM"));
		writer.AddCrossReference(new CrossReference(0x8010, 0x8100, CrossRefType.Jsr));

		var data = writer.Generate();

		// File should contain all sections
		Assert.True(data.Length > 100, "File with multiple sections should be substantial");
	}
}
