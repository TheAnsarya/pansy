# Pansy CLI Command Reference

The Pansy command-line interface provides tools for inspecting, searching, and analyzing Pansy metadata files.

## Installation

```bash
# Build the CLI
cd src/Pansy.Cli
dotnet build

# Run commands
dotnet run -- <command> [options]

# Or use the built executable
./bin/Debug/net10.0/Pansy.Cli <command> [options]
```

## Global Options

All commands support:
- `--help` - Show command-specific help
- `-v`, `--verbose` - Enable verbose output (where applicable)

## Commands

### `info` - Display File Information

Shows comprehensive information about a Pansy file including header details, content statistics, and memory regions.

**Usage:**
```bash
dotnet run --project src/Pansy.Cli -- info <file> [-v|--verbose]
```

**Arguments:**
- `<file>` - Path to the Pansy file

**Options:**
- `-v`, `--verbose` - Show additional details

**Example:**
```bash
$ dotnet run --project src/Pansy.Cli -- info game.pansy

🌼 Pansy File Viewer

╭────────────────┬───────────────────╮
│ Property       │ Value             │
├────────────────┼───────────────────┤
│ Format Version │ 0100              │
│ Platform       │ NES               │
│ ROM Size       │ 32768 bytes (32K) │
│ ROM CRC32      │ 12345678          │
│ Flags          │ None              │
╰────────────────┴───────────────────╯

Content Statistics:
  • Symbols: 7
  • Comments: 3
  • Code Offsets: 7
  • Data Offsets: 0
  • Jump Targets: 2
  • Subroutines: 3
  • Memory Regions: 2
  • Cross-refs: 4

Memory Regions:
╭───────┬───────┬──────┬──────┬────────────────╮
│ Start │ End   │ Bank │ Type │ Name           │
├───────┼───────┼──────┼──────┼────────────────┤
│ $8000 │ $bfff │ 0    │ 1    │ PRG-ROM Bank 0 │
│ $c000 │ $ffff │ 1    │ 1    │ PRG-ROM Bank 1 │
╰───────┴───────┴──────┴──────┴────────────────╯
```

**Output:**
- File header (version, platform, ROM size/CRC, flags)
- Content statistics (symbol counts, code/data coverage)
- Memory regions table (if any)
- Project metadata (if present)

---

### `symbols` - List Symbols

Lists all symbols defined in a Pansy file with addresses and names.

**Usage:**
```bash
dotnet run --project src/Pansy.Cli -- symbols <file> [--sort address|name] [--filter <pattern>]
```

**Arguments:**
- `<file>` - Path to the Pansy file

**Options:**
- `--sort address` - Sort by address (default)
- `--sort name` - Sort alphabetically by name
- `--filter <pattern>` - Filter symbols by pattern (case-insensitive)

**Example:**
```bash
$ dotnet run --project src/Pansy.Cli -- symbols game.pansy

🌼 Symbols in game.pansy
Total: 7 symbols

╭─────────┬─────────────────╮
│ Address │ Name            │
├─────────┼─────────────────┤
│ $8000   │ Reset           │
│ $8003   │ NMI_Handler     │
│ $8006   │ IRQ_Handler     │
│ $8010   │ Main_Loop       │
│ $8050   │ Update_Graphics │
│ $8100   │ Read_Controller │
│ $8150   │ Play_Sound      │
╰─────────┴─────────────────╯
```

**Output:**
- Table of addresses and symbol names
- Total symbol count

---

### `find` - Search Symbols and Comments

Searches for symbols and comments matching a pattern. Supports plain text, regex, and wildcard patterns.

**Usage:**
```bash
dotnet run --project src/Pansy.Cli -- find <file> <pattern> [options]
```

**Arguments:**
- `<file>` - Path to the Pansy file
- `<pattern>` - Search pattern

**Options:**
- `-c`, `--comments` - Search comments only
- `-s`, `--symbols` - Search symbols only
- `-i`, `--case-insensitive` - Case-insensitive search
- `-r`, `--regex` - Treat pattern as a regex
- `-w`, `--wildcard` - Treat pattern as a wildcard (* and ?)

**Pattern Modes:**
- **Plain text** (default): Matches if the pattern appears anywhere in the text
  - Example: `Main` matches `Main_Loop`, `Update_Main`
- **Regex** (`-r`): Full regular expression support
  - Example: `^NMI_.*Handler$` matches exactly `NMI_Handler`
- **Wildcard** (`-w`): Simple wildcard patterns
  - `*` matches any characters (zero or more)
  - `?` matches any single character
  - Example: `*Loop` matches `Main_Loop`, `Update_Loop`

**Examples:**
```bash
# Plain text search
$ dotnet run --project src/Pansy.Cli -- find game.pansy "Handler"

🌼 Search results for 'Handler' (text, case-sensitive)

Symbols:
╭─────────┬─────────────╮
│ Address │ Name        │
├─────────┼─────────────┤
│ $8003   │ NMI_Handler │
│ $8006   │ IRQ_Handler │
╰─────────┴─────────────╯

Found 2 match(es)

# Regex search
$ dotnet run --project src/Pansy.Cli -- find game.pansy "^NMI_.*" -r

# Wildcard search (case-insensitive)
$ dotnet run --project src/Pansy.Cli -- find game.pansy "*Main*" -w -i

# Search only symbols
$ dotnet run --project src/Pansy.Cli -- find game.pansy "Loop" -s
```

**Output:**
- Matching symbols table (if searching symbols)
- Matching comments table (if searching comments)
- Total match count

---

### `xrefs` - Show Cross-References

Displays cross-references for a specific address, showing both incoming (references TO) and outgoing (references FROM) the address.

**Usage:**
```bash
dotnet run --project src/Pansy.Cli -- xrefs <file> <address>
```

**Arguments:**
- `<file>` - Path to the Pansy file
- `<address>` - Address to查询 (decimal format, e.g., 32784 for $8010)

**Example:**
```bash
$ dotnet run --project src/Pansy.Cli -- xrefs game.pansy 32784

🌼 Cross-references for $8010
Symbol: Main_Loop

References TO this address:
╭───────┬──────┬────────╮
│ From  │ Type │ Symbol │
├───────┼──────┼────────┤
│ $8070 │ Jmp  │        │
╰───────┴──────┴────────╯

Total: 1 incoming, 0 outgoing
```

**Output:**
- Target address and symbol name (if any)
- Incoming references table (FROM → TO this address)
- Outgoing references table (FROM this address → TO)
- Total counts

**Note:** Addresses must be specified in decimal. To convert hex to decimal:
- `$8000` = 32768
- `$8010` = 32784
- `$C000` = 49152

---

### `diff` - Compare Files

Compares two Pansy files and shows differences in headers, symbols, comments, and cross-references.

**Usage:**
```bash
dotnet run --project src/Pansy.Cli -- diff <file1> <file2> [--summary]
```

**Arguments:**
- `<file1>` - Path to first Pansy file
- `<file2>` - Path to second Pansy file

**Options:**
- `--summary` - Show only summary, not detailed differences

**Example:**
```bash
$ dotnet run --project src/Pansy.Cli -- diff original.pansy modified.pansy

🌼 Comparing Pansy Files

Header Comparison:
╭──────────┬──────────┬──────────╮
│ Property │ File 1   │ File 2   │
├──────────┼──────────┼──────────┤
│ Platform │ NES      │ NES      │
│ ROM Size │ 32768    │ 32768    │
│ ROM CRC  │ 12345678 │ 12345678 │
╰──────────┴──────────┴──────────╯

Symbol Changes:
  • Added: 3
  • Removed: 1
  • Modified: 0

Symbols Added:
  $8200: New_Function
  $8210: Helper_Routine
  $8220: Data_Table

Symbols Removed:
  $8150: Old_Function
```

**Output:**
- Header comparison table
- Change statistics
- Added/removed/modified symbols
- Added/removed comments
- Cross-reference differences

---

## Error Handling

All commands return exit codes:
- `0` - Success
- `1` - Error (file not found, invalid format, etc.)

Error messages are displayed in red with the `Error:` prefix.

**Example:**
```bash
$ dotnet run --project src/Pansy.Cli -- info missing.pansy
Error: File not found: missing.pansy
```

## Advanced Usage

### Piping Output

Commands output to stdout and can be piped:

```bash
# Count symbols
dotnet run --project src/Pansy.Cli -- symbols game.pansy | grep -c '^│'

# Search for specific pattern
dotnet run --project src/Pansy.Cli -- symbols game.pansy | grep "Handler"

# Save output to file
dotnet run --project src/Pansy.Cli -- info game.pansy > analysis.txt
```

### Batch Processing

Process multiple files with shell scripting:

```bash
# PowerShell: Check all .pansy files
Get-ChildItem *.pansy | ForEach-Object {
	dotnet run --project src/Pansy.Cli -- info $_.FullName
}

# Bash: Find files with symbols count
for file in *.pansy; do
	echo "$file:"
	dotnet run --project src/Pansy.Cli -- info "$file" | grep "Symbols:"
done
```

## Integration Examples

### Mesen Integration

Export symbols to Mesen label file format (coming soon):

```bash
# Convert Pansy to Mesen MLB
dotnet run --project src/Pansy.Cli -- convert game.pansy --to mesen --output game.mlb
```

### CI/CD Integration

Use in continuous integration to verify analysis:

```yaml
# GitHub Actions example
- name: Verify Pansy metadata
  run: |
	dotnet run --project src/Pansy.Cli -- info game.pansy
	if [ $? -ne 0 ]; then
	  echo "Invalid Pansy file"
	  exit 1
	fi
```

## Tips

1. **Use Tab Completion**: Most shells support path completion for file arguments
2. **Verbose Mode**: Add `-v` to see additional details during processing
3. **Hex Addresses**: Remember to convert hex to decimal for the `xrefs` command
4. **Large Files**: For ROMs over 1MB, expect slightly longer loading times
5. **Symbol Naming**: Use consistent naming conventions for better searchability

## Troubleshooting

**Problem:** Command not recognized
```bash
Error: Unknown command: infp
```
**Solution:** Check spelling - valid commands are `info`, `symbols`, `find`, `xrefs`, `diff`

---

**Problem:** File format error
```bash
Error: Invalid Pansy file: bad magic number
```
**Solution:** Verify the file is a valid Pansy file (not corrupted or different format)

---

**Problem:** Address out of range
```bash
Error: Invalid address: 0x8010
```
**Solution:** Use decimal addresses, not hex format:
- Instead of `0x8010`, use `32784`
- Instead of `$C000`, use `49152`

## See Also

- [File Format Specification](FILE-FORMAT.md) - Complete format documentation
- [Library API](../src/Pansy.Core/README.md) - Using Pansy in code
- [GitHub Issues](https://github.com/TheAnsarya/pansy/issues) - Report bugs or request features
