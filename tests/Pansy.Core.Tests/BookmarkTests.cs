// ============================================================================
// BookmarkTests.cs - Tests for Bookmark Section Support
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;

namespace Pansy.Core.Tests;

public class BookmarkTests {
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
	public void Bookmarks_Roundtrip_PreservesData() {
		var loader = MakeLoader(w => {
			w.AddBookmark(new Bookmark(0x8000, "Reset Vector", 1));
			w.AddBookmark(new Bookmark(0x8100, "Main Loop", 0));
			w.AddBookmark(new Bookmark(0xfffa, "NMI Handler", 2));
		});

		Assert.Equal(3, loader.Bookmarks.Count);

		Assert.Equal(0x8000u, loader.Bookmarks[0].Address);
		Assert.Equal("Reset Vector", loader.Bookmarks[0].Name);
		Assert.Equal(1, loader.Bookmarks[0].Color);

		Assert.Equal(0x8100u, loader.Bookmarks[1].Address);
		Assert.Equal("Main Loop", loader.Bookmarks[1].Name);
		Assert.Equal(0, loader.Bookmarks[1].Color);

		Assert.Equal(0xfffau, loader.Bookmarks[2].Address);
		Assert.Equal("NMI Handler", loader.Bookmarks[2].Name);
		Assert.Equal(2, loader.Bookmarks[2].Color);
	}

	[Fact]
	public void Bookmarks_EmptyFile_ReturnsEmptyList() {
		var loader = MakeLoader(_ => { });
		Assert.Empty(loader.Bookmarks);
	}

	[Fact]
	public void Bookmarks_DefaultColor_IsZero() {
		var loader = MakeLoader(w => {
			w.AddBookmark(new Bookmark(0x8000, "Test"));
		});

		Assert.Single(loader.Bookmarks);
		Assert.Equal(0, loader.Bookmarks[0].Color);
	}

	[Fact]
	public void Bookmarks_UnicodeNames_Preserved() {
		var loader = MakeLoader(w => {
			w.AddBookmark(new Bookmark(0x8000, "メインループ", 1));
			w.AddBookmark(new Bookmark(0x8100, "🌼 Start", 2));
		});

		Assert.Equal("メインループ", loader.Bookmarks[0].Name);
		Assert.Equal("🌼 Start", loader.Bookmarks[1].Name);
	}

	[Fact]
	public void Bookmarks_CoexistWithOtherSections() {
		var loader = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.InterruptVector);
			w.AddComment(0x8000, "Entry point");
			w.AddBookmark(new Bookmark(0x8000, "Boot", 1));
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		Assert.Single(loader.Bookmarks);
		Assert.Equal("Boot", loader.Bookmarks[0].Name);
		Assert.Equal("Reset", loader.Symbols.Values.First());
		Assert.Equal("Entry point", loader.Comments.Values.First());
		Assert.Single(loader.CrossReferences);
	}

	[Fact]
	public void Bookmarks_Merge_UnionWithDedup() {
		var base1 = MakeLoader(w => {
			w.AddBookmark(new Bookmark(0x8000, "Reset", 1));
			w.AddBookmark(new Bookmark(0x8100, "Main", 2));
		});

		var overlay = MakeLoader(w => {
			w.AddBookmark(new Bookmark(0x8000, "Reset", 1)); // duplicate
			w.AddBookmark(new Bookmark(0x8200, "Sub", 3)); // new
		});

		var merged = PansyMerger.Merge(base1, overlay);
		var result = new PansyLoader(merged.Generate());

		Assert.Equal(3, result.Bookmarks.Count);
	}

	[Fact]
	public void Bookmarks_MultipleAtSameAddress() {
		var loader = MakeLoader(w => {
			w.AddBookmark(new Bookmark(0x8000, "Bookmark A", 0));
			w.AddBookmark(new Bookmark(0x8000, "Bookmark B", 1));
		});

		Assert.Equal(2, loader.Bookmarks.Count);
		Assert.Equal("Bookmark A", loader.Bookmarks[0].Name);
		Assert.Equal("Bookmark B", loader.Bookmarks[1].Name);
	}
}
