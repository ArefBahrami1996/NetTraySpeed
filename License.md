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
