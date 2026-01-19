using Avalonia.Controls;
using Avalonia.Interactivity;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pansy.UI;

public partial class MemoryRegionDialog : Window, INotifyPropertyChanged {
	private string _regionName = "";
	private string _regionType = "ROM";
	private string _startAddress = "";
	private string _endAddress = "";
	private string _bank = "-";

	public string RegionName {
		get => _regionName;
		set { _regionName = value; OnPropertyChanged(); }
	}

	public string RegionType {
		get => _regionType;
		set { _regionType = value; OnPropertyChanged(); }
	}

	public string StartAddress {
		get => _startAddress;
		set { _startAddress = value; OnPropertyChanged(); }
	}

	public string EndAddress {
		get => _endAddress;
		set { _endAddress = value; OnPropertyChanged(); }
	}

	public string Bank {
		get => _bank;
		set { _bank = value; OnPropertyChanged(); }
	}

	public new event PropertyChangedEventHandler? PropertyChanged;

	public MemoryRegionDialog() {
		InitializeComponent();
		DataContext = this;
	}

	public MemoryRegionDialog(string name, string type, string start, string end, string bank) : this() {
		RegionName = name;
		RegionType = type;
		StartAddress = start;
		EndAddress = end;
		Bank = bank;
	}

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void OK_Click(object? sender, RoutedEventArgs e) {
		Close((true, RegionName, RegionType, StartAddress, EndAddress, Bank));
	}

	private void Cancel_Click(object? sender, RoutedEventArgs e) {
		Close((false, "", "", "", "", ""));
	}
}
