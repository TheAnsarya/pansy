using System.Buffers.Binary;
using System.Collections.Frozen;
using System.IO.Compression;

namespace Pansy.Core;

/// <summary>
/// Loads Pansy (Program ANalysis SYstem) files for comprehensive metadata import.
/// Pansy files contain code/data maps, symbols, comments, cross-references, and more,
/// providing complete roundtrip support between Poppy (assembler) and Peony (disassembler).
/// </summary>
public sealed class PansyLoader {
	private readonly byte[] _data;
	private readonly ushort _version;
	private readonly PansyFlags _flags;
	private readonly byte _platform;
	private readonly uint _romSize;
	private readonly uint _romCrc32;
	private readonly List<SectionInfo> _sections = [];

	// Parsed data (frozen after construction for optimal lookup)
	private byte[]? _codeDataMap;
	private FrozenSet<int> _codeOffsets = FrozenSet<int>.Empty;
	private FrozenSet<int> _dataOffsets = FrozenSet<int>.Empty;
	private FrozenSet<int> _jumpTargets = FrozenSet<int>.Empty;
	private FrozenSet<int> _subEntryPoints = FrozenSet<int>.Empty;
	private FrozenSet<int> _opcodeOffsets = FrozenSet<int>.Empty;
	private FrozenSet<int> _drawnOffsets = FrozenSet<int>.Empty;
	private FrozenSet<int> _readOffsets = FrozenSet<int>.Empty;
	private FrozenSet<int> _indirectOffsets = FrozenSet<int>.Empty;
	private FrozenDictionary<int, IReadOnlyList<SymbolEntry>> _symbolEntries = FrozenDictionary<int, IReadOnlyList<SymbolEntry>>.Empty;
	private FrozenDictionary<int, IReadOnlyList<CommentEntry>> _commentEntries = FrozenDictionary<int, IReadOnlyList<CommentEntry>>.Empty;
	private readonly List<MemoryRegion> _memoryRegions = [];
	private readonly List<CrossReference> _crossRefs = [];
	private readonly HashSet<(uint From, uint To, CrossRefType Type)> _crossRefSet = [];
	private readonly List<MultiTargetCrossReference> _multiTargetCrossRefs = [];
	private readonly List<Bookmark> _bookmarks = [];
	private readonly List<DataTypeEntry> _dataTypes = [];
	private readonly List<string> _sourceFiles = [];
	private readonly List<SourceMapEntry> _sourceMapEntries = [];
	private readonly List<CpuStateEntry> _cpuStateEntries = [];
	private string _projectName = "";
	private string _author = "";
	private string _projectVersion = "";

	// Cross-reference indexes (built after parsing for efficient queries)
	private FrozenDictionary<int, IReadOnlyList<CrossReference>> _xrefsTo = FrozenDictionary<int, IReadOnlyList<CrossReference>>.Empty;
	private FrozenDictionary<int, IReadOnlyList<CrossReference>> _xrefsFrom = FrozenDictionary<int, IReadOnlyList<CrossReference>>.Empty;
	private FrozenDictionary<int, IReadOnlyList<MultiTargetCrossReference>> _multiXrefsFrom = FrozenDictionary<int, IReadOnlyList<MultiTargetCrossReference>>.Empty;

	// Cached backward-compat dictionaries (computed once, not per-access)
	private IReadOnlyDictionary<int, string>? _symbolsCache;
	private IReadOnlyDictionary<int, string>? _commentsCache;
	private IReadOnlyDictionary<int, SymbolEntry>? _singleSymbolEntriesCache;
	private IReadOnlyDictionary<int, CommentEntry>? _singleCommentEntriesCache;

	// Temporary mutable collections used during parsing (nulled after freeze)
	private HashSet<int>? _tempCodeOffsets;
	private HashSet<int>? _tempDataOffsets;
	private HashSet<int>? _tempJumpTargets;
	private HashSet<int>? _tempSubEntryPoints;
	private HashSet<int>? _tempOpcodeOffsets;
	private HashSet<int>? _tempDrawnOffsets;
	private HashSet<int>? _tempReadOffsets;
	private HashSet<int>? _tempIndirectOffsets;
	private Dictionary<int, List<SymbolEntry>>? _tempSymbolEntries;
	private Dictionary<int, List<CommentEntry>>? _tempCommentEntries;

	#region Constants
	// Platform IDs
	/// <summary>Platform ID for NES.</summary>
	public const byte PLATFORM_NES = 0x01;
	/// <summary>Platform ID for SNES.</summary>
	public const byte PLATFORM_SNES = 0x02;
	/// <summary>Platform ID for Game Boy.</summary>
	public const byte PLATFORM_GB = 0x03;
	/// <summary>Platform ID for Game Boy Advance.</summary>
	public const byte PLATFORM_GBA = 0x04;
	/// <summary>Platform ID for Sega Genesis.</summary>
	public const byte PLATFORM_GENESIS = 0x05;
	/// <summary>Platform ID for Sega Master System.</summary>
	public const byte PLATFORM_SMS = 0x06;
	/// <summary>Platform ID for TurboGrafx-16.</summary>
	public const byte PLATFORM_PCE = 0x07;
	/// <summary>Platform ID for Atari 2600.</summary>
	public const byte PLATFORM_ATARI_2600 = 0x08;
	/// <summary>Platform ID for Atari Lynx.</summary>
	public const byte PLATFORM_LYNX = 0x09;
	/// <summary>Platform ID for WonderSwan.</summary>
	public const byte PLATFORM_WONDERSWAN = 0x0a;
	/// <summary>Platform ID for Neo Geo.</summary>
	public const byte PLATFORM_NEOGEO = 0x0b;
	/// <summary>Platform ID for SPC700.</summary>
	public const byte PLATFORM_SPC700 = 0x0c;
	/// <summary>Platform ID for Commodore 64.</summary>
	public const byte PLATFORM_C64 = 0x0d;
	/// <summary>Platform ID for MSX.</summary>
	public const byte PLATFORM_MSX = 0x0e;
	/// <summary>Platform ID for Atari 7800.</summary>
	public const byte PLATFORM_ATARI_7800 = 0x0f;
	/// <summary>Platform ID for Atari 8-bit (400/800/XL/XE).</summary>
	public const byte PLATFORM_ATARI_8BIT = 0x10;
	/// <summary>Platform ID for Apple II.</summary>
	public const byte PLATFORM_APPLE_II = 0x11;
	/// <summary>Platform ID for ZX Spectrum.</summary>
	public const byte PLATFORM_ZX_SPECTRUM = 0x12;
	/// <summary>Platform ID for ColecoVision.</summary>
	public const byte PLATFORM_COLECO = 0x13;
	/// <summary>Platform ID for Intellivision.</summary>
	public const byte PLATFORM_INTELLIVISION = 0x14;
	/// <summary>Platform ID for Vectrex.</summary>
	public const byte PLATFORM_VECTREX = 0x15;
	/// <summary>Platform ID for Sega Game Gear.</summary>
	public const byte PLATFORM_GAMEGEAR = 0x16;
	/// <summary>Platform ID for Sega 32X.</summary>
	public const byte PLATFORM_32X = 0x17;
	/// <summary>Platform ID for Sega CD.</summary>
	public const byte PLATFORM_SEGACD = 0x18;
	/// <summary>Platform ID for Virtual Boy.</summary>
	public const byte PLATFORM_VIRTUALBOY = 0x19;
	/// <summary>Platform ID for Amstrad CPC.</summary>
	public const byte PLATFORM_AMSTRAD_CPC = 0x1a;
	/// <summary>Platform ID for BBC Micro.</summary>
	public const byte PLATFORM_BBC_MICRO = 0x1b;
	/// <summary>Platform ID for Commodore VIC-20.</summary>
	public const byte PLATFORM_VIC20 = 0x1c;
	/// <summary>Platform ID for Commodore Plus/4.</summary>
	public const byte PLATFORM_PLUS4 = 0x1d;
	/// <summary>Platform ID for Commodore 128.</summary>
	public const byte PLATFORM_C128 = 0x1e;
	/// <summary>Platform ID for Fairchild Channel F.</summary>
	public const byte PLATFORM_CHANNEL_F = 0x1f;
	/// <summary>Platform ID for custom/unknown platform.</summary>
	public const byte PLATFORM_CUSTOM = 0xff;

	// Section types
	private const uint SECTION_CODE_DATA_MAP = 0x0001;
	private const uint SECTION_SYMBOLS = 0x0002;
	private const uint SECTION_COMMENTS = 0x0003;
	private const uint SECTION_MEMORY_REGIONS = 0x0004;
	private const uint SECTION_DATA_TYPES = 0x0005;
	private const uint SECTION_CROSS_REFS = 0x0006;
	private const uint SECTION_SOURCE_MAP = 0x0007;
	private const uint SECTION_METADATA = 0x0008;
	private const uint SECTION_CPU_STATE = 0x0009;
	private const uint SECTION_BOOKMARKS = 0x000a;
	private const uint SECTION_MULTI_TARGET_CROSS_REFS = 0x000b;

	// Byte flags
	private const byte FLAG_CODE = 0x01;
	private const byte FLAG_DATA = 0x02;
	private const byte FLAG_JUMP_TARGET = 0x04;
	private const byte FLAG_SUB_ENTRY = 0x08;
	private const byte FLAG_OPCODE = 0x10;
	private const byte FLAG_DRAWN = 0x20;
	private const byte FLAG_READ = 0x40;
	private const byte FLAG_INDIRECT = 0x80;
	#endregion

	#region Properties
	/// <summary>Gets the format version.</summary>
	public ushort Version => _version;

	/// <summary>Gets the file flags.</summary>
	public PansyFlags Flags => _flags;

	/// <summary>Gets whether the file uses compression.</summary>
	public bool IsCompressed => _flags.HasFlag(PansyFlags.Compressed);

	/// <summary>Gets the platform ID.</summary>
	public byte Platform => _platform;

	/// <summary>Gets the ROM size.</summary>
	public uint RomSize => _romSize;

	/// <summary>Gets the ROM CRC32.</summary>
	public uint RomCrc32 => _romCrc32;

	/// <summary>
	/// Indicates whether this Pansy file contains code/data map information.
	/// If false, IsCode() and IsData() will always return false, which should be
	/// interpreted as "no information available" rather than "not code/data".
	/// </summary>
	public bool HasCodeDataMap => _codeOffsets.Count > 0 || _dataOffsets.Count > 0;

	/// <summary>Gets the raw code/data map bytes (one byte per ROM offset, using CDL flag constants).</summary>
	public byte[]? CodeDataMapBytes => _codeDataMap;

	/// <summary>Gets all ROM offsets marked as code.</summary>
	public IReadOnlySet<int> CodeOffsets => _codeOffsets;

	/// <summary>Gets all ROM offsets marked as data.</summary>
	public IReadOnlySet<int> DataOffsets => _dataOffsets;

	/// <summary>Gets all ROM offsets that are jump targets.</summary>
	public IReadOnlySet<int> JumpTargets => _jumpTargets;

	/// <summary>Gets all ROM offsets that are subroutine entry points.</summary>
	public IReadOnlySet<int> SubEntryPoints => _subEntryPoints;

	/// <summary>Gets all ROM offsets that are opcodes (vs operands).</summary>
	public IReadOnlySet<int> OpcodeOffsets => _opcodeOffsets;

	/// <summary>Gets all ROM offsets that were drawn/rendered (graphics).</summary>
	public IReadOnlySet<int> DrawnOffsets => _drawnOffsets;

	/// <summary>Gets all ROM offsets that were read as data.</summary>
	public IReadOnlySet<int> ReadOffsets => _readOffsets;

	/// <summary>Gets all ROM offsets accessed via indirect addressing.</summary>
	public IReadOnlySet<int> IndirectOffsets => _indirectOffsets;

	/// <summary>Gets symbols by address (name only, for backward compatibility). Returns first symbol name per address. Cached.</summary>
	public IReadOnlyDictionary<int, string> Symbols =>
		_symbolsCache ??= _symbolEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0].Name);

	/// <summary>Gets first typed symbol entry per address (for backward compatibility). Cached.</summary>
	public IReadOnlyDictionary<int, SymbolEntry> SymbolEntries =>
		_singleSymbolEntriesCache ??= _symbolEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0]);

	/// <summary>Gets all typed symbol entries per address, supporting multiple symbols at the same address.</summary>
	public IReadOnlyDictionary<int, IReadOnlyList<SymbolEntry>> AllSymbolEntries => _symbolEntries;

	/// <summary>Gets comments by address (text only, for backward compatibility). Returns first comment text per address. Cached.</summary>
	public IReadOnlyDictionary<int, string> Comments =>
		_commentsCache ??= _commentEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0].Text);

	/// <summary>Gets first typed comment entry per address (for backward compatibility). Cached.</summary>
	public IReadOnlyDictionary<int, CommentEntry> CommentEntries =>
		_singleCommentEntriesCache ??= _commentEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0]);

	/// <summary>Gets all typed comment entries per address, supporting multiple comments at the same address.</summary>
	public IReadOnlyDictionary<int, IReadOnlyList<CommentEntry>> AllCommentEntries => _commentEntries;

	/// <summary>Gets memory regions.</summary>
	public IReadOnlyList<MemoryRegion> MemoryRegions => _memoryRegions;

	/// <summary>Gets cross-references.</summary>
	public IReadOnlyList<CrossReference> CrossReferences => _crossRefs;

	/// <summary>Gets grouped one-source-many-target cross-references.</summary>
	public IReadOnlyList<MultiTargetCrossReference> MultiTargetCrossReferences => _multiTargetCrossRefs;

	/// <summary>Gets bookmarks.</summary>
	public IReadOnlyList<Bookmark> Bookmarks => _bookmarks;

	/// <summary>Gets data type annotations.</summary>
	public IReadOnlyList<DataTypeEntry> DataTypes => _dataTypes;

	/// <summary>Gets source file paths referenced by the source map.</summary>
	public IReadOnlyList<string> SourceFiles => _sourceFiles;

	/// <summary>Gets source map entries linking ROM addresses to source locations.</summary>
	public IReadOnlyList<SourceMapEntry> SourceMapEntries => _sourceMapEntries;

	/// <summary>Gets CPU state entries for per-address processor state.</summary>
	public IReadOnlyList<CpuStateEntry> CpuStateEntries => _cpuStateEntries;

	/// <summary>Gets the project name.</summary>
	public string ProjectName => _projectName;

	/// <summary>Gets the author.</summary>
	public string Author => _author;

	/// <summary>Gets the project version.</summary>
	public string ProjectVersion => _projectVersion;
	#endregion

	/// <summary>
	/// Creates a Pansy loader from raw data.
	/// </summary>
	/// <param name="data">The raw Pansy file bytes.</param>
	public PansyLoader(byte[] data) {
		_data = data;

		// Validate magic
		if (data.Length < 32 ||
			data[0] != 'P' || data[1] != 'A' || data[2] != 'N' ||
			data[3] != 'S' || data[4] != 'Y') {
			throw new InvalidDataException("Invalid Pansy file: bad magic number");
		}

		// Parse header
		var span = data.AsSpan();
		_version = BinaryPrimitives.ReadUInt16LittleEndian(span[8..]);
		_flags = (PansyFlags)BinaryPrimitives.ReadUInt16LittleEndian(span[10..]);
		_platform = data[12];
		_romSize = BinaryPrimitives.ReadUInt32LittleEndian(span[16..]);
		_romCrc32 = BinaryPrimitives.ReadUInt32LittleEndian(span[20..]);
		var sectionCount = BinaryPrimitives.ReadUInt32LittleEndian(span[24..]);

		// Parse section table
		var tableOffset = 32;
		for (int i = 0; i < sectionCount; i++) {
			var type = BinaryPrimitives.ReadUInt32LittleEndian(span[tableOffset..]);
			var offset = BinaryPrimitives.ReadUInt32LittleEndian(span[(tableOffset + 4)..]);
			var compSize = BinaryPrimitives.ReadUInt32LittleEndian(span[(tableOffset + 8)..]);
			var uncompSize = BinaryPrimitives.ReadUInt32LittleEndian(span[(tableOffset + 12)..]);
			_sections.Add(new SectionInfo(type, offset, compSize, uncompSize));
			tableOffset += 16;
		}

		// Initialize temporary mutable collections for parsing
		_tempCodeOffsets = [];
		_tempDataOffsets = [];
		_tempJumpTargets = [];
		_tempSubEntryPoints = [];
		_tempOpcodeOffsets = [];
		_tempDrawnOffsets = [];
		_tempReadOffsets = [];
		_tempIndirectOffsets = [];
		_tempSymbolEntries = [];
		_tempCommentEntries = [];

		// Decompress sections in parallel (DEFLATE is CPU-intensive)
		var decompressed = new byte[_sections.Count][];
		if (_sections.Count >= 3) {
			Parallel.For(0, _sections.Count, i => {
				decompressed[i] = GetSectionData(_sections[i]);
			});
		} else {
			for (int i = 0; i < _sections.Count; i++) {
				decompressed[i] = GetSectionData(_sections[i]);
			}
		}

		// Parse sections sequentially (writes to shared temp collections)
		for (int i = 0; i < _sections.Count; i++) {
			ParseSectionData(_sections[i].Type, decompressed[i]);
		}

		// Freeze all collections for optimal immutable lookup performance
		_codeOffsets = _tempCodeOffsets.ToFrozenSet();
		_dataOffsets = _tempDataOffsets.ToFrozenSet();
		_jumpTargets = _tempJumpTargets.ToFrozenSet();
		_subEntryPoints = _tempSubEntryPoints.ToFrozenSet();
		_opcodeOffsets = _tempOpcodeOffsets.ToFrozenSet();
		_drawnOffsets = _tempDrawnOffsets.ToFrozenSet();
		_readOffsets = _tempReadOffsets.ToFrozenSet();
		_indirectOffsets = _tempIndirectOffsets.ToFrozenSet();
		_symbolEntries = _tempSymbolEntries.ToFrozenDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<SymbolEntry>)kvp.Value.AsReadOnly());
		_commentEntries = _tempCommentEntries.ToFrozenDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<CommentEntry>)kvp.Value.AsReadOnly());

		// Build cross-reference indexes for efficient queries
		BuildCrossRefIndexes();

		// Release temporary collections
		_tempCodeOffsets = null;
		_tempDataOffsets = null;
		_tempJumpTargets = null;
		_tempSubEntryPoints = null;
		_tempOpcodeOffsets = null;
		_tempDrawnOffsets = null;
		_tempReadOffsets = null;
		_tempIndirectOffsets = null;
		_tempSymbolEntries = null;
		_tempCommentEntries = null;
	}

	/// <summary>
	/// Loads a Pansy file from disk.
	/// </summary>
	/// <param name="path">Path to the Pansy file.</param>
	/// <returns>A new PansyLoader instance.</returns>
	public static PansyLoader Load(string path) {
		var data = File.ReadAllBytes(path);
		return new PansyLoader(data);
	}

	/// <summary>
	/// Checks if a ROM offset is marked as code.
	/// </summary>
	public bool IsCode(int offset) => _codeOffsets.Contains(offset);

	/// <summary>
	/// Checks if a ROM offset is marked as data.
	/// </summary>
	public bool IsData(int offset) => _dataOffsets.Contains(offset);

	/// <summary>
	/// Checks if a ROM offset is a jump target.
	/// </summary>
	public bool IsJumpTarget(int offset) => _jumpTargets.Contains(offset);

	/// <summary>
	/// Checks if a ROM offset is a subroutine entry point.
	/// </summary>
	public bool IsSubEntryPoint(int offset) => _subEntryPoints.Contains(offset);

	/// <summary>
	/// Checks if a ROM offset is an opcode.
	/// </summary>
	public bool IsOpcode(int offset) => _opcodeOffsets.Contains(offset);

	/// <summary>
	/// Gets the symbol name at an address, or null if none. Returns the first symbol if multiple exist.
	/// </summary>
	public string? GetSymbol(int address) =>
		_symbolEntries.TryGetValue(address, out var entries) ? entries[0].Name : null;

	/// <summary>
	/// Gets the first typed symbol entry at an address, or null if none.
	/// </summary>
	public SymbolEntry? GetSymbolEntry(int address) =>
		_symbolEntries.TryGetValue(address, out var entries) ? entries[0] : null;

	/// <summary>
	/// Gets all typed symbol entries at an address, or null if none.
	/// </summary>
	public IReadOnlyList<SymbolEntry>? GetSymbolEntries(int address) =>
		_symbolEntries.GetValueOrDefault(address);

	/// <summary>
	/// Gets the symbol type at an address, or null if no symbol exists. Returns the first symbol's type if multiple exist.
	/// </summary>
	public SymbolType? GetSymbolType(int address) =>
		_symbolEntries.TryGetValue(address, out var entries) ? entries[0].Type : null;

	/// <summary>
	/// Gets the comment text at an address, or null if none. Returns the first comment if multiple exist.
	/// </summary>
	public string? GetComment(int address) =>
		_commentEntries.TryGetValue(address, out var entries) ? entries[0].Text : null;

	/// <summary>
	/// Gets the first typed comment entry at an address, or null if none.
	/// </summary>
	public CommentEntry? GetCommentEntry(int address) =>
		_commentEntries.TryGetValue(address, out var entries) ? entries[0] : null;

	/// <summary>
	/// Gets all typed comment entries at an address, or null if none.
	/// </summary>
	public IReadOnlyList<CommentEntry>? GetCommentEntries(int address) =>
		_commentEntries.GetValueOrDefault(address);

	/// <summary>
	/// Gets the comment type at an address, or null if no comment exists. Returns the first comment's type if multiple exist.
	/// </summary>
	public CommentType? GetCommentType(int address) =>
		_commentEntries.TryGetValue(address, out var entries) ? entries[0].Type : null;

	/// <summary>
	/// Checks if a ROM offset was drawn/rendered as graphics.
	/// </summary>
	public bool IsDrawn(int offset) => _drawnOffsets.Contains(offset);

	/// <summary>
	/// Checks if a ROM offset was read as data by the CPU.
	/// </summary>
	public bool IsRead(int offset) => _readOffsets.Contains(offset);

	/// <summary>
	/// Checks if a ROM offset was accessed via indirect addressing.
	/// </summary>
	public bool IsIndirect(int offset) => _indirectOffsets.Contains(offset);

	/// <summary>
	/// Gets coverage statistics.
	/// </summary>
	public (int CodeBytes, int DataBytes, int TotalSize, double CoveragePercent) GetCoverageStats() {
		var totalSize = (int)_romSize;
		var totalMarked = _codeOffsets.Count + _dataOffsets.Count;
		var coverage = totalSize > 0 ? (totalMarked * 100.0) / totalSize : 0;
		return (_codeOffsets.Count, _dataOffsets.Count, totalSize, coverage);
	}

	#region Cross-Reference Queries

	/// <summary>
	/// Gets all cross-references pointing TO an address.
	/// </summary>
	public IReadOnlyList<CrossReference> GetCrossRefsTo(int address) =>
		_xrefsTo.GetValueOrDefault(address) ?? [];

	/// <summary>
	/// Gets all cross-references originating FROM an address.
	/// </summary>
	public IReadOnlyList<CrossReference> GetCrossRefsFrom(int address) =>
		_xrefsFrom.GetValueOrDefault(address) ?? [];

	/// <summary>
	/// Gets grouped one-source-many-target cross-references originating FROM an address.
	/// </summary>
	public IReadOnlyList<MultiTargetCrossReference> GetMultiTargetCrossRefsFrom(int address) =>
		_multiXrefsFrom.GetValueOrDefault(address) ?? [];

	/// <summary>
	/// Gets all cross-references of a specific type.
	/// </summary>
	public IEnumerable<CrossReference> GetCrossRefsByType(CrossRefType type) =>
		_crossRefs.Where(x => x.Type == type);

	/// <summary>
	/// Gets all cross-references where the source address is in the specified range.
	/// </summary>
	public IEnumerable<CrossReference> GetCrossRefsFromRange(int start, int end) =>
		_crossRefs.Where(x => x.From >= (uint)start && x.From <= (uint)end);

	/// <summary>
	/// Gets all cross-references where the target address is in the specified range.
	/// </summary>
	public IEnumerable<CrossReference> GetCrossRefsToRange(int start, int end) =>
		_crossRefs.Where(x => x.To >= (uint)start && x.To <= (uint)end);

	/// <summary>
	/// Gets the number of cross-references pointing TO an address.
	/// </summary>
	public int GetReferenceCount(int address) =>
		_xrefsTo.TryGetValue(address, out var list) ? list.Count : 0;

	/// <summary>
	/// Gets addresses that are subroutine entry points but have no cross-references pointing to them.
	/// These are potential dead code or entry points only reached via indirect calls.
	/// </summary>
	public IEnumerable<int> GetUnreferencedSubroutines() =>
		_subEntryPoints.Where(addr => !_xrefsTo.ContainsKey(addr));

	/// <summary>
	/// Gets the most-referenced addresses, sorted by reference count descending.
	/// </summary>
	public IEnumerable<(int Address, int Count)> GetMostReferencedAddresses(int limit = 20) =>
		_xrefsTo
			.Select(kvp => (Address: kvp.Key, Count: kvp.Value.Count))
			.OrderByDescending(x => x.Count)
			.Take(limit);

	/// <summary>
	/// Gets cross-reference statistics.
	/// </summary>
	public (int TotalXrefs, int JsrCount, int JmpCount, int BranchCount, int ReadCount, int WriteCount) GetCrossRefStats() {
		int jsr = 0, jmp = 0, branch = 0, read = 0, write = 0;
		foreach (var xref in _crossRefs) {
			switch (xref.Type) {
				case CrossRefType.Jsr: jsr++; break;
				case CrossRefType.Jmp: jmp++; break;
				case CrossRefType.Branch: branch++; break;
				case CrossRefType.Read: read++; break;
				case CrossRefType.Write: write++; break;
			}
		}
		return (_crossRefs.Count, jsr, jmp, branch, read, write);
	}

	#endregion

	/// <summary>
	/// Gets platform name from ID.
	/// </summary>
	public static string GetPlatformName(byte platformId) => platformId switch {
		PLATFORM_NES => "NES",
		PLATFORM_SNES => "SNES",
		PLATFORM_GB => "Game Boy",
		PLATFORM_GBA => "Game Boy Advance",
		PLATFORM_GENESIS => "Sega Genesis",
		PLATFORM_SMS => "Sega Master System",
		PLATFORM_PCE => "TurboGrafx-16",
		PLATFORM_ATARI_2600 => "Atari 2600",
		PLATFORM_LYNX => "Atari Lynx",
		PLATFORM_WONDERSWAN => "WonderSwan",
		PLATFORM_NEOGEO => "Neo Geo",
		PLATFORM_SPC700 => "SPC700",
		PLATFORM_C64 => "Commodore 64",
		PLATFORM_MSX => "MSX",
		PLATFORM_ATARI_7800 => "Atari 7800",
		PLATFORM_ATARI_8BIT => "Atari 8-bit",
		PLATFORM_APPLE_II => "Apple II",
		PLATFORM_ZX_SPECTRUM => "ZX Spectrum",
		PLATFORM_COLECO => "ColecoVision",
		PLATFORM_INTELLIVISION => "Intellivision",
		PLATFORM_VECTREX => "Vectrex",
		PLATFORM_GAMEGEAR => "Sega Game Gear",
		PLATFORM_32X => "Sega 32X",
		PLATFORM_SEGACD => "Sega CD",
		PLATFORM_VIRTUALBOY => "Virtual Boy",
		PLATFORM_AMSTRAD_CPC => "Amstrad CPC",
		PLATFORM_BBC_MICRO => "BBC Micro",
		PLATFORM_VIC20 => "Commodore VIC-20",
		PLATFORM_PLUS4 => "Commodore Plus/4",
		PLATFORM_C128 => "Commodore 128",
		PLATFORM_CHANNEL_F => "Fairchild Channel F",
		PLATFORM_CUSTOM => "Custom",
		_ => "Unknown"
	};

	/// <summary>
	/// Decompresses section data if needed.
	/// </summary>
	private byte[] GetSectionData(SectionInfo section) {
		// Check if compressed (different sizes)
		if (section.CompressedSize != section.UncompressedSize && _flags.HasFlag(PansyFlags.Compressed)) {
			try {
				using var compStream = new MemoryStream(_data, (int)section.Offset, (int)section.CompressedSize, writable: false);
				using var deflate = new DeflateStream(compStream, CompressionMode.Decompress);
				var result = new byte[section.UncompressedSize];
				deflate.ReadExactly(result);
				return result;
			} catch {
				// If decompression fails, return raw data
				return _data.AsSpan((int)section.Offset, (int)section.CompressedSize).ToArray();
			}
		}

		return _data.AsSpan((int)section.Offset, (int)section.CompressedSize).ToArray();
	}

	/// <summary>
	/// Parses a section based on its type.
	/// </summary>
	private void ParseSection(SectionInfo section) {
		var data = GetSectionData(section);
		ParseSectionData(section.Type, data);
	}

	/// <summary>
	/// Parses pre-decompressed section data based on its type.
	/// </summary>
	private void ParseSectionData(uint sectionType, byte[] data) {
		switch (sectionType) {
			case SECTION_CODE_DATA_MAP:
				ParseCodeDataMap(data);
				break;
			case SECTION_SYMBOLS:
				ParseSymbols(data);
				break;
			case SECTION_COMMENTS:
				ParseComments(data);
				break;
			case SECTION_MEMORY_REGIONS:
				ParseMemoryRegions(data);
				break;
			case SECTION_CROSS_REFS:
				ParseCrossRefs(data);
				break;
			case SECTION_BOOKMARKS:
				ParseBookmarks(data);
				break;
			case SECTION_MULTI_TARGET_CROSS_REFS:
				ParseMultiTargetCrossRefs(data);
				break;
			case SECTION_DATA_TYPES:
				ParseDataTypes(data);
				break;
			case SECTION_SOURCE_MAP:
				ParseSourceMap(data);
				break;
			case SECTION_CPU_STATE:
				ParseCpuState(data);
				break;
			case SECTION_METADATA:
				ParseMetadata(data);
				break;
			// SOURCE_MAP is reserved for future use
		}
	}

	private void ParseCodeDataMap(byte[] data) {
		_codeDataMap = data;

		for (int i = 0; i < data.Length; i++) {
			var flags = data[i];
			if (flags == 0) continue;

			if ((flags & FLAG_CODE) != 0)
				_tempCodeOffsets!.Add(i);

			if ((flags & FLAG_DATA) != 0)
				_tempDataOffsets!.Add(i);

			if ((flags & FLAG_JUMP_TARGET) != 0)
				_tempJumpTargets!.Add(i);

			if ((flags & FLAG_SUB_ENTRY) != 0)
				_tempSubEntryPoints!.Add(i);

			if ((flags & FLAG_OPCODE) != 0)
				_tempOpcodeOffsets!.Add(i);

			if ((flags & FLAG_DRAWN) != 0)
				_tempDrawnOffsets!.Add(i);

			if ((flags & FLAG_READ) != 0)
				_tempReadOffsets!.Add(i);

			if ((flags & FLAG_INDIRECT) != 0)
				_tempIndirectOffsets!.Add(i);
		}
	}

	private void ParseSymbols(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		while (pos + 10 <= len) { // minimum record: 4+1+1+2+0+2 = 10 bytes
			var address = (int)BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var type = (SymbolType)data[pos + 4];
			// skip flags byte at pos+5
			var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 6)..]);
			pos += 8;

			if (pos + nameLength > len) break;
			var name = Encoding.UTF8.GetString(data.AsSpan(pos, nameLength));
			pos += nameLength;

			if (pos + 2 > len) break;
			var valueLength = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
			pos += 2 + valueLength;

			if (!_tempSymbolEntries!.TryGetValue(address, out var list)) {
				list = [];
				_tempSymbolEntries[address] = list;
			}
			list.Add(new SymbolEntry(name, type));
		}
	}

	private void ParseComments(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		while (pos + 7 <= len) { // minimum record: 4+1+2+0 = 7 bytes
			var address = (int)BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var type = data[pos + 4];
			var length = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 5)..]);
			pos += 7;

			if (pos + length > len) break;
			var text = Encoding.UTF8.GetString(data.AsSpan(pos, length));
			pos += length;

			if (!_tempCommentEntries!.TryGetValue(address, out var list)) {
				list = [];
				_tempCommentEntries[address] = list;
			}
			list.Add(new CommentEntry(text, (CommentType)type));
		}
	}

	private void ParseMemoryRegions(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		while (pos + 14 <= len) { // minimum: 4+4+1+1+2+2+0 = 14 bytes
			var start = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var end = BinaryPrimitives.ReadUInt32LittleEndian(span[(pos + 4)..]);
			var type = data[pos + 8];
			var bank = data[pos + 9];
			// flags at pos+10 (2 bytes) — currently unused
			var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 12)..]);
			pos += 14;

			if (pos + nameLength > len) break;
			var name = Encoding.UTF8.GetString(data.AsSpan(pos, nameLength));
			pos += nameLength;

			_memoryRegions.Add(new MemoryRegion(start, end, type, bank, name));
		}
	}

	private void ParseCrossRefs(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		while (pos + 9 <= len) { // 4+4+1 = 9 bytes per record
			var from = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var to = BinaryPrimitives.ReadUInt32LittleEndian(span[(pos + 4)..]);
			var type = (CrossRefType)data[pos + 8];
			pos += 9;

			AddCrossRefUnique(from, to, type);
		}
	}

	private void ParseMultiTargetCrossRefs(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		while (pos + 7 <= len) {
			var from = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var type = (CrossRefType)data[pos + 4];
			var targetCount = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 5)..]);
			pos += 7;

			if (targetCount == 0) {
				continue;
			}

			int bytesNeeded = targetCount * 4;
			if (pos + bytesNeeded > len) {
				break;
			}

			var targets = new List<uint>(targetCount);
			for (int i = 0; i < targetCount; i++) {
				var target = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
				pos += 4;
				targets.Add(target);
				// Keep legacy edge list complete for older query paths.
				AddCrossRefUnique(from, target, type);
			}

			_multiTargetCrossRefs.Add(new MultiTargetCrossReference(from, type, targets));
		}
	}

	private void AddCrossRefUnique(uint from, uint to, CrossRefType type) {
		if (_crossRefSet.Add((from, to, type))) {
			_crossRefs.Add(new CrossReference(from, to, type));
		}
	}

	private void ParseBookmarks(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		while (pos + 7 <= len) { // minimum: 4+1+2+0 = 7 bytes
			var address = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var color = data[pos + 4];
			var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 5)..]);
			pos += 7;

			if (pos + nameLength > len) break;
			var name = Encoding.UTF8.GetString(data.AsSpan(pos, nameLength));
			pos += nameLength;

			_bookmarks.Add(new Bookmark(address, name, color));
		}
	}

	private void ParseDataTypes(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		while (pos + 15 <= len) { // minimum: 4+4+2+2+1+2+0 = 15 bytes
			var address = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var length = BinaryPrimitives.ReadUInt32LittleEndian(span[(pos + 4)..]);
			var elementSize = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 8)..]);
			var elementCount = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 10)..]);
			var type = (DataElementType)data[pos + 12];
			var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 13)..]);
			pos += 15;

			if (pos + nameLength > len) break;
			var name = Encoding.UTF8.GetString(data.AsSpan(pos, nameLength));
			pos += nameLength;

			_dataTypes.Add(new DataTypeEntry(address, length, elementSize, elementCount, type, name));
		}
	}

	private void ParseSourceMap(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		if (pos + 2 > len) return;

		// Read source file table
		var fileCount = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
		pos += 2;

		for (int i = 0; i < fileCount; i++) {
			if (pos + 2 > len) return;
			var pathLength = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
			pos += 2;

			if (pos + pathLength > len) return;
			var path = Encoding.UTF8.GetString(data.AsSpan(pos, pathLength));
			pos += pathLength;
			_sourceFiles.Add(path);
		}

		// Read source map entries (10 bytes each: 4+2+2+2)
		while (pos + 10 <= len) {
			var romAddress = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var fileIndex = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 4)..]);
			var line = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 6)..]);
			var column = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 8)..]);
			pos += 10;

			_sourceMapEntries.Add(new SourceMapEntry(romAddress, fileIndex, line, column));
		}
	}

	private void ParseCpuState(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		while (pos + 9 <= len) { // 4+1+1+2+1 = 9 bytes per record
			var address = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
			var flags = data[pos + 4];
			var dataBank = data[pos + 5];
			var directPage = BinaryPrimitives.ReadUInt16LittleEndian(span[(pos + 6)..]);
			var mode = (CpuMode)data[pos + 8];
			pos += 9;

			_cpuStateEntries.Add(new CpuStateEntry(address, flags, dataBank, directPage, mode));
		}
	}

	private void ParseMetadata(byte[] data) {
		int pos = 0;
		int len = data.Length;
		var span = data.AsSpan();

		if (pos + 2 > len) return;
		var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
		pos += 2;
		if (pos + nameLength > len) return;
		_projectName = Encoding.UTF8.GetString(data.AsSpan(pos, nameLength));
		pos += nameLength;

		if (pos + 2 > len) return;
		var authorLength = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
		pos += 2;
		if (pos + authorLength > len) return;
		_author = Encoding.UTF8.GetString(data.AsSpan(pos, authorLength));
		pos += authorLength;

		if (pos + 2 > len) return;
		var versionLength = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
		pos += 2;
		if (pos + versionLength > len) return;
		_projectVersion = Encoding.UTF8.GetString(data.AsSpan(pos, versionLength));
		// Timestamps are ignored for now
	}

	private void BuildCrossRefIndexes() {
		if (_crossRefs.Count == 0 && _multiTargetCrossRefs.Count == 0) return;

		var toIndex = new Dictionary<int, List<CrossReference>>();
		var fromIndex = new Dictionary<int, List<CrossReference>>();
		var multiFromIndex = new Dictionary<int, List<MultiTargetCrossReference>>();

		foreach (var xref in _crossRefs) {
			var toAddr = (int)xref.To;
			if (!toIndex.TryGetValue(toAddr, out var toList)) {
				toList = [];
				toIndex[toAddr] = toList;
			}
			toList.Add(xref);

			var fromAddr = (int)xref.From;
			if (!fromIndex.TryGetValue(fromAddr, out var fromList)) {
				fromList = [];
				fromIndex[fromAddr] = fromList;
			}
			fromList.Add(xref);
		}

		foreach (var xref in _multiTargetCrossRefs) {
			var fromAddr = (int)xref.From;
			if (!multiFromIndex.TryGetValue(fromAddr, out var fromList)) {
				fromList = [];
				multiFromIndex[fromAddr] = fromList;
			}
			fromList.Add(xref);
		}

		_xrefsTo = toIndex.ToFrozenDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<CrossReference>)kvp.Value.AsReadOnly());
		_xrefsFrom = fromIndex.ToFrozenDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<CrossReference>)kvp.Value.AsReadOnly());
		_multiXrefsFrom = multiFromIndex.ToFrozenDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<MultiTargetCrossReference>)kvp.Value.AsReadOnly());
	}
}
