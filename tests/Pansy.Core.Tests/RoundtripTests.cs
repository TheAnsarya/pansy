using Xunit;

namespace Pansy.Core.Tests;

public class RoundtripTests {
	[Fact]
	public void RoundTrip_BasicFile_PreservesAllData() {
		// Arrange
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000,
			RomCrc32 = 0x12345678,
			ProjectName = "Test Project",
			Author = "Test Author",
			ProjectVersion = "1.0.0"
		};

		writer.AddSymbol(0x8000, "Main");
		writer.AddSymbol(0x8010, "Loop");
		writer.AddSymbol(0x8020, "Exit");
		writer.AddComment(0x8000, "Program entry point");
		writer.AddComment(0x8010, "Main game loop");
		writer.MarkAsCode(0x8000);
		writer.MarkAsCode(0x8001);
		writer.MarkAsCode(0x8002);
		writer.MarkAsData(0x9000);
		writer.MarkAsData(0x9001);
		writer.MarkAsJumpTarget(0x8010);
		writer.MarkAsSubroutine(0x8020);
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xFFFF, 1, 0, "PRG-ROM"));
		writer.AddCrossReference(new CrossReference(0x8005, 0x8010, CrossRefType.Jmp));

		// Act
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Assert - Header
		Assert.Equal(PansyLoader.PLATFORM_NES, loader.Platform);
		Assert.Equal(0x20000u, loader.RomSize);
		Assert.Equal(0x12345678u, loader.RomCrc32);
		Assert.Equal("Test Project", loader.ProjectName);
		Assert.Equal("Test Author", loader.Author);
		Assert.Equal("1.0.0", loader.ProjectVersion);

		// Assert - Symbols
		Assert.Equal(3, loader.Symbols.Count);
		Assert.Equal("Main", loader.GetSymbol(0x8000));
		Assert.Equal("Loop", loader.GetSymbol(0x8010));
		Assert.Equal("Exit", loader.GetSymbol(0x8020));

		// Assert - Comments
		Assert.Equal(2, loader.Comments.Count);
		Assert.Equal("Program entry point", loader.GetComment(0x8000));
		Assert.Equal("Main game loop", loader.GetComment(0x8010));

		// Assert - Code/Data flags
		Assert.True(loader.IsCode(0x8000));
		Assert.True(loader.IsCode(0x8001));
		Assert.True(loader.IsCode(0x8002));
		Assert.True(loader.IsData(0x9000));
		Assert.True(loader.IsData(0x9001));
		Assert.True(loader.IsJumpTarget(0x8010));
		Assert.True(loader.IsSubEntryPoint(0x8020));

		// Assert - Memory regions
		Assert.Single(loader.MemoryRegions);
		var region = loader.MemoryRegions[0];
		Assert.Equal(0x8000u, region.Start);
		Assert.Equal(0xFFFFu, region.End);
		Assert.Equal((byte)1, region.Type);
		Assert.Equal((byte)0, region.Bank);
		Assert.Equal("PRG-ROM", region.Name);

		// Assert - Cross-references
		Assert.Single(loader.CrossReferences);
		var xref = loader.CrossReferences[0];
		Assert.Equal(0x8005u, xref.From);
		Assert.Equal(0x8010u, xref.To);
		Assert.Equal(CrossRefType.Jmp, xref.Type);
	}

	[Fact]
	public void RoundTrip_EmptyFile_Works() {
		// Arrange
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000,
			RomCrc32 = 0xABCDEF00
		};

		// Act
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Assert
		Assert.Equal(PansyLoader.PLATFORM_SNES, loader.Platform);
		Assert.Equal(0x80000u, loader.RomSize);
		Assert.Equal(0xABCDEF00u, loader.RomCrc32);
		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
		Assert.Empty(loader.MemoryRegions);
		Assert.Empty(loader.CrossReferences);
	}

	[Fact]
	public void RoundTrip_LargeFile_PreservesData() {
		// Arrange
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_GB,
			RomSize = 0x100000,
			RomCrc32 = 0x11223344
		};

		// Add 1000 symbols
		for (uint i = 0; i < 1000; i++) {
			writer.AddSymbol(0x4000 + i * 4, $"Symbol_{i:D4}");
		}

		// Add 500 comments
		for (uint i = 0; i < 500; i++) {
			writer.AddComment(0x8000 + i * 8, $"Comment #{i}");
		}

		// Mark 10000 addresses as code
		for (uint i = 0; i < 10000; i++) {
			writer.MarkAsCode(i);
		}

		// Add 100 cross-references
		for (uint i = 0; i < 100; i++) {
			writer.AddCrossReference(new CrossReference(0x4000 + i, 0x8000 + i, CrossRefType.Jsr));
		}

		// Act
		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Assert
		Assert.Equal(1000, loader.Symbols.Count);
		Assert.Equal(500, loader.Comments.Count);
		Assert.Equal(10000, loader.CodeOffsets.Count);
		Assert.Equal(100, loader.CrossReferences.Count);

		// Spot-check some values
		Assert.Equal("Symbol_0000", loader.GetSymbol(0x4000));
		Assert.Equal("Symbol_0999", loader.GetSymbol(0x4000 + 999 * 4));
		Assert.Equal("Comment #0", loader.GetComment(0x8000));
		Assert.Equal("Comment #499", loader.GetComment(0x8000 + 499 * 8));
		Assert.True(loader.IsCode(0));
		Assert.True(loader.IsCode(9999));
	}
}
