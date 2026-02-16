// ============================================================================
// PlatformDefaults.cs - Platform-Specific Default Metadata
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

namespace Pansy.Core;

/// <summary>
/// Provides platform-specific default memory regions and symbols.
/// </summary>
public static class PlatformDefaults {
	/// <summary>
	/// Gets default memory regions for the specified platform.
	/// </summary>
	/// <param name="platformId">The platform ID.</param>
	/// <returns>Array of default memory regions.</returns>
	public static MemoryRegion[] GetDefaultRegions(byte platformId) {
		return platformId switch {
			PansyLoader.PLATFORM_NES => GetNesDefaultRegions(),
			PansyLoader.PLATFORM_SNES => GetSnesDefaultRegions(),
			PansyLoader.PLATFORM_GB => GetGbDefaultRegions(),
			PansyLoader.PLATFORM_LYNX => GetLynxDefaultRegions(),
			PansyLoader.PLATFORM_ATARI_2600 => GetAtari2600DefaultRegions(),
			_ => []
		};
	}

	/// <summary>
	/// Gets default memory regions for Atari Lynx.
	/// </summary>
	public static MemoryRegion[] GetLynxDefaultRegions() => [
		new MemoryRegion(0x0000, 0x00ff, (byte)MemoryRegionType.RAM, 0, "Zero Page"),
		new MemoryRegion(0x0100, 0x01ff, (byte)MemoryRegionType.RAM, 0, "Stack"),
		new MemoryRegion(0x0200, 0xfbff, (byte)MemoryRegionType.WRAM, 0, "Work RAM"),
		new MemoryRegion(0xfc00, 0xfcff, (byte)MemoryRegionType.IO, 0, "Suzy Registers"),
		new MemoryRegion(0xfd00, 0xfdff, (byte)MemoryRegionType.IO, 0, "Mikey Registers"),
		new MemoryRegion(0xfe00, 0xffff, (byte)MemoryRegionType.ROM, 0, "Boot ROM"),
	];

	/// <summary>
	/// Gets default memory regions for NES.
	/// </summary>
	public static MemoryRegion[] GetNesDefaultRegions() => [
		new MemoryRegion(0x0000, 0x00ff, (byte)MemoryRegionType.RAM, 0, "Zero Page"),
		new MemoryRegion(0x0100, 0x01ff, (byte)MemoryRegionType.RAM, 0, "Stack"),
		new MemoryRegion(0x0200, 0x07ff, (byte)MemoryRegionType.RAM, 0, "RAM"),
		new MemoryRegion(0x2000, 0x2007, (byte)MemoryRegionType.IO, 0, "PPU Registers"),
		new MemoryRegion(0x4000, 0x4017, (byte)MemoryRegionType.IO, 0, "APU/IO Registers"),
		new MemoryRegion(0x6000, 0x7fff, (byte)MemoryRegionType.SRAM, 0, "Save RAM"),
		new MemoryRegion(0x8000, 0xffff, (byte)MemoryRegionType.ROM, 0, "PRG ROM"),
	];

	/// <summary>
	/// Gets default memory regions for SNES (LoROM).
	/// </summary>
	public static MemoryRegion[] GetSnesDefaultRegions() => [
		new MemoryRegion(0x0000, 0x1fff, (byte)MemoryRegionType.RAM, 0, "Low RAM"),
		new MemoryRegion(0x2100, 0x21ff, (byte)MemoryRegionType.IO, 0, "PPU Registers"),
		new MemoryRegion(0x4200, 0x43ff, (byte)MemoryRegionType.IO, 0, "CPU Registers"),
		new MemoryRegion(0x7e0000, 0x7fffff, (byte)MemoryRegionType.WRAM, 0, "Work RAM"),
	];

	/// <summary>
	/// Gets default memory regions for Game Boy.
	/// </summary>
	public static MemoryRegion[] GetGbDefaultRegions() => [
		new MemoryRegion(0x0000, 0x3fff, (byte)MemoryRegionType.ROM, 0, "ROM Bank 0"),
		new MemoryRegion(0x4000, 0x7fff, (byte)MemoryRegionType.ROM, 1, "ROM Bank N"),
		new MemoryRegion(0x8000, 0x9fff, (byte)MemoryRegionType.VRAM, 0, "Video RAM"),
		new MemoryRegion(0xa000, 0xbfff, (byte)MemoryRegionType.SRAM, 0, "External RAM"),
		new MemoryRegion(0xc000, 0xdfff, (byte)MemoryRegionType.WRAM, 0, "Work RAM"),
		new MemoryRegion(0xff00, 0xff7f, (byte)MemoryRegionType.IO, 0, "I/O Registers"),
		new MemoryRegion(0xff80, 0xfffe, (byte)MemoryRegionType.RAM, 0, "High RAM"),
	];

	/// <summary>
	/// Gets default memory regions for Atari 2600.
	/// </summary>
	public static MemoryRegion[] GetAtari2600DefaultRegions() => [
		new MemoryRegion(0x0000, 0x007f, (byte)MemoryRegionType.IO, 0, "TIA Registers"),
		new MemoryRegion(0x0080, 0x00ff, (byte)MemoryRegionType.RAM, 0, "RAM"),
		new MemoryRegion(0x0280, 0x0297, (byte)MemoryRegionType.IO, 0, "RIOT Registers"),
		new MemoryRegion(0xf000, 0xffff, (byte)MemoryRegionType.ROM, 0, "ROM"),
	];

	/// <summary>
	/// Gets default symbols for the specified platform.
	/// </summary>
	/// <param name="platformId">The platform ID.</param>
	/// <returns>Dictionary of address to symbol name.</returns>
	public static Dictionary<uint, string> GetDefaultSymbols(byte platformId) {
		return platformId switch {
			PansyLoader.PLATFORM_LYNX => GetLynxDefaultSymbols(),
			_ => []
		};
	}

	/// <summary>
	/// Gets default symbols for Atari Lynx hardware registers.
	/// </summary>
	public static Dictionary<uint, string> GetLynxDefaultSymbols() => new() {
		// Suzy Sprite Registers
		{ 0xfc80, "SPRCTL0" },
		{ 0xfc81, "SPRCTL1" },
		{ 0xfc82, "SPRCOLL" },
		{ 0xfc83, "SPRINIT" },
		{ 0xfc90, "SUZYBUSEN" },
		{ 0xfc91, "SPRGO" },
		{ 0xfc92, "SPRSYS" },

		// Suzy Input
		{ 0xfcb0, "JOYSTICK" },
		{ 0xfcb1, "SWITCHES" },

		// Suzy Math
		{ 0xfc52, "MATHD" },
		{ 0xfc53, "MATHC" },
		{ 0xfc54, "MATHB" },
		{ 0xfc55, "MATHA" },
		{ 0xfc6e, "MATHK" },
		{ 0xfc6c, "MATHM" },

		// Mikey Timers
		{ 0xfd00, "TIM0BKUP" },
		{ 0xfd01, "TIM0CTLA" },
		{ 0xfd02, "TIM0CNT" },
		{ 0xfd03, "TIM0CTLB" },
		{ 0xfd08, "TIM2BKUP" },
		{ 0xfd09, "TIM2CTLA" },
		{ 0xfd0a, "TIM2CNT" },
		{ 0xfd0b, "TIM2CTLB" },

		// Mikey Interrupts
		{ 0xfd80, "INTRST" },
		{ 0xfd81, "INTSET" },

		// Mikey I/O
		{ 0xfd8a, "IODIR" },
		{ 0xfd8b, "IODAT" },
		{ 0xfd8c, "SERCTL" },
		{ 0xfd8d, "SERDAT" },

		// Mikey Display
		{ 0xfd92, "DISPCTL" },
		{ 0xfd94, "DISPADRL" },
		{ 0xfd95, "DISPADRH" },

		// Vectors
		{ 0xfffa, "NMI_VECTOR" },
		{ 0xfffc, "RESET_VECTOR" },
		{ 0xfffe, "IRQ_VECTOR" },
	};
}

