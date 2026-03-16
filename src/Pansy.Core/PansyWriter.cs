// ============================================================================
// PansyWriter.cs - Program ANalysis SYstem File Generation
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using System.IO.Compression;

namespace Pansy.Core;

/// <summary>
/// Writes Pansy (Program ANalysis SYstem) files for comprehensive metadata export.
/// Pansy files contain code/data maps, symbols, comments, cross-references, and more.
/// </summary>
public sealed class PansyWriter {
	private readonly Dictionary<uint, List<(string Name, SymbolType Type)>> _symbols = [];
	private readonly Dictionary<uint, List<(string Text, byte CommentType)>> _comments = [];
	private readonly HashSet<uint> _codeOffsets = [];
	private readonly HashSet<uint> _dataOffsets = [];
	private readonly HashSet<uint> _jumpTargets = [];
	private readonly HashSet<uint> _subEntryPoints = [];
	private readonly HashSet<uint> _opcodeOffsets = [];
	private readonly HashSet<uint> _drawnOffsets = [];
	private readonly HashSet<uint> _readOffsets = [];
	private readonly HashSet<uint> _indirectOffsets = [];
	private readonly List<MemoryRegion> _memoryRegions = [];
	private readonly List<CrossReference> _crossRefs = [];
	private readonly List<Bookmark> _bookmarks = [];
	private readonly List<DataTypeEntry> _dataTypes = [];
	private readonly List<string> _sourceFiles = [];
	private readonly Dictionary<string, ushort> _sourceFileIndex = [];
	private readonly List<SourceMapEntry> _sourceMapEntries = [];
	private readonly List<CpuStateEntry> _cpuStateEntries = [];
	private byte _platform = PansyLoader.PLATFORM_CUSTOM;
	private uint _romSize;
	private uint _romCrc32;
	private string _projectName = "";
	private string _author = "";
	private string _projectVersion = "";
	private bool _enableCompression = false;

	// Pansy file magic and version
	private static readonly byte[] Magic = "PANSY\0\0\0"u8.ToArray();
	private const ushort FormatVersion = 0x0100; // v1.0

	/// <summary>Gets or sets the platform ID.</summary>
	public byte Platform {
		get => _platform;
		set => _platform = value;
	}

	/// <summary>Gets or sets the ROM size in bytes.</summary>
	public uint RomSize {
		get => _romSize;
		set => _romSize = value;
	}

	/// <summary>Gets or sets the ROM CRC32 checksum.</summary>
	public uint RomCrc32 {
		get => _romCrc32;
		set => _romCrc32 = value;
	}

	/// <summary>Gets or sets the project name.</summary>
	public string ProjectName {
		get => _projectName;
		set => _projectName = value ?? "";
	}

	/// <summary>Gets or sets the author name.</summary>
	public string Author {
		get => _author;
		set => _author = value ?? "";
	}

	/// <summary>Gets or sets the project version.</summary>
	public string ProjectVersion {
		get => _projectVersion;
		set => _projectVersion = value ?? "";
	}

	/// <summary>Gets or sets whether to enable DEFLATE compression.</summary>
	public bool EnableCompression {
		get => _enableCompression;
		set => _enableCompression = value;
	}

	/// <summary>Adds a symbol at the specified address. Multiple symbols per address are supported.</summary>
	public void AddSymbol(uint address, string name) {
		if (!_symbols.TryGetValue(address, out var list)) {
			list = [];
			_symbols[address] = list;
		}
		list.Add((name, SymbolType.Label));
	}

	/// <summary>Adds a typed symbol at the specified address. Multiple symbols per address are supported.</summary>
	public void AddSymbol(uint address, string name, SymbolType type) {
		if (!_symbols.TryGetValue(address, out var list)) {
			list = [];
			_symbols[address] = list;
		}
		list.Add((name, type));
	}

	/// <summary>Adds a comment at the specified address. Multiple comments per address are supported.</summary>
	public void AddComment(uint address, string comment) {
		if (!_comments.TryGetValue(address, out var list)) {
			list = [];
			_comments[address] = list;
		}
		list.Add((comment, 1)); // 1 = inline comment
	}

	/// <summary>Adds a typed comment at the specified address. Multiple comments per address are supported.</summary>
	/// <param name="address">The address for the comment.</param>
	/// <param name="comment">The comment text.</param>
	/// <param name="commentType">Comment type: 1=inline, 2=block, 3=todo.</param>
	public void AddComment(uint address, string comment, byte commentType) {
		if (!_comments.TryGetValue(address, out var list)) {
			list = [];
			_comments[address] = list;
		}
		list.Add((comment, commentType));
	}

	/// <summary>Adds a typed comment at the specified address using the CommentType enum.</summary>
	public void AddComment(uint address, string comment, CommentType commentType) {
		AddComment(address, comment, (byte)commentType);
	}

	/// <summary>Adds multiple symbols in batch for efficient bulk insertion.</summary>
	public void AddSymbols(IEnumerable<(uint Address, string Name, SymbolType Type)> symbols) {
		foreach (var (address, name, type) in symbols) {
			AddSymbol(address, name, type);
		}
	}

	/// <summary>Adds multiple comments in batch for efficient bulk insertion.</summary>
	public void AddComments(IEnumerable<(uint Address, string Text, CommentType Type)> comments) {
		foreach (var (address, text, type) in comments) {
			AddComment(address, text, (byte)type);
		}
	}

	/// <summary>Adds multiple cross-references in batch.</summary>
	public void AddCrossReferences(IEnumerable<CrossReference> crossRefs) {
		foreach (var xref in crossRefs) {
			_crossRefs.Add(xref);
		}
	}

	/// <summary>Adds multiple memory regions in batch.</summary>
	public void AddMemoryRegions(IEnumerable<MemoryRegion> regions) {
		foreach (var region in regions) {
			_memoryRegions.Add(region);
		}
	}

	/// <summary>Marks an address as code.</summary>
	public void MarkAsCode(uint address) {
		_codeOffsets.Add(address);
	}

	/// <summary>Marks an address as data.</summary>
	public void MarkAsData(uint address) {
		_dataOffsets.Add(address);
	}

	/// <summary>Marks an address as a jump target.</summary>
	public void MarkAsJumpTarget(uint address) {
		_jumpTargets.Add(address);
	}

	/// <summary>Marks an address as a subroutine entry point.</summary>
	public void MarkAsSubroutine(uint address) {
		_subEntryPoints.Add(address);
	}

	/// <summary>Marks an address as an opcode (vs operand byte).</summary>
	public void MarkAsOpcode(uint address) {
		_opcodeOffsets.Add(address);
	}

	/// <summary>Marks an address as drawn/rendered (graphics data accessed by PPU).</summary>
	public void MarkAsDrawn(uint address) {
		_drawnOffsets.Add(address);
	}

	/// <summary>Marks an address as read (data read by CPU).</summary>
	public void MarkAsRead(uint address) {
		_readOffsets.Add(address);
	}

	/// <summary>Marks an address as accessed via indirect addressing.</summary>
	public void MarkAsIndirect(uint address) {
		_indirectOffsets.Add(address);
	}

	/// <summary>Adds a memory region.</summary>
	public void AddMemoryRegion(MemoryRegion region) {
		_memoryRegions.Add(region);
	}

	/// <summary>Adds a cross-reference.</summary>
	public void AddCrossReference(CrossReference xref) {
		_crossRefs.Add(xref);
	}

	/// <summary>Adds a bookmark at the specified address.</summary>
	public void AddBookmark(Bookmark bookmark) {
		_bookmarks.Add(bookmark);
	}

	/// <summary>Adds a data type annotation.</summary>
	public void AddDataType(DataTypeEntry entry) {
		_dataTypes.Add(entry);
	}

	/// <summary>Adds a source file path and returns its index.</summary>
	public ushort AddSourceFile(string path) {
		if (_sourceFileIndex.TryGetValue(path, out var existing))
			return existing;
		var index = (ushort)_sourceFiles.Count;
		_sourceFileIndex[path] = index;
		_sourceFiles.Add(path);
		return index;
	}

	/// <summary>Adds a source map entry linking a ROM address to source location.</summary>
	public void AddSourceMapping(SourceMapEntry entry) {
		_sourceMapEntries.Add(entry);
	}

	/// <summary>Adds a CPU state entry for per-address processor state tracking.</summary>
	public void AddCpuState(CpuStateEntry entry) {
		_cpuStateEntries.Add(entry);
	}

	/// <summary>Adds multiple CPU state entries in batch.</summary>
	public void AddCpuStates(IEnumerable<CpuStateEntry> entries) {
		foreach (var entry in entries) {
			_cpuStateEntries.Add(entry);
		}
	}

	/// <summary>Generates the Pansy file as a byte array.</summary>
	public byte[] Generate() {
		// Build sections first to get their data and sizes
		var sectionData = new List<(uint Type, byte[] Data)>();

		// Code/Data map section (combines all code/data/jump/sub flags)
		var codeDataMap = BuildCodeDataMap();
		if (codeDataMap.Length > 0) {
			sectionData.Add((0x0001u, codeDataMap));
		}

		// Symbols section
		if (_symbols.Count > 0) {
			sectionData.Add((0x0002u, BuildSymbolsSection()));
		}

		// Comments section
		if (_comments.Count > 0) {
			sectionData.Add((0x0003u, BuildCommentsSection()));
		}

		// Memory regions section
		if (_memoryRegions.Count > 0) {
			sectionData.Add((0x0004u, BuildMemoryRegionsSection()));
		}

		// Data types section
		if (_dataTypes.Count > 0) {
			sectionData.Add((0x0005u, BuildDataTypesSection()));
		}

		// Cross-references section
		if (_crossRefs.Count > 0) {
			sectionData.Add((0x0006u, BuildCrossReferencesSection()));
		}

		// Source map section
		if (_sourceMapEntries.Count > 0) {
			sectionData.Add((0x0007u, BuildSourceMapSection()));
		}

		// Bookmarks section
		if (_bookmarks.Count > 0) {
			sectionData.Add((0x000au, BuildBookmarksSection()));
		}

		// CPU state section
		if (_cpuStateEntries.Count > 0) {
			sectionData.Add((0x0009u, BuildCpuStateSection()));
		}

		// Metadata section
		if (!string.IsNullOrEmpty(_projectName) || !string.IsNullOrEmpty(_author) || !string.IsNullOrEmpty(_projectVersion)) {
			sectionData.Add((0x0008u, BuildMetadataSection()));
		}

		// Now build the file with section table
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);

		// Header (32 bytes)
		writer.Write(Magic); // 8 bytes
		writer.Write(FormatVersion); // 2 bytes (offset 8)

		// Flags
		var flags = PansyFlags.None;
		if (_enableCompression) {
			flags |= PansyFlags.Compressed;
		}
		if (_sourceMapEntries.Count > 0) {
			flags |= PansyFlags.HasSourceMap;
		}
		if (_crossRefs.Count > 0) {
			flags |= PansyFlags.HasCrossRefs;
		}
		if (_cpuStateEntries.Count > 0) {
			flags |= PansyFlags.HasCpuState;
		}
		writer.Write((ushort)flags); // 2 bytes (offset 10)

		// Platform and padding
		writer.Write(_platform); // 1 byte (offset 12)
		writer.Write((byte)0); // 1 byte reserved (offset 13)
		writer.Write((ushort)0); // 2 bytes padding (offset 14)

		// ROM info
		writer.Write(_romSize); // 4 bytes (offset 16)
		writer.Write(_romCrc32); // 4 bytes (offset 20)

		// Section count
		writer.Write((uint)sectionData.Count); // 4 bytes (offset 24)

		// Reserved
		writer.Write((uint)0); // 4 bytes (offset 28)

		// Section table starts at offset 32
		// Each entry: Type (4), Offset (4), CompSize (4), UncompSize (4) = 16 bytes

		// Build compressed section data if compression is enabled
		var processedSections = new List<(uint Type, byte[] WrittenData, uint UncompSize)>();
		foreach (var (type, data) in sectionData) {
			if (_enableCompression && data.Length > 0) {
				using var compMs = new MemoryStream();
				using (var deflate = new DeflateStream(compMs, CompressionLevel.Optimal, leaveOpen: true)) {
					deflate.Write(data, 0, data.Length);
				}
				var compressedData = compMs.ToArray();
				// Only use compressed version if it's actually smaller
				if (compressedData.Length < data.Length) {
					processedSections.Add((type, compressedData, (uint)data.Length));
				} else {
					processedSections.Add((type, data, (uint)data.Length));
				}
			} else {
				processedSections.Add((type, data, (uint)data.Length));
			}
		}

		var dataOffset = (uint)(32 + processedSections.Count * 16);

		// Write section table
		foreach (var (type, writtenData, uncompSize) in processedSections) {
			writer.Write(type); // Type
			writer.Write(dataOffset); // Offset
			writer.Write((uint)writtenData.Length); // CompSize
			writer.Write(uncompSize); // UncompSize
			dataOffset += (uint)writtenData.Length;
		}

		// Write section data
		foreach (var (_, writtenData, _) in processedSections) {
			writer.Write(writtenData);
		}

		return ms.ToArray();
	}

	private byte[] BuildCodeDataMap() {
		if (_codeOffsets.Count == 0 && _dataOffsets.Count == 0 &&
			_jumpTargets.Count == 0 && _subEntryPoints.Count == 0 &&
			_opcodeOffsets.Count == 0 && _drawnOffsets.Count == 0 &&
			_readOffsets.Count == 0 && _indirectOffsets.Count == 0) {
			return [];
		}

		// Determine the size needed — manual max avoids LINQ allocation
		var maxOffset = 0u;
		foreach (var o in _codeOffsets) if (o > maxOffset) maxOffset = o;
		foreach (var o in _dataOffsets) if (o > maxOffset) maxOffset = o;
		foreach (var o in _jumpTargets) if (o > maxOffset) maxOffset = o;
		foreach (var o in _subEntryPoints) if (o > maxOffset) maxOffset = o;
		foreach (var o in _opcodeOffsets) if (o > maxOffset) maxOffset = o;
		foreach (var o in _drawnOffsets) if (o > maxOffset) maxOffset = o;
		foreach (var o in _readOffsets) if (o > maxOffset) maxOffset = o;
		foreach (var o in _indirectOffsets) if (o > maxOffset) maxOffset = o;

		var map = new byte[maxOffset + 1];

		// Set flags for each offset
		foreach (var offset in _codeOffsets) {
			map[offset] |= 0x01; // FLAG_CODE
		}
		foreach (var offset in _dataOffsets) {
			map[offset] |= 0x02; // FLAG_DATA
		}
		foreach (var offset in _jumpTargets) {
			map[offset] |= 0x04; // FLAG_JUMP_TARGET
		}
		foreach (var offset in _subEntryPoints) {
			map[offset] |= 0x08; // FLAG_SUB_ENTRY
		}
		foreach (var offset in _opcodeOffsets) {
			map[offset] |= 0x10; // FLAG_OPCODE
		}
		foreach (var offset in _drawnOffsets) {
			map[offset] |= 0x20; // FLAG_DRAWN
		}
		foreach (var offset in _readOffsets) {
			map[offset] |= 0x40; // FLAG_READ
		}
		foreach (var offset in _indirectOffsets) {
			map[offset] |= 0x80; // FLAG_INDIRECT
		}

		return map;
	}

	private byte[] BuildMetadataSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		WriteString(writer, _projectName);
		WriteString(writer, _author);
		WriteString(writer, _projectVersion);
		return ms.ToArray();
	}

	private byte[] BuildSymbolsSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		// No count - loader reads until EOF
		foreach (var (addr, entries) in _symbols.OrderBy(x => x.Key)) {
			foreach (var (name, type) in entries) {
				writer.Write(addr); // Address (uint32)
				writer.Write((byte)type); // Type: from SymbolType enum
				writer.Write((byte)0); // Flags
				var nameBytes = Encoding.UTF8.GetBytes(name);
				writer.Write((ushort)nameBytes.Length); // NameLength
				writer.Write(nameBytes); // Name
				writer.Write((ushort)0); // ValueLength (no value for labels)
			}
		}
		return ms.ToArray();
	}

	private byte[] BuildCommentsSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		// No count - loader reads until EOF
		foreach (var (addr, entries) in _comments.OrderBy(x => x.Key)) {
			foreach (var (comment, commentType) in entries) {
				writer.Write(addr); // Address (uint32)
				writer.Write(commentType); // Type: from comment type parameter
				var commentBytes = Encoding.UTF8.GetBytes(comment);
				writer.Write((ushort)commentBytes.Length); // Length
				writer.Write(commentBytes); // Text
			}
		}
		return ms.ToArray();
	}

	private byte[] BuildMemoryRegionsSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		// No count - loader reads until EOF
		foreach (var region in _memoryRegions) {
			writer.Write(region.Start); // Start address (uint32)
			writer.Write(region.End); // End address (uint32)
			writer.Write((byte)region.Type); // Type (byte)
			writer.Write(region.Bank); // Bank (byte)
			writer.Write((ushort)0); // Flags (reserved)
			var nameBytes = Encoding.UTF8.GetBytes(region.Name);
			writer.Write((ushort)nameBytes.Length); // NameLength
			writer.Write(nameBytes); // Name
		}
		return ms.ToArray();
	}

	private byte[] BuildCrossReferencesSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		// No count - loader reads until EOF
		foreach (var xref in _crossRefs) {
			writer.Write(xref.From); // From address (uint32)
			writer.Write(xref.To); // To address (uint32)
			writer.Write((byte)xref.Type); // Type (CrossRefType)
		}
		return ms.ToArray();
	}

	private byte[] BuildBookmarksSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		foreach (var bookmark in _bookmarks) {
			writer.Write(bookmark.Address); // Address (uint32)
			writer.Write(bookmark.Color); // Color index (byte)
			WriteString(writer, bookmark.Name); // Name (length-prefixed string)
		}
		return ms.ToArray();
	}

	private byte[] BuildDataTypesSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		foreach (var dt in _dataTypes) {
			writer.Write(dt.Address); // Address (uint32)
			writer.Write(dt.Length); // Length (uint32)
			writer.Write(dt.ElementSize); // ElementSize (uint16)
			writer.Write(dt.ElementCount); // ElementCount (uint16)
			writer.Write((byte)dt.Type); // Type (byte)
			WriteString(writer, dt.Name); // Name (length-prefixed string)
		}
		return ms.ToArray();
	}

	private byte[] BuildSourceMapSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);

		// Write source file table: count + paths
		writer.Write((ushort)_sourceFiles.Count);
		foreach (var path in _sourceFiles) {
			WriteString(writer, path);
		}

		// Write source map entries
		foreach (var entry in _sourceMapEntries) {
			writer.Write(entry.RomAddress);
			writer.Write(entry.FileIndex);
			writer.Write(entry.Line);
			writer.Write(entry.Column);
		}

		return ms.ToArray();
	}

	private byte[] BuildCpuStateSection() {
		using var ms = new MemoryStream(_cpuStateEntries.Count * 9);
		using var writer = new BinaryWriter(ms);
		foreach (var entry in _cpuStateEntries) {
			writer.Write(entry.Address); // Address (uint32)
			writer.Write(entry.Flags); // Flags (byte) - bit 0 = XFlag, bit 1 = MFlag
			writer.Write(entry.DataBank); // DataBank (byte)
			writer.Write(entry.DirectPage); // DirectPage (uint16)
			writer.Write((byte)entry.Mode); // CpuMode (byte)
		}
		return ms.ToArray();
	}

	private static void WriteString(BinaryWriter writer, string value) {
		var bytes = Encoding.UTF8.GetBytes(value);
		writer.Write((ushort)bytes.Length);
		writer.Write(bytes);
	}
}
