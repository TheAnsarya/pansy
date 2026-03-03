// ============================================================================
// BugFixTests.cs - Tests for critical bug fixes in Pansy.Core
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;
using Xunit;

namespace Pansy.Core.Tests;

/// <summary>
/// Tests for critical bug fixes and edge cases discovered during the
/// comprehensive Pansy project evaluation.
/// </summary>
public class BugFixTests {
	#region Symbol Type Support

	[Fact]
	public void AddSymbol_WithType_RoundtripsCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbol(0x8000, "Reset", SymbolType.Label);
		writer.AddSymbol(0x8010, "MAX_ENEMIES", SymbolType.Constant);
		writer.AddSymbol(0x8020, "Direction", SymbolType.Enum);
		writer.AddSymbol(0x8030, "PlayerData", SymbolType.Struct);
		writer.AddSymbol(0x8040, "LOAD_CHR", SymbolType.Macro);
		writer.AddSymbol(0x8050, ".loop", SymbolType.Local);
		writer.AddSymbol(0x8060, "+", SymbolType.Anonymous);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(7, loader.Symbols.Count);
		Assert.Equal("Reset", loader.GetSymbol(0x8000));
		Assert.Equal("MAX_ENEMIES", loader.GetSymbol(0x8010));
		Assert.Equal("Direction", loader.GetSymbol(0x8020));
		Assert.Equal("PlayerData", loader.GetSymbol(0x8030));
		Assert.Equal("LOAD_CHR", loader.GetSymbol(0x8040));
		Assert.Equal(".loop", loader.GetSymbol(0x8050));
		Assert.Equal("+", loader.GetSymbol(0x8060));
	}

	[Fact]
	public void AddSymbol_DefaultType_IsLabel() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbol(0x8000, "Reset");

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal("Reset", loader.GetSymbol(0x8000));
	}

	#endregion

	#region Opcode Marking

	[Fact]
	public void MarkAsOpcode_RoundtripsCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.MarkAsCode(0x8000);
		writer.MarkAsOpcode(0x8000);
		writer.MarkAsCode(0x8001); // operand byte, not opcode
		writer.MarkAsCode(0x8002);
		writer.MarkAsOpcode(0x8002);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.True(loader.IsOpcode(0x8000));
		Assert.True(loader.IsCode(0x8000));
		Assert.False(loader.IsOpcode(0x8001));
		Assert.True(loader.IsCode(0x8001));
		Assert.True(loader.IsOpcode(0x8002));
	}

	#endregion

	#region Comment Type Support

	[Fact]
	public void AddComment_WithType_RoundtripsCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddComment(0x8000, "Entry point"); // default inline
		writer.AddComment(0x8010, "Block comment here", 2); // block
		writer.AddComment(0x8020, "TODO: optimize this", 3); // todo

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(3, loader.Comments.Count);
		Assert.Equal("Entry point", loader.GetComment(0x8000));
		Assert.Equal("Block comment here", loader.GetComment(0x8010));
		Assert.Equal("TODO: optimize this", loader.GetComment(0x8020));
	}

	#endregion

	#region Address Preservation (32-bit, no truncation)

	[Fact]
	public void Symbol_FullAddress_NotTruncated() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_GBA,
			RomSize = 0x2000000
		};

		// GBA addresses can exceed 24 bits (bank byte in top position)
		writer.AddSymbol(0x08000000, "GBA_ROM_Start");
		writer.AddSymbol(0x02000000, "GBA_EWRAM");
		writer.AddSymbol(0x03000000, "GBA_IWRAM");

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Addresses must survive full 32-bit roundtrip
		Assert.Equal("GBA_ROM_Start", loader.GetSymbol(0x08000000));
		Assert.Equal("GBA_EWRAM", loader.GetSymbol(0x02000000));
		Assert.Equal("GBA_IWRAM", loader.GetSymbol(0x03000000));
	}

	#endregion

	#region Compression Actually Works

	[Fact]
	public void Compression_ActuallyCompresses() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			EnableCompression = true
		};

		// Add repetitive data that compresses well
		for (uint i = 0; i < 1000; i++) {
			writer.MarkAsCode(i);
		}
		for (uint i = 0; i < 500; i++) {
			writer.AddSymbol(0x8000 + i, $"Symbol_{i:D4}");
		}

		var compressed = writer.Generate();

		// Also generate without compression for comparison
		var writer2 = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			EnableCompression = false
		};
		for (uint i = 0; i < 1000; i++) {
			writer2.MarkAsCode(i);
		}
		for (uint i = 0; i < 500; i++) {
			writer2.AddSymbol(0x8000 + i, $"Symbol_{i:D4}");
		}
		var uncompressed = writer2.Generate();

		// Compressed should be smaller than uncompressed
		Assert.True(compressed.Length < uncompressed.Length,
			$"Compressed ({compressed.Length}) should be smaller than uncompressed ({uncompressed.Length})");
	}

	[Fact]
	public void Compression_Roundtrip_PreservesData() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000,
			RomCrc32 = 0xdeadbeef,
			EnableCompression = true,
			ProjectName = "Compressed Test",
			Author = "Test",
			ProjectVersion = "1.0"
		};

		for (uint i = 0; i < 500; i++) {
			writer.MarkAsCode(i);
			writer.AddSymbol(0x8000 + i, $"Sub_{i:x4}");
		}
		for (uint i = 0; i < 100; i++) {
			writer.AddComment(0x8000 + i * 10, $"Comment at {i}");
		}
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, 1, 0, "ROM Bank 0"));
		writer.AddCrossReference(new CrossReference(0x8010, 0x8100, CrossRefType.Jsr));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(PansyLoader.PLATFORM_SNES, loader.Platform);
		Assert.Equal(0x80000u, loader.RomSize);
		Assert.Equal(0xdeadbeefu, loader.RomCrc32);
		Assert.Equal("Compressed Test", loader.ProjectName);
		Assert.Equal(500, loader.Symbols.Count);
		Assert.Equal(100, loader.Comments.Count);
		Assert.Equal(500, loader.CodeOffsets.Count);
		Assert.Single(loader.MemoryRegions);
		Assert.Single(loader.CrossReferences);
		Assert.Equal("Sub_0000", loader.GetSymbol(0x8000));
		Assert.Equal("Sub_01f3", loader.GetSymbol(0x8000 + 499));
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void Symbol_EmptyString_Roundtrips() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbol(0x8000, "");

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal("", loader.GetSymbol(0x8000));
	}

	[Fact]
	public void Symbol_Unicode_Roundtrips() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbol(0x8000, "ゲーム開始");
		writer.AddSymbol(0x8010, "エネミー_攻撃");
		writer.AddSymbol(0x8020, "🌼PansySymbol");

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal("ゲーム開始", loader.GetSymbol(0x8000));
		Assert.Equal("エネミー_攻撃", loader.GetSymbol(0x8010));
		Assert.Equal("🌼PansySymbol", loader.GetSymbol(0x8020));
	}

	[Fact]
	public void Address_Zero_Roundtrips() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_ATARI_2600,
			RomSize = 0x1000
		};

		writer.AddSymbol(0x0000, "ZeroPage_Start");
		writer.MarkAsData(0x0000);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal("ZeroPage_Start", loader.GetSymbol(0x0000));
		Assert.True(loader.IsData(0));
	}

	[Fact]
	public void Address_MaxUint24_Roundtrips() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x1000000
		};

		writer.AddSymbol(0xffffff, "MaxAddress");

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal("MaxAddress", loader.GetSymbol(0xffffff));
	}

	[Fact]
	public void DuplicateSymbol_LastOneWins() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbol(0x8000, "FirstName");
		writer.AddSymbol(0x8000, "SecondName"); // overwrites

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal("SecondName", loader.GetSymbol(0x8000));
	}

	[Fact]
	public void LongSymbolName_Roundtrips() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var longName = new string('A', 1000);
		writer.AddSymbol(0x8000, longName);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(longName, loader.GetSymbol(0x8000));
	}

	[Fact]
	public void AllCodeDataFlags_Combined() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		// Address has all flags set
		writer.MarkAsCode(0x100);
		writer.MarkAsJumpTarget(0x100);
		writer.MarkAsSubroutine(0x100);
		writer.MarkAsOpcode(0x100);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.True(loader.IsCode(0x100));
		Assert.True(loader.IsJumpTarget(0x100));
		Assert.True(loader.IsSubEntryPoint(0x100));
		Assert.True(loader.IsOpcode(0x100));
		Assert.False(loader.IsData(0x100));
	}

	#endregion

	#region Cross-Reference Types

	[Fact]
	public void AllCrossRefTypes_Roundtrip() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x8010, 0x8200, CrossRefType.Jmp));
		writer.AddCrossReference(new CrossReference(0x8020, 0x8030, CrossRefType.Branch));
		writer.AddCrossReference(new CrossReference(0x8040, 0x9000, CrossRefType.Read));
		writer.AddCrossReference(new CrossReference(0x8050, 0x9100, CrossRefType.Write));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(5, loader.CrossReferences.Count);

		var xrefs = loader.CrossReferences.OrderBy(x => x.From).ToList();
		Assert.Equal(CrossRefType.Jsr, xrefs[0].Type);
		Assert.Equal(CrossRefType.Jmp, xrefs[1].Type);
		Assert.Equal(CrossRefType.Branch, xrefs[2].Type);
		Assert.Equal(CrossRefType.Read, xrefs[3].Type);
		Assert.Equal(CrossRefType.Write, xrefs[4].Type);
	}

	#endregion

	#region Memory Region Types

	[Fact]
	public void AllMemoryRegionTypes_Roundtrip() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddMemoryRegion(new MemoryRegion(0x0000, 0x07ff, (byte)MemoryRegionType.RAM, 0, "Internal RAM"));
		writer.AddMemoryRegion(new MemoryRegion(0x2000, 0x2007, (byte)MemoryRegionType.IO, 0, "PPU Registers"));
		writer.AddMemoryRegion(new MemoryRegion(0x6000, 0x7fff, (byte)MemoryRegionType.SRAM, 0, "Battery RAM"));
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, (byte)MemoryRegionType.ROM, 0, "PRG-ROM"));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(4, loader.MemoryRegions.Count);
		var regions = loader.MemoryRegions.OrderBy(r => r.Start).ToList();

		Assert.Equal((byte)MemoryRegionType.RAM, regions[0].Type);
		Assert.Equal("Internal RAM", regions[0].Name);
		Assert.Equal((byte)MemoryRegionType.IO, regions[1].Type);
		Assert.Equal((byte)MemoryRegionType.SRAM, regions[2].Type);
		Assert.Equal((byte)MemoryRegionType.ROM, regions[3].Type);
	}

	[Fact]
	public void MemoryRegion_WithBank_Roundtrips() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x40000
		};

		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xbfff, (byte)MemoryRegionType.ROM, 0, "Bank 0"));
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xbfff, (byte)MemoryRegionType.ROM, 1, "Bank 1"));
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xbfff, (byte)MemoryRegionType.ROM, 2, "Bank 2"));

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(3, loader.MemoryRegions.Count);
		Assert.Equal((byte)0, loader.MemoryRegions[0].Bank);
		Assert.Equal((byte)1, loader.MemoryRegions[1].Bank);
		Assert.Equal((byte)2, loader.MemoryRegions[2].Bank);
	}

	#endregion

	#region Coverage Stats

	[Fact]
	public void GetCoverageStats_CalculatesCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 1000
		};

		for (uint i = 0; i < 300; i++) {
			writer.MarkAsCode(i);
		}
		for (uint i = 300; i < 500; i++) {
			writer.MarkAsData(i);
		}

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var stats = loader.GetCoverageStats();
		Assert.Equal(300, stats.CodeBytes);
		Assert.Equal(200, stats.DataBytes);
		Assert.Equal(1000, stats.TotalSize);
		Assert.Equal(50.0, stats.CoveragePercent, 1);
	}

	#endregion

	#region Loader Error Handling

	[Fact]
	public void Loader_TooShortFile_Throws() {
		var shortData = new byte[] { 0x50, 0x41, 0x4e }; // "PAN" only
		Assert.Throws<InvalidDataException>(() => new PansyLoader(shortData));
	}

	[Fact]
	public void Loader_BadMagic_Throws() {
		var badData = new byte[32];
		badData[0] = (byte)'B';
		badData[1] = (byte)'A';
		badData[2] = (byte)'D';
		Assert.Throws<InvalidDataException>(() => new PansyLoader(badData));
	}

	[Fact]
	public void Loader_EmptyPansyFile_LoadsOk() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_CUSTOM,
			RomSize = 0
		};

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(PansyLoader.PLATFORM_CUSTOM, loader.Platform);
		Assert.Equal(0u, loader.RomSize);
		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
	}

	#endregion

	#region Platform Constants

	[Fact]
	public void AllPlatforms_HaveNames() {
		byte[] platformIds = [
			PansyLoader.PLATFORM_NES, PansyLoader.PLATFORM_SNES,
			PansyLoader.PLATFORM_GB, PansyLoader.PLATFORM_GBA,
			PansyLoader.PLATFORM_GENESIS, PansyLoader.PLATFORM_SMS,
			PansyLoader.PLATFORM_PCE, PansyLoader.PLATFORM_ATARI_2600,
			PansyLoader.PLATFORM_LYNX, PansyLoader.PLATFORM_WONDERSWAN,
			PansyLoader.PLATFORM_NEOGEO, PansyLoader.PLATFORM_SPC700,
			PansyLoader.PLATFORM_C64, PansyLoader.PLATFORM_MSX,
			PansyLoader.PLATFORM_CUSTOM
		];

		foreach (var id in platformIds) {
			var name = PansyLoader.GetPlatformName(id);
			Assert.NotEqual("Unknown", name);
			Assert.NotEmpty(name);
		}
	}

	[Fact]
	public void UnknownPlatform_ReturnsUnknown() {
		Assert.Equal("Unknown", PansyLoader.GetPlatformName(0xfe));
	}

	#endregion
}
