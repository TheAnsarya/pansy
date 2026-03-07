using Pansy.Core;
using Xunit;

namespace Pansy.Core.Tests;

public class WriterValidationTests {
	[Fact]
	public void ProjectName_NullSetter_DefaultsToEmpty() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			ProjectName = null!
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal("", loader.ProjectName);
	}

	[Fact]
	public void Author_NullSetter_DefaultsToEmpty() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			Author = null!
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal("", loader.Author);
	}

	[Fact]
	public void ProjectVersion_NullSetter_DefaultsToEmpty() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			ProjectVersion = null!
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal("", loader.ProjectVersion);
	}

	[Fact]
	public void RomSize_Zero_Accepted() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(0u, loader.RomSize);
	}

	[Fact]
	public void RomSize_MaxValue_Accepted() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_CUSTOM,
			RomSize = uint.MaxValue
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(uint.MaxValue, loader.RomSize);
	}

	[Fact]
	public void RomCrc32_Zero_Accepted() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			RomCrc32 = 0
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(0u, loader.RomCrc32);
	}

	[Fact]
	public void RomCrc32_MaxValue_Accepted() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			RomCrc32 = uint.MaxValue
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(uint.MaxValue, loader.RomCrc32);
	}

	[Fact]
	public void Platform_AllConstants_Roundtrip() {
		byte[] platforms = [
			PansyLoader.PLATFORM_NES,
			PansyLoader.PLATFORM_SNES,
			PansyLoader.PLATFORM_GB,
			PansyLoader.PLATFORM_GBA,
			PansyLoader.PLATFORM_GENESIS,
			PansyLoader.PLATFORM_SMS,
			PansyLoader.PLATFORM_PCE,
			PansyLoader.PLATFORM_ATARI_2600,
			PansyLoader.PLATFORM_LYNX,
			PansyLoader.PLATFORM_WONDERSWAN,
			PansyLoader.PLATFORM_CUSTOM,
		];

		foreach (var platform in platforms) {
			var writer = new PansyWriter {
				Platform = platform,
				RomSize = 0x8000
			};

			var loader = new PansyLoader(writer.Generate());
			Assert.Equal(platform, loader.Platform);
		}
	}

	[Fact]
	public void GenerateEmpty_ProducesValidHeader() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var data = writer.Generate();

		// Header must be at least 32 bytes
		Assert.True(data.Length >= 32, $"Expected at least 32 bytes header, got {data.Length}");

		// Magic
		Assert.Equal((byte)'P', data[0]);
		Assert.Equal((byte)'A', data[1]);
		Assert.Equal((byte)'N', data[2]);
		Assert.Equal((byte)'S', data[3]);
		Assert.Equal((byte)'Y', data[4]);
		Assert.Equal(0, data[5]);
		Assert.Equal(0, data[6]);
		Assert.Equal(0, data[7]);

		// Version
		Assert.Equal(0x00, data[8]);
		Assert.Equal(0x01, data[9]);
	}

	[Fact]
	public void Symbol_EmptyName_RoundtripsAsEmpty() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbol(0x8000, "");

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal("", loader.GetSymbol(0x8000));
	}

	[Fact]
	public void Comment_EmptyText_RoundtripsAsEmpty() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddComment(0x8000, "");

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal("", loader.GetComment(0x8000));
	}

	[Fact]
	public void Symbol_VeryLongName_RoundtripsCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var longName = new string('X', 5000);
		writer.AddSymbol(0x8000, longName);

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(longName, loader.GetSymbol(0x8000));
	}

	[Fact]
	public void Comment_VeryLongText_RoundtripsCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var longText = new string('Y', 5000);
		writer.AddComment(0x8000, longText);

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(longText, loader.GetComment(0x8000));
	}

	[Fact]
	public void Metadata_VeryLongProjectName_RoundtripsCorrectly() {
		var longName = new string('Z', 2000);
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			ProjectName = longName,
			Author = "TestAuthor",
			ProjectVersion = "1.0"
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(longName, loader.ProjectName);
	}

	[Fact]
	public void Metadata_UnicodeValues_RoundtripCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			ProjectName = "テストプロジェクト 🎮",
			Author = "著者名 🌼",
			ProjectVersion = "バージョン 1.0"
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal("テストプロジェクト 🎮", loader.ProjectName);
		Assert.Equal("著者名 🌼", loader.Author);
		Assert.Equal("バージョン 1.0", loader.ProjectVersion);
	}

	[Fact]
	public void MemoryRegion_BankValues_AllPreserved() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x100000
		};

		for (byte bank = 0; bank < 10; bank++) {
			writer.AddMemoryRegion(new MemoryRegion(
				(uint)(bank * 0x10000 + 0x8000),
				(uint)(bank * 0x10000 + 0xffff),
				(byte)MemoryRegionType.ROM,
				bank,
				$"Bank_{bank:x2}"));
		}

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(10, loader.MemoryRegions.Count);
		for (byte bank = 0; bank < 10; bank++) {
			var region = loader.MemoryRegions.Single(r => r.Name == $"Bank_{bank:x2}");
			Assert.Equal(bank, region.Bank);
			Assert.Equal((byte)MemoryRegionType.ROM, region.Type);
		}
	}

	[Fact]
	public void CrossRef_Address_Zero_Roundtrips() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddCrossReference(new CrossReference(0, 0x8000, CrossRefType.Jsr));
		writer.AddCrossReference(new CrossReference(0x8000, 0, CrossRefType.Read));

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(2, loader.CrossReferences.Count);
		Assert.Contains(loader.CrossReferences, x => x.From == 0 && x.To == 0x8000);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8000 && x.To == 0);
	}

	[Fact]
	public void Bookmark_AddressZero_Roundtrips() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddBookmark(new Bookmark(0, "Start", 1));

		var loader = new PansyLoader(writer.Generate());
		Assert.Single(loader.Bookmarks);
		Assert.Equal(0u, loader.Bookmarks[0].Address);
		Assert.Equal("Start", loader.Bookmarks[0].Name);
	}

	[Fact]
	public void DataType_AllElementTypes_Roundtrip() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddDataType(new DataTypeEntry(0x8000, 16, 1, 16, DataElementType.Byte, "ByteArray"));
		writer.AddDataType(new DataTypeEntry(0x8100, 32, 2, 16, DataElementType.Word, "WordArray"));
		writer.AddDataType(new DataTypeEntry(0x8200, 48, 4, 12, DataElementType.Long, "LongArray"));
		writer.AddDataType(new DataTypeEntry(0x8300, 20, 1, 20, DataElementType.String, "TextData"));

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(4, loader.DataTypes.Count);

		var byteType = loader.DataTypes.Single(d => d.Name == "ByteArray");
		Assert.Equal(DataElementType.Byte, byteType.Type);
		Assert.Equal(1, byteType.ElementSize);

		var wordType = loader.DataTypes.Single(d => d.Name == "WordArray");
		Assert.Equal(DataElementType.Word, wordType.Type);
		Assert.Equal(2, wordType.ElementSize);

		var longType = loader.DataTypes.Single(d => d.Name == "LongArray");
		Assert.Equal(DataElementType.Long, longType.Type);
		Assert.Equal(4, longType.ElementSize);

		var strType = loader.DataTypes.Single(d => d.Name == "TextData");
		Assert.Equal(DataElementType.String, strType.Type);
	}

	[Fact]
	public void Compression_SingleSymbol_StillWorks() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			EnableCompression = true
		};

		writer.AddSymbol(0x8000, "OnlySymbol");

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal("OnlySymbol", loader.GetSymbol(0x8000));
	}

	[Fact]
	public void Compression_AllSectionTypes_Roundtrip() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000,
			RomCrc32 = 0xdeadbeef,
			ProjectName = "Compressed Test",
			Author = "Tester",
			ProjectVersion = "2.0",
			EnableCompression = true
		};

		// All section types
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
		writer.AddBookmark(new Bookmark(0x8000, "Bookmark1", 3));
		writer.AddDataType(new DataTypeEntry(0x9000, 16, 1, 16, DataElementType.Byte, "Table"));
		var fileIdx = writer.AddSourceFile("main.pasm");
		writer.AddSourceMapping(new SourceMapEntry(0x8000, fileIdx, 10, 0));

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal("Main", loader.GetSymbol(0x8000));
		Assert.Equal(SymbolType.Function, loader.GetSymbolType(0x8000));
		Assert.Equal("Entry", loader.GetComment(0x8000));
		Assert.True(loader.IsCode(0x8000));
		Assert.True(loader.IsData(0x9000));
		Assert.True(loader.IsJumpTarget(0x8050));
		Assert.True(loader.IsSubEntryPoint(0x8100));
		Assert.True(loader.IsDrawn(0xa000));
		Assert.True(loader.IsRead(0xb000));
		Assert.True(loader.IsIndirect(0xc000));
		Assert.Single(loader.MemoryRegions);
		Assert.Single(loader.CrossReferences);
		Assert.Single(loader.Bookmarks);
		Assert.Single(loader.DataTypes);
		Assert.Single(loader.SourceFiles);
		Assert.Single(loader.SourceMapEntries);
		Assert.Equal("Compressed Test", loader.ProjectName);
	}

	[Fact]
	public void AllFlags_CompressedVsUncompressed_SameData() {
		PansyWriter CreateWriter(bool compress) {
			var w = new PansyWriter {
				Platform = PansyLoader.PLATFORM_NES,
				RomSize = 0x20000,
				RomCrc32 = 0x11223344,
				EnableCompression = compress
			};
			for (uint i = 0; i < 100; i++) {
				w.MarkAsCode(0x8000 + i);
				w.MarkAsOpcode(0x8000 + i * 3);
			}
			w.AddSymbol(0x8000, "Test");
			w.AddComment(0x8000, "Hello");
			return w;
		}

		var loaderUncompressed = new PansyLoader(CreateWriter(false).Generate());
		var loaderCompressed = new PansyLoader(CreateWriter(true).Generate());

		Assert.Equal(loaderUncompressed.Symbols.Count, loaderCompressed.Symbols.Count);
		Assert.Equal(loaderUncompressed.Comments.Count, loaderCompressed.Comments.Count);
		Assert.Equal(loaderUncompressed.CodeOffsets.Count, loaderCompressed.CodeOffsets.Count);
		Assert.Equal(loaderUncompressed.OpcodeOffsets.Count, loaderCompressed.OpcodeOffsets.Count);
	}

	[Fact]
	public void SourceFile_DuplicatePath_ReturnsSameIndex() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var idx1 = writer.AddSourceFile("main.pasm");
		var idx2 = writer.AddSourceFile("utils.pasm");
		var idx3 = writer.AddSourceFile("main.pasm");

		Assert.Equal(idx1, idx3);
		Assert.NotEqual(idx1, idx2);
	}

	[Fact]
	public void SourceMap_MultipleEntries_AllRoundtrip() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var mainIdx = writer.AddSourceFile("main.pasm");
		var utilIdx = writer.AddSourceFile("utils.pasm");

		writer.AddSourceMapping(new SourceMapEntry(0x8000, mainIdx, 1, 0));
		writer.AddSourceMapping(new SourceMapEntry(0x8003, mainIdx, 5, 4));
		writer.AddSourceMapping(new SourceMapEntry(0x8010, utilIdx, 10, 0));

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(2, loader.SourceFiles.Count);
		Assert.Equal(3, loader.SourceMapEntries.Count);
	}
}
