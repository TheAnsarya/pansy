// ============================================================================
// DataTypeTests.cs - Tests for DATA_TYPES Section (0x0005)
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;

namespace Pansy.Core.Tests;

public class DataTypeTests {
	private static PansyLoader MakeLoader(Action<PansyWriter> configure) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000,
			RomCrc32 = 0xdeadbeef,
		};
		configure(writer);
		return new PansyLoader(writer.Generate());
	}

	[Fact]
	public void DataTypes_Roundtrip_PreservesData() {
		var loader = MakeLoader(w => {
			w.AddDataType(new DataTypeEntry(0x8000, 16, 1, 16, DataElementType.Byte, "SpriteTable"));
			w.AddDataType(new DataTypeEntry(0x8100, 64, 2, 32, DataElementType.Word, "TileMap"));
			w.AddDataType(new DataTypeEntry(0x9000, 256, 4, 64, DataElementType.Long, "PointerTable"));
		});

		Assert.Equal(3, loader.DataTypes.Count);

		Assert.Equal(0x8000u, loader.DataTypes[0].Address);
		Assert.Equal(16u, loader.DataTypes[0].Length);
		Assert.Equal((ushort)1, loader.DataTypes[0].ElementSize);
		Assert.Equal((ushort)16, loader.DataTypes[0].ElementCount);
		Assert.Equal(DataElementType.Byte, loader.DataTypes[0].Type);
		Assert.Equal("SpriteTable", loader.DataTypes[0].Name);

		Assert.Equal(0x8100u, loader.DataTypes[1].Address);
		Assert.Equal(64u, loader.DataTypes[1].Length);
		Assert.Equal((ushort)2, loader.DataTypes[1].ElementSize);
		Assert.Equal((ushort)32, loader.DataTypes[1].ElementCount);
		Assert.Equal(DataElementType.Word, loader.DataTypes[1].Type);
		Assert.Equal("TileMap", loader.DataTypes[1].Name);

		Assert.Equal(0x9000u, loader.DataTypes[2].Address);
		Assert.Equal(DataElementType.Long, loader.DataTypes[2].Type);
		Assert.Equal("PointerTable", loader.DataTypes[2].Name);
	}

	[Fact]
	public void DataTypes_EmptyFile_ReturnsEmptyList() {
		var loader = MakeLoader(_ => { });
		Assert.Empty(loader.DataTypes);
	}

	[Fact]
	public void DataTypes_StringType_Roundtrip() {
		var loader = MakeLoader(w => {
			w.AddDataType(new DataTypeEntry(0xc000, 32, 1, 32, DataElementType.String, "DialogText"));
		});

		Assert.Single(loader.DataTypes);
		Assert.Equal(DataElementType.String, loader.DataTypes[0].Type);
		Assert.Equal("DialogText", loader.DataTypes[0].Name);
		Assert.Equal(32u, loader.DataTypes[0].Length);
	}

	[Fact]
	public void DataTypes_CoexistsWithOtherSections() {
		var loader = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.Label);
			w.AddComment(0x8000, "Entry point", (byte)CommentType.Inline);
			w.AddBookmark(new Bookmark(0x8000, "Start", 0));
			w.AddDataType(new DataTypeEntry(0x8100, 8, 1, 8, DataElementType.Byte, "Buffer"));
		});

		Assert.Single(loader.DataTypes);
		Assert.Equal("Buffer", loader.DataTypes[0].Name);
		Assert.NotEmpty(loader.AllSymbolEntries);
		Assert.NotEmpty(loader.AllCommentEntries);
		Assert.NotEmpty(loader.Bookmarks);
	}

	[Fact]
	public void DataTypes_Unicode_PreservesName() {
		var loader = MakeLoader(w => {
			w.AddDataType(new DataTypeEntry(0xa000, 4, 2, 2, DataElementType.Word, "ポインタ表"));
		});

		Assert.Single(loader.DataTypes);
		Assert.Equal("ポインタ表", loader.DataTypes[0].Name);
	}

	[Fact]
	public void DataTypes_MultipleAtSameAddress() {
		var loader = MakeLoader(w => {
			w.AddDataType(new DataTypeEntry(0x8000, 16, 1, 16, DataElementType.Byte, "ByteView"));
			w.AddDataType(new DataTypeEntry(0x8000, 16, 2, 8, DataElementType.Word, "WordView"));
		});

		Assert.Equal(2, loader.DataTypes.Count);
		Assert.Equal("ByteView", loader.DataTypes[0].Name);
		Assert.Equal("WordView", loader.DataTypes[1].Name);
	}

	[Fact]
	public void DataTypes_Merge_Deduplicates() {
		var baseLoader = MakeLoader(w => {
			w.AddDataType(new DataTypeEntry(0x8000, 16, 1, 16, DataElementType.Byte, "Table"));
			w.AddDataType(new DataTypeEntry(0x9000, 32, 2, 16, DataElementType.Word, "Pointers"));
		});

		var overlayLoader = MakeLoader(w => {
			w.AddDataType(new DataTypeEntry(0x8000, 16, 1, 16, DataElementType.Byte, "Table")); // duplicate
			w.AddDataType(new DataTypeEntry(0xa000, 8, 1, 8, DataElementType.Byte, "Extra"));
		});

		var merged = PansyMerger.Merge(baseLoader, overlayLoader);
		var result = new PansyLoader(merged.Generate());

		Assert.Equal(3, result.DataTypes.Count);
		Assert.Contains(result.DataTypes, d => d.Name == "Table");
		Assert.Contains(result.DataTypes, d => d.Name == "Pointers");
		Assert.Contains(result.DataTypes, d => d.Name == "Extra");
	}
}
