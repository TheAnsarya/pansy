// ============================================================================
// PansyMerger.cs - Pansy File Merge Operations
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

namespace Pansy.Core;

/// <summary>
/// Merges two Pansy files into one, combining symbols, comments, code/data maps,
/// cross-references, and memory regions.
/// </summary>
/// <remarks>
/// Merge strategy:
/// <list type="bullet">
/// <item>Symbols: Union of all entries at each address (overlay appended after base)</item>
/// <item>Comments: Union of all entries at each address (overlay appended after base)</item>
/// <item>Code/data offsets: Union (OR of all flags)</item>
/// <item>Cross-references: Union (deduplicated)</item>
/// <item>Memory regions: Union (overlay wins on overlap by name)</item>
/// <item>Header: ROM info from base, metadata merged (overlay wins on conflict)</item>
/// </list>
/// </remarks>
public static class PansyMerger {
	/// <summary>
	/// Merges two Pansy files. Base provides the foundation; overlay adds or overrides data.
	/// </summary>
	/// <param name="basePansy">The base Pansy file (provides ROM info and foundational data).</param>
	/// <param name="overlayPansy">The overlay Pansy file (adds or supplements data).</param>
	/// <returns>A PansyWriter ready to generate the merged file.</returns>
	public static PansyWriter Merge(PansyLoader basePansy, PansyLoader overlayPansy) {
		var writer = new PansyWriter {
			Platform = basePansy.Platform,
			RomSize = basePansy.RomSize,
			RomCrc32 = basePansy.RomCrc32,
			ProjectName = !string.IsNullOrEmpty(overlayPansy.ProjectName) ? overlayPansy.ProjectName : basePansy.ProjectName,
			Author = !string.IsNullOrEmpty(overlayPansy.Author) ? overlayPansy.Author : basePansy.Author,
			ProjectVersion = !string.IsNullOrEmpty(overlayPansy.ProjectVersion) ? overlayPansy.ProjectVersion : basePansy.ProjectVersion,
			EnableCompression = basePansy.IsCompressed || overlayPansy.IsCompressed,
		};

		MergeSymbols(writer, basePansy, overlayPansy);
		MergeComments(writer, basePansy, overlayPansy);
		MergeCodeDataFlags(writer, basePansy, overlayPansy);
		MergeCrossReferences(writer, basePansy, overlayPansy);
		MergeMemoryRegions(writer, basePansy, overlayPansy);
		MergeBookmarks(writer, basePansy, overlayPansy);
		MergeDataTypes(writer, basePansy, overlayPansy);
		MergeSourceMap(writer, basePansy, overlayPansy);

		return writer;
	}

	private static void MergeSymbols(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
		// Collect all addresses from both files
		var allAddresses = new HashSet<int>(basePansy.AllSymbolEntries.Keys);
		allAddresses.UnionWith(overlayPansy.AllSymbolEntries.Keys);

		foreach (var address in allAddresses) {
			var baseEntries = basePansy.GetSymbolEntries(address);
			var overlayEntries = overlayPansy.GetSymbolEntries(address);

			if (baseEntries != null) {
				foreach (var entry in baseEntries) {
					writer.AddSymbol((uint)address, entry.Name, entry.Type);
				}
			}

			if (overlayEntries != null) {
				foreach (var entry in overlayEntries) {
					// Skip exact duplicates already added from base
					if (baseEntries != null && baseEntries.Any(b => b.Name == entry.Name && b.Type == entry.Type)) {
						continue;
					}
					writer.AddSymbol((uint)address, entry.Name, entry.Type);
				}
			}
		}
	}

	private static void MergeComments(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
		var allAddresses = new HashSet<int>(basePansy.AllCommentEntries.Keys);
		allAddresses.UnionWith(overlayPansy.AllCommentEntries.Keys);

		foreach (var address in allAddresses) {
			var baseEntries = basePansy.GetCommentEntries(address);
			var overlayEntries = overlayPansy.GetCommentEntries(address);

			if (baseEntries != null) {
				foreach (var entry in baseEntries) {
					writer.AddComment((uint)address, entry.Text, (byte)entry.Type);
				}
			}

			if (overlayEntries != null) {
				foreach (var entry in overlayEntries) {
					if (baseEntries != null && baseEntries.Any(b => b.Text == entry.Text && b.Type == entry.Type)) {
						continue;
					}
					writer.AddComment((uint)address, entry.Text, (byte)entry.Type);
				}
			}
		}
	}

	private static void MergeCodeDataFlags(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
		// Union all code/data flag sets
		foreach (var offset in basePansy.CodeOffsets.Concat(overlayPansy.CodeOffsets).Distinct()) {
			writer.MarkAsCode((uint)offset);
		}
		foreach (var offset in basePansy.DataOffsets.Concat(overlayPansy.DataOffsets).Distinct()) {
			writer.MarkAsData((uint)offset);
		}
		foreach (var offset in basePansy.JumpTargets.Concat(overlayPansy.JumpTargets).Distinct()) {
			writer.MarkAsJumpTarget((uint)offset);
		}
		foreach (var offset in basePansy.SubEntryPoints.Concat(overlayPansy.SubEntryPoints).Distinct()) {
			writer.MarkAsSubroutine((uint)offset);
		}
		foreach (var offset in basePansy.OpcodeOffsets.Concat(overlayPansy.OpcodeOffsets).Distinct()) {
			writer.MarkAsOpcode((uint)offset);
		}
		foreach (var offset in basePansy.DrawnOffsets.Concat(overlayPansy.DrawnOffsets).Distinct()) {
			writer.MarkAsDrawn((uint)offset);
		}
		foreach (var offset in basePansy.ReadOffsets.Concat(overlayPansy.ReadOffsets).Distinct()) {
			writer.MarkAsRead((uint)offset);
		}
		foreach (var offset in basePansy.IndirectOffsets.Concat(overlayPansy.IndirectOffsets).Distinct()) {
			writer.MarkAsIndirect((uint)offset);
		}
	}

	private static void MergeCrossReferences(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
		var seen = new HashSet<(uint From, uint To, CrossRefType Type)>();

		foreach (var xref in basePansy.CrossReferences) {
			if (seen.Add((xref.From, xref.To, xref.Type))) {
				writer.AddCrossReference(xref);
			}
		}

		foreach (var xref in overlayPansy.CrossReferences) {
			if (seen.Add((xref.From, xref.To, xref.Type))) {
				writer.AddCrossReference(xref);
			}
		}
	}

	private static void MergeMemoryRegions(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
		// Use overlay regions as overrides by name, then add remaining base regions
		var overlayByName = new Dictionary<string, MemoryRegion>();
		foreach (var region in overlayPansy.MemoryRegions) {
			overlayByName[region.Name] = region;
		}

		var addedNames = new HashSet<string>();

		// Add base regions, substituting overlay versions where names match
		foreach (var region in basePansy.MemoryRegions) {
			if (overlayByName.TryGetValue(region.Name, out var overlayRegion)) {
				writer.AddMemoryRegion(overlayRegion);
				addedNames.Add(region.Name);
			} else {
				writer.AddMemoryRegion(region);
				addedNames.Add(region.Name);
			}
		}

		// Add overlay-only regions
		foreach (var region in overlayPansy.MemoryRegions) {
			if (!addedNames.Contains(region.Name)) {
				writer.AddMemoryRegion(region);
			}
		}
	}

	private static void MergeBookmarks(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
		var seen = new HashSet<(uint Address, string Name)>();

		foreach (var bookmark in basePansy.Bookmarks) {
			if (seen.Add((bookmark.Address, bookmark.Name))) {
				writer.AddBookmark(bookmark);
			}
		}

		foreach (var bookmark in overlayPansy.Bookmarks) {
			if (seen.Add((bookmark.Address, bookmark.Name))) {
				writer.AddBookmark(bookmark);
			}
		}
	}

	private static void MergeDataTypes(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
		var seen = new HashSet<(uint Address, string Name)>();

		foreach (var dt in basePansy.DataTypes) {
			if (seen.Add((dt.Address, dt.Name))) {
				writer.AddDataType(dt);
			}
		}

		foreach (var dt in overlayPansy.DataTypes) {
			if (seen.Add((dt.Address, dt.Name))) {
				writer.AddDataType(dt);
			}
		}
	}

	private static void MergeSourceMap(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
		// Build unified file table from both loaders, remapping indexes
		var fileToIndex = new Dictionary<string, ushort>();

		void AddFiles(IReadOnlyList<string> files) {
			foreach (var file in files) {
				if (!fileToIndex.ContainsKey(file)) {
					fileToIndex[file] = writer.AddSourceFile(file);
				}
			}
		}

		AddFiles(basePansy.SourceFiles);
		AddFiles(overlayPansy.SourceFiles);

		var seen = new HashSet<(uint RomAddress, ushort FileIndex, ushort Line, ushort Column)>();

		void AddEntries(PansyLoader loader) {
			foreach (var entry in loader.SourceMapEntries) {
				// Remap file index to the unified table
				var originalPath = loader.SourceFiles[entry.FileIndex];
				var newIndex = fileToIndex[originalPath];
				var remapped = entry with { FileIndex = newIndex };
				if (seen.Add((remapped.RomAddress, remapped.FileIndex, remapped.Line, remapped.Column))) {
					writer.AddSourceMapping(remapped);
				}
			}
		}

		AddEntries(basePansy);
		AddEntries(overlayPansy);
	}
}
