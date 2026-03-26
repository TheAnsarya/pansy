using Pansy.Core;
using System.Text;
using Xunit;

namespace Pansy.Core.Tests;

public class PansyLoaderTests {
	[Fact]
	public void Load_InvalidMagic_ThrowsException() {
		var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

		Assert.Throws<System.IO.InvalidDataException>(() => new PansyLoader(data));
	}

	[Fact]
	public void Load_TooShort_ThrowsException() {
		var data = new byte[] { (byte)'P', (byte)'A', (byte)'N', (byte)'S', (byte)'Y' };

		Assert.Throws<System.IO.InvalidDataException>(() => new PansyLoader(data));
	}

	[Fact]
	public void PlatformConstants_Defined() {
		// Verify platform constants are accessible
		Assert.Equal(0x01, PansyLoader.PLATFORM_NES);
		Assert.Equal(0x02, PansyLoader.PLATFORM_SNES);
		Assert.Equal(0x03, PansyLoader.PLATFORM_GB);
		Assert.Equal(0x04, PansyLoader.PLATFORM_GBA);
		Assert.Equal(0x05, PansyLoader.PLATFORM_GENESIS);
		Assert.Equal(0x1f, PansyLoader.PLATFORM_CHANNEL_F);
		Assert.Equal(0xff, PansyLoader.PLATFORM_CUSTOM);
	}

	[Fact]
	public void Load_ChannelFPlatform_RoundtripsPlatformId() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_CHANNEL_F,
			RomSize = 0x2000
		};

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(PansyLoader.PLATFORM_CHANNEL_F, loader.Platform);
		Assert.Equal("Fairchild Channel F", PansyLoader.GetPlatformName(loader.Platform));
	}

	[Fact]
	public void Load_ChannelFPlatform_RoundtripsCdlFlags() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_CHANNEL_F,
			RomSize = 0x2000
		};

		// Mark Channel F ROM addresses with various CDL flags
		writer.MarkAsCode(0x0000);
		writer.MarkAsOpcode(0x0000);
		writer.MarkAsSubroutine(0x0000);
		writer.MarkAsCode(0x0001);
		writer.MarkAsOpcode(0x0001);
		writer.MarkAsData(0x0100);
		writer.MarkAsRead(0x0100);
		writer.MarkAsJumpTarget(0x0200);
		writer.MarkAsCode(0x0200);
		writer.MarkAsIndirect(0x0300);
		writer.MarkAsDrawn(0x0400);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.True(loader.HasCodeDataMap);
		Assert.Contains(0x0000, loader.CodeOffsets);
		Assert.Contains(0x0000, loader.OpcodeOffsets);
		Assert.Contains(0x0000, loader.SubEntryPoints);
		Assert.Contains(0x0001, loader.CodeOffsets);
		Assert.Contains(0x0100, loader.DataOffsets);
		Assert.Contains(0x0100, loader.ReadOffsets);
		Assert.Contains(0x0200, loader.JumpTargets);
		Assert.Contains(0x0300, loader.IndirectOffsets);
		Assert.Contains(0x0400, loader.DrawnOffsets);
	}

	[Fact]
	public void Load_ChannelFPlatform_RoundtripsSymbolsAndComments() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_CHANNEL_F,
			RomSize = 0x2000
		};

		// Add typed symbols at Channel F addresses
		writer.AddSymbol(0x0000, "RESET", SymbolType.Function);
		writer.AddSymbol(0x3800, "CH_F_PORT0", SymbolType.Constant);
		writer.AddSymbol(0x3fff, "RESET_VECTOR", SymbolType.InterruptVector);

		// Add typed comments
		writer.AddComment(0x0000, "Entry point after reset", CommentType.Block);
		writer.AddComment(0x3800, "Console buttons and flags", CommentType.Inline);
		writer.AddComment(0x0100, "TODO: verify timing", CommentType.Todo);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Verify symbols
		Assert.Equal(3, loader.Symbols.Count);
		Assert.Equal("RESET", loader.Symbols[0x0000]);
		Assert.Equal("CH_F_PORT0", loader.Symbols[0x3800]);
		Assert.Equal("RESET_VECTOR", loader.Symbols[0x3fff]);
		Assert.Equal(SymbolType.Function, loader.SymbolEntries[0x0000].Type);
		Assert.Equal(SymbolType.Constant, loader.SymbolEntries[0x3800].Type);
		Assert.Equal(SymbolType.InterruptVector, loader.SymbolEntries[0x3fff].Type);

		// Verify comments
		Assert.Equal(3, loader.Comments.Count);
		Assert.Equal("Entry point after reset", loader.Comments[0x0000]);
		Assert.Equal(CommentType.Block, loader.CommentEntries[0x0000].Type);
		Assert.Equal(CommentType.Inline, loader.CommentEntries[0x3800].Type);
		Assert.Equal(CommentType.Todo, loader.CommentEntries[0x0100].Type);
	}

	[Fact]
	public void Load_ChannelFPlatform_RoundtripsCrossRefsAndRegions() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_CHANNEL_F,
			RomSize = 0x2000
		};

		// Add cross-references typical of Channel F code
		writer.AddCrossReference(new CrossReference(0x0010, 0x0200, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x0020, 0x0300, CrossRefType.Jmp));
		writer.AddCrossReference(new CrossReference(0x0030, 0x0040, CrossRefType.Branch));
		writer.AddCrossReference(new CrossReference(0x0050, 0x3800, CrossRefType.Read));
		writer.AddCrossReference(new CrossReference(0x0060, 0x2800, CrossRefType.Write));

		// Add Channel F memory regions
		writer.AddMemoryRegion(new MemoryRegion(0x0000, 0x17ff, (byte)MemoryRegionType.ROM, 0, "Cartridge ROM"));
		writer.AddMemoryRegion(new MemoryRegion(0x2800, 0x2fff, (byte)MemoryRegionType.RAM, 0, "System RAM"));
		writer.AddMemoryRegion(new MemoryRegion(0x3000, 0x37ff, (byte)MemoryRegionType.VRAM, 0, "Video RAM"));
		writer.AddMemoryRegion(new MemoryRegion(0x3800, 0x38ff, (byte)MemoryRegionType.IO, 0, "I/O Registers"));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Verify cross-references
		Assert.Equal(5, loader.CrossReferences.Count);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x0010 && x.To == 0x0200 && x.Type == CrossRefType.Jsr);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x0020 && x.To == 0x0300 && x.Type == CrossRefType.Jmp);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x0030 && x.To == 0x0040 && x.Type == CrossRefType.Branch);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x0050 && x.To == 0x3800 && x.Type == CrossRefType.Read);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x0060 && x.To == 0x2800 && x.Type == CrossRefType.Write);

		// Verify memory regions
		Assert.Equal(4, loader.MemoryRegions.Count);
		Assert.Contains(loader.MemoryRegions, r => r.Name == "Cartridge ROM" && r.Start == 0x0000 && r.End == 0x17ff);
		Assert.Contains(loader.MemoryRegions, r => r.Name == "System RAM" && r.Start == 0x2800 && r.End == 0x2fff);
		Assert.Contains(loader.MemoryRegions, r => r.Name == "Video RAM" && r.Start == 0x3000 && r.End == 0x37ff);
		Assert.Contains(loader.MemoryRegions, r => r.Name == "I/O Registers" && r.Start == 0x3800 && r.End == 0x38ff);
	}

	[Fact]
	public void Load_ChannelFPlatform_FullFileRoundtrip() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_CHANNEL_F,
			RomSize = 0x1800,
			RomCrc32 = 0xdeadbeef,
			ProjectName = "Channel F Demo",
			Author = "Test",
			ProjectVersion = "1.0.0"
		};

		// Add a representative mix of all sections
		writer.MarkAsCode(0x0000);
		writer.MarkAsOpcode(0x0000);
		writer.MarkAsSubroutine(0x0000);
		writer.MarkAsData(0x1000);
		writer.AddSymbol(0x0000, "start", SymbolType.Function);
		writer.AddSymbol(0x3fff, "RESET_VECTOR", SymbolType.InterruptVector);
		writer.AddComment(0x0000, "Program entry", CommentType.Block);
		writer.AddCrossReference(new CrossReference(0x0010, 0x0200, CrossRefType.Jsr));
		writer.AddMemoryRegion(new MemoryRegion(0x0000, 0x17ff, (byte)MemoryRegionType.ROM, 0, "Cartridge ROM"));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Verify header fields
		Assert.Equal(PansyLoader.PLATFORM_CHANNEL_F, loader.Platform);
		Assert.Equal(0x1800u, loader.RomSize);
		Assert.Equal(0xdeadbeefu, loader.RomCrc32);
		Assert.Equal("Channel F Demo", loader.ProjectName);
		Assert.Equal("Test", loader.Author);
		Assert.Equal("1.0.0", loader.ProjectVersion);

		// Verify all sections present
		Assert.True(loader.HasCodeDataMap);
		Assert.NotEmpty(loader.Symbols);
		Assert.NotEmpty(loader.Comments);
		Assert.NotEmpty(loader.CrossReferences);
		Assert.NotEmpty(loader.MemoryRegions);
	}
}
