using Avalonia.Controls;
using Avalonia.Interactivity;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pansy.UI;

public partial class InputDialog : Window, INotifyPropertyChanged {
	private string _title = "";
	private string _label1 = "";
	private string _label2 = "";
	private string _value1 = "";
	private string _value2 = "";

	public new string Title {
		get => _title;
		set { _title = value; OnPropertyChanged(); }
	}

	public string Label1 {
		get => _label1;
		set { _label1 = value; OnPropertyChanged(); }
	}

	public string Label2 {
		get => _label2;
		set { _label2 = value; OnPropertyChanged(); }
	}

	public string Value1 {
		get => _value1;
		set { _value1 = value; OnPropertyChanged(); }
	}

	public string Value2 {
		get => _value2;
		set { _value2 = value; OnPropertyChanged(); }
	}

	public new event PropertyChangedEventHandler? PropertyChanged;

	public InputDialog() {
		InitializeComponent();
		DataContext = this;
	}

	public InputDialog(string title, string label1, string label2, string value1 = "", string value2 = "") : this() {
		Title = title;
		Label1 = label1;
		Label2 = label2;
		Value1 = value1;
		Value2 = value2;
	}

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void OK_Click(object? sender, RoutedEventArgs e) {
		Close((true, Value1, Value2));
	}

	private void Cancel_Click(object? sender, RoutedEventArgs e) {
		Close((false, "", ""));
	}
}
