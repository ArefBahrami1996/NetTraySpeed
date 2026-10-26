# NetSpeedTray 🚀

A lightweight, borderless hardware monitor and network speed widget for Windows 10 & 11. It docks seamlessly next to your system tray or floats anywhere on your desktop with live network rates, CPU turbo frequencies, RAM, multi-disk activity, and GPU thermals.

![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-blue?logo=windows)
![Architecture](https://img.shields.io/badge/Architecture-x64-green)
![Target](https://img.shields.io/badge/.NET-8.0--windows-purple?logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-brightgreen)

---

## ✨ Features

- **🌐 Live Network Monitoring**:
  - Real-time Upload & Download speed meters with exponential smoothing to prevent jitter.
  - Forward-accumulating, strictly monotonic session totals (`TU` / `TD`).
  - Automatic detection of active physical network adapters (skips virtual/VPN/container adapters).

- **⚡ Hardware & System Metrics**:
  - **CPU**: Real-time load percentage and live dynamic turbo clock speed in GHz (calculated using PDH base frequency scaling).
  - **RAM**: Memory usage in GB and physical memory load percentage.
  - **Disks**: Real-time activity percentage for all internal drives (NVMe, SSD, HDD) and removable devices (USB thumb drives) with automatic plug/unplug detection.
  - **GPUs**: Dedicated core load percentage and temperature reading for NVIDIA, AMD, and Intel graphics processors.

- **📐 Multiple View Layouts**:
  - **Horizontal**: Classic dockable strip that sits flush to the left of the system tray clock/icons.
  - **Vertical Tower**: Single-column vertical stack.
  - **Vertical Double-Column**: Compact dual-column grid extending safely upwards onto the desktop.

- **📍 Docking & Positioning Modes**:
  - **📌 Dock to Taskbar**: Automatically docks flush-left of the Windows system notification area.
  - **🔓 Unlocked (Drag to Move)**: Free-floating mode—click and drag the widget anywhere across any monitor.
  - **🔒 Lock in Place**: Pins the widget to its current screen coordinates.

- **🖱️ Direct Action Click Targets**:
  - Click **Current Speed (▲ / ▼)** ➔ Opens **Windows Proxy Settings** (`ms-settings:network-proxy` with auto-detect toggle).
  - Click **Session Totals (TU / TD)** ➔ Opens **Network Connections** (`ncpa.cpl`).
  - Click **CPU** ➔ Opens **Windows Mobility Center** (`mblctr.exe`).
  - Click **Action Circle** ➔ Opens the Context Menu.
  - Click **Dock Icon** ➔ Quickly cycles between `Docked`, `Unlocked`, and `Locked` modes.

- **🔍 Custom Scaling & Visibility**:
  - Scale the UI to **75%, 100%, 125%, 150%, or 200%** on the fly (fully DPI-aware).
  - Toggle visibility for any metric component, specific storage drive, or individual GPU.

- **⌨️ Global Keyboard Shortcuts**:
  - `Ctrl + Shift + H`: **Toggle Visibility** (Instant show/hide).
  - `Ctrl + Shift + R`: **Emergency Reset** (Instantly resets layout to Horizontal Taskbar dock).
  - `Ctrl + Shift + M`: **Menu at Cursor** (Opens settings menu directly under mouse pointer).

- **🪶 Zero Window Clutter**:
  - Runs with `WS_EX_TOOLWINDOW` and `WS_EX_NOACTIVATE`—never steals keyboard focus, never shows up in Alt+Tab, and leaves no taskbar button.

---

## ⌨️ Shortcut Reference

| Shortcut | Description |
| :--- | :--- |
| **`Ctrl + Shift + H`** | Toggle Show / Hide widget |
| **`Ctrl + Shift + R`** | Emergency Reset (switches back to Horizontal Taskbar mode) |
| **`Ctrl + Shift + M`** | Open Context Settings Menu under cursor |

---

## 🚀 Installation & Download

1. Go to the [**Releases**](https://github.com/YOUR_USERNAME/NetSpeedTray/releases) page.
2. Download the latest `NetSpeedTray.exe`.
3. *(Recommended)* Right-click `NetSpeedTray.exe` ➔ **Run as Administrator** to allow `LibreHardwareMonitor` full access to GPU thermals and drive activity sensors.
4. Place the executable anywhere (e.g., `C:\Program Files\NetSpeedTray\` or your user folder).

> **Note:** To have it start automatically with Windows, press `Win + R`, type `shell:startup`, and create a shortcut to `NetSpeedTray.exe` inside that folder.

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer)
- Windows 10 (Build 19041+) or Windows 11
- IDE: JetBrains Rider or Visual Studio 2022

### Build via Terminal / CLI

Clone the repository:
```bash
git clone https://github.com/YOUR_USERNAME/NetSpeedTray.git
cd NetSpeedTray
```

Build a standalone, single-file release (64-bit):
```bash
# Lightweight (~10 MB, requires .NET Desktop Runtime installed)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Or build a fully self-contained release (runs without any pre-installed .NET runtime):
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output executable will be generated at:
`bin/Release/net8.0-windows/win-x64/publish/NetSpeedTray.exe`

---

## ⚙️ Configuration

Settings (active layout, scaling, custom position, hidden components) are automatically saved to:
```
%APPDATA%\NetSpeedTray\config.json
```

---

## 🛡️ License

This project is licensed under the [MIT License](LICENSE).
```

---

### 2. GitHub Release Description (Copy & paste into the GitHub "Draft a new release" text box)

```markdown
# NetSpeedTray v1.0.0 — Initial Release 🚀

A lightweight, borderless hardware monitor and network speed tray widget for Windows 10 & 11.

---

### 🌟 Key Highlights

- **Live Network Tracking**: Real-time Upload/Download rates and forward-accumulating session totals (`TU`/`TD`).
- **Comprehensive Hardware Sensors**:
  - Live CPU utilization + Dynamic Turbo Frequency (GHz).
  - Physical RAM usage (GB and %).
  - Multi-Disk activity monitoring (automatically detects internal NVMe/SSDs and plugged-in USB storage).
  - Multi-GPU monitoring (core load and temperature for NVIDIA, AMD, and Intel).
- **Flexible Layouts**: Switch on the fly between **Horizontal (Taskbar Strip)**, **Vertical Tower**, or **Vertical Double-Column**.
- **Docking Modes**: Pin to taskbar, free-drag across multiple monitors (`🔓 Unlock`), or lock to specific desktop coordinates (`🔒 Lock`).
- **Interactive Quick-Launch Actions**:
  - Click **Current Speed** ➔ Opens Windows 11 Proxy Settings (`ms-settings:network-proxy`).
  - Click **Totals** ➔ Opens Network Connections (`ncpa.cpl`).
  - Click **CPU** ➔ Opens Windows Mobility Center (`mblctr.exe`).
- **Global Hotkeys**:
  - `Ctrl + Shift + H`: Toggle Hide/Show.
  - `Ctrl + Shift + R`: Emergency Reset to Horizontal Taskbar dock.
  - `Ctrl + Shift + M`: Open Settings Menu anywhere on screen.
- **Always-on-Top & Focus-Free**: Never steals focus during gaming or typing; no Alt+Tab clutter.
- **UI Scaling**: Scalable between 75% and 200%.

---

### 📦 Which File to Download?

- **`NetSpeedTray.exe` (Framework-Dependent, ~10 MB) [Recommended]**:  
  Lightweight and fast. Requires standard [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed on your system (standard on most modern Windows PCs).
- **`NetSpeedTray-SelfContained.zip` (~65 MB)**:  
  Contains all dependencies bundled inside. Runs on any Windows x64 PC out of the box with zero prerequisites.

---

### ⚠️ Administrator Rights Notice
To allow `LibreHardwareMonitor` to read low-level ring-0 sensor data (GPU temperatures and physical drive controller metrics), please **Run as Administrator**. If run without elevation, the app will fall back to PDH counters for CPU and primary disk metrics.
```
