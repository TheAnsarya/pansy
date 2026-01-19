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
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);

		// Header
		writer.Write(Magic);
		writer.Write(FormatVersion);

		// Flags
		var flags = PansyFlags.None;
		if (_enableCompression) {
			flags |= PansyFlags.Compressed;
		}
		writer.Write((ushort)flags);

		// Platform and ROM info
		writer.Write(_platform);
		writer.Write((byte)0); // Reserved
		writer.Write(_romSize);
		writer.Write(_romCrc32);

		// Section count (will be updated)
		var sectionCountPos = ms.Position;
		writer.Write((ushort)0);

		// Write sections
		var sectionCount = 0;

		// Metadata section
		if (!string.IsNullOrEmpty(_projectName) || !string.IsNullOrEmpty(_author) || !string.IsNullOrEmpty(_projectVersion)) {
			WriteSectionHeader(writer, SectionType.Metadata);
			WriteString(writer, _projectName);
			WriteString(writer, _author);
			WriteString(writer, _projectVersion);
			sectionCount++;
		}

		// Symbols section
		if (_symbols.Count > 0) {
			WriteSectionHeader(writer, SectionType.Symbols);
			writer.Write(_symbols.Count);
			foreach (var (addr, name) in _symbols.OrderBy(x => x.Key)) {
				writer.Write(addr);
				WriteString(writer, name);
			}
			sectionCount++;
		}

		// Comments section
		if (_comments.Count > 0) {
			WriteSectionHeader(writer, SectionType.Comments);
			writer.Write(_comments.Count);
			foreach (var (addr, comment) in _comments.OrderBy(x => x.Key)) {
				writer.Write(addr);
				WriteString(writer, comment);
			}
			sectionCount++;
		}

		// Code offsets section
		if (_codeOffsets.Count > 0) {
			WriteSectionHeader(writer, SectionType.CodeOffsets);
			writer.Write(_codeOffsets.Count);
			foreach (var addr in _codeOffsets.OrderBy(x => x)) {
				writer.Write(addr);
			}
			sectionCount++;
		}

		// Data offsets section
		if (_dataOffsets.Count > 0) {
			WriteSectionHeader(writer, SectionType.DataOffsets);
			writer.Write(_dataOffsets.Count);
			foreach (var addr in _dataOffsets.OrderBy(x => x)) {
				writer.Write(addr);
			}
			sectionCount++;
		}

		// Jump targets section
		if (_jumpTargets.Count > 0) {
			WriteSectionHeader(writer, SectionType.JumpTargets);
			writer.Write(_jumpTargets.Count);
			foreach (var addr in _jumpTargets.OrderBy(x => x)) {
				writer.Write(addr);
			}
			sectionCount++;
		}

		// Subroutines section
		if (_subEntryPoints.Count > 0) {
			WriteSectionHeader(writer, SectionType.SubEntryPoints);
			writer.Write(_subEntryPoints.Count);
			foreach (var addr in _subEntryPoints.OrderBy(x => x)) {
				writer.Write(addr);
			}
			sectionCount++;
		}

		// Memory regions section
		if (_memoryRegions.Count > 0) {
			WriteSectionHeader(writer, SectionType.MemoryRegions);
			writer.Write(_memoryRegions.Count);
			foreach (var region in _memoryRegions) {
				writer.Write(region.Start);
				writer.Write(region.End);
				writer.Write(region.Bank);
				writer.Write((byte)region.Type);
				WriteString(writer, region.Name);
			}
			sectionCount++;
		}

		// Cross-references section
		if (_crossRefs.Count > 0) {
			WriteSectionHeader(writer, SectionType.CrossReferences);
			writer.Write(_crossRefs.Count);
			foreach (var xref in _crossRefs) {
				writer.Write((byte)xref.Type);
				writer.Write(xref.From);
				writer.Write(xref.To);
			}
			sectionCount++;
		}

		// Update section count
		var endPos = ms.Position;
		ms.Position = sectionCountPos;
		writer.Write((ushort)sectionCount);
		ms.Position = endPos;

		var data = ms.ToArray();

		// Apply compression if enabled
		if (_enableCompression) {
			return CompressData(data);
		}

		return data;
	}

	private static void WriteSectionHeader(BinaryWriter writer, SectionType type) {
		writer.Write((ushort)type);
		// Section size will be implicit from content
	}

	private static void WriteString(BinaryWriter writer, string value) {
		var bytes = Encoding.UTF8.GetBytes(value);
		writer.Write((ushort)bytes.Length);
		writer.Write(bytes);
	}

	private static byte[] CompressData(byte[] data) {
		using var output = new MemoryStream();
		using (var deflate = new DeflateStream(output, CompressionLevel.Optimal)) {
			deflate.Write(data, 0, data.Length);
		}
		return output.ToArray();
	}

	[Flags]
	private enum PansyFlags : ushort {
		None = 0,
		Compressed = 1 << 0
	}

	private enum SectionType : ushort {
		Metadata = 0x0001,
		Symbols = 0x0002,
		Comments = 0x0003,
		CodeOffsets = 0x0004,
		DataOffsets = 0x0005,
		JumpTargets = 0x0006,
		SubEntryPoints = 0x0007,
		MemoryRegions = 0x0008,
		CrossReferences = 0x0009
	}
}
