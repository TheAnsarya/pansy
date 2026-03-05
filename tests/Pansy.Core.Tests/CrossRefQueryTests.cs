// ============================================================================
// CrossRefQueryTests.cs - Tests for Cross-Reference Query APIs
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using Pansy.Core;

namespace Pansy.Core.Tests;

public class CrossRefQueryTests {
	private static PansyLoader MakeLoader(Action<PansyWriter> configure) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000,
			RomCrc32 = 0xdeadbeef,
		};
		configure(writer);
		return new PansyLoader(writer.Generate());
	}

	#region GetCrossRefsTo Tests

	[Fact]
	public void GetCrossRefsTo_ReturnsAllRefsToAddress() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8050, 0x8100, CrossRefType.Jmp));
			w.AddCrossReference(new CrossReference(0x8080, 0x8200, CrossRefType.Branch));
		});

		var refsTo8100 = loader.GetCrossRefsTo(0x8100);
		Assert.Equal(2, refsTo8100.Count);
		Assert.Contains(refsTo8100, x => x.From == 0x8000);
		Assert.Contains(refsTo8100, x => x.From == 0x8050);
	}

	[Fact]
	public void GetCrossRefsTo_ReturnsEmptyForNoRefs() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var refs = loader.GetCrossRefsTo(0x9999);
		Assert.Empty(refs);
	}

	#endregion

	#region GetCrossRefsFrom Tests

	[Fact]
	public void GetCrossRefsFrom_ReturnsAllRefsFromAddress() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8000, 0x8200, CrossRefType.Jmp));
			w.AddCrossReference(new CrossReference(0x8050, 0x8300, CrossRefType.Branch));
		});

		var refsFrom8000 = loader.GetCrossRefsFrom(0x8000);
		Assert.Equal(2, refsFrom8000.Count);
		Assert.Contains(refsFrom8000, x => x.To == 0x8100);
		Assert.Contains(refsFrom8000, x => x.To == 0x8200);
	}

	[Fact]
	public void GetCrossRefsFrom_ReturnsEmptyForNoRefs() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var refs = loader.GetCrossRefsFrom(0x9999);
		Assert.Empty(refs);
	}

	#endregion

	#region GetCrossRefsByType Tests

	[Fact]
	public void GetCrossRefsByType_FiltersCorrectly() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8010, 0x8200, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8020, 0x8300, CrossRefType.Jmp));
			w.AddCrossReference(new CrossReference(0x8030, 0x8400, CrossRefType.Branch));
			w.AddCrossReference(new CrossReference(0x8040, 0x9000, CrossRefType.Read));
			w.AddCrossReference(new CrossReference(0x8050, 0x9100, CrossRefType.Write));
		});

		Assert.Equal(2, loader.GetCrossRefsByType(CrossRefType.Jsr).Count());
		Assert.Single(loader.GetCrossRefsByType(CrossRefType.Jmp));
		Assert.Single(loader.GetCrossRefsByType(CrossRefType.Branch));
		Assert.Single(loader.GetCrossRefsByType(CrossRefType.Read));
		Assert.Single(loader.GetCrossRefsByType(CrossRefType.Write));
	}

	#endregion

	#region Range Query Tests

	[Fact]
	public void GetCrossRefsFromRange_FiltersCorrectly() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x9000, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8100, 0x9100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8200, 0x9200, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0xa000, 0x9300, CrossRefType.Jsr));
		});

		var inRange = loader.GetCrossRefsFromRange(0x8000, 0x8100).ToList();
		Assert.Equal(2, inRange.Count);
	}

	[Fact]
	public void GetCrossRefsToRange_FiltersCorrectly() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x9000, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8100, 0x9100, CrossRefType.Jmp));
			w.AddCrossReference(new CrossReference(0x8200, 0xa000, CrossRefType.Branch));
		});

		var inRange = loader.GetCrossRefsToRange(0x9000, 0x9100).ToList();
		Assert.Equal(2, inRange.Count);
	}

	#endregion

	#region GetReferenceCount Tests

	[Fact]
	public void GetReferenceCount_ReturnsCorrectCount() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8010, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8020, 0x8100, CrossRefType.Jmp));
			w.AddCrossReference(new CrossReference(0x8030, 0x8200, CrossRefType.Jsr));
		});

		Assert.Equal(3, loader.GetReferenceCount(0x8100));
		Assert.Equal(1, loader.GetReferenceCount(0x8200));
		Assert.Equal(0, loader.GetReferenceCount(0x9999));
	}

	#endregion

	#region GetUnreferencedSubroutines Tests

	[Fact]
	public void GetUnreferencedSubroutines_FindsOrphanedCode() {
		var loader = MakeLoader(w => {
			w.MarkAsSubroutine(0x8000);
			w.MarkAsSubroutine(0x8100);
			w.MarkAsSubroutine(0x8200);
			// Only 0x8000 and 0x8100 are referenced
			w.AddCrossReference(new CrossReference(0x9000, 0x8000, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x9010, 0x8100, CrossRefType.Jsr));
		});

		var unreferenced = loader.GetUnreferencedSubroutines().ToList();
		Assert.Single(unreferenced);
		Assert.Equal(0x8200, unreferenced[0]);
	}

	[Fact]
	public void GetUnreferencedSubroutines_EmptyWhenAllReferenced() {
		var loader = MakeLoader(w => {
			w.MarkAsSubroutine(0x8000);
			w.AddCrossReference(new CrossReference(0x9000, 0x8000, CrossRefType.Jsr));
		});

		Assert.Empty(loader.GetUnreferencedSubroutines());
	}

	#endregion

	#region GetMostReferencedAddresses Tests

	[Fact]
	public void GetMostReferencedAddresses_SortedByCount() {
		var loader = MakeLoader(w => {
			// 0x8100 referenced 3 times
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8010, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8020, 0x8100, CrossRefType.Jsr));
			// 0x8200 referenced 1 time
			w.AddCrossReference(new CrossReference(0x8030, 0x8200, CrossRefType.Jsr));
			// 0x8300 referenced 2 times
			w.AddCrossReference(new CrossReference(0x8040, 0x8300, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8050, 0x8300, CrossRefType.Jmp));
		});

		var top = loader.GetMostReferencedAddresses(3).ToList();
		Assert.Equal(3, top.Count);
		Assert.Equal(0x8100, top[0].Address);
		Assert.Equal(3, top[0].Count);
		Assert.Equal(0x8300, top[1].Address);
		Assert.Equal(2, top[1].Count);
		Assert.Equal(0x8200, top[2].Address);
		Assert.Equal(1, top[2].Count);
	}

	[Fact]
	public void GetMostReferencedAddresses_RespectsLimit() {
		var loader = MakeLoader(w => {
			for (uint i = 0; i < 50; i++) {
				w.AddCrossReference(new CrossReference(0x8000 + i, 0x9000 + i, CrossRefType.Jsr));
			}
		});

		var top = loader.GetMostReferencedAddresses(5).ToList();
		Assert.Equal(5, top.Count);
	}

	#endregion

	#region GetCrossRefStats Tests

	[Fact]
	public void GetCrossRefStats_ReturnsCorrectCounts() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8010, 0x8200, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8020, 0x8300, CrossRefType.Jmp));
			w.AddCrossReference(new CrossReference(0x8030, 0x8400, CrossRefType.Branch));
			w.AddCrossReference(new CrossReference(0x8040, 0x9000, CrossRefType.Read));
			w.AddCrossReference(new CrossReference(0x8050, 0x9100, CrossRefType.Write));
			w.AddCrossReference(new CrossReference(0x8060, 0x9200, CrossRefType.Write));
		});

		var stats = loader.GetCrossRefStats();
		Assert.Equal(7, stats.TotalXrefs);
		Assert.Equal(2, stats.JsrCount);
		Assert.Equal(1, stats.JmpCount);
		Assert.Equal(1, stats.BranchCount);
		Assert.Equal(1, stats.ReadCount);
		Assert.Equal(2, stats.WriteCount);
	}

	[Fact]
	public void GetCrossRefStats_EmptyFile_ReturnsZeros() {
		var loader = MakeLoader(w => { });

		var stats = loader.GetCrossRefStats();
		Assert.Equal(0, stats.TotalXrefs);
		Assert.Equal(0, stats.JsrCount);
	}

	#endregion

	#region No Cross-Refs Edge Cases

	[Fact]
	public void NoCrossRefs_AllQueriesReturnEmpty() {
		var loader = MakeLoader(w => {
			w.AddSymbol(0x8000, "Main");
		});

		Assert.Empty(loader.GetCrossRefsTo(0x8000));
		Assert.Empty(loader.GetCrossRefsFrom(0x8000));
		Assert.Empty(loader.GetCrossRefsByType(CrossRefType.Jsr));
		Assert.Empty(loader.GetCrossRefsFromRange(0x8000, 0xffff));
		Assert.Empty(loader.GetCrossRefsToRange(0x8000, 0xffff));
		Assert.Equal(0, loader.GetReferenceCount(0x8000));
		Assert.Empty(loader.GetMostReferencedAddresses());
	}

	#endregion
}
