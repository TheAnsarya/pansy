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
	private readonly Dictionary<uint, string> _symbols = [];
	private readonly Dictionary<uint, string> _comments = [];
	private readonly HashSet<uint> _codeOffsets = [];
	private readonly HashSet<uint> _dataOffsets = [];
	private readonly HashSet<uint> _jumpTargets = [];
	private readonly HashSet<uint> _subEntryPoints = [];
	private readonly List<MemoryRegion> _memoryRegions = [];
	private readonly List<CrossReference> _crossRefs = [];
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

	/// <summary>Adds a symbol at the specified address.</summary>
	public void AddSymbol(uint address, string name) {
		_symbols[address] = name;
	}

	/// <summary>Adds a comment at the specified address.</summary>
	public void AddComment(uint address, string comment) {
		_comments[address] = comment;
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

	/// <summary>Adds a memory region.</summary>
	public void AddMemoryRegion(MemoryRegion region) {
		_memoryRegions.Add(region);
	}

	/// <summary>Adds a cross-reference.</summary>
	public void AddCrossReference(CrossReference xref) {
		_crossRefs.Add(xref);
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

		// Cross-references section
		if (_crossRefs.Count > 0) {
			sectionData.Add((0x0006u, BuildCrossReferencesSection()));
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
		var dataOffset = (uint)(32 + sectionData.Count * 16);

		// Write section table
		foreach (var (type, data) in sectionData) {
			writer.Write(type); // Type
			writer.Write(dataOffset); // Offset
			writer.Write((uint)data.Length); // CompSize (uncompressed for now)
			writer.Write((uint)data.Length); // UncompSize
			dataOffset += (uint)data.Length;
		}

		// Write section data
		foreach (var (_, data) in sectionData) {
			writer.Write(data);
		}

		return ms.ToArray();
	}

	private byte[] BuildCodeDataMap() {
		if (_codeOffsets.Count == 0 && _dataOffsets.Count == 0 &&
			_jumpTargets.Count == 0 && _subEntryPoints.Count == 0) {
			return [];
		}

		// Determine the size needed
		var maxOffset = 0u;
		if (_codeOffsets.Count > 0) maxOffset = Math.Max(maxOffset, _codeOffsets.Max());
		if (_dataOffsets.Count > 0) maxOffset = Math.Max(maxOffset, _dataOffsets.Max());
		if (_jumpTargets.Count > 0) maxOffset = Math.Max(maxOffset, _jumpTargets.Max());
		if (_subEntryPoints.Count > 0) maxOffset = Math.Max(maxOffset, _subEntryPoints.Max());

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
		foreach (var (addr, name) in _symbols.OrderBy(x => x.Key)) {
			writer.Write(addr); // Address (uint32)
			writer.Write((byte)1); // Type: Label
			writer.Write((byte)0); // Flags
			var nameBytes = Encoding.UTF8.GetBytes(name);
			writer.Write((ushort)nameBytes.Length); // NameLength
			writer.Write(nameBytes); // Name
			writer.Write((ushort)0); // ValueLength (no value for labels)
		}
		return ms.ToArray();
	}

	private byte[] BuildCommentsSection() {
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		// No count - loader reads until EOF
		foreach (var (addr, comment) in _comments.OrderBy(x => x.Key)) {
			writer.Write(addr); // Address (uint32)
			writer.Write((byte)1); // Type: inline comment
			var commentBytes = Encoding.UTF8.GetBytes(comment);
			writer.Write((ushort)commentBytes.Length); // Length
			writer.Write(commentBytes); // Text
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

	private static void WriteString(BinaryWriter writer, string value) {
		var bytes = Encoding.UTF8.GetBytes(value);
		writer.Write((ushort)bytes.Length);
		writer.Write(bytes);
	}

	[Flags]
	private enum PansyFlags : ushort {
		None = 0,
		Compressed = 1 << 0
	}
}
