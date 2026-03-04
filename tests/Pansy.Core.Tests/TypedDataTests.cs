// ============================================================================
// TypedDataTests.cs - Tests for Typed Symbol/Comment Preservation
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;

namespace Pansy.Core.Tests;

/// <summary>
/// Tests that PansyLoader preserves SymbolType and CommentType information
/// through write → load roundtrips, and that the new typed APIs work correctly.
/// </summary>
public class TypedDataTests {
	#region SymbolEntry Roundtrip Tests

	[Fact]
	public void SymbolEntries_PreservesAllTypes() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "Reset", SymbolType.Label);
		writer.AddSymbol(0x8100, "MAX_HP", SymbolType.Constant);
		writer.AddSymbol(0x8200, "Direction", SymbolType.Enum);
		writer.AddSymbol(0x8300, "Actor", SymbolType.Struct);
		writer.AddSymbol(0x8400, "INIT_PPU", SymbolType.Macro);
		writer.AddSymbol(0x8500, ".loop", SymbolType.Local);
		writer.AddSymbol(0x8600, "+", SymbolType.Anonymous);
		writer.AddSymbol(0xfffa, "NMI_VECTOR", SymbolType.InterruptVector);
		writer.AddSymbol(0x8700, "UpdatePlayer", SymbolType.Function);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Check SymbolEntries dictionary
		Assert.Equal(9, loader.SymbolEntries.Count);
		Assert.Equal(SymbolType.Label, loader.SymbolEntries[0x8000].Type);
		Assert.Equal("Reset", loader.SymbolEntries[0x8000].Name);
		Assert.Equal(SymbolType.Constant, loader.SymbolEntries[0x8100].Type);
		Assert.Equal(SymbolType.Enum, loader.SymbolEntries[0x8200].Type);
		Assert.Equal(SymbolType.Struct, loader.SymbolEntries[0x8300].Type);
		Assert.Equal(SymbolType.Macro, loader.SymbolEntries[0x8400].Type);
		Assert.Equal(SymbolType.Local, loader.SymbolEntries[0x8500].Type);
		Assert.Equal(SymbolType.Anonymous, loader.SymbolEntries[0x8600].Type);
		Assert.Equal(SymbolType.InterruptVector, loader.SymbolEntries[0xfffa].Type);
		Assert.Equal(SymbolType.Function, loader.SymbolEntries[0x8700].Type);
	}

	[Fact]
	public void GetSymbolEntry_ReturnsTypedEntry() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "Reset", SymbolType.InterruptVector);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var entry = loader.GetSymbolEntry(0x8000);
		Assert.NotNull(entry);
		Assert.Equal("Reset", entry.Name);
		Assert.Equal(SymbolType.InterruptVector, entry.Type);
	}

	[Fact]
	public void GetSymbolEntry_ReturnsNullForMissingAddress() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "Reset", SymbolType.Label);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Null(loader.GetSymbolEntry(0x9999));
	}

	[Fact]
	public void GetSymbolType_ReturnsCorrectType() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "Reset", SymbolType.InterruptVector);
		writer.AddSymbol(0x8100, "MAX_HP", SymbolType.Constant);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0x8000));
		Assert.Equal(SymbolType.Constant, loader.GetSymbolType(0x8100));
		Assert.Null(loader.GetSymbolType(0x9999));
	}

	[Fact]
	public void Symbols_BackwardCompatible_ReturnsNameOnly() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "Reset", SymbolType.InterruptVector);
		writer.AddSymbol(0x8100, "Loop", SymbolType.Label);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Old API still works — returns name string
		Assert.Equal("Reset", loader.Symbols[0x8000]);
		Assert.Equal("Loop", loader.Symbols[0x8100]);
		Assert.Equal("Reset", loader.GetSymbol(0x8000));
	}

	#endregion

	#region CommentEntry Roundtrip Tests

	[Fact]
	public void CommentEntries_PreservesAllTypes() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "Initialize PPU", (byte)CommentType.Inline);
		writer.AddComment(0x8100, "Main game loop\nThis is the entry point", (byte)CommentType.Block);
		writer.AddComment(0x8200, "TODO: optimize this loop", (byte)CommentType.Todo);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(3, loader.CommentEntries.Count);
		Assert.Equal(CommentType.Inline, loader.CommentEntries[0x8000].Type);
		Assert.Equal("Initialize PPU", loader.CommentEntries[0x8000].Text);
		Assert.Equal(CommentType.Block, loader.CommentEntries[0x8100].Type);
		Assert.Contains("Main game loop", loader.CommentEntries[0x8100].Text);
		Assert.Equal(CommentType.Todo, loader.CommentEntries[0x8200].Type);
	}

	[Fact]
	public void GetCommentEntry_ReturnsTypedEntry() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "TODO: fix later", (byte)CommentType.Todo);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var entry = loader.GetCommentEntry(0x8000);
		Assert.NotNull(entry);
		Assert.Equal("TODO: fix later", entry.Text);
		Assert.Equal(CommentType.Todo, entry.Type);
	}

	[Fact]
	public void GetCommentEntry_ReturnsNullForMissingAddress() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "test", (byte)CommentType.Inline);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Null(loader.GetCommentEntry(0x9999));
	}

	[Fact]
	public void GetCommentType_ReturnsCorrectType() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "inline", (byte)CommentType.Inline);
		writer.AddComment(0x8100, "block", (byte)CommentType.Block);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(CommentType.Inline, loader.GetCommentType(0x8000));
		Assert.Equal(CommentType.Block, loader.GetCommentType(0x8100));
		Assert.Null(loader.GetCommentType(0x9999));
	}

	[Fact]
	public void Comments_BackwardCompatible_ReturnsTextOnly() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "test comment", (byte)CommentType.Block);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Old API still works — returns text string
		Assert.Equal("test comment", loader.Comments[0x8000]);
		Assert.Equal("test comment", loader.GetComment(0x8000));
	}

	[Fact]
	public void AddComment_DefaultType_IsInline() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "default type comment"); // no type = inline (1)

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(CommentType.Inline, loader.CommentEntries[0x8000].Type);
	}

	#endregion

	#region InterruptVector Tests

	[Fact]
	public void InterruptVector_NES_Roundtrip() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0xfffa, "NMI", SymbolType.InterruptVector);
		writer.AddSymbol(0xfffc, "RESET", SymbolType.InterruptVector);
		writer.AddSymbol(0xfffe, "IRQ", SymbolType.InterruptVector);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0xfffa));
		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0xfffc));
		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0xfffe));
		Assert.Equal("NMI", loader.GetSymbol(0xfffa));
		Assert.Equal("RESET", loader.GetSymbol(0xfffc));
		Assert.Equal("IRQ", loader.GetSymbol(0xfffe));
	}

	[Fact]
	public void Function_Roundtrip() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_GBA, RomSize = 0x100000 };
		writer.AddSymbol(0x08000000, "main", SymbolType.Function);
		writer.AddSymbol(0x08001000, "vblank_handler", SymbolType.Function);
		writer.AddSymbol(0x08002000, "MAX_ENEMIES", SymbolType.Constant);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(SymbolType.Function, loader.GetSymbolType(0x08000000));
		Assert.Equal(SymbolType.Function, loader.GetSymbolType(0x08001000));
		Assert.Equal(SymbolType.Constant, loader.GetSymbolType(0x08002000));
	}

	#endregion

	#region Multiple Symbols Per Address Tests

	[Fact]
	public void MultipleSymbols_SameAddress_AllPreserved() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "Reset", SymbolType.InterruptVector);
		writer.AddSymbol(0x8000, "Main", SymbolType.Function);
		writer.AddSymbol(0x8000, "ProgramStart", SymbolType.Label);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var entries = loader.GetSymbolEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Equal(3, entries.Count);
		Assert.Equal("Reset", entries[0].Name);
		Assert.Equal(SymbolType.InterruptVector, entries[0].Type);
		Assert.Equal("Main", entries[1].Name);
		Assert.Equal(SymbolType.Function, entries[1].Type);
		Assert.Equal("ProgramStart", entries[2].Name);
		Assert.Equal(SymbolType.Label, entries[2].Type);
	}

	[Fact]
	public void MultipleSymbols_BackwardCompat_ReturnsFirst() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "First", SymbolType.Label);
		writer.AddSymbol(0x8000, "Second", SymbolType.Function);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// Backward-compat APIs return first entry
		Assert.Equal("First", loader.GetSymbol(0x8000));
		Assert.Equal(SymbolType.Label, loader.GetSymbolType(0x8000));
		var entry = loader.GetSymbolEntry(0x8000);
		Assert.NotNull(entry);
		Assert.Equal("First", entry.Name);

		// SymbolEntries dict returns first entry per address
		Assert.Equal("First", loader.SymbolEntries[0x8000].Name);
		Assert.Equal("First", loader.Symbols[0x8000]);
	}

	[Fact]
	public void MultipleSymbols_AllSymbolEntries_ReturnsLists() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "A", SymbolType.Label);
		writer.AddSymbol(0x8000, "B", SymbolType.Function);
		writer.AddSymbol(0x8100, "C", SymbolType.Constant);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		// AllSymbolEntries returns lists
		Assert.Equal(2, loader.AllSymbolEntries.Count); // 2 addresses
		Assert.Equal(2, loader.AllSymbolEntries[0x8000].Count); // 2 symbols at 0x8000
		Assert.Single(loader.AllSymbolEntries[0x8100]); // 1 symbol at 0x8100

		// SymbolEntries count = number of addresses (backward compat)
		Assert.Equal(2, loader.SymbolEntries.Count);
	}

	[Fact]
	public void MultipleSymbols_GetSymbolEntries_NullForMissing() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "Exists", SymbolType.Label);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Null(loader.GetSymbolEntries(0x9999));
	}

	[Fact]
	public void MultipleSymbols_SingleEntry_WorksAsListOfOne() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddSymbol(0x8000, "Only", SymbolType.Label);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var entries = loader.GetSymbolEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Single(entries);
		Assert.Equal("Only", entries[0].Name);
	}

	#endregion

	#region Multiple Comments Per Address Tests

	[Fact]
	public void MultipleComments_SameAddress_AllPreserved() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "Initialize PPU", (byte)CommentType.Block);
		writer.AddComment(0x8000, "Sets up rendering", (byte)CommentType.Inline);
		writer.AddComment(0x8000, "TODO: optimize", (byte)CommentType.Todo);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		var entries = loader.GetCommentEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Equal(3, entries.Count);
		Assert.Equal("Initialize PPU", entries[0].Text);
		Assert.Equal(CommentType.Block, entries[0].Type);
		Assert.Equal("Sets up rendering", entries[1].Text);
		Assert.Equal(CommentType.Inline, entries[1].Type);
		Assert.Equal("TODO: optimize", entries[2].Text);
		Assert.Equal(CommentType.Todo, entries[2].Type);
	}

	[Fact]
	public void MultipleComments_BackwardCompat_ReturnsFirst() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "First comment", (byte)CommentType.Block);
		writer.AddComment(0x8000, "Second comment", (byte)CommentType.Inline);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal("First comment", loader.GetComment(0x8000));
		Assert.Equal(CommentType.Block, loader.GetCommentType(0x8000));
		Assert.Equal("First comment", loader.CommentEntries[0x8000].Text);
		Assert.Equal("First comment", loader.Comments[0x8000]);
	}

	[Fact]
	public void MultipleComments_AllCommentEntries_ReturnsLists() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "A", (byte)CommentType.Block);
		writer.AddComment(0x8000, "B", (byte)CommentType.Inline);
		writer.AddComment(0x8100, "C", (byte)CommentType.Todo);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(2, loader.AllCommentEntries.Count);
		Assert.Equal(2, loader.AllCommentEntries[0x8000].Count);
		Assert.Single(loader.AllCommentEntries[0x8100]);
	}

	[Fact]
	public void MultipleComments_GetCommentEntries_NullForMissing() {
		var writer = new PansyWriter { Platform = PansyLoader.PLATFORM_NES, RomSize = 0x8000 };
		writer.AddComment(0x8000, "Exists", (byte)CommentType.Inline);

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Null(loader.GetCommentEntries(0x9999));
	}

	#endregion
}
