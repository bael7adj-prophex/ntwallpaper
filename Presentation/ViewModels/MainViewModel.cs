namespace NTWallpaper.Presentation.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Orchestration;

/// <summary>Top-level shell. Holds the currently-displayed child view-model.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly DashboardViewModel _dashboard;
    private readonly GroupsViewModel _groups;
    private readonly HistoryViewModel _history;
    private readonly SearchViewModel _search;
    private readonly SettingsViewModel _settings;

    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private string _currentTitle = "Dashboard";

    public WallpaperOrchestrator Orchestrator { get; }
    public IMonitorService MonitorService { get; }

    public MainViewModel(
        WallpaperOrchestrator orchestrator,
        IMonitorService monitorService,
        DashboardViewModel dashboard,
        GroupsViewModel groups,
        HistoryViewModel history,
        SearchViewModel search,
        SettingsViewModel settings)
    {
        Orchestrator = orchestrator;
        MonitorService = monitorService;
        _dashboard = dashboard;
        _groups = groups;
        _history = history;
        _search = search;
        _settings = settings;

        Navigate("Dashboard");
    }

    public void Navigate(string page)
    {
        CurrentTitle = page switch
        {
            "Groups" => "Groups",
            "History" => "History",
            "Search" => "Search",
            "Settings" => "Settings",
            _ => "Dashboard"
        };

        CurrentView = page switch
        {
            "Groups" => _groups,
            "History" => _history,
            "Search" => _search,
            "Settings" => _settings,
            _ => _dashboard
        };
    }
}