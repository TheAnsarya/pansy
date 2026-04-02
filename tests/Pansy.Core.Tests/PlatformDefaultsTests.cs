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
	[InlineData(PansyLoader.PLATFORM_GAMEGEAR)]
	[InlineData(PansyLoader.PLATFORM_NEOGEO)]
	[InlineData(PansyLoader.PLATFORM_C64)]
	[InlineData(PansyLoader.PLATFORM_ATARI_7800)]
	[InlineData(PansyLoader.PLATFORM_VECTREX)]
	[InlineData(PansyLoader.PLATFORM_VIRTUALBOY)]
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
	[InlineData(PansyLoader.PLATFORM_GAMEGEAR)]
	[InlineData(PansyLoader.PLATFORM_NEOGEO)]
	[InlineData(PansyLoader.PLATFORM_C64)]
	[InlineData(PansyLoader.PLATFORM_ATARI_7800)]
	[InlineData(PansyLoader.PLATFORM_VECTREX)]
	[InlineData(PansyLoader.PLATFORM_VIRTUALBOY)]
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
	[InlineData(PansyLoader.PLATFORM_GAMEGEAR)]
	[InlineData(PansyLoader.PLATFORM_NEOGEO)]
	[InlineData(PansyLoader.PLATFORM_C64)]
	[InlineData(PansyLoader.PLATFORM_ATARI_7800)]
	[InlineData(PansyLoader.PLATFORM_VECTREX)]
	[InlineData(PansyLoader.PLATFORM_VIRTUALBOY)]
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

	// ========================================================================
	// Game Gear Platform Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_GameGear_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_GAMEGEAR);

		Assert.NotNull(regions);
		Assert.Equal(4, regions.Length);
		Assert.Contains(regions, r => r.Name == "ROM" && r.Start == 0x0000 && r.End == 0xbfff);
		Assert.Contains(regions, r => r.Name == "RAM" && r.Start == 0xc000 && r.End == 0xdfff);
		Assert.Contains(regions, r => r.Name == "RAM Mirror" && r.Start == 0xe000 && r.End == 0xfffb);
		Assert.Contains(regions, r => r.Name == "Mapper Control" && r.Start == 0xfffc && r.End == 0xffff);
	}

	[Fact]
	public void GetDefaultSymbolEntries_GameGear_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_GAMEGEAR);

		Assert.Equal(16, entries.Count);
		// GG-specific ports
		Assert.True(entries.ContainsKey(0x0000), "GG: Missing GG_START_PORT at $0000");
		Assert.Equal("GG_START_PORT", entries[0x0000].Name);
		Assert.True(entries.ContainsKey(0x0005), "GG: Missing GG_STEREO at $0005");
		Assert.True(entries.ContainsKey(0x0006), "GG: Missing GG_SERIAL_CTRL at $0006");
		// Z80 vectors
		Assert.True(entries.ContainsKey(0x0038), "GG: Missing IRQ_HANDLER at $0038");
		Assert.Equal(SymbolType.InterruptVector, entries[0x0038].Type);
		Assert.True(entries.ContainsKey(0x0066), "GG: Missing NMI_HANDLER at $0066");
		Assert.Equal(SymbolType.InterruptVector, entries[0x0066].Type);
		// SMS-compatible ports
		Assert.True(entries.ContainsKey(0x00be), "GG: Missing VDP_DATA at $be");
		Assert.True(entries.ContainsKey(0x00dc), "GG: Missing JOY1 at $dc");
	}

	[Fact]
	public void GameGearRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_GAMEGEAR);

		var romRegion = regions.First(r => r.Name == "ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, romRegion.Type);

		var ramRegion = regions.First(r => r.Name == "RAM");
		Assert.Equal((byte)MemoryRegionType.RAM, ramRegion.Type);

		var mirrorRegion = regions.First(r => r.Name == "RAM Mirror");
		Assert.Equal((byte)MemoryRegionType.Mirror, mirrorRegion.Type);

		var mapperRegion = regions.First(r => r.Name == "Mapper Control");
		Assert.Equal((byte)MemoryRegionType.IO, mapperRegion.Type);
	}

	// ========================================================================
	// Neo Geo Platform Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_NeoGeo_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_NEOGEO);

		Assert.NotNull(regions);
		Assert.Equal(5, regions.Length);
		Assert.Contains(regions, r => r.Name == "P-ROM (Program)" && r.Start == 0x000000 && r.End == 0x0fffff);
		Assert.Contains(regions, r => r.Name == "Work RAM" && r.Start == 0x100000 && r.End == 0x10ffff);
		Assert.Contains(regions, r => r.Name == "P-ROM Bank" && r.Start == 0x200000 && r.End == 0x2fffff);
		Assert.Contains(regions, r => r.Name == "I/O Registers" && r.Start == 0x300000 && r.End == 0x31ffff);
		Assert.Contains(regions, r => r.Name == "Palette RAM" && r.Start == 0x400000 && r.End == 0x401fff);
	}

	[Fact]
	public void GetDefaultSymbolEntries_NeoGeo_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_NEOGEO);

		Assert.Equal(9, entries.Count);
		Assert.True(entries.ContainsKey(0x300000), "NeoGeo: Missing REG_P1CNT at $300000");
		Assert.Equal("REG_P1CNT", entries[0x300000].Name);
		Assert.True(entries.ContainsKey(0x320000), "NeoGeo: Missing REG_SOUND at $320000");
		Assert.True(entries.ContainsKey(0x380000), "NeoGeo: Missing REG_WATCHDOG at $380000");
		Assert.True(entries.ContainsKey(0x3c0000), "NeoGeo: Missing REG_VRAMADDR at $3c0000");

		// Vectors
		Assert.True(entries.ContainsKey(0x000000), "NeoGeo: Missing SSP at $000000");
		Assert.Equal(SymbolType.InterruptVector, entries[0x000000].Type);
		Assert.True(entries.ContainsKey(0x000004), "NeoGeo: Missing RESET_VECTOR at $000004");
		Assert.Equal(SymbolType.InterruptVector, entries[0x000004].Type);
	}

	[Fact]
	public void NeoGeoRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_NEOGEO);

		var promRegion = regions.First(r => r.Name == "P-ROM (Program)");
		Assert.Equal((byte)MemoryRegionType.ROM, promRegion.Type);

		var workRam = regions.First(r => r.Name == "Work RAM");
		Assert.Equal((byte)MemoryRegionType.WRAM, workRam.Type);

		var ioRegion = regions.First(r => r.Name == "I/O Registers");
		Assert.Equal((byte)MemoryRegionType.IO, ioRegion.Type);

		var paletteRam = regions.First(r => r.Name == "Palette RAM");
		Assert.Equal((byte)MemoryRegionType.RAM, paletteRam.Type);
	}

	// ========================================================================
	// Commodore 64 Platform Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_C64_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_C64);

		Assert.NotNull(regions);
		Assert.Equal(11, regions.Length);
		Assert.Contains(regions, r => r.Name == "Zero Page" && r.Start == 0x0000 && r.End == 0x00ff);
		Assert.Contains(regions, r => r.Name == "Stack" && r.Start == 0x0100 && r.End == 0x01ff);
		Assert.Contains(regions, r => r.Name == "BASIC/User RAM" && r.Start == 0x0200 && r.End == 0x9fff);
		Assert.Contains(regions, r => r.Name == "BASIC ROM" && r.Start == 0xa000 && r.End == 0xbfff);
		Assert.Contains(regions, r => r.Name == "VIC-II Registers" && r.Start == 0xd000 && r.End == 0xd3ff);
		Assert.Contains(regions, r => r.Name == "SID Registers" && r.Start == 0xd400 && r.End == 0xd7ff);
		Assert.Contains(regions, r => r.Name == "CIA 1" && r.Start == 0xdc00 && r.End == 0xdcff);
		Assert.Contains(regions, r => r.Name == "CIA 2" && r.Start == 0xdd00 && r.End == 0xddff);
		Assert.Contains(regions, r => r.Name == "Kernal ROM" && r.Start == 0xe000 && r.End == 0xffff);
	}

	[Fact]
	public void GetDefaultSymbolEntries_C64_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_C64);

		Assert.Equal(24, entries.Count);

		// VIC-II
		Assert.True(entries.ContainsKey(0xd011), "C64: Missing VIC_SCROLY at $d011");
		Assert.Equal("VIC_SCROLY", entries[0xd011].Name);
		Assert.True(entries.ContainsKey(0xd020), "C64: Missing VIC_EXTCOL at $d020");
		Assert.True(entries.ContainsKey(0xd021), "C64: Missing VIC_BGCOL0 at $d021");

		// SID
		Assert.True(entries.ContainsKey(0xd400), "C64: Missing SID_V1FREQLO at $d400");
		Assert.True(entries.ContainsKey(0xd418), "C64: Missing SID_SIGVOL at $d418");

		// CIA
		Assert.True(entries.ContainsKey(0xdc00), "C64: Missing CIA1_PRA at $dc00");
		Assert.True(entries.ContainsKey(0xdd00), "C64: Missing CIA2_PRA at $dd00");

		// Vectors
		Assert.True(entries.ContainsKey(0xfffa), "C64: Missing NMI_VECTOR");
		Assert.Equal(SymbolType.InterruptVector, entries[0xfffa].Type);
		Assert.True(entries.ContainsKey(0xfffc), "C64: Missing RESET_VECTOR");
		Assert.True(entries.ContainsKey(0xfffe), "C64: Missing IRQ_VECTOR");
	}

	[Fact]
	public void C64Regions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_C64);

		var zp = regions.First(r => r.Name == "Zero Page");
		Assert.Equal((byte)MemoryRegionType.RAM, zp.Type);

		var vicII = regions.First(r => r.Name == "VIC-II Registers");
		Assert.Equal((byte)MemoryRegionType.IO, vicII.Type);

		var sid = regions.First(r => r.Name == "SID Registers");
		Assert.Equal((byte)MemoryRegionType.IO, sid.Type);

		var cia1 = regions.First(r => r.Name == "CIA 1");
		Assert.Equal((byte)MemoryRegionType.IO, cia1.Type);

		var basicRom = regions.First(r => r.Name == "BASIC ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, basicRom.Type);

		var kernalRom = regions.First(r => r.Name == "Kernal ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, kernalRom.Type);
	}

	// ========================================================================
	// Atari 7800 Platform Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_Atari7800_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_7800);

		Assert.NotNull(regions);
		Assert.Equal(6, regions.Length);
		Assert.Contains(regions, r => r.Name == "TIA Registers" && r.Start == 0x0000 && r.End == 0x001f);
		Assert.Contains(regions, r => r.Name == "MARIA Registers" && r.Start == 0x0020 && r.End == 0x003f);
		Assert.Contains(regions, r => r.Name == "Zero Page RAM" && r.Start == 0x0040 && r.End == 0x00ff);
		Assert.Contains(regions, r => r.Name == "Stack" && r.Start == 0x0100 && r.End == 0x01ff);
		Assert.Contains(regions, r => r.Name == "RAM" && r.Start == 0x1800 && r.End == 0x27ff);
		Assert.Contains(regions, r => r.Name == "ROM" && r.Start == 0x4000 && r.End == 0xffff);
	}

	[Fact]
	public void GetDefaultSymbolEntries_Atari7800_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_ATARI_7800);

		Assert.Equal(9, entries.Count);

		// MARIA
		Assert.True(entries.ContainsKey(0x0020), "7800: Missing BACKGRND at $0020");
		Assert.Equal("BACKGRND", entries[0x0020].Name);
		Assert.True(entries.ContainsKey(0x003c), "7800: Missing CTRL at $003c");
		Assert.True(entries.ContainsKey(0x002c), "7800: Missing DPPH at $002c");
		Assert.True(entries.ContainsKey(0x0030), "7800: Missing DPPL at $0030");

		// Vectors
		Assert.True(entries.ContainsKey(0xfffa), "7800: Missing NMI_VECTOR");
		Assert.Equal(SymbolType.InterruptVector, entries[0xfffa].Type);
		Assert.True(entries.ContainsKey(0xfffc), "7800: Missing RESET_VECTOR");
		Assert.True(entries.ContainsKey(0xfffe), "7800: Missing IRQ_VECTOR");
	}

	[Fact]
	public void Atari7800Regions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_7800);

		var tia = regions.First(r => r.Name == "TIA Registers");
		Assert.Equal((byte)MemoryRegionType.IO, tia.Type);

		var maria = regions.First(r => r.Name == "MARIA Registers");
		Assert.Equal((byte)MemoryRegionType.IO, maria.Type);

		var zpRam = regions.First(r => r.Name == "Zero Page RAM");
		Assert.Equal((byte)MemoryRegionType.RAM, zpRam.Type);

		var rom = regions.First(r => r.Name == "ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, rom.Type);
	}

	// ========================================================================
	// Vectrex Platform Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_Vectrex_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_VECTREX);

		Assert.NotNull(regions);
		Assert.Equal(4, regions.Length);
		Assert.Contains(regions, r => r.Name == "Cart ROM" && r.Start == 0x0000 && r.End == 0x7fff);
		Assert.Contains(regions, r => r.Name == "RAM" && r.Start == 0xc800 && r.End == 0xcbff);
		Assert.Contains(regions, r => r.Name == "VIA 6522 Registers" && r.Start == 0xd000 && r.End == 0xd7ff);
		Assert.Contains(regions, r => r.Name == "System ROM" && r.Start == 0xe000 && r.End == 0xffff);
	}

	[Fact]
	public void GetDefaultSymbolEntries_Vectrex_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_VECTREX);

		Assert.Equal(15, entries.Count);

		// VIA 6522
		Assert.True(entries.ContainsKey(0xd000), "Vectrex: Missing VIA_PORTB at $d000");
		Assert.Equal("VIA_PORTB", entries[0xd000].Name);
		Assert.True(entries.ContainsKey(0xd001), "Vectrex: Missing VIA_PORTA at $d001");
		Assert.True(entries.ContainsKey(0xd004), "Vectrex: Missing VIA_T1CL at $d004");
		Assert.True(entries.ContainsKey(0xd00d), "Vectrex: Missing VIA_IFR at $d00d");
		Assert.True(entries.ContainsKey(0xd00e), "Vectrex: Missing VIA_IER at $d00e");

		// Vectors (6809 style)
		Assert.True(entries.ContainsKey(0xfff6), "Vectrex: Missing SWI_VECTOR at $fff6");
		Assert.Equal(SymbolType.InterruptVector, entries[0xfff6].Type);
		Assert.True(entries.ContainsKey(0xfff8), "Vectrex: Missing IRQ_VECTOR at $fff8");
		Assert.True(entries.ContainsKey(0xfffa), "Vectrex: Missing FIRQ_VECTOR at $fffa");
		Assert.True(entries.ContainsKey(0xfffc), "Vectrex: Missing NMI_VECTOR at $fffc");
		Assert.True(entries.ContainsKey(0xfffe), "Vectrex: Missing RESET_VECTOR at $fffe");
	}

	[Fact]
	public void VectrexRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_VECTREX);

		var cartRom = regions.First(r => r.Name == "Cart ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, cartRom.Type);

		var ram = regions.First(r => r.Name == "RAM");
		Assert.Equal((byte)MemoryRegionType.RAM, ram.Type);

		var via = regions.First(r => r.Name == "VIA 6522 Registers");
		Assert.Equal((byte)MemoryRegionType.IO, via.Type);

		var sysRom = regions.First(r => r.Name == "System ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, sysRom.Type);
	}

	// ========================================================================
	// Virtual Boy Platform Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_VirtualBoy_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_VIRTUALBOY);

		Assert.NotNull(regions);
		Assert.Equal(5, regions.Length);
		Assert.Contains(regions, r => r.Name == "VRAM" && r.Start == 0x00000000 && r.End == 0x00ffffff);
		Assert.Contains(regions, r => r.Name == "Hardware Registers" && r.Start == 0x02000000 && r.End == 0x0200ffff);
		Assert.Contains(regions, r => r.Name == "Work RAM" && r.Start == 0x05000000 && r.End == 0x0500ffff);
		Assert.Contains(regions, r => r.Name == "Cart RAM" && r.Start == 0x06000000 && r.End == 0x06003fff);
		Assert.Contains(regions, r => r.Name == "ROM" && r.Start == 0x07000000 && r.End == 0x07ffffff);
	}

	[Fact]
	public void GetDefaultSymbolEntries_VirtualBoy_HasKeyRegisters() {
		var entries = PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_VIRTUALBOY);

		Assert.Equal(10, entries.Count);

		Assert.True(entries.ContainsKey(0x02000000), "VB: Missing CCR at $02000000");
		Assert.Equal("CCR", entries[0x02000000].Name);
		Assert.True(entries.ContainsKey(0x02000010), "VB: Missing INTPND at $02000010");
		Assert.True(entries.ContainsKey(0x02000014), "VB: Missing INTENB at $02000014");
		Assert.True(entries.ContainsKey(0x02000020), "VB: Missing DPSTTS at $02000020");
		Assert.True(entries.ContainsKey(0x02000024), "VB: Missing DPCTRL at $02000024");
		Assert.True(entries.ContainsKey(0x02000028), "VB: Missing BRTA at $02000028");

		// Reset vector
		Assert.True(entries.ContainsKey(0xfffffff0), "VB: Missing RESET_VECTOR at $fffffff0");
		Assert.Equal(SymbolType.InterruptVector, entries[0xfffffff0].Type);
	}

	[Fact]
	public void VirtualBoyRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_VIRTUALBOY);

		var vram = regions.First(r => r.Name == "VRAM");
		Assert.Equal((byte)MemoryRegionType.VRAM, vram.Type);

		var hwRegs = regions.First(r => r.Name == "Hardware Registers");
		Assert.Equal((byte)MemoryRegionType.IO, hwRegs.Type);

		var workRam = regions.First(r => r.Name == "Work RAM");
		Assert.Equal((byte)MemoryRegionType.WRAM, workRam.Type);

		var cartRam = regions.First(r => r.Name == "Cart RAM");
		Assert.Equal((byte)MemoryRegionType.SRAM, cartRam.Type);

		var rom = regions.First(r => r.Name == "ROM");
		Assert.Equal((byte)MemoryRegionType.ROM, rom.Type);
	}

	// ========================================================================
	// GBA Region Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_GBA_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_GBA);

		Assert.NotNull(regions);
		Assert.Equal(9, regions.Length);
		Assert.Contains(regions, r => r.Name == "BIOS" && r.Start == 0x00000000 && r.End == 0x00003fff);
		Assert.Contains(regions, r => r.Name == "EWRAM" && r.Start == 0x02000000);
		Assert.Contains(regions, r => r.Name == "IWRAM" && r.Start == 0x03000000);
		Assert.Contains(regions, r => r.Name == "I/O Registers" && r.Start == 0x04000000);
		Assert.Contains(regions, r => r.Name == "VRAM" && r.Start == 0x06000000);
		Assert.Contains(regions, r => r.Name == "ROM" && r.Start == 0x08000000);
		Assert.Contains(regions, r => r.Name == "SRAM" && r.Start == 0x0e000000);
	}

	[Fact]
	public void GbaRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_GBA);

		Assert.Equal((byte)MemoryRegionType.ROM, regions.First(r => r.Name == "BIOS").Type);
		Assert.Equal((byte)MemoryRegionType.WRAM, regions.First(r => r.Name == "EWRAM").Type);
		Assert.Equal((byte)MemoryRegionType.WRAM, regions.First(r => r.Name == "IWRAM").Type);
		Assert.Equal((byte)MemoryRegionType.IO, regions.First(r => r.Name == "I/O Registers").Type);
		Assert.Equal((byte)MemoryRegionType.RAM, regions.First(r => r.Name == "Palette RAM").Type);
		Assert.Equal((byte)MemoryRegionType.VRAM, regions.First(r => r.Name == "VRAM").Type);
		Assert.Equal((byte)MemoryRegionType.RAM, regions.First(r => r.Name == "OAM").Type);
		Assert.Equal((byte)MemoryRegionType.ROM, regions.First(r => r.Name == "ROM").Type);
		Assert.Equal((byte)MemoryRegionType.SRAM, regions.First(r => r.Name == "SRAM").Type);
	}

	// ========================================================================
	// SMS Region Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_SMS_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_SMS);

		Assert.NotNull(regions);
		Assert.Equal(4, regions.Length);
		Assert.Contains(regions, r => r.Name == "ROM" && r.Start == 0x0000 && r.End == 0xbfff);
		Assert.Contains(regions, r => r.Name == "RAM" && r.Start == 0xc000 && r.End == 0xdfff);
		Assert.Contains(regions, r => r.Name == "RAM Mirror" && r.Start == 0xe000 && r.End == 0xfffb);
		Assert.Contains(regions, r => r.Name == "Mapper Control" && r.Start == 0xfffc && r.End == 0xffff);
	}

	[Fact]
	public void SmsRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_SMS);

		Assert.Equal((byte)MemoryRegionType.ROM, regions.First(r => r.Name == "ROM").Type);
		Assert.Equal((byte)MemoryRegionType.RAM, regions.First(r => r.Name == "RAM").Type);
		Assert.Equal((byte)MemoryRegionType.Mirror, regions.First(r => r.Name == "RAM Mirror").Type);
		Assert.Equal((byte)MemoryRegionType.IO, regions.First(r => r.Name == "Mapper Control").Type);
	}

	// ========================================================================
	// PCE Region Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_PCE_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_PCE);

		Assert.NotNull(regions);
		Assert.Equal(7, regions.Length);
		Assert.Contains(regions, r => r.Name == "Work RAM" && r.Start == 0x2000 && r.End == 0x3fff);
		Assert.Contains(regions, r => r.Name == "VDC Registers" && r.Start == 0x1fe000);
		Assert.Contains(regions, r => r.Name == "VCE Registers" && r.Start == 0x1fe400);
		Assert.Contains(regions, r => r.Name == "PSG Registers" && r.Start == 0x1fe800);
		Assert.Contains(regions, r => r.Name == "Timer" && r.Start == 0x1fec00);
		Assert.Contains(regions, r => r.Name == "Joypad" && r.Start == 0x1ff000);
		Assert.Contains(regions, r => r.Name == "IRQ Control" && r.Start == 0x1ff400);
	}

	[Fact]
	public void PceRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_PCE);

		Assert.Equal((byte)MemoryRegionType.WRAM, regions.First(r => r.Name == "Work RAM").Type);
		// All I/O sub-regions should be IO type
		foreach (var region in regions.Where(r => r.Name != "Work RAM")) {
			Assert.Equal((byte)MemoryRegionType.IO, region.Type);
		}
	}

	// ========================================================================
	// WonderSwan Region Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_WS_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_WONDERSWAN);

		Assert.NotNull(regions);
		Assert.Equal(3, regions.Length);
		Assert.Contains(regions, r => r.Name == "RAM" && r.Start == 0x00000 && r.End == 0x03fff);
		Assert.Contains(regions, r => r.Name == "Cartridge SRAM" && r.Start == 0x04000 && r.End == 0x0ffff);
		Assert.Contains(regions, r => r.Name == "ROM" && r.Start == 0x20000 && r.End == 0xfffff);
	}

	[Fact]
	public void WsRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_WONDERSWAN);

		Assert.Equal((byte)MemoryRegionType.RAM, regions.First(r => r.Name == "RAM").Type);
		Assert.Equal((byte)MemoryRegionType.SRAM, regions.First(r => r.Name == "Cartridge SRAM").Type);
		Assert.Equal((byte)MemoryRegionType.ROM, regions.First(r => r.Name == "ROM").Type);
	}

	// ========================================================================
	// Genesis Region Tests
	// ========================================================================

	[Fact]
	public void GetDefaultRegions_Genesis_ReturnsExpectedRegions() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_GENESIS);

		Assert.NotNull(regions);
		Assert.Equal(5, regions.Length);
		Assert.Contains(regions, r => r.Name == "ROM" && r.Start == 0x000000 && r.End == 0x3fffff);
		Assert.Contains(regions, r => r.Name == "Z80 Address Space" && r.Start == 0xa00000);
		Assert.Contains(regions, r => r.Name == "I/O Registers" && r.Start == 0xa10000);
		Assert.Contains(regions, r => r.Name == "VDP Registers" && r.Start == 0xc00000);
		Assert.Contains(regions, r => r.Name == "Work RAM" && r.Start == 0xff0000);
	}

	[Fact]
	public void GenesisRegions_HaveCorrectTypes() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_GENESIS);

		Assert.Equal((byte)MemoryRegionType.ROM, regions.First(r => r.Name == "ROM").Type);
		Assert.Equal((byte)MemoryRegionType.RAM, regions.First(r => r.Name == "Z80 Address Space").Type);
		Assert.Equal((byte)MemoryRegionType.IO, regions.First(r => r.Name == "I/O Registers").Type);
		Assert.Equal((byte)MemoryRegionType.IO, regions.First(r => r.Name == "VDP Registers").Type);
		Assert.Equal((byte)MemoryRegionType.WRAM, regions.First(r => r.Name == "Work RAM").Type);
	}
}
