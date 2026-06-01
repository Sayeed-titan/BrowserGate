# 🔒 EdgeLocker

> Lock Microsoft Edge (and optionally Chrome) behind a master password — without locking your whole PC.

Leave your desk. Anyone can still use the computer. But the moment they click Edge, they need **your** password. Sessions stay logged in — no need to re-enter Gmail / social passwords every time.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

---

## ✨ Features

- 🔐 **System-wide lock** — every path to Edge (taskbar, Start, links from other apps) goes through the password prompt
- 🌐 **Optional Chrome lock** — same protection for Google Chrome
- 📧 **Forgot-password reset** — 6-digit code emailed to your Gmail (Argon2id + Gmail App Password)
- ⏱️ **Auto-relock** — re-arm the lock after N minutes of Edge being closed
- 🎯 **Tray icon** — status, lock-now, settings, quit
- 🧹 **Clean uninstaller** — one click removes registry keys + config
- 📦 **Single-file .exe** — no installer, no .NET runtime needed
- 🎨 **Modern dark UI** — borderless, rounded, drop-shadow

## 🔒 Security stack

| Layer | Tech |
|---|---|
| Password hash | **Argon2id** (64 MB, 3 iterations, 4 lanes) |
| Config storage | **Windows DPAPI** — bound to your Windows user account |
| Enforcement | **IFEO Debugger** registry hijack (HKLM) |
| Reset | **Gmail SMTP + 6-digit OTP** (10 min expiry, app-password based) |
| Brute-force | 5-attempt cap per prompt session |

## 🚀 Quick start

1. Download `EdgeLocker.exe` from [Releases](../../releases).
2. Double-click it. The Setup wizard opens.
3. Enter your **master password**, **Gmail**, and a **Gmail App Password** ([generate one here](https://myaccount.google.com/apppasswords) — *not* your normal Gmail password).
4. Tick **Lock Edge** (and optionally **Lock Chrome**) → click **Save**.
5. Approve the one-time **UAC admin prompt**.
6. Done. Open Edge → password prompt → unlock → real Edge launches with all your logged-in sessions intact.

## 🔁 Daily flow

| Action | How |
|---|---|
| Open Edge | Click Edge → enter password → done |
| Forgot password | Click **Forgot password** in prompt → OTP sent to Gmail → set new password |
| Change settings | Run `EdgeLocker.exe` or click tray icon → **Settings** |
| Disable lock temporarily | Tray → **Unlock browsers** |
| Uninstall completely | Settings → **Uninstall EdgeLocker** |

## 🛡️ Threat model — what it does and doesn't protect

**Protects against:**
- A coworker using your unlocked PC clicking Edge to check your Gmail / socials
- Edge launched from taskbar, Start, pinned tile, or links in other apps
- Reading the EdgeLocker config from another Windows account (DPAPI-bound)

**Does NOT protect against:**
- Physical access + your Windows session unlocked → use `Win + L` for that
- An attacker who copies `%LOCALAPPDATA%\Microsoft\Edge\User Data` to another machine
- A determined attacker exactly timing a launch during the ~1.5 s mutex window

For everyday "I'm stepping away from my desk" — this is exactly the right tool.

## 🛠️ Build from source

Requires .NET 10 SDK.

```powershell
git clone https://github.com/<you>/EdgeLocker.git
cd EdgeLocker
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Output: `bin\Release\net10.0-windows\win-x64\publish\EdgeLocker.exe`

## 📂 Project layout

```
EdgeLocker/
├── App.xaml / App.xaml.cs         # entry point + CLI arg routing
├── Views/                          # WPF windows
│   ├── SetupWindow                 # first-run wizard
│   ├── PasswordPromptWindow        # lock-screen
│   ├── ResetWindow                 # forgot-password OTP flow
│   └── SettingsWindow              # main control panel
├── Services/
│   ├── ConfigStore                 # DPAPI + Argon2id
│   ├── IFEORegistrar               # registry hijack install/uninstall
│   ├── EdgeLauncher                # safe Edge launch through IFEO
│   ├── MailService                 # Gmail SMTP OTP
│   └── TrayIcon                    # NotifyIcon
└── EdgeLocker.csproj
```

## 📝 License

MIT — see [LICENSE](LICENSE).

## 🙋 Issues / Ideas

PRs welcome. Open an issue for feature requests (Firefox lock, biometric unlock, etc.).
