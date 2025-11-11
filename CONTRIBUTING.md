🦆 DuckBot Vision & Architecture
🧭 Overview

DuckBot is a modular automation manager and scripting framework built in C# / WPF (.NET 8).
It controls multiple LDPlayer instances and executes game-specific scripts such as West Game automation.

DuckBot provides a unified, modern interface for:

Managing emulators and accounts

Running automation scripts (via JavaScript)

Capturing screenshots, performing OCR, and image recognition

Debugging and monitoring live bot behavior

Handling updates, backups, and configuration storage

🧱 Project Structure
Project	Purpose
DuckBot.GUI	WPF front-end (UI, themes, view-models, user interaction)
DuckBot.Core	Core logic & services (ADB, OCR, image recognition, instance management, script execution)
DuckBot.Data	Data persistence — configs, accounts, scripts, settings, backups
DuckBot.Scripting	JavaScript engine layer using Jint
DuckBot.API (future)	Optional online sync / cloud update layer
🧩 Core Modules
1. My Bots

Main manager — controls all instances, accounts, and scripts.

Features

List & configure multiple bots

Each bot:

Targets one LDPlayer instance

Can rotate through multiple accounts

Runs one or more scripts sequentially

Prevents duplicate emulator assignment

Saves configuration in /data/bots/[botname].json

2. Live Runner

Single-instance test view for debugging.

Features

Displays script execution logs in real time

Shows OCR/image recognition results

Lets user pause, resume, stop, or restart

Used for verifying scripts before scaling to multi-instance operation

3. Script Builder

Visual editor for creating automation logic.

Features

Step types: TAP, WAIT, INPUT, IF IMAGE EXISTS, etc.

Parameterized scripting (e.g., coordinates, image, delay)

Crop & capture reference images

Auto-save/load JSON scripts per game

Templates stored in /data/scripts/[game]/[script].json

4. Settings & Tools

Global configuration hub.

Tabs

General – emulator settings, theme, cache

Advanced – cooldowns, OCR tuning

Solvers – popup & logic handlers

Repository – local script library

Backups – export/import user data

5. Updates

Reads a remote JSON manifest (UpdateManifest.cs) to show:

Latest version

Changelog / release notes

Download link

Supports automatic or manual update checks.

⚙️ Backend Systems
🧠 Emulator Management

Detects and manages both LDPlayer 4 (used as “5”) and LDPlayer 9.

Default paths

C:\LDPlayer4.0\LDPlayer
C:\LDPlayer\LDPlayer9


Uses ldconsole.exe and adb.exe to:

List instances (ldconsole list2)

Start / stop / focus instances

Execute ADB commands (tap, swipe, input)

Capture screenshots

Manage multi-account rotations

Each instance is represented by an EmulatorInstance.
The EmulatorManager ensures only one bot uses a given instance at a time.

📬 Account Management

Stored in /data/accounts/accounts.json.

Each account entry contains:

Email / login credentials

Assigned emulator

Game profile info

Last activity timestamp

Used by My Bots and Mail Login interfaces.

🧰 Script Execution Engine

Powered by Jint (embedded JavaScript engine).

Provides a sandboxed API for:

ADB commands (tap, swipe, input)

OCR & image recognition

Conditional logic (if image exists)

Timing (wait, delay)

Multiple bots can execute scripts concurrently in isolated sandboxes.

🖼️ OCR & Image Recognition

Uses OpenCV + Tesseract to:

Detect template images

Recognize screen text

Extract sub-regions (cropping)

Trigger script conditions based on visual matches

🎨 UI Guidelines
Element	Style
Background	#1E1E1E
Panels	#252525
Text (primary)	#E0E0E0
Accent / Buttons	#3A86FF
Borders	#404040
Font	Segoe UI / Consolas
Padding	6–10 px
Corner Radius	4–6 px
Hover State	lighten (#2F2F2F)
Pressed State	darken (#191919)

All dropdowns, textboxes, and combo boxes must use dark backgrounds and light text for contrast.

🧩 Developer Standards

Keep modularity intact – maintain GUI, Core, Data, Scripting boundaries.

Do not overwrite working logic – extend or polish only.

Target: net8.0-windows.

Verify build: dotnet build DuckBot.sln before commit.

Apply global theme consistency.

Store all user data under /data.

Ensure one-click usability.

Prepare for future cross-emulator support.

🚀 Future Roadmap

Discord webhook control

Remote script library fetch

User authentication system

Automated image calibration

Local & cloud backups

Plugin architecture for new games

✅ Summary

DuckBot aims to be:

A stable, polished, modular automation manager with a professional dark UI and scalable backend.

All contributions — including Codex commits — must follow these rules:

Keep it clean

Keep it stable

Keep it beautiful