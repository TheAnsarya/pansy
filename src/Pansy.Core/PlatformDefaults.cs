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
	/// Gets default symbols (name only) for the specified platform.
	/// For richer data including descriptions and types, use <see cref="GetDefaultSymbolEntries"/>.
	/// </summary>
	/// <param name="platformId">The platform ID.</param>
	/// <returns>Dictionary of address to symbol name.</returns>
	public static Dictionary<uint, string> GetDefaultSymbols(byte platformId) {
		var entries = GetDefaultSymbolEntries(platformId);
		var result = new Dictionary<uint, string>(entries.Count);
		foreach (var kvp in entries) {
			result[kvp.Key] = kvp.Value.Name;
		}
		return result;
	}

	/// <summary>
	/// Gets default symbols with full metadata for the specified platform.
	/// Includes register name, description, and symbol type.
	/// </summary>
	/// <param name="platformId">The platform ID.</param>
	/// <returns>Dictionary of address to DefaultSymbol with name, description, and type.</returns>
	public static Dictionary<uint, DefaultSymbol> GetDefaultSymbolEntries(byte platformId) {
		return platformId switch {
			PansyLoader.PLATFORM_NES => GetNesDefaultSymbolEntries(),
			PansyLoader.PLATFORM_SNES => GetSnesDefaultSymbolEntries(),
			PansyLoader.PLATFORM_GB => GetGbDefaultSymbolEntries(),
			PansyLoader.PLATFORM_GBA => GetGbaDefaultSymbolEntries(),
			PansyLoader.PLATFORM_PCE => GetPceDefaultSymbolEntries(),
			PansyLoader.PLATFORM_SMS => GetSmsDefaultSymbolEntries(),
			PansyLoader.PLATFORM_WONDERSWAN => GetWsDefaultSymbolEntries(),
			PansyLoader.PLATFORM_LYNX => GetLynxDefaultSymbolEntries(),
			PansyLoader.PLATFORM_ATARI_2600 => GetAtari2600DefaultSymbolEntries(),
			PansyLoader.PLATFORM_SPC700 => GetSpc700DefaultSymbolEntries(),
			_ => []
		};
	}

	// ========================================================================
	// NES Hardware Registers
	// ========================================================================

	/// <summary>
	/// Gets default symbols for NES hardware registers.
	/// Includes PPU ($2000-$2007), APU ($4000-$4017), and interrupt vectors.
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetNesDefaultSymbolEntries() => new() {
		// PPU Registers
		{ 0x2000, new("PPUCTRL", "PPU Control Register", SymbolType.Constant) },
		{ 0x2001, new("PPUMASK", "PPU Mask Register", SymbolType.Constant) },
		{ 0x2002, new("PPUSTATUS", "PPU Status Register", SymbolType.Constant) },
		{ 0x2003, new("OAMADDR", "OAM Address", SymbolType.Constant) },
		{ 0x2004, new("OAMDATA", "OAM Data Read/Write", SymbolType.Constant) },
		{ 0x2005, new("PPUSCROLL", "PPU Scroll Position", SymbolType.Constant) },
		{ 0x2006, new("PPUADDR", "PPU Address", SymbolType.Constant) },
		{ 0x2007, new("PPUDATA", "PPU Data Read/Write", SymbolType.Constant) },

		// APU Square Wave 1
		{ 0x4000, new("SQ1_VOL", "Square 1 Duty/Volume", SymbolType.Constant) },
		{ 0x4001, new("SQ1_SWEEP", "Square 1 Sweep", SymbolType.Constant) },
		{ 0x4002, new("SQ1_LO", "Square 1 Timer Low", SymbolType.Constant) },
		{ 0x4003, new("SQ1_HI", "Square 1 Length/Timer High", SymbolType.Constant) },

		// APU Square Wave 2
		{ 0x4004, new("SQ2_VOL", "Square 2 Duty/Volume", SymbolType.Constant) },
		{ 0x4005, new("SQ2_SWEEP", "Square 2 Sweep", SymbolType.Constant) },
		{ 0x4006, new("SQ2_LO", "Square 2 Timer Low", SymbolType.Constant) },
		{ 0x4007, new("SQ2_HI", "Square 2 Length/Timer High", SymbolType.Constant) },

		// APU Triangle
		{ 0x4008, new("TRI_LINEAR", "Triangle Linear Counter", SymbolType.Constant) },
		{ 0x400a, new("TRI_LO", "Triangle Timer Low", SymbolType.Constant) },
		{ 0x400b, new("TRI_HI", "Triangle Length/Timer High", SymbolType.Constant) },

		// APU Noise
		{ 0x400c, new("NOISE_VOL", "Noise Volume", SymbolType.Constant) },
		{ 0x400e, new("NOISE_LO", "Noise Period", SymbolType.Constant) },
		{ 0x400f, new("NOISE_HI", "Noise Length", SymbolType.Constant) },

		// APU DMC
		{ 0x4010, new("DMC_FREQ", "DMC Frequency", SymbolType.Constant) },
		{ 0x4011, new("DMC_RAW", "DMC Raw Counter", SymbolType.Constant) },
		{ 0x4012, new("DMC_START", "DMC Sample Address", SymbolType.Constant) },
		{ 0x4013, new("DMC_LEN", "DMC Sample Length", SymbolType.Constant) },

		// APU Control/Status
		{ 0x4014, new("OAMDMA", "OAM DMA", SymbolType.Constant) },
		{ 0x4015, new("SND_CHN", "Sound Channel Enable/Status", SymbolType.Constant) },

		// Controller/Frame Counter
		{ 0x4016, new("JOY1", "Joypad 1 / Strobe", SymbolType.Constant) },
		{ 0x4017, new("JOY2", "Joypad 2 / Frame Counter", SymbolType.Constant) },

		// Interrupt Vectors
		{ 0xfffa, new("NMI_VECTOR", "NMI Vector", SymbolType.InterruptVector) },
		{ 0xfffc, new("RESET_VECTOR", "Reset Vector", SymbolType.InterruptVector) },
		{ 0xfffe, new("IRQ_VECTOR", "IRQ/BRK Vector", SymbolType.InterruptVector) },
	};

	// ========================================================================
	// SNES Hardware Registers
	// ========================================================================

	/// <summary>
	/// Gets default symbols for SNES hardware registers.
	/// Includes PPU ($2100-$213F), APU I/O ($2140-$2143), WRAM ($2180-$2183),
	/// CPU ($4016-$421F), DMA ($4300-$437B), and SPC ($F0-$FF).
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetSnesDefaultSymbolEntries() {
		var symbols = new Dictionary<uint, DefaultSymbol> {
			// B-Bus PPU Registers
			{ 0x2100, new("INIDISP", "Screen Display Register", SymbolType.Constant) },
			{ 0x2101, new("OBSEL", "Object Size and Character Size", SymbolType.Constant) },
			{ 0x2102, new("OAMADDL", "OAM Address Low", SymbolType.Constant) },
			{ 0x2103, new("OAMADDH", "OAM Address High", SymbolType.Constant) },
			{ 0x2104, new("OAMDATA", "OAM Data Write", SymbolType.Constant) },
			{ 0x2105, new("BGMODE", "BG Mode and Character Size", SymbolType.Constant) },
			{ 0x2106, new("MOSAIC", "Mosaic Register", SymbolType.Constant) },
			{ 0x2107, new("BG1SC", "BG1 Tilemap Address", SymbolType.Constant) },
			{ 0x2108, new("BG2SC", "BG2 Tilemap Address", SymbolType.Constant) },
			{ 0x2109, new("BG3SC", "BG3 Tilemap Address", SymbolType.Constant) },
			{ 0x210a, new("BG4SC", "BG4 Tilemap Address", SymbolType.Constant) },
			{ 0x210b, new("BG12NBA", "BG1/2 Character Address", SymbolType.Constant) },
			{ 0x210c, new("BG34NBA", "BG3/4 Character Address", SymbolType.Constant) },
			{ 0x210d, new("BG1HOFS", "BG1 Horizontal Scroll", SymbolType.Constant) },
			{ 0x210e, new("BG1VOFS", "BG1 Vertical Scroll", SymbolType.Constant) },
			{ 0x210f, new("BG2HOFS", "BG2 Horizontal Scroll", SymbolType.Constant) },
			{ 0x2110, new("BG2VOFS", "BG2 Vertical Scroll", SymbolType.Constant) },
			{ 0x2111, new("BG3HOFS", "BG3 Horizontal Scroll", SymbolType.Constant) },
			{ 0x2112, new("BG3VOFS", "BG3 Vertical Scroll", SymbolType.Constant) },
			{ 0x2113, new("BG4HOFS", "BG4 Horizontal Scroll", SymbolType.Constant) },
			{ 0x2114, new("BG4VOFS", "BG4 Vertical Scroll", SymbolType.Constant) },
			{ 0x2115, new("VMAIN", "Video Port Control", SymbolType.Constant) },
			{ 0x2116, new("VMADDL", "VRAM Address Low", SymbolType.Constant) },
			{ 0x2117, new("VMADDH", "VRAM Address High", SymbolType.Constant) },
			{ 0x2118, new("VMDATAL", "VRAM Data Write Low", SymbolType.Constant) },
			{ 0x2119, new("VMDATAH", "VRAM Data Write High", SymbolType.Constant) },
			{ 0x211a, new("M7SEL", "Mode 7 Settings", SymbolType.Constant) },
			{ 0x211b, new("M7A", "Mode 7 Matrix A", SymbolType.Constant) },
			{ 0x211c, new("M7B", "Mode 7 Matrix B", SymbolType.Constant) },
			{ 0x211d, new("M7C", "Mode 7 Matrix C", SymbolType.Constant) },
			{ 0x211e, new("M7D", "Mode 7 Matrix D", SymbolType.Constant) },
			{ 0x211f, new("M7X", "Mode 7 Center X", SymbolType.Constant) },
			{ 0x2120, new("M7Y", "Mode 7 Center Y", SymbolType.Constant) },
			{ 0x2121, new("CGADD", "CGRAM Address", SymbolType.Constant) },
			{ 0x2122, new("CGDATA", "CGRAM Data Write", SymbolType.Constant) },
			{ 0x2123, new("W12SEL", "Window Mask Settings BG1/2", SymbolType.Constant) },
			{ 0x2124, new("W34SEL", "Window Mask Settings BG3/4", SymbolType.Constant) },
			{ 0x2125, new("WOBJSEL", "Window Mask Settings OBJ", SymbolType.Constant) },
			{ 0x2126, new("WH0", "Window 1 Left Position", SymbolType.Constant) },
			{ 0x2127, new("WH1", "Window 1 Right Position", SymbolType.Constant) },
			{ 0x2128, new("WH2", "Window 2 Left Position", SymbolType.Constant) },
			{ 0x2129, new("WH3", "Window 2 Right Position", SymbolType.Constant) },
			{ 0x212a, new("WBGLOG", "Window Mask Logic BG", SymbolType.Constant) },
			{ 0x212b, new("WOBJLOG", "Window Mask Logic OBJ", SymbolType.Constant) },
			{ 0x212c, new("TM", "Main Screen Designation", SymbolType.Constant) },
			{ 0x212d, new("TS", "Sub Screen Designation", SymbolType.Constant) },
			{ 0x212e, new("TMW", "Window Mask Main Screen", SymbolType.Constant) },
			{ 0x212f, new("TSW", "Window Mask Sub Screen", SymbolType.Constant) },
			{ 0x2130, new("CGWSEL", "Color Math Control A", SymbolType.Constant) },
			{ 0x2131, new("CGADSUB", "Color Math Control B", SymbolType.Constant) },
			{ 0x2132, new("COLDATA", "Fixed Color Data", SymbolType.Constant) },
			{ 0x2133, new("SETINI", "Screen Mode Select", SymbolType.Constant) },
			{ 0x2134, new("MPYL", "Multiplication Result Low", SymbolType.Constant) },
			{ 0x2135, new("MPYM", "Multiplication Result Mid", SymbolType.Constant) },
			{ 0x2136, new("MPYH", "Multiplication Result High", SymbolType.Constant) },
			{ 0x2137, new("SLHV", "Software Latch", SymbolType.Constant) },
			{ 0x2138, new("OAMDATAREAD", "OAM Data Read", SymbolType.Constant) },
			{ 0x2139, new("VMDATALREAD", "VRAM Data Read Low", SymbolType.Constant) },
			{ 0x213a, new("VMDATAHREAD", "VRAM Data Read High", SymbolType.Constant) },
			{ 0x213b, new("CGDATAREAD", "CGRAM Data Read", SymbolType.Constant) },
			{ 0x213c, new("OPHCT", "Horizontal Scanline Location", SymbolType.Constant) },
			{ 0x213d, new("OPVCT", "Vertical Scanline Location", SymbolType.Constant) },
			{ 0x213e, new("STAT77", "PPU1 Status", SymbolType.Constant) },
			{ 0x213f, new("STAT78", "PPU2 Status", SymbolType.Constant) },

			// APU I/O
			{ 0x2140, new("APUIO0", "APU I/O Port 0", SymbolType.Constant) },
			{ 0x2141, new("APUIO1", "APU I/O Port 1", SymbolType.Constant) },
			{ 0x2142, new("APUIO2", "APU I/O Port 2", SymbolType.Constant) },
			{ 0x2143, new("APUIO3", "APU I/O Port 3", SymbolType.Constant) },

			// WRAM Access
			{ 0x2180, new("WMDATA", "WRAM Data", SymbolType.Constant) },
			{ 0x2181, new("WMADDL", "WRAM Address Low", SymbolType.Constant) },
			{ 0x2182, new("WMADDM", "WRAM Address Mid", SymbolType.Constant) },
			{ 0x2183, new("WMADDH", "WRAM Address High", SymbolType.Constant) },

			// A-Bus CPU Registers
			{ 0x4016, new("JOYSER0", "Joypad Port 0", SymbolType.Constant) },
			{ 0x4017, new("JOYSER1", "Joypad Port 1", SymbolType.Constant) },
			{ 0x4200, new("NMITIMEN", "Interrupt Enable", SymbolType.Constant) },
			{ 0x4201, new("WRIO", "I/O Port Write", SymbolType.Constant) },
			{ 0x4202, new("WRMPYA", "Multiplicand A", SymbolType.Constant) },
			{ 0x4203, new("WRMPYB", "Multiplicand B", SymbolType.Constant) },
			{ 0x4204, new("WRDIVL", "Divisor Low", SymbolType.Constant) },
			{ 0x4205, new("WRDIVH", "Divisor High", SymbolType.Constant) },
			{ 0x4206, new("WRDIVB", "Dividend", SymbolType.Constant) },
			{ 0x4207, new("HTIMEL", "IRQ H-Timer Low", SymbolType.Constant) },
			{ 0x4208, new("HTIMEH", "IRQ H-Timer High", SymbolType.Constant) },
			{ 0x4209, new("VTIMEL", "IRQ V-Timer Low", SymbolType.Constant) },
			{ 0x420a, new("VTIMEH", "IRQ V-Timer High", SymbolType.Constant) },
			{ 0x420b, new("MDMAEN", "DMA Enable", SymbolType.Constant) },
			{ 0x420c, new("HDMAEN", "HDMA Enable", SymbolType.Constant) },
			{ 0x420d, new("MEMSEL", "ROM Speed", SymbolType.Constant) },
			{ 0x4210, new("RDNMI", "NMI Flag", SymbolType.Constant) },
			{ 0x4211, new("TIMEUP", "IRQ Flag", SymbolType.Constant) },
			{ 0x4212, new("HVBJOY", "PPU Status", SymbolType.Constant) },
			{ 0x4213, new("RDIO", "I/O Port Read", SymbolType.Constant) },
			{ 0x4214, new("RDDIVL", "Division Result Low", SymbolType.Constant) },
			{ 0x4215, new("RDDIVH", "Division Result High", SymbolType.Constant) },
			{ 0x4216, new("RDMPYL", "Multiply Result Low", SymbolType.Constant) },
			{ 0x4217, new("RDMPYH", "Multiply Result High", SymbolType.Constant) },
			{ 0x4218, new("JOY1L", "Joypad 1 Low", SymbolType.Constant) },
			{ 0x4219, new("JOY1H", "Joypad 1 High", SymbolType.Constant) },
			{ 0x421a, new("JOY2L", "Joypad 2 Low", SymbolType.Constant) },
			{ 0x421b, new("JOY2H", "Joypad 2 High", SymbolType.Constant) },
			{ 0x421c, new("JOY3L", "Joypad 3 Low", SymbolType.Constant) },
			{ 0x421d, new("JOY3H", "Joypad 3 High", SymbolType.Constant) },
			{ 0x421e, new("JOY4L", "Joypad 4 Low", SymbolType.Constant) },
			{ 0x421f, new("JOY4H", "Joypad 4 High", SymbolType.Constant) },
		};

		// DMA Registers (8 channels)
		for (uint ch = 0; ch < 8; ch++) {
			uint baseAddr = 0x4300 + (ch * 0x10);
			string c = ch.ToString();
			symbols[baseAddr + 0x00] = new("DMAP" + c, "DMA Control Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x01] = new("BBAD" + c, "DMA B-Bus Address Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x02] = new("A1T" + c + "L", "DMA A-Bus Address Low Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x03] = new("A1T" + c + "H", "DMA A-Bus Address High Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x04] = new("A1B" + c, "DMA A-Bus Bank Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x05] = new("DAS" + c + "L", "DMA Size Low Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x06] = new("DAS" + c + "H", "DMA Size High Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x07] = new("DAS" + c + "B", "HDMA Indirect Bank Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x08] = new("A2A" + c + "L", "HDMA Table Address Low Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x09] = new("A2A" + c + "H", "HDMA Table Address High Ch" + c, SymbolType.Constant);
			symbols[baseAddr + 0x0a] = new("NTLR" + c, "HDMA Line Counter Ch" + c, SymbolType.Constant);
		}

		return symbols;
	}

	// ========================================================================
	// Game Boy Hardware Registers
	// ========================================================================

	/// <summary>
	/// Gets default symbols for Game Boy hardware registers.
	/// Includes LCD ($FF40-$FF4B), APU ($FF10-$FF26), Timer, Serial, and Interrupts.
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetGbDefaultSymbolEntries() => new() {
		// Joypad
		{ 0xff00, new("JOYP", "Joypad", SymbolType.Constant) },

		// Serial
		{ 0xff01, new("SB", "Serial Data", SymbolType.Constant) },
		{ 0xff02, new("SC", "Serial Control", SymbolType.Constant) },

		// Timer
		{ 0xff04, new("DIV", "Divider", SymbolType.Constant) },
		{ 0xff05, new("TIMA", "Timer Counter", SymbolType.Constant) },
		{ 0xff06, new("TMA", "Timer Modulo", SymbolType.Constant) },
		{ 0xff07, new("TAC", "Timer Control", SymbolType.Constant) },

		// Interrupts
		{ 0xff0f, new("IF", "Interrupt Flag", SymbolType.Constant) },
		{ 0xffff, new("IE", "Interrupt Enable", SymbolType.Constant) },

		// APU Channel 1 (Square with Sweep)
		{ 0xff10, new("NR10", "Channel 1 Sweep", SymbolType.Constant) },
		{ 0xff11, new("NR11", "Channel 1 Length/Duty", SymbolType.Constant) },
		{ 0xff12, new("NR12", "Channel 1 Volume Envelope", SymbolType.Constant) },
		{ 0xff13, new("NR13", "Channel 1 Frequency Low", SymbolType.Constant) },
		{ 0xff14, new("NR14", "Channel 1 Frequency High", SymbolType.Constant) },

		// APU Channel 2 (Square)
		{ 0xff16, new("NR21", "Channel 2 Length/Duty", SymbolType.Constant) },
		{ 0xff17, new("NR22", "Channel 2 Volume Envelope", SymbolType.Constant) },
		{ 0xff18, new("NR23", "Channel 2 Frequency Low", SymbolType.Constant) },
		{ 0xff19, new("NR24", "Channel 2 Frequency High", SymbolType.Constant) },

		// APU Channel 3 (Wave)
		{ 0xff1a, new("NR30", "Channel 3 On/Off", SymbolType.Constant) },
		{ 0xff1b, new("NR31", "Channel 3 Length", SymbolType.Constant) },
		{ 0xff1c, new("NR32", "Channel 3 Output Level", SymbolType.Constant) },
		{ 0xff1d, new("NR33", "Channel 3 Frequency Low", SymbolType.Constant) },
		{ 0xff1e, new("NR34", "Channel 3 Frequency High", SymbolType.Constant) },

		// APU Channel 4 (Noise)
		{ 0xff20, new("NR41", "Channel 4 Length", SymbolType.Constant) },
		{ 0xff21, new("NR42", "Channel 4 Volume Envelope", SymbolType.Constant) },
		{ 0xff22, new("NR43", "Channel 4 Polynomial Counter", SymbolType.Constant) },
		{ 0xff23, new("NR44", "Channel 4 Counter/Consecutive", SymbolType.Constant) },

		// APU Control
		{ 0xff24, new("NR50", "Channel Volume", SymbolType.Constant) },
		{ 0xff25, new("NR51", "Sound Output Terminal", SymbolType.Constant) },
		{ 0xff26, new("NR52", "Sound On/Off", SymbolType.Constant) },

		// LCD
		{ 0xff40, new("LCDC", "LCD Control", SymbolType.Constant) },
		{ 0xff41, new("STAT", "LCD Status", SymbolType.Constant) },
		{ 0xff42, new("SCY", "Scroll Y", SymbolType.Constant) },
		{ 0xff43, new("SCX", "Scroll X", SymbolType.Constant) },
		{ 0xff44, new("LY", "LCD Y Coordinate", SymbolType.Constant) },
		{ 0xff45, new("LYC", "LY Compare", SymbolType.Constant) },
		{ 0xff46, new("DMA", "OAM DMA Start", SymbolType.Constant) },
		{ 0xff47, new("BGP", "BG Palette Data", SymbolType.Constant) },
		{ 0xff48, new("OBP0", "Object Palette 0", SymbolType.Constant) },
		{ 0xff49, new("OBP1", "Object Palette 1", SymbolType.Constant) },
		{ 0xff4a, new("WY", "Window Y Position", SymbolType.Constant) },
		{ 0xff4b, new("WX", "Window X Position", SymbolType.Constant) },
	};

	// ========================================================================
	// GBA Hardware Registers
	// ========================================================================

	/// <summary>
	/// Gets default symbols for GBA hardware registers.
	/// All at base address $04000000: Display, Sound, DMA, Timers, I/O, Interrupts.
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetGbaDefaultSymbolEntries() => new() {
		// Display Control
		{ 0x04000000, new("DISPCNT", "Display Control", SymbolType.Constant) },
		{ 0x04000002, new("GREENSWAP", "Green Swap", SymbolType.Constant) },
		{ 0x04000004, new("DISPSTAT", "Display Status", SymbolType.Constant) },
		{ 0x04000006, new("VCOUNT", "Vertical Counter", SymbolType.Constant) },

		// BG Control
		{ 0x04000008, new("BG0CNT", "BG0 Control", SymbolType.Constant) },
		{ 0x0400000a, new("BG1CNT", "BG1 Control", SymbolType.Constant) },
		{ 0x0400000c, new("BG2CNT", "BG2 Control", SymbolType.Constant) },
		{ 0x0400000e, new("BG3CNT", "BG3 Control", SymbolType.Constant) },

		// BG Scroll
		{ 0x04000010, new("BG0HOFS", "BG0 X Scroll", SymbolType.Constant) },
		{ 0x04000012, new("BG0VOFS", "BG0 Y Scroll", SymbolType.Constant) },
		{ 0x04000014, new("BG1HOFS", "BG1 X Scroll", SymbolType.Constant) },
		{ 0x04000016, new("BG1VOFS", "BG1 Y Scroll", SymbolType.Constant) },
		{ 0x04000018, new("BG2HOFS", "BG2 X Scroll", SymbolType.Constant) },
		{ 0x0400001a, new("BG2VOFS", "BG2 Y Scroll", SymbolType.Constant) },
		{ 0x0400001c, new("BG3HOFS", "BG3 X Scroll", SymbolType.Constant) },
		{ 0x0400001e, new("BG3VOFS", "BG3 Y Scroll", SymbolType.Constant) },

		// BG2/3 Affine Transform
		{ 0x04000020, new("BG2PA", "BG2 Transform A", SymbolType.Constant) },
		{ 0x04000022, new("BG2PB", "BG2 Transform B", SymbolType.Constant) },
		{ 0x04000024, new("BG2PC", "BG2 Transform C", SymbolType.Constant) },
		{ 0x04000026, new("BG2PD", "BG2 Transform D", SymbolType.Constant) },
		{ 0x04000028, new("BG2X", "BG2 Origin X", SymbolType.Constant) },
		{ 0x0400002c, new("BG2Y", "BG2 Origin Y", SymbolType.Constant) },
		{ 0x04000030, new("BG3PA", "BG3 Transform A", SymbolType.Constant) },
		{ 0x04000032, new("BG3PB", "BG3 Transform B", SymbolType.Constant) },
		{ 0x04000034, new("BG3PC", "BG3 Transform C", SymbolType.Constant) },
		{ 0x04000036, new("BG3PD", "BG3 Transform D", SymbolType.Constant) },
		{ 0x04000038, new("BG3X", "BG3 Origin X", SymbolType.Constant) },
		{ 0x0400003c, new("BG3Y", "BG3 Origin Y", SymbolType.Constant) },

		// Window
		{ 0x04000040, new("WIN0H", "Window 0 Horizontal", SymbolType.Constant) },
		{ 0x04000042, new("WIN1H", "Window 1 Horizontal", SymbolType.Constant) },
		{ 0x04000044, new("WIN0V", "Window 0 Vertical", SymbolType.Constant) },
		{ 0x04000046, new("WIN1V", "Window 1 Vertical", SymbolType.Constant) },
		{ 0x04000048, new("WININ", "Window Inside Config", SymbolType.Constant) },
		{ 0x0400004a, new("WINOUT", "Window Outside Config", SymbolType.Constant) },

		// Effects
		{ 0x0400004c, new("MOSAIC", "Mosaic Size", SymbolType.Constant) },
		{ 0x04000050, new("BLDCNT", "Blend Control", SymbolType.Constant) },
		{ 0x04000052, new("BLDALPHA", "Blend Coefficients", SymbolType.Constant) },
		{ 0x04000054, new("BLDY", "Brightness Coefficient", SymbolType.Constant) },

		// Sound
		{ 0x04000060, new("NR10", "Channel 1 Sweep", SymbolType.Constant) },
		{ 0x04000062, new("NR11", "Channel 1 Length/Duty", SymbolType.Constant) },
		{ 0x04000064, new("NR13", "Channel 1 Frequency", SymbolType.Constant) },
		{ 0x04000068, new("NR21", "Channel 2 Length/Duty", SymbolType.Constant) },
		{ 0x0400006c, new("NR23", "Channel 2 Frequency", SymbolType.Constant) },
		{ 0x04000070, new("NR30", "Channel 3 On/Off", SymbolType.Constant) },
		{ 0x04000072, new("NR31", "Channel 3 Length", SymbolType.Constant) },
		{ 0x04000074, new("NR33", "Channel 3 Frequency", SymbolType.Constant) },
		{ 0x04000078, new("NR41", "Channel 4 Length", SymbolType.Constant) },
		{ 0x0400007c, new("NR43", "Channel 4 Polynomial", SymbolType.Constant) },
		{ 0x04000080, new("NR50", "Channel Volume", SymbolType.Constant) },
		{ 0x04000082, new("SOUNDCNT_H", "Mixing Control", SymbolType.Constant) },
		{ 0x04000084, new("NR52", "Sound On/Off", SymbolType.Constant) },
		{ 0x04000088, new("SOUNDBIAS", "Sound Bias", SymbolType.Constant) },
		{ 0x04000090, new("WAVERAM", "Channel 3 Wave RAM", SymbolType.Constant) },
		{ 0x040000a0, new("FIFO_A", "Channel A FIFO", SymbolType.Constant) },
		{ 0x040000a4, new("FIFO_B", "Channel B FIFO", SymbolType.Constant) },

		// DMA
		{ 0x040000b0, new("DMA0SAD", "DMA 0 Source", SymbolType.Constant) },
		{ 0x040000b4, new("DMA0DAD", "DMA 0 Destination", SymbolType.Constant) },
		{ 0x040000b8, new("DMA0CNT_L", "DMA 0 Length", SymbolType.Constant) },
		{ 0x040000ba, new("DMA0CNT_H", "DMA 0 Control", SymbolType.Constant) },
		{ 0x040000bc, new("DMA1SAD", "DMA 1 Source", SymbolType.Constant) },
		{ 0x040000c0, new("DMA1DAD", "DMA 1 Destination", SymbolType.Constant) },
		{ 0x040000c4, new("DMA1CNT_L", "DMA 1 Length", SymbolType.Constant) },
		{ 0x040000c6, new("DMA1CNT_H", "DMA 1 Control", SymbolType.Constant) },
		{ 0x040000c8, new("DMA2SAD", "DMA 2 Source", SymbolType.Constant) },
		{ 0x040000cc, new("DMA2DAD", "DMA 2 Destination", SymbolType.Constant) },
		{ 0x040000d0, new("DMA2CNT_L", "DMA 2 Length", SymbolType.Constant) },
		{ 0x040000d2, new("DMA2CNT_H", "DMA 2 Control", SymbolType.Constant) },
		{ 0x040000d4, new("DMA3SAD", "DMA 3 Source", SymbolType.Constant) },
		{ 0x040000d8, new("DMA3DAD", "DMA 3 Destination", SymbolType.Constant) },
		{ 0x040000dc, new("DMA3CNT_L", "DMA 3 Length", SymbolType.Constant) },
		{ 0x040000de, new("DMA3CNT_H", "DMA 3 Control", SymbolType.Constant) },

		// Timers
		{ 0x04000100, new("TM0CNT_L", "Timer 0 Counter/Reload", SymbolType.Constant) },
		{ 0x04000102, new("TM0CNT_H", "Timer 0 Control", SymbolType.Constant) },
		{ 0x04000104, new("TM1CNT_L", "Timer 1 Counter/Reload", SymbolType.Constant) },
		{ 0x04000106, new("TM1CNT_H", "Timer 1 Control", SymbolType.Constant) },
		{ 0x04000108, new("TM2CNT_L", "Timer 2 Counter/Reload", SymbolType.Constant) },
		{ 0x0400010a, new("TM2CNT_H", "Timer 2 Control", SymbolType.Constant) },
		{ 0x0400010c, new("TM3CNT_L", "Timer 3 Counter/Reload", SymbolType.Constant) },
		{ 0x0400010e, new("TM3CNT_H", "Timer 3 Control", SymbolType.Constant) },

		// Serial I/O
		{ 0x04000120, new("SIODATA32", "Serial Data 32-bit", SymbolType.Constant) },
		{ 0x04000128, new("SIOCNT", "Serial Control", SymbolType.Constant) },
		{ 0x0400012a, new("SIODATA8", "Serial Data 8-bit", SymbolType.Constant) },

		// Key Input
		{ 0x04000130, new("KEYINPUT", "Key Status", SymbolType.Constant) },
		{ 0x04000132, new("KEYCNT", "Key IRQ Control", SymbolType.Constant) },

		// Serial Mode Select
		{ 0x04000134, new("RCNT", "Serial Mode Select", SymbolType.Constant) },
		{ 0x04000140, new("JOYCNT", "JOY Bus Control", SymbolType.Constant) },
		{ 0x04000150, new("JOYRECV", "JOY Bus Receive", SymbolType.Constant) },
		{ 0x04000154, new("JOYSEND", "JOY Bus Send", SymbolType.Constant) },
		{ 0x04000158, new("JOYSTAT", "JOY Bus Status", SymbolType.Constant) },

		// Interrupts/System
		{ 0x04000200, new("IE", "IRQ Enable", SymbolType.Constant) },
		{ 0x04000202, new("IF", "IRQ Flags", SymbolType.Constant) },
		{ 0x04000204, new("WAITCNT", "Waitstate Control", SymbolType.Constant) },
		{ 0x04000208, new("IME", "IRQ Master Enable", SymbolType.Constant) },
		{ 0x04000300, new("POSTFLG", "Post Boot Flag", SymbolType.Constant) },
		{ 0x04000301, new("HALTCNT", "Halt Control", SymbolType.Constant) },
	};

	// ========================================================================
	// PC Engine (TurboGrafx-16) Hardware Registers
	// ========================================================================

	/// <summary>
	/// Gets default symbols for PC Engine hardware registers.
	/// Includes VDC ($000-$003), VCE ($400-$405), PSG ($800-$809),
	/// Timer ($C00-$C01), Joypad ($1000), IRQ ($1402-$1403).
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetPceDefaultSymbolEntries() => new() {
		// VDC
		{ 0x0000, new("VDC_AR", "VDC Address/Status Register", SymbolType.Constant) },
		{ 0x0002, new("VDC_DATA_LO", "VDC Data Low", SymbolType.Constant) },
		{ 0x0003, new("VDC_DATA_HI", "VDC Data High", SymbolType.Constant) },

		// VCE
		{ 0x0400, new("VCE_CONTROL", "VCE Control", SymbolType.Constant) },
		{ 0x0402, new("VCE_ADDR_LO", "VCE Color Address Low", SymbolType.Constant) },
		{ 0x0403, new("VCE_ADDR_HI", "VCE Color Address High", SymbolType.Constant) },
		{ 0x0404, new("VCE_DATA_LO", "VCE Color Data Low", SymbolType.Constant) },
		{ 0x0405, new("VCE_DATA_HI", "VCE Color Data High", SymbolType.Constant) },

		// PSG
		{ 0x0800, new("PSG_CHANSELECT", "PSG Channel Select", SymbolType.Constant) },
		{ 0x0801, new("PSG_GLOBALVOL", "PSG Global Volume", SymbolType.Constant) },
		{ 0x0802, new("PSG_FREQLO", "PSG Frequency Low", SymbolType.Constant) },
		{ 0x0803, new("PSG_FREQHI", "PSG Frequency High", SymbolType.Constant) },
		{ 0x0804, new("PSG_CHANCTRL", "PSG Channel Control", SymbolType.Constant) },
		{ 0x0805, new("PSG_CHANPAN", "PSG Channel Pan", SymbolType.Constant) },
		{ 0x0806, new("PSG_CHANDATA", "PSG Channel Data", SymbolType.Constant) },
		{ 0x0807, new("PSG_NOISE", "PSG Noise Control", SymbolType.Constant) },
		{ 0x0808, new("PSG_LFOFREQ", "PSG LFO Frequency", SymbolType.Constant) },
		{ 0x0809, new("PSG_LFOCONTROL", "PSG LFO Control", SymbolType.Constant) },

		// Timer
		{ 0x0c00, new("TIMER_COUNTER", "Timer Counter/Latch", SymbolType.Constant) },
		{ 0x0c01, new("TIMER_CONTROL", "Timer Control", SymbolType.Constant) },

		// Joypad
		{ 0x1000, new("JOYPAD", "Joypad I/O", SymbolType.Constant) },

		// IRQ
		{ 0x1402, new("IRQ_DISABLE", "IRQ Disable", SymbolType.Constant) },
		{ 0x1403, new("IRQ_STATUS", "IRQ Status/Acknowledge", SymbolType.Constant) },
	};

	// ========================================================================
	// Master System Hardware Registers (Port-Mapped)
	// ========================================================================

	/// <summary>
	/// Gets default symbols for Sega Master System hardware registers.
	/// Port-mapped I/O registers.
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetSmsDefaultSymbolEntries() => new() {
		{ 0x3e, new("MEMORY_ENABLE", "Memory Enable", SymbolType.Constant) },
		{ 0x3f, new("IO_PORT", "I/O Port Control", SymbolType.Constant) },
		{ 0x7e, new("VDP_V_COUNTER", "VDP V Counter", SymbolType.Constant) },
		{ 0x7f, new("PSG", "PSG Audio", SymbolType.Constant) },
		{ 0xbe, new("VDP_DATA", "VDP Data Port", SymbolType.Constant) },
		{ 0xbf, new("VDP_CMD_STATUS", "VDP Command/Status", SymbolType.Constant) },
		{ 0xdc, new("JOY1", "Joypad 1", SymbolType.Constant) },
		{ 0xdd, new("JOY2", "Joypad 2", SymbolType.Constant) },
	};

	// ========================================================================
	// WonderSwan Hardware Registers (Port-Mapped)
	// ========================================================================

	/// <summary>
	/// Gets default symbols for WonderSwan hardware registers.
	/// Port-mapped I/O: Display, Palettes, DMA, Sound, System, IRQ.
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetWsDefaultSymbolEntries() => new() {
		// Display
		{ 0x00, new("DISPLAY_CTRL", "Display Control", SymbolType.Constant) },
		{ 0x01, new("DISPLAY_BACK", "Background Color", SymbolType.Constant) },
		{ 0x02, new("LCD_LINE", "LCD Current Line", SymbolType.Constant) },
		{ 0x03, new("LCD_INTERRUPT", "LCD Interrupt Line", SymbolType.Constant) },
		{ 0x04, new("SPR_BASE", "Sprite Base Address", SymbolType.Constant) },
		{ 0x05, new("SPR_FIRST", "First Sprite", SymbolType.Constant) },
		{ 0x06, new("SPR_COUNT", "Sprite Count", SymbolType.Constant) },
		{ 0x07, new("SCR_BASE", "Screen Base Address", SymbolType.Constant) },
		{ 0x08, new("SCR2_WIN_X1", "Screen 2 Window X1", SymbolType.Constant) },
		{ 0x09, new("SCR2_WIN_Y1", "Screen 2 Window Y1", SymbolType.Constant) },
		{ 0x0a, new("SCR2_WIN_X2", "Screen 2 Window X2", SymbolType.Constant) },
		{ 0x0b, new("SCR2_WIN_Y2", "Screen 2 Window Y2", SymbolType.Constant) },
		{ 0x0c, new("SPR_WIN_X1", "Sprite Window X1", SymbolType.Constant) },
		{ 0x0d, new("SPR_WIN_Y1", "Sprite Window Y1", SymbolType.Constant) },
		{ 0x0e, new("SPR_WIN_X2", "Sprite Window X2", SymbolType.Constant) },
		{ 0x0f, new("SPR_WIN_Y2", "Sprite Window Y2", SymbolType.Constant) },
		{ 0x10, new("SCR1_SCRL_X", "Screen 1 Scroll X", SymbolType.Constant) },
		{ 0x11, new("SCR1_SCRL_Y", "Screen 1 Scroll Y", SymbolType.Constant) },
		{ 0x12, new("SCR2_SCRL_X", "Screen 2 Scroll X", SymbolType.Constant) },
		{ 0x13, new("SCR2_SCRL_Y", "Screen 2 Scroll Y", SymbolType.Constant) },
		{ 0x14, new("LCD_CTRL", "LCD Control", SymbolType.Constant) },
		{ 0x15, new("LCD_SEG", "LCD Segment", SymbolType.Constant) },
		{ 0x16, new("LCD_VTOTAL", "LCD V Total", SymbolType.Constant) },
		{ 0x17, new("LCD_VSYNC", "LCD V Sync", SymbolType.Constant) },
		{ 0x1a, new("LCD_STATUS", "LCD Status", SymbolType.Constant) },
		{ 0x1c, new("LCD_SHADE_01", "LCD Shade 0/1", SymbolType.Constant) },
		{ 0x1d, new("LCD_SHADE_23", "LCD Shade 2/3", SymbolType.Constant) },
		{ 0x1e, new("LCD_SHADE_45", "LCD Shade 4/5", SymbolType.Constant) },
		{ 0x1f, new("LCD_SHADE_67", "LCD Shade 6/7", SymbolType.Constant) },

		// Palettes
		{ 0x20, new("PAL_0", "Palette 0", SymbolType.Constant) },
		{ 0x22, new("PAL_1", "Palette 1", SymbolType.Constant) },
		{ 0x24, new("PAL_2", "Palette 2", SymbolType.Constant) },
		{ 0x26, new("PAL_3", "Palette 3", SymbolType.Constant) },
		{ 0x28, new("PAL_4", "Palette 4", SymbolType.Constant) },
		{ 0x2a, new("PAL_5", "Palette 5", SymbolType.Constant) },
		{ 0x2c, new("PAL_6", "Palette 6", SymbolType.Constant) },
		{ 0x2e, new("PAL_7", "Palette 7", SymbolType.Constant) },
		{ 0x30, new("PAL_8", "Palette 8", SymbolType.Constant) },
		{ 0x32, new("PAL_9", "Palette 9", SymbolType.Constant) },
		{ 0x34, new("PAL_10", "Palette 10", SymbolType.Constant) },
		{ 0x36, new("PAL_11", "Palette 11", SymbolType.Constant) },
		{ 0x38, new("PAL_12", "Palette 12", SymbolType.Constant) },
		{ 0x3a, new("PAL_13", "Palette 13", SymbolType.Constant) },
		{ 0x3c, new("PAL_14", "Palette 14", SymbolType.Constant) },
		{ 0x3e, new("PAL_15", "Palette 15", SymbolType.Constant) },

		// DMA
		{ 0x40, new("DMA_SOURCE_L", "DMA Source Low", SymbolType.Constant) },
		{ 0x42, new("DMA_SOURCE_H", "DMA Source High", SymbolType.Constant) },
		{ 0x44, new("DMA_DEST", "DMA Destination", SymbolType.Constant) },
		{ 0x46, new("DMA_LENGTH", "DMA Length", SymbolType.Constant) },
		{ 0x48, new("DMA_CTRL", "DMA Control", SymbolType.Constant) },
		{ 0x4a, new("SDMA_SOURCE_L", "Sound DMA Source Low", SymbolType.Constant) },
		{ 0x4c, new("SDMA_SOURCE_H", "Sound DMA Source High", SymbolType.Constant) },
		{ 0x4e, new("SDMA_LENGTH_L", "Sound DMA Length Low", SymbolType.Constant) },
		{ 0x50, new("SDMA_LENGTH_H", "Sound DMA Length High", SymbolType.Constant) },
		{ 0x52, new("SDMA_CTRL", "Sound DMA Control", SymbolType.Constant) },

		// System
		{ 0x60, new("SYSTEM_CTRL2", "System Control 2", SymbolType.Constant) },
		{ 0x62, new("SYSTEM_CTRL3", "System Control 3", SymbolType.Constant) },

		// HyperVoice
		{ 0x64, new("HYPERV_OUT_L", "HyperVoice Out Left", SymbolType.Constant) },
		{ 0x66, new("HYPERV_OUT_R", "HyperVoice Out Right", SymbolType.Constant) },
		{ 0x68, new("HYPERV_IN_L", "HyperVoice In Left", SymbolType.Constant) },
		{ 0x69, new("HYPERV_IN_R", "HyperVoice In Right", SymbolType.Constant) },
		{ 0x6a, new("HYPERV_CTRL", "HyperVoice Control", SymbolType.Constant) },

		// Sound
		{ 0x80, new("SND_FREQ_CH1", "Sound Frequency Ch1", SymbolType.Constant) },
		{ 0x82, new("SND_FREQ_CH2", "Sound Frequency Ch2", SymbolType.Constant) },
		{ 0x84, new("SND_FREQ_CH3", "Sound Frequency Ch3", SymbolType.Constant) },
		{ 0x86, new("SND_FREQ_CH4", "Sound Frequency Ch4", SymbolType.Constant) },
		{ 0x88, new("SND_VOL_CH1", "Sound Volume Ch1", SymbolType.Constant) },
		{ 0x89, new("SND_VOL_CH2", "Sound Volume Ch2", SymbolType.Constant) },
		{ 0x8a, new("SND_VOL_CH3", "Sound Volume Ch3", SymbolType.Constant) },
		{ 0x8b, new("SND_VOL_CH4", "Sound Volume Ch4", SymbolType.Constant) },
		{ 0x8c, new("SND_SWEEP", "Sound Sweep Value", SymbolType.Constant) },
		{ 0x8d, new("SND_SWEEP_TIME", "Sound Sweep Time", SymbolType.Constant) },
		{ 0x8e, new("SND_NOISE_CTRL", "Sound Noise Control", SymbolType.Constant) },
		{ 0x8f, new("SND_WAVE_BASE", "Sound Wave Base", SymbolType.Constant) },
		{ 0x90, new("SND_CH_CTRL", "Sound Channel Control", SymbolType.Constant) },
		{ 0x91, new("SND_OUT_CTRL", "Sound Output Control", SymbolType.Constant) },
		{ 0x92, new("SND_RANDOM", "Sound Random", SymbolType.Constant) },
		{ 0x9e, new("SND_HW_VOL", "Sound Hardware Volume", SymbolType.Constant) },

		// System Control
		{ 0xa0, new("SYSTEM_CTRL1", "System Control 1", SymbolType.Constant) },
		{ 0xa2, new("TIMER_CTRL", "Timer Control", SymbolType.Constant) },
		{ 0xa4, new("HBLANK_TIMER", "HBlank Timer", SymbolType.Constant) },
		{ 0xa6, new("VBLANK_TIMER", "VBlank Timer", SymbolType.Constant) },
		{ 0xa8, new("HBLANK_COUNTER", "HBlank Counter", SymbolType.Constant) },
		{ 0xaa, new("VBLANK_COUNTER", "VBlank Counter", SymbolType.Constant) },

		// Interrupts
		{ 0xb0, new("HWINT_VECTOR", "HW Interrupt Vector", SymbolType.Constant) },
		{ 0xb1, new("SERIAL_DATA", "Serial Data", SymbolType.Constant) },
		{ 0xb2, new("HWINT_ENABLE", "HW Interrupt Enable", SymbolType.Constant) },
		{ 0xb3, new("SERIAL_STATUS", "Serial Status", SymbolType.Constant) },
		{ 0xb4, new("HWINT_STATUS", "HW Interrupt Status", SymbolType.Constant) },
		{ 0xb5, new("KEY_SCAN", "Key Scan", SymbolType.Constant) },
		{ 0xb6, new("HWINT_ACK", "HW Interrupt Acknowledge", SymbolType.Constant) },
		{ 0xb7, new("INT_NMI_CTRL", "NMI Control", SymbolType.Constant) },

		// EEPROM/Bank
		{ 0xba, new("IEEP_DATA", "Internal EEPROM Data", SymbolType.Constant) },
		{ 0xbc, new("IEEP_CMD", "Internal EEPROM Command", SymbolType.Constant) },
		{ 0xbe, new("IEEP_CTRL", "Internal EEPROM Control", SymbolType.Constant) },
		{ 0xc1, new("BANK_RAM", "Bank Control RAM", SymbolType.Constant) },
		{ 0xc2, new("BANK_ROM0", "Bank Control ROM0", SymbolType.Constant) },
		{ 0xc3, new("BANK_ROM1", "Bank Control ROM1", SymbolType.Constant) },
	};

	// ========================================================================
	// Atari Lynx Hardware Registers
	// ========================================================================

	/// <summary>
	/// Gets default symbols for Atari Lynx hardware registers.
	/// Includes Suzy GPU ($FC00-$FCFF) and Mikey ($FD00-$FDFF) registers.
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetLynxDefaultSymbolEntries() => new() {
		// Suzy Sprite/GPU Registers
		{ 0xfc00, new("TMPADR", "Suzy Temporary Address", SymbolType.Constant) },
		{ 0xfc04, new("TILTACUM", "Suzy Tilt Accumulator", SymbolType.Constant) },
		{ 0xfc08, new("HOFF", "Suzy H Offset", SymbolType.Constant) },
		{ 0xfc0a, new("VOFF", "Suzy V Offset", SymbolType.Constant) },
		{ 0xfc10, new("SCBNEXT", "Suzy SCB Next Address", SymbolType.Constant) },
		{ 0xfc14, new("SPRDLINE", "Suzy Sprite Data Line", SymbolType.Constant) },
		{ 0xfc18, new("HPOSSTRT", "Suzy H Position Start", SymbolType.Constant) },
		{ 0xfc1a, new("VPOSSTRT", "Suzy V Position Start", SymbolType.Constant) },
		{ 0xfc1c, new("SPRHSIZ", "Suzy Sprite H Size", SymbolType.Constant) },
		{ 0xfc1e, new("SPRVSIZ", "Suzy Sprite V Size", SymbolType.Constant) },
		{ 0xfc28, new("COLLOFF", "Suzy Collision Offset", SymbolType.Constant) },
		{ 0xfc2c, new("VSIZACUM", "Suzy V Size Accumulator", SymbolType.Constant) },
		{ 0xfc40, new("HSIZOFF", "Suzy H Size Offset", SymbolType.Constant) },
		{ 0xfc42, new("VSIZOFF", "Suzy V Size Offset", SymbolType.Constant) },

		// Suzy Math
		{ 0xfc52, new("MATHD", "Suzy Math D", SymbolType.Constant) },
		{ 0xfc54, new("MATHC", "Suzy Math C", SymbolType.Constant) },
		{ 0xfc56, new("MATHB", "Suzy Math B", SymbolType.Constant) },
		{ 0xfc58, new("MATHA", "Suzy Math A", SymbolType.Constant) },
		{ 0xfc60, new("MATHP", "Suzy Math P", SymbolType.Constant) },
		{ 0xfc6c, new("MATHM", "Suzy Math M", SymbolType.Constant) },

		// Suzy Sprite Control
		{ 0xfc80, new("COLLBUF", "Suzy Collision Buffer", SymbolType.Constant) },
		{ 0xfc90, new("SPRCTL0", "Suzy Sprite Control 0", SymbolType.Constant) },
		{ 0xfc91, new("SPRCTL1", "Suzy Sprite Control 1", SymbolType.Constant) },
		{ 0xfc92, new("SPRCOLL", "Suzy Sprite Collision Number", SymbolType.Constant) },
		{ 0xfc93, new("SPRINIT", "Suzy Sprite Initialize", SymbolType.Constant) },
		{ 0xfca0, new("SUZYMATH", "Suzy Math Control", SymbolType.Constant) },
		{ 0xfcb0, new("SUZYHREV", "Suzy Hardware Revision", SymbolType.Constant) },
		{ 0xfcb2, new("SUZYSREV", "Suzy Software Revision", SymbolType.Constant) },

		// Suzy Input
		{ 0xfcc0, new("JOYSTICK", "Suzy Joystick Port", SymbolType.Constant) },
		{ 0xfcc2, new("SWITCHES", "Suzy Switch Port", SymbolType.Constant) },

		// Mikey Timers
		{ 0xfd00, new("TIM0BKUP", "Mikey Timer 0 Backup", SymbolType.Constant) },
		{ 0xfd01, new("TIM0CTLA", "Mikey Timer 0 Control A", SymbolType.Constant) },
		{ 0xfd02, new("TIM0CNT", "Mikey Timer 0 Count", SymbolType.Constant) },
		{ 0xfd03, new("TIM0CTLB", "Mikey Timer 0 Control B", SymbolType.Constant) },

		// Mikey Audio
		{ 0xfd20, new("AUD0VOL", "Mikey Audio Ch0 Volume", SymbolType.Constant) },
		{ 0xfd28, new("AUD1VOL", "Mikey Audio Ch1 Volume", SymbolType.Constant) },
		{ 0xfd30, new("AUD2VOL", "Mikey Audio Ch2 Volume", SymbolType.Constant) },
		{ 0xfd38, new("AUD3VOL", "Mikey Audio Ch3 Volume", SymbolType.Constant) },
		{ 0xfd50, new("MSTEREO", "Mikey Stereo Control", SymbolType.Constant) },

		// Mikey Display/IRQ/Serial
		{ 0xfd80, new("INTRST", "Mikey Interrupt Reset", SymbolType.Constant) },
		{ 0xfd81, new("INTSET", "Mikey Interrupt Set", SymbolType.Constant) },
		{ 0xfd84, new("MAGRDY0", "Mikey Magazine Port 0", SymbolType.Constant) },
		{ 0xfd85, new("MAGRDY1", "Mikey Magazine Port 1", SymbolType.Constant) },
		{ 0xfd87, new("SYSCTL1", "Mikey System Control 1", SymbolType.Constant) },
		{ 0xfd88, new("MIKYHREV", "Mikey Hardware Revision", SymbolType.Constant) },
		{ 0xfd8b, new("MIKYREV", "Mikey Software Revision", SymbolType.Constant) },
		{ 0xfd8c, new("SERDAT", "Mikey Serial Data", SymbolType.Constant) },
		{ 0xfd8d, new("SERCTL", "Mikey Serial Control", SymbolType.Constant) },
		{ 0xfd92, new("DISPCTL", "Mikey Display Control", SymbolType.Constant) },
		{ 0xfd94, new("PBKUP", "Mikey Audio Period Backup", SymbolType.Constant) },
		{ 0xfd9c, new("DISPADRL", "Mikey Display Address Low", SymbolType.Constant) },
		{ 0xfd9d, new("DISPADRH", "Mikey Display Address High", SymbolType.Constant) },

		// Mikey Palette
		{ 0xfda0, new("PALETTE", "Mikey Palette Start", SymbolType.Constant) },

		// Memory Map Control
		{ 0xfff9, new("MAPCTL", "Memory Map Control", SymbolType.Constant) },

		// Vectors
		{ 0xfffa, new("NMI_VECTOR", "NMI Vector", SymbolType.InterruptVector) },
		{ 0xfffc, new("RESET_VECTOR", "Reset Vector", SymbolType.InterruptVector) },
		{ 0xfffe, new("IRQ_VECTOR", "IRQ Vector", SymbolType.InterruptVector) },
	};

	// ========================================================================
	// Atari 2600 Hardware Registers
	// ========================================================================

	/// <summary>
	/// Gets default symbols for Atari 2600 hardware registers.
	/// Includes TIA write ($00-$2C), TIA read ($00-$0D), RIOT ($280-$297), and vectors.
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetAtari2600DefaultSymbolEntries() => new() {
		// TIA Write Registers
		{ 0x00, new("VSYNC", "Vertical Sync", SymbolType.Constant) },
		{ 0x01, new("VBLANK", "Vertical Blank", SymbolType.Constant) },
		{ 0x02, new("WSYNC", "Wait for Sync", SymbolType.Constant) },
		{ 0x03, new("RSYNC", "Reset Sync", SymbolType.Constant) },
		{ 0x04, new("NUSIZ0", "Number-Size Player 0", SymbolType.Constant) },
		{ 0x05, new("NUSIZ1", "Number-Size Player 1", SymbolType.Constant) },
		{ 0x06, new("COLUP0", "Color-Luminance Player 0", SymbolType.Constant) },
		{ 0x07, new("COLUP1", "Color-Luminance Player 1", SymbolType.Constant) },
		{ 0x08, new("COLUPF", "Color-Luminance Playfield", SymbolType.Constant) },
		{ 0x09, new("COLUBK", "Color-Luminance Background", SymbolType.Constant) },
		{ 0x0a, new("CTRLPF", "Control Playfield", SymbolType.Constant) },
		{ 0x0b, new("REFP0", "Reflect Player 0", SymbolType.Constant) },
		{ 0x0c, new("REFP1", "Reflect Player 1", SymbolType.Constant) },
		{ 0x0d, new("PF0", "Playfield 0", SymbolType.Constant) },
		{ 0x0e, new("PF1", "Playfield 1", SymbolType.Constant) },
		{ 0x0f, new("PF2", "Playfield 2", SymbolType.Constant) },
		{ 0x10, new("RESP0", "Reset Player 0", SymbolType.Constant) },
		{ 0x11, new("RESP1", "Reset Player 1", SymbolType.Constant) },
		{ 0x12, new("RESM0", "Reset Missile 0", SymbolType.Constant) },
		{ 0x13, new("RESM1", "Reset Missile 1", SymbolType.Constant) },
		{ 0x14, new("RESBL", "Reset Ball", SymbolType.Constant) },
		{ 0x15, new("AUDC0", "Audio Control 0", SymbolType.Constant) },
		{ 0x16, new("AUDC1", "Audio Control 1", SymbolType.Constant) },
		{ 0x17, new("AUDF0", "Audio Frequency 0", SymbolType.Constant) },
		{ 0x18, new("AUDF1", "Audio Frequency 1", SymbolType.Constant) },
		{ 0x19, new("AUDV0", "Audio Volume 0", SymbolType.Constant) },
		{ 0x1a, new("AUDV1", "Audio Volume 1", SymbolType.Constant) },
		{ 0x1b, new("GRP0", "Graphics Player 0", SymbolType.Constant) },
		{ 0x1c, new("GRP1", "Graphics Player 1", SymbolType.Constant) },
		{ 0x1d, new("ENAM0", "Enable Missile 0", SymbolType.Constant) },
		{ 0x1e, new("ENAM1", "Enable Missile 1", SymbolType.Constant) },
		{ 0x1f, new("ENABL", "Enable Ball", SymbolType.Constant) },
		{ 0x20, new("HMP0", "Horizontal Motion Player 0", SymbolType.Constant) },
		{ 0x21, new("HMP1", "Horizontal Motion Player 1", SymbolType.Constant) },
		{ 0x22, new("HMM0", "Horizontal Motion Missile 0", SymbolType.Constant) },
		{ 0x23, new("HMM1", "Horizontal Motion Missile 1", SymbolType.Constant) },
		{ 0x24, new("HMBL", "Horizontal Motion Ball", SymbolType.Constant) },
		{ 0x25, new("VDELP0", "Vertical Delay Player 0", SymbolType.Constant) },
		{ 0x26, new("VDELP1", "Vertical Delay Player 1", SymbolType.Constant) },
		{ 0x27, new("VDELBL", "Vertical Delay Ball", SymbolType.Constant) },
		{ 0x28, new("RESMP0", "Reset Missile 0 to Player 0", SymbolType.Constant) },
		{ 0x29, new("RESMP1", "Reset Missile 1 to Player 1", SymbolType.Constant) },
		{ 0x2a, new("HMOVE", "Apply Horizontal Motion", SymbolType.Constant) },
		{ 0x2b, new("HMCLR", "Clear Horizontal Move Registers", SymbolType.Constant) },
		{ 0x2c, new("CXCLR", "Clear Collision Latches", SymbolType.Constant) },

		// TIA Read Registers (Collision & Input)
		// Read registers share addresses 0x00-0x0d with write registers; the TIA
		// distinguishes read vs write internally.  Following the DASM vcs.h convention,
		// we place them at mirror addresses 0x30-0x3d (A5=1, A4=1) to avoid symbol
		// collisions in the dictionary.
		{ 0x30, new("CXM0P", "Collision M0-P1, M0-P0", SymbolType.Constant) },
		{ 0x31, new("CXM1P", "Collision M1-P0, M1-P1", SymbolType.Constant) },
		{ 0x32, new("CXP0FB", "Collision P0-PF, P0-BL", SymbolType.Constant) },
		{ 0x33, new("CXP1FB", "Collision P1-PF, P1-BL", SymbolType.Constant) },
		{ 0x34, new("CXM0FB", "Collision M0-PF, M0-BL", SymbolType.Constant) },
		{ 0x35, new("CXM1FB", "Collision M1-PF, M1-BL", SymbolType.Constant) },
		{ 0x36, new("CXBLPF", "Collision BL-PF", SymbolType.Constant) },
		{ 0x37, new("CXPPMM", "Collision P0-P1, M0-M1", SymbolType.Constant) },
		{ 0x38, new("INPT0", "Read Pot Port 0", SymbolType.Constant) },
		{ 0x39, new("INPT1", "Read Pot Port 1", SymbolType.Constant) },
		{ 0x3a, new("INPT2", "Read Pot Port 2", SymbolType.Constant) },
		{ 0x3b, new("INPT3", "Read Pot Port 3", SymbolType.Constant) },
		{ 0x3c, new("INPT4", "Read Input (Trigger) 0", SymbolType.Constant) },
		{ 0x3d, new("INPT5", "Read Input (Trigger) 1", SymbolType.Constant) },

		// RIOT Registers
		{ 0x0280, new("SWCHA", "Port A Data", SymbolType.Constant) },
		{ 0x0281, new("SWACNT", "Port A Data Direction", SymbolType.Constant) },
		{ 0x0282, new("SWCHB", "Port B Data (Console Switches)", SymbolType.Constant) },
		{ 0x0283, new("SWBCNT", "Port B Data Direction", SymbolType.Constant) },
		{ 0x0284, new("INTIM", "Timer Output", SymbolType.Constant) },
		{ 0x0285, new("INSTAT", "Timer Status", SymbolType.Constant) },
		{ 0x0294, new("TIM1T", "Timer 1 Clock", SymbolType.Constant) },
		{ 0x0295, new("TIM8T", "Timer 8 Clock", SymbolType.Constant) },
		{ 0x0296, new("TIM64T", "Timer 64 Clock", SymbolType.Constant) },
		{ 0x0297, new("T1024T", "Timer 1024 Clock", SymbolType.Constant) },

		// Vectors
		{ 0xfffa, new("NMI_VECTOR", "NMI Vector", SymbolType.InterruptVector) },
		{ 0xfffc, new("RESET_VECTOR", "Reset Vector", SymbolType.InterruptVector) },
		{ 0xfffe, new("IRQ_VECTOR", "IRQ/BRK Vector", SymbolType.InterruptVector) },
	};

	// ========================================================================
	// SPC700 Audio Processor Registers (SNES APU)
	// ========================================================================

	/// <summary>
	/// Gets default symbols for SPC700 audio processor registers.
	/// These are in SPC RAM space ($F0-$FF).
	/// </summary>
	public static Dictionary<uint, DefaultSymbol> GetSpc700DefaultSymbolEntries() => new() {
		{ 0xf0, new("TEST", "Testing Functions", SymbolType.Constant) },
		{ 0xf1, new("CONTROL", "I/O and Timer Control", SymbolType.Constant) },
		{ 0xf2, new("DSPADDR", "DSP Address", SymbolType.Constant) },
		{ 0xf3, new("DSPDATA", "DSP Data", SymbolType.Constant) },
		{ 0xf4, new("CPUIO0", "CPU I/O 0", SymbolType.Constant) },
		{ 0xf5, new("CPUIO1", "CPU I/O 1", SymbolType.Constant) },
		{ 0xf6, new("CPUIO2", "CPU I/O 2", SymbolType.Constant) },
		{ 0xf7, new("CPUIO3", "CPU I/O 3", SymbolType.Constant) },
		{ 0xf8, new("RAMREG1", "Memory Register 1", SymbolType.Constant) },
		{ 0xf9, new("RAMREG2", "Memory Register 2", SymbolType.Constant) },
		{ 0xfa, new("T0TARGET", "Timer 0 Target", SymbolType.Constant) },
		{ 0xfb, new("T1TARGET", "Timer 1 Target", SymbolType.Constant) },
		{ 0xfc, new("T2TARGET", "Timer 2 Target", SymbolType.Constant) },
		{ 0xfd, new("T0OUT", "Timer 0 Output", SymbolType.Constant) },
		{ 0xfe, new("T1OUT", "Timer 1 Output", SymbolType.Constant) },
		{ 0xff, new("T2OUT", "Timer 2 Output", SymbolType.Constant) },
	};
}

