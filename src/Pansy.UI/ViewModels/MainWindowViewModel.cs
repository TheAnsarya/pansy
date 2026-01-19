using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pansy.Core;

namespace Pansy.UI.ViewModels;

/// <summary>
/// View model for the main window displaying Pansy file contents.
/// </summary>
public class MainWindowViewModel {
	public string Title => "🌼 Pansy - Disassembly Metadata Viewer";
	
	public string? FileName { get; set; }
	public string? PlatformName { get; set; }
	public string? RomSize { get; set; }
	public string? RomCrc { get; set; }
	public string? FileVersion { get; set; }
	public string? IsCompressed { get; set; }
	
	public string? SymbolCount { get; set; }
	public string? CommentCount { get; set; }
	public string? CodeOffsetCount { get; set; }
	public string? DataOffsetCount { get; set; }
	public string? JumpTargetCount { get; set; }
	public string? SubroutineCount { get; set; }
	public string? MemoryRegionCount { get; set; }
	public string? CrossRefCount { get; set; }
	
	public ObservableCollection<SymbolInfo> Symbols { get; } = new();
	public ObservableCollection<CommentInfo> Comments { get; } = new();
	public ObservableCollection<MemoryRegionInfo> MemoryRegions { get; } = new();
	public ObservableCollection<CrossRefInfo> CrossReferences { get; } = new();
	
	public bool HasFileLoaded => !string.IsNullOrEmpty(FileName);
	
	/// <summary>
	/// Load and display a Pansy file.
	/// </summary>
	public async Task LoadFileAsync(string filePath) {
		await Task.Run(() => LoadFile(filePath));
	}
	
	private void LoadFile(string filePath) {
		try {
			var data = File.ReadAllBytes(filePath);
			var loader = new PansyLoader(data);
			
			// Load file info
			FileName = Path.GetFileName(filePath);
			PlatformName = GetPlatformName(loader.Platform);
			RomSize = $"{loader.RomSize:N0} bytes";
			RomCrc = $"0x{loader.RomCrc32:X8}";
			FileVersion = $"{loader.Version >> 8}.{loader.Version & 0xFF}";
			IsCompressed = loader.Flags.HasFlag(PansyFlags.Compressed) ? "Yes" : "No";
			
			// Load statistics
			SymbolCount = loader.Symbols.Count.ToString("N0");
			CommentCount = loader.Comments.Count.ToString("N0");
			CodeOffsetCount = loader.CodeOffsets.Count.ToString("N0");
			DataOffsetCount = loader.DataOffsets.Count.ToString("N0");
			JumpTargetCount = loader.JumpTargets.Count.ToString("N0");
			SubroutineCount = loader.SubEntryPoints.Count.ToString("N0");
			MemoryRegionCount = loader.MemoryRegions.Count.ToString("N0");
			CrossRefCount = loader.CrossReferences.Count.ToString("N0");
			
			// Load symbols (sorted by address)
			Symbols.Clear();
			foreach (var symbol in loader.Symbols.OrderBy(s => s.Key)) {
				Symbols.Add(new SymbolInfo {
					Address = $"${symbol.Key:X4}",
					Name = symbol.Value
				});
			}
			
			// Load comments (sorted by address)
			Comments.Clear();
			foreach (var comment in loader.Comments.OrderBy(c => c.Key)) {
				Comments.Add(new CommentInfo {
					Address = $"${comment.Key:X4}",
					Text = comment.Value
				});
			}
			
			// Load memory regions
			MemoryRegions.Clear();
			foreach (var region in loader.MemoryRegions) {
				MemoryRegions.Add(new MemoryRegionInfo {
					Name = region.Name,
					Type = GetRegionTypeName(region.Type),
					Start = $"${region.Start:X4}",
					End = $"${region.End:X4}",
					Bank = region.Bank > 0 ? $"${region.Bank:X2}" : "-"
				});
			}
			
			// Load cross-references (sorted by from address)
			CrossReferences.Clear();
			foreach (var xref in loader.CrossReferences.OrderBy(x => x.From)) {
				CrossReferences.Add(new CrossRefInfo {
					From = $"${xref.From:X4}",
					To = $"${xref.To:X4}",
					Type = xref.Type.ToString()
				});
			}
		} catch (Exception ex) {
			// TODO: Show error dialog
			Console.Error.WriteLine($"Error loading file: {ex.Message}");
		}
	}
	
	private static string GetPlatformName(byte platform) {
		return platform switch {
			0x01 => "NES (6502)",
			0x02 => "SNES (65816)",
			0x03 => "Game Boy (Z80-like)",
			0x04 => "Game Boy Advance (ARM7TDMI)",
			0x05 => "Sega Genesis (68000)",
			0x06 => "Atari 2600 (6507)",
			0xFF => "Custom",
			_ => $"Unknown (0x{platform:X2})"
		};
	}
	
	private static string GetRegionTypeName(byte type) {
		return type switch {
			0x01 => "ROM",
			0x02 => "RAM",
			0x03 => "SRAM",
			0x04 => "VRAM",
			0x05 => "PRG-ROM",
			0x06 => "CHR-ROM",
			0xFF => "Custom",
			_ => $"Unknown (0x{type:X2})"
		};
	}
}

public class SymbolInfo {
	public string Address { get; set; } = "";
	public string Name { get; set; } = "";
}

public class CommentInfo {
	public string Address { get; set; } = "";
	public string Text { get; set; } = "";
}

public class MemoryRegionInfo {
	public string Name { get; set; } = "";
	public string Type { get; set; } = "";
	public string Start { get; set; } = "";
	public string End { get; set; } = "";
	public string Bank { get; set; } = "";
}

public class CrossRefInfo {
	public string From { get; set; } = "";
	public string To { get; set; } = "";
	public string Type { get; set; } = "";
}
