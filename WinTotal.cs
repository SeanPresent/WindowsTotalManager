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

        private static readonly CultureInfo KoCulture = new CultureInfo("ko-KR");
        private static readonly CultureInfo EnCulture = new CultureInfo("en-US");
        public static CultureInfo Culture
        {
            get { return Ko ? KoCulture : EnCulture; }
        }
        public static string ClockFormat { get { return Ko ? "M월 d일 dddd" : "ddd, MMM d"; } }
        public static string DateFormat { get { return Ko ? "yyyy년 M월 d일" : "MMM d, yyyy"; } }

        private static readonly Dictionary<string, string> KoMap = new Dictionary<string, string>
        {
            { "System manager", "시스템 종합 관리" },
            { "Dashboard", "대시보드" },
            { "System Specs", "시스템 사양" },
            { "Apps", "앱 관리" },
            { "v1.5 · single executable", "v1.5 · 단일 실행 파일" },
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
            { "System · cannot close", "시스템 · 종료 불가" },
            { "Close WinTotal (this app)?", "WinTotal(이 앱)을 종료할까요?" },
            { "Protected system process — cannot be closed", "보호된 시스템 프로세스 — 종료할 수 없습니다" },
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
            { "Diagnosing...", "진단 중..." },
            { "Check failed — press Run Check to retry", "진단 실패 — 진단 실행을 눌러 다시 시도하세요" },
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
            { "Check first, then repair only if needed — for common Windows problems", "먼저 검사하고, 필요할 때만 수리합니다 — 자주 겪는 Windows 문제 해결" },
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
            { "Check", "검사" },
            { "Checking...", "검사 중..." },
            { "Not checked yet", "아직 검사하지 않음" },
            { "Network OK — repair not needed", "네트워크 정상 — 수리가 필요하지 않습니다" },
            { "Connection problems detected — repair recommended", "연결 문제 감지 — 수리를 권장합니다" },
            { "No network connection — check the adapter, then run repair", "네트워크 연결 없음 — 어댑터 확인 후 수리를 실행하세요" },
            { "ping 8.8.8.8: OK ({0} ms)", "ping 8.8.8.8: 정상 ({0} ms)" },
            { "ping 8.8.8.8: failed", "ping 8.8.8.8: 실패" },
            { "DNS lookup: OK", "DNS 조회: 정상" },
            { "DNS lookup: failed", "DNS 조회: 실패" },
            { "Temp files: {0} ({1} files)", "임시 파일: {0} ({1}개)" },
            { "Recycle Bin: {0} ({1} items)", "휴지통: {0} ({1}개)" },
            { "About {0} can be freed — temp files {1} · Recycle Bin {2}", "약 {0} 정리 가능 — 임시 파일 {1} · 휴지통 {2}" },
            { "Nothing significant to clean (about {0})", "정리할 항목이 거의 없습니다 (약 {0})" },
            { "Component store status: {0}", "구성 요소 저장소 상태: {0}" },
            { "No system file corruption detected — repair not needed", "시스템 파일 손상 흔적 없음 — 수리가 필요하지 않습니다" },
            { "Corruption detected — repair is recommended", "손상 감지 — 수리 실행을 권장합니다" },
            { "Check inconclusive — you can still run the repair", "검사 불확정 — 필요하면 수리를 실행하세요" },
            { "Administrator rights are required for this check.", "이 검사에는 관리자 권한이 필요합니다." },
            { "Explorer is running normally", "탐색기 정상 동작 중" },
            { "Explorer is not responding — restart recommended", "탐색기 응답 없음 — 재시작을 권장합니다" },
            { "Explorer is not running — restart needed", "탐색기가 실행 중이 아님 — 재시작이 필요합니다" },
            { "Repair finished — run Check again to verify", "수리 완료 — 다시 검사해서 확인하세요" },
            { "Store", "스토어" },
            { "ERROR: ", "오류: " },
            { "{0} files deleted...", "{0}개 파일 삭제됨..." },
            { "Recycle Bin already empty (or could not be emptied)", "휴지통이 이미 비어 있음 (또는 비우기 실패)" },
            { "Step failed (exit code {0})", "단계 실패 (종료 코드 {0})" },
            { "Finished, but some steps failed — see the log above.", "완료했지만 일부 단계가 실패했습니다 — 위 로그를 확인하세요." },
            { "Command timed out and was stopped.", "명령이 시간을 초과해 중단되었습니다." },
            { "Not running as administrator — network reset, system file repair and Windows Temp cleanup may fail.", "관리자 권한이 아닙니다 — 네트워크 초기화, 시스템 파일 복구, Windows Temp 정리가 실패할 수 있습니다." },
            { "A repair is still running. Quit anyway?\n(The running command will continue in the background.)", "수리 작업이 아직 실행 중입니다. 그래도 종료할까요?\n(실행 중인 명령은 백그라운드에서 계속 진행됩니다.)" },
            { "Store app list unavailable — showing desktop apps only.", "스토어 앱 목록을 가져오지 못했습니다 — 데스크톱 앱만 표시합니다." },
            { "Could not uninstall \"{0}\". The app may be running or need administrator rights.", "\"{0}\"을(를) 제거하지 못했습니다. 앱이 실행 중이거나 관리자 권한이 필요할 수 있습니다." },
            { "{0} process(es) did not close.", "프로세스 {0}개가 종료되지 않았습니다." },
            { "Deleting leftovers...", "잔여 파일 삭제 중..." },
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
        // the palette is a fixed set of constants, so frozen brushes are cached forever;
        // frozen brushes are thread-safe and carry no change-notification overhead
        private static readonly Dictionary<string, SolidColorBrush> BrushCache = new Dictionary<string, SolidColorBrush>();
        private static readonly object BrushLock = new object();

        public static SolidColorBrush Br(string hex)
        {
            lock (BrushLock)
            {
                SolidColorBrush b;
                if (BrushCache.TryGetValue(hex, out b)) return b;
                b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                b.Freeze();
                BrushCache[hex] = b;
                return b;
            }
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
            gridBrush.Freeze();
            for (int i = 0; i < 3; i++)
            {
                _gridLines[i] = new Line { Stroke = gridBrush, StrokeThickness = 1 };
                Children.Add(_gridLines[i]);
            }
            var gb = new LinearGradientBrush(
                Color.FromArgb(85, c.R, c.G, c.B),
                Color.FromArgb(0, c.R, c.G, c.B), 90);
            gb.Freeze();
            _fill = new Polygon { Fill = gb };
            var glowBrush = new SolidColorBrush(Color.FromArgb(55, c.R, c.G, c.B));
            glowBrush.Freeze();
            _glow = new Polyline
            {
                Stroke = glowBrush,
                StrokeThickness = 6,
                StrokeLineJoin = PenLineJoin.Round
            };
            var lineBrush = new SolidColorBrush(c);
            lineBrush.Freeze();
            _line = new Polyline
            {
                Stroke = lineBrush,
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
            // reuse the PointCollections; in-place writes on a Freezable trigger re-render
            bool rebuild = _pts == null || _pts.Count != n;
            if (rebuild)
            {
                _pts = new PointCollection(n);
                _fillPts = new PointCollection(n + 2);
                for (int i = 0; i < n; i++) _pts.Add(new Point());
                for (int i = 0; i < n + 2; i++) _fillPts.Add(new Point());
            }
            for (int i = 0; i < n; i++)
            {
                double x = w - (n - 1 - i) * step;
                double y = h - 2 - (_values[i] / 100.0) * (h - 6);
                var pt = new Point(x, y);
                _pts[i] = pt;
                _fillPts[i + 1] = pt;
            }
            _fillPts[0] = new Point(w - (n - 1) * step, h);
            _fillPts[n + 1] = new Point(w, h);
            if (rebuild)
            {
                _line.Points = _pts;
                _glow.Points = _pts;
                _fill.Points = _fillPts;
            }
        }

        private PointCollection _pts, _fillPts;
    }

    // ---------- GPU usage (Task Manager style: max of per-engine-type sums) ----------
    public class GpuMonitor
    {
        private List<PerformanceCounter> _counters = new List<PerformanceCounter>();
        private PerformanceCounterCategory _cat;
        private HashSet<string> _instNames = new HashSet<string>();
        private DateTime _lastCheck = DateTime.MinValue;
        public bool Available = true;
        public Dictionary<int, float> PidUsage = new Dictionary<int, float>(); // per-process GPU %
        private readonly Dictionary<string, float> _byType = new Dictionary<string, float>();
        private readonly Dictionary<int, Dictionary<string, float>> _byPidType = new Dictionary<int, Dictionary<string, float>>();

        public float Read()
        {
            if (!Available) return 0;
            try
            {
                // recreate the (potentially hundreds of) counters only when the instance list changed
                if ((DateTime.Now - _lastCheck).TotalSeconds > 15)
                {
                    _lastCheck = DateTime.Now;
                    if (_cat == null) _cat = new PerformanceCounterCategory("GPU Engine");
                    var names = _cat.GetInstanceNames();
                    if (InstancesChanged(names, _instNames)) Rebuild(names);
                }
                var byType = _byType;
                var byPidType = _byPidType;
                byType.Clear();
                byPidType.Clear();
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

        internal static bool InstancesChanged(string[] names, HashSet<string> known)
        {
            if (names.Length != known.Count) return true;
            foreach (var nm in names)
                if (!known.Contains(nm)) return true;
            return false;
        }

        private void Rebuild(string[] names)
        {
            foreach (var c in _counters) { try { c.Dispose(); } catch { } }
            _counters.Clear();
            foreach (var inst in names)
            {
                try
                {
                    var pc = new PerformanceCounter("GPU Engine", "Utilization Percentage", inst);
                    pc.NextValue(); // prime the first sample (avoids 0)
                    _counters.Add(pc);
                }
                catch { }
            }
            _instNames = new HashSet<string>(names);
        }
    }

    // ---------- GPU memory (adapter total + per-process dedicated VRAM) ----------
    public class GpuMemMonitor
    {
        private List<PerformanceCounter> _adapter = new List<PerformanceCounter>();
        private List<PerformanceCounter> _process = new List<PerformanceCounter>();
        private PerformanceCounterCategory _adapterCat, _processCat;
        private HashSet<string> _adapterNames = new HashSet<string>();
        private HashSet<string> _processNames = new HashSet<string>();
        private DateTime _lastCheck = DateTime.MinValue;
        public bool Available = true;
        public double TotalUsedBytes;
        public Dictionary<int, double> PidBytes = new Dictionary<int, double>();

        public void Read()
        {
            if (!Available) return;
            try
            {
                if ((DateTime.Now - _lastCheck).TotalSeconds > 15) CheckRebuild();
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

        // recreate counters per category only when that category's instance list changed
        private void CheckRebuild()
        {
            _lastCheck = DateTime.Now;
            try
            {
                if (_adapterCat == null) _adapterCat = new PerformanceCounterCategory("GPU Adapter Memory");
                var names = _adapterCat.GetInstanceNames();
                if (GpuMonitor.InstancesChanged(names, _adapterNames))
                {
                    foreach (var c in _adapter) { try { c.Dispose(); } catch { } }
                    _adapter.Clear();
                    foreach (var inst in names)
                    {
                        try
                        {
                            var pc = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", inst);
                            pc.NextValue();
                            _adapter.Add(pc);
                        }
                        catch { }
                    }
                    _adapterNames = new HashSet<string>(names);
                }
            }
            catch { }
            try
            {
                if (_processCat == null) _processCat = new PerformanceCounterCategory("GPU Process Memory");
                var names2 = _processCat.GetInstanceNames();
                if (GpuMonitor.InstancesChanged(names2, _processNames))
                {
                    foreach (var c in _process) { try { c.Dispose(); } catch { } }
                    _process.Clear();
                    foreach (var inst in names2)
                    {
                        try
                        {
                            var pc = new PerformanceCounter("GPU Process Memory", "Dedicated Usage", inst);
                            pc.NextValue();
                            _process.Add(pc);
                        }
                        catch { }
                    }
                    _processNames = new HashSet<string>(names2);
                }
            }
            catch { }
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
        private readonly object _sync = new object(); // Tick and health check may refresh concurrently

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
            lock (_sync)
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
                string line = null;
                using (var p = Process.Start(psi))
                {
                    // single-line output cannot fill the pipe, so waiting before reading is safe;
                    // a hung nvidia-smi gets killed instead of blocking this thread forever
                    if (p.WaitForExit(5000)) line = p.StandardOutput.ReadLine();
                    else { try { p.Kill(); } catch { } }
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
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MemStatusEx buf);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

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
        private volatile bool _nvBusy;
        private double _gpuUsageCache;
        private volatile bool _gpuSampleBusy;
        private volatile bool _diskSubBusy;
        private volatile bool _topProcsBusy;
        private volatile int _procCount;
        private DispatcherTimer _timer;
        private int _tickCount;

        // dashboard UI
        private TextBlock _cpuVal, _cpuSub, _gpuVal, _gpuSub, _ramVal, _ramSub, _diskVal, _diskSub;
        private LineChart _cpuChart, _gpuChart, _ramChart, _diskChart;
        private TextBlock _headerInfo, _clockTime, _clockDate;
        private string _infoLine, _gpuName;

        // top-process tracking — row UI is pooled and recycled every refresh (no per-tick rebuild)
        private class TopRow
        {
            public Grid Root;
            public TextBlock Name;
            public TextBlock Value;
            public Border Fill;
            public Grid FillHost;
            public double Ratio;
            public bool Critical;
            public Border KillBtn;
            public TextBlock LockIcon;
            public string ProcName; // read by the kill handler; updated on recycle
        }
        private class TopColumn
        {
            public StackPanel Panel;
            public string Accent;
            public List<TopRow> Rows = new List<TopRow>();
            public UIElement GroupLabel;
            public TextBlock Placeholder;
        }
        private TopColumn _colCpu, _colRam, _colGpu;
        private static readonly FontFamily Mdl2Font = new FontFamily("Segoe MDL2 Assets");
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
        private volatile bool _appsLoading;
        private int _rowBuildGen;
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
        private volatile bool _fixRunning;
        private readonly TextBlock[] _fixStatusText = new TextBlock[4];
        private readonly Ellipse[] _fixStatusDot = new Ellipse[4];
        private readonly Border[] _fixRepairBtn = new Border[4];
        private readonly object _fixLogLock = new object();
        private readonly StringBuilder _fixLogBuf = new StringBuilder();
        private bool _fixLogFlushQueued;

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
            // 810: the metric-card charts need ~56px each after the health strip and the
            // System rows of the top-process panel (added in v1.5) grew; clamp for short screens
            Width = 1120; Height = Math.Min(810, SystemParameters.WorkArea.Height - 24);
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
                {
                    // pixels are copied into the BitmapSource, so the GDI icon can be released immediately
                    try
                    {
                        Icon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    }
                    finally { ico.Dispose(); }
                }
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
                Text = L.T("v1.5 · single executable"),
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

            _colCpu = new TopColumn { Panel = MakeTopColumn(procGrid, 0, L.T("Top CPU processes"), Theme.AccCpu), Accent = Theme.AccCpu };
            _colGpu = new TopColumn { Panel = MakeTopColumn(procGrid, 2, L.T("Top GPU processes"), Theme.AccGpu), Accent = Theme.AccGpu };
            _colRam = new TopColumn { Panel = MakeTopColumn(procGrid, 4, L.T("Top memory processes"), Theme.AccRam), Accent = Theme.AccRam };

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

        private TopRow MakeTopRow(string accent)
        {
            var row = new TopRow();
            var g = new Grid { Margin = new Thickness(15, 0, 0, 6), MinHeight = 16 };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            row.Name = new TextBlock
            {
                FontSize = 11.5,
                Foreground = Ui.Br("#C9C9D1"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(row.Name, 0);
            g.Children.Add(row.Name);

            var track = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                Background = Ui.Br("#1B1B20"),
                VerticalAlignment = VerticalAlignment.Center
            };
            row.FillHost = new Grid();
            row.Fill = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Ui.Br(accent),
                Width = 0
            };
            row.FillHost.Children.Add(track);
            row.FillHost.Children.Add(row.Fill);
            var holder = row;
            row.FillHost.SizeChanged += delegate
            {
                holder.Fill.Width = Math.Max(0, Math.Min(1, holder.Ratio)) * holder.FillHost.ActualWidth;
            };
            Grid.SetColumn(row.FillHost, 1);
            g.Children.Add(row.FillHost);

            row.Value = new TextBlock
            {
                FontSize = 11,
                Foreground = Ui.Br(Theme.TextMid),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(row.Value, 2);
            g.Children.Add(row.Value);

            // column 3 holds both the lock icon and the X button; visibility picks one
            row.LockIcon = new TextBlock
            {
                Text = ((char)0xE72E).ToString(), // MDL2 Lock
                FontFamily = Mdl2Font,
                FontSize = 10,
                Foreground = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(6, 0, 5, 0),
                ToolTip = L.T("Protected system process — cannot be closed"),
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(row.LockIcon, 3);
            g.Children.Add(row.LockIcon);

            row.KillBtn = new Border
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
                FontFamily = Mdl2Font,
                FontSize = 10,
                Foreground = Ui.Br(Theme.TextLow)
            };
            row.KillBtn.Child = killGlyph;
            row.KillBtn.MouseEnter += delegate
            {
                holder.KillBtn.Background = Ui.Br(Theme.DangerBg);
                killGlyph.Foreground = Ui.Br(Theme.DangerText);
            };
            row.KillBtn.MouseLeave += delegate
            {
                holder.KillBtn.Background = Brushes.Transparent;
                killGlyph.Foreground = Ui.Br(Theme.TextLow);
            };
            row.KillBtn.MouseLeftButtonUp += delegate { SafeKill(holder.ProcName); };
            Grid.SetColumn(row.KillBtn, 3);
            g.Children.Add(row.KillBtn);

            row.Root = g;
            row.Critical = false;
            return row;
        }

        private void UpdateTopRow(TopRow row, string name, string valueText, double ratio, bool critical)
        {
            row.ProcName = name;
            if (row.Name.Text != name) row.Name.Text = name;
            if (row.Value.Text != valueText) row.Value.Text = valueText;
            row.Ratio = ratio;
            row.Fill.Width = Math.Max(0, Math.Min(1, ratio)) * row.FillHost.ActualWidth;
            if (row.Critical != critical)
            {
                row.Critical = critical;
                row.KillBtn.Visibility = critical ? Visibility.Collapsed : Visibility.Visible;
                row.LockIcon.Visibility = critical ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ---------- Graceful close: close request → 5s wait → confirmed force kill ----------
        private static readonly string[] CriticalProcs = new string[]
        {
            "system", "idle", "secure system", "csrss", "wininit", "winlogon", "lsass",
            "services", "smss", "svchost", "dwm", "fontdrvhost", "registry",
            "memory compression", "memcompression", "runtimebroker", "sihost",
            "taskhostw", "ctfmon", "explorer", "audiodg", "conhost",
            "msmpeng", "securityhealthservice", "lsaiso", "sgrmbroker",
            "vmmem", "vmmemwsl"
        };

        private static readonly string SelfProcName = Process.GetCurrentProcess().ProcessName;

        // true Windows system processes — shown under "시스템 · 종료 불가" without the X button
        private static bool IsSystemProc(string procName)
        {
            string low = procName.ToLowerInvariant();
            foreach (var c in CriticalProcs)
                if (low == c) return true;
            return false;
        }

        // this running instance itself — X closes the app instead of the kill flow
        private static bool IsSelf(string procName)
        {
            return string.Equals(procName, SelfProcName, StringComparison.OrdinalIgnoreCase);
        }

        private void SafeKill(string procName)
        {
            if (IsSelf(procName))
            {
                if (MessageBox.Show(this, L.T("Close WinTotal (this app)?"),
                    L.T("Graceful Close"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    Close();
                return;
            }
            if (IsSystemProc(procName))
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

        // full-system process enumeration opens a handle per process and can take
        // hundreds of ms with many processes — never run it on the UI thread
        private void UpdateTopProcs()
        {
            if (_topProcsBusy) return;
            _topProcsBusy = true;
            Task.Run(delegate
            {
                try { ComputeTopProcs(); }
                catch { }
                finally { _topProcsBusy = false; }
            });
        }

        private void ComputeTopProcs()
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
            _procCount = procs.Length;
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

            double maxCpu = 0.01;
            foreach (var kv in topCpu) maxCpu = Math.Max(maxCpu, kv.Value[0] / denom * 100.0);
            var cpuRows = new List<Tuple<string, string, double, bool>>();
            foreach (var kv in topCpu)
            {
                double pct = kv.Value[0] / denom * 100.0;
                cpuRows.Add(Tuple.Create(kv.Key, string.Format("{0:F1}%", pct), pct / maxCpu, false));
            }

            double maxRam = 1;
            foreach (var kv in topRam) maxRam = Math.Max(maxRam, kv.Value[1]);
            var ramRows = new List<Tuple<string, string, double, bool>>();
            foreach (var kv in topRam)
            {
                double mb = kv.Value[1] / 1048576.0;
                string txt = mb >= 1024
                    ? string.Format("{0:F1} GB", mb / 1024.0)
                    : string.Format("{0:F0} MB", mb);
                ramRows.Add(Tuple.Create(kv.Key, txt, kv.Value[1] / maxRam, false));
            }

            // Top GPU processes (usage % from GPU Engine, VRAM from GPU Process Memory)
            // snapshot the dictionaries: the GPU sampler swaps in fresh instances, so a
            // local reference is always internally consistent
            var pidUsageSnap = _gpu.PidUsage;
            var pidBytesSnap = _gpuMem.PidBytes;
            var gpuAgg = new Dictionary<string, double[]>(); // name → [usage %, vram bytes]
            foreach (var kv in pidUsageSnap)
            {
                string nm;
                if (!pidName.TryGetValue(kv.Key, out nm)) continue;
                double[] slot;
                if (!gpuAgg.TryGetValue(nm, out slot)) { slot = new double[2]; gpuAgg[nm] = slot; }
                slot[0] += kv.Value;
            }
            foreach (var kv in pidBytesSnap)
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
            var gpuRows = new List<Tuple<string, string, double, bool>>();
            double maxG = 0.01;
            foreach (var kv in topGpu) maxG = Math.Max(maxG, kv.Value[0]);
            foreach (var kv in topGpu)
            {
                string gtxt = kv.Value[1] > 0
                    ? string.Format("{0:F0}% · {1:F1} GB", kv.Value[0], kv.Value[1] / 1073741824.0)
                    : string.Format("{0:F0}%", kv.Value[0]);
                gpuRows.Add(Tuple.Create(kv.Key, gtxt, Math.Min(1.0, kv.Value[0] / maxG), false));
            }

            var cpuOrdered = SplitBySystem(cpuRows);
            var ramOrdered = SplitBySystem(ramRows);
            var gpuOrdered = SplitBySystem(gpuRows);

            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (_colCpu == null) return;
                FillTopColumn(_colCpu, cpuOrdered);
                FillTopColumn(_colRam, ramOrdered);
                if (gpuOrdered.Count == 0) ShowGpuPlaceholder();
                else FillTopColumn(_colGpu, gpuOrdered);
            }));
        }

        // closable rows first, then the "system · cannot close" group (Item4 = system)
        private static List<Tuple<string, string, double, bool>> SplitBySystem(List<Tuple<string, string, double, bool>> rows)
        {
            var ordered = new List<Tuple<string, string, double, bool>>(rows.Count);
            foreach (var r in rows)
                if (!IsSystemProc(r.Item1)) ordered.Add(r);
            foreach (var r in rows)
                if (IsSystemProc(r.Item1)) ordered.Add(Tuple.Create(r.Item1, r.Item2, r.Item3, true));
            return ordered;
        }

        private void FillTopColumn(TopColumn col, List<Tuple<string, string, double, bool>> ordered)
        {
            var panel = col.Panel;
            panel.Children.Clear();
            bool labelAdded = false;
            for (int i = 0; i < ordered.Count; i++)
            {
                var r = ordered[i];
                if (r.Item4 && !labelAdded)
                {
                    if (col.GroupLabel == null) col.GroupLabel = SystemGroupLabel();
                    panel.Children.Add(col.GroupLabel);
                    labelAdded = true;
                }
                if (i >= col.Rows.Count) col.Rows.Add(MakeTopRow(col.Accent));
                var row = col.Rows[i];
                UpdateTopRow(row, r.Item1, r.Item2, r.Item3, r.Item4);
                panel.Children.Add(row.Root);
            }
        }

        private void ShowGpuPlaceholder()
        {
            var panel = _colGpu.Panel;
            panel.Children.Clear();
            if (_colGpu.Placeholder == null)
                _colGpu.Placeholder = new TextBlock
                {
                    Text = L.T("No active GPU processes"),
                    FontSize = 11.5,
                    Foreground = Ui.Br(Theme.TextLow),
                    Margin = new Thickness(15, 2, 0, 2)
                };
            panel.Children.Add(_colGpu.Placeholder);
        }

        private UIElement SystemGroupLabel()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(15, 3, 0, 5) };
            row.Children.Add(new TextBlock
            {
                Text = ((char)0xE72E).ToString(), // MDL2 Lock
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 9,
                Foreground = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 6, 0)
            });
            row.Children.Add(new TextBlock
            {
                Text = L.T("System · cannot close"),
                FontSize = 10,
                Foreground = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Center
            });
            return row;
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

        // the two Runs are created once (kept in Tag) and only their Text is updated each tick
        private static void SetBigValue(TextBlock tb, string num, string unit)
        {
            var runs = tb.Tag as Run[];
            if (runs == null)
            {
                var r1 = new Run(num)
                {
                    FontSize = 38,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                var r2 = new Run(unit ?? "")
                {
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Ui.Br(Theme.TextLow)
                };
                tb.Inlines.Add(r1);
                tb.Inlines.Add(r2);
                tb.Tag = new Run[] { r1, r2 };
                return;
            }
            if (runs[0].Text != num) runs[0].Text = num;
            string u = unit ?? "";
            if (runs[1].Text != u) runs[1].Text = u;
        }

        private void StartMonitoring()
        {
            // counter-catalog first load can block for seconds — create off the UI thread
            // (Tick null-guards _cpu/_disk until they are ready)
            Task.Run(delegate
            {
                try { var pc = new PerformanceCounter("Processor", "% Processor Time", "_Total"); pc.NextValue(); _cpu = pc; }
                catch { _cpu = null; }
                try { var pd = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total"); pd.NextValue(); _disk = pd; }
                catch { _disk = null; }
            });

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

            // Tick skips UI work while minimized; refresh immediately on restore
            StateChanged += delegate
            {
                if (WindowState != WindowState.Minimized && _timer != null) Tick();
            };

            RunHealthCheck(); // initial diagnosis; re-runs every 10 minutes from Tick
        }

        private void Tick()
        {
            _tickCount++;

            // nothing is rendered while minimized — skip the whole UI pass
            if (WindowState == WindowState.Minimized) return;

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
                    // process count is cached by the top-process aggregation (no extra full enumeration)
                    if (_procCount > 0)
                    {
                        var up = TimeSpan.FromMilliseconds(GetTickCount64());
                        _cpuSub.Text = string.Format(L.T("{0} processes · up {1}d {2}h {3}m"),
                            _procCount, up.Days, up.Hours, up.Minutes);
                    }
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

        // background + DriveType checked before IsReady: a disconnected network drive's
        // IsReady can block for seconds on SMB timeouts, and must never stall the UI thread
        private void UpdateDiskSub()
        {
            if (_diskSubBusy) return;
            _diskSubBusy = true;
            Task.Run(delegate
            {
                string txt = null;
                try
                {
                    var sb = new StringBuilder();
                    foreach (var d in DriveInfo.GetDrives())
                    {
                        if (d.DriveType != DriveType.Fixed || !d.IsReady) continue;
                        if (sb.Length > 0) sb.Append("   ");
                        sb.AppendFormat(L.T("{0} {1:F0} GB free"), d.Name.TrimEnd('\\'), d.AvailableFreeSpace / 1073741824.0);
                    }
                    txt = sb.ToString();
                }
                catch { }
                _diskSubBusy = false;
                if (txt != null)
                {
                    string done = txt;
                    Dispatcher.BeginInvoke(new Action(delegate { _diskSub.Text = done; }));
                }
            });
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
                        sys.Rows.Add(new[] { L.T("Manufacturer · Model"), (WmiStr(mo, "Manufacturer") + "  " + WmiStr(mo, "Model")).Trim() });
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
                            disk.Rows.Add(new[] { L.T("Disk ") + di, string.Format("{0} · {1:F0} GB", WmiStr(mo, "Model"), sz / 1000000000.0) });
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
                Text = L.T("Diagnosing..."),
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
            _healthStatus.Text = L.T("Diagnosing...");
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
                Text = L.T("Check first, then repair only if needed — for common Windows problems"),
                FontSize = 11.5,
                Foreground = Ui.Br(Theme.TextLow),
                Margin = new Thickness(1, 5, 0, 0)
            });
            if (!IsAdmin())
            {
                headLeft.Children.Add(new TextBlock
                {
                    Text = L.T("Not running as administrator — network reset, system file repair and Windows Temp cleanup may fail."),
                    FontSize = 11,
                    Foreground = Ui.Br(Theme.AccDisk),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(1, 4, 0, 0)
                });
            }
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

            // check-result row (dot + status text)
            var statusRow = new Grid { Margin = new Thickness(0, 7, 0, 0) };
            statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _fixStatusDot[id] = new Ellipse
            {
                Width = 7, Height = 7,
                Fill = Ui.Br(Theme.TextLow),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 7, 0)
            };
            Grid.SetColumn(_fixStatusDot[id], 0);
            statusRow.Children.Add(_fixStatusDot[id]);
            _fixStatusText[id] = new TextBlock
            {
                Text = L.T("Not checked yet"),
                FontSize = 11,
                Foreground = Ui.Br(Theme.TextLow),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(_fixStatusText[id], 1);
            statusRow.Children.Add(_fixStatusText[id]);
            left.Children.Add(statusRow);

            Grid.SetColumn(left, 0);
            g.Children.Add(left);
            var localTitle = title;
            var localDesc = desc;
            var btns = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var checkBtn = PillButton(null, L.T("Check"), Theme.BtnBg, Theme.BtnHover, Theme.BtnText,
                delegate { CheckFix(id, localTitle); });
            btns.Children.Add(checkBtn);
            _fixRepairBtn[id] = PillButton(null, L.T("Run"), Theme.DangerBg, Theme.DangerHover, Theme.DangerText,
                delegate { RunFix(id, localTitle, localDesc); });
            _fixRepairBtn[id].Margin = new Thickness(0, 8, 0, 0);
            _fixRepairBtn[id].Visibility = Visibility.Collapsed; // revealed after a check
            btns.Children.Add(_fixRepairBtn[id]);
            Grid.SetColumn(btns, 1);
            g.Children.Add(btns);
            card.Child = g;
            return card;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_fixRunning)
            {
                if (MessageBox.Show(this,
                    L.T("A repair is still running. Quit anyway?\n(The running command will continue in the background.)"),
                    "WinTotal", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnClosing(e);
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

        // buffered console log: bursts of output coalesce into one dispatcher pass,
        // and the console text is capped so long runs (sfc etc.) cannot grow memory unbounded
        private void FixLog(string line)
        {
            lock (_fixLogLock)
            {
                _fixLogBuf.AppendLine(line);
                if (_fixLogFlushQueued) return;
                _fixLogFlushQueued = true;
            }
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(FlushFixLog));
        }

        private void FlushFixLog()
        {
            string chunk;
            lock (_fixLogLock)
            {
                chunk = _fixLogBuf.ToString();
                _fixLogBuf.Length = 0;
                _fixLogFlushQueued = false;
            }
            if (_fixConsole == null || chunk.Length == 0) return;
            if (_fixConsole.Text.Length > 120000)
                _fixConsole.Text = _fixConsole.Text.Substring(_fixConsole.Text.Length - 60000);
            _fixConsole.AppendText(chunk);
            _fixConsole.ScrollToEnd();
        }

        private int RunCmdStep(string file, string args, bool unicodeOut, List<string> capture = null,
            int timeoutMs = 3600000)
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
                        if (!string.IsNullOrEmpty(e.Data) && e.Data.Trim().Length > 0)
                        {
                            FixLog("  " + e.Data.Trim());
                            if (capture != null) lock (capture) capture.Add(e.Data.Trim());
                        }
                    };
                    p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
                    {
                        if (!string.IsNullOrEmpty(e.Data) && e.Data.Trim().Length > 0) FixLog("  " + e.Data.Trim());
                    };
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    // bounded wait: a hung command must never lock the repair page forever
                    if (!p.WaitForExit(timeoutMs))
                    {
                        try { p.Kill(); } catch { }
                        FixLog("  " + L.T("Command timed out and was stopped."));
                        return -2;
                    }
                    p.WaitForExit(); // flush remaining async output events
                    return p.ExitCode;
                }
            }
            catch (Exception ex)
            {
                FixLog("  " + L.T("ERROR: ") + ex.Message);
                return -1;
            }
        }

        // EnumerateFiles/EnumerateDirectories keep memory flat even with tens of thousands of temp files
        private double CleanTempDir(string dir)
        {
            double freed = 0;
            long count = 0;
            try
            {
                var di = new DirectoryInfo(dir);
                if (!di.Exists) return 0;
                CleanDirRec(di, true, ref freed, ref count);
                FixLog(string.Format("  {0} — {1:F0} MB", dir, freed / 1048576.0));
            }
            catch { }
            return freed;
        }

        private void CleanDirRec(DirectoryInfo di, bool isRoot, ref double freed, ref long count)
        {
            try
            {
                foreach (var fi in di.EnumerateFiles())
                {
                    try
                    {
                        double len = fi.Length;
                        fi.Delete();
                        freed += len;
                        count++;
                        if (count % 2000 == 0) FixLog("  " + string.Format(L.T("{0} files deleted..."), count));
                    }
                    catch { }
                }
                foreach (var sub in di.EnumerateDirectories())
                {
                    try
                    {
                        // never descend into junctions/symlinks — their targets live outside the temp dir
                        if ((sub.Attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            try { sub.Delete(false); } catch { }
                            continue;
                        }
                        CleanDirRec(sub, false, ref freed, ref count);
                    }
                    catch { }
                }
                if (!isRoot)
                {
                    try { di.Delete(false); } catch { } // only removes now-empty folders
                }
            }
            catch { }
        }

        private static long DirSizeSafe(string dir, ref long fileCount)
        {
            long total = 0;
            try
            {
                var di = new DirectoryInfo(dir);
                if (!di.Exists) return 0;
                foreach (var fi in di.EnumerateFiles())
                {
                    try { total += fi.Length; fileCount++; }
                    catch { }
                }
                foreach (var sub in di.EnumerateDirectories())
                {
                    try
                    {
                        if ((sub.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                        total += DirSizeSafe(sub.FullName, ref fileCount);
                    }
                    catch { }
                }
            }
            catch { }
            return total;
        }

        private static string FmtBytes(long bytes)
        {
            double mb = bytes / 1048576.0;
            if (mb >= 1024) return string.Format("{0:F1} GB", mb / 1024.0);
            if (mb >= 1) return string.Format("{0:F0} MB", mb);
            return string.Format("{0:F0} KB", bytes / 1024.0);
        }

        private static bool IsAdmin()
        {
            try
            {
                return new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent())
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        // ---------- Check (read-only diagnosis) → repair flow ----------
        private void SetFixResult(int id, string dotColor, string statusText)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (_fixStatusDot[id] == null || _fixStatusText[id] == null) return;
                _fixStatusDot[id].Fill = Ui.Br(dotColor);
                _fixStatusText[id].Text = statusText;
                _fixStatusText[id].Foreground = Ui.Br(Theme.TextMid);
                if (_fixRepairBtn[id] != null) _fixRepairBtn[id].Visibility = Visibility.Visible;
            }));
        }

        private void CheckFix(int id, string title)
        {
            if (_fixRunning)
            {
                MessageBox.Show(this, L.T("Another fix is already running."), "WinTotal",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _fixRunning = true;
            _fixStatusDot[id].Fill = Ui.Br(Theme.TextLow);
            _fixStatusText[id].Text = L.T("Checking...");
            _fixStatusText[id].Foreground = Ui.Br(Theme.TextLow);
            FixLog("");
            FixLog("═══ " + L.T("Check") + ": " + title + " ═══");
            Task.Run(delegate
            {
                try
                {
                    switch (id)
                    {
                        case 0: CheckNetwork(id); break;
                        case 1: CheckDiskSpace(id); break;
                        case 2: CheckSystemFiles(id); break;
                        case 3: CheckExplorer(id); break;
                    }
                }
                catch (Exception ex)
                {
                    FixLog("  " + L.T("ERROR: ") + ex.Message);
                    SetFixResult(id, Theme.AccDisk, L.T("Check inconclusive — you can still run the repair"));
                }
                finally
                {
                    FixLog(L.T("Done."));
                    _fixRunning = false;
                }
            });
        }

        private void CheckNetwork(int id)
        {
            bool net = false, ping = false, dns = false;
            try { net = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable(); }
            catch { }
            if (net)
            {
                try
                {
                    using (var pi = new System.Net.NetworkInformation.Ping())
                    {
                        var rep = pi.Send("8.8.8.8", 2000);
                        ping = rep != null && rep.Status == System.Net.NetworkInformation.IPStatus.Success;
                        if (ping) FixLog("  " + string.Format(L.T("ping 8.8.8.8: OK ({0} ms)"), rep.RoundtripTime));
                    }
                }
                catch { }
                if (!ping) FixLog("  " + L.T("ping 8.8.8.8: failed"));
                try
                {
                    var ar = System.Net.Dns.BeginGetHostAddresses("www.microsoft.com", null, null);
                    if (ar.AsyncWaitHandle.WaitOne(3000))
                        dns = System.Net.Dns.EndGetHostAddresses(ar).Length > 0;
                }
                catch { }
                FixLog("  " + L.T(dns ? "DNS lookup: OK" : "DNS lookup: failed"));
            }
            if (!net)
                SetFixResult(id, Theme.DangerText, L.T("No network connection — check the adapter, then run repair"));
            else if (ping && dns)
                SetFixResult(id, Theme.AccRam, L.T("Network OK — repair not needed"));
            else
                SetFixResult(id, Theme.AccDisk, L.T("Connection problems detected — repair recommended"));
        }

        private void CheckDiskSpace(int id)
        {
            long files = 0;
            long tempBytes = DirSizeSafe(System.IO.Path.GetTempPath(), ref files)
                           + DirSizeSafe(System.IO.Path.Combine(
                                 Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), ref files);
            long rbBytes = 0, rbItems = 0;
            try
            {
                var q = new SHQUERYRBINFO();
                q.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
                if (SHQueryRecycleBin(null, ref q) == 0) { rbBytes = q.i64Size; rbItems = q.i64NumItems; }
            }
            catch { }
            FixLog("  " + string.Format(L.T("Temp files: {0} ({1} files)"), FmtBytes(tempBytes), files));
            FixLog("  " + string.Format(L.T("Recycle Bin: {0} ({1} items)"), FmtBytes(rbBytes), rbItems));
            long total = tempBytes + rbBytes;
            if (total < 50L * 1048576)
                SetFixResult(id, Theme.AccRam, string.Format(L.T("Nothing significant to clean (about {0})"), FmtBytes(total)));
            else
                SetFixResult(id, Theme.AccDisk, string.Format(
                    L.T("About {0} can be freed — temp files {1} · Recycle Bin {2}"),
                    FmtBytes(total), FmtBytes(tempBytes), FmtBytes(rbBytes)));
        }

        private void CheckSystemFiles(int id)
        {
            if (!IsAdmin())
            {
                FixLog("  " + L.T("Administrator rights are required for this check."));
                SetFixResult(id, Theme.AccDisk, L.T("Administrator rights are required for this check."));
                return;
            }
            // ImageHealthState enum values are invariant English even on localized Windows
            var lines = new List<string>();
            int code = RunCmdStep("powershell",
                "-NoProfile -ExecutionPolicy Bypass -Command \"(Repair-WindowsImage -Online -CheckHealth).ImageHealthState\"",
                false, lines);
            string state = null;
            lock (lines)
            {
                foreach (var ln in lines)
                {
                    var t = ln.Trim();
                    if (t == "Healthy" || t == "Repairable" || t == "NonRepairable") { state = t; break; }
                }
            }
            if (state != null) FixLog("  " + string.Format(L.T("Component store status: {0}"), state));
            if (code == 0 && state == "Healthy")
                SetFixResult(id, Theme.AccRam, L.T("No system file corruption detected — repair not needed"));
            else if (state == "Repairable" || state == "NonRepairable")
                SetFixResult(id, Theme.DangerText, L.T("Corruption detected — repair is recommended"));
            else
                SetFixResult(id, Theme.AccDisk, L.T("Check inconclusive — you can still run the repair"));
        }

        private void CheckExplorer(int id)
        {
            int count = 0;
            bool hung = false;
            try
            {
                var ps = Process.GetProcessesByName("explorer");
                count = ps.Length;
                foreach (var p in ps)
                {
                    try { if (!p.Responding) hung = true; }
                    catch { }
                    finally { try { p.Dispose(); } catch { } }
                }
            }
            catch { }
            string msg;
            string color;
            if (count == 0) { msg = L.T("Explorer is not running — restart needed"); color = Theme.DangerText; }
            else if (hung) { msg = L.T("Explorer is not responding — restart recommended"); color = Theme.AccDisk; }
            else { msg = L.T("Explorer is running normally"); color = Theme.AccRam; }
            FixLog("  " + msg);
            SetFixResult(id, color, msg);
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
                bool failed = false;
                try
                {
                    switch (id)
                    {
                        case 0: // network
                            if (LogStepResult(RunCmdStep("ipconfig", "/flushdns", false))) failed = true;
                            if (LogStepResult(RunCmdStep("netsh", "winsock reset", false))) failed = true;
                            if (LogStepResult(RunCmdStep("netsh", "int ip reset", false))) failed = true;
                            FixLog(L.T("A reboot is recommended."));
                            break;
                        case 1: // disk space
                            double freed = 0;
                            freed += CleanTempDir(System.IO.Path.GetTempPath());
                            freed += CleanTempDir(System.IO.Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                            try
                            {
                                int hr = SHEmptyRecycleBin(IntPtr.Zero, null, 0x7); // no confirm/progress/sound
                                FixLog("  " + (hr == 0
                                    ? L.T("Recycle Bin emptied")
                                    : L.T("Recycle Bin already empty (or could not be emptied)")));
                            }
                            catch { }
                            FixLog(string.Format(L.T("Freed {0:F0} MB"), freed / 1048576.0));
                            break;
                        case 2: // sfc — its exit code semantics are murky; only treat run/timeout errors as failure
                            if (RunCmdStep("sfc", "/scannow", true) < 0) failed = true;
                            break;
                        case 3: // explorer
                            RunCmdStep("taskkill", "/f /im explorer.exe", false);
                            System.Threading.Thread.Sleep(800);
                            try { Process.Start(new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true }); }
                            catch { }
                            FixLog("  " + L.T("Explorer restarted"));
                            break;
                    }
                    FixLog(failed
                        ? L.T("Finished, but some steps failed — see the log above.")
                        : L.T("Done."));
                    SetFixResult(id, failed ? Theme.AccDisk : Theme.TextLow,
                        L.T("Repair finished — run Check again to verify"));
                }
                catch (Exception ex)
                {
                    FixLog("  " + L.T("ERROR: ") + ex.Message);
                }
                finally { _fixRunning = false; }
            });
        }

        private bool LogStepResult(int exitCode)
        {
            if (exitCode == 0) return false;
            FixLog("  " + string.Format(L.T("Step failed (exit code {0})"), exitCode));
            return true;
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
            if (_appsLoading) return; // no duplicate scans (each spawns a PowerShell)
            _appsLoading = true;
            _appCountText.Text = L.T("Loading app list...");
            _appListPanel.Children.Clear();
            Task.Run(delegate
            {
                var list = new List<AppEntry>();
                ScanUninstallKeys(RegistryHive.LocalMachine, RegistryView.Registry64, list);
                ScanUninstallKeys(RegistryHive.LocalMachine, RegistryView.Registry32, list);
                ScanUninstallKeys(RegistryHive.CurrentUser, RegistryView.Registry64, list);
                bool storeOk = LoadStoreApps(list);

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
                return Tuple.Create(final, storeOk);
            }).ContinueWith(t =>
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _appsLoading = false;
                    _apps = t.IsFaulted ? new List<AppEntry>() : t.Result.Item1;
                    BuildAppRows();
                    if (!t.IsFaulted && !t.Result.Item2)
                        SetStatus(L.T("Store app list unavailable — showing desktop apps only."));
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

        // async output collection: a hung PowerShell is killed after 30s instead of
        // blocking ReadToEnd forever and leaving the app list stuck on "Loading..."
        private bool LoadStoreApps(List<AppEntry> list)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-AppxPackage | Where-Object { -not $_.IsFramework -and $_.SignatureKind -ne 'System' } | ForEach-Object { $_.Name + '|' + $_.PackageFullName + '|' + $_.Publisher + '|' + $_.Version }\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                var lines = new List<string>();
                using (var p = Process.Start(psi))
                {
                    p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
                    {
                        if (!string.IsNullOrEmpty(e.Data)) lock (lines) lines.Add(e.Data);
                    };
                    p.ErrorDataReceived += delegate { }; // drain so stderr can never fill the pipe
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    if (!p.WaitForExit(30000))
                    {
                        try { p.Kill(); } catch { }
                        return false;
                    }
                    p.WaitForExit(); // flush remaining async output events
                }
                lock (lines)
                {
                    foreach (var line in lines)
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
                return true;
            }
            catch { return false; }
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

        // rows are materialized in small dispatcher batches so a machine with hundreds of
        // installed apps never freezes the UI while the list is built
        private void BuildAppRows()
        {
            _appListPanel.Children.Clear();
            int gen = ++_rowBuildGen;
            RefreshCount();
            RebuildChips();
            AddAppRowBatch(0, gen);
        }

        private void AddAppRowBatch(int start, int gen)
        {
            if (gen != _rowBuildGen) return; // a newer rebuild superseded this one
            string q = (_searchBox.Text ?? "").Trim().ToLowerInvariant();
            int end = Math.Min(_apps.Count, start + 60);
            for (int i = start; i < end; i++)
            {
                var row = MakeAppRow(_apps[i]);
                row.Visibility = RowMatchesFilter(_apps[i], q) ? Visibility.Visible : Visibility.Collapsed;
                _appListPanel.Children.Add(row);
            }
            if (end < _apps.Count)
                Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate { AddAppRowBatch(end, gen); }));
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
                badge.Child = new TextBlock { Text = L.T("Store"), FontSize = 10, Foreground = Ui.Br("#5EAAFF") };
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

        private bool RowMatchesFilter(AppEntry a, string q)
        {
            bool catOk = _activeCategory == "All" || (a.Category ?? "Other") == _activeCategory;
            return catOk && (q.Length == 0
                || a.Name.ToLowerInvariant().Contains(q)
                || (a.Publisher ?? "").ToLowerInvariant().Contains(q)
                || (a.PackageFullName ?? "").ToLowerInvariant().Contains(q));
        }

        private void ApplyFilter()
        {
            string q = (_searchBox.Text ?? "").Trim().ToLowerInvariant();
            foreach (UIElement el in _appListPanel.Children)
            {
                var row = el as Border;
                if (row == null) continue;
                var a = row.Tag as AppEntry;
                row.Visibility = RowMatchesFilter(a, q) ? Visibility.Visible : Visibility.Collapsed;
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
                    if (still.Count == 0)
                    {
                        a.RunningPids = null;
                        SetStatus(string.Format(L.T("Processes of \"{0}\" closed."), a.Name));
                        return;
                    }
                    if (MessageBox.Show(this,
                        string.Format(L.T("{0} process(es) of \"{1}\" are still running.\nForce kill? Unsaved data may be lost."), still.Count, a.Name),
                        L.T("Force Kill"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        // declined: the processes are still alive — keep the pids so the badge stays truthful
                        var keep = new List<int>();
                        foreach (var p in still) { try { if (!p.HasExited) keep.Add(p.Id); } catch { } }
                        a.RunningPids = keep.Count > 0 ? keep : null;
                        SetStatus(L.T("Cancelled"));
                        return;
                    }
                    Task.Run(delegate
                    {
                        foreach (var p in still) { try { p.Kill(); } catch { } }
                        var left = new List<int>();
                        foreach (var p in still)
                        {
                            try { if (!p.WaitForExit(2000)) left.Add(p.Id); }
                            catch { }
                        }
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (left.Count > 0)
                            {
                                a.RunningPids = left;
                                SetStatus(string.Format(L.T("{0} process(es) did not close."), left.Count));
                            }
                            else
                            {
                                a.RunningPids = null;
                                SetStatus(string.Format(L.T("Processes of \"{0}\" closed."), a.Name));
                            }
                        }));
                    });
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
                    // success is verified (exit code + stderr); a hung PowerShell is killed
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = string.Format("-NoProfile -Command \"Remove-AppxPackage -Package '{0}'\"", pkg),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    };
                    var errLines = new List<string>();
                    using (var p = Process.Start(psi))
                    {
                        p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
                        {
                            if (!string.IsNullOrEmpty(e.Data)) lock (errLines) errLines.Add(e.Data);
                        };
                        p.BeginErrorReadLine();
                        if (!p.WaitForExit(120000))
                        {
                            try { p.Kill(); } catch { }
                            return false;
                        }
                        p.WaitForExit();
                        int errCount;
                        lock (errLines) errCount = errLines.Count;
                        return p.ExitCode == 0 && errCount == 0;
                    }
                }).ContinueWith(t =>
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        bool ok = !t.IsFaulted && t.Result;
                        if (ok)
                        {
                            _appListPanel.Children.Remove(row);
                            _apps.Remove(a);
                            RefreshCount();
                            SetStatus(string.Format(L.T("\"{0}\" uninstalled"), a.Name));
                        }
                        else
                        {
                            string msg = string.Format(
                                L.T("Could not uninstall \"{0}\". The app may be running or need administrator rights."), a.Name);
                            SetStatus(msg);
                            MessageBox.Show(this, msg, L.T("Uninstall App"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
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
                // registry sweep stays on this background thread — it can take seconds
                var removed = keyGone ? CleanRegistryLeftovers(a) : null;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (keyGone)
                    {
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

            // deleting a large install folder + sweeping the registry can take minutes —
            // run off the UI thread so the window never goes "not responding"
            SetStatus(L.T("Deleting leftovers..."));
            Task.Run(delegate
            {
                var removed = CleanRegistryLeftovers(a);
                int folders = 0;

                if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc) && IsSafeToDeleteDir(loc))
                {
                    try { Directory.Delete(loc, true); folders = 1; }
                    catch { }
                }

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _appListPanel.Children.Remove(row);
                    _apps.Remove(a);
                    RefreshCount();
                    string summary = string.Format(L.T("\"{0}\" force-deleted · {1} registry keys, {2} folders removed"),
                        a.Name, removed.Count, folders);
                    SetStatus(summary);
                    if (confirm)
                        MessageBox.Show(this, summary + (removed.Count > 0 ? "\n\n" + string.Join("\n", removed.Take(15)) : ""),
                            L.T("Done"), MessageBoxButton.OK, MessageBoxImage.Information);
                }));
            });
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
