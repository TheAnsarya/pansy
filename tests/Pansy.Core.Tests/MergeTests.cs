// ============================================================================
// MergeTests.cs - Tests for PansyMerger Functionality
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;

namespace Pansy.Core.Tests;

public class MergeTests {
	private static PansyLoader MakeLoader(Action<PansyWriter> configure) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000,
			RomCrc32 = 0xdeadbeef,
		};
		configure(writer);
		return new PansyLoader(writer.Generate());
	}

	#region Symbol Merge Tests

	[Fact]
	public void Merge_Symbols_UnionFromBothFiles() {
		var basePansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.Label);
			w.AddSymbol(0x8100, "Loop", SymbolType.Label);
		});

		var overlayPansy = MakeLoader(w => {
			w.AddSymbol(0x8200, "Exit", SymbolType.Label);
			w.AddSymbol(0x8300, "Init", SymbolType.Function);
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal(4, loader.SymbolEntries.Count);
		Assert.Equal("Reset", loader.GetSymbol(0x8000));
		Assert.Equal("Loop", loader.GetSymbol(0x8100));
		Assert.Equal("Exit", loader.GetSymbol(0x8200));
		Assert.Equal("Init", loader.GetSymbol(0x8300));
	}

	[Fact]
	public void Merge_Symbols_OverlayAddsToSameAddress() {
		var basePansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.InterruptVector);
		});

		var overlayPansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Main", SymbolType.Function);
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		var entries = loader.GetSymbolEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Equal(2, entries.Count);
		Assert.Equal("Reset", entries[0].Name);
		Assert.Equal("Main", entries[1].Name);
	}

	[Fact]
	public void Merge_Symbols_DeduplicatesExactMatches() {
		var basePansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.Label);
		});

		var overlayPansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.Label); // exact duplicate
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		var entries = loader.GetSymbolEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Single(entries);
	}

	[Fact]
	public void Merge_Symbols_SameNameDifferentType_BothKept() {
		var basePansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.Label);
		});

		var overlayPansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.InterruptVector); // same name, different type
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		var entries = loader.GetSymbolEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Equal(2, entries.Count);
	}

	#endregion

	#region Comment Merge Tests

	[Fact]
	public void Merge_Comments_UnionFromBothFiles() {
		var basePansy = MakeLoader(w => {
			w.AddComment(0x8000, "Entry point", (byte)CommentType.Inline);
		});

		var overlayPansy = MakeLoader(w => {
			w.AddComment(0x8100, "Loop start", (byte)CommentType.Block);
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal(2, loader.CommentEntries.Count);
		Assert.Equal("Entry point", loader.GetComment(0x8000));
		Assert.Equal("Loop start", loader.GetComment(0x8100));
	}

	[Fact]
	public void Merge_Comments_SameAddress_BothPreserved() {
		var basePansy = MakeLoader(w => {
			w.AddComment(0x8000, "Base comment", (byte)CommentType.Block);
		});

		var overlayPansy = MakeLoader(w => {
			w.AddComment(0x8000, "Overlay comment", (byte)CommentType.Inline);
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		var entries = loader.GetCommentEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Equal(2, entries.Count);
		Assert.Equal("Base comment", entries[0].Text);
		Assert.Equal("Overlay comment", entries[1].Text);
	}

	[Fact]
	public void Merge_Comments_DeduplicatesExactMatches() {
		var basePansy = MakeLoader(w => {
			w.AddComment(0x8000, "Same comment", (byte)CommentType.Inline);
		});

		var overlayPansy = MakeLoader(w => {
			w.AddComment(0x8000, "Same comment", (byte)CommentType.Inline);
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		var entries = loader.GetCommentEntries(0x8000);
		Assert.NotNull(entries);
		Assert.Single(entries);
	}

	#endregion

	#region Code/Data Map Merge Tests

	[Fact]
	public void Merge_CodeDataFlags_Union() {
		var basePansy = MakeLoader(w => {
			w.MarkAsCode(0x8000);
			w.MarkAsCode(0x8001);
			w.MarkAsJumpTarget(0x8010);
		});

		var overlayPansy = MakeLoader(w => {
			w.MarkAsCode(0x8002);
			w.MarkAsData(0x9000);
			w.MarkAsSubroutine(0x8010);
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.True(loader.IsCode(0x8000));
		Assert.True(loader.IsCode(0x8001));
		Assert.True(loader.IsCode(0x8002));
		Assert.True(loader.IsData(0x9000));
		Assert.True(loader.IsJumpTarget(0x8010));
		Assert.True(loader.IsSubEntryPoint(0x8010));
	}

	[Fact]
	public void Merge_AllCdlFlags_Union() {
		var basePansy = MakeLoader(w => {
			w.MarkAsOpcode(0x100);
			w.MarkAsDrawn(0x200);
		});

		var overlayPansy = MakeLoader(w => {
			w.MarkAsRead(0x300);
			w.MarkAsIndirect(0x400);
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.True(loader.IsOpcode(0x100));
		Assert.True(loader.IsDrawn(0x200));
		Assert.True(loader.IsRead(0x300));
		Assert.True(loader.IsIndirect(0x400));
	}

	#endregion

	#region Cross-Reference Merge Tests

	[Fact]
	public void Merge_CrossRefs_Union() {
		var basePansy = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var overlayPansy = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8200, 0x8300, CrossRefType.Jmp));
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal(2, loader.CrossReferences.Count);
	}

	[Fact]
	public void Merge_CrossRefs_Deduplicated() {
		var basePansy = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var overlayPansy = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr)); // duplicate
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jmp)); // same addr, different type
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal(2, loader.CrossReferences.Count);
	}

	#endregion

	#region Memory Region Merge Tests

	[Fact]
	public void Merge_MemoryRegions_Union() {
		var basePansy = MakeLoader(w => {
			w.AddMemoryRegion(new MemoryRegion(0x8000, 0xbfff, (byte)MemoryRegionType.ROM, 0, "PRG-ROM0"));
		});

		var overlayPansy = MakeLoader(w => {
			w.AddMemoryRegion(new MemoryRegion(0xc000, 0xffff, (byte)MemoryRegionType.ROM, 1, "PRG-ROM1"));
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal(2, loader.MemoryRegions.Count);
	}

	[Fact]
	public void Merge_MemoryRegions_OverlayWinsByName() {
		var basePansy = MakeLoader(w => {
			w.AddMemoryRegion(new MemoryRegion(0x8000, 0xbfff, (byte)MemoryRegionType.ROM, 0, "PRG-ROM"));
		});

		var overlayPansy = MakeLoader(w => {
			w.AddMemoryRegion(new MemoryRegion(0x8000, 0xffff, (byte)MemoryRegionType.ROM, 0, "PRG-ROM")); // updated range
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Single(loader.MemoryRegions);
		Assert.Equal(0xffffu, loader.MemoryRegions[0].End); // overlay's value
	}

	#endregion

	#region Header/Metadata Merge Tests

	[Fact]
	public void Merge_Header_BaseRomInfo() {
		var basePansy = MakeLoader(w => {
			w.Platform = PansyLoader.PLATFORM_SNES;
			w.RomSize = 0x80000;
			w.RomCrc32 = 0x12345678;
		});

		var overlayPansy = MakeLoader(w => {
			w.Platform = PansyLoader.PLATFORM_NES; // should be ignored
			w.RomSize = 0x40000; // should be ignored
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal(PansyLoader.PLATFORM_SNES, loader.Platform);
		Assert.Equal(0x80000u, loader.RomSize);
		Assert.Equal(0x12345678u, loader.RomCrc32);
	}

	[Fact]
	public void Merge_Metadata_OverlayWins() {
		var basePansy = MakeLoader(w => {
			w.ProjectName = "Base Project";
			w.Author = "Base Author";
			w.ProjectVersion = "1.0";
		});

		var overlayPansy = MakeLoader(w => {
			w.ProjectName = "Overlay Project";
			w.Author = "Overlay Author";
			w.ProjectVersion = "2.0";
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal("Overlay Project", loader.ProjectName);
		Assert.Equal("Overlay Author", loader.Author);
		Assert.Equal("2.0", loader.ProjectVersion);
	}

	[Fact]
	public void Merge_Metadata_FallsBackToBase() {
		var basePansy = MakeLoader(w => {
			w.ProjectName = "Base Project";
			w.Author = "Base Author";
		});

		var overlayPansy = MakeLoader(w => {
			// No metadata set
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal("Base Project", loader.ProjectName);
		Assert.Equal("Base Author", loader.Author);
	}

	#endregion

	#region Empty File Merge Tests

	[Fact]
	public void Merge_EmptyBase_ReturnsOverlayData() {
		var basePansy = MakeLoader(w => { });

		var overlayPansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Main");
			w.MarkAsCode(0x8000);
		});

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal("Main", loader.GetSymbol(0x8000));
		Assert.True(loader.IsCode(0x8000));
	}

	[Fact]
	public void Merge_EmptyOverlay_ReturnsBaseData() {
		var basePansy = MakeLoader(w => {
			w.AddSymbol(0x8000, "Main");
			w.MarkAsCode(0x8000);
		});

		var overlayPansy = MakeLoader(w => { });

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Equal("Main", loader.GetSymbol(0x8000));
		Assert.True(loader.IsCode(0x8000));
	}

	[Fact]
	public void Merge_BothEmpty_ReturnsEmpty() {
		var basePansy = MakeLoader(w => { });
		var overlayPansy = MakeLoader(w => { });

		var merged = PansyMerger.Merge(basePansy, overlayPansy);
		var loader = new PansyLoader(merged.Generate());

		Assert.Empty(loader.Symbols);
		Assert.Empty(loader.Comments);
		Assert.Empty(loader.CrossReferences);
		Assert.Empty(loader.MemoryRegions);
	}

	#endregion

	#region Use-Case Tests

	[Fact]
	public void Merge_SymbolsFile_WithCdlFile_CombinesAll() {
		// Simulates the FFMQ use case from issue #16
		var symbolsPansy = MakeLoader(w => {
			w.ProjectName = "FFMQ";
			w.AddSymbol(0x8000, "Main", SymbolType.Label);
			w.AddSymbol(0x8100, "VBlank", SymbolType.InterruptVector);
			w.AddComment(0x8000, "Program entry point");
			w.AddCrossReference(new CrossReference(0x8005, 0x8100, CrossRefType.Jsr));
		});

		var cdlPansy = MakeLoader(w => {
			w.MarkAsCode(0x8000);
			w.MarkAsCode(0x8001);
			w.MarkAsCode(0x8002);
			w.MarkAsData(0x9000);
			w.MarkAsJumpTarget(0x8100);
			w.MarkAsSubroutine(0x8100);
		});

		var merged = PansyMerger.Merge(symbolsPansy, cdlPansy);
		var loader = new PansyLoader(merged.Generate());

		// Symbols from base
		Assert.Equal("Main", loader.GetSymbol(0x8000));
		Assert.Equal("VBlank", loader.GetSymbol(0x8100));

		// Comments from base
		Assert.Equal("Program entry point", loader.GetComment(0x8000));

		// CDL from overlay
		Assert.True(loader.IsCode(0x8000));
		Assert.True(loader.IsData(0x9000));
		Assert.True(loader.IsJumpTarget(0x8100));
		Assert.True(loader.IsSubEntryPoint(0x8100));

		// Cross-refs from base
		Assert.Single(loader.CrossReferences);

		// Metadata from base
		Assert.Equal("FFMQ", loader.ProjectName);
	}

	#endregion
}
