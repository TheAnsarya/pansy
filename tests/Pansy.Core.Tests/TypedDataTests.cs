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
}
