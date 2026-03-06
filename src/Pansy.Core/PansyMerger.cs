// ============================================================================
// PansyMerger.cs - Pansy File Merge Operations
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using System.Collections.Concurrent;

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

		// Collect order-independent merge results in parallel
		var codeFlags = new ConcurrentBag<(uint Offset, byte FlagType)>();
		var crossRefs = new ConcurrentBag<CrossReference>();
		var regions = new ConcurrentBag<MemoryRegion>();
		var bookmarks = new ConcurrentBag<Bookmark>();
		var dataTypes = new ConcurrentBag<DataTypeEntry>();

		Parallel.Invoke(
			() => CollectCodeDataFlags(codeFlags, basePansy, overlayPansy),
			() => CollectCrossReferences(crossRefs, basePansy, overlayPansy),
			() => CollectMemoryRegions(regions, basePansy, overlayPansy),
			() => CollectBookmarks(bookmarks, basePansy, overlayPansy),
			() => CollectDataTypes(dataTypes, basePansy, overlayPansy)
		);

		// Symbols and comments must be sequential to preserve base-first ordering
		MergeSymbols(writer, basePansy, overlayPansy);
		MergeComments(writer, basePansy, overlayPansy);

		// Apply parallel-collected results to writer
		foreach (var (offset, flagType) in codeFlags) {
			switch (flagType) {
				case 1: writer.MarkAsCode(offset); break;
				case 2: writer.MarkAsData(offset); break;
				case 3: writer.MarkAsJumpTarget(offset); break;
				case 4: writer.MarkAsSubroutine(offset); break;
				case 5: writer.MarkAsOpcode(offset); break;
				case 6: writer.MarkAsDrawn(offset); break;
				case 7: writer.MarkAsRead(offset); break;
				case 8: writer.MarkAsIndirect(offset); break;
			}
		}
		foreach (var xref in crossRefs) {
			writer.AddCrossReference(xref);
		}
		foreach (var region in regions) {
			writer.AddMemoryRegion(region);
		}
		foreach (var bookmark in bookmarks) {
			writer.AddBookmark(bookmark);
		}
		foreach (var dt in dataTypes) {
			writer.AddDataType(dt);
		}

		// Source map merge must be sequential (needs file index remapping)
		MergeSourceMap(writer, basePansy, overlayPansy);

		return writer;
	}

	private static void MergeSymbols(PansyWriter writer, PansyLoader basePansy, PansyLoader overlayPansy) {
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

	private static void CollectCodeDataFlags(
		ConcurrentBag<(uint Offset, byte FlagType)> bag,
		PansyLoader basePansy, PansyLoader overlayPansy) {
		foreach (var offset in basePansy.CodeOffsets.Concat(overlayPansy.CodeOffsets).Distinct()) {
			bag.Add(((uint)offset, 1));
		}
		foreach (var offset in basePansy.DataOffsets.Concat(overlayPansy.DataOffsets).Distinct()) {
			bag.Add(((uint)offset, 2));
		}
		foreach (var offset in basePansy.JumpTargets.Concat(overlayPansy.JumpTargets).Distinct()) {
			bag.Add(((uint)offset, 3));
		}
		foreach (var offset in basePansy.SubEntryPoints.Concat(overlayPansy.SubEntryPoints).Distinct()) {
			bag.Add(((uint)offset, 4));
		}
		foreach (var offset in basePansy.OpcodeOffsets.Concat(overlayPansy.OpcodeOffsets).Distinct()) {
			bag.Add(((uint)offset, 5));
		}
		foreach (var offset in basePansy.DrawnOffsets.Concat(overlayPansy.DrawnOffsets).Distinct()) {
			bag.Add(((uint)offset, 6));
		}
		foreach (var offset in basePansy.ReadOffsets.Concat(overlayPansy.ReadOffsets).Distinct()) {
			bag.Add(((uint)offset, 7));
		}
		foreach (var offset in basePansy.IndirectOffsets.Concat(overlayPansy.IndirectOffsets).Distinct()) {
			bag.Add(((uint)offset, 8));
		}
	}

	private static void CollectCrossReferences(
		ConcurrentBag<CrossReference> bag,
		PansyLoader basePansy, PansyLoader overlayPansy) {
		var seen = new HashSet<(uint From, uint To, CrossRefType Type)>();

		foreach (var xref in basePansy.CrossReferences) {
			if (seen.Add((xref.From, xref.To, xref.Type))) {
				bag.Add(xref);
			}
		}

		foreach (var xref in overlayPansy.CrossReferences) {
			if (seen.Add((xref.From, xref.To, xref.Type))) {
				bag.Add(xref);
			}
		}
	}

	private static void CollectMemoryRegions(
		ConcurrentBag<MemoryRegion> bag,
		PansyLoader basePansy, PansyLoader overlayPansy) {
		var overlayByName = new Dictionary<string, MemoryRegion>();
		foreach (var region in overlayPansy.MemoryRegions) {
			overlayByName[region.Name] = region;
		}

		var addedNames = new HashSet<string>();

		foreach (var region in basePansy.MemoryRegions) {
			if (overlayByName.TryGetValue(region.Name, out var overlayRegion)) {
				bag.Add(overlayRegion);
				addedNames.Add(region.Name);
			} else {
				bag.Add(region);
				addedNames.Add(region.Name);
			}
		}

		foreach (var region in overlayPansy.MemoryRegions) {
			if (!addedNames.Contains(region.Name)) {
				bag.Add(region);
			}
		}
	}

	private static void CollectBookmarks(
		ConcurrentBag<Bookmark> bag,
		PansyLoader basePansy, PansyLoader overlayPansy) {
		var seen = new HashSet<(uint Address, string Name)>();

		foreach (var bookmark in basePansy.Bookmarks) {
			if (seen.Add((bookmark.Address, bookmark.Name))) {
				bag.Add(bookmark);
			}
		}

		foreach (var bookmark in overlayPansy.Bookmarks) {
			if (seen.Add((bookmark.Address, bookmark.Name))) {
				bag.Add(bookmark);
			}
		}
	}

	private static void CollectDataTypes(
		ConcurrentBag<DataTypeEntry> bag,
		PansyLoader basePansy, PansyLoader overlayPansy) {
		var seen = new HashSet<(uint Address, string Name)>();

		foreach (var dt in basePansy.DataTypes) {
			if (seen.Add((dt.Address, dt.Name))) {
				bag.Add(dt);
			}
		}

		foreach (var dt in overlayPansy.DataTypes) {
			if (seen.Add((dt.Address, dt.Name))) {
				bag.Add(dt);
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
