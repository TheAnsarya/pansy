// ============================================================================
// GraphExportTests.cs - Tests for Cross-Reference Graph Export
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using System.Text.Json;
using Pansy.Core;

namespace Pansy.Core.Tests;

public class GraphExportTests {
	private static PansyLoader MakeLoader(Action<PansyWriter> configure) {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_NES,
			RomSize = 0x20000,
			RomCrc32 = 0xdeadbeef,
		};
		configure(writer);
		return new PansyLoader(writer.Generate());
	}

	#region DOT Format Tests

	[Fact]
	public void ToDot_EmptyLoader_ReturnsValidEmptyGraph() {
		var loader = MakeLoader(_ => { });
		var dot = PansyGraphExporter.ToDot(loader);

		Assert.Contains("digraph CrossRefs {", dot);
		Assert.Contains("}", dot);
		Assert.DoesNotContain("->", dot);
	}

	[Fact]
	public void ToDot_EmitsEdgesWithTypeLabels() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8010, 0x8200, CrossRefType.Jmp));
		});

		var dot = PansyGraphExporter.ToDot(loader);

		Assert.Contains("\"$8000\" -> \"$8100\"", dot);
		Assert.Contains("label=\"JSR\"", dot);
		Assert.Contains("\"$8010\" -> \"$8200\"", dot);
		Assert.Contains("label=\"JMP\"", dot);
	}

	[Fact]
	public void ToDot_IncludesSymbolNamesInNodeLabels() {
		var loader = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.Label);
			w.AddSymbol(0x8100, "MainLoop", SymbolType.Function);
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var dot = PansyGraphExporter.ToDot(loader);

		Assert.Contains("Reset", dot);
		Assert.Contains("MainLoop", dot);
	}

	[Fact]
	public void ToDot_FilterByType_OnlyIncludesMatchingEdges() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8010, 0x8200, CrossRefType.Read));
		});

		var dot = PansyGraphExporter.ToDot(loader, CrossRefType.Jsr);

		Assert.Contains("$8000", dot);
		Assert.Contains("$8100", dot);
		Assert.DoesNotContain("$8010", dot);
		Assert.DoesNotContain("$8200", dot);
	}

	[Fact]
	public void ToDot_AllCrossRefTypes_HaveDistinctColors() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x1000, 0x2000, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x1000, 0x3000, CrossRefType.Jmp));
			w.AddCrossReference(new CrossReference(0x1000, 0x4000, CrossRefType.Branch));
			w.AddCrossReference(new CrossReference(0x1000, 0x5000, CrossRefType.Read));
			w.AddCrossReference(new CrossReference(0x1000, 0x6000, CrossRefType.Write));
		});

		var dot = PansyGraphExporter.ToDot(loader);

		Assert.Contains("color=blue", dot);
		Assert.Contains("color=red", dot);
		Assert.Contains("color=green", dot);
		Assert.Contains("color=orange", dot);
		Assert.Contains("color=purple", dot);
	}

	#endregion

	#region GraphML Format Tests

	[Fact]
	public void ToGraphML_EmptyLoader_ReturnsValidXml() {
		var loader = MakeLoader(_ => { });
		var graphml = PansyGraphExporter.ToGraphML(loader);

		Assert.Contains("<?xml", graphml);
		Assert.Contains("<graphml", graphml);
		Assert.Contains("<graph", graphml);
		Assert.Contains("</graphml>", graphml);
	}

	[Fact]
	public void ToGraphML_EmitsNodesAndEdges() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var graphml = PansyGraphExporter.ToGraphML(loader);

		Assert.Contains("<node id=\"n8000\"", graphml);
		Assert.Contains("<node id=\"n8100\"", graphml);
		Assert.Contains("<edge id=\"e0\"", graphml);
		Assert.Contains("source=\"n8000\"", graphml);
		Assert.Contains("target=\"n8100\"", graphml);
	}

	[Fact]
	public void ToGraphML_IncludesSymbolData() {
		var loader = MakeLoader(w => {
			w.AddSymbol(0x8000, "Reset", SymbolType.Label);
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var graphml = PansyGraphExporter.ToGraphML(loader);

		Assert.Contains("Reset", graphml);
	}

	[Fact]
	public void ToGraphML_FilterByType_ExcludesNonMatching() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x8010, 0x8200, CrossRefType.Read));
		});

		var graphml = PansyGraphExporter.ToGraphML(loader, CrossRefType.Read);

		Assert.DoesNotContain("n8000", graphml);
		Assert.Contains("n8010", graphml);
		Assert.Contains("n8200", graphml);
	}

	[Fact]
	public void ToGraphML_EscapesXmlSpecialChars() {
		var loader = MakeLoader(w => {
			w.AddSymbol(0x8000, "Load<A&B>", SymbolType.Label);
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var graphml = PansyGraphExporter.ToGraphML(loader);

		Assert.Contains("Load&lt;A&amp;B&gt;", graphml);
		Assert.DoesNotContain("Load<A&B>", graphml);
	}

	#endregion

	#region JSON Format Tests

	[Fact]
	public void ToJson_EmptyLoader_ReturnsValidJson() {
		var loader = MakeLoader(_ => { });
		var json = PansyGraphExporter.ToJson(loader);

		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;
		Assert.Equal(0, root.GetProperty("nodes").GetArrayLength());
		Assert.Equal(0, root.GetProperty("edges").GetArrayLength());
	}

	[Fact]
	public void ToJson_EmitsNodesAndEdges() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var json = PansyGraphExporter.ToJson(loader);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		Assert.Equal(2, root.GetProperty("nodes").GetArrayLength());
		Assert.Equal(1, root.GetProperty("edges").GetArrayLength());

		var edge = root.GetProperty("edges")[0];
		Assert.Equal("$8000", edge.GetProperty("from").GetString());
		Assert.Equal("$8100", edge.GetProperty("to").GetString());
		Assert.Equal("Jsr", edge.GetProperty("type").GetString());
	}

	[Fact]
	public void ToJson_IncludesSymbolNames() {
		var loader = MakeLoader(w => {
			w.AddSymbol(0x8000, "NMI", SymbolType.InterruptVector);
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var json = PansyGraphExporter.ToJson(loader);
		var doc = JsonDocument.Parse(json);
		var nodes = doc.RootElement.GetProperty("nodes");

		var nmiNode = nodes.EnumerateArray().First(n => n.GetProperty("id").GetString() == "$8000");
		Assert.Equal("NMI", nmiNode.GetProperty("symbol").GetString());
	}

	[Fact]
	public void ToJson_IncludesMetadata() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
		});

		var json = PansyGraphExporter.ToJson(loader);
		var doc = JsonDocument.Parse(json);
		var meta = doc.RootElement.GetProperty("metadata");

		Assert.Equal("NES", meta.GetProperty("platform").GetString());
		Assert.Equal(0x20000u, meta.GetProperty("romSize").GetUInt32());
		Assert.Equal(1, meta.GetProperty("totalCrossRefs").GetInt32());
		Assert.Equal(2, meta.GetProperty("nodeCount").GetInt32());
	}

	[Fact]
	public void ToJson_FilterByType_OnlyIncludesMatching() {
		var loader = MakeLoader(w => {
			w.AddCrossReference(new CrossReference(0x8000, 0x8100, CrossRefType.Jsr));
			w.AddCrossReference(new CrossReference(0x9000, 0x9100, CrossRefType.Write));
		});

		var json = PansyGraphExporter.ToJson(loader, CrossRefType.Write);
		var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		Assert.Equal(2, root.GetProperty("nodes").GetArrayLength());
		Assert.Equal(1, root.GetProperty("edges").GetArrayLength());
		Assert.Equal(1, root.GetProperty("metadata").GetProperty("totalCrossRefs").GetInt32());
	}

	#endregion
}
