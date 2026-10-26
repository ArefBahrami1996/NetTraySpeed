using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;

namespace NetSpeedTray;

// ---------------------------------------------------------------------------
// Entry Point
// ---------------------------------------------------------------------------
internal static class Program
{
    private static readonly IntPtr PerMonitorV2 = new(-4);

    [STAThread]
    private static void Main()
    {
        try { NativeMethods.SetProcessDpiAwarenessContext(PerMonitorV2); } catch { }
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayMonitorForm());
    }
}

// ---------------------------------------------------------------------------
// Enums & Configuration Persistence
// ---------------------------------------------------------------------------
public enum LayoutMode { Horizontal, VerticalTower, VerticalDouble }
public enum DockMode { DockedToTaskbar, Unlocked, Locked }

public sealed class AppConfig
{
    public DockMode Dock { get; set; } = DockMode.DockedToTaskbar;
    public LayoutMode Layout { get; set; } = LayoutMode.Horizontal;
    public float Scale { get; set; } = 1.0f;
    public int CustomX { get; set; } = -1;
    public int CustomY { get; set; } = -1;
    public HashSet<string> HiddenItems { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetSpeedTray", "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(ConfigPath)!;
            Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}

// ---------------------------------------------------------------------------
// Metrics Snapshot
// ---------------------------------------------------------------------------
internal sealed class MetricsSnapshot
{
    public sealed class GpuInfo
    {
        public string Label = "";
        public string Name = "";
        public double Load;
        public double Temp;
    }

    public sealed class DiskInfo
    {
        public string Id = "";
        public string Label = "";
        public string Name = "";
        public double ActivePercent;
    }

    public double UpBytesPerSec;
    public double DownBytesPerSec;
    public long SessionUpBytes;
    public long SessionDownBytes;
    public string AdapterName = "";

    public double CpuLoadPercent;
    public double CpuLiveGhz;

    public double RamUsedGb;
    public double RamTotalGb;
    public double RamLoadPercent;

    public DiskInfo[] Disks = Array.Empty<DiskInfo>();
    public GpuInfo[] Gpus = Array.Empty<GpuInfo>();
}

// ---------------------------------------------------------------------------
// Win32 Interop
// ---------------------------------------------------------------------------
internal static class NativeMethods
{
    [DllImport("user32.dll")] internal static extern IntPtr SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpszClass, string? lpszWindow);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] internal static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
    [DllImport("user32.dll")] internal static extern bool ReleaseCapture();
    [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    internal static readonly IntPtr HwndTopmost = new(-1);

    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_SHOWWINDOW = 0x0040;
    internal const int WM_NCLBUTTONDOWN = 0x00A1;
    internal static readonly IntPtr HTCAPTION = new(2);

    internal const uint MOD_CONTROL = 0x0002;
    internal const uint MOD_SHIFT = 0x0004;
    internal const uint MOD_NOREPEAT = 0x4000;

    [DllImport("gdi32.dll")] internal static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
    [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("kernel32.dll")] internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    internal struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public static MEMORYSTATUSEX Create() => new() { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] internal static extern uint PdhOpenQuery(string? szDataSource, IntPtr dwUserData, out IntPtr phQuery);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode, EntryPoint = "PdhAddEnglishCounterW")] internal static extern uint PdhAddEnglishCounter(IntPtr hQuery, string szFullCounterPath, IntPtr dwUserData, out IntPtr phCounter);
    [DllImport("pdh.dll")] internal static extern uint PdhCollectQueryData(IntPtr hQuery);
    [DllImport("pdh.dll")] internal static extern uint PdhGetFormattedCounterValue(IntPtr hCounter, uint dwFormat, IntPtr lpdwType, out PDH_FMT_COUNTERVALUE pValue);
    [DllImport("pdh.dll")] internal static extern uint PdhCloseQuery(IntPtr hQuery);

    internal const uint PDH_FMT_DOUBLE = 0x00000200;

    [StructLayout(LayoutKind.Explicit)]
    internal struct PDH_FMT_COUNTERVALUE
    {
        [FieldOffset(0)] public uint CStatus;
        [FieldOffset(8)] public double DoubleValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}

// ---------------------------------------------------------------------------
// PDH Sampler
// ---------------------------------------------------------------------------
internal sealed class PdhSampler : IDisposable
{
    private IntPtr _query;
    private IntPtr _cpuLoadCounter;
    private IntPtr _cpuPerfCounter;
    private IntPtr _diskTimeCounter;
    private readonly bool _available;

    public PdhSampler()
    {
        if (NativeMethods.PdhOpenQuery(null, IntPtr.Zero, out _query) != 0) return;

        NativeMethods.PdhAddEnglishCounter(_query, @"\Processor Information(_Total)\% Processor Time", IntPtr.Zero, out _cpuLoadCounter);
        NativeMethods.PdhAddEnglishCounter(_query, @"\Processor Information(_Total)\% Processor Performance", IntPtr.Zero, out _cpuPerfCounter);
        NativeMethods.PdhAddEnglishCounter(_query, @"\PhysicalDisk(_Total)\% Disk Time", IntPtr.Zero, out _diskTimeCounter);

        NativeMethods.PdhCollectQueryData(_query);
        _available = true;
    }

    public (double CpuLoad, double CpuPerformance, double DiskTime) Sample()
    {
        if (!_available) return (0, 0, 0);
        NativeMethods.PdhCollectQueryData(_query);
        return (GetValue(_cpuLoadCounter), GetValue(_cpuPerfCounter), GetValue(_diskTimeCounter));
    }

    private static double GetValue(IntPtr counter)
    {
        if (counter == IntPtr.Zero) return 0;
        uint status = NativeMethods.PdhGetFormattedCounterValue(counter, NativeMethods.PDH_FMT_DOUBLE, IntPtr.Zero, out var val);
        return status == 0 && val.CStatus == 0 ? val.DoubleValue : 0;
    }

    public void Dispose()
    {
        if (_query != IntPtr.Zero)
        {
            NativeMethods.PdhCloseQuery(_query);
            _query = IntPtr.Zero;
        }
    }
}

// ---------------------------------------------------------------------------
// Network Sampler
// ---------------------------------------------------------------------------
internal sealed class NetworkSampler
{
    private string? _adapterId;
    private string _adapterName = "No active adapter";
    private long _lastSent = -1, _lastRecv = -1;
    private long _sessionSent, _sessionRecv;
    private double _upRate, _downRate;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastTimestamp;

    private static NetworkInterface? PickActiveAdapter()
    {
        NetworkInterface? best = null;
        long bestSpeed = -1;

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
            string desc = ni.Description;
            if (desc.Contains("virtual", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("hyper-v", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("vmware", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("virtualbox", StringComparison.OrdinalIgnoreCase))
                continue;

            long speed = 0;
            try { speed = ni.Speed; } catch { }
            if (speed > bestSpeed)
            {
                best = ni;
                bestSpeed = speed;
            }
        }
        return best;
    }

    public void Sample(MetricsSnapshot snap)
    {
        NetworkInterface? adapter = PickActiveAdapter();
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Max(0.02, now - _lastTimestamp);
        _lastTimestamp = now;

        if (adapter == null)
        {
            _adapterId = null;
            snap.AdapterName = _adapterName = "No active adapter";
            snap.UpBytesPerSec = snap.DownBytesPerSec = 0;
            return;
        }

        long sent, recv;
        try
        {
            var stats = adapter.GetIPStatistics();
            sent = stats.BytesSent;
            recv = stats.BytesReceived;
        }
        catch { return; }

        if (_adapterId != adapter.Id || _lastSent < 0)
        {
            _adapterId = adapter.Id;
            _adapterName = adapter.Name;
            _lastSent = sent;
            _lastRecv = recv;
            _upRate = _downRate = 0;
        }
        else
        {
            long deltaSent = Math.Max(0, sent - _lastSent);
            long deltaRecv = Math.Max(0, recv - _lastRecv);
            _lastSent = sent;
            _lastRecv = recv;
            _sessionSent += deltaSent;
            _sessionRecv += deltaRecv;

            _upRate = _upRate * 0.4 + (deltaSent / dt) * 0.6;
            _downRate = _downRate * 0.4 + (deltaRecv / dt) * 0.6;
        }

        snap.AdapterName = _adapterName;
        snap.UpBytesPerSec = _upRate;
        snap.DownBytesPerSec = _downRate;
        snap.SessionUpBytes = _sessionSent;
        snap.SessionDownBytes = _sessionRecv;
    }
}

// ---------------------------------------------------------------------------
// Hardware Sampler (GPUs + Multiple Storage Devices)
// ---------------------------------------------------------------------------
internal sealed class HardwareSampler : IDisposable
{
    private sealed class GpuEntry
    {
        public required IHardware Hardware;
        public ISensor? LoadSensor;
        public ISensor? TempSensor;
        public string Label = "G0";
    }

    private sealed class DiskEntry
    {
        public required IHardware Hardware;
        public ISensor? ActivitySensor;
        public string Id = "";
        public string Label = "";
    }

    private Computer? _computer;
    private bool _initAttempted, _ok;
    private readonly List<GpuEntry> _gpus = new();
    private readonly List<DiskEntry> _disks = new();
    private int _refreshCounter;

    private void EnsureInitialized()
    {
        if (_initAttempted) return;
        _initAttempted = true;

        try
        {
            _computer = new Computer { IsGpuEnabled = true, IsStorageEnabled = true };
            _computer.Open();
            _ok = true;
            RefreshDevices();
        }
        catch { _ok = false; }
    }

    private void RefreshDevices()
    {
        _gpus.Clear();
        _disks.Clear();
        if (_computer == null) return;

        int gpuIdx = 0, diskIdx = 0;
        foreach (IHardware hw in _computer.Hardware)
        {
            try { hw.Update(); } catch { continue; }

            if (hw.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
            {
                char letter = hw.HardwareType switch
                {
                    HardwareType.GpuNvidia => 'N',
                    HardwareType.GpuAmd => 'A',
                    HardwareType.GpuIntel => 'I',
                    _ => 'G'
                };

                _gpus.Add(new GpuEntry
                {
                    Hardware = hw,
                    LoadSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Core")) ??
                                 hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load),
                    TempSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core")) ??
                                 hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature),
                    Label = $"{letter}{gpuIdx++}"
                });
            }
            else if (hw.HardwareType == HardwareType.Storage)
            {
                _disks.Add(new DiskEntry
                {
                    Hardware = hw,
                    ActivitySensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Activity")) ??
                                     hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load),
                    Id = $"D{diskIdx}",
                    Label = $"D{diskIdx++}"
                });
            }
        }
    }

    public void Sample(MetricsSnapshot snap, double pdhDiskFallback)
    {
        EnsureInitialized();

        if (_ok && ++_refreshCounter % 8 == 0)
        {
            try { _computer?.Reset(); RefreshDevices(); } catch { }
        }

        var gpuList = new List<MetricsSnapshot.GpuInfo>();
        var diskList = new List<MetricsSnapshot.DiskInfo>();

        if (_ok)
        {
            foreach (var gpu in _gpus)
            {
                try { gpu.Hardware.Update(); } catch { }
                gpuList.Add(new MetricsSnapshot.GpuInfo
                {
                    Label = gpu.Label,
                    Name = gpu.Hardware.Name,
                    Load = gpu.LoadSensor?.Value ?? 0,
                    Temp = gpu.TempSensor?.Value ?? 0
                });
            }

            foreach (var disk in _disks)
            {
                try { disk.Hardware.Update(); } catch { }
                diskList.Add(new MetricsSnapshot.DiskInfo
                {
                    Id = disk.Id,
                    Label = disk.Label,
                    Name = disk.Hardware.Name,
                    ActivePercent = Math.Clamp(disk.ActivitySensor?.Value ?? 0, 0, 100)
                });
            }
        }

        if (diskList.Count == 0)
        {
            try
            {
                int idx = 0;
                foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    diskList.Add(new MetricsSnapshot.DiskInfo
                    {
                        Id = $"D{idx}",
                        Label = d.Name.TrimEnd('\\'),
                        Name = $"{d.Name} ({d.DriveType})",
                        ActivePercent = Math.Clamp(pdhDiskFallback, 0, 100)
                    });
                    idx++;
                }
            }
            catch { }
        }

        snap.Gpus = gpuList.ToArray();
        snap.Disks = diskList.ToArray();
    }

    public void Dispose()
    {
        try { _computer?.Close(); } catch { }
    }
}

// ---------------------------------------------------------------------------
// Dark Menu Renderer
// ---------------------------------------------------------------------------
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    private sealed class DarkColorTable : ProfessionalColorTable
    {
        private static readonly Color Bg = Color.FromArgb(31, 31, 35);
        private static readonly Color Hover = Color.FromArgb(52, 52, 60);
        private static readonly Color Border = Color.FromArgb(70, 70, 78);

        public override Color MenuStripGradientBegin => Bg;
        public override Color MenuStripGradientEnd => Bg;
        public override Color ToolStripDropDownBackground => Bg;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemBorder => Hover;
        public override Color MenuBorder => Border;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color CheckBackground => Hover;
        public override Color CheckSelectedBackground => Hover;
        public override Color ImageMarginGradientBegin => Bg;
        public override Color ImageMarginGradientMiddle => Bg;
        public override Color ImageMarginGradientEnd => Bg;
    }

    public DarkMenuRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? Color.White : Color.FromArgb(220, 220, 224);
        base.OnRenderItemText(e);
    }
}

// ---------------------------------------------------------------------------
// Main Form (Widget)
// ---------------------------------------------------------------------------
internal sealed class TrayMonitorForm : Form
{
    private static readonly Color BgColor = Color.FromArgb(24, 24, 26);
    private static readonly Color LabelColor = Color.FromArgb(138, 138, 147);
    private static readonly Color DividerColor = Color.FromArgb(46, 46, 51);
    private static readonly Color SkyBlue = Color.FromArgb(0, 176, 255);
    private static readonly Color NeonGreen = Color.FromArgb(0, 230, 118);
    private static readonly Color Amber = Color.FromArgb(255, 167, 38);
    private static readonly Color Violet = Color.FromArgb(186, 104, 200);
    private static readonly Color Teal = Color.FromArgb(38, 166, 154);
    private static readonly Color Coral = Color.FromArgb(255, 112, 67);

    private static readonly Color CircleNormalColor = Color.FromArgb(95, 95, 105);
    private static readonly Color CircleHoverColor = SkyBlue;

    private const int BaseColWidth = 96;
    private const int BaseRowHeight = 36;

    // Hotkey IDs
    private const int HotkeyToggle = 9119;
    private const int HotkeyReset = 9120;
    private const int HotkeyMenu = 9121;

    private readonly AppConfig _config;
    private volatile MetricsSnapshot _snapshot = new();

    private readonly PdhSampler _pdh = new();
    private readonly NetworkSampler _network = new();
    private readonly HardwareSampler _hardware = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };

    private int _polling;
    private int _tickCounter;

    private Font _font;
    private Font _symbolFont;
    private Pen _dividerPen;

    // Action Circle (Opens Menu)
    private Rectangle _circleBounds;
    private Rectangle _circleHitArea;
    private bool _circleHovered;

    // Dock Mode Icon (Toggles Docked / Unlocked / Locked)
    private Rectangle _dockIconBounds;
    private Rectangle _dockIconHitArea;
    private bool _dockIconHovered;

    private Point _mouseDownPos;
    private bool _isDragging;

    private sealed class MetricBlock
    {
        public string Id = "";
        public string TopLabel = "";
        public string TopValue = "";
        public Color TopColor;
        public string BottomLabel = "";
        public string BottomValue = "";
        public Color BottomColor;
        public Rectangle Bounds;
    }

    private readonly List<MetricBlock> _renderedBlocks = new();

    private int S(int logical) => (int)Math.Round(logical * (DeviceDpi / 96.0f) * _config.Scale);

    public TrayMonitorForm()
    {
        _config = AppConfig.Load();

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = BgColor;
        TopMost = true;

        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint, true);

        _font = new Font("Segoe UI", 8.25f * _config.Scale);
        _symbolFont = new Font("Segoe UI Emoji", 8.25f * _config.Scale);
        _dividerPen = new Pen(DividerColor, 1f);

        Poll();

        _timer.Tick += (_, _) =>
        {
            if (Interlocked.Exchange(ref _polling, 1) == 1) return;
            Task.Run(() =>
            {
                try { Poll(); }
                finally
                {
                    Interlocked.Exchange(ref _polling, 0);
                    try { BeginInvoke(AfterPoll); } catch { }
                }
            });
        };
        _timer.Start();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        int preference = 3; // DWMWCP_ROUNDSMALL
        NativeMethods.DwmSetWindowAttribute(Handle, 33, ref preference, sizeof(int));

        // Hotkey 1: Ctrl + Shift + H -> Toggle Hide/Show
        NativeMethods.RegisterHotKey(Handle, HotkeyToggle,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT, (uint)Keys.H);

        // Hotkey 2: Ctrl + Shift + R -> Emergency Reset to Horizontal Taskbar Mode
        NativeMethods.RegisterHotKey(Handle, HotkeyReset,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT, (uint)Keys.R);

        // Hotkey 3: Ctrl + Shift + M -> Open Settings Menu at Cursor
        NativeMethods.RegisterHotKey(Handle, HotkeyMenu,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT, (uint)Keys.M);

        EnforceTopmost();
        ResizeAndLayout();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_ERASEBKGND = 0x0014;
        const int WM_HOTKEY = 0x0312;

        switch (m.Msg)
        {
            case WM_ERASEBKGND:
                m.Result = (IntPtr)1;
                return;
            case WM_HOTKEY:
                int hotkey = m.WParam.ToInt32();
                if (hotkey == HotkeyToggle)
                {
                    Visible = !Visible;
                    if (Visible)
                    {
                        if (_config.Dock == DockMode.DockedToTaskbar) DockToTray();
                        EnforceTopmost();
                        BringToFront();
                    }
                }
                else if (hotkey == HotkeyReset)
                {
                    // Instant Emergency Reset
                    _config.Layout = LayoutMode.Horizontal;
                    _config.Dock = DockMode.DockedToTaskbar;
                    _config.Scale = 1.0f;
                    _config.Save();

                    _font.Dispose();
                    _symbolFont.Dispose();
                    _font = new Font("Segoe UI", 8.25f);
                    _symbolFont = new Font("Segoe UI Emoji", 8.25f);

                    Visible = true;
                    ResizeAndLayout();
                    DockToTray();
                    EnforceTopmost();
                    BringToFront();
                    Invalidate();
                }
                else if (hotkey == HotkeyMenu)
                {
                    ShowContextMenu();
                }
                return;
            default:
                base.WndProc(ref m);
                return;
        }
    }

    private void EnforceTopmost()
    {
        NativeMethods.SetWindowPos(Handle, NativeMethods.HwndTopmost, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    private void Poll()
    {
        var snap = new MetricsSnapshot();
        var (cpuLoad, cpuPerf, diskTime) = _pdh.Sample();

        snap.CpuLoadPercent = Math.Clamp(cpuLoad, 0, 100);
        snap.CpuLiveGhz = GetProcessorBaseMhz() * (cpuPerf / 100.0) / 1000.0;

        var mem = NativeMethods.MEMORYSTATUSEX.Create();
        if (NativeMethods.GlobalMemoryStatusEx(ref mem))
        {
            snap.RamTotalGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
            snap.RamUsedGb = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
            snap.RamLoadPercent = mem.dwMemoryLoad;
        }

        _network.Sample(snap);
        _hardware.Sample(snap, diskTime);

        _snapshot = snap;
    }

    private static double _cachedBaseMhz;
    private static double GetProcessorBaseMhz()
    {
        if (_cachedBaseMhz > 0) return _cachedBaseMhz;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (key?.GetValue("~MHz") is int mhz && mhz > 0) _cachedBaseMhz = mhz;
        }
        catch { }
        if (_cachedBaseMhz <= 0) _cachedBaseMhz = 2500;
        return _cachedBaseMhz;
    }

    private void AfterPoll()
    {
        ResizeAndLayout();
        if (_config.Dock == DockMode.DockedToTaskbar && ++_tickCounter % 5 == 0)
        {
            DockToTray();
        }
        Invalidate();
    }

    // ---------------------------------------------------------------------------
    // Layout and Measurement Engine
    // ---------------------------------------------------------------------------
    private void ResizeAndLayout()
    {
        var snap = _snapshot;
        var blocks = new List<MetricBlock>();

        if (!_config.HiddenItems.Contains("NET_SPEED"))
        {
            blocks.Add(new MetricBlock
            {
                Id = "NET_SPEED",
                TopLabel = "▲", TopValue = FormatRate(snap.UpBytesPerSec), TopColor = SkyBlue,
                BottomLabel = "▼", BottomValue = FormatRate(snap.DownBytesPerSec), BottomColor = NeonGreen
            });
        }

        if (!_config.HiddenItems.Contains("NET_TOTAL"))
        {
            blocks.Add(new MetricBlock
            {
                Id = "NET_TOTAL",
                TopLabel = "TU", TopValue = FormatTotal(snap.SessionUpBytes), TopColor = SkyBlue,
                BottomLabel = "TD", BottomValue = FormatTotal(snap.SessionDownBytes), BottomColor = NeonGreen
            });
        }

        if (!_config.HiddenItems.Contains("CPU"))
        {
            blocks.Add(new MetricBlock
            {
                Id = "CPU",
                TopLabel = "CPU", TopValue = $"{snap.CpuLoadPercent:F0} %", TopColor = Amber,
                BottomLabel = "", BottomValue = $"{snap.CpuLiveGhz:F2} GHz", BottomColor = Amber
            });
        }

        if (!_config.HiddenItems.Contains("RAM"))
        {
            blocks.Add(new MetricBlock
            {
                Id = "RAM",
                TopLabel = "RAM", TopValue = $"{snap.RamUsedGb:F1}G", TopColor = Violet,
                BottomLabel = "", BottomValue = $"{snap.RamLoadPercent:F0} %", BottomColor = Violet
            });
        }

        foreach (var disk in snap.Disks)
        {
            if (!_config.HiddenItems.Contains($"DISK:{disk.Id}"))
            {
                blocks.Add(new MetricBlock
                {
                    Id = $"DISK:{disk.Id}",
                    TopLabel = disk.Label, TopValue = $"{disk.ActivePercent:F0} %", TopColor = Teal,
                    BottomLabel = "", BottomValue = disk.ActivePercent >= 1.0 ? "Active" : "Idle", BottomColor = Teal
                });
            }
        }

        foreach (var gpu in snap.Gpus)
        {
            if (!_config.HiddenItems.Contains($"GPU:{gpu.Label}"))
            {
                blocks.Add(new MetricBlock
                {
                    Id = $"GPU:{gpu.Label}",
                    TopLabel = gpu.Label, TopValue = $"{gpu.Load:F0} %", TopColor = Coral,
                    BottomLabel = "", BottomValue = $"{gpu.Temp:F0}°C", BottomColor = Coral
                });
            }
        }

        int colW = S(BaseColWidth);
        int rowH = S(BaseRowHeight);
        int ctrlArea = S(38);
        int pad = S(6);

        _renderedBlocks.Clear();

        switch (_config.Layout)
        {
            case LayoutMode.Horizontal:
            {
                int totalW = pad + (blocks.Count * colW) + ctrlArea + pad;
                int totalH = rowH;
                SetDimensions(totalW, totalH);

                int x = pad;
                foreach (var b in blocks)
                {
                    b.Bounds = new Rectangle(x, 0, colW, totalH);
                    _renderedBlocks.Add(b);
                    x += colW;
                }

                int ctrlLeft = Width - pad - ctrlArea;
                int d = S(10);

                _circleBounds = new Rectangle(ctrlLeft + S(4), Height / 2 - d / 2, d, d);
                _circleHitArea = new Rectangle(ctrlLeft, 0, S(18), Height);

                _dockIconBounds = new Rectangle(ctrlLeft + S(18), Height / 2 - S(8), S(16), S(16));
                _dockIconHitArea = new Rectangle(ctrlLeft + S(18), 0, S(20), Height);
                break;
            }

            case LayoutMode.VerticalTower:
            {
                int totalW = colW + pad * 2;
                int totalH = pad + (blocks.Count * rowH) + S(26) + pad;
                SetDimensions(totalW, totalH);

                int y = pad;
                foreach (var b in blocks)
                {
                    b.Bounds = new Rectangle(pad, y, colW, rowH);
                    _renderedBlocks.Add(b);
                    y += rowH;
                }

                int footerY = Height - pad - S(24);
                int d = S(10);

                _circleBounds = new Rectangle(Width / 2 - S(16), footerY + S(6), d, d);
                _circleHitArea = new Rectangle(Width / 2 - S(22), footerY, S(20), S(24));

                _dockIconBounds = new Rectangle(Width / 2 + S(4), footerY + S(4), S(16), S(16));
                _dockIconHitArea = new Rectangle(Width / 2 + S(2), footerY, S(20), S(24));
                break;
            }

            case LayoutMode.VerticalDouble:
            {
                int totalW = (colW * 2) + pad * 2;
                int rows = Math.Max(1, (blocks.Count + 1) / 2);
                int totalH = pad + (rows * rowH) + S(26) + pad;
                SetDimensions(totalW, totalH);

                for (int i = 0; i < blocks.Count; i++)
                {
                    int r = i / 2;
                    int c = i % 2;
                    blocks[i].Bounds = new Rectangle(pad + c * colW, pad + r * rowH, colW, rowH);
                    _renderedBlocks.Add(blocks[i]);
                }

                int footerY = Height - pad - S(24);
                int d = S(10);

                _circleBounds = new Rectangle(Width / 2 - S(16), footerY + S(6), d, d);
                _circleHitArea = new Rectangle(Width / 2 - S(22), footerY, S(20), S(24));

                _dockIconBounds = new Rectangle(Width / 2 + S(4), footerY + S(4), S(16), S(16));
                _dockIconHitArea = new Rectangle(Width / 2 + S(2), footerY, S(20), S(24));
                break;
            }
        }

        ApplyRegion();

        if (_config.Dock == DockMode.DockedToTaskbar)
        {
            DockToTray();
        }
        else if (_config.CustomX >= 0 && _config.CustomY >= 0)
        {
            NativeMethods.SetWindowPos(Handle, IntPtr.Zero, _config.CustomX, _config.CustomY, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void SetDimensions(int w, int h)
    {
        if (Width != w || Height != h)
        {
            Size = new Size(w, h);
        }
    }

    private void ApplyRegion()
    {
        if (!IsHandleCreated) return;
        int r = Math.Max(6, S(7));
        IntPtr rgn = NativeMethods.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, r * 2, r * 2);
        NativeMethods.SetWindowRgn(Handle, rgn, true);
    }

    /// <summary>
    /// Positions the widget. In vertical layouts, anchors the bottom of the widget
    /// to the taskbar and extends upwards into the screen, preventing clipping off-screen.
    /// </summary>
    private void DockToTray()
    {
        if (!IsHandleCreated) return;

        var screen = Screen.FromHandle(Handle);
        Rectangle bounds = screen.Bounds;
        Rectangle wa = screen.WorkingArea;

        int x = wa.Right - Width - S(8);
        int y = wa.Bottom - Height - S(8);

        IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
        if (taskbar != IntPtr.Zero)
        {
            IntPtr tray = NativeMethods.FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
            if (tray != IntPtr.Zero && NativeMethods.GetWindowRect(tray, out var trayRect))
            {
                x = trayRect.Left - Width - S(4);
                int trayH = trayRect.Bottom - trayRect.Top;

                // If widget fits within taskbar height, center it vertically.
                // If it is a tall vertical layout, anchor its bottom to the taskbar bottom
                // so the buttons stay 100% on screen and the rest extends upwards.
                if (Height <= trayH + S(4))
                {
                    y = trayRect.Top + (trayH - Height) / 2;
                }
                else
                {
                    y = trayRect.Bottom - Height;
                }
            }
            else if (NativeMethods.GetWindowRect(taskbar, out var taskbarRect))
            {
                x = taskbarRect.Right - Width - S(8);
                int barH = taskbarRect.Bottom - taskbarRect.Top;
                y = Height <= barH + S(4) ? taskbarRect.Top + (barH - Height) / 2 : taskbarRect.Bottom - Height;
            }
        }

        // Safety clamp: Ensure widget NEVER hangs off any edge of the monitor
        x = Math.Clamp(x, bounds.Left + S(4), bounds.Right - Width - S(4));
        y = Math.Clamp(y, bounds.Top + S(4), bounds.Bottom - Height - S(4));

        NativeMethods.SetWindowPos(Handle, IntPtr.Zero, x, y, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    // ---------------------------------------------------------------------------
    // Rendering
    // ---------------------------------------------------------------------------
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        using var bg = new SolidBrush(BackColor);
        g.FillRectangle(bg, ClientRectangle);

        using var labelBrush = new SolidBrush(LabelColor);

        foreach (var block in _renderedBlocks)
        {
            Rectangle r = block.Bounds;
            using var topBrush = new SolidBrush(block.TopColor);
            using var botBrush = new SolidBrush(block.BottomColor);

            float topY = r.Y + S(4);
            float botY = r.Y + S(18);

            DrawTextRow(g, r.X, topY, block.TopLabel, block.TopValue, labelBrush, topBrush);
            DrawTextRow(g, r.X, botY, block.BottomLabel, block.BottomValue, labelBrush, botBrush);

            if (_config.Layout == LayoutMode.Horizontal)
            {
                int divX = r.Right - S(4);
                g.DrawLine(_dividerPen, divX, S(6), divX, Height - S(6));
            }
            else
            {
                int divY = r.Bottom - S(2);
                g.DrawLine(_dividerPen, r.X, divY, r.Right - S(6), divY);
            }
        }

        // 1. Menu Circle Button
        Color cCol = _circleHovered ? CircleHoverColor : CircleNormalColor;
        using (var cBrush = new SolidBrush(cCol))
        {
            g.FillEllipse(cBrush, _circleBounds);
        }

        // 2. Dock Mode Icon (📌 = Taskbar, 🔓 = Moveable, 🔒 = Locked)
        string dockGlyph = _config.Dock switch
        {
            DockMode.DockedToTaskbar => "📌",
            DockMode.Unlocked => "🔓",
            DockMode.Locked => "🔒",
            _ => "📌"
        };

        if (_dockIconHovered)
        {
            using var hoverBg = new SolidBrush(Color.FromArgb(45, 45, 52));
            g.FillRectangle(hoverBg, _dockIconBounds);
        }

        using var glyphBrush = new SolidBrush(LabelColor);
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(dockGlyph, _symbolFont, glyphBrush, _dockIconBounds, sf);
    }

    private void DrawTextRow(Graphics g, float x, float y, string lbl, string val, Brush lblBrush, Brush valBrush)
    {
        var sf = StringFormat.GenericTypographic;
        float cur = x;

        if (!string.IsNullOrEmpty(lbl))
        {
            g.DrawString(lbl, _font, lblBrush, cur, y, sf);
            cur += g.MeasureString(lbl, _font, PointF.Empty, sf).Width + S(6);
        }

        g.DrawString(val, _font, valBrush, cur, y, sf);
    }

    private static string FormatRate(double bytesPerSec)
    {
        const double Kb = 1024.0, Mb = 1024.0 * 1024.0;
        return bytesPerSec >= Mb ? $"{bytesPerSec / Mb:F1} MB/s" : $"{bytesPerSec / Kb:F0} KB/s";
    }

    private static string FormatTotal(long bytes)
    {
        const double Kb = 1024.0, Mb = 1024.0 * 1024.0, Gb = 1024.0 * 1024.0 * 1024.0;
        if (bytes >= Gb) return $"{bytes / Gb:F2} GB";
        if (bytes >= Mb) return $"{bytes / Mb:F1} MB";
        return $"{bytes / Kb:F0} KB";
    }

    // ---------------------------------------------------------------------------
    // Mouse Interaction & Dragging
    // ---------------------------------------------------------------------------
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        bool inCircle = _circleHitArea.Contains(e.Location);
        bool inDock = _dockIconHitArea.Contains(e.Location);

        if (_circleHovered != inCircle)
        {
            _circleHovered = inCircle;
            Invalidate(_circleHitArea);
        }

        if (_dockIconHovered != inDock)
        {
            _dockIconHovered = inDock;
            Invalidate(_dockIconHitArea);
        }

        var block = _renderedBlocks.FirstOrDefault(b => b.Bounds.Contains(e.Location));

        if (inCircle || inDock)
        {
            Cursor = Cursors.Hand;
        }
        else if (block != null && block.Id is "NET_SPEED" or "NET_TOTAL" or "CPU")
        {
            Cursor = Cursors.Hand;
        }
        else if (_config.Dock == DockMode.Unlocked)
        {
            Cursor = Cursors.SizeAll;
            if (e.Button == MouseButtons.Left && !_circleHitArea.Contains(_mouseDownPos) && !_dockIconHitArea.Contains(_mouseDownPos))
            {
                if (Math.Abs(e.X - _mouseDownPos.X) > 4 || Math.Abs(e.Y - _mouseDownPos.Y) > 4)
                {
                    _isDragging = true;
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, IntPtr.Zero);
                    _config.CustomX = Left;
                    _config.CustomY = Top;
                    _config.Save();
                }
            }
        }
        else
        {
            Cursor = Cursors.Default;
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_circleHovered || _dockIconHovered)
        {
            _circleHovered = false;
            _dockIconHovered = false;
            Invalidate();
        }
        Cursor = Cursors.Default;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _mouseDownPos = e.Location;
            _isDragging = false;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_isDragging)
        {
            _isDragging = false;
            return;
        }

        if (Math.Abs(e.X - _mouseDownPos.X) > 4 || Math.Abs(e.Y - _mouseDownPos.Y) > 4)
        {
            return;
        }

        // Right-click ANYWHERE on the app opens the context menu
        if (e.Button == MouseButtons.Right)
        {
            ShowContextMenu();
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (_circleHitArea.Contains(e.Location))
            {
                ShowContextMenu();
                return;
            }

            if (_dockIconHitArea.Contains(e.Location))
            {
                ToggleDockMode();
                return;
            }

            var block = _renderedBlocks.FirstOrDefault(b => b.Bounds.Contains(e.Location));
            if (block != null)
            {
                if (block.Id == "NET_SPEED")
                {
                    OpenProxySettings();
                }
                else if (block.Id == "NET_TOTAL")
                {
                    OpenNetworkConnections();
                }
                else if (block.Id == "CPU")
                {
                    OpenMobilityCenter();
                }
            }
        }
    }

    private void ToggleDockMode()
    {
        _config.Dock = _config.Dock switch
        {
            DockMode.DockedToTaskbar => DockMode.Unlocked,
            DockMode.Unlocked => DockMode.Locked,
            DockMode.Locked => DockMode.DockedToTaskbar,
            _ => DockMode.DockedToTaskbar
        };

        if (_config.Dock == DockMode.Locked)
        {
            _config.CustomX = Left;
            _config.CustomY = Top;
        }

        _config.Save();
        ResizeAndLayout();
        Invalidate();
    }

    private static void OpenProxySettings()
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:network-proxy") { UseShellExecute = true }); } catch { }
    }

    private static void OpenNetworkConnections()
    {
        try { Process.Start(new ProcessStartInfo("ncpa.cpl") { UseShellExecute = true }); }
        catch
        {
            try { Process.Start(new ProcessStartInfo("control.exe", "ncpa.cpl") { UseShellExecute = true }); } catch { }
        }
    }

    private static void OpenMobilityCenter()
    {
        try { Process.Start(new ProcessStartInfo("mblctr.exe") { UseShellExecute = true }); } catch { }
    }

    // ---------------------------------------------------------------------------
    // Context Menu
    // ---------------------------------------------------------------------------
    private void ShowContextMenu()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(),
            Font = new Font("Segoe UI", 9f),
            ShowImageMargin = false
        };

        // Layout Mode
        var layoutMenu = new ToolStripMenuItem("📐 Layout Mode");
        AddLayoutOption(layoutMenu, "Horizontal (Taskbar Strip)", LayoutMode.Horizontal);
        AddLayoutOption(layoutMenu, "Vertical Tower", LayoutMode.VerticalTower);
        AddLayoutOption(layoutMenu, "Vertical Double-Column", LayoutMode.VerticalDouble);

        // Docking & Position
        var dockMenu = new ToolStripMenuItem("📍 Position & Docking");
        AddDockOption(dockMenu, "📌 Dock to Taskbar", DockMode.DockedToTaskbar);
        AddDockOption(dockMenu, "🔓 Unlock (Drag to Move)", DockMode.Unlocked);
        AddDockOption(dockMenu, "🔒 Lock at Current Position", DockMode.Locked);

        // Scale
        var scaleMenu = new ToolStripMenuItem("🔍 App Scale");
        float[] scales = { 0.75f, 1.0f, 1.25f, 1.5f, 2.0f };
        foreach (float s in scales)
        {
            var sItem = new ToolStripMenuItem($"{(int)(s * 100)}%") { Checked = Math.Abs(_config.Scale - s) < 0.01f };
            sItem.Click += (_, _) =>
            {
                _config.Scale = s;
                _config.Save();
                _font.Dispose();
                _symbolFont.Dispose();
                _font = new Font("Segoe UI", 8.25f * _config.Scale);
                _symbolFont = new Font("Segoe UI Emoji", 8.25f * _config.Scale);
                ResizeAndLayout();
                Invalidate();
            };
            scaleMenu.DropDownItems.Add(sItem);
        }

        // Show/Hide Items
        var visibilityMenu = new ToolStripMenuItem("👁️ Show / Hide Items");
        AddVisibilityToggle(visibilityMenu, "Network Speed", "NET_SPEED");
        AddVisibilityToggle(visibilityMenu, "Session Totals", "NET_TOTAL");
        AddVisibilityToggle(visibilityMenu, "CPU Monitor", "CPU");
        AddVisibilityToggle(visibilityMenu, "RAM Monitor", "RAM");

        if (_snapshot.Disks.Length > 0)
        {
            visibilityMenu.DropDownItems.Add(new ToolStripSeparator());
            foreach (var disk in _snapshot.Disks)
            {
                AddVisibilityToggle(visibilityMenu, $"Disk: {disk.Label} ({disk.Name})", $"DISK:{disk.Id}");
            }
        }

        if (_snapshot.Gpus.Length > 0)
        {
            visibilityMenu.DropDownItems.Add(new ToolStripSeparator());
            foreach (var gpu in _snapshot.Gpus)
            {
                AddVisibilityToggle(visibilityMenu, $"GPU: {gpu.Label} ({gpu.Name})", $"GPU:{gpu.Label}");
            }
        }

        var hotkeyHint1 = new ToolStripMenuItem("⌨️ Toggle Visibility: Ctrl + Shift + H") { Enabled = false };
        var hotkeyHint2 = new ToolStripMenuItem("⌨️ Emergency Reset: Ctrl + Shift + R") { Enabled = false };

        menu.Items.Add(layoutMenu);
        menu.Items.Add(dockMenu);
        menu.Items.Add(scaleMenu);
        menu.Items.Add(visibilityMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(hotkeyHint1);
        menu.Items.Add(hotkeyHint2);

        menu.Show(Cursor.Position);
    }

    private void AddLayoutOption(ToolStripMenuItem parent, string label, LayoutMode mode)
    {
        var item = new ToolStripMenuItem(label) { Checked = _config.Layout == mode };
        item.Click += (_, _) =>
        {
            _config.Layout = mode;
            _config.Save();
            ResizeAndLayout();
            Invalidate();
        };
        parent.DropDownItems.Add(item);
    }

    private void AddDockOption(ToolStripMenuItem parent, string label, DockMode mode)
    {
        var item = new ToolStripMenuItem(label) { Checked = _config.Dock == mode };
        item.Click += (_, _) =>
        {
            _config.Dock = mode;
            if (mode == DockMode.Locked)
            {
                _config.CustomX = Left;
                _config.CustomY = Top;
            }
            _config.Save();
            ResizeAndLayout();
        };
        parent.DropDownItems.Add(item);
    }

    private void AddVisibilityToggle(ToolStripMenuItem parent, string label, string id)
    {
        var item = new ToolStripMenuItem(label) { Checked = !_config.HiddenItems.Contains(id) };
        item.Click += (_, _) =>
        {
            if (_config.HiddenItems.Contains(id)) _config.HiddenItems.Remove(id);
            else _config.HiddenItems.Add(id);
            _config.Save();
            ResizeAndLayout();
            Invalidate();
        };
        parent.DropDownItems.Add(item);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyToggle);
            NativeMethods.UnregisterHotKey(Handle, HotkeyReset);
            NativeMethods.UnregisterHotKey(Handle, HotkeyMenu);
            _timer.Stop();
            _timer.Dispose();
            _font.Dispose();
            _symbolFont.Dispose();
            _dividerPen.Dispose();
            _pdh.Dispose();
            _hardware.Dispose();
        }
        base.Dispose(disposing);
    }
}