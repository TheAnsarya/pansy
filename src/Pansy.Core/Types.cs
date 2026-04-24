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
/// Grouped cross-reference entry representing one source with multiple targets.
/// </summary>
public record MultiTargetCrossReference(uint From, CrossRefType Type, IReadOnlyList<uint> Targets);

/// <summary>
/// A user-placed bookmark for quick navigation during analysis.
/// </summary>
/// <param name="Address">The bookmarked address.</param>
/// <param name="Name">Bookmark label/name.</param>
/// <param name="Color">Optional color index (0 = default).</param>
public record Bookmark(uint Address, string Name, byte Color = 0);

/// <summary>
/// Data element type for typed data annotations.
/// </summary>
public enum DataElementType : byte {
	/// <summary>Single byte.</summary>
	Byte = 1,
	/// <summary>16-bit word.</summary>
	Word = 2,
	/// <summary>32-bit long.</summary>
	Long = 3,
	/// <summary>Pointer (address).</summary>
	Pointer = 4,
	/// <summary>String/text data.</summary>
	String = 5,
}

/// <summary>
/// A data type annotation marking a range of bytes with structural information.
/// </summary>
/// <param name="Address">Start address of the data.</param>
/// <param name="Length">Total length in bytes.</param>
/// <param name="ElementSize">Size of each element in bytes.</param>
/// <param name="ElementCount">Number of elements.</param>
/// <param name="Type">The element type.</param>
/// <param name="Name">Optional name/label for this data.</param>
public record DataTypeEntry(uint Address, uint Length, ushort ElementSize, ushort ElementCount, DataElementType Type, string Name);

/// <summary>
/// A source map entry linking a ROM address to a source file location.
/// </summary>
/// <param name="RomAddress">Address in the ROM.</param>
/// <param name="FileIndex">Index into the source file list.</param>
/// <param name="Line">1-based line number in the source file.</param>
/// <param name="Column">1-based column number in the source file.</param>
public record SourceMapEntry(uint RomAddress, ushort FileIndex, ushort Line, ushort Column);

/// <summary>
/// CPU execution mode for per-address state tracking.
/// Captures the processor mode at each address for correct disassembly.
/// </summary>
public enum CpuMode : byte {
	/// <summary>65816 native mode (16-bit capable).</summary>
	Native65816 = 0,
	/// <summary>65816 emulation mode (6502-compatible).</summary>
	Emulation6502 = 1,
	/// <summary>ARM mode (32-bit instructions).</summary>
	ARM = 2,
	/// <summary>THUMB mode (16-bit instructions).</summary>
	THUMB = 3,
	/// <summary>M68000 execution mode (Genesis main CPU context).</summary>
	M68000 = 4,
	/// <summary>Z80 execution mode (Genesis audio/control CPU context).</summary>
	Z80 = 5,
}

/// <summary>
/// Per-address CPU state entry for accurate disassembly.
/// Captures processor register state at specific addresses, particularly
/// important for SNES 65816 where instruction lengths depend on mode flags.
/// </summary>
/// <param name="Address">The CPU address.</param>
/// <param name="Flags">Bit flags: bit 0 = XFlag (8-bit index), bit 1 = MFlag (8-bit accumulator).</param>
/// <param name="DataBank">Data bank register (DBR) value.</param>
/// <param name="DirectPage">Direct page offset.</param>
/// <param name="Mode">CPU execution mode.</param>
public record CpuStateEntry(uint Address, byte Flags, byte DataBank, ushort DirectPage, CpuMode Mode);

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
	/// <summary>File contains CPU state section.</summary>
	HasCpuState = 1 << 4,
}

/// <summary>
/// A default hardware register symbol with name, description, and type metadata.
/// Used by <see cref="PlatformDefaults"/> for platform-specific register databases.
/// </summary>
/// <param name="Name">Register name (e.g., "PPUCTRL", "INIDISP").</param>
/// <param name="Description">Human-readable description of the register's purpose.</param>
/// <param name="Type">Symbol type classification.</param>
/// <param name="BitFields">Optional bit field descriptions for the register.</param>
public record DefaultSymbol(string Name, string Description, SymbolType Type, BitField[]? BitFields = null);

/// <summary>
/// Describes a single bit field within a hardware register.
/// </summary>
/// <param name="Bit">The starting bit position (0 = LSB).</param>
/// <param name="Width">The number of bits in this field.</param>
/// <param name="Name">Short name for the bit field.</param>
/// <param name="Description">Human-readable description of the field's purpose.</param>
public record BitField(int Bit, int Width, string Name, string Description);

/// <summary>
/// Source of a label, used by <see cref="LabelMergeEngine"/> for priority resolution.
/// Higher numeric value = lower priority.
/// </summary>
public enum LabelSource : byte {
	/// <summary>User-defined label (highest priority, never overwritten).</summary>
	User = 0,
	/// <summary>Hardware register name from PlatformDefaults (authoritative).</summary>
	HardwareRegister = 1,
	/// <summary>Label sourced from internet databases (DataCrystal, NESdev, etc.).</summary>
	InternetDatabase = 2,
	/// <summary>Auto-generated labels from CDL analysis (sub_, loc_, etc.).</summary>
	AutoGenerated = 3,
	/// <summary>Pattern-detected labels (str_, ptrtbl_, tiles_).</summary>
	PatternDetected = 4,
}

/// <summary>
/// A label with source provenance and metadata for merge conflict resolution.
/// </summary>
/// <param name="Address">Address this label applies to.</param>
/// <param name="Name">The label name.</param>
/// <param name="Type">Symbol type classification.</param>
/// <param name="Source">Where this label came from.</param>
/// <param name="Description">Optional description (from HW register databases, etc.).</param>
public record MergedLabel(
	uint Address,
	string Name,
	SymbolType Type,
	LabelSource Source,
	string? Description = null);

/// <summary>
/// A conflict that occurred during label merging.
/// </summary>
/// <param name="Address">Address where the conflict occurred.</param>
/// <param name="Winner">The label that won the conflict (higher priority).</param>
/// <param name="Loser">The label that was discarded.</param>
public record MergeConflict(uint Address, MergedLabel Winner, MergedLabel Loser);

/// <summary>
/// A suggested label name from an AI/LLM source.
/// </summary>
/// <param name="Address">The address this suggestion applies to.</param>
/// <param name="SuggestedName">The suggested label name.</param>
/// <param name="Confidence">Confidence score from 0.0 (low) to 1.0 (high).</param>
/// <param name="Reasoning">Brief explanation of why this name was suggested.</param>
public record LabelSuggestion(uint Address, string SuggestedName, double Confidence, string? Reasoning = null);

/// <summary>
/// Context provided to an AI label suggester for generating name suggestions.
/// </summary>
/// <param name="Platform">Pansy platform ID.</param>
/// <param name="RomData">The raw ROM data.</param>
/// <param name="Metadata">Loaded Pansy metadata with existing labels and CDL data.</param>
/// <param name="TargetAddresses">Addresses to generate suggestions for.</param>
public record SuggestionContext(byte Platform, byte[] RomData, PansyLoader Metadata, IReadOnlyList<uint> TargetAddresses);

/// <summary>
/// Interface for AI-powered label name suggestion providers.
/// Implementations may use local models (Ollama), cloud APIs, or pattern matching.
/// </summary>
public interface ILabelSuggester {
	/// <summary>
	/// Suggest label names for the specified target addresses.
	/// </summary>
	Task<List<LabelSuggestion>> SuggestLabelsAsync(
		SuggestionContext context,
		CancellationToken ct = default);
}
