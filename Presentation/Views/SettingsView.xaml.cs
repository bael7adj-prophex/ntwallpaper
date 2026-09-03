using System.Windows;
using System.Windows.Controls;
using NTWallpaper.Presentation.ViewModels;

namespace NTWallpaper.Presentation.Views;

public partial class SettingsView : System.Windows.Controls.UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, __) =>
        {
            if (DataContext is SettingsViewModel vm && !string.IsNullOrEmpty(vm.ApiKey))
                ApiKeyBox.Password = vm.ApiKey;
            ApiKeyBox.PasswordChanged += (_, __) =>
            {
                if (DataContext is SettingsViewModel v) v.ApiKey = ApiKeyBox.Password;
            };
        };
    }
}