using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Pansy.UI.ViewModels;

namespace Pansy.UI;

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();
		DataContext = new MainWindowViewModel();
	}

	private async void OpenFile_Click(object? sender, RoutedEventArgs e) {
		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel == null) return;

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
			Title = "Open Pansy File",
			AllowMultiple = false,
			FileTypeFilter = new[] {
				new FilePickerFileType("Pansy Files") { Patterns = new[] { "*.pansy" } },
				new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
			}
		});

		if (files.Count > 0) {
			var file = files[0];
			var path = file.TryGetLocalPath();
			if (!string.IsNullOrEmpty(path) && DataContext is MainWindowViewModel vm) {
				await vm.LoadFileAsync(path);
			}
		}
	}

	private async void SaveFile_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is MainWindowViewModel vm) {
			await vm.SaveFileAsync();
		}
	}

	private async void SaveFileAs_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is not MainWindowViewModel vm) return;

		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel == null) return;

		var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
			Title = "Save Pansy File As",
			FileTypeChoices = new[] {
				new FilePickerFileType("Pansy Files") { Patterns = new[] { "*.pansy" } }
			},
			DefaultExtension = "pansy",
			SuggestedFileName = "output.pansy"
		});

		if (file != null) {
			var path = file.TryGetLocalPath();
			if (!string.IsNullOrEmpty(path)) {
				await vm.SaveFileAsAsync(path);
			}
		}
	}

	private async void AddSymbol_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is not MainWindowViewModel vm) return;

		var dialog = new InputDialog("Add Symbol", "Address:", "Name:");
		var result = await dialog.ShowDialog<(bool, string, string)>(this);
		
		if (result.Item1) {
			vm.AddSymbol(result.Item2, result.Item3);
		}
	}

	private async void EditSymbol_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is not MainWindowViewModel vm || vm.SelectedSymbol == null) return;

		var dialog = new InputDialog("Edit Symbol", "Address:", "Name:", 
			vm.SelectedSymbol.Address, vm.SelectedSymbol.Name);
		var result = await dialog.ShowDialog<(bool, string, string)>(this);
		
		if (result.Item1) {
			vm.EditSymbol(vm.SelectedSymbol, result.Item2, result.Item3);
		}
	}

	private void DeleteSymbol_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is MainWindowViewModel vm && vm.SelectedSymbol != null) {
			vm.DeleteSymbol(vm.SelectedSymbol);
		}
	}

	private async void AddComment_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is not MainWindowViewModel vm) return;

		var dialog = new InputDialog("Add Comment", "Address:", "Text:");
		var result = await dialog.ShowDialog<(bool, string, string)>(this);
		
		if (result.Item1) {
			vm.AddComment(result.Item2, result.Item3);
		}
	}

	private async void EditComment_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is not MainWindowViewModel vm || vm.SelectedComment == null) return;

		var dialog = new InputDialog("Edit Comment", "Address:", "Text:", 
			vm.SelectedComment.Address, vm.SelectedComment.Text);
		var result = await dialog.ShowDialog<(bool, string, string)>(this);
		
		if (result.Item1) {
			vm.EditComment(vm.SelectedComment, result.Item2, result.Item3);
		}
	}

	private void DeleteComment_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is MainWindowViewModel vm && vm.SelectedComment != null) {
			vm.DeleteComment(vm.SelectedComment);
		}
	}

	private async void AddMemoryRegion_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is not MainWindowViewModel vm) return;

		var dialog = new MemoryRegionDialog();
		var result = await dialog.ShowDialog<(bool, string, string, string, string, string)>(this);
		
		if (result.Item1) {
			vm.AddMemoryRegion(result.Item2, result.Item3, result.Item4, result.Item5, result.Item6);
		}
	}

	private async void EditMemoryRegion_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is not MainWindowViewModel vm || vm.SelectedMemoryRegion == null) return;

		var region = vm.SelectedMemoryRegion;
		var dialog = new MemoryRegionDialog(region.Name, region.Type, region.Start, region.End, region.Bank);
		var result = await dialog.ShowDialog<(bool, string, string, string, string, string)>(this);
		
		if (result.Item1) {
			vm.EditMemoryRegion(region, result.Item2, result.Item3, result.Item4, result.Item5, result.Item6);
		}
	}

	private void DeleteMemoryRegion_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is MainWindowViewModel vm && vm.SelectedMemoryRegion != null) {
			vm.DeleteMemoryRegion(vm.SelectedMemoryRegion);
		}
	}

	private void DeleteCrossRef_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is MainWindowViewModel vm && vm.SelectedCrossRef != null) {
			vm.DeleteCrossRef(vm.SelectedCrossRef);
		}
	}

	private void SearchSymbols_Changed(object? sender, TextChangedEventArgs e) {
		if (DataContext is MainWindowViewModel vm && sender is TextBox textBox) {
			if (string.IsNullOrWhiteSpace(textBox.Text)) {
				vm.ClearSearch();
			} else {
				vm.ApplySearch(textBox.Text);
			}
		}
	}

	private void ClearSearch_Click(object? sender, RoutedEventArgs e) {
		if (DataContext is MainWindowViewModel vm) {
			vm.ClearSearch();
		}
	}

	private void ShowSearch_Click(object? sender, RoutedEventArgs e) {
		// Focus the search textbox in the current tab
		// Implementation depends on current tab - simplified for now
	}

	private void Exit_Click(object? sender, RoutedEventArgs e) {
		Close();
	}
}
