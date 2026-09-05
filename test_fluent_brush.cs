using Avalonia;
using Avalonia.Themes.Fluent;
using Avalonia.Media;
using System;

var app = AppBuilder.Configure<Application>().UsePlatformDetect().SetupWithoutStarting();
var theme = new FluentTheme();
app.Instance.Styles.Add(theme);

foreach (var key in new[] { "SystemRegionBrush", "SystemControlBackgroundAltHighBrush", "SystemAltHighColor", "ThemeBackgroundBrush" })
{
    if (app.Instance.TryFindResource(key, out var res))
        Console.WriteLine($"{key} -> {res.GetType().Name} ({res})");
    else
        Console.WriteLine($"{key} -> NOT FOUND");
}
