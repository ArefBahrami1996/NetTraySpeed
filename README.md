# NetSpeedTray

A sleek, lightweight, dark-themed hardware and network monitor for Windows 10 & 11. Designed to dock flush-left against the system tray or float anywhere on your desktop.

![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20(x64)-blue)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0%20WinForms-purple)
![License](https://img.shields.io/badge/License-MIT-green)

---

## ✨ Features

- **⚡ Real-Time Network Monitoring:**
  - Live upload & download speeds (smoothed with exponential moving average).
  - Strictly monotonic session totals (won't reset or bounce backwards on adapter reconnection).
- **💻 Hardware Telemetry:**
  - **CPU:** Load percentage + live dynamic turbo clock frequency (GHz).
  - **RAM:** Used physical memory (GB) and percentage load.
  - **Storage / Disks:** Monitors internal NVMe/SSDs, HDDs, and automatically detects plugged-in removable USB storage devices.
  - **GPUs:** Core load (%) and temperature (°C) for NVIDIA, AMD, and Intel GPUs.
- **🚀 One-Click Windows Shortcuts:**
  - Click **Current Speed** $\rightarrow$ Opens **Windows 11 Proxy Settings** (`ms-settings:network-proxy`) for instant toggling of *"Automatically detect settings"*.
  - Click **Totals** $\rightarrow$ Opens **Network Connections** (`ncpa.cpl`).
  - Click **CPU** $\rightarrow$ Opens **Windows Mobility Center** (`mblctr.exe`).
- **📐 Flexible Layouts:**
  - **Horizontal:** Classic taskbar strip.
  - **Vertical Tower:** Single-column status bar.
  - **Vertical Double-Column:** Compact 2-column widget.
  - *Smart Screen Clamping:* In vertical modes, the widget automatically anchors above the taskbar and extends upward into your workspace, preventing control buttons from clipping off-screen.
- **📍 Position & Docking Modes:**
  - **📌 Docked to Taskbar:** Automatically docks flush-left of the system tray notification area (`TrayNotifyWnd`).
  - **🔓 Unlocked:** Click and drag the widget anywhere on any screen. Moving/dragging is guarded so it will never trigger accidental clicks.
  - **🔒 Locked:** Pin the widget to your custom coordinates.
- **🔍 Scalable UI:** Scale the entire widget smoothly from **75% to 200%** with crisp font and vector rendering.
- **👁️ Component Visibility:** Selectively toggle any metric, disk, or GPU from the context menu.
- **⚙️ Auto-Saving:** Layout, position, scale, and visible items are automatically saved to `%APPDATA%\NetSpeedTray\config.json`.

---

## ⌨️ Global Hotkeys

These shortcuts work globally from anywhere in Windows:

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + Shift + H` | **Toggle Visibility** (Hide or Show the widget) |
| `Ctrl + Shift + R` | **Emergency Reset** (Instantly resets to Horizontal Taskbar dock @ 100% scale) |
| `Ctrl + Shift + M` | **Open Settings Menu** (Brings up context menu right at your mouse cursor) |

---

## 🖱️ Mouse Controls

| Action | Target | Result |
| :--- | :--- | :--- |
| **Left Click** | Current Speed (`▲` / `▼`) | Opens **Windows Proxy Settings** |
| **Left Click** | Session Totals (`TU` / `TD`) | Opens **Network Connections** (`ncpa.cpl`) |
| **Left Click** | CPU Block | Opens **Windows Mobility Center** |
| **Left Click** | Dock Icon (`📌`/`🔓`/`🔒`) | Cycles mode: *Taskbar* $\rightarrow$ *Moveable* $\rightarrow$ *Locked* |
| **Left Click** | Menu Circle (`●`) | Opens Settings Context Menu |
| **Right Click** | **Anywhere on the widget** | Opens Settings Context Menu |
| **Left Drag** | Body *(in Unlocked mode)* | Moves the widget across your screen(s) |

---

## 📦 Requirements & Installation

### Requirements
- **OS:** Windows 10 or Windows 11 (64-bit only).
- **Runtime:** [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (x64).
- **Permissions:** **Run as Administrator** is recommended for full hardware access. `LibreHardwareMonitor` requires administrative privileges to query low-level GPU core temperatures and physical drive activities.

### Download
Grab the latest single `.exe` release from the **[Releases](../../releases)** tab and run it.

---

## 🛠️ Building from Source

### Prerequisites
- .NET 8.0 SDK (or later)
- JetBrains Rider, Visual Studio 2022, or the .NET CLI

### Clone the Repository
```bash
git clone https://github.com/yourusername/NetSpeedTray.git
cd NetSpeedTray
