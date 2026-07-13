// WinTotal - Ultra-lightweight all-in-one Windows utility (Pure Black Edition)
// Real-time CPU/GPU/RAM/Disk monitor + full installed-app list + registry force-delete
// .NET Framework 4.8 (built into Windows) / single exe / zero dependencies
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace WinTotal
{
    public static class Program
    {
        public static bool DemoMode; // hide identifiable values (machine name) for screenshots

        [STAThread]
        public static void Main(string[] args)
        {
            int page = 0;
            string query = null;
            string lang = null;
            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg.StartsWith("--apps")) page = 1;
                    else if (arg == "--specs") page = 2;
                    else if (arg == "--fix") page = 3;
                    else if (arg == "--demo") DemoMode = true;
                    else if (arg == "--ko") lang = "ko";
                    else if (arg == "--en") lang = "en";
                    int eq = arg.IndexOf('=');
                    if (eq > 0 && eq < arg.Length - 1) query = arg.Substring(eq + 1);
                }
            }
            // language: --ko/--en override > saved preference > system UI language
            if (lang == null)
            {
                try
                {
                    using (var k = Registry.CurrentUser.OpenSubKey(@"Software\WinTotal"))
                        if (k != null) lang = Convert.ToString(k.GetValue("Language", ""));
                }
                catch { }
            }
            if (string.IsNullOrEmpty(lang))
                lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            L.Ko = lang == "ko";

            var app = new Application();
            app.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
            {
                try
                {
                    File.AppendAllText(
                        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WinTotal_error.log"),
                        DateTime.Now.ToString("s") + "  " + e.Exception + Environment.NewLine);
                }
                catch { }
                e.Handled = true; // keep the app alive; details go to the log
            };
            app.Run(new MainWindow(page, query));
        }
    }

    // ---------- Palette (Pure Black) ----------
    public static class Theme
    {
        public const string Bg          = "#000000"; // exact black
        public const string Card        = "#0C0C0E";
        public const string CardBorder  = "#1C1C21";
        public const string CardHover   = "#121216";
        public const string NavBg       = "#000000";
        public const string NavLine     = "#161619";
        public const string NavActive   = "#1B1B20";
        public const string NavHover    = "#111114";
        public const string TextHi      = "#FFFFFF";
        public const string TextMid     = "#9A9AA3";
        public const string TextLow     = "#5C5C66";
        public const string AccCpu      = "#0A84FF";
        public const string AccGpu      = "#BF5AF2";
        public const string AccRam      = "#30D158";
        public const string AccDisk     = "#FF9F0A";
        public const string BtnBg       = "#16202E";
        public const string BtnHover    = "#1D2C42";
        public const string BtnText     = "#6FB3FF";
        public const string DangerBg    = "#2A1215";
        public const string DangerHover = "#3B181D";
        public const string DangerText  = "#FF6B61";
        public const string InputBg     = "#0C0C0E";
    }

    // ---------- Localization (EN keys → KO) ----------
    public static class L
    {
        public static bool Ko;

        public static CultureInfo Culture
        {
            get { return new CultureInfo(Ko ? "ko-KR" : "en-US"); }
        }
        public static string ClockFormat { get { return Ko ? "M월 d일 dddd" : "ddd, MMM d"; } }
        public static string DateFormat { get { return Ko ? "yyyy년 M월 d일" : "MMM d, yyyy"; } }

        private static readonly Dictionary<string, string> KoMap = new Dictionary<string, string>
        {
            { "System manager", "시스템 종합 관리" },
            { "Dashboard", "대시보드" },
            { "System Specs", "시스템 사양" },
            { "Apps", "앱 관리" },
            { "v1.4 · single executable", "v1.4 · 단일 실행 파일" },
            { "Real-time System Monitor", "실시간 시스템 모니터" },
            { "Collecting system info...", "시스템 정보 수집 중..." },
            { "Memory", "메모리" },
            { "Disk", "디스크" },
            { "Disk ", "디스크 " },
            { "{0} processes · up {1}d {2}h {3}m", "프로세스 {0}개 · 가동 {1}일 {2}시간 {3}분" },
            { "{0:F1} / {1:F1} GB in use", "{0:F1} / {1:F1} GB 사용 중" },
            { "{0} {1:F0} GB free", "{0} {1:F0}GB 여유" },
            { "Top CPU processes", "CPU를 많이 쓰는 프로세스" },
            { "Top memory processes", "메모리를 많이 쓰는 프로세스" },
            { "Measuring...", "측정 중..." },
            { "Close gracefully (close request → wait → force kill only if needed)", "안전하게 종료 (닫기 요청 → 대기 → 필요 시 강제 종료)" },
            { "\"{0}\" is a critical Windows process and cannot be terminated.", "\"{0}\"은(는) Windows 필수 프로세스라 종료할 수 없습니다." },
            { "Graceful Close", "안전 종료" },
            { "Close \"{0}\" gracefully?\n\nStep 1 — a close request is sent (the app may ask you to save)\nStep 2 — if still running after 5 seconds, you will be asked to force kill", "\"{0}\" 프로세스를 안전하게 종료할까요?\n\n1단계 — 창 닫기 요청을 보냅니다 (앱이 저장 여부를 물어볼 수 있음)\n2단계 — 5초 안에 종료되지 않으면 강제 종료를 확인받습니다" },
            { "\"{0}\" closed gracefully.", "\"{0}\"이(가) 정상적으로 종료되었습니다." },
            { "{1} \"{0}\" process(es) are still running.\n(no window, or not responding to the close request)\n\nForce kill? Unsaved data may be lost.", "\"{0}\" 프로세스 {1}개가 아직 실행 중입니다.\n(창이 없거나 닫기 요청에 응답하지 않음)\n\n강제 종료할까요? 저장하지 않은 데이터는 사라질 수 있습니다." },
            { "Force Kill", "강제 종료" },
            { "Hardware & software details of this PC", "이 PC의 하드웨어 · 소프트웨어 정보" },
            { "Rescan", "재검사" },
            { "Scanning hardware...", "하드웨어 검사 중..." },
            { "Collecting specifications...", "사양 정보 수집 중..." },
            { "Scan failed — press Rescan to retry", "검사 실패 — 재검사를 눌러 다시 시도하세요" },
            { "Could not retrieve system specifications.", "사양 정보를 가져오지 못했습니다." },
            { "Last scan {0} · took {1:F1}s · {2} sections · {3} items verified", "마지막 검사 {0} · {1:F1}초 소요 · {2}개 섹션 · {3}개 항목 확인" },
            { "System", "시스템" },
            { "Manufacturer · Model", "제조사 · 모델" },
            { "Computer name", "컴퓨터 이름" },
            { "Operating system", "운영체제" },
            { "Version · Build", "버전 · 빌드" },
            { " · build ", " · 빌드 " },
            { "OS installed", "OS 설치일" },
            { "Processor (CPU)", "프로세서 (CPU)" },
            { "Model", "모델" },
            { "Cores · Threads", "코어 · 스레드" },
            { " cores · ", "코어 · " },
            { " threads", "스레드" },
            { "Base clock", "기본 클럭" },
            { " · L3 cache {0:F0} MB", " · L3 캐시 {0:F0} MB" },
            { "Socket", "소켓" },
            { "Graphics (GPU {0})", "그래픽 (GPU {0})" },
            { "Driver", "드라이버" },
            { "Resolution", "해상도" },
            { "Memory (RAM)", "메모리 (RAM)" },
            { "Total", "총 용량" },
            { "{0:F0} GB ({1} slots populated)", "{0:F0} GB ({1}개 슬롯 사용)" },
            { "Storage", "저장장치" },
            { " drive", " 드라이브" },
            { "{1:F0} GB free of {0:F0} GB ({2})", "{0:F0} GB 중 {1:F0} GB 여유 ({2})" },
            { "Motherboard · BIOS", "메인보드 · BIOS" },
            { "Motherboard", "메인보드" },
            { "Display", "디스플레이" },
            { "Monitor ", "모니터 " },
            { "Battery", "배터리" },
            { "Charge", "충전 상태" },
            { "Plugged in", "전원 연결됨" },
            { "On battery", "배터리 사용 중" },
            { "Battery health", "배터리 수명" },
            { "{0:F0}% (design {1:N0} mWh → full charge {2:N0} mWh)", "{0:F0}% (설계 {1:N0} mWh → 현재 완충 {2:N0} mWh)" },
            { "Security", "보안" },
            { "Secure Boot", "보안 부팅" },
            { "Enabled", "사용 중" },
            { "Disabled", "사용 안 함" },
            { " · version ", " · 버전 " },
            { "Network", "네트워크" },
            { "Connected", "연결됨" },
            { "Inactive", "비활성" },
            { "Installed Apps", "설치된 앱" },
            { "Loading...", "불러오는 중..." },
            { "Loading app list...", "앱 목록 불러오는 중..." },
            { "{0} apps ({1} desktop · {2} Store)", "총 {0}개 (데스크톱 {1} · 스토어 {2})" },
            { "Refresh", "새로고침" },
            { "Search apps by name or publisher", "앱 이름 또는 게시자 검색" },
            { "Unknown publisher", "게시자 정보 없음" },
            { "Microsoft Store app", "Microsoft Store 앱" },
            { "Uninstall", "제거" },
            { "Force Delete", "완전 삭제" },
            { "Uninstall Store app \"{0}\"?", "스토어 앱 \"{0}\"을(를) 제거할까요?" },
            { "Uninstall App", "앱 제거" },
            { "Uninstalling \"{0}\"...", "\"{0}\" 제거 중..." },
            { "\"{0}\" uninstalled", "\"{0}\" 제거 완료" },
            { "\"{0}\" has no uninstaller.\nForce-clean its registry entries and install folder?", "\"{0}\"에는 제거 프로그램이 없습니다.\n레지스트리와 설치 폴더를 강제로 정리할까요?" },
            { "No Uninstaller", "제거 프로그램 없음" },
            { "Uninstall \"{0}\"?\n\nAfter the uninstaller finishes, leftover registry entries are cleaned up automatically.", "\"{0}\"을(를) 제거할까요?\n\n제거 프로그램 실행 후 남은 레지스트리 항목까지 자동으로 정리합니다." },
            { "Running the uninstaller for \"{0}\"...", "\"{0}\" 제거 프로그램 실행 중..." },
            { "Failed to run the uninstaller: ", "제거 프로그램 실행 실패: " },
            { "\"{0}\" uninstalled · {1} leftover registry keys cleaned", "\"{0}\" 제거 완료 · 잔여 레지스트리 {1}개 정리" },
            { "The uninstall of \"{0}\" does not look complete (its registration is still present).\nForce-clean the registry?", "\"{0}\" 제거가 완료되지 않은 것 같습니다 (등록 정보가 남아 있음).\n레지스트리를 강제로 정리할까요?" },
            { "Leftovers", "잔여 항목" },
            { "Cancelled", "취소됨" },
            { "All traces of \"{0}\" will be force-deleted.\n\n", "\"{0}\"의 흔적을 강제로 삭제합니다.\n\n" },
            { "· Program registration (Uninstall registry key)", "· 프로그램 등록 정보 (Uninstall 레지스트리 키)" },
            { "· Leftover registry keys under HKLM / HKCU Software", "· HKLM / HKCU Software 아래 잔여 레지스트리 키" },
            { "· Install folder: ", "· 설치 폴더: " },
            { "This deletes immediately WITHOUT running the uninstaller. Continue?", "제거 프로그램을 실행하지 않고 바로 삭제합니다. 계속할까요?" },
            { "\"{0}\" force-deleted · {1} registry keys, {2} folders removed", "\"{0}\" 완전 삭제 완료 · 레지스트리 키 {1}개, 폴더 {2}개 제거" },
            { "Done", "완료" },
            { "All", "전체" },
            { "AI Tools", "AI 도구" },
            { "Development", "개발" },
            { "Games", "게임" },
            { "Internet & Chat", "인터넷·메신저" },
            { "Media & Creative", "미디어·창작" },
            { "Utilities", "유틸리티" },
            { "System Components", "시스템 구성요소" },
            { "Other", "기타" },
            { "Top GPU processes", "GPU를 많이 쓰는 프로세스" },
            { "No active GPU processes", "GPU 사용 중인 프로세스 없음" },
            { "Health Check", "상태 진단" },
            { "Run Check", "진단 실행" },
            { "Checking...", "진단 중..." },
            { "One-click hardware health diagnosis", "클릭 한 번으로 하드웨어 상태를 진단합니다" },
            { "Check failed — press Run Check to retry", "진단 실패 — 진단 실행을 눌러 다시 시도하세요" },
            { "Last check {0} · took {1:F1}s · {2} items", "마지막 진단 {0} · {1:F1}초 소요 · {2}개 항목" },
            { "{0} OK · {1} warning · {2} critical · {3} unknown", "정상 {0} · 주의 {1} · 위험 {2} · 확인 불가 {3}" },
            { "Healthy", "정상" },
            { "Warning", "주의" },
            { "Critical", "위험" },
            { "Unknown", "확인 불가" },
            { "SMART status: ", "SMART 상태: " },
            { "SSD wear", "SSD 마모도" },
            { "{0:F0}% worn", "{0:F0}% 마모" },
            { "Disk temperature", "디스크 온도" },
            { "free space", "여유 공간" },
            { "{0:F0} GB free ({1:F0}%)", "{0:F0} GB 여유 ({1:F0}%)" },
            { "GPU temperature", "GPU 온도" },
            { "GPU throttling", "GPU 스로틀링" },
            { "Thermal/power slowdown active", "온도/전력 스로틀링 발생 중" },
            { "Not throttling", "스로틀링 없음" },
            { "VRAM headroom", "VRAM 여유" },
            { "Power draw", "전력 사용" },
            { "nvidia-smi not available", "nvidia-smi를 사용할 수 없음" },
            { "Memory pressure", "메모리 압박" },
            { "{0}% in use ({1:F1} / {2:F1} GB)", "{0}% 사용 중 ({1:F1} / {2:F1} GB)" },
            { "Commit charge", "커밋 사용량" },
            { "Uptime", "가동 시간" },
            { "{0:F0} days without a reboot — consider restarting", "{0:F0}일째 재부팅 없음 — 재시작 권장" },
            { "{0:F1} days", "{0:F1}일" },
            { "Thermal zone", "온도 센서" },
            { "CPU temperature", "CPU 온도" },
            { "Not supported on this system", "이 시스템에서는 지원되지 않음" },
            { "Running", "실행 중" },
            { "End", "종료" },
            { "Close the running processes of \"{0}\"?\n\nA close request is sent first so the app can save; you will be asked again before any force kill.", "\"{0}\"의 실행 중인 프로세스를 종료할까요?\n\n먼저 닫기 요청을 보내 저장할 기회를 주고, 강제 종료 전에 다시 확인합니다." },
            { "Closing processes of \"{0}\"...", "\"{0}\" 프로세스 종료 중..." },
            { "{0} process(es) of \"{1}\" are still running.\nForce kill? Unsaved data may be lost.", "\"{1}\"의 프로세스 {0}개가 아직 실행 중입니다.\n강제 종료할까요? 저장하지 않은 데이터는 사라질 수 있습니다." },
            { "Processes of \"{0}\" closed.", "\"{0}\" 프로세스를 종료했습니다." },
            { "Quick Fixes", "빠른 수리" },
            { "One-click repairs for common Windows problems", "자주 겪는 윈도우 문제를 클릭 한 번으로 수리합니다" },
            { "Open Terminal", "터미널 열기" },
            { "Fix Internet", "인터넷 수리" },
            { "Flush DNS and reset the network stack (Winsock/IP). A reboot is recommended afterwards.", "DNS 캐시를 비우고 네트워크 스택(Winsock/IP)을 초기화합니다. 완료 후 재부팅을 권장합니다." },
            { "Free Disk Space", "디스크 공간 확보" },
            { "Delete temp files (user + Windows) and empty the Recycle Bin.", "임시 파일(사용자·Windows)을 삭제하고 휴지통을 비웁니다." },
            { "Repair System Files", "시스템 파일 복구" },
            { "Run sfc /scannow to find and fix corrupted system files. Can take 10+ minutes.", "sfc /scannow로 손상된 시스템 파일을 검사·복구합니다. 10분 이상 걸릴 수 있습니다." },
            { "Restart Explorer", "탐색기 재시작" },
            { "Restart the taskbar/desktop when they freeze or misbehave.", "작업표시줄·바탕화면이 멈추거나 이상할 때 재시작합니다." },
            { "Run", "실행" },
            { "Run \"{0}\"?\n\n{1}", "\"{0}\"을(를) 실행할까요?\n\n{1}" },
            { "Another fix is already running.", "다른 수리 작업이 실행 중입니다." },
            { "A reboot is recommended.", "재부팅을 권장합니다." },
            { "Recycle Bin emptied", "휴지통 비움" },
            { "Freed {0:F0} MB", "{0:F0} MB 확보" },
            { "Explorer restarted", "탐색기 재시작됨" },
            { "Done.", "완료." },
            { "Output will appear here.", "실행 결과가 여기에 표시됩니다." }
        };

        public static string T(string s)
        {
            if (!Ko || s == null) return s;
            string v;
            return KoMap.TryGetValue(s, out v) ? v : s;
        }
    }
    public static class Ui
    {
        public static SolidColorBrush Br(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        public static Color Col(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }

    // ---------- Real-time line chart (glow + gradient fill + grid) ----------
    public class LineChart : Grid
    {
        private readonly Polyline _line;
        private readonly Polyline _glow;
        private readonly Polygon _fill;
        private readonly Line[] _gridLines = new Line[3];
        private readonly List<double> _values = new List<double>();
        private const int Capacity = 60;

        public LineChart(Color c)
        {
            ClipToBounds = true;
            var gridBrush = new SolidColorBrush(Color.FromArgb(12, 255, 255, 255));
            for (int i = 0; i < 3; i++)
            {
                _gridLines[i] = new Line { Stroke = gridBrush, StrokeThickness = 1 };
                Children.Add(_gridLines[i]);
            }
            var gb = new LinearGradientBrush(
                Color.FromArgb(85, c.R, c.G, c.B),
                Color.FromArgb(0, c.R, c.G, c.B), 90);
            _fill = new Polygon { Fill = gb };
            _glow = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(55, c.R, c.G, c.B)),
                StrokeThickness = 6,
                StrokeLineJoin = PenLineJoin.Round
            };
            _line = new Polyline
            {
                Stroke = new SolidColorBrush(c),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };
            Children.Add(_fill);
            Children.Add(_glow);
            Children.Add(_line);
            SizeChanged += delegate { Redraw(); };
        }

        private bool _seeded;

        public void Push(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) v = 0;
            double cv = Math.Max(0, Math.Min(100, v));
            if (!_seeded)
            {
                // seed the full width with the first sample for a natural line from the start
                _seeded = true;
                for (int i = 0; i < Capacity; i++) _values.Add(cv);
            }
            else
            {
                _values.Add(cv);
                if (_values.Count > Capacity) _values.RemoveAt(0);
            }
            Redraw();
        }

        private void Redraw()
        {
            double w = ActualWidth, h = ActualHeight;
            if (w < 4 || h < 4) return;
            for (int i = 0; i < 3; i++)
            {
                double gy = h * 0.25 * (i + 1);
                _gridLines[i].X1 = 0; _gridLines[i].X2 = w;
                _gridLines[i].Y1 = gy; _gridLines[i].Y2 = gy;
            }
            if (_values.Count < 2) return;
            double step = w / (Capacity - 1);
            int n = _values.Count;
            var pts = new PointCollection();
            for (int i = 0; i < n; i++)
            {
                double x = w - (n - 1 - i) * step;
                double y = h - 2 - (_values[i] / 100.0) * (h - 6);
                pts.Add(new Point(x, y));
            }
            _line.Points = pts;
            _glow.Points = pts;
            var fp = new PointCollection(pts);
            fp.Insert(0, new Point(w - (n - 1) * step, h));
            fp.Add(new Point(w, h));
            _fill.Points = fp;
        }
    }

    // ---------- GPU usage (Task Manager style: max of per-engine-type sums) ----------
    public class GpuMonitor
    {
        private List<PerformanceCounter> _counters = new List<PerformanceCounter>();
        private DateTime _lastRebuild = DateTime.MinValue;
        public bool Available = true;
        public Dictionary<int, float> PidUsage = new Dictionary<int, float>(); // per-process GPU %

        public float Read()
        {
            if (!Available) return 0;
            try
            {
                if ((DateTime.Now - _lastRebuild).TotalSeconds > 15) Rebuild();
                var byType = new Dictionary<string, float>();
                var byPidType = new Dictionary<int, Dictionary<string, float>>();
                foreach (var c in _counters)
                {
                    float v;
                    try { v = c.NextValue(); }
                    catch { continue; }
                    string inst = c.InstanceName;
                    int idx = inst.IndexOf("engtype_", StringComparison.OrdinalIgnoreCase);
                    string t = idx >= 0 ? inst.Substring(idx) : "other";
                    if (!byType.ContainsKey(t)) byType[t] = 0;
                    byType[t] += v;

                    int pid = GpuMemMonitor.ParsePid(inst);
                    if (pid > 0 && v > 0)
                    {
                        Dictionary<string, float> slot;
                        if (!byPidType.TryGetValue(pid, out slot))
                        {
                            slot = new Dictionary<string, float>();
                            byPidType[pid] = slot;
                        }
                        float cur;
                        slot.TryGetValue(t, out cur);
                        slot[t] = cur + v;
                    }
                }
                var pidUsage = new Dictionary<int, float>();
                foreach (var kv in byPidType)
                {
                    float m = 0;
                    foreach (var tv in kv.Value) if (tv.Value > m) m = tv.Value;
                    pidUsage[kv.Key] = Math.Min(100f, m);
                }
                PidUsage = pidUsage;

                float max = 0;
                foreach (var kv in byType) if (kv.Value > max) max = kv.Value;
                return Math.Min(100f, max);
            }
            catch
            {
                Available = false;
                return 0;
            }
        }

        private void Rebuild()
        {
            foreach (var c in _counters) { try { c.Dispose(); } catch { } }
            _counters = new List<PerformanceCounter>();
            var cat = new PerformanceCounterCategory("GPU Engine");
            foreach (var inst in cat.GetInstanceNames())
            {
                try
                {
                    var pc = new PerformanceCounter("GPU Engine", "Utilization Percentage", inst);
                    pc.NextValue(); // prime the first sample (avoids 0)
                    _counters.Add(pc);
                }
                catch { }
            }
            _lastRebuild = DateTime.Now;
        }
    }

    // ---------- GPU memory (adapter total + per-process dedicated VRAM) ----------
    public class GpuMemMonitor
    {
        private List<PerformanceCounter> _adapter = new List<PerformanceCounter>();
        private List<PerformanceCounter> _process = new List<PerformanceCounter>();
        private DateTime _lastRebuild = DateTime.MinValue;
        public bool Available = true;
        public double TotalUsedBytes;
        public Dictionary<int, double> PidBytes = new Dictionary<int, double>();

        public void Read()
        {
            if (!Available) return;
            try
            {
                if ((DateTime.Now - _lastRebuild).TotalSeconds > 15) Rebuild();
                double total = 0;
                foreach (var c in _adapter)
                {
                    try { total += c.NextValue(); } catch { }
                }
                TotalUsedBytes = total;
                var pids = new Dictionary<int, double>();
                foreach (var c in _process)
                {
                    float v;
                    try { v = c.NextValue(); } catch { continue; }
                    int pid = ParsePid(c.InstanceName);
                    if (pid <= 0) continue;
                    double cur;
                    pids.TryGetValue(pid, out cur);
                    pids[pid] = cur + v;
                }
                PidBytes = pids;
            }
            catch { Available = false; }
        }

        // "pid_12345_luid_..." → 12345
        public static int ParsePid(string inst)
        {
            if (inst == null || !inst.StartsWith("pid_")) return -1;
            int end = inst.IndexOf('_', 4);
            if (end < 0) return -1;
            int pid;
            return int.TryParse(inst.Substring(4, end - 4), out pid) ? pid : -1;
        }

        private void Rebuild()
        {
            foreach (var c in _adapter) { try { c.Dispose(); } catch { } }
            foreach (var c in _process) { try { c.Dispose(); } catch { } }
            _adapter = new List<PerformanceCounter>();
            _process = new List<PerformanceCounter>();
            try
            {
                var cat = new PerformanceCounterCategory("GPU Adapter Memory");
                foreach (var inst in cat.GetInstanceNames())
                {
                    try
                    {
                        var pc = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", inst);
                        pc.NextValue();
                        _adapter.Add(pc);
                    }
                    catch { }
                }
            }
            catch { }
            try
            {
                var cat2 = new PerformanceCounterCategory("GPU Process Memory");
                foreach (var inst in cat2.GetInstanceNames())
                {
                    try
                    {
                        var pc = new PerformanceCounter("GPU Process Memory", "Dedicated Usage", inst);
                        pc.NextValue();
                        _process.Add(pc);
                    }
                    catch { }
                }
            }
            catch { }
            _lastRebuild = DateTime.Now;
        }
    }

    // ---------- nvidia-smi polling (temperature / power / clocks / VRAM / throttling) ----------
    public class NvSmi
    {
        public bool Available = true;
        public volatile bool HasData;
        public float TempC, PowerW, PowerLimitW, ClockMhz, MemUsedMB, MemTotalMB;
        public string DriverVer = "", GpuName = "", ThrottleHex = "";
        private string _exe;

        private string FindExe()
        {
            var candidates = new string[]
            {
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvidia-smi.exe"),
                @"C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe"
            };
            foreach (var cand in candidates)
            {
                try { if (File.Exists(cand)) return cand; }
                catch { }
            }
            return null;
        }

        public void Refresh()
        {
            if (!Available) return;
            try
            {
                if (_exe == null)
                {
                    _exe = FindExe();
                    if (_exe == null) { Available = false; return; }
                }
                var psi = new ProcessStartInfo
                {
                    FileName = _exe,
                    Arguments = "--query-gpu=temperature.gpu,power.draw,power.limit,clocks.sm,memory.used,memory.total,driver_version,name,clocks_throttle_reasons.active --format=csv,noheader,nounits",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                string line;
                using (var p = Process.Start(psi))
                {
                    line = p.StandardOutput.ReadLine();
                    p.WaitForExit(5000);
                }
                if (string.IsNullOrEmpty(line)) { Available = false; return; }
                var parts = line.Split(',');
                if (parts.Length < 9) return;
                TempC = ParseF(parts[0]);
                PowerW = ParseF(parts[1]);
                PowerLimitW = ParseF(parts[2]);
                ClockMhz = ParseF(parts[3]);
                MemUsedMB = ParseF(parts[4]);
                MemTotalMB = ParseF(parts[5]);
                DriverVer = parts[6].Trim();
                GpuName = parts[7].Trim();
                ThrottleHex = parts[8].Trim();
                HasData = true;
            }
            catch { Available = false; }
        }

        private static float ParseF(string s)
        {
            float v;
            return float.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : -1;
        }
    }
    // ---------- Spec section ----------
    public class SpecSection
    {
        public string Title;
        public string Accent;
        public List<string[]> Rows = new List<string[]>();
    }

    // ---------- Health check items ----------
    public class HealthItem
    {
        public string Name;
        public string Detail;
        public int Level; // 0 ok · 1 warning · 2 critical · 3 unknown
    }

    public class HealthSection
    {
        public string Title;
        public List<HealthItem> Items = new List<HealthItem>();
    }

    // ---------- App entry ----------
    public class AppEntry
    {
        public string Name;
        public string Version;
        public string Publisher;
        public string UninstallString;
        public string InstallLocation;
        public string KeyPath;
        public RegistryHive Hive;
        public RegistryView View;
        public double SizeMB;
        public bool IsStore;
        public string PackageFullName;
        public string Category;
        public List<int> RunningPids;
    }

    // ---------- Main window ----------
    public class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string root, uint flags);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MemStatusEx buf);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MemStatusEx
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MemStatusEx));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        // monitoring
        private PerformanceCounter _cpu;
        private PerformanceCounter _disk;
        private GpuMonitor _gpu = new GpuMonitor();
        private GpuMemMonitor _gpuMem = new GpuMemMonitor();
        private NvSmi _nv = new NvSmi();
        private bool _nvBusy;
        private double _gpuUsageCache;
        private bool _gpuSampleBusy;
        private DispatcherTimer _timer;
        private int _tickCount;

        // dashboard UI
        private TextBlock _cpuVal, _cpuSub, _gpuVal, _gpuSub, _ramVal, _ramSub, _diskVal, _diskSub;
        private LineChart _cpuChart, _gpuChart, _ramChart, _diskChart;
        private TextBlock _headerInfo, _clockTime, _clockDate;
        private string _infoLine, _gpuName;

        // top-process tracking
        private StackPanel _topCpuPanel, _topRamPanel, _topGpuPanel;
        private Dictionary<int, double> _prevCpuMs = new Dictionary<int, double>();
        private DateTime _prevProcSample = DateTime.MinValue;

        // app list UI
        private StackPanel _appListPanel;
        private WrapPanel _chipsPanel;
        private string _activeCategory = "All";
        private TextBox _searchBox;
        private TextBlock _searchPlaceholder;
        private TextBlock _appCountText, _statusText;
        private List<AppEntry> _apps = new List<AppEntry>();
        private bool _appsLoaded;
        private readonly int _startPage;
        private readonly string _startQuery;

        // pages
        private Grid _dashboardPage, _appsPage, _specsPage;
        private Border _navDash, _navApps, _navSpecs;
        private StackPanel _specsPanel;
        private TextBlock _specsStatus;
        private bool _specsLoaded;
        private bool _specsScanning;

        // Health check (compact card at the bottom of the dashboard)
        private Ellipse _healthDot;
        private TextBlock _healthSummary, _healthStatus, _healthChevron;
        private StackPanel _healthDetail;
        private ScrollViewer _healthDetailScroll;
        private List<HealthSection> _healthResults;
        private bool _healthRunning, _healthExpanded;

        // Quick Fixes UI
        private Grid _fixesPage;
        private Border _navFixes;
        private TextBox _fixConsole;
        private bool _fixRunning;

        private static readonly string[] ProtectedKeys = new string[]
        {
            "microsoft", "windows", "wow6432node", "classes", "policies", "clients",
            "intel", "nvidia", "amd", "realtek", "google", "apple inc.", "oracle",
            "regedit", "odbc", "wbem", "defaults", "khronos", "mozilla"
        };

        public MainWindow(int startPage, string startQuery)
        {
            _startPage = startPage;
            _startQuery = startQuery;
            Title = "WinTotal";
            Width = 1120; Height = 700;
            MinWidth = 900; MinHeight = 580;
            Background = Ui.Br(Theme.Bg);
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI, Malgun Gothic");
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);

            Resources.MergedDictionaries.Add(BuildScrollBarStyle());
            TrySetWindowIcon();
            Content = BuildLayout();

            SourceInitialized += delegate
            {
                try
                {
                    var hwnd = new WindowInteropHelper(this).Handle;
                    int on = 1;
                    DwmSetWindowAttribute(hwnd, 20, ref on, 4);       // dark mode
                    int black = 0x00000000;                            // COLORREF black
                    DwmSetWindowAttribute(hwnd, 35, ref black, 4);     // caption color = exact black
                    DwmSetWindowAttribute(hwnd, 34, ref black, 4);     // border color = black
                    int white = 0x00FFFFFF;
                    DwmSetWindowAttribute(hwnd, 36, ref white, 4);     // caption text = white
                }
                catch { }
            };
            Loaded += delegate
            {
                StartMonitoring();
                if (_startPage != 0) ShowPage(_startPage);
                if (!string.IsNullOrEmpty(_startQuery)) _searchBox.Text = _startQuery;
            };
        }

        private void TrySetWindowIcon()
        {
            try
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                var ico = System.Drawing.Icon.ExtractAssociatedIcon(exe);
                if (ico != null)
                    Icon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch { }
        }

        // thin dark scrollbar
        private static ResourceDictionary BuildScrollBarStyle()
        {
            const string xaml = @"<ResourceDictionary
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
              <Style TargetType='ScrollBar'>
                <Setter Property='Width' Value='9'/>
                <Setter Property='Background' Value='Transparent'/>
                <Setter Property='Template'>
                  <Setter.Value>
                    <ControlTemplate TargetType='ScrollBar'>
                      <Grid Background='Transparent'>
                        <Track x:Name='PART_Track' IsDirectionReversed='True'>
                          <Track.Thumb>
                            <Thumb>
                              <Thumb.Template>
                                <ControlTemplate TargetType='Thumb'>
                                  <Border Background='#2A2A31' CornerRadius='4' Margin='2,1,1,1'/>
                                </ControlTemplate>
                              </Thumb.Template>
                            </Thumb>
                          </Track.Thumb>
                        </Track>
                      </Grid>
                    </ControlTemplate>
                  </Setter.Value>
                </Setter>
              </Style>
            </ResourceDictionary>";
            return (ResourceDictionary)XamlReader.Parse(xaml);
        }

        // ================= Layout =================
        private UIElement BuildLayout()
        {
            var root = new DockPanel();

            // ----- Left navigation -----
            var nav = new Border
            {
                Width = 208,
                Background = Ui.Br(Theme.NavBg),
                BorderBrush = Ui.Br(Theme.NavLine),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };
            var navDock = new DockPanel { Margin = new Thickness(14, 24, 14, 16) };

            // logo
            var logoRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 28) };
            var logoBadge = new Border
            {
                Width = 36, Height = 36,
                CornerRadius = new CornerRadius(10),
                Background = new LinearGradientBrush(Ui.Col(Theme.AccCpu), Ui.Col(Theme.AccGpu), 45)
            };
            logoBadge.Child = new TextBlock
            {
                Text = "",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 17,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var logoText = new StackPanel { Margin = new Thickness(11, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            logoText.Children.Add(new TextBlock
            {
                Text = "WinTotal",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            logoText.Children.Add(new TextBlock
            {
                Text = L.T("System manager"),
                FontSize = 10.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(1, 1, 0, 0)
            });
            logoRow.Children.Add(logoBadge);
            logoRow.Children.Add(logoText);
            DockPanel.SetDock(logoRow, Dock.Top);
            navDock.Children.Add(logoRow);

            // footer
            var bottomInfo = new StackPanel { Margin = new Thickness(0, 0, 0, 2) };
            var langBtn = new Border
            {
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(11, 7, 11, 8),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var langStack = new StackPanel { Orientation = Orientation.Horizontal };
            langStack.Children.Add(new TextBlock
            {
                Text = ((char)0xE774).ToString(), // MDL2 Globe
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 13,
                Foreground = Ui.Br(Theme.TextMid),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 10, 0)
            });
            langStack.Children.Add(new TextBlock
            {
                Text = L.Ko ? "English" : "한국어", // shows the language you would switch TO
                FontSize = 12.5,
                Foreground = Ui.Br(Theme.TextMid),
                VerticalAlignment = VerticalAlignment.Center
            });
            langBtn.Child = langStack;
            langBtn.MouseEnter += delegate { langBtn.Background = Ui.Br(Theme.NavHover); };
            langBtn.MouseLeave += delegate { langBtn.Background = Brushes.Transparent; };
            langBtn.MouseLeftButtonUp += delegate { SwitchLanguage(); };
            bottomInfo.Children.Add(langBtn);
            bottomInfo.Children.Add(new TextBlock
            {
                Text = L.T("v1.4 · single executable"),
                FontSize = 10.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(13, 0, 0, 0)
            });
            DockPanel.SetDock(bottomInfo, Dock.Bottom);
            navDock.Children.Add(bottomInfo);

            // menu
            var menu = new StackPanel();
            _navDash = NavItem("", L.T("Dashboard"), delegate { ShowPage(0); });
            _navSpecs = NavItem("", L.T("System Specs"), delegate { ShowPage(2); });
            _navApps = NavItem("", L.T("Apps"), delegate { ShowPage(1); });
            menu.Children.Add(_navDash);
            menu.Children.Add(_navSpecs);
            _navFixes = NavItem(((char)0xE90F).ToString(), L.T("Quick Fixes"), delegate { ShowPage(3); });
            menu.Children.Add(_navFixes);
            menu.Children.Add(_navApps);
            navDock.Children.Add(menu);

            nav.Child = navDock;
            DockPanel.SetDock(nav, Dock.Left);
            root.Children.Add(nav);

            // ----- Content area -----
            var content = new Grid();
            _dashboardPage = BuildDashboardPage();
            _appsPage = BuildAppsPage();
            _specsPage = BuildSpecsPage();
            _fixesPage = BuildFixesPage();
            content.Children.Add(_dashboardPage);
            content.Children.Add(_appsPage);
            content.Children.Add(_specsPage);
            content.Children.Add(_fixesPage);
            root.Children.Add(content);

            ShowPage(0);
            return root;
        }

        private Border NavItem(string glyph, string text, Action onClick)
        {
            var b = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(13, 11, 13, 11),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 15,
                Foreground = Ui.Br(Theme.TextMid),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 11, 0)
            });
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 13.5,
                Foreground = Ui.Br(Theme.TextMid),
                VerticalAlignment = VerticalAlignment.Center
            });
            b.Child = sp;
            b.MouseLeftButtonUp += delegate { onClick(); };
            b.MouseEnter += delegate
            {
                if (b.Tag as string != "active") b.Background = Ui.Br(Theme.NavHover);
            };
            b.MouseLeave += delegate
            {
                if (b.Tag as string != "active") b.Background = Brushes.Transparent;
            };
            return b;
        }

        private void ShowPage(int idx)
        {
            _dashboardPage.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
            _appsPage.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
            _specsPage.Visibility = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
            _fixesPage.Visibility = idx == 3 ? Visibility.Visible : Visibility.Collapsed;
            SetNavActive(_navDash, idx == 0);
            SetNavActive(_navApps, idx == 1);
            SetNavActive(_navSpecs, idx == 2);
            SetNavActive(_navFixes, idx == 3);
            if (idx == 1 && !_appsLoaded)
            {
                _appsLoaded = true;
                LoadAppsAsync();
            }
            if (idx == 2 && !_specsLoaded)
            {
                _specsLoaded = true;
                LoadSpecsAsync();
            }
        }

        private void SetNavActive(Border b, bool active)
        {
            b.Tag = active ? "active" : null;
            b.Background = active ? Ui.Br(Theme.NavActive) : Brushes.Transparent;
            var sp = (StackPanel)b.Child;
            var icon = (TextBlock)sp.Children[0];
            var label = (TextBlock)sp.Children[1];
            icon.Foreground = active ? Brushes.White : Ui.Br(Theme.TextMid);
            label.Foreground = active ? Brushes.White : Ui.Br(Theme.TextMid);
            label.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
        }

        private void SwitchLanguage()
        {
            L.Ko = !L.Ko;
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(@"Software\WinTotal"))
                    if (k != null) k.SetValue("Language", L.Ko ? "ko" : "en");
            }
            catch { }
            _appsLoaded = false;
            _specsLoaded = false;
            _specsScanning = false;
            _healthRunning = false;
            _apps = new List<AppEntry>();
            Content = BuildLayout(); // rebuild the whole UI in the new language
            RunHealthCheck();
        }

        // ================= Dashboard =================
        private Grid BuildDashboardPage()
        {
            var page = new Grid { Margin = new Thickness(24, 20, 24, 24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // header (title left / clock right) — Grid keeps the clock visible at narrow widths
            var headRow = new Grid { Margin = new Thickness(8, 0, 8, 14) };
            headRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var headLeft = new StackPanel();
            headLeft.Children.Add(new TextBlock
            {
                Text = L.T("Real-time System Monitor"),
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            _headerInfo = new TextBlock
            {
                Text = L.T("Collecting system info..."),
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(1, 5, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            headLeft.Children.Add(_headerInfo);
            Grid.SetColumn(headLeft, 0);
            headRow.Children.Add(headLeft);

            var clock = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16, 0, 0, 0)
            };
            _clockTime = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                FontSize = 21,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _clockDate = new TextBlock
            {
                Text = DateTime.Now.ToString(L.ClockFormat, L.Culture),
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextLow),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 1, 0)
            };
            clock.Children.Add(_clockTime);
            clock.Children.Add(_clockDate);
            Grid.SetColumn(clock, 1);
            headRow.Children.Add(clock);
            Grid.SetRow(headRow, 0);
            page.Children.Add(headRow);

            // 2x2 cards
            var grid = new UniformGrid { Columns = 2 };
            grid.Children.Add(MakeCard("CPU", Theme.AccCpu, out _cpuVal, out _cpuSub, out _cpuChart));
            grid.Children.Add(MakeCard("GPU", Theme.AccGpu, out _gpuVal, out _gpuSub, out _gpuChart));
            grid.Children.Add(MakeCard(L.T("Memory"), Theme.AccRam, out _ramVal, out _ramSub, out _ramChart));
            grid.Children.Add(MakeCard(L.T("Disk"), Theme.AccDisk, out _diskVal, out _diskSub, out _diskChart));
            Grid.SetRow(grid, 2);
            page.Children.Add(grid);

            // top-process card (what is using resources)
            var procCard = new Border
            {
                Background = Ui.Br(Theme.Card),
                BorderBrush = Ui.Br(Theme.CardBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(22, 15, 22, 14),
                Margin = new Thickness(8, 8, 8, 0)
            };
            var procGrid = new Grid();
            procGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            procGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            procGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            procGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            procGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _topCpuPanel = MakeTopColumn(procGrid, 0, L.T("Top CPU processes"), Theme.AccCpu);
            _topGpuPanel = MakeTopColumn(procGrid, 2, L.T("Top GPU processes"), Theme.AccGpu);
            _topRamPanel = MakeTopColumn(procGrid, 4, L.T("Top memory processes"), Theme.AccRam);

            procCard.Child = procGrid;
            Grid.SetRow(procCard, 3);
            page.Children.Add(procCard);

            var healthCard = BuildHealthCard();
            Grid.SetRow(healthCard, 1);
            page.Children.Add(healthCard);

            return page;
        }

        private StackPanel MakeTopColumn(Grid host, int col, string title, string accent)
        {
            var stack = new StackPanel();
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            titleRow.Children.Add(new Ellipse
            {
                Width = 7, Height = 7,
                Fill = Ui.Br(accent),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 8, 0)
            });
            titleRow.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ui.Br(Theme.TextMid)
            });
            stack.Children.Add(titleRow);
            var listPanel = new StackPanel();
            listPanel.Children.Add(new TextBlock
            {
                Text = L.T("Measuring..."),
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(15, 2, 0, 2)
            });
            stack.Children.Add(listPanel);
            Grid.SetColumn(stack, col);
            host.Children.Add(stack);
            return listPanel;
        }

        private UIElement ProcRow(string name, string valueText, double ratio, string accent)
        {
            var g = new Grid { Margin = new Thickness(15, 0, 0, 6), MinHeight = 16 };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            var nm = new TextBlock
            {
                Text = name,
                FontSize = 11.5,
                Foreground = Ui.Br("#C9C9D1"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(nm, 0);
            g.Children.Add(nm);

            var track = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                Background = Ui.Br("#1B1B20"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var fillHost = new Grid();
            var c = Ui.Col(accent);
            var fill = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(c),
                Width = 0
            };
            fillHost.Children.Add(track);
            fillHost.Children.Add(fill);
            fillHost.SizeChanged += delegate
            {
                fill.Width = Math.Max(0, Math.Min(1, ratio)) * fillHost.ActualWidth;
            };
            Grid.SetColumn(fillHost, 1);
            g.Children.Add(fillHost);

            var val = new TextBlock
            {
                Text = valueText,
                FontSize = 11,
                Foreground = Ui.Br(Theme.TextMid),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(val, 2);
            g.Children.Add(val);

            // graceful-close button (X)
            var killBtn = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(5, 3, 5, 3),
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                ToolTip = L.T("Close gracefully (close request → wait → force kill only if needed)")
            };
            var killGlyph = new TextBlock
            {
                Text = ((char)0xE711).ToString(), // MDL2 Cancel(X)
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 10,
                Foreground = Ui.Br(Theme.TextLow)
            };
            killBtn.Child = killGlyph;
            killBtn.MouseEnter += delegate
            {
                killBtn.Background = Ui.Br(Theme.DangerBg);
                killGlyph.Foreground = Ui.Br(Theme.DangerText);
            };
            killBtn.MouseLeave += delegate
            {
                killBtn.Background = Brushes.Transparent;
                killGlyph.Foreground = Ui.Br(Theme.TextLow);
            };
            string procName = name;
            killBtn.MouseLeftButtonUp += delegate { SafeKill(procName); };
            Grid.SetColumn(killBtn, 3);
            g.Children.Add(killBtn);
            return g;
        }

        // ---------- Graceful close: close request → 5s wait → confirmed force kill ----------
        private static readonly string[] CriticalProcs = new string[]
        {
            "system", "idle", "secure system", "csrss", "wininit", "winlogon", "lsass",
            "services", "smss", "svchost", "dwm", "fontdrvhost", "registry",
            "memory compression", "memcompression", "runtimebroker", "sihost",
            "taskhostw", "ctfmon", "explorer", "audiodg", "conhost",
            "wintotal", "wintotal_dbg"
        };

        private void SafeKill(string procName)
        {
            string low = procName.ToLowerInvariant();
            foreach (var c in CriticalProcs)
                if (low == c)
                {
                    MessageBox.Show(this,
                        string.Format(L.T("\"{0}\" is a critical Windows process and cannot be terminated."), procName),
                        L.T("Graceful Close"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

            if (MessageBox.Show(this,
                string.Format(L.T("Close \"{0}\" gracefully?\n\nStep 1 — a close request is sent (the app may ask you to save)\nStep 2 — if still running after 5 seconds, you will be asked to force kill"), procName),
                L.T("Graceful Close"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Task.Run(delegate
            {
                Process[] ps;
                try { ps = Process.GetProcessesByName(procName); }
                catch { return; }
                if (ps.Length == 0) return;

                foreach (var p in ps)
                {
                    try { if (p.MainWindowHandle != IntPtr.Zero) p.CloseMainWindow(); }
                    catch { }
                }

                // wait up to 5 seconds
                for (int i = 0; i < 10; i++)
                {
                    System.Threading.Thread.Sleep(500);
                    bool anyAlive = false;
                    foreach (var p in ps)
                    {
                        try { if (!p.HasExited) { anyAlive = true; break; } }
                        catch { }
                    }
                    if (!anyAlive) break;
                }

                var alive = new List<Process>();
                foreach (var p in ps)
                {
                    try { if (!p.HasExited) alive.Add(p); }
                    catch { }
                }

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (alive.Count == 0)
                    {
                        MessageBox.Show(this,
                            string.Format(L.T("\"{0}\" closed gracefully."), procName),
                            L.T("Graceful Close"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        if (MessageBox.Show(this,
                            string.Format(L.T("{1} \"{0}\" process(es) are still running.\n(no window, or not responding to the close request)\n\nForce kill? Unsaved data may be lost."), procName, alive.Count),
                            L.T("Force Kill"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            foreach (var p in alive)
                            {
                                try { p.Kill(); }
                                catch { }
                            }
                        }
                    }
                    UpdateTopProcs();
                }));
            });
        }

        private void UpdateTopProcs()
        {
            var now = DateTime.UtcNow;
            double elapsedMs = _prevProcSample == DateTime.MinValue
                ? 0 : (now - _prevProcSample).TotalMilliseconds;

            var cur = new Dictionary<int, double>();
            var pidName = new Dictionary<int, string>();
            // aggregate by name: [0]=cpu delta ms, [1]=ram bytes
            var agg = new Dictionary<string, double[]>();
            Process[] procs;
            try { procs = Process.GetProcesses(); } catch { return; }
            foreach (var p in procs)
            {
                string name;
                int pid;
                double ms = -1, ram = 0;
                try
                {
                    pid = p.Id;
                    name = p.ProcessName;
                    try { ms = p.TotalProcessorTime.TotalMilliseconds; } catch { }
                    try { ram = p.WorkingSet64; } catch { }
                }
                catch { continue; }
                finally { try { p.Dispose(); } catch { } }
                if (ms >= 0) cur[pid] = ms;
                pidName[pid] = name;

                double delta = 0;
                double prev;
                if (elapsedMs > 0 && ms >= 0 && _prevCpuMs.TryGetValue(pid, out prev))
                    delta = Math.Max(0, ms - prev);

                double[] slot;
                if (!agg.TryGetValue(name, out slot)) { slot = new double[2]; agg[name] = slot; }
                slot[0] += delta;
                slot[1] += ram;
            }
            _prevCpuMs = cur;
            _prevProcSample = now;
            if (elapsedMs <= 0) return;

            double denom = elapsedMs * Environment.ProcessorCount;

            var topCpu = agg.OrderByDescending(kv => kv.Value[0]).Take(5).ToList();
            var topRam = agg.OrderByDescending(kv => kv.Value[1]).Take(5).ToList();

            _topCpuPanel.Children.Clear();
            double maxCpu = 0.01;
            foreach (var kv in topCpu) maxCpu = Math.Max(maxCpu, kv.Value[0] / denom * 100.0);
            foreach (var kv in topCpu)
            {
                double pct = kv.Value[0] / denom * 100.0;
                _topCpuPanel.Children.Add(ProcRow(kv.Key,
                    string.Format("{0:F1}%", pct), pct / maxCpu, Theme.AccCpu));
            }

            _topRamPanel.Children.Clear();
            double maxRam = 1;
            foreach (var kv in topRam) maxRam = Math.Max(maxRam, kv.Value[1]);
            foreach (var kv in topRam)
            {
                double mb = kv.Value[1] / 1048576.0;
                string txt = mb >= 1024
                    ? string.Format("{0:F1} GB", mb / 1024.0)
                    : string.Format("{0:F0} MB", mb);
                _topRamPanel.Children.Add(ProcRow(kv.Key, txt, kv.Value[1] / maxRam, Theme.AccRam));
            }

            // Top GPU processes (usage % from GPU Engine, VRAM from GPU Process Memory)
            var gpuAgg = new Dictionary<string, double[]>(); // name → [usage %, vram bytes]
            foreach (var kv in _gpu.PidUsage)
            {
                string nm;
                if (!pidName.TryGetValue(kv.Key, out nm)) continue;
                double[] slot;
                if (!gpuAgg.TryGetValue(nm, out slot)) { slot = new double[2]; gpuAgg[nm] = slot; }
                slot[0] += kv.Value;
            }
            foreach (var kv in _gpuMem.PidBytes)
            {
                string nm;
                if (!pidName.TryGetValue(kv.Key, out nm)) continue;
                double[] slot;
                if (!gpuAgg.TryGetValue(nm, out slot)) { slot = new double[2]; gpuAgg[nm] = slot; }
                slot[1] += kv.Value;
            }
            var topGpu = gpuAgg.Where(kv => kv.Value[0] >= 0.5 || kv.Value[1] > 64 * 1048576.0)
                               .OrderByDescending(kv => kv.Value[0])
                               .ThenByDescending(kv => kv.Value[1])
                               .Take(5).ToList();
            _topGpuPanel.Children.Clear();
            if (topGpu.Count == 0)
            {
                _topGpuPanel.Children.Add(new TextBlock
                {
                    Text = L.T("No active GPU processes"),
                    FontSize = 11.5,
                    Foreground = Ui.Br(Theme.TextLow),
                    Margin = new Thickness(15, 2, 0, 2)
                });
            }
            else
            {
                double maxG = 0.01;
                foreach (var kv in topGpu) maxG = Math.Max(maxG, kv.Value[0]);
                foreach (var kv in topGpu)
                {
                    string gtxt = kv.Value[1] > 0
                        ? string.Format("{0:F0}% · {1:F1} GB", kv.Value[0], kv.Value[1] / 1073741824.0)
                        : string.Format("{0:F0}%", kv.Value[0]);
                    _topGpuPanel.Children.Add(ProcRow(kv.Key, gtxt, Math.Min(1.0, kv.Value[0] / maxG), Theme.AccGpu));
                }
            }
        }

        private Border MakeCard(string title, string hex, out TextBlock big, out TextBlock sub, out LineChart chart)
        {
            var c = Ui.Col(hex);
            var card = new Border
            {
                Background = Ui.Br(Theme.Card),
                BorderBrush = Ui.Br(Theme.CardBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(22, 18, 22, 16),
                Margin = new Thickness(8)
            };
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // title row
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
            titleRow.Children.Add(new Ellipse
            {
                Width = 7, Height = 7,
                Fill = new SolidColorBrush(c),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 8, 0)
            });
            titleRow.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ui.Br(Theme.TextMid)
            });
            Grid.SetRow(titleRow, 0);
            g.Children.Add(titleRow);

            // value row
            var valRow = new DockPanel { Margin = new Thickness(0, 8, 0, 0), LastChildFill = false };
            big = new TextBlock { Foreground = Brushes.White };
            SetBigValue(big, "--", "");
            sub = new TextBlock
            {
                Text = "",
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(12, 0, 0, 7),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            DockPanel.SetDock(big, Dock.Left);
            DockPanel.SetDock(sub, Dock.Right);
            valRow.Children.Add(big);
            valRow.Children.Add(sub);
            Grid.SetRow(valRow, 1);
            g.Children.Add(valRow);

            // chart
            chart = new LineChart(c) { Margin = new Thickness(0, 12, 0, 0), MinHeight = 56 };
            Grid.SetRow(chart, 2);
            g.Children.Add(chart);

            card.Child = g;
            return card;
        }

        private static void SetBigValue(TextBlock tb, string num, string unit)
        {
            tb.Inlines.Clear();
            var r1 = new Run(num)
            {
                FontSize = 38,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            tb.Inlines.Add(r1);
            if (!string.IsNullOrEmpty(unit))
            {
                var r2 = new Run(unit)
                {
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Ui.Br(Theme.TextLow)
                };
                tb.Inlines.Add(r2);
            }
        }

        private void StartMonitoring()
        {
            try { _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total"); _cpu.NextValue(); }
            catch { _cpu = null; }
            try { _disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total"); _disk.NextValue(); }
            catch { _disk = null; }

            // system info (background)
            Task.Run(delegate
            {
                string cpuName = "", gpuName = "";
                try
                {
                    using (var k = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                        if (k != null) cpuName = Convert.ToString(k.GetValue("ProcessorNameString", "")).Trim();
                }
                catch { }
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                    {
                        var names = new List<string>();
                        foreach (ManagementObject mo in searcher.Get())
                        {
                            var n = Convert.ToString(mo["Name"]);
                            if (!string.IsNullOrEmpty(n) && !n.ToLower().Contains("basic")) names.Add(n);
                        }
                        gpuName = string.Join(" / ", names);
                    }
                }
                catch { }
                var mem = new MemStatusEx();
                double totalGb = 0;
                if (GlobalMemoryStatusEx(mem)) totalGb = mem.ullTotalPhys / 1073741824.0;

                string info = string.Format("{0}   ·   {1}   ·   RAM {2:F0} GB",
                    cpuName, gpuName, totalGb);
                string gsub = gpuName;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    int cut = gsub.IndexOf(" / ");
                    _infoLine = info;
                    _gpuName = cut > 0 ? gsub.Substring(0, cut) : gsub;
                    _headerInfo.Text = _infoLine;
                    _gpuSub.Text = _gpuName;
                }));
            });

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            _timer.Tick += delegate { Tick(); };
            _timer.Start();

            RunHealthCheck(); // initial diagnosis; re-runs every 10 minutes from Tick
        }

        private void Tick()
        {
            _tickCount++;

            _clockTime.Text = DateTime.Now.ToString("HH:mm:ss");
            if (_tickCount % 30 == 1)
                _clockDate.Text = DateTime.Now.ToString(L.ClockFormat, L.Culture);

            // CPU
            double cpu = 0;
            if (_cpu != null) { try { cpu = _cpu.NextValue(); } catch { } }
            SetBigValue(_cpuVal, string.Format("{0:F0}", cpu), "%");
            _cpuChart.Push(cpu);
            if (_tickCount % 5 == 1)
            {
                if (_infoLine != null) _headerInfo.Text = _infoLine;
                try
                {
                    var up = TimeSpan.FromMilliseconds(GetTickCount64());
                    _cpuSub.Text = string.Format(L.T("{0} processes · up {1}d {2}h {3}m"),
                        Process.GetProcesses().Length, up.Days, up.Hours, up.Minutes);
                }
                catch { }
            }

            // GPU — sampled on a background thread (counter rebuilds can stall for seconds)
            if (!_gpuSampleBusy)
            {
                _gpuSampleBusy = true;
                Task.Run(delegate
                {
                    _gpuUsageCache = _gpu.Read();
                    _gpuMem.Read();
                    _gpuSampleBusy = false;
                });
            }
            double gpu = _gpuUsageCache;
            SetBigValue(_gpuVal, _gpu.Available ? string.Format("{0:F0}", gpu) : "N/A", _gpu.Available ? "%" : "");
            _gpuChart.Push(gpu);
            if (_tickCount % 3 == 0 && _nv.Available && !_nvBusy)
            {
                _nvBusy = true;
                Task.Run(delegate { _nv.Refresh(); _nvBusy = false; });
            }
            if (_nv.HasData)
                _gpuSub.Text = string.Format("{0:F0}°C · {1:F0} W · {2:F1} / {3:F1} GB",
                    _nv.TempC, _nv.PowerW, _nv.MemUsedMB / 1024.0, _nv.MemTotalMB / 1024.0);
            else if (_gpuMem.Available && _gpuMem.TotalUsedBytes > 0)
                _gpuSub.Text = string.Format("VRAM {0:F1} GB", _gpuMem.TotalUsedBytes / 1073741824.0);
            else if (_gpuName != null)
                _gpuSub.Text = _gpuName;

            // RAM
            var mem = new MemStatusEx();
            if (GlobalMemoryStatusEx(mem))
            {
                double total = mem.ullTotalPhys / 1073741824.0;
                double used = (mem.ullTotalPhys - mem.ullAvailPhys) / 1073741824.0;
                double pct = mem.dwMemoryLoad;
                SetBigValue(_ramVal, string.Format("{0:F0}", pct), "%");
                _ramSub.Text = string.Format(L.T("{0:F1} / {1:F1} GB in use"), used, total);
                _ramChart.Push(pct);
            }

            // Disk
            double disk = 0;
            if (_disk != null) { try { disk = Math.Min(100, _disk.NextValue()); } catch { } }
            SetBigValue(_diskVal, string.Format("{0:F0}", disk), "%");
            if (_tickCount % 10 == 1) UpdateDiskSub();
            _diskChart.Push(disk);

            // top processes (every 3s)
            if (_tickCount % 3 == 1) UpdateTopProcs();

            // re-run the health check every 10 minutes
            if (_tickCount % 600 == 0) RunHealthCheck();
        }

        private void UpdateDiskSub()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var d in DriveInfo.GetDrives())
                {
                    if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
                    if (sb.Length > 0) sb.Append("   ");
                    sb.AppendFormat(L.T("{0} {1:F0} GB free"), d.Name.TrimEnd('\\'), d.AvailableFreeSpace / 1073741824.0);
                }
                _diskSub.Text = sb.ToString();
            }
            catch { }
        }

        // ================= System specs =================
        private Grid BuildSpecsPage()
        {
            var page = new Grid { Margin = new Thickness(24, 20, 24, 24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var head = new Grid { Margin = new Thickness(8, 0, 8, 14) };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var headLeft = new StackPanel();
            headLeft.Children.Add(new TextBlock
            {
                Text = L.T("System Specs"),
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            _specsStatus = new TextBlock
            {
                Text = L.T("Hardware & software details of this PC"),
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(1, 5, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            headLeft.Children.Add(_specsStatus);
            Grid.SetColumn(headLeft, 0);
            head.Children.Add(headLeft);

            var rescanBtn = PillButton(((char)0xE72C).ToString(), L.T("Rescan"), Theme.BtnBg, Theme.BtnHover, Theme.BtnText,
                delegate { LoadSpecsAsync(); });
            rescanBtn.VerticalAlignment = VerticalAlignment.Center;
            rescanBtn.Margin = new Thickness(16, 0, 0, 0);
            Grid.SetColumn(rescanBtn, 1);
            head.Children.Add(rescanBtn);
            Grid.SetRow(head, 0);
            page.Children.Add(head);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(8, 0, 8, 0)
            };
            _specsPanel = new StackPanel();
            _specsPanel.Children.Add(new TextBlock
            {
                Text = L.T("Collecting specifications..."),
                FontSize = 12.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(4, 10, 0, 0)
            });
            scroll.Content = _specsPanel;
            Grid.SetRow(scroll, 1);
            page.Children.Add(scroll);
            return page;
        }

        private void LoadSpecsAsync()
        {
            if (_specsScanning) return;
            _specsScanning = true;
            _specsStatus.Text = L.T("Scanning hardware...");
            _specsPanel.Children.Clear();
            _specsPanel.Children.Add(new TextBlock
            {
                Text = L.T("Collecting specifications..."),
                FontSize = 12.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(4, 10, 0, 0)
            });
            var sw = Stopwatch.StartNew();
            Task.Run<List<SpecSection>>(new Func<List<SpecSection>>(CollectSpecs)).ContinueWith(t =>
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    sw.Stop();
                    _specsScanning = false;
                    _specsPanel.Children.Clear();
                    if (t.IsFaulted || t.Result == null || t.Result.Count == 0)
                    {
                        _specsStatus.Text = L.T("Scan failed — press Rescan to retry");
                        _specsPanel.Children.Add(new TextBlock
                        {
                            Text = L.T("Could not retrieve system specifications."),
                            FontSize = 12.5,
                            Foreground = Ui.Br(Theme.TextLow),
                            Margin = new Thickness(4, 10, 0, 0)
                        });
                        return;
                    }
                    int items = 0;
                    foreach (var s in t.Result)
                    {
                        items += s.Rows.Count;
                        _specsPanel.Children.Add(SpecCard(s));
                    }
                    _specsStatus.Text = string.Format(L.T("Last scan {0} · took {1:F1}s · {2} sections · {3} items verified"),
                        DateTime.Now.ToString("HH:mm:ss"), sw.Elapsed.TotalSeconds, t.Result.Count, items);
                }));
            });
        }

        private Border SpecCard(SpecSection s)
        {
            var card = new Border
            {
                Background = Ui.Br(Theme.Card),
                BorderBrush = Ui.Br(Theme.CardBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20, 15, 20, 16),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var stack = new StackPanel();

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            titleRow.Children.Add(new Ellipse
            {
                Width = 7, Height = 7,
                Fill = Ui.Br(s.Accent),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 9, 0)
            });
            titleRow.Children.Add(new TextBlock
            {
                Text = s.Title,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            });
            stack.Children.Add(titleRow);

            foreach (var r in s.Rows)
            {
                var rowGrid = new Grid { Margin = new Thickness(16, 0, 0, 7) };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var lbl = new TextBlock
                {
                    Text = r[0],
                    FontSize = 12,
                    Foreground = Ui.Br(Theme.TextLow),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(lbl, 0);
                rowGrid.Children.Add(lbl);
                var val = new TextBlock
                {
                    Text = r[1],
                    FontSize = 12.5,
                    Foreground = Ui.Br("#E8E8ED"),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(val, 1);
                rowGrid.Children.Add(val);
                stack.Children.Add(rowGrid);
            }

            card.Child = stack;
            return card;
        }

        private static string WmiStr(ManagementObject mo, string prop)
        {
            try { var v = mo[prop]; return v == null ? "" : Convert.ToString(v).Trim(); }
            catch { return ""; }
        }

        // decode WMI uint16[] char arrays (monitor names etc.)
        private static string DecodeWmiChars(object o)
        {
            var arr = o as ushort[];
            if (arr == null) return "";
            var sb = new StringBuilder();
            foreach (var u in arr)
            {
                if (u == 0) break;
                sb.Append((char)u);
            }
            return sb.ToString().Trim();
        }

        private List<SpecSection> CollectSpecs()
        {
            var sections = new List<SpecSection>();

            // --- System / OS ---
            var sys = new SpecSection { Title = L.T("System"), Accent = Theme.AccCpu };
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem"))
                    foreach (ManagementObject mo in s.Get())
                    {
                        sys.Rows.Add(new[] { L.T("Manufacturer · Model"), (WmiStr(mo, "Manufacturer") + "  " + WmiStr(mo, L.T("Model"))).Trim() });
                        break;
                    }
            }
            catch { }
            sys.Rows.Add(new[] { L.T("Computer name"), Program.DemoMode ? "DESKTOP-DEMO" : Environment.MachineName });
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber, OSArchitecture, InstallDate FROM Win32_OperatingSystem"))
                    foreach (ManagementObject mo in s.Get())
                    {
                        string disp = "";
                        try
                        {
                            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                                if (k != null) disp = Convert.ToString(k.GetValue("DisplayVersion", ""));
                        }
                        catch { }
                        sys.Rows.Add(new[] { L.T("Operating system"), WmiStr(mo, "Caption").Replace("Microsoft ", "") +
                            (disp.Length > 0 ? " (" + disp + ")" : "") });
                        sys.Rows.Add(new[] { L.T("Version · Build"), WmiStr(mo, "Version") + L.T(" · build ") + WmiStr(mo, "BuildNumber") + " · " + WmiStr(mo, "OSArchitecture") });
                        try
                        {
                            var d = ManagementDateTimeConverter.ToDateTime(WmiStr(mo, "InstallDate"));
                            sys.Rows.Add(new[] { L.T("OS installed"), d.ToString(L.DateFormat, L.Culture) });
                        }
                        catch { }
                        break;
                    }
            }
            catch { }
            sections.Add(sys);

            // --- CPU ---
            var cpu = new SpecSection { Title = L.T("Processor (CPU)"), Accent = Theme.AccCpu };
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L3CacheSize, SocketDesignation FROM Win32_Processor"))
                    foreach (ManagementObject mo in s.Get())
                    {
                        cpu.Rows.Add(new[] { L.T("Model"), WmiStr(mo, "Name") });
                        cpu.Rows.Add(new[] { L.T("Cores · Threads"), WmiStr(mo, "NumberOfCores") + L.T(" cores · ") + WmiStr(mo, "NumberOfLogicalProcessors") + L.T(" threads") });
                        double mhz = 0; double.TryParse(WmiStr(mo, "MaxClockSpeed"), out mhz);
                        string clock = string.Format("{0:F2} GHz", mhz / 1000.0);
                        double l3 = 0; double.TryParse(WmiStr(mo, "L3CacheSize"), out l3);
                        if (l3 > 0) clock += string.Format(L.T(" · L3 cache {0:F0} MB"), l3 / 1024.0);
                        cpu.Rows.Add(new[] { L.T("Base clock"), clock });
                        string sock = WmiStr(mo, "SocketDesignation");
                        if (sock.Length > 0) cpu.Rows.Add(new[] { L.T("Socket"), sock });
                        break;
                    }
            }
            catch { }
            sections.Add(cpu);

            // --- GPU ---
            var gpuVram = new Dictionary<string, double>(); // DriverDesc → GB
            try
            {
                using (var cls = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (cls != null)
                        foreach (var sub in cls.GetSubKeyNames())
                        {
                            if (!Regex.IsMatch(sub, @"^\d{4}$")) continue;
                            try
                            {
                                using (var k = cls.OpenSubKey(sub))
                                {
                                    if (k == null) continue;
                                    string desc = Convert.ToString(k.GetValue("DriverDesc", ""));
                                    if (desc.Length == 0) continue;
                                    object q = k.GetValue("HardwareInformation.qwMemorySize");
                                    double gb = 0;
                                    if (q != null) gb = Convert.ToInt64(q) / 1073741824.0;
                                    else
                                    {
                                        object m = k.GetValue("HardwareInformation.MemorySize");
                                        if (m is byte[]) gb = BitConverter.ToUInt32((byte[])m, 0) / 1073741824.0;
                                        else if (m != null) gb = Convert.ToInt64(m) / 1073741824.0;
                                    }
                                    if (gb > 0 && !gpuVram.ContainsKey(desc)) gpuVram[desc] = gb;
                                }
                            }
                            catch { }
                        }
                }
            }
            catch { }
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name, DriverVersion, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate FROM Win32_VideoController"))
                {
                    int gi = 0;
                    foreach (ManagementObject mo in s.Get())
                    {
                        string name = WmiStr(mo, "Name");
                        if (name.ToLower().Contains("basic")) continue;
                        gi++;
                        var gpu = new SpecSection
                        {
                            Title = string.Format(L.T("Graphics (GPU {0})"), gi),
                            Accent = Theme.AccGpu
                        };
                        gpu.Rows.Add(new[] { L.T("Model"), name });
                        double vram;
                        if (gpuVram.TryGetValue(name, out vram))
                            gpu.Rows.Add(new[] { "VRAM", string.Format("{0:F0} GB", vram) });
                        string drv = WmiStr(mo, "DriverVersion");
                        if (drv.Length > 0) gpu.Rows.Add(new[] { L.T("Driver"), drv });
                        string hr = WmiStr(mo, "CurrentHorizontalResolution");
                        string vr = WmiStr(mo, "CurrentVerticalResolution");
                        string rr = WmiStr(mo, "CurrentRefreshRate");
                        if (hr.Length > 0 && hr != "0")
                            gpu.Rows.Add(new[] { L.T("Resolution"), hr + " × " + vr + (rr.Length > 0 && rr != "0" ? " · " + rr + "Hz" : "") });
                        sections.Add(gpu);
                    }
                }
            }
            catch { }

            // --- Memory ---
            var ram = new SpecSection { Title = L.T("Memory (RAM)"), Accent = Theme.AccRam };
            try
            {
                double totalGb = 0;
                var sticks = new List<string[]>();
                using (var s = new ManagementObjectSearcher("SELECT Capacity, Speed, ConfiguredClockSpeed, Manufacturer, PartNumber, DeviceLocator FROM Win32_PhysicalMemory"))
                    foreach (ManagementObject mo in s.Get())
                    {
                        double gb = 0;
                        double.TryParse(WmiStr(mo, "Capacity"), out gb);
                        gb /= 1073741824.0;
                        totalGb += gb;
                        string speed = WmiStr(mo, "ConfiguredClockSpeed");
                        if (speed.Length == 0 || speed == "0") speed = WmiStr(mo, "Speed");
                        string mfr = WmiStr(mo, "Manufacturer");
                        string part = WmiStr(mo, "PartNumber");
                        sticks.Add(new[]
                        {
                            WmiStr(mo, "DeviceLocator"),
                            string.Format("{0:F0} GB · {1} MHz · {2} {3}", gb, speed, mfr, part).Trim()
                        });
                    }
                ram.Rows.Add(new[] { L.T("Total"), string.Format(L.T("{0:F0} GB ({1} slots populated)"), totalGb, sticks.Count) });
                foreach (var st in sticks) ram.Rows.Add(st);
            }
            catch { }
            sections.Add(ram);

            // --- Storage ---
            var disk = new SpecSection { Title = L.T("Storage"), Accent = Theme.AccDisk };
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT FriendlyName, MediaType, BusType, Size FROM MSFT_PhysicalDisk")))
                {
                    int di = 0;
                    foreach (ManagementObject mo in s.Get())
                    {
                        di++;
                        string mediaType;
                        switch (WmiStr(mo, "MediaType"))
                        {
                            case "4": mediaType = "SSD"; break;
                            case "3": mediaType = "HDD"; break;
                            case "5": mediaType = "SCM"; break;
                            default: mediaType = L.T("Disk"); break;
                        }
                        if (WmiStr(mo, "BusType") == "17") mediaType = "NVMe " + mediaType;
                        double sz = 0;
                        double.TryParse(WmiStr(mo, "Size"), out sz);
                        disk.Rows.Add(new[]
                        {
                            L.T("Disk ") + di,
                            string.Format("{0} · {1} · {2:F0} GB", WmiStr(mo, "FriendlyName"), mediaType, sz / 1000000000.0)
                        });
                    }
                }
            }
            catch
            {
                try
                {
                    using (var s = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive"))
                    {
                        int di = 0;
                        foreach (ManagementObject mo in s.Get())
                        {
                            di++;
                            double sz = 0;
                            double.TryParse(WmiStr(mo, "Size"), out sz);
                            disk.Rows.Add(new[] { L.T("Disk ") + di, string.Format("{0} · {1:F0} GB", WmiStr(mo, L.T("Model")), sz / 1000000000.0) });
                        }
                    }
                }
                catch { }
            }
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
                    disk.Rows.Add(new[]
                    {
                        d.Name.TrimEnd('\\') + L.T(" drive"),
                        string.Format(L.T("{1:F0} GB free of {0:F0} GB ({2})"),
                            d.TotalSize / 1073741824.0, d.AvailableFreeSpace / 1073741824.0, d.DriveFormat)
                    });
                }
            }
            catch { }
            sections.Add(disk);

            // --- Motherboard / BIOS ---
            var board = new SpecSection { Title = L.T("Motherboard · BIOS"), Accent = "#40C8E0" };
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                    foreach (ManagementObject mo in s.Get())
                    {
                        board.Rows.Add(new[] { L.T("Motherboard"), (WmiStr(mo, "Manufacturer") + "  " + WmiStr(mo, "Product")).Trim() });
                        break;
                    }
            }
            catch { }
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS"))
                    foreach (ManagementObject mo in s.Get())
                    {
                        string bios = WmiStr(mo, "SMBIOSBIOSVersion");
                        try
                        {
                            var d = ManagementDateTimeConverter.ToDateTime(WmiStr(mo, "ReleaseDate"));
                            bios += " (" + d.ToString("yyyy-MM-dd") + ")";
                        }
                        catch { }
                        board.Rows.Add(new[] { "BIOS", bios });
                        break;
                    }
            }
            catch { }
            if (board.Rows.Count > 0) sections.Add(board);

            // --- Display ---
            var mon = new SpecSection { Title = L.T("Display"), Accent = "#DA8FFF" };
            try
            {
                var scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT UserFriendlyName, ManufacturerName FROM WmiMonitorID")))
                {
                    int mi = 0;
                    foreach (ManagementObject mo in s.Get())
                    {
                        string name = "", mfr = "";
                        try { name = DecodeWmiChars(mo["UserFriendlyName"]); } catch { }
                        try { mfr = DecodeWmiChars(mo["ManufacturerName"]); } catch { }
                        if (name.Length == 0 && mfr.Length == 0) continue;
                        mi++;
                        mon.Rows.Add(new[]
                        {
                            L.T("Monitor ") + mi,
                            name.Length > 0 ? name + (mfr.Length > 0 ? " (" + mfr + ")" : "") : mfr
                        });
                    }
                }
            }
            catch { }
            if (mon.Rows.Count > 0) sections.Add(mon);

            // --- Battery ---
            var bat = new SpecSection { Title = L.T("Battery"), Accent = "#FFD60A" };
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining, BatteryStatus FROM Win32_Battery"))
                    foreach (ManagementObject mo in s.Get())
                    {
                        string pct = WmiStr(mo, "EstimatedChargeRemaining");
                        string st = WmiStr(mo, "BatteryStatus");
                        string stTxt = st == "2" ? L.T("Plugged in") : (st == "1" ? L.T("On battery") : "");
                        if (pct.Length > 0)
                            bat.Rows.Add(new[] { L.T("Charge"), pct + "%" + (stTxt.Length > 0 ? " · " + stTxt : "") });
                        break;
                    }
            }
            catch { }
            try
            {
                // battery health: current full charge vs design capacity
                var scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();
                double design = 0, full = 0;
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DesignedCapacity FROM BatteryStaticData")))
                    foreach (ManagementObject mo in s.Get())
                    {
                        double.TryParse(WmiStr(mo, "DesignedCapacity"), out design);
                        break;
                    }
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT FullChargedCapacity FROM BatteryFullChargedCapacity")))
                    foreach (ManagementObject mo in s.Get())
                    {
                        double.TryParse(WmiStr(mo, "FullChargedCapacity"), out full);
                        break;
                    }
                if (design > 0 && full > 0)
                    bat.Rows.Add(new[]
                    {
                        L.T("Battery health"),
                        string.Format(L.T("{0:F0}% (design {1:N0} mWh → full charge {2:N0} mWh)"),
                            Math.Min(100, full / design * 100.0), design, full)
                    });
            }
            catch { }
            if (bat.Rows.Count > 0) sections.Add(bat);

            // --- Security ---
            var sec = new SpecSection { Title = L.T("Security"), Accent = "#FF6482" };
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
                    if (k != null)
                    {
                        object v = k.GetValue("UEFISecureBootEnabled");
                        if (v != null)
                            sec.Rows.Add(new[] { L.T("Secure Boot"), Convert.ToInt32(v) == 1 ? L.T("Enabled") : L.T("Disabled") });
                    }
            }
            catch { }
            try
            {
                var scope = new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftTpm");
                scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT IsEnabled_InitialValue, SpecVersion FROM Win32_Tpm")))
                    foreach (ManagementObject mo in s.Get())
                    {
                        string ver = WmiStr(mo, "SpecVersion");
                        int comma = ver.IndexOf(',');
                        if (comma > 0) ver = ver.Substring(0, comma);
                        bool on = WmiStr(mo, "IsEnabled_InitialValue").ToLower() == "true";
                        sec.Rows.Add(new[] { "TPM", (on ? L.T("Enabled") : L.T("Disabled")) + (ver.Length > 0 ? L.T(" · version ") + ver : "") });
                        break;
                    }
            }
            catch { }
            if (sec.Rows.Count > 0) sections.Add(sec);

            // --- Network ---
            var net = new SpecSection { Title = L.T("Network"), Accent = "#8BD65A" };
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name, Speed, NetEnabled FROM Win32_NetworkAdapter WHERE PhysicalAdapter = TRUE"))
                    foreach (ManagementObject mo in s.Get())
                    {
                        string name = WmiStr(mo, "Name");
                        bool enabled = WmiStr(mo, "NetEnabled").ToLower() == "true";
                        double bps = 0;
                        double.TryParse(WmiStr(mo, "Speed"), out bps);
                        string speed = "";
                        if (enabled && bps > 0 && bps < 100000000000.0)
                            speed = bps >= 1000000000.0
                                ? string.Format(" · {0:F0} Gbps", bps / 1000000000.0)
                                : string.Format(" · {0:F0} Mbps", bps / 1000000.0);
                        net.Rows.Add(new[] { enabled ? L.T("Connected") : L.T("Inactive"), name + speed });
                    }
            }
            catch { }
            if (net.Rows.Count > 0) sections.Add(net);

            return sections;
        }

        // ================= Health Check (compact dashboard card) =================
        private Border BuildHealthCard()
        {
            var card = new Border
            {
                Background = Ui.Br(Theme.Card),
                BorderBrush = Ui.Br(Theme.CardBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(22, 12, 22, 12),
                Margin = new Thickness(8, 0, 8, 6)
            };
            var stack = new StackPanel();

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // clickable area: dot + title + chevron + summary → toggles the detail list
            var clickArea = new Grid { Background = Brushes.Transparent, Cursor = Cursors.Hand };
            clickArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            clickArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            clickArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            clickArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _healthDot = new Ellipse
            {
                Width = 10, Height = 10,
                Fill = Ui.Br("#5C5C66"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 10, 0)
            };
            Grid.SetColumn(_healthDot, 0);
            clickArea.Children.Add(_healthDot);

            var title = new TextBlock
            {
                Text = L.T("Health Check"),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ui.Br(Theme.TextMid),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(title, 1);
            clickArea.Children.Add(title);

            _healthChevron = new TextBlock
            {
                Text = ((char)0xE70D).ToString(), // ChevronDown
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 10,
                Foreground = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 14, 0)
            };
            Grid.SetColumn(_healthChevron, 2);
            clickArea.Children.Add(_healthChevron);

            _healthSummary = new TextBlock
            {
                Text = L.T("Checking..."),
                FontSize = 12.5,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(_healthSummary, 3);
            clickArea.Children.Add(_healthSummary);

            clickArea.MouseLeftButtonUp += delegate { ToggleHealthDetail(); };
            Grid.SetColumn(clickArea, 0);
            row.Children.Add(clickArea);

            _healthStatus = new TextBlock
            {
                Text = "",
                FontSize = 11,
                Foreground = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 14, 0)
            };
            Grid.SetColumn(_healthStatus, 1);
            row.Children.Add(_healthStatus);

            var refresh = PillButton(((char)0xE72C).ToString(), L.T("Run Check"),
                Theme.BtnBg, Theme.BtnHover, Theme.BtnText, delegate { RunHealthCheck(); });
            refresh.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(refresh, 2);
            row.Children.Add(refresh);

            stack.Children.Add(row);

            _healthDetail = new StackPanel { Margin = new Thickness(4, 6, 0, 0) };
            _healthDetailScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 195,
                Margin = new Thickness(16, 4, 0, 0),
                Visibility = Visibility.Collapsed,
                Content = _healthDetail
            };
            stack.Children.Add(_healthDetailScroll);

            card.Child = stack;
            return card;
        }

        private void ToggleHealthDetail()
        {
            _healthExpanded = !_healthExpanded;
            ApplyHealthDetailVisibility();
        }

        private void ApplyHealthDetailVisibility()
        {
            if (_healthDetailScroll == null) return;
            _healthDetailScroll.Visibility = _healthExpanded ? Visibility.Visible : Visibility.Collapsed;
            _healthChevron.Text = (_healthExpanded ? (char)0xE70E : (char)0xE70D).ToString();
        }

        private void RenderHealthDetail()
        {
            _healthDetail.Children.Clear();
            if (_healthResults == null) return;
            foreach (var s in _healthResults)
            {
                _healthDetail.Children.Add(new TextBlock
                {
                    Text = s.Title,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Ui.Br(Theme.TextLow),
                    Margin = new Thickness(0, 6, 0, 4)
                });
                foreach (var it in s.Items)
                {
                    var irow = new Grid { Margin = new Thickness(6, 0, 0, 4) };
                    irow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
                    irow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
                    irow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var dot = new Ellipse
                    {
                        Width = 8, Height = 8,
                        Fill = Ui.Br(LevelColor(it.Level)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(dot, 0);
                    irow.Children.Add(dot);
                    var nm = new TextBlock
                    {
                        Text = it.Name,
                        FontSize = 11.5,
                        Foreground = Ui.Br("#C9C9D1"),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    Grid.SetColumn(nm, 1);
                    irow.Children.Add(nm);
                    var dt = new TextBlock
                    {
                        Text = it.Detail,
                        FontSize = 11.5,
                        Foreground = Ui.Br(Theme.TextMid),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Grid.SetColumn(dt, 2);
                    irow.Children.Add(dt);
                    _healthDetail.Children.Add(irow);
                }
            }
        }

        private static string LevelColor(int level)
        {
            switch (level)
            {
                case 0: return "#30D158";
                case 1: return "#FFD60A";
                case 2: return "#FF453A";
                default: return "#5C5C66";
            }
        }

        private void RunHealthCheck()
        {
            if (_healthRunning || _healthSummary == null) return;
            _healthRunning = true;
            _healthStatus.Text = L.T("Checking...");
            Task.Run<List<HealthSection>>(new Func<List<HealthSection>>(CollectHealth)).ContinueWith(t =>
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _healthRunning = false;
                    if (_healthSummary == null) return;
                    if (t.IsFaulted || t.Result == null || t.Result.Count == 0)
                    {
                        _healthStatus.Text = L.T("Check failed — press Run Check to retry");
                        return;
                    }
                    _healthResults = t.Result;
                    int okC = 0, warn = 0, bad = 0, unk = 0;
                    foreach (var s in t.Result)
                        foreach (var it in s.Items)
                        {
                            if (it.Level == 0) okC++;
                            else if (it.Level == 1) warn++;
                            else if (it.Level == 2) bad++;
                            else unk++;
                        }
                    int overall = bad > 0 ? 2 : (warn > 0 ? 1 : 0);
                    _healthDot.Fill = Ui.Br(LevelColor(overall));
                    _healthSummary.Text = string.Format(L.T("{0} OK · {1} warning · {2} critical · {3} unknown"), okC, warn, bad, unk);
                    _healthStatus.Text = DateTime.Now.ToString("HH:mm:ss");
                    RenderHealthDetail();
                    if (bad > 0 || warn > 0) _healthExpanded = true; // auto-expand on issues
                    ApplyHealthDetailVisibility();
                }));
            });
        }
        private List<HealthSection> CollectHealth()
        {
            var sections = new List<HealthSection>();

            // --- Storage ---
            var st = new HealthSection { Title = L.T("Storage") };
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT FriendlyName, HealthStatus FROM MSFT_PhysicalDisk")))
                    foreach (ManagementObject mo in s.Get())
                    {
                        string name = WmiStr(mo, "FriendlyName");
                        string hs = WmiStr(mo, "HealthStatus");
                        int lvl = hs == "0" ? 0 : (hs == "1" ? 1 : (hs == "2" ? 2 : 3));
                        string verdict = lvl == 0 ? L.T("Healthy") : (lvl == 1 ? L.T("Warning") : (lvl == 2 ? L.T("Critical") : L.T("Unknown")));
                        st.Items.Add(new HealthItem { Name = name, Detail = L.T("SMART status: ") + verdict, Level = lvl });
                    }
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Wear, Temperature FROM MSFT_StorageReliabilityCounter")))
                {
                    int di = 0;
                    foreach (ManagementObject mo in s.Get())
                    {
                        di++;
                        double wear = -1, temp = -1;
                        double.TryParse(WmiStr(mo, "Wear"), out wear);
                        double.TryParse(WmiStr(mo, "Temperature"), out temp);
                        if (wear > 0)
                        {
                            int lvl = wear >= 80 ? 2 : (wear >= 50 ? 1 : 0);
                            st.Items.Add(new HealthItem
                            {
                                Name = L.T("SSD wear") + " " + di,
                                Detail = string.Format(L.T("{0:F0}% worn"), wear),
                                Level = lvl
                            });
                        }
                        if (temp > 0)
                        {
                            int lvl = temp >= 70 ? 2 : (temp >= 60 ? 1 : 0);
                            st.Items.Add(new HealthItem
                            {
                                Name = L.T("Disk temperature") + " " + di,
                                Detail = string.Format("{0:F0}°C", temp),
                                Level = lvl
                            });
                        }
                    }
                }
            }
            catch { }
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    if (!d.IsReady || d.DriveType != DriveType.Fixed) continue;
                    double freePct = 100.0 * d.AvailableFreeSpace / d.TotalSize;
                    int lvl = freePct < 5 ? 2 : (freePct < 10 ? 1 : 0);
                    st.Items.Add(new HealthItem
                    {
                        Name = d.Name.TrimEnd('\\') + " " + L.T("free space"),
                        Detail = string.Format(L.T("{0:F0} GB free ({1:F0}%)"), d.AvailableFreeSpace / 1073741824.0, freePct),
                        Level = lvl
                    });
                }
            }
            catch { }
            if (st.Items.Count > 0) sections.Add(st);

            // --- GPU (nvidia-smi) ---
            var gp = new HealthSection { Title = "GPU" };
            if (_nv.Available && !_nv.HasData)
            {
                try { _nv.Refresh(); } catch { }
            }
            if (_nv.HasData)
            {
                int tl = _nv.TempC >= 85 ? 2 : (_nv.TempC >= 75 ? 1 : 0);
                gp.Items.Add(new HealthItem
                {
                    Name = L.T("GPU temperature"),
                    Detail = string.Format("{0:F0}°C — {1}", _nv.TempC, _nv.GpuName),
                    Level = tl
                });
                long mask = 0;
                try
                {
                    string hx = _nv.ThrottleHex.Trim();
                    if (hx.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        mask = Convert.ToInt64(hx.Substring(2), 16);
                }
                catch { }
                // 0x8 HW slowdown · 0x40 SW thermal · 0x80 HW power brake
                bool slow = (mask & 0x8L) != 0 || (mask & 0x40L) != 0 || (mask & 0x80L) != 0;
                gp.Items.Add(new HealthItem
                {
                    Name = L.T("GPU throttling"),
                    Detail = slow ? L.T("Thermal/power slowdown active") : L.T("Not throttling"),
                    Level = slow ? 1 : 0
                });
                if (_nv.MemTotalMB > 0)
                {
                    double used = _nv.MemUsedMB / _nv.MemTotalMB * 100.0;
                    int vl = used >= 97 ? 2 : (used >= 90 ? 1 : 0);
                    gp.Items.Add(new HealthItem
                    {
                        Name = L.T("VRAM headroom"),
                        Detail = string.Format("{0:F1} / {1:F1} GB ({2:F0}%)", _nv.MemUsedMB / 1024.0, _nv.MemTotalMB / 1024.0, used),
                        Level = vl
                    });
                }
                if (_nv.PowerW >= 0 && _nv.PowerLimitW > 0)
                    gp.Items.Add(new HealthItem
                    {
                        Name = L.T("Power draw"),
                        Detail = string.Format("{0:F0} / {1:F0} W", _nv.PowerW, _nv.PowerLimitW),
                        Level = 0
                    });
                gp.Items.Add(new HealthItem
                {
                    Name = L.T("Driver"),
                    Detail = _nv.DriverVer,
                    Level = 0
                });
            }
            else
            {
                gp.Items.Add(new HealthItem { Name = "NVIDIA", Detail = L.T("nvidia-smi not available"), Level = 3 });
            }
            sections.Add(gp);

            // --- Memory ---
            var me = new HealthSection { Title = L.T("Memory") };
            var mem = new MemStatusEx();
            if (GlobalMemoryStatusEx(mem))
            {
                int lvl = mem.dwMemoryLoad >= 97 ? 2 : (mem.dwMemoryLoad >= 90 ? 1 : 0);
                me.Items.Add(new HealthItem
                {
                    Name = L.T("Memory pressure"),
                    Detail = string.Format(L.T("{0}% in use ({1:F1} / {2:F1} GB)"), mem.dwMemoryLoad,
                        (mem.ullTotalPhys - mem.ullAvailPhys) / 1073741824.0, mem.ullTotalPhys / 1073741824.0),
                    Level = lvl
                });
                if (mem.ullTotalPageFile > 0)
                {
                    double commit = 100.0 * (mem.ullTotalPageFile - mem.ullAvailPageFile) / mem.ullTotalPageFile;
                    int cl = commit >= 95 ? 2 : (commit >= 85 ? 1 : 0);
                    me.Items.Add(new HealthItem
                    {
                        Name = L.T("Commit charge"),
                        Detail = string.Format("{0:F0}%", commit),
                        Level = cl
                    });
                }
            }
            sections.Add(me);

            // --- Battery ---
            var ba = new HealthSection { Title = L.T("Battery") };
            try
            {
                var scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();
                double design = 0, full = 0;
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DesignedCapacity FROM BatteryStaticData")))
                    foreach (ManagementObject mo in s.Get()) { double.TryParse(WmiStr(mo, "DesignedCapacity"), out design); break; }
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT FullChargedCapacity FROM BatteryFullChargedCapacity")))
                    foreach (ManagementObject mo in s.Get()) { double.TryParse(WmiStr(mo, "FullChargedCapacity"), out full); break; }
                if (design > 0 && full > 0)
                {
                    double health = Math.Min(100, full / design * 100.0);
                    int lvl = health < 60 ? 2 : (health < 80 ? 1 : 0);
                    ba.Items.Add(new HealthItem
                    {
                        Name = L.T("Battery health"),
                        Detail = string.Format("{0:F0}%", health),
                        Level = lvl
                    });
                }
            }
            catch { }
            if (ba.Items.Count > 0) sections.Add(ba);

            // --- System ---
            var sy = new HealthSection { Title = L.T("System") };
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
                    if (k != null)
                    {
                        object v = k.GetValue("UEFISecureBootEnabled");
                        if (v != null)
                        {
                            bool on = Convert.ToInt32(v) == 1;
                            sy.Items.Add(new HealthItem
                            {
                                Name = L.T("Secure Boot"),
                                Detail = on ? L.T("Enabled") : L.T("Disabled"),
                                Level = on ? 0 : 1
                            });
                        }
                    }
            }
            catch { }
            try
            {
                double days = TimeSpan.FromMilliseconds(GetTickCount64()).TotalDays;
                int lvl = days >= 30 ? 1 : 0;
                sy.Items.Add(new HealthItem
                {
                    Name = L.T("Uptime"),
                    Detail = lvl == 1
                        ? string.Format(L.T("{0:F0} days without a reboot — consider restarting"), days)
                        : string.Format(L.T("{0:F1} days"), days),
                    Level = lvl
                });
            }
            catch { }
            // CPU/system temperature (often unsupported without vendor drivers)
            bool gotTemp = false;
            try
            {
                var scope = new ManagementScope(@"\\.\root\wmi");
                scope.Connect();
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature")))
                    foreach (ManagementObject mo in s.Get())
                    {
                        double raw = 0;
                        double.TryParse(WmiStr(mo, "CurrentTemperature"), out raw);
                        if (raw > 0)
                        {
                            double cel = raw / 10.0 - 273.15;
                            int lvl = cel >= 95 ? 2 : (cel >= 85 ? 1 : 0);
                            sy.Items.Add(new HealthItem
                            {
                                Name = L.T("Thermal zone"),
                                Detail = string.Format("{0:F0}°C", cel),
                                Level = lvl
                            });
                            gotTemp = true;
                            break;
                        }
                    }
            }
            catch { }
            if (!gotTemp)
                sy.Items.Add(new HealthItem
                {
                    Name = L.T("CPU temperature"),
                    Detail = L.T("Not supported on this system"),
                    Level = 3
                });
            sections.Add(sy);

            return sections;
        }

        // ================= Quick Fixes =================
        private Grid BuildFixesPage()
        {
            var page = new Grid { Margin = new Thickness(24, 20, 24, 24) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var head = new Grid { Margin = new Thickness(8, 0, 8, 14) };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var headLeft = new StackPanel();
            headLeft.Children.Add(new TextBlock
            {
                Text = L.T("Quick Fixes"),
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            headLeft.Children.Add(new TextBlock
            {
                Text = L.T("One-click repairs for common Windows problems"),
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(1, 5, 0, 0)
            });
            Grid.SetColumn(headLeft, 0);
            head.Children.Add(headLeft);

            var termBtn = PillButton(((char)0xE756).ToString(), L.T("Open Terminal"),
                Theme.BtnBg, Theme.BtnHover, Theme.BtnText, delegate { OpenTerminal(); });
            termBtn.VerticalAlignment = VerticalAlignment.Center;
            termBtn.Margin = new Thickness(16, 0, 0, 0);
            Grid.SetColumn(termBtn, 1);
            head.Children.Add(termBtn);
            Grid.SetRow(head, 0);
            page.Children.Add(head);

            var grid = new UniformGrid { Columns = 2, Margin = new Thickness(2, 0, 2, 4) };
            grid.Children.Add(FixCard(0, L.T("Fix Internet"),
                L.T("Flush DNS and reset the network stack (Winsock/IP). A reboot is recommended afterwards.")));
            grid.Children.Add(FixCard(1, L.T("Free Disk Space"),
                L.T("Delete temp files (user + Windows) and empty the Recycle Bin.")));
            grid.Children.Add(FixCard(2, L.T("Repair System Files"),
                L.T("Run sfc /scannow to find and fix corrupted system files. Can take 10+ minutes.")));
            grid.Children.Add(FixCard(3, L.T("Restart Explorer"),
                L.T("Restart the taskbar/desktop when they freeze or misbehave.")));
            Grid.SetRow(grid, 1);
            page.Children.Add(grid);

            var consoleBorder = new Border
            {
                Background = Ui.Br("#0A0A0C"),
                BorderBrush = Ui.Br(Theme.CardBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(8, 8, 8, 0),
                Padding = new Thickness(4)
            };
            _fixConsole = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = Ui.Br("#B9C2CF"),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                FontSize = 11.5,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10, 8, 10, 8),
                Text = L.T("Output will appear here.")
            };
            consoleBorder.Child = _fixConsole;
            Grid.SetRow(consoleBorder, 2);
            page.Children.Add(consoleBorder);

            return page;
        }

        private Border FixCard(int id, string title, string desc)
        {
            var card = new Border
            {
                Background = Ui.Br(Theme.Card),
                BorderBrush = Ui.Br(Theme.CardBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18, 13, 18, 13),
                Margin = new Thickness(6)
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var left = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
            left.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            });
            left.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 11,
                Foreground = Ui.Br(Theme.TextLow),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(left, 0);
            g.Children.Add(left);
            var localTitle = title;
            var localDesc = desc;
            var btn = PillButton(null, L.T("Run"), Theme.BtnBg, Theme.BtnHover, Theme.BtnText,
                delegate { RunFix(id, localTitle, localDesc); });
            btn.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(btn, 1);
            g.Children.Add(btn);
            card.Child = g;
            return card;
        }

        private void OpenTerminal()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "wt.exe", UseShellExecute = true });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "powershell.exe", UseShellExecute = true });
                }
                catch { }
            }
        }

        private void FixLog(string line)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (_fixConsole == null) return;
                _fixConsole.AppendText(line + Environment.NewLine);
                _fixConsole.ScrollToEnd();
            }));
        }

        private int RunCmdStep(string file, string args, bool unicodeOut)
        {
            FixLog("> " + file + " " + args);
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                if (unicodeOut) psi.StandardOutputEncoding = Encoding.Unicode;
                using (var p = Process.Start(psi))
                {
                    p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
                    {
                        if (!string.IsNullOrEmpty(e.Data) && e.Data.Trim().Length > 0) FixLog("  " + e.Data.Trim());
                    };
                    p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
                    {
                        if (!string.IsNullOrEmpty(e.Data) && e.Data.Trim().Length > 0) FixLog("  " + e.Data.Trim());
                    };
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    return p.ExitCode;
                }
            }
            catch (Exception ex)
            {
                FixLog("  ERROR: " + ex.Message);
                return -1;
            }
        }

        private double CleanTempDir(string dir)
        {
            double freed = 0;
            try
            {
                var di = new DirectoryInfo(dir);
                if (!di.Exists) return 0;
                foreach (var fi in di.GetFiles())
                {
                    try { double len = fi.Length; fi.Delete(); freed += len; }
                    catch { }
                }
                foreach (var sub in di.GetDirectories())
                {
                    try { sub.Delete(true); }
                    catch { }
                }
                FixLog(string.Format("  {0} — {1:F0} MB", dir, freed / 1048576.0));
            }
            catch { }
            return freed;
        }

        private void RunFix(int id, string title, string desc)
        {
            if (_fixRunning)
            {
                MessageBox.Show(this, L.T("Another fix is already running."), "WinTotal",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(this, string.Format(L.T("Run \"{0}\"?\n\n{1}"), title, desc),
                title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _fixRunning = true;
            FixLog("");
            FixLog("═══ " + title + " ═══");
            Task.Run(delegate
            {
                try
                {
                    switch (id)
                    {
                        case 0: // network
                            RunCmdStep("ipconfig", "/flushdns", false);
                            RunCmdStep("netsh", "winsock reset", false);
                            RunCmdStep("netsh", "int ip reset", false);
                            FixLog(L.T("A reboot is recommended."));
                            break;
                        case 1: // disk space
                            double freed = 0;
                            freed += CleanTempDir(System.IO.Path.GetTempPath());
                            freed += CleanTempDir(System.IO.Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                            try
                            {
                                SHEmptyRecycleBin(IntPtr.Zero, null, 0x7); // no confirm/progress/sound
                                FixLog("  " + L.T("Recycle Bin emptied"));
                            }
                            catch { }
                            FixLog(string.Format(L.T("Freed {0:F0} MB"), freed / 1048576.0));
                            break;
                        case 2: // sfc
                            RunCmdStep("sfc", "/scannow", true);
                            break;
                        case 3: // explorer
                            RunCmdStep("taskkill", "/f /im explorer.exe", false);
                            System.Threading.Thread.Sleep(800);
                            try { Process.Start(new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true }); }
                            catch { }
                            FixLog("  " + L.T("Explorer restarted"));
                            break;
                    }
                    FixLog(L.T("Done."));
                }
                catch (Exception ex)
                {
                    FixLog("  ERROR: " + ex.Message);
                }
                _fixRunning = false;
            });
        }

        // ================= Apps =================
        private Grid BuildAppsPage()
        {
            var page = new Grid { Margin = new Thickness(24, 20, 24, 14) };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // header
            var headRow = new DockPanel { Margin = new Thickness(8, 0, 8, 14), LastChildFill = false };
            var headLeft = new StackPanel();
            headLeft.Children.Add(new TextBlock
            {
                Text = L.T("Installed Apps"),
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            _appCountText = new TextBlock
            {
                Text = L.T("Loading..."),
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(1, 5, 0, 0)
            };
            headLeft.Children.Add(_appCountText);
            DockPanel.SetDock(headLeft, Dock.Left);
            headRow.Children.Add(headLeft);

            var refreshBtn = PillButton("", L.T("Refresh"), Theme.BtnBg, Theme.BtnHover, Theme.BtnText,
                delegate { LoadAppsAsync(); });
            refreshBtn.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(refreshBtn, Dock.Right);
            headRow.Children.Add(refreshBtn);
            Grid.SetRow(headRow, 0);
            page.Children.Add(headRow);

            // search
            var searchBorder = new Border
            {
                Background = Ui.Br(Theme.InputBg),
                BorderBrush = Ui.Br(Theme.CardBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(11),
                Margin = new Thickness(8, 0, 8, 12),
                Padding = new Thickness(14, 9, 14, 9)
            };
            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var searchIcon = new TextBlock
            {
                Text = "",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 13,
                Foreground = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            var inputHost = new Grid();
            _searchBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _searchPlaceholder = new TextBlock
            {
                Text = L.T("Search apps by name or publisher"),
                FontSize = 13,
                Foreground = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            _searchBox.TextChanged += delegate
            {
                _searchPlaceholder.Visibility = _searchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
                ApplyFilter();
            };
            inputHost.Children.Add(_searchBox);
            inputHost.Children.Add(_searchPlaceholder);
            Grid.SetColumn(inputHost, 1);
            searchGrid.Children.Add(inputHost);
            searchBorder.Child = searchGrid;
            Grid.SetRow(searchBorder, 1);
            page.Children.Add(searchBorder);

            // category chips
            _chipsPanel = new WrapPanel { Margin = new Thickness(8, 0, 8, 10) };
            Grid.SetRow(_chipsPanel, 2);
            page.Children.Add(_chipsPanel);

            // list
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(8, 0, 8, 0)
            };
            _appListPanel = new StackPanel();
            scroll.Content = _appListPanel;
            Grid.SetRow(scroll, 3);
            page.Children.Add(scroll);

            // status bar
            _statusText = new TextBlock
            {
                Text = "",
                FontSize = 11,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(10, 8, 8, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(_statusText, 4);
            page.Children.Add(_statusText);

            return page;
        }

        // ---------- Categories ----------
        private static readonly string[] CategoryOrder = new string[]
        {
            "All", "AI Tools", "Development", "Games", "Internet & Chat", "Media & Creative",
            "Security", "Utilities", "System Components", "Other"
        };

        private static bool HasAny(string text, string[] keys)
        {
            foreach (var k in keys)
                if (text.Contains(k)) return true;
            return false;
        }

        private static string Categorize(AppEntry a)
        {
            string t = ((a.Name ?? "") + " " + (a.Publisher ?? "") + " " + (a.PackageFullName ?? "")).ToLowerInvariant();

            if (HasAny(t, new[] { "claude", "anthropic", "chatgpt", "openai", "copilot", "ollama",
                "gemini", "perplexity", "stable diffusion", "comfyui", "lm studio", "midjourney" }))
                return "AI Tools";
            if (HasAny(t, new[] { "visual studio", "vs code", "vscode", "python", "anaconda", "miniconda",
                "node.js", "nodejs", "git", "github", "docker", "jetbrains", "intellij", "pycharm",
                "android studio", " sdk", "sdk ", " jdk", "cuda", "cudnn", "cmake", "postman",
                "windows terminal", "powershell", "wsl", "kubernetes", "rust", "golang", "mysql",
                "postgres", "mongodb", "arduino", "putty", "wireshark", "development kit" }))
                return "Development";
            if (HasAny(t, new[] { "steam", "epic games", "riot", "battle.net", "blizzard", "xbox",
                "nexon", "ubisoft", "ea app", "gog galaxy", "game", "게임", "minecraft", "roblox" }))
                return "Games";
            if (HasAny(t, new[] { "chrome", "edge", "firefox", "whale", "opera", "brave", "browser",
                "kakaotalk", "카카오", "discord", "telegram", "slack", "teams", "zoom", "skype",
                "line ", "webex", "메신저" }))
                return "Internet & Chat";
            if (HasAny(t, new[] { "spotify", "vlc", "obs", "adobe", "photoshop", "premiere", "lightroom",
                "illustrator", "netflix", "davinci", "gimp", "blender", "audacity", "media player",
                "곰플레이어", "potplayer", "팟플레이어", "itunes", "music", "paint", "capcut", "canva",
                "clip studio", "krita", "figma" }))
                return "Media & Creative";
            if (HasAny(t, new[] { "defender", "antivirus", "백신", "v3 ", "알약", "avast", "norton",
                "mcafee", "malware", "bitdefender", "kaspersky", "안랩", "ahnlab" }))
                return "Security";
            if (HasAny(t, new[] { "redistributable", "visual c++", "runtime", ".net", "webview",
                "driver", "드라이버", "chipset", "firmware", "directx", "vulkan", "update for",
                "intel", "nvidia", "realtek", "amd ", "synaptics", "dolby", "thunderbolt",
                "extension", "codec", "installer", "component", "package" }))
                return "System Components";
            if (HasAny(t, new[] { "7-zip", "winrar", "반디집", "bandizip", "zip", "cleaner", "everything",
                "powertoys", "utility", "notepad", "메모장", "ultraedit", "sumatra", "pdf", "capture",
                "screenshot", "sharex", "utility", "알집", "압축" }))
                return "Utilities";
            return "Other";
        }

        private Border MakeChip(string cat, int count)
        {
            var chip = new Border
            {
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(12, 5, 12, 6),
                Margin = new Thickness(0, 0, 7, 7),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                Tag = cat
            };
            var tb = new TextBlock { FontSize = 11.5 };
            tb.Inlines.Add(new Run(L.T(cat)));
            tb.Inlines.Add(new Run("  " + count) { FontWeight = FontWeights.SemiBold });
            chip.Child = tb;
            chip.MouseLeftButtonUp += delegate
            {
                _activeCategory = cat;
                RestyleChips();
                ApplyFilter();
            };
            return chip;
        }

        private void RestyleChips()
        {
            foreach (UIElement el in _chipsPanel.Children)
            {
                var chip = el as Border;
                if (chip == null) continue;
                bool active = (chip.Tag as string) == _activeCategory;
                chip.Background = active ? Ui.Br("#12283E") : Brushes.Transparent;
                chip.BorderBrush = active ? Ui.Br("#1E4A73") : Ui.Br(Theme.CardBorder);
                var tb = (TextBlock)chip.Child;
                tb.Foreground = active ? Ui.Br("#6FB3FF") : Ui.Br(Theme.TextMid);
            }
        }

        private void RebuildChips()
        {
            _chipsPanel.Children.Clear();
            var counts = new Dictionary<string, int>();
            foreach (var a in _apps)
            {
                string c = a.Category ?? "Other";
                if (!counts.ContainsKey(c)) counts[c] = 0;
                counts[c]++;
            }
            if (!CategoryOrder.Contains(_activeCategory) ||
                (_activeCategory != "All" && !counts.ContainsKey(_activeCategory)))
                _activeCategory = "All";
            foreach (var cat in CategoryOrder)
            {
                int n = cat == "All" ? _apps.Count : (counts.ContainsKey(cat) ? counts[cat] : 0);
                if (n == 0) continue;
                _chipsPanel.Children.Add(MakeChip(cat, n));
            }
            RestyleChips();
        }

        private Border PillButton(string glyph, string text, string bg, string hoverBg, string fg, Action onClick)
        {
            var b = new Border
            {
                Background = Ui.Br(bg),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(13, 7, 13, 8),
                Cursor = Cursors.Hand
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            if (!string.IsNullOrEmpty(glyph))
            {
                sp.Children.Add(new TextBlock
                {
                    Text = glyph,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 12,
                    Foreground = Ui.Br(fg),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 1, 7, 0)
                });
            }
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ui.Br(fg),
                VerticalAlignment = VerticalAlignment.Center
            });
            b.Child = sp;
            b.MouseEnter += delegate { b.Background = Ui.Br(hoverBg); };
            b.MouseLeave += delegate { b.Background = Ui.Br(bg); };
            b.MouseLeftButtonUp += delegate { onClick(); };
            return b;
        }

        private void SetStatus(string msg)
        {
            Dispatcher.BeginInvoke(new Action(delegate { _statusText.Text = msg; }));
        }

        private void LoadAppsAsync()
        {
            _appCountText.Text = L.T("Loading app list...");
            _appListPanel.Children.Clear();
            Task.Run(delegate
            {
                var list = new List<AppEntry>();
                ScanUninstallKeys(RegistryHive.LocalMachine, RegistryView.Registry64, list);
                ScanUninstallKeys(RegistryHive.LocalMachine, RegistryView.Registry32, list);
                ScanUninstallKeys(RegistryHive.CurrentUser, RegistryView.Registry64, list);
                LoadStoreApps(list);

                var seen = new HashSet<string>();
                var final = new List<AppEntry>();
                foreach (var a in list.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    string key = (a.Name + "|" + a.Version).ToLowerInvariant();
                    if (seen.Add(key))
                    {
                        a.Category = Categorize(a);
                        final.Add(a);
                    }
                }
                MarkRunning(final);
                return final;
            }).ContinueWith(t =>
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _apps = t.IsFaulted ? new List<AppEntry>() : t.Result;
                    BuildAppRows();
                }));
            });
        }

        private void ScanUninstallKeys(RegistryHive hive, RegistryView view, List<AppEntry> list)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                using (var uk = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (uk == null) return;
                    foreach (var sub in uk.GetSubKeyNames())
                    {
                        try
                        {
                            using (var k = uk.OpenSubKey(sub))
                            {
                                if (k == null) continue;
                                string name = Convert.ToString(k.GetValue("DisplayName", "")).Trim();
                                if (string.IsNullOrEmpty(name)) continue;
                                object sysComp = k.GetValue("SystemComponent");
                                if (sysComp != null && Convert.ToInt32(sysComp) == 1) continue;
                                if (k.GetValue("ParentKeyName") != null) continue;

                                var a = new AppEntry();
                                a.Name = name;
                                a.Version = Convert.ToString(k.GetValue("DisplayVersion", ""));
                                a.Publisher = Convert.ToString(k.GetValue("Publisher", ""));
                                a.UninstallString = Convert.ToString(k.GetValue("QuietUninstallString", ""));
                                if (string.IsNullOrEmpty(a.UninstallString))
                                    a.UninstallString = Convert.ToString(k.GetValue("UninstallString", ""));
                                a.InstallLocation = Convert.ToString(k.GetValue("InstallLocation", "")).Trim('"');
                                object size = k.GetValue("EstimatedSize");
                                if (size != null) { try { a.SizeMB = Convert.ToInt64(size) / 1024.0; } catch { } }
                                a.KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + sub;
                                a.Hive = hive;
                                a.View = view;
                                list.Add(a);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void LoadStoreApps(List<AppEntry> list)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-AppxPackage | Where-Object { -not $_.IsFramework -and $_.SignatureKind -ne 'System' } | ForEach-Object { $_.Name + '|' + $_.PackageFullName + '|' + $_.Publisher + '|' + $_.Version }\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                using (var p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(30000);
                    foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = line.Split('|');
                        if (parts.Length < 4) continue;
                        var a = new AppEntry();
                        a.Name = PrettyStoreName(parts[0]);
                        a.PackageFullName = parts[1];
                        a.Publisher = SimplifyPublisher(parts[2]);
                        a.Version = parts[3];
                        a.IsStore = true;
                        list.Add(a);
                    }
                }
            }
            catch { }
        }

        private static string SimplifyPublisher(string dn)
        {
            var m = Regex.Match(dn, @"CN=([^,]+)");
            string r = m.Success ? m.Groups[1].Value.Trim() : dn;
            // GUID publishers are meaningless — substitute
            if (Regex.IsMatch(r, @"^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-"))
                r = L.T("Microsoft Store app");
            return r;
        }

        // "AppUp.IntelGraphicsExperience" → "Intel Graphics Experience"
        private static string PrettyStoreName(string raw)
        {
            string n = raw;
            int dot = n.LastIndexOf('.');
            if (dot >= 0 && dot < n.Length - 1) n = n.Substring(dot + 1);
            n = Regex.Replace(n, @"^\d+", "");
            n = Regex.Replace(n, "(?<=[a-z0-9])(?=[A-Z])", " ");
            n = Regex.Replace(n, "(?<=[A-Z])(?=[A-Z][a-z])", " ");
            n = n.Trim();
            return n.Length >= 2 ? n : raw;
        }

        private void BuildAppRows()
        {
            _appListPanel.Children.Clear();
            foreach (var a in _apps)
                _appListPanel.Children.Add(MakeAppRow(a));
            RefreshCount();
            RebuildChips();
            ApplyFilter();
        }

        private static readonly string[] RowColors = new string[]
        {
            "#0A84FF", "#BF5AF2", "#30D158", "#FF9F0A", "#FF6482", "#40C8E0", "#DA8FFF", "#8BD65A"
        };

        private Border MakeAppRow(AppEntry a)
        {
            var row = new Border
            {
                Background = Ui.Br(Theme.Card),
                BorderBrush = Ui.Br("#17171B"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 7),
                Tag = a
            };
            row.MouseEnter += delegate { row.Background = Ui.Br(Theme.CardHover); };
            row.MouseLeave += delegate { row.Background = Ui.Br(Theme.Card); };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // icon (initial letter)
            string colHex = RowColors[Math.Abs(a.Name.GetHashCode()) % RowColors.Length];
            var c = Ui.Col(colHex);
            var icon = new Border
            {
                Width = 36, Height = 36,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(38, c.R, c.G, c.B)),
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.Child = new TextBlock
            {
                Text = a.Name.Substring(0, 1).ToUpper(),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(c),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            g.Children.Add(icon);

            // name/publisher
            var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 8, 0) };
            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            nameRow.Children.Add(new TextBlock
            {
                Text = a.Name,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            if (a.IsStore)
            {
                var badge = new Border
                {
                    Background = Ui.Br("#0E2238"),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(7, 1, 7, 2),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock { Text = "Store", FontSize = 10, Foreground = Ui.Br("#5EAAFF") };
                nameRow.Children.Add(badge);
            }
            if (a.RunningPids != null && a.RunningPids.Count > 0)
            {
                var runBadge = new Border
                {
                    Background = Ui.Br("#0F2E1B"),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(7, 1, 7, 2),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                runBadge.Child = new TextBlock { Text = L.T("Running"), FontSize = 10, Foreground = Ui.Br("#4ADE80") };
                nameRow.Children.Add(runBadge);
            }
            nameStack.Children.Add(nameRow);
            nameStack.Children.Add(new TextBlock
            {
                Text = (string.IsNullOrEmpty(a.Publisher) ? L.T("Unknown publisher") : a.Publisher)
                    + (string.IsNullOrEmpty(a.Category) ? "" : "  ·  " + L.T(a.Category)),
                FontSize = 11,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(0, 2, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            Grid.SetColumn(nameStack, 1);
            g.Children.Add(nameStack);

            // version
            var ver = new TextBlock
            {
                Text = a.Version ?? "",
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextMid),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(ver, 2);
            g.Children.Add(ver);

            // size
            var size = new TextBlock
            {
                Text = a.SizeMB > 0 ? string.Format("{0:F0} MB", a.SizeMB) : "",
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextMid),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(size, 3);
            g.Children.Add(size);

            // buttons
            var btns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var localA = a;
            var localRow = row;
            if (a.RunningPids != null && a.RunningPids.Count > 0)
            {
                var endBtn = PillButton(null, L.T("End"), "#2E2410", "#41331A", "#FFD60A",
                    delegate { EndAppProcesses(localA); });
                endBtn.Margin = new Thickness(0, 0, 7, 0);
                btns.Children.Add(endBtn);
            }
            btns.Children.Add(PillButton(null, L.T("Uninstall"), Theme.BtnBg, Theme.BtnHover, Theme.BtnText,
                delegate { UninstallApp(localA, localRow); }));
            if (!a.IsStore)
            {
                var force = PillButton(null, L.T("Force Delete"), Theme.DangerBg, Theme.DangerHover, Theme.DangerText,
                    delegate { ForceClean(localA, localRow, true); });
                force.Margin = new Thickness(7, 0, 0, 0);
                btns.Children.Add(force);
            }
            Grid.SetColumn(btns, 4);
            g.Children.Add(btns);

            row.Child = g;
            return row;
        }

        private void ApplyFilter()
        {
            string q = (_searchBox.Text ?? "").Trim().ToLowerInvariant();
            foreach (UIElement el in _appListPanel.Children)
            {
                var row = el as Border;
                if (row == null) continue;
                var a = row.Tag as AppEntry;
                bool catOk = _activeCategory == "All" || (a.Category ?? "Other") == _activeCategory;
                bool show = catOk && (q.Length == 0
                    || a.Name.ToLowerInvariant().Contains(q)
                    || (a.Publisher ?? "").ToLowerInvariant().Contains(q)
                    || (a.PackageFullName ?? "").ToLowerInvariant().Contains(q));
                row.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ---------- Running-process detection & graceful close ----------
        // Match running processes to apps by install dir (desktop) or package name in path (Store)
        private static void MarkRunning(List<AppEntry> apps)
        {
            var dirMap = new List<KeyValuePair<string, AppEntry>>();
            foreach (var a in apps)
            {
                if (a.IsStore)
                {
                    if (!string.IsNullOrEmpty(a.PackageFullName))
                        dirMap.Add(new KeyValuePair<string, AppEntry>("\\" + a.PackageFullName.ToLowerInvariant() + "\\", a));
                }
                else
                {
                    string dir = GuessInstallDir(a);
                    if (!string.IsNullOrEmpty(dir) && dir.Length > 4)
                        dirMap.Add(new KeyValuePair<string, AppEntry>(dir.TrimEnd('\\') .ToLowerInvariant() + "\\", a));
                }
            }
            if (dirMap.Count == 0) return;
            Process[] procs;
            try { procs = Process.GetProcesses(); } catch { return; }
            foreach (var p in procs)
            {
                int pid;
                string path = null;
                try
                {
                    pid = p.Id;
                    path = p.MainModule.FileName.ToLowerInvariant();
                }
                catch { continue; }
                finally { try { p.Dispose(); } catch { } }
                if (string.IsNullOrEmpty(path)) continue;
                foreach (var kv in dirMap)
                {
                    bool hit = kv.Key[0] == '\\' ? path.Contains(kv.Key) : path.StartsWith(kv.Key);
                    if (hit)
                    {
                        if (kv.Value.RunningPids == null) kv.Value.RunningPids = new List<int>();
                        kv.Value.RunningPids.Add(pid);
                        break;
                    }
                }
            }
        }

        private void EndAppProcesses(AppEntry a)
        {
            if (a.RunningPids == null || a.RunningPids.Count == 0) return;
            if (MessageBox.Show(this,
                string.Format(L.T("Close the running processes of \"{0}\"?\n\nA close request is sent first so the app can save; you will be asked again before any force kill."), a.Name),
                L.T("Graceful Close"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            var pids = new List<int>(a.RunningPids);
            SetStatus(string.Format(L.T("Closing processes of \"{0}\"..."), a.Name));
            Task.Run(delegate
            {
                var procs = new List<Process>();
                foreach (var pid in pids)
                {
                    try { procs.Add(Process.GetProcessById(pid)); } catch { }
                }
                foreach (var p in procs)
                {
                    try { if (p.MainWindowHandle != IntPtr.Zero) p.CloseMainWindow(); } catch { }
                }
                for (int i = 0; i < 10; i++)
                {
                    System.Threading.Thread.Sleep(500);
                    bool alive = false;
                    foreach (var p in procs) { try { if (!p.HasExited) { alive = true; break; } } catch { } }
                    if (!alive) break;
                }
                var still = new List<Process>();
                foreach (var p in procs) { try { if (!p.HasExited) still.Add(p); } catch { } }
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (still.Count > 0)
                    {
                        if (MessageBox.Show(this,
                            string.Format(L.T("{0} process(es) of \"{1}\" are still running.\nForce kill? Unsaved data may be lost."), still.Count, a.Name),
                            L.T("Force Kill"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                            foreach (var p in still) { try { p.Kill(); } catch { } }
                    }
                    a.RunningPids = null;
                    SetStatus(string.Format(L.T("Processes of \"{0}\" closed."), a.Name));
                }));
            });
        }

        // ---------- Uninstall ----------
        private void UninstallApp(AppEntry a, Border row)
        {
            if (a.IsStore)
            {
                if (MessageBox.Show(this,
                    string.Format(L.T("Uninstall Store app \"{0}\"?"), a.Name),
                    L.T("Uninstall App"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                SetStatus(string.Format(L.T("Uninstalling \"{0}\"..."), a.Name));
                var pkg = a.PackageFullName;
                Task.Run(delegate
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = string.Format("-NoProfile -Command \"Remove-AppxPackage -Package '{0}'\"", pkg),
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var p = Process.Start(psi)) p.WaitForExit(120000);
                }).ContinueWith(t =>
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        _appListPanel.Children.Remove(row);
                        _apps.Remove(a);
                        RefreshCount();
                        SetStatus(string.Format(L.T("\"{0}\" uninstalled"), a.Name));
                    }));
                });
                return;
            }

            if (string.IsNullOrEmpty(a.UninstallString))
            {
                if (MessageBox.Show(this,
                    string.Format(L.T("\"{0}\" has no uninstaller.\nForce-clean its registry entries and install folder?"), a.Name),
                    L.T("No Uninstaller"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    ForceClean(a, row, false);
                return;
            }

            if (MessageBox.Show(this,
                string.Format(L.T("Uninstall \"{0}\"?\n\nAfter the uninstaller finishes, leftover registry entries are cleaned up automatically."), a.Name),
                L.T("Uninstall App"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            SetStatus(string.Format(L.T("Running the uninstaller for \"{0}\"..."), a.Name));
            string cmd = a.UninstallString;
            cmd = Regex.Replace(cmd, @"(msiexec(\.exe)?""?\s+.*)/I\{", "$1/X{", RegexOptions.IgnoreCase);

            Task.Run(delegate
            {
                try
                {
                    string file, args;
                    ParseCommand(cmd, out file, out args);
                    var psi = new ProcessStartInfo { FileName = file, Arguments = args, UseShellExecute = true };
                    using (var p = Process.Start(psi))
                        if (p != null) p.WaitForExit();
                }
                catch (Exception ex)
                {
                    SetStatus(L.T("Failed to run the uninstaller: ") + ex.Message);
                    return;
                }

                bool keyGone = !UninstallKeyExists(a);
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (keyGone)
                    {
                        var removed = CleanRegistryLeftovers(a);
                        _appListPanel.Children.Remove(row);
                        _apps.Remove(a);
                        RefreshCount();
                        SetStatus(string.Format(L.T("\"{0}\" uninstalled · {1} leftover registry keys cleaned"), a.Name, removed.Count));
                    }
                    else
                    {
                        if (MessageBox.Show(this,
                            string.Format(L.T("The uninstall of \"{0}\" does not look complete (its registration is still present).\nForce-clean the registry?"), a.Name),
                            L.T("Leftovers"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            ForceClean(a, row, false);
                        else
                            SetStatus(L.T("Cancelled"));
                    }
                }));
            });
        }

        private static void ParseCommand(string cmd, out string file, out string args)
        {
            cmd = cmd.Trim();
            if (cmd.StartsWith("\""))
            {
                int end = cmd.IndexOf('"', 1);
                file = cmd.Substring(1, end - 1);
                args = end + 1 < cmd.Length ? cmd.Substring(end + 1).Trim() : "";
                return;
            }
            if (File.Exists(cmd)) { file = cmd; args = ""; return; }
            int exe = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exe > 0)
            {
                file = cmd.Substring(0, exe + 4);
                args = cmd.Substring(exe + 4).Trim();
                return;
            }
            int sp = cmd.IndexOf(' ');
            if (sp > 0) { file = cmd.Substring(0, sp); args = cmd.Substring(sp + 1); }
            else { file = cmd; args = ""; }
        }

        private bool UninstallKeyExists(AppEntry a)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(a.Hive, a.View))
                using (var k = baseKey.OpenSubKey(a.KeyPath))
                    return k != null;
            }
            catch { return false; }
        }

        // guess the install dir: if InstallLocation is empty, infer it from the uninstaller path
        // (e.g. Squirrel apps like Claude — %LocalAppData%\AnthropicClaude\Update.exe)
        private static string GuessInstallDir(AppEntry a)
        {
            if (!string.IsNullOrEmpty(a.InstallLocation)) return a.InstallLocation;
            if (string.IsNullOrEmpty(a.UninstallString)) return null;
            if (a.UninstallString.IndexOf("msiexec", StringComparison.OrdinalIgnoreCase) >= 0) return null;
            try
            {
                string file, args;
                ParseCommand(a.UninstallString, out file, out args);
                string dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(file));
                if (string.IsNullOrEmpty(dir)) return null;
                // only folders directly under allowed roots (anything else is too risky)
                string parent = System.IO.Path.GetDirectoryName(dir.TrimEnd('\\'));
                if (parent == null) return null;
                string[] allowedRoots = new string[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                };
                foreach (var root in allowedRoots)
                    if (!string.IsNullOrEmpty(root) &&
                        string.Equals(parent.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                        return dir;
                return null;
            }
            catch { return null; }
        }

        // ---------- Force delete (registry + folders) ----------
        private void ForceClean(AppEntry a, Border row, bool confirm)
        {
            string loc = GuessInstallDir(a);
            if (confirm)
            {
                var msg = new StringBuilder();
                msg.AppendFormat(L.T("All traces of \"{0}\" will be force-deleted.\n\n"), a.Name);
                msg.AppendLine(L.T("· Program registration (Uninstall registry key)"));
                msg.AppendLine(L.T("· Leftover registry keys under HKLM / HKCU Software"));
                if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc))
                    msg.AppendLine(L.T("· Install folder: ") + loc);
                msg.AppendLine();
                msg.Append(L.T("This deletes immediately WITHOUT running the uninstaller. Continue?"));
                if (MessageBox.Show(this, msg.ToString(), L.T("Force Delete"),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            }

            var removed = CleanRegistryLeftovers(a);
            int folders = 0;

            if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc) && IsSafeToDeleteDir(loc))
            {
                try { Directory.Delete(loc, true); folders = 1; }
                catch { }
            }

            _appListPanel.Children.Remove(row);
            _apps.Remove(a);
            RefreshCount();
            string summary = string.Format(L.T("\"{0}\" force-deleted · {1} registry keys, {2} folders removed"),
                a.Name, removed.Count, folders);
            SetStatus(summary);
            if (confirm)
                MessageBox.Show(this, summary + (removed.Count > 0 ? "\n\n" + string.Join("\n", removed.Take(15)) : ""),
                    L.T("Done"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static bool IsSafeToDeleteDir(string path)
        {
            try
            {
                string full = System.IO.Path.GetFullPath(path).TrimEnd('\\');
                string[] forbidden = new string[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Environment.GetFolderPath(Environment.SpecialFolder.System)
                };
                if (full.Length <= 3) return false;
                foreach (var f in forbidden)
                    if (!string.IsNullOrEmpty(f) && string.Equals(full, f.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                        return false;
                return full.Count(ch => ch == '\\') >= 2;
            }
            catch { return false; }
        }

        private List<string> CleanRegistryLeftovers(AppEntry a)
        {
            var removed = new List<string>();

            if (!string.IsNullOrEmpty(a.KeyPath) && DeleteKeyTree(a.Hive, a.View, a.KeyPath))
                removed.Add(HiveName(a.Hive) + "\\" + a.KeyPath);

            var candidates = BuildNameCandidates(a.Name);
            if (candidates.Count == 0) return removed;

            var roots = new[]
            {
                Tuple.Create(RegistryHive.CurrentUser, RegistryView.Registry64),
                Tuple.Create(RegistryHive.LocalMachine, RegistryView.Registry64),
                Tuple.Create(RegistryHive.LocalMachine, RegistryView.Registry32)
            };

            foreach (var root in roots)
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(root.Item1, root.Item2))
                    using (var sw = baseKey.OpenSubKey("SOFTWARE"))
                    {
                        if (sw == null) continue;
                        foreach (var sub in sw.GetSubKeyNames())
                        {
                            if (IsNameMatch(sub, candidates))
                            {
                                if (DeleteKeyTree(root.Item1, root.Item2, "SOFTWARE\\" + sub))
                                    removed.Add(HiveName(root.Item1) + "\\SOFTWARE\\" + sub +
                                        (root.Item2 == RegistryView.Registry32 ? " (32bit)" : ""));
                            }
                            else if (!string.IsNullOrEmpty(a.Publisher) &&
                                     string.Equals(sub, a.Publisher, StringComparison.OrdinalIgnoreCase) &&
                                     !IsProtected(sub))
                            {
                                try
                                {
                                    using (var vendor = sw.OpenSubKey(sub))
                                    {
                                        if (vendor == null) continue;
                                        foreach (var child in vendor.GetSubKeyNames())
                                        {
                                            if (IsNameMatch(child, candidates))
                                            {
                                                if (DeleteKeyTree(root.Item1, root.Item2, "SOFTWARE\\" + sub + "\\" + child))
                                                    removed.Add(HiveName(root.Item1) + "\\SOFTWARE\\" + sub + "\\" + child);
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }
            return removed;
        }

        private static string HiveName(RegistryHive h)
        {
            return h == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
        }

        private static List<string> BuildNameCandidates(string name)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(name)) return list;
            string n = name.Trim();
            list.Add(n);
            string noParen = Regex.Replace(n, @"\s*\([^)]*\)\s*", " ").Trim();
            if (noParen.Length >= 3 && !list.Contains(noParen)) list.Add(noParen);
            string noVer = Regex.Replace(noParen, @"\s+v?\d[\d\.]*$", "", RegexOptions.IgnoreCase).Trim();
            if (noVer.Length >= 3 && !list.Contains(noVer)) list.Add(noVer);
            return list;
        }

        private static bool IsProtected(string keyName)
        {
            string low = keyName.ToLowerInvariant();
            foreach (var p in ProtectedKeys)
                if (low == p) return true;
            return false;
        }

        private static bool IsNameMatch(string keyName, List<string> candidates)
        {
            if (IsProtected(keyName)) return false;
            foreach (var c in candidates)
                if (c.Length >= 3 && string.Equals(keyName, c, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private bool DeleteKeyTree(RegistryHive hive, RegistryView view, string path)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                {
                    using (var k = baseKey.OpenSubKey(path))
                        if (k == null) return false;
                    baseKey.DeleteSubKeyTree(path, false);
                    return true;
                }
            }
            catch { return false; }
        }

        private void RefreshCount()
        {
            _appCountText.Text = string.Format(L.T("{0} apps ({1} desktop · {2} Store)"),
                _apps.Count, _apps.Count(x => !x.IsStore), _apps.Count(x => x.IsStore));
        }
    }
}
