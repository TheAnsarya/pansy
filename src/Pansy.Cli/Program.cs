// ============================================================================
// Program.cs - CLI Entry Point
// 🌼 Pansy - Universal Disassembly Metadata Format
// ============================================================================

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
	AnsiConsole.MarkupLine("  find <file> <pattern>   Search for symbols/comments matching pattern");
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
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy find <file> <pattern> [-c|--comments] [-s|--symbols] [-i|--case-insensitive]");
		return 1;
	}

	var filePath = args[0];
	var pattern = args[1];
	var searchComments = args.Any(a => a == "-c" || a == "--comments");
	var searchSymbols = args.Any(a => a == "-s" || a == "--symbols");
	var caseInsensitive = args.Any(a => a == "-i" || a == "--case-insensitive");

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

	var comparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

	AnsiConsole.MarkupLine($"[bold magenta]🌼 Search results for '{Markup.Escape(pattern)}'[/]");
	AnsiConsole.WriteLine();

	var foundCount = 0;

	// Search symbols
	if (searchSymbols && pansy.Symbols.Count > 0) {
		var matches = pansy.Symbols.Where(x => x.Value.Contains(pattern, comparison)).ToList();
		if (matches.Count > 0) {
			AnsiConsole.MarkupLine("[bold cyan]Symbols:[/]");
			var table = new Table()
				.Border(TableBorder.Rounded)
				.AddColumn("Address")
				.AddColumn("Name");

			foreach (var (addr, name) in matches.OrderBy(x => x.Key)) {
				table.AddRow($"${addr:X4}", Markup.Escape(name));
				foundCount++;
			}

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}
	}

	// Search comments
	if (searchComments && pansy.Comments.Count > 0) {
		var matches = pansy.Comments.Where(x => x.Value.Contains(pattern, comparison)).ToList();
		if (matches.Count > 0) {
			AnsiConsole.MarkupLine("[bold cyan]Comments:[/]");
			var table = new Table()
				.Border(TableBorder.Rounded)
				.AddColumn("Address")
				.AddColumn("Comment");

			foreach (var (addr, comment) in matches.OrderBy(x => x.Key)) {
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

static int RunXrefs(string[] args) {
	if (args.Length < 2) {
		AnsiConsole.MarkupLine("[red]Error:[/] Missing arguments");
		AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy xrefs <file> <address>");
		AnsiConsole.MarkupLine("[grey]Address format: decimal (12345) or hex with $ prefix ($3039)[/]");
		return 1;
	}

	var filePath = args[0];
	var addressStr = args[1];

	if (!File.Exists(filePath)) {
		AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(filePath)}");
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

	var data = File.ReadAllBytes(filePath);
	var pansy = new PansyLoader(data);

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
