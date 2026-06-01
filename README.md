# ðŸ”’ BrowserGate

> Lock Microsoft Edge (and optionally Chrome) behind a master password â€” without locking your whole PC.

Leave your desk. Anyone can still use the computer. But the moment they click Edge, they need **your** password. Sessions stay logged in â€” no need to re-enter Gmail / social passwords every time.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

---

## âœ¨ Features

- ðŸ” **System-wide lock** â€” every path to Edge (taskbar, Start, links from other apps) goes through the password prompt
- ðŸŒ **Optional Chrome lock** â€” same protection for Google Chrome
- ðŸ“§ **Forgot-password reset** â€” 6-digit code emailed to your Gmail (Argon2id + Gmail App Password)
- â±ï¸ **Auto-relock** â€” re-arm the lock after N minutes of Edge being closed
- ðŸŽ¯ **Tray icon** â€” status, lock-now, settings, quit
- ðŸ§¹ **Clean uninstaller** â€” one click removes registry keys + config
- ðŸ“¦ **Single-file .exe** â€” no installer, no .NET runtime needed
- ðŸŽ¨ **Modern dark UI** â€” borderless, rounded, drop-shadow

## ðŸ”’ Security stack

| Layer | Tech |
|---|---|
| Password hash | **Argon2id** (64 MB, 3 iterations, 4 lanes) |
| Config storage | **Windows DPAPI** â€” bound to your Windows user account |
| Enforcement | **IFEO Debugger** registry hijack (HKLM) |
| Reset | **Gmail SMTP + 6-digit OTP** (10 min expiry, app-password based) |
| Brute-force | 5-attempt cap per prompt session |

## ðŸš€ Quick start

1. Download `BrowserGate.exe` from [Releases](../../releases).
2. Double-click it. The Setup wizard opens.
3. Enter your **master password**, **Gmail**, and a **Gmail App Password** ([generate one here](https://myaccount.google.com/apppasswords) â€” *not* your normal Gmail password).
4. Tick **Lock Edge** (and optionally **Lock Chrome**) â†’ click **Save**.
5. Approve the one-time **UAC admin prompt**.
6. Done. Open Edge â†’ password prompt â†’ unlock â†’ real Edge launches with all your logged-in sessions intact.

## ðŸ” Daily flow

| Action | How |
|---|---|
| Open Edge | Click Edge â†’ enter password â†’ done |
| Forgot password | Click **Forgot password** in prompt â†’ OTP sent to Gmail â†’ set new password |
| Change settings | Run `BrowserGate.exe` or click tray icon â†’ **Settings** |
| Disable lock temporarily | Tray â†’ **Unlock browsers** |
| Uninstall completely | Settings â†’ **Uninstall BrowserGate** |

## ðŸ›¡ï¸ Threat model â€” what it does and doesn't protect

**Protects against:**
- A coworker using your unlocked PC clicking Edge to check your Gmail / socials
- Edge launched from taskbar, Start, pinned tile, or links in other apps
- Reading the BrowserGate config from another Windows account (DPAPI-bound)

**Does NOT protect against:**
- Physical access + your Windows session unlocked â†’ use `Win + L` for that
- An attacker who copies `%LOCALAPPDATA%\Microsoft\Edge\User Data` to another machine
- A determined attacker exactly timing a launch during the ~1.5 s mutex window

For everyday "I'm stepping away from my desk" â€” this is exactly the right tool.

## ðŸ› ï¸ Build from source

Requires .NET 10 SDK.

```powershell
git clone https://github.com/<you>/BrowserGate.git
cd BrowserGate
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Output: `bin\Release\net10.0-windows\win-x64\publish\BrowserGate.exe`

## ðŸ“‚ Project layout

```
BrowserGate/
â”œâ”€â”€ App.xaml / App.xaml.cs         # entry point + CLI arg routing
â”œâ”€â”€ Views/                          # WPF windows
â”‚   â”œâ”€â”€ SetupWindow                 # first-run wizard
â”‚   â”œâ”€â”€ PasswordPromptWindow        # lock-screen
â”‚   â”œâ”€â”€ ResetWindow                 # forgot-password OTP flow
â”‚   â””â”€â”€ SettingsWindow              # main control panel
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ ConfigStore                 # DPAPI + Argon2id
â”‚   â”œâ”€â”€ IFEORegistrar               # registry hijack install/uninstall
â”‚   â”œâ”€â”€ EdgeLauncher                # safe Edge launch through IFEO
â”‚   â”œâ”€â”€ MailService                 # Gmail SMTP OTP
â”‚   â””â”€â”€ TrayIcon                    # NotifyIcon
â””â”€â”€ BrowserGate.csproj
```

## ðŸ“ License

MIT â€” see [LICENSE](LICENSE).

## ðŸ™‹ Issues / Ideas

PRs welcome. Open an issue for feature requests (Firefox lock, biometric unlock, etc.).
