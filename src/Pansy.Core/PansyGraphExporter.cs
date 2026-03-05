// ============================================================================
// PansyGraphExporter.cs - Cross-Reference Graph Export
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using System.Text;
using System.Text.Json;

namespace Pansy.Core;

/// <summary>
/// Exports cross-reference data from a Pansy file to graph formats (DOT, GraphML, JSON)
/// for visualization and analysis in external tools.
/// </summary>
public static class PansyGraphExporter {
	/// <summary>
	/// Exports cross-references to Graphviz DOT format.
	/// </summary>
	/// <param name="loader">The Pansy file to export from.</param>
	/// <param name="filter">Optional cross-reference type filter. Null exports all types.</param>
	/// <returns>DOT format string.</returns>
	public static string ToDot(PansyLoader loader, CrossRefType? filter = null) {
		var sb = new StringBuilder();
		sb.AppendLine("digraph CrossRefs {");
		sb.AppendLine("\trankdir=LR;");
		sb.AppendLine("\tnode [shape=box, fontname=\"Consolas\"];");
		sb.AppendLine("\tedge [fontname=\"Consolas\", fontsize=10];");

		var xrefs = filter.HasValue
			? loader.GetCrossRefsByType(filter.Value)
			: loader.CrossReferences.AsEnumerable();

		// Collect all addresses to emit node labels with symbol names
		var addresses = new HashSet<uint>();
		var edges = new List<CrossReference>();
		foreach (var xref in xrefs) {
			addresses.Add(xref.From);
			addresses.Add(xref.To);
			edges.Add(xref);
		}

		// Emit nodes with symbol names where available
		foreach (var addr in addresses.Order()) {
			var symbol = loader.GetSymbol((int)addr);
			var label = symbol != null ? $"{symbol}\\n${addr:x}" : $"${addr:x}";
			sb.AppendLine($"\t\"${addr:x}\" [label=\"{label}\"];");
		}

		sb.AppendLine();

		// Emit edges
		foreach (var xref in edges) {
			var typeLabel = xref.Type switch {
				CrossRefType.Jsr => "JSR",
				CrossRefType.Jmp => "JMP",
				CrossRefType.Branch => "BRA",
				CrossRefType.Read => "READ",
				CrossRefType.Write => "WRITE",
				_ => "?"
			};
			var color = xref.Type switch {
				CrossRefType.Jsr => "blue",
				CrossRefType.Jmp => "red",
				CrossRefType.Branch => "green",
				CrossRefType.Read => "orange",
				CrossRefType.Write => "purple",
				_ => "black"
			};
			sb.AppendLine($"\t\"${xref.From:x}\" -> \"${xref.To:x}\" [label=\"{typeLabel}\", color={color}];");
		}

		sb.AppendLine("}");
		return sb.ToString();
	}

	/// <summary>
	/// Exports cross-references to GraphML format (XML-based graph exchange format).
	/// </summary>
	public static string ToGraphML(PansyLoader loader, CrossRefType? filter = null) {
		var sb = new StringBuilder();
		sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		sb.AppendLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\"");
		sb.AppendLine("         xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
		sb.AppendLine("         xsi:schemaLocation=\"http://graphml.graphdrawing.org/xmlns http://graphml.graphdrawing.org/xmlns/1.0/graphml.xsd\">");
		sb.AppendLine("\t<key id=\"d0\" for=\"node\" attr.name=\"address\" attr.type=\"string\"/>");
		sb.AppendLine("\t<key id=\"d1\" for=\"node\" attr.name=\"symbol\" attr.type=\"string\"/>");
		sb.AppendLine("\t<key id=\"d2\" for=\"edge\" attr.name=\"type\" attr.type=\"string\"/>");
		sb.AppendLine("\t<graph id=\"G\" edgemode=\"directed\">");

		var xrefs = filter.HasValue
			? loader.GetCrossRefsByType(filter.Value)
			: loader.CrossReferences.AsEnumerable();

		var addresses = new HashSet<uint>();
		var edges = new List<CrossReference>();
		foreach (var xref in xrefs) {
			addresses.Add(xref.From);
			addresses.Add(xref.To);
			edges.Add(xref);
		}

		foreach (var addr in addresses.Order()) {
			var symbol = loader.GetSymbol((int)addr);
			sb.AppendLine($"\t\t<node id=\"n{addr:x}\">");
			sb.AppendLine($"\t\t\t<data key=\"d0\">${addr:x}</data>");
			if (symbol != null) {
				sb.AppendLine($"\t\t\t<data key=\"d1\">{EscapeXml(symbol)}</data>");
			}
			sb.AppendLine("\t\t</node>");
		}

		int edgeId = 0;
		foreach (var xref in edges) {
			sb.AppendLine($"\t\t<edge id=\"e{edgeId++}\" source=\"n{xref.From:x}\" target=\"n{xref.To:x}\">");
			sb.AppendLine($"\t\t\t<data key=\"d2\">{xref.Type}</data>");
			sb.AppendLine("\t\t</edge>");
		}

		sb.AppendLine("\t</graph>");
		sb.AppendLine("</graphml>");
		return sb.ToString();
	}

	/// <summary>
	/// Exports cross-references to a JSON format suitable for web-based visualization tools.
	/// </summary>
	public static string ToJson(PansyLoader loader, CrossRefType? filter = null) {
		var xrefs = filter.HasValue
			? loader.GetCrossRefsByType(filter.Value).ToList()
			: loader.CrossReferences.ToList();

		var addresses = new HashSet<uint>();
		foreach (var xref in xrefs) {
			addresses.Add(xref.From);
			addresses.Add(xref.To);
		}

		var nodes = addresses.Order().Select(addr => {
			var symbol = loader.GetSymbol((int)addr);
			var dict = new Dictionary<string, object> {
				["id"] = $"${addr:x}",
				["address"] = addr,
			};
			if (symbol != null) {
				dict["symbol"] = symbol;
			}
			return dict;
		}).ToList();

		var edgesList = xrefs.Select(xref => new Dictionary<string, object> {
			["from"] = $"${xref.From:x}",
			["to"] = $"${xref.To:x}",
			["type"] = xref.Type.ToString(),
		}).ToList();

		var graph = new Dictionary<string, object> {
			["nodes"] = nodes,
			["edges"] = edgesList,
			["metadata"] = new Dictionary<string, object> {
				["platform"] = PansyLoader.GetPlatformName(loader.Platform),
				["romSize"] = loader.RomSize,
				["totalCrossRefs"] = xrefs.Count,
				["nodeCount"] = nodes.Count,
			}
		};

		return JsonSerializer.Serialize(graph, new JsonSerializerOptions {
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		});
	}

	private static string EscapeXml(string value) =>
		value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
