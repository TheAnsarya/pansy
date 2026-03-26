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
	public void GetDefaultRegions_ChannelF_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_CHANNEL_F);

		Assert.NotNull(regions);
		Assert.Equal(4, regions.Length);
		Assert.Contains(regions, r => r.Name == "Cartridge ROM" && r.Start == 0x0000 && r.End == 0x17ff);
		Assert.Contains(regions, r => r.Name == "System RAM" && r.Start == 0x2800 && r.End == 0x2fff);
		Assert.Contains(regions, r => r.Name == "Video RAM" && r.Start == 0x3000 && r.End == 0x37ff);
		Assert.Contains(regions, r => r.Name == "I/O Registers" && r.Start == 0x3800 && r.End == 0x38ff);
	}

	[Fact]
	public void GetDefaultRegions_Unknown_ReturnsEmptyArray() {
		var regions = PlatformDefaults.GetDefaultRegions(0x99);

		Assert.NotNull(regions);
		Assert.Empty(regions);
	}

	// ========================================================================
	// GetDefaultSymbols Tests (backward compat wrapper)
	// ========================================================================

	[Fact]
	public void GetDefaultSymbols_Lynx_ReturnsNames() {
		var symbols = PlatformDefaults.GetDefaultSymbols(PansyLoader.PLATFORM_LYNX);

		Assert.NotNull(symbols);
		Assert.True(symbols.Count >= 10, $"Expected at least 10 Lynx symbols, got {symbols.Count}");

		Assert.True(symbols.ContainsKey(0xfc90), "Expected SPRCTL0 at $fc90");
		Assert.Equal("SPRCTL0", symbols[0xfc90]);
		Assert.True(symbols.ContainsKey(0xfcc0), "Expected JOYSTICK at $fcc0");
		Assert.Equal("JOYSTICK", symbols[0xfcc0]);
	}

	[Fact]
	public void GetDefaultSymbols_Unknown_ReturnsEmptyDictionary() {
		var symbols = PlatformDefaults.GetDefaultSymbols(0x99);

		Assert.NotNull(symbols);
		Assert.Empty(symbols);
	}

	// ========================================================================
	// GetDefaultSymbolEntries Tests — All Platforms
	// ========================================================================

	[Theory]
	[InlineData(PansyLoader.PLATFORM_NES)]
	[InlineData(PansyLoader.PLATFORM_SNES)]
	[InlineData(PansyLoader.PLATFORM_GB)]
	[InlineData(PansyLoader.PLATFORM_GBA)]
	[InlineData(PansyLoader.PLATFORM_PCE)]
	[InlineData(PansyLoader.PLATFORM_SMS)]
	[InlineData(PansyLoader.PLATFORM_WONDERSWAN)]
	[InlineData(PansyLoader.PLATFORM_LYNX)]
	[InlineData(PansyLoader.PLATFORM_ATARI_2600)]
	[InlineData(PansyLoader.PLATFORM_CHANNEL_F)]
	[InlineData(PansyLoader.PLATFORM_SPC700)]
	public void GetDefaultSymbolEntries_AllPlatforms_ReturnNonEmpty(byte platformId) {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(platformId);

		Assert.NotNull(entries);
		Assert.NotEmpty(entries);
	}

	[Fact]
	public void GetDefaultSymbolEntries_Unknown_ReturnsEmpty() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(0x99);

		Assert.NotNull(entries);
		Assert.Empty(entries);
	}

	[Theory]
	[InlineData(PansyLoader.PLATFORM_NES)]
	[InlineData(PansyLoader.PLATFORM_SNES)]
	[InlineData(PansyLoader.PLATFORM_GB)]
	[InlineData(PansyLoader.PLATFORM_GBA)]
	[InlineData(PansyLoader.PLATFORM_PCE)]
	[InlineData(PansyLoader.PLATFORM_SMS)]
	[InlineData(PansyLoader.PLATFORM_WONDERSWAN)]
	[InlineData(PansyLoader.PLATFORM_LYNX)]
	[InlineData(PansyLoader.PLATFORM_ATARI_2600)]
	[InlineData(PansyLoader.PLATFORM_CHANNEL_F)]
	[InlineData(PansyLoader.PLATFORM_SPC700)]
	public void GetDefaultSymbolEntries_AllSymbols_HaveNonEmptyNames(byte platformId) {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(platformId);

		foreach (var (address, symbol) in entries) {
			Assert.False(string.IsNullOrWhiteSpace(symbol.Name),
				$"Platform 0x{platformId:x2}: Symbol at 0x{address:x4} has empty name");
			Assert.False(string.IsNullOrWhiteSpace(symbol.Description),
				$"Platform 0x{platformId:x2}: Symbol at 0x{address:x4} has empty description");
		}
	}

	[Theory]
	[InlineData(PansyLoader.PLATFORM_NES)]
	[InlineData(PansyLoader.PLATFORM_SNES)]
	[InlineData(PansyLoader.PLATFORM_GB)]
	[InlineData(PansyLoader.PLATFORM_GBA)]
	[InlineData(PansyLoader.PLATFORM_PCE)]
	[InlineData(PansyLoader.PLATFORM_SMS)]
	[InlineData(PansyLoader.PLATFORM_WONDERSWAN)]
	[InlineData(PansyLoader.PLATFORM_LYNX)]
	[InlineData(PansyLoader.PLATFORM_ATARI_2600)]
	[InlineData(PansyLoader.PLATFORM_CHANNEL_F)]
	[InlineData(PansyLoader.PLATFORM_SPC700)]
	public void GetDefaultSymbolEntries_ConsistentWithGetDefaultSymbols(byte platformId) {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(platformId);
		var symbols = PlatformDefaults.GetDefaultSymbols(platformId);

		Assert.Equal(entries.Count, symbols.Count);
		foreach (var (address, symbol) in entries) {
			Assert.True(symbols.ContainsKey(address),
				$"Platform 0x{platformId:x2}: Address 0x{address:x4} in entries but not in symbols");
			Assert.Equal(symbol.Name, symbols[address]);
		}
	}

	// NES-specific key registers
	[Fact]
	public void GetDefaultSymbolEntries_NES_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_NES);

		Assert.True(entries.ContainsKey(0x2000), "NES: Missing PPUCTRL at $2000");
		Assert.Equal("PPUCTRL", entries[0x2000].Name);

		Assert.True(entries.ContainsKey(0x2001), "NES: Missing PPUMASK at $2001");
		Assert.Equal("PPUMASK", entries[0x2001].Name);

		Assert.True(entries.ContainsKey(0x2002), "NES: Missing PPUSTATUS at $2002");
		Assert.True(entries.ContainsKey(0x4014), "NES: Missing OAMDMA at $4014");
		Assert.True(entries.ContainsKey(0x4015), "NES: Missing SND_CHN at $4015");
		Assert.True(entries.ContainsKey(0x4016), "NES: Missing JOY1 at $4016");

		// Interrupt vectors
		Assert.True(entries.ContainsKey(0xfffa), "NES: Missing NMI vector");
		Assert.Equal(SymbolType.InterruptVector, entries[0xfffa].Type);
		Assert.True(entries.ContainsKey(0xfffc), "NES: Missing RESET vector");
		Assert.True(entries.ContainsKey(0xfffe), "NES: Missing IRQ vector");
	}

	// SNES-specific key registers
	[Fact]
	public void GetDefaultSymbolEntries_SNES_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_SNES);

		Assert.True(entries.ContainsKey(0x2100), "SNES: Missing INIDISP at $2100");
		Assert.Equal("INIDISP", entries[0x2100].Name);

		Assert.True(entries.ContainsKey(0x2105), "SNES: Missing BGMODE at $2105");
		Assert.True(entries.ContainsKey(0x2140), "SNES: Missing APUIO0 at $2140");
		Assert.True(entries.ContainsKey(0x4200), "SNES: Missing NMITIMEN at $4200");
		Assert.True(entries.ContainsKey(0x420b), "SNES: Missing MDMAEN at $420b");

		// DMA channels (8 channels)
		for (uint ch = 0; ch < 8; ch++) {
			uint dmaBase = 0x4300 + (ch * 0x10);
			Assert.True(entries.ContainsKey(dmaBase),
				$"SNES: Missing DMA channel {ch} control at ${dmaBase:x4}");
		}
	}

	// GB-specific key registers
	[Fact]
	public void GetDefaultSymbolEntries_GB_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_GB);

		Assert.True(entries.ContainsKey(0xff40), "GB: Missing LCDC at $ff40");
		Assert.Equal("LCDC", entries[0xff40].Name);

		Assert.True(entries.ContainsKey(0xff00), "GB: Missing JOYP at $ff00");
		Assert.True(entries.ContainsKey(0xff0f), "GB: Missing IF at $ff0f");
		Assert.True(entries.ContainsKey(0xffff), "GB: Missing IE at $ffff");
		Assert.True(entries.ContainsKey(0xff10), "GB: Missing NR10 at $ff10");
	}

	// GBA-specific key registers
	[Fact]
	public void GetDefaultSymbolEntries_GBA_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_GBA);

		Assert.True(entries.ContainsKey(0x04000000), "GBA: Missing DISPCNT at $04000000");
		Assert.Equal("DISPCNT", entries[0x04000000].Name);

		Assert.True(entries.ContainsKey(0x04000130), "GBA: Missing KEYINPUT at $04000130");
		Assert.True(entries.ContainsKey(0x04000200), "GBA: Missing IE at $04000200");
		Assert.True(entries.ContainsKey(0x04000208), "GBA: Missing IME at $04000208");
	}

	// SPC700-specific key registers
	[Fact]
	public void GetDefaultSymbolEntries_SPC700_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_SPC700);

		Assert.True(entries.ContainsKey(0xf0), "SPC700: Missing TEST at $f0");
		Assert.True(entries.ContainsKey(0xf1), "SPC700: Missing CONTROL at $f1");
		Assert.True(entries.ContainsKey(0xf2), "SPC700: Missing DSPADDR at $f2");
		Assert.True(entries.ContainsKey(0xf4), "SPC700: Missing CPUIO0 at $f4");
		Assert.Equal(16, entries.Count);
	}

	// Atari 2600 key registers
	[Fact]
	public void GetDefaultSymbolEntries_Atari2600_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_ATARI_2600);

		Assert.True(entries.ContainsKey(0x00), "A2600: Missing VSYNC at $00");
		Assert.Equal("VSYNC", entries[0x00].Name);
		Assert.True(entries.ContainsKey(0x02), "A2600: Missing WSYNC at $02");
		Assert.True(entries.ContainsKey(0x0280), "A2600: Missing SWCHA at $0280");
		Assert.True(entries.ContainsKey(0x0284), "A2600: Missing INTIM at $0284");

		// Interrupt vectors
		Assert.True(entries.ContainsKey(0xfffc), "A2600: Missing RESET vector");
		Assert.Equal(SymbolType.InterruptVector, entries[0xfffc].Type);
	}

	[Fact]
	public void GetDefaultSymbolEntries_ChannelF_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_CHANNEL_F);

		Assert.True(entries.ContainsKey(0x3800), "Channel F: Missing CH_F_PORT0 at $3800");
		Assert.Equal("CH_F_PORT0", entries[0x3800].Name);
		Assert.True(entries.ContainsKey(0x3803), "Channel F: Missing CH_F_PORT3 at $3803");
		Assert.True(entries.ContainsKey(0x3fff), "Channel F: Missing RESET_VECTOR at $3fff");
		Assert.Equal(SymbolType.InterruptVector, entries[0x3fff].Type);
	}

	// Lynx updated key registers
	[Fact]
	public void GetDefaultSymbolEntries_Lynx_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_LYNX);

		Assert.True(entries.ContainsKey(0xfc90), "Lynx: Missing SPRCTL0 at $fc90");
		Assert.Equal("SPRCTL0", entries[0xfc90].Name);

		Assert.True(entries.ContainsKey(0xfcc0), "Lynx: Missing JOYSTICK at $fcc0");
		Assert.True(entries.ContainsKey(0xfd00), "Lynx: Missing TIM0BKUP at $fd00");
		Assert.True(entries.ContainsKey(0xfd80), "Lynx: Missing INTRST at $fd80");
		Assert.True(entries.ContainsKey(0xfd92), "Lynx: Missing DISPCTL at $fd92");

		// Vectors
		Assert.True(entries.ContainsKey(0xfffa), "Lynx: Missing NMI vector");
		Assert.Equal(SymbolType.InterruptVector, entries[0xfffa].Type);
	}

	// PCE key registers
	[Fact]
	public void GetDefaultSymbolEntries_PCE_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_PCE);

		Assert.True(entries.ContainsKey(0x0000), "PCE: Missing VDC_AR at $0000");
		Assert.True(entries.ContainsKey(0x0800), "PCE: Missing PSG_CHANSELECT at $0800");
		Assert.True(entries.ContainsKey(0x1000), "PCE: Missing JOYPAD at $1000");
	}

	// SMS key registers
	[Fact]
	public void GetDefaultSymbolEntries_SMS_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_SMS);

		Assert.Equal(8, entries.Count);
		Assert.True(entries.ContainsKey(0xbe), "SMS: Missing VDP_DATA at $be");
		Assert.True(entries.ContainsKey(0xbf), "SMS: Missing VDP_CMD_STATUS at $bf");
	}

	// WonderSwan key registers
	[Fact]
	public void GetDefaultSymbolEntries_WS_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_WONDERSWAN);

		Assert.True(entries.Count >= 80, $"WS: Expected at least 80 registers, got {entries.Count}");
		Assert.True(entries.ContainsKey(0x00), "WS: Missing DISPLAY_CTRL at $00");
		Assert.True(entries.ContainsKey(0x80), "WS: Missing SND_FREQ_CH1 at $80");
		Assert.True(entries.ContainsKey(0xb2), "WS: Missing HWINT_ENABLE at $b2");
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
