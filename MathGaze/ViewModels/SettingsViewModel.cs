using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathGaze.Properties;
using System.Windows;

namespace MathGaze.ViewModels;

/// <summary>
/// Drives the in-window settings panel (D-04, D-05, D-06, D-07).
/// IsDarkMode state is the source of truth; ApplyTheme delegates to App.
/// Theme preference is persisted via UserPreferences (JSON in %APPDATA%\MathGaze\).
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private bool _isSettingsPanelOpen;
    [ObservableProperty] private bool _isProtractorSmall;
    [ObservableProperty] private bool _isProtractorMedium;
    [ObservableProperty] private bool _isProtractorLarge;

    public SettingsViewModel()
    {
        // Initialise from the persisted preference so the toggle shows the correct state
        _isDarkMode = UserPreferences.Theme == "Dark";
        SyncProtractorSize(UserPreferences.ProtractorSize);
    }

    [RelayCommand]
    private void OpenSettings() => IsSettingsPanelOpen = true;

    [RelayCommand]
    private void CloseSettings() => IsSettingsPanelOpen = false;

    /// <summary>
    /// Called from the Light/Dark toggle buttons in the settings panel.
    /// Parameter: "Light" or "Dark".
    /// </summary>
    [RelayCommand]
    private void SetTheme(string theme)
    {
        bool isDark = theme == "Dark";
        IsDarkMode = isDark;
        ((App)Application.Current).ApplyTheme(isDark);
        UserPreferences.Theme = theme;
        UserPreferences.Save();
    }

    /// <summary>
    /// Called from Small/Medium/Large buttons in the settings panel.
    /// Parameter: "Small" | "Medium" | "Large".
    /// Affects protractors placed after this call; existing ones are unchanged.
    /// </summary>
    [RelayCommand]
    private void SetProtractorSize(string size)
    {
        SyncProtractorSize(size);
        UserPreferences.ProtractorSize = size;
        UserPreferences.Save();
    }

    private void SyncProtractorSize(string size)
    {
        IsProtractorSmall  = size == "Small";
        IsProtractorMedium = size == "Medium";
        IsProtractorLarge  = size == "Large";
    }
}
