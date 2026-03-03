// ============================================================================
// CodeDataMapTests.cs - Tests for Extended Code/Data Map Flags
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;

namespace Pansy.Core.Tests;

/// <summary>
/// Tests for all 8 code/data map flag bits including the new
/// DRAWN, READ, and INDIRECT flags.
/// </summary>
public class CodeDataMapTests {
	#region Individual Flag Tests

	[Fact]
	public void MarkAsDrawn_Roundtrip() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.MarkAsDrawn(0x0000);
		writer.MarkAsDrawn(0x0100);
		writer.MarkAsDrawn(0x0200);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.True(loader.IsDrawn(0x0000));
		Assert.True(loader.IsDrawn(0x0100));
		Assert.True(loader.IsDrawn(0x0200));
		Assert.False(loader.IsDrawn(0x0050));
		Assert.Equal(3, loader.DrawnOffsets.Count);
	}

	[Fact]
	public void MarkAsRead_Roundtrip() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.MarkAsRead(0x1000);
		writer.MarkAsRead(0x2000);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.True(loader.IsRead(0x1000));
		Assert.True(loader.IsRead(0x2000));
		Assert.False(loader.IsRead(0x3000));
		Assert.Equal(2, loader.ReadOffsets.Count);
	}

	[Fact]
	public void MarkAsIndirect_Roundtrip() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.MarkAsIndirect(0x5000);
		writer.MarkAsIndirect(0x5001);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.True(loader.IsIndirect(0x5000));
		Assert.True(loader.IsIndirect(0x5001));
		Assert.False(loader.IsIndirect(0x5002));
		Assert.Equal(2, loader.IndirectOffsets.Count);
	}

	#endregion

	#region Combined Flag Tests

	[Fact]
	public void AllEightFlags_OnSameAddress() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		uint addr = 0x1000;
		writer.MarkAsCode(addr);
		writer.MarkAsData(addr);
		writer.MarkAsJumpTarget(addr);
		writer.MarkAsSubroutine(addr);
		writer.MarkAsOpcode(addr);
		writer.MarkAsDrawn(addr);
		writer.MarkAsRead(addr);
		writer.MarkAsIndirect(addr);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.True(loader.IsCode((int)addr));
		Assert.True(loader.IsData((int)addr));
		Assert.True(loader.IsJumpTarget((int)addr));
		Assert.True(loader.IsSubEntryPoint((int)addr));
		Assert.True(loader.IsOpcode((int)addr));
		Assert.True(loader.IsDrawn((int)addr));
		Assert.True(loader.IsRead((int)addr));
		Assert.True(loader.IsIndirect((int)addr));
	}

	[Fact]
	public void MixedFlags_DifferentAddresses() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };

		// Code byte that's also a jump target
		writer.MarkAsCode(0x100);
		writer.MarkAsJumpTarget(0x100);
		writer.MarkAsOpcode(0x100);

		// Data byte that was drawn by PPU
		writer.MarkAsData(0x200);
		writer.MarkAsDrawn(0x200);

		// Data byte read by CPU
		writer.MarkAsData(0x300);
		writer.MarkAsRead(0x300);

		// Indirect pointer table entry
		writer.MarkAsData(0x400);
		writer.MarkAsIndirect(0x400);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// 0x100: code + jump target + opcode
		Assert.True(loader.IsCode(0x100));
		Assert.True(loader.IsJumpTarget(0x100));
		Assert.True(loader.IsOpcode(0x100));
		Assert.False(loader.IsData(0x100));
		Assert.False(loader.IsDrawn(0x100));

		// 0x200: data + drawn
		Assert.True(loader.IsData(0x200));
		Assert.True(loader.IsDrawn(0x200));
		Assert.False(loader.IsCode(0x200));
		Assert.False(loader.IsRead(0x200));

		// 0x300: data + read
		Assert.True(loader.IsData(0x300));
		Assert.True(loader.IsRead(0x300));
		Assert.False(loader.IsDrawn(0x300));

		// 0x400: data + indirect
		Assert.True(loader.IsData(0x400));
		Assert.True(loader.IsIndirect(0x400));
		Assert.False(loader.IsRead(0x400));
	}

	[Fact]
	public void DrawnReadIndirect_WithCompression() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			EnableCompression = true
		};

		for (uint i = 0; i < 1000; i++) {
			writer.MarkAsDrawn(i);
			if (i % 2 == 0) writer.MarkAsRead(i);
			if (i % 3 == 0) writer.MarkAsIndirect(i);
		}

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(1000, loader.DrawnOffsets.Count);
		Assert.Equal(500, loader.ReadOffsets.Count);
		Assert.Equal(334, loader.IndirectOffsets.Count);

		// Spot check
		Assert.True(loader.IsDrawn(0));
		Assert.True(loader.IsRead(0));
		Assert.True(loader.IsIndirect(0));

		Assert.True(loader.IsDrawn(1));
		Assert.False(loader.IsRead(1));
		Assert.False(loader.IsIndirect(1));

		Assert.True(loader.IsDrawn(6));
		Assert.True(loader.IsRead(6));
		Assert.True(loader.IsIndirect(6));
	}

	#endregion

	#region Realistic Scenario Tests

	[Fact]
	public void NES_PPU_DataAccess_Pattern() {
		// Simulate a real NES CHR data access pattern
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };

		// CHR tile data rendered by PPU (drawn flag)
		for (uint i = 0; i < 256; i++) {
			writer.MarkAsData(i);
			writer.MarkAsDrawn(i);
		}

		// Lookup table accessed via indirect addressing
		for (uint i = 0x1000; i < 0x1020; i++) {
			writer.MarkAsData(i);
			writer.MarkAsIndirect(i);
			writer.MarkAsRead(i);
		}

		// Code section
		for (uint i = 0x8000; i < 0x8010; i++) {
			writer.MarkAsCode(i);
			if (i % 3 == 0) writer.MarkAsOpcode(i);
		}

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Verify CHR data
		Assert.True(loader.IsData(0));
		Assert.True(loader.IsDrawn(0));
		Assert.False(loader.IsRead(0));

		// Verify lookup table
		Assert.True(loader.IsData(0x1000));
		Assert.True(loader.IsIndirect(0x1000));
		Assert.True(loader.IsRead(0x1000));
		Assert.False(loader.IsDrawn(0x1000));

		// Verify code
		Assert.True(loader.IsCode(0x8000));
		Assert.False(loader.IsOpcode(0x8000)); // 0x8000 % 3 == 2, not 0
		Assert.True(loader.IsOpcode(0x8001)); // 0x8001 % 3 == 0
		Assert.False(loader.IsDrawn(0x8000));
	}

	[Fact]
	public void EmptyCodeDataMap_NoFlagsSet() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		// Add symbols but no code/data flags
		writer.AddSymbol(0x8000, "Reset", SymbolType.Label);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Empty(loader.CodeOffsets);
		Assert.Empty(loader.DataOffsets);
		Assert.Empty(loader.DrawnOffsets);
		Assert.Empty(loader.ReadOffsets);
		Assert.Empty(loader.IndirectOffsets);
		Assert.False(loader.HasCodeDataMap);
	}

	[Fact]
	public void OnlyDrawnFlag_StillCreatesCodeDataMap() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.MarkAsDrawn(0x100);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.True(loader.IsDrawn(0x100));
		Assert.Equal(1, loader.DrawnOffsets.Count);
	}

	#endregion
}
