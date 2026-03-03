// ============================================================================
// IntegrationTests.cs - Full Workflow Integration Tests
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;

namespace Pansy.Core.Tests;

/// <summary>
/// End-to-end integration tests simulating real disassembly and analysis
/// workflows across multiple platforms.
/// </summary>
public class IntegrationTests {
	#region Full NES ROM Analysis Simulation

	[Fact]
	public void NES_FullRomAnalysis_Roundtrip() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 32768,
			RomCrc32 = 0xdeadbeef,
			ProjectName = "Super Mario Bros.",
			Author = "Analyst",
			ProjectVersion = "1.0.0",
			EnableCompression = true,
		};

		// Interrupt vectors (as InterruptVector type)
		writer.AddSymbol(0xfffa, "NMI", SymbolType.InterruptVector);
		writer.AddSymbol(0xfffc, "RESET", SymbolType.InterruptVector);
		writer.AddSymbol(0xfffe, "IRQ_BRK", SymbolType.InterruptVector);

		// Function labels
		writer.AddSymbol(0x8000, "Reset_Handler", SymbolType.Function);
		writer.AddSymbol(0x8050, "NMI_Handler", SymbolType.Function);
		writer.AddSymbol(0x80a0, "ReadJoypad", SymbolType.Function);
		writer.AddSymbol(0x80f0, "UpdateSprites", SymbolType.Function);

		// Constants
		writer.AddSymbol(0x00, "PPU_CTRL", SymbolType.Constant);
		writer.AddSymbol(0x01, "PPU_MASK", SymbolType.Constant);
		writer.AddSymbol(0x02, "PPU_STATUS", SymbolType.Constant);

		// Local labels
		writer.AddSymbol(0x8060, ".wait_vblank", SymbolType.Local);
		writer.AddSymbol(0x80b0, ".read_loop", SymbolType.Local);

		// Comments of all types
		writer.AddComment(0x8000, "Entry point - hardware init", (byte)CommentType.Inline);
		writer.AddComment(0x8050, "Called every frame during VBlank\nUpdates game state and renders", (byte)CommentType.Block);
		writer.AddComment(0x80f0, "TODO: DMA transfer could be optimized", (byte)CommentType.Todo);

		// Code/Data map with all flag types
		for (uint i = 0x8000; i < 0x8100; i++) {
			writer.MarkAsCode(i);
			if (i == 0x8000 || i == 0x8050 || i == 0x80a0 || i == 0x80f0) {
				writer.MarkAsOpcode(i);
				writer.MarkAsSubroutine(i);
			}
		}
		for (uint i = 0x8100; i < 0x8200; i++) {
			writer.MarkAsData(i);
			if (i < 0x8180) writer.MarkAsDrawn(i);
			else writer.MarkAsRead(i);
		}
		// Pointer table with indirect flag
		for (uint i = 0x8200; i < 0x8210; i++) {
			writer.MarkAsData(i);
			writer.MarkAsIndirect(i);
		}

		// Jump targets
		writer.MarkAsJumpTarget(0x8060);
		writer.MarkAsJumpTarget(0x80b0);

		// Memory regions
		writer.AddMemoryRegion(new MemoryRegion(0x0000, 0x07ff, (byte)MemoryRegionType.RAM, 0, "Internal RAM"));
		writer.AddMemoryRegion(new MemoryRegion(0x2000, 0x2007, (byte)MemoryRegionType.IO, 0, "PPU Registers"));
		writer.AddMemoryRegion(new MemoryRegion(0x4000, 0x4017, (byte)MemoryRegionType.IO, 0, "APU/IO"));
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, (byte)MemoryRegionType.ROM, 0, "PRG ROM"));

		// Cross-references
		writer.AddCrossReference(new CrossReference(0x8010, 0x80a0, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x8020, 0x80f0, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x8060, 0x8050, CrossRefType.Branch));
		writer.AddCrossReference(new CrossReference(0x8070, 0x8100, CrossRefType.Read));
		writer.AddCrossReference(new CrossReference(0x8080, 0x8200, CrossRefType.Read));

		// --- Generate and reload ---
		var fileData = writer.Generate();
		var loader = new PansyLoader(fileData);

		// Verify header
		Assert.Equal(PansyLoader.PLATFORM_NES, loader.Platform);
		Assert.Equal(32768u, loader.RomSize);
		Assert.Equal(0xdeadbeefu, loader.RomCrc32);
		Assert.Equal("Super Mario Bros.", loader.ProjectName);
		Assert.Equal("Analyst", loader.Author);
		Assert.Equal("1.0.0", loader.ProjectVersion);
		Assert.True(loader.Flags.HasFlag(PansyFlags.Compressed));

		// Verify interrupt vectors
		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0xfffa));
		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0xfffc));
		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0xfffe));

		// Verify functions
		Assert.Equal(SymbolType.Function, loader.GetSymbolType(0x8000));
		Assert.Equal("Reset_Handler", loader.GetSymbol(0x8000));

		// Verify constants
		Assert.Equal(SymbolType.Constant, loader.GetSymbolType(0x00));

		// Verify local labels
		Assert.Equal(SymbolType.Local, loader.GetSymbolType(0x8060));

		// Verify comments
		Assert.Equal(CommentType.Inline, loader.GetCommentType(0x8000));
		Assert.Equal(CommentType.Block, loader.GetCommentType(0x8050));
		Assert.Equal(CommentType.Todo, loader.GetCommentType(0x80f0));
		Assert.Contains("VBlank", loader.GetComment(0x8050)!);

		// Verify code/data map
		Assert.True(loader.IsCode(0x8000));
		Assert.True(loader.IsOpcode(0x8000));
		Assert.True(loader.IsSubEntryPoint(0x8000));
		Assert.True(loader.IsData(0x8100));
		Assert.True(loader.IsDrawn(0x8100));
		Assert.True(loader.IsData(0x8180));
		Assert.True(loader.IsRead(0x8180));
		Assert.True(loader.IsData(0x8200));
		Assert.True(loader.IsIndirect(0x8200));

		// Verify jump targets
		Assert.True(loader.IsJumpTarget(0x8060));
		Assert.True(loader.IsJumpTarget(0x80b0));

		// Verify memory regions
		Assert.Equal(4, loader.MemoryRegions.Count);
		Assert.Equal("Internal RAM", loader.MemoryRegions[0].Name);

		// Verify cross-references
		Assert.Equal(5, loader.CrossReferences.Count);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8010 && x.To == 0x80a0 && x.Type == CrossRefType.Jsr);

		// Verify coverage stats
		var (codeBytes, dataBytes, totalSize, coverage) = loader.GetCoverageStats();
		Assert.Equal(256, codeBytes);
		Assert.Equal(272, dataBytes); // 0x8100-0x8200 (256) + 0x8200-0x8210 (16)
		Assert.Equal(32768, totalSize);
		Assert.True(coverage > 0 && coverage < 100);
	}

	#endregion

	#region GBA Analysis Simulation

	[Fact]
	public void GBA_32BitAddresses_FullWorkflow() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_GBA,
			RomSize = 0x02000000, // 32MB
			RomCrc32 = 0x12345678,
			ProjectName = "Pokemon Ruby",
			EnableCompression = true,
		};

		// GBA exception vectors
		writer.AddSymbol(0x08000000, "EntryPoint", SymbolType.InterruptVector);
		writer.AddSymbol(0x08000004, "Logo", SymbolType.Label);

		// Functions at high addresses
		writer.AddSymbol(0x08100000, "battle_init", SymbolType.Function);
		writer.AddSymbol(0x08200000, "map_load", SymbolType.Function);
		writer.AddSymbol(0x08ff0000, "data_tables", SymbolType.Label);

		// WRAM and IWRAM functions
		writer.AddSymbol(0x02000000, "wram_func", SymbolType.Function);
		writer.AddSymbol(0x03000000, "iwram_func", SymbolType.Function);

		// Code at high 32-bit addresses
		for (uint i = 0x08000000; i < 0x08000020; i++) {
			writer.MarkAsCode(i);
			if (i % 4 == 0) writer.MarkAsOpcode(i); // ARM instructions are 4 bytes
		}

		// Memory regions
		writer.AddMemoryRegion(new MemoryRegion(0x02000000, 0x0203ffff, (byte)MemoryRegionType.WRAM, 0, "On-board WRAM"));
		writer.AddMemoryRegion(new MemoryRegion(0x03000000, 0x03007fff, (byte)MemoryRegionType.WRAM, 0, "On-chip WRAM"));
		writer.AddMemoryRegion(new MemoryRegion(0x08000000, 0x09ffffff, (byte)MemoryRegionType.ROM, 0, "Game ROM"));

		var fileData = writer.Generate();
		var loader = new PansyLoader(fileData);

		// Verify 32-bit addresses survived
		Assert.Equal("EntryPoint", loader.GetSymbol(0x08000000));
		Assert.Equal("battle_init", loader.GetSymbol(0x08100000));
		Assert.Equal("map_load", loader.GetSymbol(0x08200000));
		Assert.Equal("data_tables", loader.GetSymbol(0x08ff0000));
		Assert.Equal("wram_func", loader.GetSymbol(0x02000000));
		Assert.Equal("iwram_func", loader.GetSymbol(0x03000000));

		// Verify symbol types at high addresses
		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0x08000000));
		Assert.Equal(SymbolType.Function, loader.GetSymbolType(0x08100000));

		// Verify code map at high addresses
		Assert.True(loader.IsCode(0x08000000));
		Assert.True(loader.IsOpcode(0x08000000));
		Assert.True(loader.IsCode(0x08000001));
		Assert.False(loader.IsOpcode(0x08000001));

		// Verify memory regions
		Assert.Equal(3, loader.MemoryRegions.Count);
	}

	#endregion

	#region SNES LoROM Analysis Simulation

	[Fact]
	public void SNES_BankedAddresses_WithCrossRefs() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x100000, // 1MB
			RomCrc32 = 0xaabbccdd,
			ProjectName = "FFMQ",
			Author = "Disassembler",
			ProjectVersion = "0.5.0",
		};

		// Functions across banks
		writer.AddSymbol(0x008000, "bank0_start", SymbolType.Function);
		writer.AddSymbol(0x018000, "bank1_start", SymbolType.Function);
		writer.AddSymbol(0x028000, "bank2_start", SymbolType.Function);

		// Cross-refs across banks
		writer.AddCrossReference(new CrossReference(0x008100, 0x018000, CrossRefType.Jsr)); // bank 0 → bank 1
		writer.AddCrossReference(new CrossReference(0x018100, 0x028000, CrossRefType.Jmp)); // bank 1 → bank 2
		writer.AddCrossReference(new CrossReference(0x028100, 0x008000, CrossRefType.Jsr)); // bank 2 → bank 0
		writer.AddCrossReference(new CrossReference(0x008200, 0x028200, CrossRefType.Read)); // data read cross-bank

		// Memory regions with banks
		writer.AddMemoryRegion(new MemoryRegion(0x008000, 0x00ffff, (byte)MemoryRegionType.ROM, 0, "Bank 0"));
		writer.AddMemoryRegion(new MemoryRegion(0x018000, 0x01ffff, (byte)MemoryRegionType.ROM, 1, "Bank 1"));
		writer.AddMemoryRegion(new MemoryRegion(0x028000, 0x02ffff, (byte)MemoryRegionType.ROM, 2, "Bank 2"));
		writer.AddMemoryRegion(new MemoryRegion(0x7e0000, 0x7fffff, (byte)MemoryRegionType.WRAM, 0, "Work RAM"));

		var fileData = writer.Generate();
		var loader = new PansyLoader(fileData);

		// Verify cross-bank references
		Assert.Equal(4, loader.CrossReferences.Count);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x008100 && x.To == 0x018000 && x.Type == CrossRefType.Jsr);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x028100 && x.To == 0x008000 && x.Type == CrossRefType.Jsr);

		// Verify banked regions
		Assert.Equal(4, loader.MemoryRegions.Count);
		Assert.Equal(0, loader.MemoryRegions[0].Bank);
		Assert.Equal(1, loader.MemoryRegions[1].Bank);
		Assert.Equal(2, loader.MemoryRegions[2].Bank);
	}

	#endregion

	#region Large-Scale Tests

	[Fact]
	public void LargeFile_AllSections_Compressed() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x40000, // 256KB
			RomCrc32 = 0x11223344,
			ProjectName = "Large Analysis",
			Author = "Bot",
			ProjectVersion = "2.0.0",
			EnableCompression = true,
		};

		// 5000 symbols
		for (uint i = 0; i < 5000; i++) {
			var type = (SymbolType)((i % 9) + 1); // cycle through all types
			writer.AddSymbol(0x8000 + i * 4, $"sym_{i:d5}", type);
		}

		// 2000 comments
		for (uint i = 0; i < 2000; i++) {
			var ct = (byte)((i % 3) + 1);
			writer.AddComment(0x8000 + i * 8, $"Comment #{i} for address", ct);
		}

		// 20000 code offsets
		for (uint i = 0; i < 20000; i++) {
			writer.MarkAsCode(i);
			if (i % 3 == 0) writer.MarkAsOpcode(i);
			if (i % 5 == 0) writer.MarkAsJumpTarget(i);
			if (i % 7 == 0) writer.MarkAsSubroutine(i);
		}

		// 10000 data offsets with mixed flags
		for (uint i = 0x20000; i < 0x22710; i++) {
			writer.MarkAsData(i);
			if (i % 3 == 0) writer.MarkAsDrawn(i);
			if (i % 5 == 0) writer.MarkAsRead(i);
			if (i % 7 == 0) writer.MarkAsIndirect(i);
		}

		// 5000 cross-references
		for (uint i = 0; i < 5000; i++) {
			var type = (CrossRefType)((i % 5) + 1);
			writer.AddCrossReference(new CrossReference(0x8000 + i * 2, 0x8000 + (i * 3 % 20000), type));
		}

		// 50 memory regions
		for (uint i = 0; i < 50; i++) {
			writer.AddMemoryRegion(new MemoryRegion(
				i * 0x1000, (i + 1) * 0x1000 - 1,
				(byte)((i % 8) + 1), (byte)(i % 4),
				$"Region_{i:d3}"));
		}

		var fileData = writer.Generate();
		var loader = new PansyLoader(fileData);

		// Verify counts
		Assert.Equal(5000, loader.Symbols.Count);
		Assert.Equal(5000, loader.SymbolEntries.Count);
		Assert.Equal(2000, loader.Comments.Count);
		Assert.Equal(2000, loader.CommentEntries.Count);
		Assert.Equal(20000, loader.CodeOffsets.Count);
		Assert.Equal(5000, loader.CrossReferences.Count);
		Assert.Equal(50, loader.MemoryRegions.Count);

		// Verify symbol type distribution
		var typeCounts = loader.SymbolEntries.Values
			.GroupBy(e => e.Type)
			.ToDictionary(g => g.Key, g => g.Count());
		// 5000 symbols cycling through 9 types = ~555 each
		foreach (var type in System.Enum.GetValues<SymbolType>()) {
			Assert.True(typeCounts.ContainsKey(type), $"Missing SymbolType {type}");
		}

		// Verify comment type distribution
		var commentTypeCounts = loader.CommentEntries.Values
			.GroupBy(e => e.Type)
			.ToDictionary(g => g.Key, g => g.Count());
		Assert.True(commentTypeCounts.ContainsKey(CommentType.Inline));
		Assert.True(commentTypeCounts.ContainsKey(CommentType.Block));
		Assert.True(commentTypeCounts.ContainsKey(CommentType.Todo));

		// Verify compression was actually used
		Assert.True(loader.Flags.HasFlag(PansyFlags.Compressed));

		// Verify metadata
		Assert.Equal("Large Analysis", loader.ProjectName);
		Assert.Equal("Bot", loader.Author);
		Assert.Equal("2.0.0", loader.ProjectVersion);
	}

	[Fact]
	public void MultiPlatform_SameFormat_DifferentContent() {
		// Test that different platforms can all use the same format
		byte[] platforms = [
			PansyLoader.PLATFORM_NES,
			PansyLoader.PLATFORM_SNES,
			PansyLoader.PLATFORM_GB,
			PansyLoader.PLATFORM_GBA,
			PansyLoader.PLATFORM_GENESIS,
			PansyLoader.PLATFORM_SMS,
			PansyLoader.PLATFORM_PCE,
			PansyLoader.PLATFORM_ATARI_2600,
			PansyLoader.PLATFORM_LYNX,
			PansyLoader.PLATFORM_WONDERSWAN,
		];

		foreach (var platform in platforms) {
			var writer = new PansyWriter { Platform = platform, RomSize = 0x8000 };
			writer.AddSymbol(0x8000, $"start_{PansyLoader.GetPlatformName(platform)}", SymbolType.Function);
			writer.AddComment(0x8000, "Entry", (byte)CommentType.Inline);
			writer.MarkAsCode(0x8000);

			var data = writer.Generate();
			var loader = new PansyLoader(data);

			Assert.Equal(platform, loader.Platform);
			Assert.Single(loader.Symbols);
			Assert.Equal(SymbolType.Function, loader.GetSymbolType(0x8000));
			Assert.Contains(PansyLoader.GetPlatformName(platform), loader.GetSymbol(0x8000)!);
		}
	}

	#endregion

	#region PlatformDefaults Integration

	[Fact]
	public void PlatformDefaults_NES_AsMemoryRegions() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 32768,
		};

		// Load platform defaults and add them
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_NES);
		foreach (var region in regions) {
			writer.AddMemoryRegion(region);
		}

		// Add NES interrupt vectors
		writer.AddSymbol(0xfffa, "NMI", SymbolType.InterruptVector);
		writer.AddSymbol(0xfffc, "RESET", SymbolType.InterruptVector);
		writer.AddSymbol(0xfffe, "IRQ", SymbolType.InterruptVector);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(regions.Length, loader.MemoryRegions.Count);
		Assert.Equal(3, loader.Symbols.Count);

		// All vectors should be InterruptVector type
		foreach (var addr in new[] { 0xfffa, 0xfffc, 0xfffe }) {
			Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(addr));
		}
	}

	#endregion
}
