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
	AnsiConsole.MarkupLine("[cyan]Usage:[/] pansy <command> [options]");
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[cyan]Commands:[/]");
	AnsiConsole.MarkupLine("  info <file>     Display information about a Pansy file");
	AnsiConsole.WriteLine();
	return 0;
}

var command = args[0].ToLowerInvariant();

try {
	switch (command) {
		case "info":
			return RunInfo(args.Skip(1).ToArray());
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
