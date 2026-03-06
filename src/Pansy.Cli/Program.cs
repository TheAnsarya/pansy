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
	AnsiConsole.MarkupLine("  stats <file>            Show detailed statistics and analysis");
	AnsiConsole.MarkupLine("  find <file> <pattern>   Search for symbols/comments (supports regex, wildcards)");
	AnsiConsole.MarkupLine("  xrefs <file> <address>  Show cross-references for an address");
	AnsiConsole.MarkupLine("  diff <file1> <file2>    Compare two Pansy files");
	AnsiConsole.MarkupLine("  merge <base> <overlay>  Merge two Pansy files");
	AnsiConsole.MarkupLine("  validate <file>         Validate Pansy file structure");
	AnsiConsole.MarkupLine("  graph <file>            Export cross-reference graph");
	AnsiConsole.MarkupLine("  analyze <pansy> <rom>   Analyze ROM coverage and detect gaps");
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
		case "stats":
			return RunStats(args.Skip(1).ToArray());
		case "find":
			return RunFind(args.Skip(1).ToArray());
		case "xrefs":
			return RunXrefs(args.Skip(1).ToArray());
		case "diff":
			return RunDiff(args.Skip(1).ToArray());
		case "merge":
			return RunMerge(args.Skip(1).ToArray());
		case "validate":
			return RunValidate(args.Skip(1).ToArray());
		case "graph":
			return RunGraph(args.Skip(1).ToArray());
		case "analyze":
			return RunAnalyze(args.Skip(1).ToArray());
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
	headerTable.AddRow("Platform", PansyLoader.GetPlatformName(pansy.Platform));
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
		table.AddRow($"${addr:x4}", Markup.Escape(name));
	}

	AnsiConsole.Write(table);
	return 0;
}

static int RunStats(string[] args) {
	if (args.Length == 0) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing file argument");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy stats <file>");
		return 1;
	}

	var filePath = args[0];

	if (!File.Exists(filePath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePath)}");
		return 1;
	}

	var fileBytes = File.ReadAllBytes(filePath);
	var pansy = new PansyLoader(fileBytes);

	AnsiConsole.MarkupLine($"[bold magenta]\ud83c\udf3c Statistics for {Markup.Escape(Path.GetFileName(filePath))}[/]");
	AnsiConsole.WriteLine();

	// File info
	var infoTable = new Table()
		.Border(TableBorder.Rounded)
		.Title("[bold cyan]File Info[/]")
		.AddColumn("Property")
		.AddColumn("Value");

	infoTable.AddRow("File Size", $"{fileBytes.Length:N0} bytes ({fileBytes.Length / 1024.0:F1} KB)");
	infoTable.AddRow("Platform", PansyLoader.GetPlatformName(pansy.Platform));
	infoTable.AddRow("ROM Size", $"{pansy.RomSize:N0} bytes ({pansy.RomSize / 1024}K)");
	infoTable.AddRow("ROM CRC32", $"{pansy.RomCrc32:x8}");
	infoTable.AddRow("Compressed", pansy.IsCompressed ? "[green]Yes[/]" : "No");

	AnsiConsole.Write(infoTable);
	AnsiConsole.WriteLine();

	// Content summary
	var summaryTable = new Table()
		.Border(TableBorder.Rounded)
		.Title("[bold cyan]Content Summary[/]")
		.AddColumn("Section")
		.AddColumn(new TableColumn("Count").RightAligned());

	summaryTable.AddRow("Symbols", $"{pansy.Symbols.Count:N0}");
	summaryTable.AddRow("Comments", $"{pansy.Comments.Count:N0}");
	summaryTable.AddRow("Memory Regions", $"{pansy.MemoryRegions.Count:N0}");
	summaryTable.AddRow("Cross-References", $"{pansy.CrossReferences.Count:N0}");

	AnsiConsole.Write(summaryTable);
	AnsiConsole.WriteLine();

	// Code/Data map statistics
	if (pansy.HasCodeDataMap) {
		var codeCount = pansy.CodeOffsets.Count;
		var dataCount = pansy.DataOffsets.Count;
		var opcodeCount = pansy.OpcodeOffsets.Count;
		var jumpCount = pansy.JumpTargets.Count;
		var subCount = pansy.SubEntryPoints.Count;
		var drawnCount = pansy.DrawnOffsets.Count;
		var readCount = pansy.ReadOffsets.Count;
		var indirectCount = pansy.IndirectOffsets.Count;
		var total = codeCount + dataCount;

		var mapTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Code/Data Map[/]")
			.AddColumn("Flag")
			.AddColumn(new TableColumn("Count").RightAligned())
			.AddColumn(new TableColumn("Percentage").RightAligned());

		mapTable.AddRow("Code offsets", $"{codeCount:N0}", total > 0 ? $"{codeCount * 100.0 / total:F1}%" : "-");
		mapTable.AddRow("Data offsets", $"{dataCount:N0}", total > 0 ? $"{dataCount * 100.0 / total:F1}%" : "-");
		mapTable.AddRow("[grey]Opcodes[/]", $"[grey]{opcodeCount:N0}[/]", "");
		mapTable.AddRow("[grey]Jump targets[/]", $"[grey]{jumpCount:N0}[/]", "");
		mapTable.AddRow("[grey]Subroutine entries[/]", $"[grey]{subCount:N0}[/]", "");
		mapTable.AddRow("[grey]Drawn[/]", $"[grey]{drawnCount:N0}[/]", "");
		mapTable.AddRow("[grey]Read[/]", $"[grey]{readCount:N0}[/]", "");
		mapTable.AddRow("[grey]Indirect[/]", $"[grey]{indirectCount:N0}[/]", "");

		AnsiConsole.Write(mapTable);
		AnsiConsole.WriteLine();

		// Code/Data ratio bar chart
		if (total > 0) {
			var chart = new BarChart()
				.Label("[bold cyan]Code vs Data[/]")
				.CenterLabel();

			chart.AddItem("Code", codeCount, Color.Green);
			chart.AddItem("Data", dataCount, Color.Blue);

			AnsiConsole.Write(chart);
			AnsiConsole.WriteLine();
		}
	}

	// Symbol type breakdown
	if (pansy.SymbolEntries.Count > 0) {
		var typeGroups = pansy.SymbolEntries.Values
			.GroupBy(e => e.Type)
			.OrderByDescending(g => g.Count())
			.ToList();

		var symTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Symbol Type Breakdown[/]")
			.AddColumn("Type")
			.AddColumn(new TableColumn("Count").RightAligned())
			.AddColumn(new TableColumn("Percentage").RightAligned());

		var totalSymbols = pansy.SymbolEntries.Count;
		foreach (var group in typeGroups) {
			var count = group.Count();
			symTable.AddRow(
				group.Key.ToString(),
				$"{count:N0}",
				$"{count * 100.0 / totalSymbols:F1}%"
			);
		}

		AnsiConsole.Write(symTable);
		AnsiConsole.WriteLine();
	}

	// Comment type breakdown
	if (pansy.CommentEntries.Count > 0) {
		var cmtGroups = pansy.CommentEntries.Values
			.GroupBy(e => e.Type)
			.OrderByDescending(g => g.Count())
			.ToList();

		var cmtTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Comment Type Breakdown[/]")
			.AddColumn("Type")
			.AddColumn(new TableColumn("Count").RightAligned())
			.AddColumn(new TableColumn("Percentage").RightAligned());

		var totalComments = pansy.CommentEntries.Count;
		foreach (var group in cmtGroups) {
			var count = group.Count();
			cmtTable.AddRow(
				group.Key.ToString(),
				$"{count:N0}",
				$"{count * 100.0 / totalComments:F1}%"
			);
		}

		AnsiConsole.Write(cmtTable);
		AnsiConsole.WriteLine();
	}

	// Cross-reference type breakdown
	if (pansy.CrossReferences.Count > 0) {
		var xrefGroups = pansy.CrossReferences
			.GroupBy(x => x.Type)
			.OrderByDescending(g => g.Count())
			.ToList();

		var xrefTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Cross-Reference Type Breakdown[/]")
			.AddColumn("Type")
			.AddColumn(new TableColumn("Count").RightAligned())
			.AddColumn(new TableColumn("Percentage").RightAligned());

		var totalXrefs = pansy.CrossReferences.Count;
		foreach (var group in xrefGroups) {
			var count = group.Count();
			xrefTable.AddRow(
				group.Key.ToString(),
				$"{count:N0}",
				$"{count * 100.0 / totalXrefs:F1}%"
			);
		}

		AnsiConsole.Write(xrefTable);
		AnsiConsole.WriteLine();

		// Most-referenced addresses
		var topTargets = pansy.CrossReferences
			.GroupBy(x => x.To)
			.OrderByDescending(g => g.Count())
			.Take(10)
			.ToList();

		if (topTargets.Count > 0) {
			var topTable = new Table()
				.Border(TableBorder.Rounded)
				.Title("[bold cyan]Top Referenced Addresses[/]")
				.AddColumn("Address")
				.AddColumn("Symbol")
				.AddColumn(new TableColumn("References").RightAligned());

			foreach (var group in topTargets) {
				var addr = (int)group.Key;
				var symbol = pansy.GetSymbol(addr) ?? "[grey]-[/]";
				topTable.AddRow($"${addr:x6}", Markup.Escape(symbol), $"{group.Count()}");
			}

			AnsiConsole.Write(topTable);
			AnsiConsole.WriteLine();
		}
	}

	// Metadata
	if (!string.IsNullOrEmpty(pansy.ProjectName) || !string.IsNullOrEmpty(pansy.Author)) {
		var metaTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Project Metadata[/]")
			.AddColumn("Field")
			.AddColumn("Value");

		if (!string.IsNullOrEmpty(pansy.ProjectName))
			metaTable.AddRow("Project", Markup.Escape(pansy.ProjectName));
		if (!string.IsNullOrEmpty(pansy.Author))
			metaTable.AddRow("Author", Markup.Escape(pansy.Author));
		if (!string.IsNullOrEmpty(pansy.ProjectVersion))
			metaTable.AddRow("Version", Markup.Escape(pansy.ProjectVersion));

		AnsiConsole.Write(metaTable);
	}

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
				table.AddRow($"${addr:x4}", Markup.Escape(name));
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
				table.AddRow($"${addr:x4}", Markup.Escape(comment));
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

	AnsiConsole.MarkupLine($"[bold magenta]🌼 Cross-references for ${targetAddress:x4}[/]");

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
			table.AddRow($"${xref.From:x4}", xref.Type.ToString(), Markup.Escape(fromSymbol));
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
			table.AddRow($"${xref.To:x4}", xref.Type.ToString(), Markup.Escape(toSymbol));
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

		table.AddRow(rank.ToString(), $"${address:x4}", Markup.Escape(symbol), group.Count().ToString(), types);
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
		table.AddRow($"${address:x4}", Markup.Escape(symbol));
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
		table.AddRow($"${xref.From:x4}", $"${xref.To:x4}", Markup.Escape(fromSymbol), Markup.Escape(toSymbol));
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
		PansyLoader.GetPlatformName(pansy1.Platform),
		PansyLoader.GetPlatformName(pansy2.Platform),
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
			AnsiConsole.MarkupLine($"  [cyan]${addr:x4}:[/] {Markup.Escape(pansy2.Symbols[addr])}");
		}
		if (addedSymbols.Count > 10) {
			AnsiConsole.MarkupLine($"  [grey]... and {addedSymbols.Count - 10} more[/]");
		}
		AnsiConsole.WriteLine();
	}

	if (removedSymbols.Count > 0) {
		AnsiConsole.MarkupLine($"[bold yellow]Removed Symbols ({removedSymbols.Count}):[/]");
		foreach (var addr in removedSymbols.OrderBy(x => x).Take(10)) {
			AnsiConsole.MarkupLine($"  [cyan]${addr:x4}:[/] {Markup.Escape(pansy1.Symbols[addr])}");
		}
		if (removedSymbols.Count > 10) {
			AnsiConsole.MarkupLine($"  [grey]... and {removedSymbols.Count - 10} more[/]");
		}
		AnsiConsole.WriteLine();
	}

	if (changedSymbols.Count > 0) {
		AnsiConsole.MarkupLine($"[bold magenta]Changed Symbols ({changedSymbols.Count}):[/]");
		foreach (var addr in changedSymbols.OrderBy(x => x).Take(10)) {
			AnsiConsole.MarkupLine($"  [cyan]${addr:x4}:[/] {Markup.Escape(pansy1.Symbols[addr])} → {Markup.Escape(pansy2.Symbols[addr])}");
		}
		if (changedSymbols.Count > 10) {
			AnsiConsole.MarkupLine($"  [grey]... and {changedSymbols.Count - 10} more[/]");
		}
		AnsiConsole.WriteLine();
	}

	return 0;
}

static int RunMerge(string[] args) {
	if (args.Length < 2) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing arguments");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy merge <base.pansy> <overlay.pansy> [-o|--output <file>]");
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Merge strategies:[/]");
		AnsiConsole.MarkupLine("  [dim]Symbols/Comments: union with dedup[/]");
		AnsiConsole.MarkupLine("  [dim]Code/Data map: flag union[/]");
		AnsiConsole.MarkupLine("  [dim]Cross-refs: dedup by (from, to, type)[/]");
		AnsiConsole.MarkupLine("  [dim]Memory regions: overlay wins by name[/]");
		AnsiConsole.MarkupLine("  [dim]Metadata: overlay wins with fallback[/]");
		return 1;
	}

	var basePath = args[0];
	var overlayPath = args[1];
	var outputPath = "merged.pansy";

	for (int i = 2; i < args.Length; i++) {
		if ((args[i] == "-o" || args[i] == "--output") && i + 1 < args.Length) {
			outputPath = args[++i];
		}
	}

	if (!File.Exists(basePath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(basePath)}");
		return 1;
	}

	if (!File.Exists(overlayPath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(overlayPath)}");
		return 1;
	}

	var baseLoader = new PansyLoader(File.ReadAllBytes(basePath));
	var overlayLoader = new PansyLoader(File.ReadAllBytes(overlayPath));

	AnsiConsole.MarkupLine("[bold magenta]🌼 Merging Pansy files[/]");
	AnsiConsole.MarkupLine($"[cyan]Base:[/]    {Markup.Escape(basePath)}");
	AnsiConsole.MarkupLine($"[cyan]Overlay:[/] {Markup.Escape(overlayPath)}");
	AnsiConsole.MarkupLine($"[cyan]Output:[/]  {Markup.Escape(outputPath)}");
	AnsiConsole.WriteLine();

	var merged = PansyMerger.Merge(baseLoader, overlayLoader);
	var output = merged.Generate();
	File.WriteAllBytes(outputPath, output);

	// Verify the output
	var result = new PansyLoader(output);

	var table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Content")
		.AddColumn("Base")
		.AddColumn("Overlay")
		.AddColumn("Merged");

	table.AddRow("Symbols", $"{baseLoader.Symbols.Count}", $"{overlayLoader.Symbols.Count}", $"{result.Symbols.Count}");
	table.AddRow("Comments", $"{baseLoader.Comments.Count}", $"{overlayLoader.Comments.Count}", $"{result.Comments.Count}");
	table.AddRow("Memory Regions", $"{baseLoader.MemoryRegions.Count}", $"{overlayLoader.MemoryRegions.Count}", $"{result.MemoryRegions.Count}");
	table.AddRow("Cross-refs", $"{baseLoader.CrossReferences.Count}", $"{overlayLoader.CrossReferences.Count}", $"{result.CrossReferences.Count}");

	AnsiConsole.Write(table);
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine($"[green]Merged file written:[/] {Markup.Escape(outputPath)} ({output.Length:N0} bytes)");

	return 0;
}

static int RunValidate(string[] args) {
	if (args.Length == 0) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing file argument");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy validate <file>");
		return 1;
	}

	var filePath = args[0];

	if (!File.Exists(filePath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePath)}");
		return 1;
	}

	AnsiConsole.MarkupLine($"[bold magenta]🌼 Validating {Markup.Escape(Path.GetFileName(filePath))}[/]");
	AnsiConsole.WriteLine();

	var errors = new List<string>();
	var warnings = new List<string>();

	var fileBytes = File.ReadAllBytes(filePath);

	// Check minimum size
	if (fileBytes.Length < 32) {
		AnsiConsole.MarkupLine("[red]✗ File too small to contain a valid Pansy header (minimum 32 bytes)[/]");
		return 1;
	}

	// Check magic
	var magic = System.Text.Encoding.ASCII.GetString(fileBytes, 0, 5);
	if (magic != "PANSY") {
		errors.Add($"Invalid magic bytes: expected 'PANSY', got '{magic}'");
	}

	// Try to load
	PansyLoader? pansy = null;
	try {
		pansy = new PansyLoader(fileBytes);
		AnsiConsole.MarkupLine("[green]✓ File parsed successfully[/]");
	} catch (Exception ex) {
		errors.Add($"Parse error: {ex.Message}");
		AnsiConsole.MarkupLine($"[red]✗ Parse error:[/] {Markup.Escape(ex.Message)}");
		return 1;
	}

	// Header validation
	if (pansy.Version == 0) {
		warnings.Add("Version is 0 (expected ≥ 0x0100)");
	}
	if (pansy.Platform == 0) {
		warnings.Add("Platform is 0 (unknown/unset)");
	}
	if (pansy.RomSize == 0) {
		warnings.Add("ROM size is 0");
	}
	if (pansy.RomCrc32 == 0) {
		warnings.Add("ROM CRC32 is 0");
	}

	// Symbol validation
	foreach (var (addr, entries) in pansy.AllSymbolEntries) {
		foreach (var entry in entries) {
			if (string.IsNullOrWhiteSpace(entry.Name)) {
				errors.Add($"Empty symbol name at address ${addr:x}");
			}
			if ((int)entry.Type < 1 || (int)entry.Type > 9) {
				errors.Add($"Invalid symbol type {(int)entry.Type} at ${addr:x}");
			}
		}
	}

	// Comment validation
	foreach (var (addr, entries) in pansy.AllCommentEntries) {
		foreach (var entry in entries) {
			if (string.IsNullOrWhiteSpace(entry.Text)) {
				warnings.Add($"Empty comment at address ${addr:x}");
			}
			if ((int)entry.Type < 1 || (int)entry.Type > 3) {
				errors.Add($"Invalid comment type {(int)entry.Type} at ${addr:x}");
			}
		}
	}

	// Cross-reference validation
	foreach (var xref in pansy.CrossReferences) {
		if ((int)xref.Type < 1 || (int)xref.Type > 5) {
			errors.Add($"Invalid cross-reference type {(int)xref.Type}: ${xref.From:x} → ${xref.To:x}");
		}
	}

	// Memory region validation
	foreach (var region in pansy.MemoryRegions) {
		if (region.End < region.Start) {
			errors.Add($"Memory region '{region.Name}' has end (${region.End:x}) < start (${region.Start:x})");
		}
		if (string.IsNullOrWhiteSpace(region.Name)) {
			warnings.Add($"Unnamed memory region at ${region.Start:x}-${region.End:x}");
		}
	}

	// Report
	AnsiConsole.WriteLine();

	if (errors.Count > 0) {
		AnsiConsole.MarkupLine($"[bold red]Errors ({errors.Count}):[/]");
		foreach (var err in errors) {
			AnsiConsole.MarkupLine($"  [red]✗[/] {Markup.Escape(err)}");
		}
		AnsiConsole.WriteLine();
	}

	if (warnings.Count > 0) {
		AnsiConsole.MarkupLine($"[bold yellow]Warnings ({warnings.Count}):[/]");
		foreach (var warn in warnings) {
			AnsiConsole.MarkupLine($"  [yellow]⚠[/] {Markup.Escape(warn)}");
		}
		AnsiConsole.WriteLine();
	}

	// Summary
	var table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("Check")
		.AddColumn("Result");

	table.AddRow("Magic/Header", errors.Count == 0 ? "[green]✓ Valid[/]" : "[red]✗ Invalid[/]");
	table.AddRow("Symbols", $"{pansy.Symbols.Count} entries");
	table.AddRow("Comments", $"{pansy.Comments.Count} entries");
	table.AddRow("Cross-refs", $"{pansy.CrossReferences.Count} entries");
	table.AddRow("Memory Regions", $"{pansy.MemoryRegions.Count} entries");
	table.AddRow("Errors", errors.Count == 0 ? "[green]0[/]" : $"[red]{errors.Count}[/]");
	table.AddRow("Warnings", warnings.Count == 0 ? "[green]0[/]" : $"[yellow]{warnings.Count}[/]");

	AnsiConsole.Write(table);

	if (errors.Count == 0 && warnings.Count == 0) {
		AnsiConsole.MarkupLine("[bold green]✓ File is valid![/]");
	} else if (errors.Count == 0) {
		AnsiConsole.MarkupLine("[bold yellow]⚠ File is valid with warnings[/]");
	} else {
		AnsiConsole.MarkupLine("[bold red]✗ File has errors[/]");
	}

	return errors.Count > 0 ? 1 : 0;
}

static int RunGraph(string[] args) {
	if (args.Length == 0) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing file argument");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy graph <file> [-f|--format <dot|graphml|json>] [-t|--type <type>] [-o|--output <file>]");
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Formats: dot (default), graphml, json[/]");
		AnsiConsole.MarkupLine("[dim]Types: Jsr, Jmp, Branch, Read, Write (optional filter)[/]");
		return 1;
	}

	var filePath = args[0];
	var format = "dot";
	CrossRefType? filter = null;
	string? outputPath = null;

	for (int i = 1; i < args.Length; i++) {
		if ((args[i] == "-f" || args[i] == "--format") && i + 1 < args.Length) {
			format = args[++i].ToLowerInvariant();
		} else if ((args[i] == "-t" || args[i] == "--type") && i + 1 < args.Length) {
			if (Enum.TryParse<CrossRefType>(args[++i], true, out var type)) {
				filter = type;
			} else {
				AnsiConsole.MarkupLine($"[red]Error:[/] Invalid type. Valid: Jsr, Jmp, Branch, Read, Write");
				return 1;
			}
		} else if ((args[i] == "-o" || args[i] == "--output") && i + 1 < args.Length) {
			outputPath = args[++i];
		}
	}

	if (!File.Exists(filePath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePath)}");
		return 1;
	}

	var loader = new PansyLoader(File.ReadAllBytes(filePath));

	var result = format switch {
		"dot" => PansyGraphExporter.ToDot(loader, filter),
		"graphml" => PansyGraphExporter.ToGraphML(loader, filter),
		"json" => PansyGraphExporter.ToJson(loader, filter),
		_ => null
	};

	if (result == null) {
		AnsiConsole.MarkupLine($"[red]Error:[/] Unknown format '{Markup.Escape(format)}'. Use: dot, graphml, json");
		return 1;
	}

	if (outputPath != null) {
		File.WriteAllText(outputPath, result);
		AnsiConsole.MarkupLine($"[green]Graph written to:[/] {Markup.Escape(outputPath)} ({result.Length:N0} chars)");
	} else {
		Console.Write(result);
	}

	return 0;
}

static int RunAnalyze(string[] args) {
	if (args.Length < 1) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing arguments");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy analyze <pansy-file> [[rom-file]] [[-p|--patterns]]");
		AnsiConsole.MarkupLine("  Without ROM: CDL-only coverage analysis");
		AnsiConsole.MarkupLine("  With ROM: full analysis including pattern detection");
		return 1;
	}

	var pansyPath = args[0];
	if (!File.Exists(pansyPath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(pansyPath)}");
		return 1;
	}

	string? romPath = null;
	if (args.Length >= 2 && !args[1].StartsWith('-')) {
		romPath = args[1];
	}

	var detectPatterns = args.Any(a => a == "-p" || a == "--patterns");

	var pansyData = File.ReadAllBytes(pansyPath);
	var loader = new PansyLoader(pansyData);

	AnsiConsole.MarkupLine($"[bold magenta]🌼 ROM Coverage Analysis[/]");
	AnsiConsole.MarkupLine($"[grey]File: {Markup.Escape(Path.GetFileName(pansyPath))}[/]");
	AnsiConsole.WriteLine();

	AnalysisResult result;

	if (romPath != null) {
		if (!File.Exists(romPath)) {
			AnsiConsole.MarkupLine($"[red]Error:[/] ROM file not found: {Markup.Escape(romPath)}");
			return 1;
		}
		var romData = File.ReadAllBytes(romPath);
		result = PansyAnalyzer.Analyze(loader, romData, detectPatterns);
	} else {
		result = PansyAnalyzer.AnalyzeCoverage(loader, (int)loader.RomSize);
	}

	// Coverage summary
	var coverageTable = new Table()
		.Border(TableBorder.Rounded)
		.Title("[bold cyan]Coverage Summary[/]")
		.AddColumn("Metric")
		.AddColumn(new TableColumn("Value").RightAligned());

	coverageTable.AddRow("Total Bytes", $"{result.TotalBytes:N0}");
	coverageTable.AddRow("Classified Bytes", $"{result.ClassifiedBytes:N0}");
	coverageTable.AddRow("Unclassified Bytes", $"{result.TotalBytes - result.ClassifiedBytes:N0}");

	var coveragePct = result.CoveragePercent;
	var coverageColor = coveragePct >= 80 ? "green" : coveragePct >= 50 ? "yellow" : "red";
	coverageTable.AddRow("Coverage", $"[{coverageColor}]{coveragePct:F1}%[/]");

	if (result.CdlClassifiedBytes > 0) {
		coverageTable.AddRow("[grey]  CDL classified[/]", $"[grey]{result.CdlClassifiedBytes:N0}[/]");
	}
	if (result.SymbolCoveredBytes > 0) {
		coverageTable.AddRow("[grey]  Symbol covered[/]", $"[grey]{result.SymbolCoveredBytes:N0}[/]");
	}
	if (result.CrossRefCoveredBytes > 0) {
		coverageTable.AddRow("[grey]  Cross-ref covered[/]", $"[grey]{result.CrossRefCoveredBytes:N0}[/]");
	}

	AnsiConsole.Write(coverageTable);
	AnsiConsole.WriteLine();

	// Coverage bar
	if (result.TotalBytes > 0) {
		var chart = new BarChart()
			.Label("[bold cyan]Coverage[/]")
			.CenterLabel();

		chart.AddItem("Classified", result.ClassifiedBytes, Color.Green);
		chart.AddItem("Unclassified", result.TotalBytes - result.ClassifiedBytes, Color.Red);

		AnsiConsole.Write(chart);
		AnsiConsole.WriteLine();
	}

	// Gaps
	if (result.Gaps.Count > 0) {
		var gapTable = new Table()
			.Border(TableBorder.Rounded)
			.Title($"[bold cyan]Gaps ({result.Gaps.Count})[/]")
			.AddColumn("Offset")
			.AddColumn("End")
			.AddColumn(new TableColumn("Length").RightAligned());

		foreach (var gap in result.Gaps.Take(25)) {
			gapTable.AddRow(
				$"${gap.Offset:x6}",
				$"${gap.End:x6}",
				$"{gap.Length:N0}"
			);
		}

		if (result.Gaps.Count > 25) {
			gapTable.AddRow("[grey]...[/]", $"[grey]+{result.Gaps.Count - 25} more[/]", "");
		}

		AnsiConsole.Write(gapTable);
		AnsiConsole.WriteLine();
	}

	// Patterns
	if (result.Patterns.Count > 0) {
		var patternTable = new Table()
			.Border(TableBorder.Rounded)
			.Title($"[bold cyan]Detected Patterns ({result.Patterns.Count})[/]")
			.AddColumn("Offset")
			.AddColumn("Kind")
			.AddColumn(new TableColumn("Length").RightAligned())
			.AddColumn("Confidence")
			.AddColumn("Description");

		foreach (var pattern in result.Patterns.Take(25)) {
			patternTable.AddRow(
				$"${pattern.Offset:x6}",
				pattern.Kind.ToString(),
				$"{pattern.Length:N0}",
				$"{pattern.Confidence:P0}",
				Markup.Escape(pattern.Description ?? "")
			);
		}

		if (result.Patterns.Count > 25) {
			patternTable.AddRow("[grey]...[/]", "", "", "", $"[grey]+{result.Patterns.Count - 25} more[/]");
		}

		AnsiConsole.Write(patternTable);
		AnsiConsole.WriteLine();
	}

	// Symbol boundaries
	if (result.SymbolBoundaries.Count > 0) {
		var boundaryTable = new Table()
			.Border(TableBorder.Rounded)
			.Title($"[bold cyan]Symbol Boundaries ({result.SymbolBoundaries.Count})[/]")
			.AddColumn("Start")
			.AddColumn("End")
			.AddColumn("Name")
			.AddColumn("Type")
			.AddColumn(new TableColumn("Length").RightAligned());

		foreach (var b in result.SymbolBoundaries.Take(20)) {
			boundaryTable.AddRow(
				$"${b.StartAddress:x6}",
				$"${b.EndAddress:x6}",
				Markup.Escape(b.Name),
				b.Type.ToString(),
				$"{b.Length:N0}"
			);
		}

		if (result.SymbolBoundaries.Count > 20) {
			boundaryTable.AddRow("[grey]...[/]", "", $"[grey]+{result.SymbolBoundaries.Count - 20} more[/]", "", "");
		}

		AnsiConsole.Write(boundaryTable);
		AnsiConsole.WriteLine();
	}

	// Cross-ref graph stats
	if (result.GraphStats != null && result.GraphStats.TotalCrossRefs > 0) {
		var gs = result.GraphStats;
		var graphTable = new Table()
			.Border(TableBorder.Rounded)
			.Title("[bold cyan]Cross-Reference Graph[/]")
			.AddColumn("Metric")
			.AddColumn(new TableColumn("Value").RightAligned());

		graphTable.AddRow("Total Cross-refs", $"{gs.TotalCrossRefs:N0}");
		graphTable.AddRow("Unique Sources", $"{gs.UniqueSourceAddresses:N0}");
		graphTable.AddRow("Unique Targets", $"{gs.UniqueTargetAddresses:N0}");
		graphTable.AddRow("[grey]  JSR[/]", $"[grey]{gs.JsrCount:N0}[/]");
		graphTable.AddRow("[grey]  JMP[/]", $"[grey]{gs.JmpCount:N0}[/]");
		graphTable.AddRow("[grey]  Branch[/]", $"[grey]{gs.BranchCount:N0}[/]");
		graphTable.AddRow("[grey]  Read[/]", $"[grey]{gs.ReadCount:N0}[/]");
		graphTable.AddRow("[grey]  Write[/]", $"[grey]{gs.WriteCount:N0}[/]");

		if (gs.UnreferencedSubroutines.Count > 0) {
			graphTable.AddRow("[yellow]Unreferenced subs[/]", $"[yellow]{gs.UnreferencedSubroutines.Count:N0}[/]");
		}

		AnsiConsole.Write(graphTable);
		AnsiConsole.WriteLine();

		if (gs.MostReferenced.Count > 0) {
			var topTable = new Table()
				.Border(TableBorder.Rounded)
				.Title("[bold cyan]Most Referenced Addresses[/]")
				.AddColumn("Address")
				.AddColumn("Symbol")
				.AddColumn(new TableColumn("References").RightAligned());

			foreach (var (addr, count) in gs.MostReferenced.Take(10)) {
				var symbol = loader.GetSymbol(addr) ?? "[grey]-[/]";
				topTable.AddRow($"${addr:x6}", Markup.Escape(symbol), $"{count}");
			}

			AnsiConsole.Write(topTable);
			AnsiConsole.WriteLine();
		}
	}

	return 0;
}
