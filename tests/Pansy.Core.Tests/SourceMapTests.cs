// ============================================================================
// SourceMapTests.cs - Tests for SOURCE_MAP Section (0x0007)
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;

namespace Pansy.Core.Tests;

public class SourceMapTests {
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
	public void SourceMap_Roundtrip_PreservesData() {
		var loader = MakeLoader(w => {
			var f0 = w.AddSourceFile("src/main.pasm");
			var f1 = w.AddSourceFile("src/utils.pasm");
			w.AddSourceMapping(new SourceMapEntry(0x8000, f0, 1, 1));
			w.AddSourceMapping(new SourceMapEntry(0x8003, f0, 2, 1));
			w.AddSourceMapping(new SourceMapEntry(0x8010, f1, 10, 5));
		});

		Assert.Equal(2, loader.SourceFiles.Count);
		Assert.Equal("src/main.pasm", loader.SourceFiles[0]);
		Assert.Equal("src/utils.pasm", loader.SourceFiles[1]);

		Assert.Equal(3, loader.SourceMapEntries.Count);

		Assert.Equal(0x8000u, loader.SourceMapEntries[0].RomAddress);
		Assert.Equal((ushort)0, loader.SourceMapEntries[0].FileIndex);
		Assert.Equal((ushort)1, loader.SourceMapEntries[0].Line);
		Assert.Equal((ushort)1, loader.SourceMapEntries[0].Column);

		Assert.Equal(0x8010u, loader.SourceMapEntries[2].RomAddress);
		Assert.Equal((ushort)1, loader.SourceMapEntries[2].FileIndex);
		Assert.Equal((ushort)10, loader.SourceMapEntries[2].Line);
		Assert.Equal((ushort)5, loader.SourceMapEntries[2].Column);
	}

	[Fact]
	public void SourceMap_EmptyFile_ReturnsEmptyLists() {
		var loader = MakeLoader(_ => { });
		Assert.Empty(loader.SourceFiles);
		Assert.Empty(loader.SourceMapEntries);
	}

	[Fact]
	public void SourceMap_HasSourceMapFlag_SetWhenPresent() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			RomCrc32 = 0x12345678,
		};
		var f0 = writer.AddSourceFile("test.pasm");
		writer.AddSourceMapping(new SourceMapEntry(0x8000, f0, 1, 1));
		var data = writer.Generate();

		// Flags are at offset 10 (little-endian uint16)
		var flags = (PansyFlags)(data[10] | (data[11] << 8));
		Assert.True(flags.HasFlag(PansyFlags.HasSourceMap));
	}

	[Fact]
	public void SourceMap_NoEntries_FlagNotSet() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			RomCrc32 = 0x12345678,
		};
		var data = writer.Generate();

		var flags = (PansyFlags)(data[10] | (data[11] << 8));
		Assert.False(flags.HasFlag(PansyFlags.HasSourceMap));
	}

	[Fact]
	public void SourceMap_DuplicateFile_ReturnsSameIndex() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			RomCrc32 = 0x12345678,
		};

		var i1 = writer.AddSourceFile("main.pasm");
		var i2 = writer.AddSourceFile("main.pasm");
		var i3 = writer.AddSourceFile("other.pasm");

		Assert.Equal(i1, i2);
		Assert.NotEqual(i1, i3);
	}

	[Fact]
	public void SourceMap_CoexistsWithOtherSections() {
		var loader = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.Label);
			w.AddComment(0x8000, "Entry point", (byte)CommentType.Inline);
			w.AddBookmark(new Bookmark(0x8000, "Start", 0));
			w.AddDataType(new DataTypeEntry(0x8100, 8, 1, 8, DataElementType.Byte, "Buffer"));
			var f0 = w.AddSourceFile("main.pasm");
			w.AddSourceMapping(new SourceMapEntry(0x8000, f0, 1, 1));
		});

		Assert.Single(loader.SourceMapEntries);
		Assert.Single(loader.SourceFiles);
		Assert.NotEmpty(loader.AllSymbolEntries);
		Assert.NotEmpty(loader.AllCommentEntries);
		Assert.NotEmpty(loader.Bookmarks);
		Assert.NotEmpty(loader.DataTypes);
	}

	[Fact]
	public void SourceMap_Unicode_PreservesFilePaths() {
		var loader = MakeLoader(w => {
			var f0 = w.AddSourceFile("ソース/メイン.pasm");
			w.AddSourceMapping(new SourceMapEntry(0x8000, f0, 1, 1));
		});

		Assert.Single(loader.SourceFiles);
		Assert.Equal("ソース/メイン.pasm", loader.SourceFiles[0]);
	}

	[Fact]
	public void SourceMap_Merge_CombinesEntries() {
		var baseLoader = MakeLoader(w => {
			var f0 = w.AddSourceFile("main.pasm");
			w.AddSourceMapping(new SourceMapEntry(0x8000, f0, 1, 1));
			w.AddSourceMapping(new SourceMapEntry(0x8003, f0, 2, 1));
		});

		var overlayLoader = MakeLoader(w => {
			var f0 = w.AddSourceFile("utils.pasm");
			w.AddSourceMapping(new SourceMapEntry(0x9000, f0, 1, 1));
		});

		var merged = PansyMerger.Merge(baseLoader, overlayLoader);
		var result = new PansyLoader(merged.Generate());

		Assert.Equal(2, result.SourceFiles.Count);
		Assert.Contains("main.pasm", result.SourceFiles);
		Assert.Contains("utils.pasm", result.SourceFiles);
		Assert.Equal(3, result.SourceMapEntries.Count);
	}

	[Fact]
	public void SourceMap_Merge_DeduplicatesEntries() {
		var baseLoader = MakeLoader(w => {
			var f0 = w.AddSourceFile("main.pasm");
			w.AddSourceMapping(new SourceMapEntry(0x8000, f0, 1, 1));
		});

		var overlayLoader = MakeLoader(w => {
			var f0 = w.AddSourceFile("main.pasm");
			w.AddSourceMapping(new SourceMapEntry(0x8000, f0, 1, 1)); // exact duplicate
			w.AddSourceMapping(new SourceMapEntry(0x8000, f0, 1, 5)); // different column
		});

		var merged = PansyMerger.Merge(baseLoader, overlayLoader);
		var result = new PansyLoader(merged.Generate());

		Assert.Single(result.SourceFiles);
		Assert.Equal(2, result.SourceMapEntries.Count); // duplicate removed, different column kept
	}
}
