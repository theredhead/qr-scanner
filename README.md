# QR Scanner

A simple, private, cross-platform QR code scanner.

I wanted a QR scanner that just scans QR codes without ads, subscriptions, accounts, tracking, or sending anything to somebody else's server.

So I made one.

## What it does

QR Scanner scans and decodes QR codes and shows you the data they actually contain.

Rather than assuming every QR code is a web link, it recognizes common types of data and provides useful actions for them.

Depending on the contents of the QR code and the platform you're using, you can:

* Open web links
* Connect to Wi-Fi networks
* Copy Wi-Fi network names and passwords
* Copy the complete decoded data
* Use other context-specific actions appropriate to the decoded content
* Select and copy any part of the decoded data using the normal OS controls

QR Scanner also keeps a local history of the codes you've scanned, including the image they were scanned from, so you can go back to them later.

## Privacy

**Everything happens locally on your device.**

QR Scanner:

* has no accounts
* has no analytics
* has no tracking
* has no advertising
* has no backend service
* does not upload scanned QR codes
* does not upload captured images
* does not send your scan history anywhere

Your scan history and captured images remain on your device.

The application is open source, so you don't have to take my word for it.

## Platforms

QR Scanner is built with Avalonia and currently targets:

* Android (fairly well tested)
* iOS (not tested)
* Desktop (somewhat tested)

The same application and scanning logic is shared between platforms, with platform-specific implementations where the operating system requires them.

Some actions depend on what a platform permits. For example, Android can connect directly to a Wi-Fi network described by a QR code. iOS does not provide the same level of automation, so QR Scanner instead lets you copy the SSID and password.

The QR code itself is decoded the same way either way.

## Scan

The Scan screen keeps things deliberately simple.

The camera viewfinder occupies the upper part of the screen. When a QR code is detected, its decoded contents are displayed below it.

The complete value can always be copied, while additional actions are offered when QR Scanner recognizes something useful it can do with the data.

## History

Scanned QR codes are stored locally and can be revisited from the History tab.

A history entry retains both the decoded data and the image from which it was scanned.

## Why another QR scanner?

Because I needed to scan a QR code.

The QR scanner built into my phone's camera stopped working, and after trying several alternatives I got tired of scanners filled with advertising and unnecessary nonsense.

A few hours later, this existed.

It does considerably more than I originally needed, but it still follows the original requirement:

**Scan QR codes. Show me what's in them. Don't be annoying.**

## Building

The repository contains a shared Avalonia application and platform projects for Android, iOS, and desktop.

```text
QrScanner/
QrScanner.Android/
QrScanner.iOS/
QrScanner.Desktop/
```

Open `QrScanner.slnx` with a recent .NET SDK and an IDE with Avalonia/.NET support.

Platform-specific development requires the corresponding .NET workloads and SDKs.

## Status

This project is young and was originally built to solve a personal annoyance.

The functional application works, but the UI and distribution packaging are still being polished before publishing it through the respective app stores.

Contributions, bug reports, and sensible suggestions are welcome.

## License

QR Scanner is free and open-source software.

See the repository license for the exact licensing terms.
