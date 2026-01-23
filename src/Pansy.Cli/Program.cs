// ============================================================================
// Program.cs - CLI Entry Point
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

using System.Text.RegularExpressions;
using Pansy.Core;
using Spectre.Console;

if (args.Length == 0) {
	AnsiConsole.MarkupLine("[bold magenta]🌼 Pansy v0.1.0[/]");
	AnsiConsole.MarkupLine("Universal Disassembly Metadata Format");
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy <command> [[options]]");
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[cyan]Commands:[/]");
	AnsiConsole.MarkupLine("  info <file>             Display information about a Pansy file");
	AnsiConsole.MarkupLine("  symbols <file>          List all symbols in a Pansy file");
	AnsiConsole.MarkupLine("  find <file> <pattern>   Search for symbols/comments (supports regex, wildcards)");
	AnsiConsole.MarkupLine("  xrefs <file> <address>  Show cross-references for an address");
	AnsiConsole.MarkupLine("  diff <file1> <file2>    Compare two Pansy files");
	AnsiConsole.WriteLine();
	return 0;
}

var command = args[0].ToLowerInvariant();

try {
	switch (command) {
		case "info":
			return RunInfo(args.Skip(1).ToArray());
		case "symbols":
			return RunSymbols(args.Skip(1).ToArray());
		case "find":
			return RunFind(args.Skip(1).ToArray());
		case "xrefs":
			return RunXrefs(args.Skip(1).ToArray());
		case "diff":
			return RunDiff(args.Skip(1).ToArray());
		default:
			AnsiConsole.MarkupLine($"[red]Unknown command:[/] {command}");
			return 1;
	}
}
catch (Exception ex) {
	AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
	return 1;
}

static int RunInfo(string[] args) {
	if (args.Length == 0) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing file argument");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy info <file> [-v|--verbose]");
		return 1;
	}

	var filePath = args[0];
	var verbose = args.Any(a => a == "-v" || a == "--verbose");

	if (!File.Exists(filePath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePath)}");
		return 1;
	}

	AnsiConsole.MarkupLine("[bold magenta]🌼 Pansy File Viewer[/]");
	AnsiConsole.WriteLine();

	var data = File.ReadAllBytes(filePath);
	var pansy = new PansyLoader(data);

	// Display header
	var headerTable = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Property")
		.AddColumn("Value");

	headerTable.AddRow("Format Version", $"{pansy.Version:x4}");
	headerTable.AddRow("Platform", GetPlatformName(pansy.Platform));
	headerTable.AddRow("ROM Size", $"{pansy.RomSize} bytes ({pansy.RomSize / 1024}K)");
	headerTable.AddRow("ROM CRC32", $"{pansy.RomCrc32:x8}");
	headerTable.AddRow("Flags", pansy.Flags.ToString());

	AnsiConsole.Write(headerTable);
	AnsiConsole.WriteLine();

	// Content statistics
	AnsiConsole.MarkupLine("[bold cyan]Content Statistics:[/]");
	AnsiConsole.MarkupLine($"  • Symbols: {pansy.Symbols.Count}");
	AnsiConsole.MarkupLine($"  • Comments: {pansy.Comments.Count}");
	AnsiConsole.MarkupLine($"  • Code Offsets: {pansy.CodeOffsets.Count}");
	AnsiConsole.MarkupLine($"  • Data Offsets: {pansy.DataOffsets.Count}");
	AnsiConsole.MarkupLine($"  • Jump Targets: {pansy.JumpTargets.Count}");
	AnsiConsole.MarkupLine($"  • Subroutines: {pansy.SubEntryPoints.Count}");
	AnsiConsole.MarkupLine($"  • Memory Regions: {pansy.MemoryRegions.Count}");
	AnsiConsole.MarkupLine($"  • Cross-refs: {pansy.CrossReferences.Count}");
	AnsiConsole.WriteLine();

	// Memory regions
	if (pansy.MemoryRegions.Count > 0) {
		AnsiConsole.MarkupLine("[bold cyan]Memory Regions:[/]");
		var regionTable = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Start")
			.AddColumn("End")
			.AddColumn("Bank")
			.AddColumn("Type")
			.AddColumn("Name");

		foreach (var region in pansy.MemoryRegions.Take(10)) {
			regionTable.AddRow(
				$"${region.Start:x4}",
				$"${region.End:x4}",
				$"{region.Bank}",
				$"{region.Type}",
				Markup.Escape(region.Name)
			);
		}

		if (pansy.MemoryRegions.Count > 10) {
			regionTable.AddRow("[grey]...[/]", $"[grey]+{pansy.MemoryRegions.Count - 10} more[/]", "", "", "");
		}

		AnsiConsole.Write(regionTable);
		AnsiConsole.WriteLine();
	}

	// Symbols (verbose)
	if (verbose && pansy.Symbols.Count > 0) {
		AnsiConsole.MarkupLine("[bold cyan]Symbols (first 20):[/]");
		var symbolTable = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Address")
			.AddColumn("Name");

		foreach (var (addr, name) in pansy.Symbols.Take(20)) {
			symbolTable.AddRow($"${addr:x4}", Markup.Escape(name));
		}

		if (pansy.Symbols.Count > 20) {
			symbolTable.AddRow("[grey]...[/]", $"[grey]+{pansy.Symbols.Count - 20} more[/]");
		}

		AnsiConsole.Write(symbolTable);
	}

	return 0;
}

static string GetPlatformName(byte platformId) {
	return platformId switch {
		PansyLoader.PLATFORM_NES => "NES",
		PansyLoader.PLATFORM_SNES => "SNES",
		PansyLoader.PLATFORM_GB => "Game Boy",
		PansyLoader.PLATFORM_GBA => "Game Boy Advance",
		PansyLoader.PLATFORM_GENESIS => "Sega Genesis",
		PansyLoader.PLATFORM_ATARI_2600 => "Atari 2600",
		PansyLoader.PLATFORM_CUSTOM => "Custom",
		_ => $"Unknown ({platformId:x2})"
	};
}

static int RunSymbols(string[] args) {
	if (args.Length == 0) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing file argument");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy symbols <file> [-a|--address-sort] [-n|--name-sort]");
		return 1;
	}

	var filePath = args[0];
	var sortByAddress = args.Any(a => a == "-a" || a == "--address-sort");
	var sortByName = args.Any(a => a == "-n" || a == "--name-sort");

	if (!File.Exists(filePath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePath)}");
		return 1;
	}

	var data = File.ReadAllBytes(filePath);
	var pansy = new PansyLoader(data);

	if (pansy.Symbols.Count == 0) {
		AnsiConsole.MarkupLine("[yellow]No symbols found in file[/]");
		return 0;
	}

	AnsiConsole.MarkupLine($"[bold magenta]🌼 Symbols in {Markup.Escape(Path.GetFileName(filePath))}[/]");
	AnsiConsole.MarkupLine($"[grey]Total: {pansy.Symbols.Count} symbols[/]");
	AnsiConsole.WriteLine();

	var table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Address")
		.AddColumn("Name");

	IEnumerable<KeyValuePair<int, string>> symbolList = pansy.Symbols;

	if (sortByName) {
		symbolList = symbolList.OrderBy(x => x.Value);
	} else {
		symbolList = symbolList.OrderBy(x => x.Key);
	}

	foreach (var (addr, name) in symbolList) {
		table.AddRow($"${addr:X4}", Markup.Escape(name));
	}

	AnsiConsole.Write(table);
	return 0;
}

static int RunFind(string[] args) {
	if (args.Length < 2) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing arguments");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy find <file> <pattern> [-c|--comments] [-s|--symbols] [-i|--case-insensitive] [-r|--regex] [-w|--wildcard]");
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Pattern modes:[/]");
		AnsiConsole.MarkupLine("  [dim]Plain text: Main_Loop (default)[/]");
		AnsiConsole.MarkupLine("  [dim]Regex (-r): ^NMI_.*Handler$[/]");
		AnsiConsole.MarkupLine("  [dim]Wildcard (-w): *Main* or NMI_???[/]");
		return 1;
	}

	var filePath = args[0];
	var pattern = args[1];
	var searchComments = args.Any(a => a == "-c" || a == "--comments");
	var searchSymbols = args.Any(a => a == "-s" || a == "--symbols");
	var caseInsensitive = args.Any(a => a == "-i" || a == "--case-insensitive");
	var useRegex = args.Any(a => a == "-r" || a == "--regex");
	var useWildcard = args.Any(a => a == "-w" || a == "--wildcard");

	// If neither specified, search both
	if (!searchComments && !searchSymbols) {
		searchComments = searchSymbols = true;
	}

	if (!File.Exists(filePath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePath)}");
		return 1;
	}

	var data = File.ReadAllBytes(filePath);
	var pansy = new PansyLoader(data);

	// Build the matching function
	Func<string, bool> matches = BuildMatcher(pattern, caseInsensitive, useRegex, useWildcard);

	var modeDesc = useRegex ? "regex" : useWildcard ? "wildcard" : "text";
	AnsiConsole.MarkupLine($"[bold magenta]🌼 Search results for '{Markup.Escape(pattern)}' ({modeDesc}, case-{(caseInsensitive ? "insensitive" : "sensitive")})[/]");
	AnsiConsole.WriteLine();

	var foundCount = 0;

	// Search symbols
	if (searchSymbols && pansy.Symbols.Count > 0) {
		var matchedSymbols = pansy.Symbols.Where(x => matches(x.Value)).ToList();
		if (matchedSymbols.Count > 0) {
			AnsiConsole.MarkupLine("[bold cyan]Symbols:[/]");
			var table = new Table()
				.Border(TableBorder.Rounded)
				.AddColumn("Address")
				.AddColumn("Name");

			foreach (var (addr, name) in matchedSymbols.OrderBy(x => x.Key)) {
				table.AddRow($"${addr:X4}", Markup.Escape(name));
				foundCount++;
			}

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}

	// Search comments
	if (searchComments && pansy.Comments.Count > 0) {
		var matchedComments = pansy.Comments.Where(x => matches(x.Value)).ToList();
		if (matchedComments.Count > 0) {
			AnsiConsole.MarkupLine("[bold cyan]Comments:[/]");
			var table = new Table()
				.Border(TableBorder.Rounded)
				.AddColumn("Address")
				.AddColumn("Comment");

			foreach (var (addr, comment) in matchedComments.OrderBy(x => x.Key)) {
				table.AddRow($"${addr:X4}", Markup.Escape(comment));
				foundCount++;
			}

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}

	if (foundCount == 0) {
		AnsiConsole.MarkupLine("[yellow]No matches found[/]");
	} else {
		AnsiConsole.MarkupLine($"[green]Found {foundCount} match(es)[/]");
	}

	return 0;
}

static Func<string, bool> BuildMatcher(string pattern, bool caseInsensitive, bool useRegex, bool useWildcard) {
	var comparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
	var options = caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None;

	if (useRegex) {
		try {
			var regex = new Regex(pattern, options);
			return text => regex.IsMatch(text);
		} catch (ArgumentException) {
			AnsiConsole.MarkupLine($"[yellow]Warning:[/] Invalid regex pattern, falling back to plain text");
			return text => text.Contains(pattern, comparison);
		}
	}

	if (useWildcard) {
		// Convert wildcard to regex: * → .* and ? → .
		var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
		try {
			var regex = new Regex(regexPattern, options);
			return text => regex.IsMatch(text);
		} catch (ArgumentException) {
			AnsiConsole.MarkupLine($"[yellow]Warning:[/] Invalid wildcard pattern, falling back to plain text");
			return text => text.Contains(pattern, comparison);
		}
	}

	// Plain text search
	return text => text.Contains(pattern, comparison);
}

static int RunXrefs(string[] args) {
	// Check for analysis commands first
	if (args.Length >= 2 && args[1].StartsWith("--")) {
		var filePath = args[0];
		var command = args[1];

		if (!File.Exists(filePath)) {
			AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePath)}");
			return 1;
		}

		var fileData1 = File.ReadAllBytes(filePath);
		var pansyData = new PansyLoader(fileData1);

		switch (command) {
			case "--stats":
				return ShowXrefStats(pansyData);
			case "--most-called":
				var count = args.Length > 2 && int.TryParse(args[2], out var n) ? n : 10;
				return ShowMostCalled(pansyData, count);
			case "--unreferenced":
			case "--dead-code":
				return ShowUnreferencedCode(pansyData);
			case "--type":
				if (args.Length < 3) {
					AnsiConsole.MarkupLine("[red]Error:[/] Missing type argument");
					return 1;
				}
				return ShowByType(pansyData, args[2]);
			default:
				AnsiConsole.MarkupLine($"[red]Error:[/] Unknown command: {command}");
				return 1;
		}
	}

	if (args.Length < 2) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing arguments");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy xrefs <file> <address|command>");
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[cyan]Address lookup:[/]");
		AnsiConsole.MarkupLine("  pansy xrefs <file> $8000          Show refs to/from $8000");
		AnsiConsole.MarkupLine("  pansy xrefs <file> 32768          Same in decimal");
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[cyan]Analysis commands:[/]");
		AnsiConsole.MarkupLine("  pansy xrefs <file> --stats        Cross-reference statistics");
		AnsiConsole.MarkupLine("  pansy xrefs <file> --most-called [n]  Top n most referenced addresses");
		AnsiConsole.MarkupLine("  pansy xrefs <file> --unreferenced Find unreferenced subroutines");
		AnsiConsole.MarkupLine("  pansy xrefs <file> --type <type>  Filter by type (Jsr, Jmp, Branch, Read, Write)");
		return 1;
	}

	var filePathLookup = args[0];
	var addressStr = args[1];

	if (!File.Exists(filePathLookup)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePathLookup)}");
		return 1;
	}

	// Parse address
	int targetAddress;
	if (addressStr.StartsWith("$")) {
		if (!int.TryParse(addressStr[1..], System.Globalization.NumberStyles.HexNumber, null, out targetAddress)) {
			AnsiConsole.MarkupLine($"[red]Error:[/] Invalid hex address: {Markup.Escape(addressStr)}");
			return 1;
		}
	} else {
		if (!int.TryParse(addressStr, out targetAddress)) {
			AnsiConsole.MarkupLine($"[red]Error:[/] Invalid address: {Markup.Escape(addressStr)}");
			return 1;
		}
	}

	var fileData = File.ReadAllBytes(filePathLookup);
	var pansy = new PansyLoader(fileData);

	AnsiConsole.MarkupLine($"[bold magenta]🌼 Cross-references for ${targetAddress:X4}[/]");

	// Show symbol name if exists
	if (pansy.Symbols.TryGetValue(targetAddress, out var symbolName)) {
		AnsiConsole.MarkupLine($"[bold cyan]Symbol:[/] {Markup.Escape(symbolName)}");
	}

	AnsiConsole.WriteLine();

	// Find references TO this address
	var referencesTo = pansy.CrossReferences
		.Where(x => x.To == targetAddress)
		.OrderBy(x => x.From)
		.ToList();

	// Find references FROM this address
	var referencesFrom = pansy.CrossReferences
		.Where(x => x.From == targetAddress)
		.OrderBy(x => x.To)
		.ToList();

	if (referencesTo.Count > 0) {
		AnsiConsole.MarkupLine("[bold cyan]References TO this address:[/]");
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("From")
			.AddColumn("Type")
			.AddColumn("Symbol");

		foreach (var xref in referencesTo) {
			var fromSymbol = pansy.Symbols.TryGetValue((int)xref.From, out var name) ? name : "";
			table.AddRow($"${xref.From:X4}", xref.Type.ToString(), Markup.Escape(fromSymbol));
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	if (referencesFrom.Count > 0) {
		AnsiConsole.MarkupLine("[bold cyan]References FROM this address:[/]");
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("To")
			.AddColumn("Type")
			.AddColumn("Symbol");

		foreach (var xref in referencesFrom) {
			var toSymbol = pansy.Symbols.TryGetValue((int)xref.To, out var name) ? name : "";
			table.AddRow($"${xref.To:X4}", xref.Type.ToString(), Markup.Escape(toSymbol));
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	if (referencesTo.Count == 0 && referencesFrom.Count == 0) {
		AnsiConsole.MarkupLine("[yellow]No cross-references found[/]");
	} else {
		AnsiConsole.MarkupLine($"[green]Total: {referencesTo.Count} incoming, {referencesFrom.Count} outgoing[/]");
	}

	return 0;
}

static int ShowXrefStats(PansyLoader pansy) {
	AnsiConsole.MarkupLine("[bold magenta]🌼 Cross-Reference Statistics[/]");
	AnsiConsole.WriteLine();

	var totalXrefs = pansy.CrossReferences.Count;

	// Group by type
	var byType = pansy.CrossReferences
		.GroupBy(x => x.Type)
		.OrderByDescending(g => g.Count())
		.ToList();

	var table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Reference Type")
		.AddColumn(new TableColumn("Count").Centered())
		.AddColumn(new TableColumn("Percentage").Centered());

	foreach (var group in byType) {
		var percentage = totalXrefs > 0 ? (double)group.Count() / totalXrefs * 100 : 0;
		table.AddRow(group.Key.ToString(), group.Count().ToString("N0"), $"{percentage:F1}%");
	}

	table.AddRow("[bold]Total[/]", $"[bold]{totalXrefs:N0}[/]", "[bold]100%[/]");

	AnsiConsole.Write(table);
	AnsiConsole.WriteLine();

	// Additional stats
	var uniqueTargets = pansy.CrossReferences.Select(x => x.To).Distinct().Count();
	var uniqueSources = pansy.CrossReferences.Select(x => x.From).Distinct().Count();

	AnsiConsole.MarkupLine($"[cyan]Unique target addresses:[/] {uniqueTargets:N0}");
	AnsiConsole.MarkupLine($"[cyan]Unique source addresses:[/] {uniqueSources:N0}");

	return 0;
}

static int ShowMostCalled(PansyLoader pansy, int count) {
	AnsiConsole.MarkupLine($"[bold magenta]🌼 Top {count} Most Referenced Addresses[/]");
	AnsiConsole.WriteLine();

	var mostReferenced = pansy.CrossReferences
		.GroupBy(x => x.To)
		.OrderByDescending(g => g.Count())
		.Take(count)
		.ToList();

	if (mostReferenced.Count == 0) {
		AnsiConsole.MarkupLine("[yellow]No cross-references found[/]");
		return 0;
	}

	var table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Rank")
		.AddColumn("Address")
		.AddColumn("Symbol")
		.AddColumn(new TableColumn("Ref Count").Centered())
		.AddColumn("Types");

	var rank = 1;
	foreach (var group in mostReferenced) {
		var address = (int)group.First().To;
		var symbol = pansy.Symbols.TryGetValue(address, out var name) ? name : "-";
		var types = string.Join(", ", group.Select(x => x.Type).Distinct());

		table.AddRow(rank.ToString(), $"${address:X4}", Markup.Escape(symbol), group.Count().ToString(), types);
		rank++;
	}

	AnsiConsole.Write(table);

	return 0;
}

static int ShowUnreferencedCode(PansyLoader pansy) {
	AnsiConsole.MarkupLine("[bold magenta]🌼 Unreferenced Subroutines (Dead Code Detection)[/]");
	AnsiConsole.WriteLine();

	// Get all subroutine entry points
	var subroutines = pansy.SubEntryPoints.ToHashSet();

	// Get all addresses that are targets of cross-references
	var referencedAddresses = pansy.CrossReferences
		.Select(x => (int)x.To)
		.ToHashSet();

	// Find subroutines that are never referenced
	var unreferenced = subroutines.Where(addr => !referencedAddresses.Contains(addr)).OrderBy(x => x).ToList();

	if (unreferenced.Count == 0) {
		AnsiConsole.MarkupLine("[green]All subroutines are referenced![/]");
		return 0;
	}

	AnsiConsole.MarkupLine($"[yellow]Found {unreferenced.Count} unreferenced subroutine(s):[/]");
	AnsiConsole.WriteLine();

	var table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Address")
		.AddColumn("Symbol");

	foreach (var address in unreferenced) {
		var symbol = pansy.Symbols.TryGetValue(address, out var name) ? name : "-";
		table.AddRow($"${address:X4}", Markup.Escape(symbol));
	}

	AnsiConsole.Write(table);

	return 0;
}

static int ShowByType(PansyLoader pansy, string typeName) {
	// Parse the type
	if (!Enum.TryParse<CrossRefType>(typeName, true, out var xrefType)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] Invalid cross-reference type: {Markup.Escape(typeName)}");
		AnsiConsole.MarkupLine("[cyan]Valid types:[/] Jsr, Jmp, Branch, Read, Write, DataRef, IndexedRead, IndexedWrite");
		return 1;
	}

	var filtered = pansy.CrossReferences
		.Where(x => x.Type == xrefType)
		.OrderBy(x => x.From)
		.ToList();

	AnsiConsole.MarkupLine($"[bold magenta]🌼 Cross-references of type '{xrefType}'[/]");
	AnsiConsole.WriteLine();

	if (filtered.Count == 0) {
		AnsiConsole.MarkupLine($"[yellow]No {xrefType} references found[/]");
		return 0;
	}

	var table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("From")
		.AddColumn("To")
		.AddColumn("From Symbol")
		.AddColumn("To Symbol");

	foreach (var xref in filtered) {
		var fromSymbol = pansy.Symbols.TryGetValue((int)xref.From, out var fromName) ? fromName : "-";
		var toSymbol = pansy.Symbols.TryGetValue((int)xref.To, out var toName) ? toName : "-";
		table.AddRow($"${xref.From:X4}", $"${xref.To:X4}", Markup.Escape(fromSymbol), Markup.Escape(toSymbol));
	}

	AnsiConsole.Write(table);
	AnsiConsole.MarkupLine($"[green]Total: {filtered.Count} reference(s)[/]");

	return 0;
}

static int RunDiff(string[] args) {
	if (args.Length < 2) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing arguments");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy diff <file1> <file2>");
		return 1;
	}

	var file1 = args[0];
	var file2 = args[1];

	if (!File.Exists(file1)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(file1)}");
		return 1;
	}

	if (!File.Exists(file2)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(file2)}");
		return 1;
	}

	var data1 = File.ReadAllBytes(file1);
	var data2 = File.ReadAllBytes(file2);

	var pansy1 = new PansyLoader(data1);
	var pansy2 = new PansyLoader(data2);

	AnsiConsole.MarkupLine($"[bold magenta]🌼 Comparing Pansy files[/]");
	AnsiConsole.MarkupLine($"[cyan]File 1:[/] {Markup.Escape(file1)}");
	AnsiConsole.MarkupLine($"[cyan]File 2:[/] {Markup.Escape(file2)}");
	AnsiConsole.WriteLine();

	// Compare header info
	var headerTable = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Property")
		.AddColumn("File 1")
		.AddColumn("File 2")
		.AddColumn("Match");

	var platformMatch = pansy1.Platform == pansy2.Platform;
	var sizeMatch = pansy1.RomSize == pansy2.RomSize;
	var crcMatch = pansy1.RomCrc32 == pansy2.RomCrc32;

	headerTable.AddRow(
		"Platform",
		GetPlatformName(pansy1.Platform),
		GetPlatformName(pansy2.Platform),
		platformMatch ? "[green]✓[/]" : "[red]✗[/]"
	);

	headerTable.AddRow(
		"ROM Size",
		$"{pansy1.RomSize} bytes",
		$"{pansy2.RomSize} bytes",
		sizeMatch ? "[green]✓[/]" : "[red]✗[/]"
	);

	headerTable.AddRow(
		"ROM CRC32",
		$"{pansy1.RomCrc32:x8}",
		$"{pansy2.RomCrc32:x8}",
		crcMatch ? "[green]✓[/]" : "[red]✗[/]"
	);

	AnsiConsole.Write(headerTable);
	AnsiConsole.WriteLine();

	// Compare content counts
	var statsTable = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Content Type")
		.AddColumn("File 1")
		.AddColumn("File 2")
		.AddColumn("Δ");

	void AddStatRow(string name, int count1, int count2) {
		var delta = count2 - count1;
		var deltaStr = delta == 0 ? "[green]0[/]" :
					   delta > 0 ? $"[cyan]+{delta}[/]" :
					   $"[yellow]{delta}[/]";
		statsTable.AddRow(name, count1.ToString(), count2.ToString(), deltaStr);
	}

	AddStatRow("Symbols", pansy1.Symbols.Count, pansy2.Symbols.Count);
	AddStatRow("Comments", pansy1.Comments.Count, pansy2.Comments.Count);
	AddStatRow("Code Offsets", pansy1.CodeOffsets.Count, pansy2.CodeOffsets.Count);
	AddStatRow("Data Offsets", pansy1.DataOffsets.Count, pansy2.DataOffsets.Count);
	AddStatRow("Jump Targets", pansy1.JumpTargets.Count, pansy2.JumpTargets.Count);
	AddStatRow("Subroutines", pansy1.SubEntryPoints.Count, pansy2.SubEntryPoints.Count);
	AddStatRow("Memory Regions", pansy1.MemoryRegions.Count, pansy2.MemoryRegions.Count);
	AddStatRow("Cross-refs", pansy1.CrossReferences.Count, pansy2.CrossReferences.Count);

	AnsiConsole.Write(statsTable);
	AnsiConsole.WriteLine();

	// Show detailed symbol differences
	var addedSymbols = pansy2.Symbols.Keys.Except(pansy1.Symbols.Keys).ToList();
	var removedSymbols = pansy1.Symbols.Keys.Except(pansy2.Symbols.Keys).ToList();
	var commonKeys = pansy1.Symbols.Keys.Intersect(pansy2.Symbols.Keys);
	var changedSymbols = commonKeys.Where(k => pansy1.Symbols[k] != pansy2.Symbols[k]).ToList();

	if (addedSymbols.Count > 0) {
		AnsiConsole.MarkupLine($"[bold green]Added Symbols ({addedSymbols.Count}):[/]");
		foreach (var addr in addedSymbols.OrderBy(x => x).Take(10)) {
			AnsiConsole.MarkupLine($"  [cyan]${addr:X4}:[/] {Markup.Escape(pansy2.Symbols[addr])}");
		}
		if (addedSymbols.Count > 10) {
			AnsiConsole.MarkupLine($"  [grey]... and {addedSymbols.Count - 10} more[/]");
		}
		AnsiConsole.WriteLine();
	}

	if (removedSymbols.Count > 0) {
		AnsiConsole.MarkupLine($"[bold yellow]Removed Symbols ({removedSymbols.Count}):[/]");
		foreach (var addr in removedSymbols.OrderBy(x => x).Take(10)) {
			AnsiConsole.MarkupLine($"  [cyan]${addr:X4}:[/] {Markup.Escape(pansy1.Symbols[addr])}");
		}
		if (removedSymbols.Count > 10) {
			AnsiConsole.MarkupLine($"  [grey]... and {removedSymbols.Count - 10} more[/]");
		}
		AnsiConsole.WriteLine();
	}

	if (changedSymbols.Count > 0) {
		AnsiConsole.MarkupLine($"[bold magenta]Changed Symbols ({changedSymbols.Count}):[/]");
		foreach (var addr in changedSymbols.OrderBy(x => x).Take(10)) {
			AnsiConsole.MarkupLine($"  [cyan]${addr:X4}:[/] {Markup.Escape(pansy1.Symbols[addr])} → {Markup.Escape(pansy2.Symbols[addr])}");
		}
		if (changedSymbols.Count > 10) {
			AnsiConsole.MarkupLine($"  [grey]... and {changedSymbols.Count - 10} more[/]");
		}
		AnsiConsole.WriteLine();
	}

	return 0;
}
