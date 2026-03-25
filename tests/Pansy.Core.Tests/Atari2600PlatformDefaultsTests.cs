using Pansy.Core;
using Xunit;

namespace Pansy.Core.Tests;

/// <summary>
/// Comprehensive tests for Atari 2600 platform default symbols and regions.
/// Verifies all TIA write registers, TIA read registers, RIOT registers,
/// interrupt vectors, and memory regions.
/// </summary>
public class Atari2600PlatformDefaultsTests {
	private readonly Dictionary<uint, DefaultSymbol> _entries =
		PlatformDefaults.GetDefaultSymbolEntries(PansyLoader.PLATFORM_ATARI_2600);

	// ========================================================================
	// Total Count Verification
	// ========================================================================

	[Fact]
	public void TotalSymbolCount_Is72() {
		// 45 TIA write + 14 TIA read + 10 RIOT + 3 vectors = 72
		Assert.Equal(72, _entries.Count);
	}

	// ========================================================================
	// TIA Write Registers (0x00-0x2c) — 45 registers
	// ========================================================================

	[Theory]
	[InlineData(0x00u, "VSYNC", "Vertical Sync")]
	[InlineData(0x01u, "VBLANK", "Vertical Blank")]
	[InlineData(0x02u, "WSYNC", "Wait for Sync")]
	[InlineData(0x03u, "RSYNC", "Reset Sync")]
	[InlineData(0x04u, "NUSIZ0", "Number-Size Player 0")]
	[InlineData(0x05u, "NUSIZ1", "Number-Size Player 1")]
	[InlineData(0x06u, "COLUP0", "Color-Luminance Player 0")]
	[InlineData(0x07u, "COLUP1", "Color-Luminance Player 1")]
	[InlineData(0x08u, "COLUPF", "Color-Luminance Playfield")]
	[InlineData(0x09u, "COLUBK", "Color-Luminance Background")]
	[InlineData(0x0au, "CTRLPF", "Control Playfield")]
	[InlineData(0x0bu, "REFP0", "Reflect Player 0")]
	[InlineData(0x0cu, "REFP1", "Reflect Player 1")]
	[InlineData(0x0du, "PF0", "Playfield 0")]
	[InlineData(0x0eu, "PF1", "Playfield 1")]
	[InlineData(0x0fu, "PF2", "Playfield 2")]
	[InlineData(0x10u, "RESP0", "Reset Player 0")]
	[InlineData(0x11u, "RESP1", "Reset Player 1")]
	[InlineData(0x12u, "RESM0", "Reset Missile 0")]
	[InlineData(0x13u, "RESM1", "Reset Missile 1")]
	[InlineData(0x14u, "RESBL", "Reset Ball")]
	[InlineData(0x15u, "AUDC0", "Audio Control 0")]
	[InlineData(0x16u, "AUDC1", "Audio Control 1")]
	[InlineData(0x17u, "AUDF0", "Audio Frequency 0")]
	[InlineData(0x18u, "AUDF1", "Audio Frequency 1")]
	[InlineData(0x19u, "AUDV0", "Audio Volume 0")]
	[InlineData(0x1au, "AUDV1", "Audio Volume 1")]
	[InlineData(0x1bu, "GRP0", "Graphics Player 0")]
	[InlineData(0x1cu, "GRP1", "Graphics Player 1")]
	[InlineData(0x1du, "ENAM0", "Enable Missile 0")]
	[InlineData(0x1eu, "ENAM1", "Enable Missile 1")]
	[InlineData(0x1fu, "ENABL", "Enable Ball")]
	[InlineData(0x20u, "HMP0", "Horizontal Motion Player 0")]
	[InlineData(0x21u, "HMP1", "Horizontal Motion Player 1")]
	[InlineData(0x22u, "HMM0", "Horizontal Motion Missile 0")]
	[InlineData(0x23u, "HMM1", "Horizontal Motion Missile 1")]
	[InlineData(0x24u, "HMBL", "Horizontal Motion Ball")]
	[InlineData(0x25u, "VDELP0", "Vertical Delay Player 0")]
	[InlineData(0x26u, "VDELP1", "Vertical Delay Player 1")]
	[InlineData(0x27u, "VDELBL", "Vertical Delay Ball")]
	[InlineData(0x28u, "RESMP0", "Reset Missile 0 to Player 0")]
	[InlineData(0x29u, "RESMP1", "Reset Missile 1 to Player 1")]
	[InlineData(0x2au, "HMOVE", "Apply Horizontal Motion")]
	[InlineData(0x2bu, "HMCLR", "Clear Horizontal Move Registers")]
	[InlineData(0x2cu, "CXCLR", "Clear Collision Latches")]
	public void TiaWriteRegister_HasCorrectNameAndDescription(uint address, string name, string description) {
		Assert.True(_entries.ContainsKey(address),
			$"Missing TIA write register {name} at ${address:x2}");
		Assert.Equal(name, _entries[address].Name);
		Assert.Equal(description, _entries[address].Description);
		Assert.Equal(SymbolType.Constant, _entries[address].Type);
	}

	[Fact]
	public void TiaWriteRegisters_Count_Is45() {
		int count = _entries.Count(e => e.Key <= 0x2c);
		Assert.Equal(45, count);
	}

	// ========================================================================
	// TIA Read Registers (0x30-0x3d) — 14 registers (DASM mirror convention)
	// ========================================================================

	[Theory]
	[InlineData(0x30u, "CXM0P", "Collision M0-P1, M0-P0")]
	[InlineData(0x31u, "CXM1P", "Collision M1-P0, M1-P1")]
	[InlineData(0x32u, "CXP0FB", "Collision P0-PF, P0-BL")]
	[InlineData(0x33u, "CXP1FB", "Collision P1-PF, P1-BL")]
	[InlineData(0x34u, "CXM0FB", "Collision M0-PF, M0-BL")]
	[InlineData(0x35u, "CXM1FB", "Collision M1-PF, M1-BL")]
	[InlineData(0x36u, "CXBLPF", "Collision BL-PF")]
	[InlineData(0x37u, "CXPPMM", "Collision P0-P1, M0-M1")]
	[InlineData(0x38u, "INPT0", "Read Pot Port 0")]
	[InlineData(0x39u, "INPT1", "Read Pot Port 1")]
	[InlineData(0x3au, "INPT2", "Read Pot Port 2")]
	[InlineData(0x3bu, "INPT3", "Read Pot Port 3")]
	[InlineData(0x3cu, "INPT4", "Read Input (Trigger) 0")]
	[InlineData(0x3du, "INPT5", "Read Input (Trigger) 1")]
	public void TiaReadRegister_HasCorrectNameAndDescription(uint address, string name, string description) {
		Assert.True(_entries.ContainsKey(address),
			$"Missing TIA read register {name} at ${address:x2}");
		Assert.Equal(name, _entries[address].Name);
		Assert.Equal(description, _entries[address].Description);
		Assert.Equal(SymbolType.Constant, _entries[address].Type);
	}

	[Fact]
	public void TiaReadRegisters_Count_Is14() {
		int count = _entries.Count(e => e.Key >= 0x30 && e.Key <= 0x3d);
		Assert.Equal(14, count);
	}

	[Fact]
	public void TiaReadRegisters_CollisionRegisters_Are8() {
		int count = _entries.Count(e => e.Key >= 0x30 && e.Key <= 0x37);
		Assert.Equal(8, count);
	}

	[Fact]
	public void TiaReadRegisters_InputRegisters_Are6() {
		int count = _entries.Count(e => e.Key >= 0x38 && e.Key <= 0x3d);
		Assert.Equal(6, count);
	}

	// ========================================================================
	// RIOT Registers (0x0280-0x0297) — 10 registers
	// ========================================================================

	[Theory]
	[InlineData(0x0280u, "SWCHA", "Port A Data")]
	[InlineData(0x0281u, "SWACNT", "Port A Data Direction")]
	[InlineData(0x0282u, "SWCHB", "Port B Data (Console Switches)")]
	[InlineData(0x0283u, "SWBCNT", "Port B Data Direction")]
	[InlineData(0x0284u, "INTIM", "Timer Output")]
	[InlineData(0x0285u, "INSTAT", "Timer Status")]
	[InlineData(0x0294u, "TIM1T", "Timer 1 Clock")]
	[InlineData(0x0295u, "TIM8T", "Timer 8 Clock")]
	[InlineData(0x0296u, "TIM64T", "Timer 64 Clock")]
	[InlineData(0x0297u, "T1024T", "Timer 1024 Clock")]
	public void RiotRegister_HasCorrectNameAndDescription(uint address, string name, string description) {
		Assert.True(_entries.ContainsKey(address),
			$"Missing RIOT register {name} at ${address:x4}");
		Assert.Equal(name, _entries[address].Name);
		Assert.Equal(description, _entries[address].Description);
		Assert.Equal(SymbolType.Constant, _entries[address].Type);
	}

	[Fact]
	public void RiotRegisters_Count_Is10() {
		int count = _entries.Count(e => e.Key >= 0x0280 && e.Key <= 0x0297);
		Assert.Equal(10, count);
	}

	// ========================================================================
	// Interrupt Vectors (0xfffa-0xfffe) — 3 vectors
	// ========================================================================

	[Theory]
	[InlineData(0xfffau, "NMI_VECTOR", "NMI Vector")]
	[InlineData(0xfffcu, "RESET_VECTOR", "Reset Vector")]
	[InlineData(0xfffeu, "IRQ_VECTOR", "IRQ/BRK Vector")]
	public void InterruptVector_HasCorrectNameAndType(uint address, string name, string description) {
		Assert.True(_entries.ContainsKey(address),
			$"Missing interrupt vector {name} at ${address:x4}");
		Assert.Equal(name, _entries[address].Name);
		Assert.Equal(description, _entries[address].Description);
		Assert.Equal(SymbolType.InterruptVector, _entries[address].Type);
	}

	[Fact]
	public void InterruptVectors_Count_Is3() {
		int count = _entries.Count(e => e.Value.Type == SymbolType.InterruptVector);
		Assert.Equal(3, count);
	}

	// ========================================================================
	// All Symbols — General Validity
	// ========================================================================

	[Fact]
	public void AllSymbols_HaveNonEmptyNames() {
		foreach (var (address, symbol) in _entries) {
			Assert.False(string.IsNullOrWhiteSpace(symbol.Name),
				$"Symbol at ${address:x4} has empty name");
		}
	}

	[Fact]
	public void AllSymbols_HaveNonEmptyDescriptions() {
		foreach (var (address, symbol) in _entries) {
			Assert.False(string.IsNullOrWhiteSpace(symbol.Description),
				$"Symbol at ${address:x4} has empty description");
		}
	}

	[Fact]
	public void AllSymbols_AreConstantOrInterruptVector() {
		foreach (var (address, symbol) in _entries) {
			Assert.True(
				symbol.Type == SymbolType.Constant || symbol.Type == SymbolType.InterruptVector,
				$"Symbol {symbol.Name} at ${address:x4} has unexpected type {symbol.Type}");
		}
	}

	[Fact]
	public void AllSymbolNames_AreUnique() {
		var names = _entries.Values.Select(s => s.Name).ToList();
		var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
		Assert.Empty(duplicates);
	}

	// ========================================================================
	// Memory Regions
	// ========================================================================

	[Fact]
	public void Regions_Count_Is4() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_2600);
		Assert.Equal(4, regions.Length);
	}

	[Fact]
	public void Region_TIA_IsCorrect() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_2600);
		var tia = regions.First(r => r.Name == "TIA Registers");
		Assert.Equal(0x0000u, tia.Start);
		Assert.Equal(0x007fu, tia.End);
		Assert.Equal((byte)MemoryRegionType.IO, tia.Type);
	}

	[Fact]
	public void Region_RAM_IsCorrect() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_2600);
		var ram = regions.First(r => r.Name == "RAM");
		Assert.Equal(0x0080u, ram.Start);
		Assert.Equal(0x00ffu, ram.End);
		Assert.Equal((byte)MemoryRegionType.RAM, ram.Type);
	}

	[Fact]
	public void Region_RIOT_IsCorrect() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_2600);
		var riot = regions.First(r => r.Name == "RIOT Registers");
		Assert.Equal(0x0280u, riot.Start);
		Assert.Equal(0x0297u, riot.End);
		Assert.Equal((byte)MemoryRegionType.IO, riot.Type);
	}

	[Fact]
	public void Region_ROM_IsCorrect() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_2600);
		var rom = regions.First(r => r.Name == "ROM");
		Assert.Equal(0xf000u, rom.Start);
		Assert.Equal(0xffffu, rom.End);
		Assert.Equal((byte)MemoryRegionType.ROM, rom.Type);
	}

	[Fact]
	public void Regions_DoNotOverlap() {
		var regions = PlatformDefaults.GetDefaultRegions(PansyLoader.PLATFORM_ATARI_2600)
			.OrderBy(r => r.Start).ToArray();

		for (int i = 0; i < regions.Length - 1; i++) {
			Assert.True(regions[i].End < regions[i + 1].Start,
				$"Region {regions[i].Name} (${regions[i].End:x4}) overlaps with {regions[i + 1].Name} (${regions[i + 1].Start:x4})");
		}
	}

	// ========================================================================
	// Backward Compatibility — GetDefaultSymbols wrapper
	// ========================================================================

	[Fact]
	public void GetDefaultSymbols_MatchesEntries() {
		var symbols = PlatformDefaults.GetDefaultSymbols(PansyLoader.PLATFORM_ATARI_2600);
		Assert.Equal(_entries.Count, symbols.Count);

		foreach (var (address, symbol) in _entries) {
			Assert.True(symbols.ContainsKey(address),
				$"GetDefaultSymbols missing ${address:x4}");
			Assert.Equal(symbol.Name, symbols[address]);
		}
	}

	// ========================================================================
	// Bit Field Metadata Tests
	// ========================================================================

	[Fact]
	public void VBLANK_HasBitFields() {
		var symbol = _entries[0x01];
		Assert.NotNull(symbol.BitFields);
		Assert.Equal(3, symbol.BitFields.Length);
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 1 && bf.Width == 1 && bf.Name == "VBLANK");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 6 && bf.Width == 1 && bf.Name == "I4I5_LATCH");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 7 && bf.Width == 1 && bf.Name == "I0I3_DUMP");
	}

	[Fact]
	public void NUSIZ0_HasBitFields() {
		var symbol = _entries[0x04];
		Assert.NotNull(symbol.BitFields);
		Assert.Equal(2, symbol.BitFields.Length);
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 0 && bf.Width == 3 && bf.Name == "PLAYER_SIZE");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 4 && bf.Width == 2 && bf.Name == "MISSILE_SIZE");
	}

	[Fact]
	public void NUSIZ1_HasBitFields() {
		var symbol = _entries[0x05];
		Assert.NotNull(symbol.BitFields);
		Assert.Equal(2, symbol.BitFields.Length);
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 0 && bf.Width == 3 && bf.Name == "PLAYER_SIZE");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 4 && bf.Width == 2 && bf.Name == "MISSILE_SIZE");
	}

	[Fact]
	public void CTRLPF_HasBitFields() {
		var symbol = _entries[0x0a];
		Assert.NotNull(symbol.BitFields);
		Assert.Equal(4, symbol.BitFields.Length);
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 0 && bf.Width == 1 && bf.Name == "REFLECT");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 1 && bf.Width == 1 && bf.Name == "SCORE");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 2 && bf.Width == 1 && bf.Name == "PRIORITY");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 4 && bf.Width == 2 && bf.Name == "BALL_SIZE");
	}

	[Fact]
	public void PF0_HasBitFields() {
		var symbol = _entries[0x0d];
		Assert.NotNull(symbol.BitFields);
		Assert.Single(symbol.BitFields);
		Assert.Equal(4, symbol.BitFields[0].Bit);
		Assert.Equal(4, symbol.BitFields[0].Width);
	}

	[Fact]
	public void PF1_HasBitFields() {
		var symbol = _entries[0x0e];
		Assert.NotNull(symbol.BitFields);
		Assert.Single(symbol.BitFields);
		Assert.Equal(0, symbol.BitFields[0].Bit);
		Assert.Equal(8, symbol.BitFields[0].Width);
	}

	[Fact]
	public void PF2_HasBitFields() {
		var symbol = _entries[0x0f];
		Assert.NotNull(symbol.BitFields);
		Assert.Single(symbol.BitFields);
		Assert.Equal(0, symbol.BitFields[0].Bit);
		Assert.Equal(8, symbol.BitFields[0].Width);
	}

	[Fact]
	public void SWCHB_HasBitFields() {
		var symbol = _entries[0x0282];
		Assert.NotNull(symbol.BitFields);
		Assert.Equal(5, symbol.BitFields.Length);
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 0 && bf.Name == "RESET");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 1 && bf.Name == "SELECT");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 3 && bf.Name == "BW");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 6 && bf.Name == "P0_DIFF");
		Assert.Contains(symbol.BitFields, bf => bf.Bit == 7 && bf.Name == "P1_DIFF");
	}

	[Fact]
	public void RegistersWithoutBitFields_HaveNullBitFields() {
		// VSYNC has no bit fields defined
		Assert.Null(_entries[0x00].BitFields);
		// WSYNC has no bit fields
		Assert.Null(_entries[0x02].BitFields);
		// TIA read registers have no bit fields
		Assert.Null(_entries[0x30].BitFields);
		// RIOT SWCHA has no bit fields
		Assert.Null(_entries[0x0280].BitFields);
	}

	[Fact]
	public void AllBitFields_HaveNonEmptyNamesAndDescriptions() {
		foreach (var (address, symbol) in _entries) {
			if (symbol.BitFields is null) continue;
			foreach (var bf in symbol.BitFields) {
				Assert.False(string.IsNullOrWhiteSpace(bf.Name),
					$"BitField at ${address:x4} has empty name");
				Assert.False(string.IsNullOrWhiteSpace(bf.Description),
					$"BitField {bf.Name} at ${address:x4} has empty description");
				Assert.True(bf.Bit >= 0 && bf.Bit <= 7,
					$"BitField {bf.Name} at ${address:x4} has invalid bit position {bf.Bit}");
				Assert.True(bf.Width >= 1 && bf.Width <= 8,
					$"BitField {bf.Name} at ${address:x4} has invalid width {bf.Width}");
			}
		}
	}
}
