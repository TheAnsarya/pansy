using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pansy.Core;

namespace Pansy.UI.ViewModels;

/// <summary>
/// View model for the main window displaying and editing Pansy file contents.
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged {
	private string? _fileName;
	private string? _filePath;
	private PansyLoader? _currentLoader;
	private bool _isDirty;
	private string? _searchText;
	private SymbolInfo? _selectedSymbol;
	private CommentInfo? _selectedComment;
	private MemoryRegionInfo? _selectedMemoryRegion;
	private CrossRefInfo? _selectedCrossRef;

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Title => $"🌼 Pansy - {(IsDirty ? "* " : "")}{FileName ?? "Disassembly Metadata Editor"}";

	public string? FileName {
		get => _fileName;
		set {
			_fileName = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(Title));
			OnPropertyChanged(nameof(HasFileLoaded));
		}
	}

	public bool IsDirty {
		get => _isDirty;
		set {
			_isDirty = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(Title));
			OnPropertyChanged(nameof(CanSave));
		}
	}

	public string? SearchText {
		get => _searchText;
		set {
			_searchText = value;
			OnPropertyChanged();
			if (string.IsNullOrWhiteSpace(value)) {
				ClearSearch();
			} else {
				ApplySearch(value);
			}
		}
	}

	public SymbolInfo? SelectedSymbol {
		get => _selectedSymbol;
		set {
			_selectedSymbol = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(CanEditSymbol));
			OnPropertyChanged(nameof(CanDeleteSymbol));
		}
	}

	public CommentInfo? SelectedComment {
		get => _selectedComment;
		set {
			_selectedComment = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(CanEditComment));
			OnPropertyChanged(nameof(CanDeleteComment));
		}
	}

	public MemoryRegionInfo? SelectedMemoryRegion {
		get => _selectedMemoryRegion;
		set {
			_selectedMemoryRegion = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(CanEditMemoryRegion));
			OnPropertyChanged(nameof(CanDeleteMemoryRegion));
		}
	}

	public CrossRefInfo? SelectedCrossRef {
		get => _selectedCrossRef;
		set {
			_selectedCrossRef = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(CanDeleteCrossRef));
		}
	}

	private string? _platformName;
	private string? _romSize;
	private string? _romCrc;
	private string? _fileVersion;
	private string? _isCompressed;

	public string? PlatformName {
		get => _platformName;
		set { _platformName = value; OnPropertyChanged(); }
	}

	public string? RomSize {
		get => _romSize;
		set { _romSize = value; OnPropertyChanged(); }
	}

	public string? RomCrc {
		get => _romCrc;
		set { _romCrc = value; OnPropertyChanged(); }
	}

	public string? FileVersion {
		get => _fileVersion;
		set { _fileVersion = value; OnPropertyChanged(); }
	}

	public string? IsCompressed {
		get => _isCompressed;
		set { _isCompressed = value; OnPropertyChanged(); }
	}

	private string? _symbolCount;
	private string? _commentCount;
	private string? _codeOffsetCount;
	private string? _dataOffsetCount;
	private string? _jumpTargetCount;
	private string? _subroutineCount;
	private string? _memoryRegionCount;
	private string? _crossRefCount;

	public string? SymbolCount {
		get => _symbolCount;
		set { _symbolCount = value; OnPropertyChanged(); }
	}

	public string? CommentCount {
		get => _commentCount;
		set { _commentCount = value; OnPropertyChanged(); }
	}

	public string? CodeOffsetCount {
		get => _codeOffsetCount;
		set { _codeOffsetCount = value; OnPropertyChanged(); }
	}

	public string? DataOffsetCount {
		get => _dataOffsetCount;
		set { _dataOffsetCount = value; OnPropertyChanged(); }
	}

	public string? JumpTargetCount {
		get => _jumpTargetCount;
		set { _jumpTargetCount = value; OnPropertyChanged(); }
	}

	public string? SubroutineCount {
		get => _subroutineCount;
		set { _subroutineCount = value; OnPropertyChanged(); }
	}

	public string? MemoryRegionCount {
		get => _memoryRegionCount;
		set { _memoryRegionCount = value; OnPropertyChanged(); }
	}

	public string? CrossRefCount {
		get => _crossRefCount;
		set { _crossRefCount = value; OnPropertyChanged(); }
	}

	public ObservableCollection<SymbolInfo> Symbols { get; } = new();
	public ObservableCollection<SymbolInfo> FilteredSymbols { get; } = new();
	public ObservableCollection<CommentInfo> Comments { get; } = new();
	public ObservableCollection<CommentInfo> FilteredComments { get; } = new();
	public ObservableCollection<MemoryRegionInfo> MemoryRegions { get; } = new();
	public ObservableCollection<CrossRefInfo> CrossReferences { get; } = new();

	public bool HasFileLoaded => !string.IsNullOrEmpty(FileName);
	public bool CanSave => HasFileLoaded && IsDirty;
	public bool CanEditSymbol => SelectedSymbol != null;
	public bool CanDeleteSymbol => SelectedSymbol != null;
	public bool CanEditComment => SelectedComment != null;
	public bool CanDeleteComment => SelectedComment != null;
	public bool CanEditMemoryRegion => SelectedMemoryRegion != null;
	public bool CanDeleteMemoryRegion => SelectedMemoryRegion != null;
	public bool CanDeleteCrossRef => SelectedCrossRef != null;

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void AddSymbol(string address, string name) {
		var newSymbol = new SymbolInfo { Address = address, Name = name };
		Symbols.Add(newSymbol);
		FilteredSymbols.Add(newSymbol);
		SymbolCount = Symbols.Count.ToString("N0");
		IsDirty = true;
	}

	public void EditSymbol(SymbolInfo symbol, string newAddress, string newName) {
		symbol.Address = newAddress;
		symbol.Name = newName;
		IsDirty = true;
	}

	public void DeleteSymbol(SymbolInfo symbol) {
		Symbols.Remove(symbol);
		FilteredSymbols.Remove(symbol);
		SymbolCount = Symbols.Count.ToString("N0");
		IsDirty = true;
	}

	public void AddComment(string address, string text) {
		var newComment = new CommentInfo { Address = address, Text = text };
		Comments.Add(newComment);
		FilteredComments.Add(newComment);
		CommentCount = Comments.Count.ToString("N0");
		IsDirty = true;
	}

	public void EditComment(CommentInfo comment, string newAddress, string newText) {
		comment.Address = newAddress;
		comment.Text = newText;
		IsDirty = true;
	}

	public void DeleteComment(CommentInfo comment) {
		Comments.Remove(comment);
		FilteredComments.Remove(comment);
		CommentCount = Comments.Count.ToString("N0");
		IsDirty = true;
	}

	public void AddMemoryRegion(string name, string type, string start, string end, string bank) {
		var newRegion = new MemoryRegionInfo {
			Name = name,
			Type = type,
			Start = start,
			End = end,
			Bank = bank
		};
		MemoryRegions.Add(newRegion);
		MemoryRegionCount = MemoryRegions.Count.ToString("N0");
		IsDirty = true;
	}

	public void EditMemoryRegion(MemoryRegionInfo region, string name, string type, string start, string end, string bank) {
		region.Name = name;
		region.Type = type;
		region.Start = start;
		region.End = end;
		region.Bank = bank;
		IsDirty = true;
	}

	public void DeleteMemoryRegion(MemoryRegionInfo region) {
		MemoryRegions.Remove(region);
		MemoryRegionCount = MemoryRegions.Count.ToString("N0");
		IsDirty = true;
	}

	public void DeleteCrossRef(CrossRefInfo xref) {
		CrossReferences.Remove(xref);
		CrossRefCount = CrossReferences.Count.ToString("N0");
		IsDirty = true;
	}

	public void ApplySearch(string searchText) {
		SearchText = searchText;
		var lowerSearch = searchText.ToLowerInvariant();

		FilteredSymbols.Clear();
		foreach (var symbol in Symbols) {
			if (symbol.Address.ToLowerInvariant().Contains(lowerSearch) ||
				symbol.Name.ToLowerInvariant().Contains(lowerSearch)) {
				FilteredSymbols.Add(symbol);
			}
		}

		FilteredComments.Clear();
		foreach (var comment in Comments) {
			if (comment.Address.ToLowerInvariant().Contains(lowerSearch) ||
				comment.Text.ToLowerInvariant().Contains(lowerSearch)) {
				FilteredComments.Add(comment);
			}
		}
	}

	public void ClearSearch() {
		SearchText = "";
		FilteredSymbols.Clear();
		FilteredComments.Clear();
		foreach (var symbol in Symbols)
			FilteredSymbols.Add(symbol);
		foreach (var comment in Comments)
			FilteredComments.Add(comment);
	}

	/// <summary>
	/// Load and display a Pansy file.
	/// </summary>
	public async Task LoadFileAsync(string filePath) {
		await Task.Run(() => LoadFile(filePath));
	}

	/// <summary>
	/// Save the current Pansy file.
	/// </summary>
	public async Task<bool> SaveFileAsync() {
		if (string.IsNullOrEmpty(_filePath) || _currentLoader == null)
			return false;

		try {
			await Task.Run(() => SaveFile(_filePath));
			IsDirty = false;
			return true;
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error saving file: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Save the current Pansy file to a new location.
	/// </summary>
	public async Task<bool> SaveFileAsAsync(string filePath) {
		if (_currentLoader == null)
			return false;

		try {
			await Task.Run(() => SaveFile(filePath));
			_filePath = filePath;
			FileName = Path.GetFileName(filePath);
			IsDirty = false;
			return true;
		} catch (Exception ex) {
			Console.Error.WriteLine($"Error saving file: {ex.Message}");
			return false;
		}
	}

	private void SaveFile(string filePath) {
		if (_currentLoader == null)
			return;

		var writer = new PansyWriter {
			Platform = _currentLoader.Platform,
			RomSize = _currentLoader.RomSize,
			RomCrc32 = _currentLoader.RomCrc32,
			ProjectName = _currentLoader.ProjectName,
			Author = _currentLoader.Author,
			ProjectVersion = _currentLoader.ProjectVersion
		};

		// Add all symbols
		foreach (var symbol in Symbols) {
			var addr = ParseAddress(symbol.Address);
			writer.AddSymbol(addr, symbol.Name);
		}

		// Add all comments
		foreach (var comment in Comments) {
			var addr = ParseAddress(comment.Address);
			writer.AddComment(addr, comment.Text);
		}

		// Add memory regions
		foreach (var region in MemoryRegions) {
			var start = ParseAddress(region.Start);
			var end = ParseAddress(region.End);
			var bank = region.Bank == "-" ? (byte)0 : Convert.ToByte(region.Bank.TrimStart('$'), 16);
			var type = GetRegionTypeValue(region.Type);
			writer.AddMemoryRegion(new MemoryRegion(start, end, type, bank, region.Name));
		}

		// Add cross-references
		foreach (var xref in CrossReferences) {
			var from = ParseAddress(xref.From);
			var to = ParseAddress(xref.To);
			var type = Enum.Parse<CrossRefType>(xref.Type);
			writer.AddCrossReference(new CrossReference(from, to, type));
		}

		// Preserve code/data/jump/sub flags if possible
		if (_currentLoader != null) {
			foreach (var offset in _currentLoader.CodeOffsets) {
				writer.MarkAsCode((uint)offset);
			}
			foreach (var offset in _currentLoader.DataOffsets) {
				writer.MarkAsData((uint)offset);
			}
			foreach (var offset in _currentLoader.JumpTargets) {
				writer.MarkAsJumpTarget((uint)offset);
			}
			foreach (var offset in _currentLoader.SubEntryPoints) {
				writer.MarkAsSubroutine((uint)offset);
			}
		}

		var data = writer.Generate();
		File.WriteAllBytes(filePath, data);
	}

	private static uint ParseAddress(string address) {
		var cleaned = address.TrimStart('$');
		return Convert.ToUInt32(cleaned, 16);
	}

	private static byte GetRegionTypeValue(string typeName) {
		return typeName switch {
			"ROM" => 0x01,
			"RAM" => 0x02,
			"SRAM" => 0x03,
			"VRAM" => 0x04,
			"PRG-ROM" => 0x05,
			"CHR-ROM" => 0x06,
			"Custom" => 0xFF,
			_ => 0x01
		};
	}

	private void LoadFile(string filePath) {
		try {
			var data = File.ReadAllBytes(filePath);
			var loader = new PansyLoader(data);

			// Track the current file
			_currentLoader = loader;
			_filePath = filePath;

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
			FilteredSymbols.Clear();
			foreach (var symbol in loader.Symbols.OrderBy(s => s.Key)) {
				var symbolInfo = new SymbolInfo {
					Address = $"${symbol.Key:X4}",
					Name = symbol.Value
				};
				Symbols.Add(symbolInfo);
				FilteredSymbols.Add(symbolInfo);
			}

			// Load comments (sorted by address)
			Comments.Clear();
			FilteredComments.Clear();
			foreach (var comment in loader.Comments.OrderBy(c => c.Key)) {
				var commentInfo = new CommentInfo {
					Address = $"${comment.Key:X4}",
					Text = comment.Value
				};
				Comments.Add(commentInfo);
				FilteredComments.Add(commentInfo);
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

			IsDirty = false;
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
