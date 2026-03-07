using Pansy.Core;
using Xunit;

namespace Pansy.Core.Tests;

public class LoaderEdgeCaseTests {
	[Fact]
	public void Loader_OnlyMetadata_NoOtherSections() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			ProjectName = "Meta Only",
			Author = "Author",
			ProjectVersion = "1.0"
		};

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal("Meta Only", loader.ProjectName);
		Assert.Equal("Author", loader.Author);
		Assert.Equal("1.0", loader.ProjectVersion);
		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
		Assert.Empty(loader.CrossReferences);
		Assert.Empty(loader.MemoryRegions);
		Assert.Empty(loader.Bookmarks);
		Assert.Empty(loader.DataTypes);
	}

	[Fact]
	public void Loader_OnlyCodeDataMap_NoSymbolsOrComments() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.MarkAsCode(0x8000);
		writer.MarkAsData(0x9000);

		var loader = new PansyLoader(writer.Generate());

		Assert.True(loader.IsCode(0x8000));
		Assert.True(loader.IsData(0x9000));
		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
	}

	[Fact]
	public void Loader_OnlyCrossRefs_NoOtherSections() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x8010, 0x8200, CrossRefType.Jmp));

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(2, loader.CrossReferences.Count);
		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
	}

	[Fact]
	public void Loader_OnlyBookmarks_NoOtherSections() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddBookmark(new Bookmark(0x8000, "Start", 1));

		var loader = new PansyLoader(writer.Generate());

		Assert.Single(loader.Bookmarks);
		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
	}

	[Fact]
	public void Loader_OnlyDataTypes_NoOtherSections() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddDataType(new DataTypeEntry(0x8000, 16, 1, 16, DataElementType.Byte, "Table"));

		var loader = new PansyLoader(writer.Generate());

		Assert.Single(loader.DataTypes);
		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
	}

	[Fact]
	public void Loader_OnlyMemoryRegions_NoOtherSections() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, (byte)MemoryRegionType.ROM, 0, "ROM"));

		var loader = new PansyLoader(writer.Generate());

		Assert.Single(loader.MemoryRegions);
		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
	}

	[Fact]
	public void Loader_AllSectionTypes_AllPresent() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000,
			ProjectName = "Full Test",
			Author = "Auth",
			ProjectVersion = "3.0"
		};

		writer.AddSymbol(0x8000, "Main", SymbolType.Function);
		writer.AddComment(0x8000, "Entry", CommentType.Inline);
		writer.MarkAsCode(0x8000);
		writer.MarkAsData(0x9000);
		writer.MarkAsJumpTarget(0x8050);
		writer.MarkAsSubroutine(0x8100);
		writer.MarkAsOpcode(0x8000);
		writer.MarkAsDrawn(0xa000);
		writer.MarkAsRead(0xb000);
		writer.MarkAsIndirect(0xc000);
		writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, (byte)MemoryRegionType.ROM, 0, "ROM"));
		writer.AddCrossReference(new CrossReference(0x8005, 0x8100, CrossRefType.Jsr));
		writer.AddBookmark(new Bookmark(0x8000, "BM", 2));
		writer.AddDataType(new DataTypeEntry(0x9000, 8, 1, 8, DataElementType.Byte, "Data"));
		var f = writer.AddSourceFile("main.pasm");
		writer.AddSourceMapping(new SourceMapEntry(0x8000, f, 1, 0));

		var loader = new PansyLoader(writer.Generate());

		// All sections present
		Assert.NotEmpty(loader.Symbols);
		Assert.NotEmpty(loader.Comments);
		Assert.True(loader.IsCode(0x8000));
		Assert.True(loader.IsData(0x9000));
		Assert.True(loader.IsJumpTarget(0x8050));
		Assert.True(loader.IsSubEntryPoint(0x8100));
		Assert.True(loader.IsDrawn(0xa000));
		Assert.True(loader.IsRead(0xb000));
		Assert.True(loader.IsIndirect(0xc000));
		Assert.NotEmpty(loader.MemoryRegions);
		Assert.NotEmpty(loader.CrossReferences);
		Assert.NotEmpty(loader.Bookmarks);
		Assert.NotEmpty(loader.DataTypes);
		Assert.NotEmpty(loader.SourceFiles);
		Assert.NotEmpty(loader.SourceMapEntries);
		Assert.Equal("Full Test", loader.ProjectName);
		Assert.Equal("Auth", loader.Author);
		Assert.Equal("3.0", loader.ProjectVersion);
	}

	[Fact]
	public void Loader_GetSymbol_MissingAddress_ReturnsNull() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddSymbol(0x8000, "Present");

		var loader = new PansyLoader(writer.Generate());

		Assert.Null(loader.GetSymbol(0x9999));
		Assert.Null(loader.GetSymbolEntry(0x9999));
		Assert.Null(loader.GetSymbolEntries(0x9999));
	}

	[Fact]
	public void Loader_GetComment_MissingAddress_ReturnsNull() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddComment(0x8000, "Present");

		var loader = new PansyLoader(writer.Generate());

		Assert.Null(loader.GetComment(0x9999));
		Assert.Null(loader.GetCommentEntry(0x9999));
		Assert.Null(loader.GetCommentEntries(0x9999));
	}

	[Fact]
	public void Loader_IsCode_FalseForUnmarkedAddress() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.MarkAsCode(0x8000);

		var loader = new PansyLoader(writer.Generate());
		Assert.False(loader.IsCode(0x8001));
		Assert.False(loader.IsData(0x8000));
		Assert.False(loader.IsJumpTarget(0x8000));
		Assert.False(loader.IsSubEntryPoint(0x8000));
	}

	[Fact]
	public void Loader_CrossRefQueries_EmptyFile_ReturnEmpty() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var loader = new PansyLoader(writer.Generate());

		Assert.Empty(loader.GetCrossRefsTo(0x8000));
		Assert.Empty(loader.GetCrossRefsFrom(0x8000));
		Assert.Empty(loader.GetCrossRefsByType(CrossRefType.Jsr));
		Assert.Equal(0, loader.GetReferenceCount(0x8000));
	}

	[Fact]
	public void Loader_GetCoverageStats_EmptyFile_AllZero() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var loader = new PansyLoader(writer.Generate());
		var stats = loader.GetCoverageStats();

		Assert.Equal(0, stats.CodeBytes);
		Assert.Equal(0, stats.DataBytes);
		Assert.Equal(0x8000, stats.TotalSize);
	}

	[Fact]
	public void Loader_GetCrossRefStats_EmptyFile_AllZero() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var loader = new PansyLoader(writer.Generate());
		var stats = loader.GetCrossRefStats();

		Assert.Equal(0, stats.TotalXrefs);
		Assert.Equal(0, stats.JsrCount);
		Assert.Equal(0, stats.JmpCount);
		Assert.Equal(0, stats.BranchCount);
		Assert.Equal(0, stats.ReadCount);
		Assert.Equal(0, stats.WriteCount);
	}

	[Fact]
	public void Loader_LargeSymbolSet_AllAccessible() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_GB,
			RomSize = 0x100000
		};

		const int count = 5000;
		for (uint i = 0; i < count; i++) {
			writer.AddSymbol(i * 4, $"S{i:d5}", SymbolType.Label);
		}

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(count, loader.Symbols.Count);

		// Spot check
		Assert.Equal("S00000", loader.GetSymbol(0));
		Assert.Equal("S02500", loader.GetSymbol(2500 * 4));
		Assert.Equal("S04999", loader.GetSymbol(4999 * 4));
	}

	[Fact]
	public void Loader_LargeCrossRefSet_QueriesWork() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x100000
		};

		const int count = 3000;
		for (uint i = 0; i < count; i++) {
			writer.AddCrossReference(new CrossReference(
				i * 3,
				0x8000 + (i % 100) * 4,
				(CrossRefType)(i % 5 + 1)));
		}

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(count, loader.CrossReferences.Count);

		// Query by target — address 0x8000 should have 30 refs (every 100th)
		var refsTo = loader.GetCrossRefsTo(0x8000);
		Assert.Equal(30, refsTo.Count);

		// Stats
		var stats = loader.GetCrossRefStats();
		Assert.Equal(count, stats.TotalXrefs);
	}

	[Fact]
	public void Loader_ManyBookmarks_AllPreserved() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		for (uint i = 0; i < 100; i++) {
			writer.AddBookmark(new Bookmark(0x8000 + i, $"BM_{i:d3}", (byte)(i % 8)));
		}

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(100, loader.Bookmarks.Count);
	}

	[Fact]
	public void Loader_ManyDataTypes_AllPreserved() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000
		};

		for (uint i = 0; i < 50; i++) {
			writer.AddDataType(new DataTypeEntry(
				0x8000 + i * 0x100,
				64, 2, 32,
				(DataElementType)(i % 4 + 1),
				$"Type_{i:d3}"));
		}

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(50, loader.DataTypes.Count);
	}

	[Fact]
	public void Loader_SpecialCharacters_InSymbolNames() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbol(0x8000, "func@main");
		writer.AddSymbol(0x8010, "var$count");
		writer.AddSymbol(0x8020, "label.local");
		writer.AddSymbol(0x8030, "ns::method");
		writer.AddSymbol(0x8040, "arr[0]");

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal("func@main", loader.GetSymbol(0x8000));
		Assert.Equal("var$count", loader.GetSymbol(0x8010));
		Assert.Equal("label.local", loader.GetSymbol(0x8020));
		Assert.Equal("ns::method", loader.GetSymbol(0x8030));
		Assert.Equal("arr[0]", loader.GetSymbol(0x8040));
	}

	[Fact]
	public void Loader_MultilineComment_PreservedExactly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var multiline = "Line 1\nLine 2\nLine 3\r\nLine 4";
		writer.AddComment(0x8000, multiline, CommentType.Block);

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(multiline, loader.GetComment(0x8000));
	}
}
