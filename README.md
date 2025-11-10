# DuckBot (C# + JavaScript)

Native WPF GUI with a JavaScript engine (Jint) for game automation.
- Username/password login (no license keys)
- Modular games with remote updates
- Popup & logic solvers
- Quick Launch for single-instance tasks

## Build
- .NET 8 SDK
- Windows 10+
- Dependencies: Jint, OpenCvSharp4, Tesseract, Microsoft.Data.Sqlite

## Run (dev)
- Set DuckBot.GUI as startup
- In Login screen, enable "Offline Mode" to skip auth while wiring backend

## Folders
- DuckBot.Core: ADB, CV, OCR, Solvers, RunLoop
- DuckBot.Scripting: JS engine + bridges
- DuckBot.API: REST client for auth & updates
- Games: self-contained modules (scripts/images/solvers)

## Quick Launch
Dashboard → Quick Launch → pick instance + script → Run

## Modules & Updates
Each game has `update.json` with version and files.
App checks server, downloads diffs, and replaces module files.
