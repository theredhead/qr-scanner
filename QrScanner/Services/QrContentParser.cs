using System;
using System.Collections.Generic;
using System.Text;
using QrScanner.Models;

namespace QrScanner.Services;

/// <summary>Interprets the raw text carried by a scanned QR code into a displayable, actionable form.</summary>
public static class QrContentParser
{
    public static ParsedQrContent Parse(string raw)
    {
        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return new ParsedQrContent(ContentKind.Url, raw, raw, "Open link");
        }

        if (raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedQrContent(ContentKind.Email, raw, raw, "Send email");
        }

        if (raw.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedQrContent(ContentKind.Phone, raw, raw, "Call number");
        }

        if (raw.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase))
        {
            var wifi = TryParseWifi(raw);
            return new ParsedQrContent(ContentKind.WiFi, raw, null, null, wifi);
        }

        if (raw.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedQrContent(ContentKind.VCard, raw, null, null);
        }

        return new ParsedQrContent(ContentKind.Text, raw, null, null);
    }

    /// <summary>
    /// Parses the standard Wi-Fi QR payload: "WIFI:T:&lt;WPA|WEP|nopass&gt;;S:&lt;ssid&gt;;P:&lt;password&gt;;H:&lt;true|false&gt;;;"
    /// Field values may escape \, ; , and : with a backslash.
    /// </summary>
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
                : WifiSecurity.Wpa; // covers WPA/WPA2/WPA3

        var password = fields.GetValueOrDefault('P', "");
        var hidden = fields.GetValueOrDefault('H', "false").Equals("true", StringComparison.OrdinalIgnoreCase);

        return new WifiCredentials(ssid, password, security, hidden);
    }
}
