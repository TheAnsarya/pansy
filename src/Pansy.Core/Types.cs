// ============================================================================
// Types.cs - Pansy Core Types
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

namespace Pansy.Core;

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
/// Information about a section in the Pansy file.
/// </summary>
public record SectionInfo(uint Type, uint Offset, uint CompressedSize, uint UncompressedSize);

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
}
