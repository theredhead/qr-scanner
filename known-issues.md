# Known Issues

## SQLite dependency advisory

The project currently uses `SQLitePCLRaw.bundle_green` 2.1.11, which brings in native SQLite packages listed in [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q) and [CVE-2025-6965](https://nvd.nist.gov/vuln/detail/CVE-2025-6965).

The issue concerns malformed SQL with more aggregate terms than available result columns, which can lead to memory corruption in affected SQLite versions. The app uses a local-only database and does not expose arbitrary SQL input or network database access, so this is not currently an active QR-scanning attack path.

The advisory currently lists no patched `SQLitePCLRaw` NuGet release. The warning is being tracked here and intentionally left unresolved until a package release containing SQLite 3.50.2 or newer is available.
