using System.Windows;

namespace BrowserGate.Services;

/// <summary>
/// Swaps the theme ResourceDictionary at the application level. All brushes are
/// referenced through DynamicResource so the swap repaints every open window
/// without a restart.
/// </summary>
public static class ThemeManager
{
    public const string Dark  = "Dark";
    public const string Light = "Light";

    public static string Current { get; private set; } = Dark;

    public static void Apply(string theme)
    {
        if (string.IsNullOrWhiteSpace(theme)) theme = Dark;
        theme = theme.Equals(Light, StringComparison.OrdinalIgnoreCase) ? Light : Dark;

        var uri = new Uri($"pack://application:,,,/Themes/{theme}.xaml", UriKind.Absolute);
        var newDict = new ResourceDictionary { Source = uri };

        var dicts = Application.Current.Resources.MergedDictionaries;
        // Remove any previously loaded theme dictionary.
        for (int i = dicts.Count - 1; i >= 0; i--)
        {
            var src = dicts[i].Source?.OriginalString ?? "";
            if (src.Contains("/Themes/Dark.xaml") || src.Contains("/Themes/Light.xaml"))
                dicts.RemoveAt(i);
        }
        // Theme dict must come first so the App.xaml style dict can reference its keys.
        dicts.Insert(0, newDict);

        Current = theme;
    }

    public static string Toggle()
    {
        var next = Current == Dark ? Light : Dark;
        Apply(next);
        return next;
    }
}
