using System;
using System.Collections.Generic;
using System.Text;
using QrScanner.Models;

namespace QrScanner.Services;

public interface IScanPayloadStrategy
{
    bool TryParse(string raw, out ParsedQrContent content);
}

public static class ScanPayloadStrategies
{
    public static IReadOnlyList<IScanPayloadStrategy> All { get; } =
    [
        new UrlPayloadStrategy(),
        new EmailPayloadStrategy(),
        new PhonePayloadStrategy(),
        new WifiPayloadStrategy(),
        new VCardPayloadStrategy(),
        new TextPayloadStrategy()
    ];

    public static ParsedQrContent Analyze(string raw)
    {
        foreach (var strategy in All)
        {
            if (strategy.TryParse(raw, out var content))
            {
                return content;
            }
        }

        return new ParsedQrContent(ContentKind.Text, raw, null, null);
    }
}

public sealed class UrlPayloadStrategy : IScanPayloadStrategy
{
    public bool TryParse(string raw, out ParsedQrContent content)
    {
        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            content = new ParsedQrContent(ContentKind.Url, raw, raw, "Open link");
            return true;
        }

        content = default!;
        return false;
    }
}

public sealed class EmailPayloadStrategy : IScanPayloadStrategy
{
    public bool TryParse(string raw, out ParsedQrContent content)
    {
        if (raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            content = new ParsedQrContent(ContentKind.Email, raw, raw, "Send email");
            return true;
        }

        content = default!;
        return false;
    }
}

public sealed class PhonePayloadStrategy : IScanPayloadStrategy
{
    public bool TryParse(string raw, out ParsedQrContent content)
    {
        if (raw.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            content = new ParsedQrContent(ContentKind.Phone, raw, raw, "Call number");
            return true;
        }

        content = default!;
        return false;
    }
}

public sealed class WifiPayloadStrategy : IScanPayloadStrategy
{
    public bool TryParse(string raw, out ParsedQrContent content)
    {
        if (raw.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase))
        {
            content = new ParsedQrContent(ContentKind.WiFi, raw, null, null, TryParseWifi(raw));
            return true;
        }

        content = default!;
        return false;
    }

    private static WifiCredentials? TryParseWifi(string raw)
    {
        var body = raw["WIFI:".Length..];
        var fields = new Dictionary<char, string>();
        var value = new StringBuilder();
        char? key = null;

        for (var i = 0; i < body.Length; i++)
        {
            var c = body[i];

            if (c == '\\' && i + 1 < body.Length)
            {
                value.Append(body[++i]);
                continue;
            }

            if (c == ':' && key is null)
            {
                key = value.Length == 1 ? value[0] : null;
                value.Clear();
                continue;
            }

            if (c == ';')
            {
                if (key is { } k)
                {
                    fields[k] = value.ToString();
                }
                value.Clear();
                key = null;
                continue;
            }

            value.Append(c);
        }

        if (!fields.TryGetValue('S', out var ssid) || string.IsNullOrEmpty(ssid))
        {
            return null;
        }

        var securityRaw = fields.GetValueOrDefault('T', "nopass");
        var security = securityRaw.Equals("WEP", StringComparison.OrdinalIgnoreCase)
            ? WifiSecurity.Wep
            : securityRaw.Equals("nopass", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(securityRaw)
                ? WifiSecurity.None
                : WifiSecurity.Wpa;

        var password = fields.GetValueOrDefault('P', "");
        var hidden = fields.GetValueOrDefault('H', "false").Equals("true", StringComparison.OrdinalIgnoreCase);

        return new WifiCredentials(ssid, password, security, hidden);
    }
}

public sealed class VCardPayloadStrategy : IScanPayloadStrategy
{
    public bool TryParse(string raw, out ParsedQrContent content)
    {
        if (raw.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
        {
            content = new ParsedQrContent(ContentKind.VCard, raw, null, null);
            return true;
        }

        content = default!;
        return false;
    }
}

public sealed class TextPayloadStrategy : IScanPayloadStrategy
{
    public bool TryParse(string raw, out ParsedQrContent content)
    {
        content = new ParsedQrContent(ContentKind.Text, raw, null, null);
        return true;
    }
}
