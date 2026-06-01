using System.IO;
using System.Text.Json;

namespace MathGaze.Properties;

/// <summary>
/// Lightweight user preferences persistence using a JSON file in %APPDATA%\MathGaze\.
/// Replaces ApplicationSettingsBase (not available in .NET 9 SDK-style WPF without extra NuGet).
/// Mirrors the Settings.settings schema: Theme = "Light" | "Dark".
/// </summary>
public static class UserPreferences
{
    private static readonly string _prefsDir  = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MathGaze");
    private static readonly string _prefsFile = Path.Combine(_prefsDir, "preferences.json");

    private static PrefsData _data = new();

    static UserPreferences()
    {
        Load();
    }

    public static string Theme
    {
        get => _data.Theme;
        set => _data.Theme = value;
    }

    public static string ProtractorSize
    {
        get => _data.ProtractorSize;
        set => _data.ProtractorSize = value;
    }

    /// <summary>Maps ProtractorSize preference to a radius in PDF points.</summary>
    public static double ProtractorSizeRadiusPt => ProtractorSize switch
    {
        "Small" => 80.0,
        "Large" => 200.0,
        _       => 144.0,  // "Medium" and legacy default
    };

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(_prefsDir);
            string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_prefsFile, json);
        }
        catch
        {
            // Swallow — failure to save preferences is non-fatal for a gaze tool
        }
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(_prefsFile))
            {
                string json = File.ReadAllText(_prefsFile);
                _data = JsonSerializer.Deserialize<PrefsData>(json) ?? new PrefsData();
            }
        }
        catch
        {
            _data = new PrefsData();
        }
    }

    private sealed class PrefsData
    {
        public string Theme          { get; set; } = "Light";
        public string ProtractorSize { get; set; } = "Medium";
    }
}
