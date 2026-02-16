using Pansy.Core;
using Xunit;

namespace Pansy.Core.Tests;

/// <summary>
/// Unit tests for PlatformDefaults class.
/// </summary>
public class PlatformDefaultsTests {
	// ========================================================================
	// GetDefaultRegions Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_NES_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_NES);

		Assert.NotNull(regions);
		Assert.True(regions.Length >= 4, "Expected at least 4 NES regions");
		Assert.Contains(regions, r => r.Name == "Zero Page" && r.Start == 0x0000 && r.End == 0x00ff);
		Assert.Contains(regions, r => r.Name == "Stack" && r.Start == 0x0100 && r.End == 0x01ff);
		Assert.Contains(regions, r => r.Name == "RAM" && r.Start == 0x0200 && r.End == 0x07ff);
		Assert.Contains(regions, r => r.Name == "PRG ROM" && r.Start == 0x8000 && r.End == 0xffff);
	}

	[Fact]
	public void GetDefaultRegions_SNES_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_SNES);

		Assert.NotNull(regions);
		Assert.True(regions.Length >= 3, "Expected at least 3 SNES regions");
		Assert.Contains(regions, r => r.Name == "Work RAM" && r.Start == 0x7e0000 && r.End == 0x7fffff);
		Assert.Contains(regions, r => r.Name == "PPU Registers" && r.Start == 0x2100 && r.End == 0x21ff);
	}

	[Fact]
	public void GetDefaultRegions_GB_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_GB);

		Assert.NotNull(regions);
		Assert.True(regions.Length >= 5, "Expected at least 5 GB regions");
		Assert.Contains(regions, r => r.Name == "ROM Bank 0" && r.Start == 0x0000 && r.End == 0x3fff);
		Assert.Contains(regions, r => r.Name == "ROM Bank N" && r.Start == 0x4000 && r.End == 0x7fff);
		Assert.Contains(regions, r => r.Name == "Video RAM" && r.Start == 0x8000 && r.End == 0x9fff);
		Assert.Contains(regions, r => r.Name == "Work RAM" && r.Start == 0xc000 && r.End == 0xdfff);
		Assert.Contains(regions, r => r.Name == "High RAM" && r.Start == 0xff80 && r.End == 0xfffe);
	}

	[Fact]
	public void GetDefaultRegions_Lynx_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_LYNX);

		Assert.NotNull(regions);
		Assert.Equal(6, regions.Length);
		Assert.Contains(regions, r => r.Name == "Zero Page" && r.Start == 0x0000 && r.End == 0x00ff);
		Assert.Contains(regions, r => r.Name == "Stack" && r.Start == 0x0100 && r.End == 0x01ff);
		Assert.Contains(regions, r => r.Name == "Work RAM" && r.Start == 0x0200 && r.End == 0xfbff);
		Assert.Contains(regions, r => r.Name == "Suzy Registers" && r.Start == 0xfc00 && r.End == 0xfcff);
		Assert.Contains(regions, r => r.Name == "Mikey Registers" && r.Start == 0xfd00 && r.End == 0xfdff);
		Assert.Contains(regions, r => r.Name == "Boot ROM" && r.Start == 0xfe00 && r.End == 0xffff);
	}

	[Fact]
	public void GetDefaultRegions_Atari2600_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_2600);

		Assert.NotNull(regions);
		Assert.Equal(4, regions.Length);
		Assert.Contains(regions, r => r.Name == "TIA Registers" && r.Start == 0x0000 && r.End == 0x007f);
		Assert.Contains(regions, r => r.Name == "RAM" && r.Start == 0x0080 && r.End == 0x00ff);
		Assert.Contains(regions, r => r.Name == "RIOT Registers" && r.Start == 0x0280 && r.End == 0x0297);
		Assert.Contains(regions, r => r.Name == "ROM" && r.Start == 0xf000 && r.End == 0xffff);
	}

	[Fact]
	public void GetDefaultRegions_Unknown_ReturnsEmptyArray() {
		var regions = PlatformDefaults.GetDefaultRegions(0x99);

		Assert.NotNull(regions);
		Assert.Empty(regions);
	}

	// ========================================================================
	// GetDefaultSymbols Tests
	// ========================================================================

	[Fact]
	public void GetDefaultSymbols_Lynx_ReturnsExpectedSymbols() {
		var symbols = PlatformDefaults.GetDefaultSymbols(PansyLoader.PLATFORM_LYNX);

		Assert.NotNull(symbols);
		Assert.True(symbols.Count >= 10, $"Expected at least 10 Lynx symbols, got {symbols.Count}");

		// Test Suzy registers (dictionary is address -> name)
		Assert.True(symbols.ContainsKey(0xfc80), "Expected SPRCTL0 at $fc80");
		Assert.Equal("SPRCTL0", symbols[0xfc80]);
		Assert.True(symbols.ContainsKey(0xfc92), "Expected SPRSYS at $fc92");
		Assert.Equal("SPRSYS", symbols[0xfc92]);
		Assert.True(symbols.ContainsKey(0xfcb0), "Expected JOYSTICK at $fcb0");
		Assert.Equal("JOYSTICK", symbols[0xfcb0]);
	}

	[Fact]
	public void GetDefaultSymbols_Unknown_ReturnsEmptyDictionary() {
		var symbols = PlatformDefaults.GetDefaultSymbols(0x99);

		Assert.NotNull(symbols);
		Assert.Empty(symbols);
	}

	// ========================================================================
	// Region Type Tests
	// ========================================================================

	[Fact]
	public void LynxRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_LYNX);

		// Verify RAM regions have correct type
		var zeroPageRegion = regions.First(r => r.Name == "Zero Page");
		Assert.Equal((byte)MemoryRegionType.RAM, zeroPageRegion.Type);

		// Verify I/O regions have IO type
		var suzyRegion = regions.First(r => r.Name == "Suzy Registers");
		Assert.Equal((byte)MemoryRegionType.IO, suzyRegion.Type);

		var mikeyRegion = regions.First(r => r.Name == "Mikey Registers");
		Assert.Equal((byte)MemoryRegionType.IO, mikeyRegion.Type);

		// Verify Boot ROM has ROM type
		var bootRomRegion = regions.First(r => r.Name == "Boot ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, bootRomRegion.Type);

		// Verify Work RAM has WRAM type
		var workRamRegion = regions.First(r => r.Name == "Work RAM");
		Assert.Equal((byte)MemoryRegionType.WRAM, workRamRegion.Type);
	}

	[Fact]
	public void Atari2600Regions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_2600);

		// Verify types
		var tiaRegion = regions.First(r => r.Name == "TIA Registers");
		Assert.Equal((byte)MemoryRegionType.IO, tiaRegion.Type);

		var ramRegion = regions.First(r => r.Name == "RAM");
		Assert.Equal((byte)MemoryRegionType.RAM, ramRegion.Type);

		var romRegion = regions.First(r => r.Name == "ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, romRegion.Type);
	}
}
