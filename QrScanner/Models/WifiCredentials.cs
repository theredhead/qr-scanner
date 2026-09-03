namespace QrScanner.Models;

public sealed record WifiCredentials(string Ssid, string Password, WifiSecurity Security, bool Hidden);
