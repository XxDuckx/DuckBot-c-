# 🦆 DuckBot

**DuckBot** is a next-generation automation manager built in **C# (WPF)** for controlling and automating **LDPlayer** instances.  
It’s the modern evolution of *WestBot* and *LSSBot*, designed for modularity, script flexibility, and cross-game automation.

---

## 🧩 Features

### ⚙️ Core Modules
| Tab | Purpose |
|------|----------|
| **My Bots** | Create and manage bot profiles per game. Assign LDPlayer instances, switch accounts, and queue scripts. |
| **Logs** | Real-time logging of events, actions, errors, and solver activity. |
| **Script Builder** | Visual drag-and-drop editor for JSON→JS scripting with coordinate picker, image cropper, and variable inputs. |
| **Live Runner** | Execute and monitor scripts directly from the GUI. |
| **Settings** | Manage emulator paths, preferences, solvers, repository access, and backup options. |
| **Updates** | Check, download, and install updates directly from a hosted manifest. |

---

## 🧠 Tech Stack
- **Language:** C# (.NET 8)
- **Framework:** WPF (Windows Presentation Foundation)
- **Storage:** JSON (for scripts, settings, templates)
- **Automation Layer:** ADB (for LDPlayer interaction)
- **Future Integration:** OpenCV + Tesseract for OCR & image matching
- **Community Sync:** GitHub-based repository for shared templates

---

## 🗂️ Folder Structure
DuckBot/
├── DuckBot.Core/
│ ├── Models/
│ ├── Scripts/
│ ├── Services/
├── DuckBot.GUI/
│ ├── Views/
│ ├── Themes/
│ └── MainWindow.xaml
├── data/
│ └── config.json
└── Games/
└── WestGame/
├── scripts/
├── images/
└── templates/

---

## 🧰 Development Notes
- All user settings are persisted via `SettingsManager` → `data/config.json`
- Scripts and image crops are stored per-game under `/Games/{game}/`
- Cropped images automatically save to `/Games/{game}/images/`
- Full modular WPF tab layout for future features (AI scripting assistant, Discord control, etc.)
- Dark mode defined in `/Themes/Styles.xaml` for Codex to refine

---

🧩 License

Personal development project — not for resale or redistribution.
© 2025 Brandon Walker (XxDuckx)
