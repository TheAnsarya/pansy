// ============================================================================
// Types.cs - Pansy Core Types
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

namespace Pansy.Core;

/// <summary>
/// Symbol entry types.
/// </summary>
public enum SymbolType : byte {
	/// <summary>Code or data label.</summary>
	Label = 1,
	/// <summary>Named constant.</summary>
	Constant = 2,
	/// <summary>Enumeration member.</summary>
	Enum = 3,
	/// <summary>Structure definition.</summary>
	Struct = 4,
	/// <summary>Macro definition.</summary>
	Macro = 5,
	/// <summary>Local label.</summary>
	Local = 6,
	/// <summary>Anonymous label.</summary>
	Anonymous = 7,
	/// <summary>Interrupt vector (NMI, IRQ, RESET, etc.).</summary>
	InterruptVector = 8,
	/// <summary>Function entry point with known signature.</summary>
	Function = 9,
}

/// <summary>
/// Cross-reference types.
/// </summary>
public enum CrossRefType : byte {
	/// <summary>Subroutine call.</summary>
	Jsr = 1,
	/// <summary>Jump.</summary>
	Jmp = 2,
	/// <summary>Branch.</summary>
	Branch = 3,
	/// <summary>Read access.</summary>
	Read = 4,
	/// <summary>Write access.</summary>
	Write = 5,
}

/// <summary>
/// Comment types.
/// </summary>
public enum CommentType : byte {
	/// <summary>Inline comment (on same line as code).</summary>
	Inline = 1,
	/// <summary>Block comment (above the line).</summary>
	Block = 2,
	/// <summary>TODO/note comment.</summary>
	Todo = 3,
}

/// <summary>
/// A symbol entry with name and type.
/// </summary>
public record SymbolEntry(string Name, SymbolType Type);

/// <summary>
/// A comment entry with text and type.
/// </summary>
public record CommentEntry(string Text, CommentType Type);

/// <summary>
/// Information about a section in the Pansy file.
/// </summary>
public record SectionInfo(uint Type, uint Offset, uint CompressedSize, uint UncompressedSize);

/// <summary>
/// Memory region types.
/// </summary>
public enum MemoryRegionType : byte {
	/// <summary>Unknown or unspecified region type.</summary>
	Unknown = 0,
	/// <summary>Read-only memory (ROM).</summary>
	ROM = 1,
	/// <summary>Random access memory (RAM).</summary>
	RAM = 2,
	/// <summary>Video RAM.</summary>
	VRAM = 3,
	/// <summary>I/O registers.</summary>
	IO = 4,
	/// <summary>Save RAM (battery-backed).</summary>
	SRAM = 5,
	/// <summary>Work RAM.</summary>
	WRAM = 6,
	/// <summary>Open bus / unmapped.</summary>
	OpenBus = 7,
	/// <summary>Mirror of another region.</summary>
	Mirror = 8,
}

/// <summary>
/// Memory region definition.
/// </summary>
public record MemoryRegion(uint Start, uint End, byte Type, byte Bank, string Name);

/// <summary>
/// Cross-reference entry.
/// </summary>
public record CrossReference(uint From, uint To, CrossRefType Type);

/// <summary>
/// Pansy file flags.
/// </summary>
[Flags]
public enum PansyFlags : ushort {
	/// <summary>No flags set.</summary>
	None = 0,
	/// <summary>File content is compressed with DEFLATE.</summary>
	Compressed = 1 << 0,
	/// <summary>File contains source map section.</summary>
	HasSourceMap = 1 << 1,
	/// <summary>File contains cross-references section.</summary>
	HasCrossRefs = 1 << 2,
	/// <summary>File has detailed CDL data.</summary>
	DetailedCdl = 1 << 3,
}
