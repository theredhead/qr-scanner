import { Component } from "@angular/core";
import {
  Feature,
  FeaturesSectionComponent,
} from "./components/features-section/features-section";
import { HeroSectionComponent } from "./components/hero-section/hero-section";
import {
  Link,
  LinkSectionComponent,
} from "./components/link-section/link-section";
import {
  Screenshot,
  ScreenshotSectionComponent,
} from "./components/screenshot-section/screenshot-section";
import { SiteFooterComponent } from "./components/site-footer/site-footer";
import { SiteHeaderComponent } from "./components/site-header/site-header";

@Component({
  selector: "sz-root",
  standalone: true,
  imports: [
    FeaturesSectionComponent,
    HeroSectionComponent,
    LinkSectionComponent,
    ScreenshotSectionComponent,
    SiteFooterComponent,
    SiteHeaderComponent,
  ],
  templateUrl: "./app.html",
  styleUrl: "./app.css",
})
export class App {
  protected readonly releasesUrl =
    "https://github.com/theredhead/qr-scanner/releases";
  protected readonly repositoryUrl = "https://github.com/theredhead/qr-scanner";
  protected readonly issuesUrl =
    "https://github.com/theredhead/qr-scanner/issues";
  protected readonly licenseUrl =
    "https://github.com/theredhead/qr-scanner/blob/main/LICENSE";

  protected readonly downloads: Link[] = [
    {
      label: "Android APK available now",
      href: this.releasesUrl,
      note: "The current GitHub Release artifact is an unsigned Android APK.",
    },
    {
      label: "Desktop builds may follow",
      href: this.releasesUrl,
      note: "Windows, macOS, and Linux artifacts are planned but are not published yet.",
    },
    {
      label: "Source code",
      href: this.repositoryUrl,
      note: "Build it yourself, inspect it, fork it, or file a fix.",
    },
  ];

  protected readonly demos: Link[] = [
    {
      label: "Scanner strategy demo codes",
      href: "/demos/scanner-strategy-demo.html",
      note: "One scannable sample per supported barcode scanner strategy.",
    },
    {
      label: "Payload type demo codes",
      href: "/demos/payload-type-demo.html",
      note: "Samples for the payload actions ScannerZero recognizes after decoding.",
    },
  ];

  protected readonly features: Feature[] = [
    {
      title: "Reads more than QR codes",
      text: "ScannerZero supports common barcode formats through small, ordered scanner strategies.",
    },
    {
      title: "Scans shared photos",
      text: "Share an image with a code in it to ScannerZero and it can decode that photo directly.",
    },
    {
      title: "Understands payloads",
      text: "Links, Wi-Fi credentials, email, phone numbers, text, and vCards get purpose-built actions.",
    },
    {
      title: "Keeps history locally",
      text: "Scans and captured images stay on your device so you can revisit them without an account.",
    },
    {
      title: "No surveillance tax",
      text: "No ads, no analytics, no cloud scanner, no backend service, no upload pipeline.",
    },
  ];

  protected readonly screenshots: Screenshot[] = [
    {
      title: "Scan",
      src: "/assets/screenshots/scanning.jpg",
      alt: "ScannerZero scan screen detecting a QR code through the camera viewfinder.",
      text: "Aim the camera, keep the code inside the viewfinder, and let the enabled scanner strategies do the decoding.",
    },
    {
      title: "Result",
      src: "/assets/screenshots/result.jpg",
      alt: "ScannerZero scan result screen showing a decoded website link and action buttons.",
      text: "Decoded data is shown plainly, with purpose-built actions based on the payload ScannerZero recognized.",
    },
    {
      title: "History",
      src: "/assets/screenshots/history.jpg",
      alt: "ScannerZero history screen listing previous QR code scans.",
      text: "Previous scans stay local on the device, including the decoded value and the captured image.",
    },
    {
      title: "Settings",
      src: "/assets/screenshots/settings.jpg",
      alt: "ScannerZero settings screen with barcode strategy toggles and ordering controls.",
      text: "Barcode strategies can be enabled, disabled, and ordered to fit the kinds of codes you actually scan.",
    },
    {
      title: "Viewfinder",
      src: "/assets/screenshots/sky.jpg",
      alt: "ScannerZero scan screen with the orange-red viewfinder over an open sky.",
      text: "The scan screen stays quiet: camera, viewfinder, and the navigation you need.",
    },
  ];

  protected readonly supportLinks: Link[] = [
    {
      label: "Report an issue",
      href: this.issuesUrl,
      note: "Use GitHub Issues for bugs, platform quirks, and feature requests.",
    },
    {
      label: "Read the README",
      href: `${this.repositoryUrl}#readme`,
      note: "Project status, supported platforms, privacy notes, and build instructions.",
    },
    {
      label: "Check releases",
      href: this.releasesUrl,
      note: "Downloadable builds live here until there is a store listing.",
    },
  ];
}
