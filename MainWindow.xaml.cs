using System.Windows;
using System.Windows.Controls.Primitives;
using NTWallpaper.Presentation.ViewModels;

namespace NTWallpaper;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.Tag is string page)
        {
            _vm.Navigate(page);
            if (tb.Parent is System.Windows.Controls.Panel panel)
                foreach (var child in panel.Children)
                    if (child is ToggleButton other && other != tb) other.IsChecked = false;
            tb.IsChecked = true;
        }
    }
}