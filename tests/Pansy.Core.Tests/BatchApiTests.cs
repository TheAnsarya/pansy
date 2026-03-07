using Pansy.Core;
using Xunit;

namespace Pansy.Core.Tests;

public class BatchApiTests {
	[Fact]
	public void AddSymbols_Batch_RoundtripsAll() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var symbols = new (uint Address, string Name, SymbolType Type)[] {
			(0x8000, "Reset", SymbolType.Function),
			(0x8003, "NMI", SymbolType.InterruptVector),
			(0x8010, "Loop", SymbolType.Label),
			(0x8020, "CONST_A", SymbolType.Constant),
			(0x8030, ".local", SymbolType.Local),
		};

		writer.AddSymbols(symbols);

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(5, loader.Symbols.Count);
		Assert.Equal("Reset", loader.GetSymbol(0x8000));
		Assert.Equal(SymbolType.Function, loader.GetSymbolType(0x8000));
		Assert.Equal("NMI", loader.GetSymbol(0x8003));
		Assert.Equal(SymbolType.InterruptVector, loader.GetSymbolType(0x8003));
		Assert.Equal("Loop", loader.GetSymbol(0x8010));
		Assert.Equal(SymbolType.Label, loader.GetSymbolType(0x8010));
		Assert.Equal("CONST_A", loader.GetSymbol(0x8020));
		Assert.Equal(SymbolType.Constant, loader.GetSymbolType(0x8020));
		Assert.Equal(".local", loader.GetSymbol(0x8030));
		Assert.Equal(SymbolType.Local, loader.GetSymbolType(0x8030));
	}

	[Fact]
	public void AddSymbols_EmptyBatch_NoSection() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbols([]);

		var loader = new PansyLoader(writer.Generate());
		Assert.Empty(loader.Symbols);
	}

	[Fact]
	public void AddComments_Batch_RoundtripsAll() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var comments = new (uint Address, string Text, CommentType Type)[] {
			(0x8000, "Entry point", CommentType.Inline),
			(0x8010, "Main loop body", CommentType.Block),
			(0x8020, "TODO: optimize", CommentType.Todo),
		};

		writer.AddComments(comments);

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(3, loader.Comments.Count);
		Assert.Equal("Entry point", loader.GetComment(0x8000));
		Assert.Equal(CommentType.Inline, loader.GetCommentType(0x8000));
		Assert.Equal("Main loop body", loader.GetComment(0x8010));
		Assert.Equal(CommentType.Block, loader.GetCommentType(0x8010));
		Assert.Equal("TODO: optimize", loader.GetComment(0x8020));
		Assert.Equal(CommentType.Todo, loader.GetCommentType(0x8020));
	}

	[Fact]
	public void AddComments_EmptyBatch_NoSection() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddComments([]);

		var loader = new PansyLoader(writer.Generate());
		Assert.Empty(loader.Comments);
	}

	[Fact]
	public void AddCrossReferences_Batch_RoundtripsAll() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var xrefs = new CrossReference[] {
			new(0x8000, 0x8100, CrossRefType.Jsr),
			new(0x8010, 0x8200, CrossRefType.Jmp),
			new(0x8020, 0x8050, CrossRefType.Branch),
			new(0x8030, 0x9000, CrossRefType.Read),
			new(0x8040, 0x9100, CrossRefType.Write),
		};

		writer.AddCrossReferences(xrefs);

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(5, loader.CrossReferences.Count);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8000 && x.To == 0x8100 && x.Type == CrossRefType.Jsr);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8010 && x.To == 0x8200 && x.Type == CrossRefType.Jmp);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8020 && x.To == 0x8050 && x.Type == CrossRefType.Branch);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8030 && x.To == 0x9000 && x.Type == CrossRefType.Read);
		Assert.Contains(loader.CrossReferences, x => x.From == 0x8040 && x.To == 0x9100 && x.Type == CrossRefType.Write);
	}

	[Fact]
	public void AddCrossReferences_EmptyBatch_NoSection() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddCrossReferences([]);

		var loader = new PansyLoader(writer.Generate());
		Assert.Empty(loader.CrossReferences);
	}

	[Fact]
	public void AddMemoryRegions_Batch_RoundtripsAll() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		var regions = new MemoryRegion[] {
			new(0x0000, 0x07ff, (byte)MemoryRegionType.RAM, 0, "Internal RAM"),
			new(0x2000, 0x2007, (byte)MemoryRegionType.IO, 0, "PPU Registers"),
			new(0x8000, 0xffff, (byte)MemoryRegionType.ROM, 0, "PRG-ROM"),
		};

		writer.AddMemoryRegions(regions);

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(3, loader.MemoryRegions.Count);
		Assert.Contains(loader.MemoryRegions, r => r.Name == "Internal RAM" && r.Start == 0x0000 && r.End == 0x07ff);
		Assert.Contains(loader.MemoryRegions, r => r.Name == "PPU Registers" && r.Start == 0x2000);
		Assert.Contains(loader.MemoryRegions, r => r.Name == "PRG-ROM" && r.Start == 0x8000);
	}

	[Fact]
	public void AddMemoryRegions_EmptyBatch_NoSection() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddMemoryRegions([]);

		var loader = new PansyLoader(writer.Generate());
		Assert.Empty(loader.MemoryRegions);
	}

	[Fact]
	public void AddComment_CommentTypeEnum_RoundtripsCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddComment(0x8000, "Inline comment", CommentType.Inline);
		writer.AddComment(0x8010, "Block comment", CommentType.Block);
		writer.AddComment(0x8020, "Todo comment", CommentType.Todo);

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(CommentType.Inline, loader.GetCommentType(0x8000));
		Assert.Equal(CommentType.Block, loader.GetCommentType(0x8010));
		Assert.Equal(CommentType.Todo, loader.GetCommentType(0x8020));
	}

	[Fact]
	public void BatchAndSingle_MixedOperations_CombineCorrectly() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		// Add single items first
		writer.AddSymbol(0x8000, "Manual", SymbolType.Label);
		writer.AddComment(0x8000, "Manual comment");
		writer.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		writer.AddMemoryRegion(new MemoryRegion(0x0000, 0x07ff, (byte)MemoryRegionType.RAM, 0, "RAM"));

		// Then add batches
		writer.AddSymbols([
			(0x8010, "Batch1", SymbolType.Function),
			(0x8020, "Batch2", SymbolType.Constant),
		]);
		writer.AddComments([
			(0x8010, "Batch comment 1", CommentType.Block),
			(0x8020, "Batch comment 2", CommentType.Todo),
		]);
		writer.AddCrossReferences([
			new CrossReference(0x8010, 0x8200, CrossRefType.Jmp),
		]);
		writer.AddMemoryRegions([
			new MemoryRegion(0x8000, 0xffff, (byte)MemoryRegionType.ROM, 0, "ROM"),
		]);

		var loader = new PansyLoader(writer.Generate());

		Assert.Equal(3, loader.Symbols.Count);
		Assert.Equal(3, loader.Comments.Count);
		Assert.Equal(2, loader.CrossReferences.Count);
		Assert.Equal(2, loader.MemoryRegions.Count);
	}

	[Fact]
	public void BatchSymbols_MultipleAtSameAddress_AllPreserved() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddSymbols([
			(0x8000, "Reset", SymbolType.Function),
			(0x8000, "EntryPoint", SymbolType.Label),
			(0x8000, "Main", SymbolType.InterruptVector),
		]);

		var loader = new PansyLoader(writer.Generate());

		var entries = loader.GetSymbolEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Equal(3, entries.Count);
	}

	[Fact]
	public void BatchComments_MultipleAtSameAddress_AllPreserved() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000
		};

		writer.AddComments([
			(0x8000, "Entry point", CommentType.Inline),
			(0x8000, "Called from NMI handler", CommentType.Block),
			(0x8000, "TODO: add bounds check", CommentType.Todo),
		]);

		var loader = new PansyLoader(writer.Generate());

		var entries = loader.GetCommentEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Equal(3, entries.Count);
	}

	[Fact]
	public void BatchSymbols_LargeBatch_AllPreserved() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_SNES,
			RomSize = 0x80000
		};

		var symbols = Enumerable.Range(0, 2000)
			.Select(i => ((uint)(0x8000 + i * 4), $"Symbol_{i:d4}", SymbolType.Label))
			.ToArray();

		writer.AddSymbols(symbols);

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(2000, loader.Symbols.Count);

		// Spot check first and last
		Assert.Equal("Symbol_0000", loader.GetSymbol(0x8000));
		Assert.Equal("Symbol_1999", loader.GetSymbol((int)(0x8000 + 1999 * 4)));
	}

	[Fact]
	public void BatchCrossRefs_WithCompression_RoundtripsAll() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x8000,
			EnableCompression = true
		};

		var xrefs = Enumerable.Range(0, 200)
			.Select(i => new CrossReference(
				(uint)(0x8000 + i * 3),
				(uint)(0x9000 + i * 5),
				(CrossRefType)(i % 5 + 1)))
			.ToArray();

		writer.AddCrossReferences(xrefs);

		var loader = new PansyLoader(writer.Generate());
		Assert.Equal(200, loader.CrossReferences.Count);
	}
}
