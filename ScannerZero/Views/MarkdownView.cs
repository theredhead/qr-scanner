using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace ScannerZero.Views;

public sealed class MarkdownView : UserControl
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownView, string?>(nameof(Markdown));

    private static readonly FontFamily CodeFont = new("Consolas, Menlo, Monaco, monospace");
    private readonly StackPanel _panel = new() { Spacing = 8 };

    public MarkdownView()
    {
        Content = _panel;
        ActualThemeVariantChanged += (_, _) => Render(Markdown);
    }

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MarkdownProperty)
        {
            Render(Markdown);
        }
    }

    private void Render(string? markdown)
    {
        _panel.Children.Clear();

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return;
        }

        var lines = markdown.ReplaceLineEndings("\n").Split('\n');
        var paragraph = new List<string>();
        var code = new StringBuilder();
        var inCodeBlock = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeBlock)
                {
                    AddCodeBlock(code.ToString().TrimEnd('\n'));
                    code.Clear();
                    inCodeBlock = false;
                }
                else
                {
                    FlushParagraph(paragraph);
                    inCodeBlock = true;
                }

                continue;
            }

            if (inCodeBlock)
            {
                code.AppendLine(line);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(paragraph);
                continue;
            }

            if (TryParseHeading(line, out var level, out var heading))
            {
                FlushParagraph(paragraph);
                AddHeading(heading, level);
                continue;
            }

            if (TryParseBullet(line, out var bullet))
            {
                FlushParagraph(paragraph);
                AddBullet(bullet);
                continue;
            }

            paragraph.Add(line.Trim());
        }

        if (inCodeBlock && code.Length > 0)
        {
            AddCodeBlock(code.ToString().TrimEnd('\n'));
        }

        FlushParagraph(paragraph);
    }

    private static bool TryParseHeading(string line, out int level, out string heading)
    {
        level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        if (level is < 1 or > 6 || level >= line.Length || line[level] != ' ')
        {
            heading = string.Empty;
            return false;
        }

        heading = line[(level + 1)..].Trim();
        return heading.Length > 0;
    }

    private static bool TryParseBullet(string line, out string bullet)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length > 2 && (trimmed[0] == '*' || trimmed[0] == '-') && trimmed[1] == ' ')
        {
            bullet = trimmed[2..].Trim();
            return true;
        }

        bullet = string.Empty;
        return false;
    }

    private void FlushParagraph(List<string> paragraph)
    {
        if (paragraph.Count == 0)
        {
            return;
        }

        AddInlineText(string.Join(' ', paragraph), 14, FontWeight.Normal, new Thickness(0, 0, 0, 2));
        paragraph.Clear();
    }

    private void AddHeading(string text, int level)
    {
        var fontSize = level switch
        {
            1 => 24,
            2 => 19,
            _ => 16
        };

        var margin = level == 1
            ? new Thickness(0, 0, 0, 8)
            : new Thickness(0, 10, 0, 2);

        AddInlineText(text, fontSize, FontWeight.Bold, margin);
    }

    private void AddBullet(string text)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(0, 0, 0, 2)
        };

        var marker = new TextBlock
        {
            Text = "-",
            FontSize = 14,
            Margin = new Thickness(2, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        var content = CreateInlineTextBlock(text, 14, FontWeight.Normal);
        Grid.SetColumn(content, 1);

        grid.Children.Add(marker);
        grid.Children.Add(content);
        _panel.Children.Add(grid);
    }

    private void AddCodeBlock(string text)
    {
        _panel.Children.Add(new Border
        {
            Background = CodeBackground,
            BorderBrush = CodeBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8),
            Margin = new Thickness(0, 2, 0, 4),
            Child = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = CodeFont,
                FontSize = 13,
                Foreground = CodeForeground,
                LineHeight = 17
            }
        });
    }

    private void AddInlineText(string text, double fontSize, FontWeight fontWeight, Thickness margin)
    {
        var textBlock = CreateInlineTextBlock(text, fontSize, fontWeight);
        textBlock.Margin = margin;
        _panel.Children.Add(textBlock);
    }

    private TextBlock CreateInlineTextBlock(string text, double fontSize, FontWeight fontWeight)
    {
        var textBlock = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = fontWeight,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = fontSize * 1.35
        };

        AddInlines(textBlock.Inlines!, text, IsDarkTheme);
        return textBlock;
    }

    private static void AddInlines(InlineCollection inlines, string text, bool isDarkTheme)
    {
        var index = 0;
        while (index < text.Length)
        {
            var codeStart = text.IndexOf('`', index);
            var boldStart = text.IndexOf("**", index, StringComparison.Ordinal);
            var next = MinPositive(codeStart, boldStart);

            if (next < 0)
            {
                inlines.Add(text[index..]);
                return;
            }

            if (next > index)
            {
                inlines.Add(text[index..next]);
            }

            if (next == codeStart)
            {
                var end = text.IndexOf('`', codeStart + 1);
                if (end < 0)
                {
                    inlines.Add(text[codeStart..]);
                    return;
                }

                inlines.Add(new Run(text[(codeStart + 1)..end])
                {
                    FontFamily = CodeFont,
                    Background = CreateCodeBackground(isDarkTheme),
                    Foreground = CreateCodeForeground(isDarkTheme)
                });
                index = end + 1;
                continue;
            }

            var boldEnd = text.IndexOf("**", boldStart + 2, StringComparison.Ordinal);
            if (boldEnd < 0)
            {
                inlines.Add(text[boldStart..]);
                return;
            }

            var bold = new Bold();
            bold.Inlines!.Add(text[(boldStart + 2)..boldEnd]);
            inlines.Add(bold);
            index = boldEnd + 2;
        }
    }

    private static int MinPositive(int first, int second) =>
        first < 0 ? second :
        second < 0 ? first :
        Math.Min(first, second);

    private bool IsDarkTheme => ActualThemeVariant == ThemeVariant.Dark;

    private IBrush CodeBackground => CreateCodeBackground(IsDarkTheme);

    private IBrush CodeForeground => CreateCodeForeground(IsDarkTheme);

    private IBrush CodeBorderBrush => IsDarkTheme
        ? new SolidColorBrush(Color.Parse("#3A3A3A"))
        : new SolidColorBrush(Color.Parse("#D8DEE8"));

    private static IBrush CreateCodeBackground(bool isDarkTheme) =>
        new SolidColorBrush(Color.Parse(isDarkTheme ? "#1F1F1F" : "#EEF2F7"));

    private static IBrush CreateCodeForeground(bool isDarkTheme) =>
        new SolidColorBrush(Color.Parse(isDarkTheme ? "#F4F6F8" : "#17202A"));
}
