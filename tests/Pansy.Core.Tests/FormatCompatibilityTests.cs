using Pansy.Core;
using Xunit;

namespace Pansy.Core.Tests;

public class FormatCompatibilityTests {
	[Fact]
	public void Header_MagicBytes_CorrectSequence() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var data = writer.Generate();

		// First 8 bytes: "PANSY\0\0\0"
		Assert.Equal((byte)'P', data[0]);
		Assert.Equal((byte)'A', data[1]);
		Assert.Equal((byte)'N', data[2]);
		Assert.Equal((byte)'S', data[3]);
		Assert.Equal((byte)'Y', data[4]);
		Assert.Equal(0, data[5]);
		Assert.Equal(0, data[6]);
		Assert.Equal(0, data[7]);
	}

	[Fact]
	public void Header_Version_Is0x0100() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var data = writer.Generate();
		var version = BitConverter.ToUInt16(data, 8);
		Assert.Equal(0x0100, version);
	}

	[Fact]
	public void Header_Platform_AtCorrectOffset() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000
		};

		var data = writer.Generate();
		// Platform is at offset 12 (after magic:8 + version:2 + flags:2)
		Assert.Equal(PansyLoader.PLATFORM_SNES, data[12]);
	}

	[Fact]
	public void Header_RomSize_AtCorrectOffset() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000
		};

		var data = writer.Generate();
		// RomSize is at offset 16 (after magic:8 + version:2 + flags:2 + platform:1 + reserved:3)
		var romSize = BitConverter.ToUInt32(data, 16);
		Assert.Equal(0x20000u, romSize);
	}

	[Fact]
	public void Header_RomCrc32_AtCorrectOffset() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			RomCrc32 = 0xaabbccdd
		};

		var data = writer.Generate();
		// CRC32 is at offset 20 (after magic:8 + version:2 + flags:2 + platform:1 + reserved:3 + size:4)
		var crc = BitConverter.ToUInt32(data, 20);
		Assert.Equal(0xaabbccddu, crc);
	}

	[Fact]
	public void Header_SectionCount_AtCorrectOffset() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		// Empty file, 0 sections
		var data = writer.Generate();
		var sectionCount = BitConverter.ToUInt32(data, 24);
		Assert.Equal(0u, sectionCount);
	}

	[Fact]
	public void Header_SectionCount_MatchesActualSections() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			ProjectName = "Test"
		};
		writer.AddSymbol(0x8000, "Sym");
		writer.AddComment(0x8000, "Com");
		writer.MarkAsCode(0x8000);

		var data = writer.Generate();
		var sectionCount = BitConverter.ToUInt32(data, 24);
		// 4 sections: CDL (0x0001), Symbols (0x0002), Comments (0x0003), Metadata (0x0008)
		Assert.Equal(4u, sectionCount);
	}

	[Fact]
	public void Header_TotalSize_Is32Bytes() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		// Empty file: 32-byte header + 0 section table entries + 0 section data
		var data = writer.Generate();
		Assert.Equal(32, data.Length);
	}

	[Fact]
	public void Flags_NoCompression_IsZero() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var data = writer.Generate();
		var flags = BitConverter.ToUInt16(data, 10);
		Assert.Equal(0, flags);
	}

	[Fact]
	public void Flags_Compression_SetsBit0() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			EnableCompression = true
		};
		writer.AddSymbol(0x8000, "Sym");

		var data = writer.Generate();
		var flags = BitConverter.ToUInt16(data, 10);
		Assert.True((flags & 0x0001) != 0, "Compressed flag should be set");
	}

	[Fact]
	public void Flags_CrossRefs_SetsBit2() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));

		var data = writer.Generate();
		var flags = BitConverter.ToUInt16(data, 10);
		Assert.True((flags & 0x0004) != 0, "HasCrossRefs flag should be set");
	}

	[Fact]
	public void Flags_SourceMap_SetsBit1() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		var idx = writer.AddSourceFile("main.pasm");
		writer.AddSourceMapping(new SourceMapEntry(0x8000, idx, 1, 0));

		var data = writer.Generate();
		var flags = BitConverter.ToUInt16(data, 10);
		Assert.True((flags & 0x0002) != 0, "HasSourceMap flag should be set");
	}

	[Fact]
	public void Loader_TruncatedHeader_Throws() {
		var shortData = new byte[16]; // Less than 32-byte header
		shortData[0] = (byte)'P';
		shortData[1] = (byte)'A';
		shortData[2] = (byte)'N';
		shortData[3] = (byte)'S';
		shortData[4] = (byte)'Y';

		Assert.Throws<InvalidDataException>(() => new PansyLoader(shortData));
	}

	[Fact]
	public void Loader_WrongMagic_Throws() {
		var badData = new byte[32];
		badData[0] = (byte)'X';
		badData[1] = (byte)'Y';
		badData[2] = (byte)'Z';

		Assert.Throws<InvalidDataException>(() => new PansyLoader(badData));
	}

	[Fact]
	public void Loader_EmptyArray_Throws() {
		Assert.Throws<InvalidDataException>(() => new PansyLoader([]));
	}

	[Fact]
	public void SectionTable_EntrySize_Is16Bytes() {
		// Each section table entry: Type(4) + Offset(4) + CompSize(4) + UncompSize(4) = 16
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddSymbol(0x8000, "Test");

		var data = writer.Generate();
		var sectionCount = BitConverter.ToUInt32(data, 24);
		Assert.Equal(1u, sectionCount);

		// Section table starts at offset 32
		var sectionType = BitConverter.ToUInt32(data, 32);
		Assert.Equal(0x0002u, sectionType); // SYMBOLS

		var sectionOffset = BitConverter.ToUInt32(data, 36);
		// Data starts after header (32) + section table (16 * sectionCount)
		Assert.Equal(48u, sectionOffset);
	}

	[Fact]
	public void MultipleSections_OffsetsAreContiguous() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};
		writer.AddSymbol(0x8000, "Sym");
		writer.AddComment(0x8000, "Com");

		var data = writer.Generate();
		var sectionCount = BitConverter.ToUInt32(data, 24);
		Assert.Equal(2u, sectionCount);

		// First section table entry at offset 32
		var offset1 = BitConverter.ToUInt32(data, 36);
		var compSize1 = BitConverter.ToUInt32(data, 40);

		// Second section table entry at offset 48
		var offset2 = BitConverter.ToUInt32(data, 52);

		// Second section starts right after first
		Assert.Equal(offset1 + compSize1, offset2);
	}

	[Fact]
	public void Loader_Version_Preserved() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(0x0100, loader.Version);
	}
}
