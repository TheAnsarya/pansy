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

	private void Exit_Click(object? sender, RoutedEventArgs e) {
		Close();
	}
}
