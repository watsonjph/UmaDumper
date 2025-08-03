using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UmamusumeDumper
{
    public class DebugConsole
    {
        // Windows API imports for console creation
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleTitle(string lpConsoleTitle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, ushort wAttributes);

        private readonly Action<string> _logger;
        private readonly string _gameDirectory;
        private bool _consoleCreated = false;

        // Console colors
        private const ushort FOREGROUND_RED = 0x0004;
        private const ushort FOREGROUND_GREEN = 0x0002;
        private const ushort FOREGROUND_BLUE = 0x0001;
        private const ushort FOREGROUND_INTENSITY = 0x0008;

        public DebugConsole(Action<string> logger, string gameDirectory)
        {
            _logger = logger;
            _gameDirectory = gameDirectory;
        }

        public async Task<bool> CreateDebugConsole()
        {
            try
            {
                _logger("   Creating debug console...");

                // Allocate console
                if (!AllocConsole())
                {
                    _logger("❌ Failed to allocate console!");
                    return false;
                }

                // Set console title
                var consoleTitle = "UmaDumper by watsonjph - Debug Console";
                SetConsoleTitle(consoleTitle);

                // Set window title
                var consoleWindow = GetConsoleWindow();
                if (consoleWindow != IntPtr.Zero)
                {
                    SetWindowText(consoleWindow, consoleTitle);
                }

                _consoleCreated = true;
                _logger("   [+] Debug console created successfully");

                // Show initial debug information
                await ShowDebugInfo();

                return true;
            }
            catch (Exception ex)
            {
                _logger($"❌ Failed to create debug console: {ex.Message}");
                return false;
            }
        }

        private async Task ShowDebugInfo()
        {
            try
            {
                var consoleOutput = GetStdHandle(-11); // STD_OUTPUT_HANDLE

                // Show header
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_GREEN | FOREGROUND_INTENSITY);
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    UMA DUMPER BY WATSONJPH                  ║");
                Console.WriteLine("║                        DEBUG CONSOLE                        ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine();

                // Show system info
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_BLUE | FOREGROUND_INTENSITY);
                Console.WriteLine("📋 System Information:");
                Console.WriteLine($"   OS Version: {Environment.OSVersion}");
                Console.WriteLine($"   Game Directory: {_gameDirectory}");
                Console.WriteLine($"   Process ID: {Process.GetCurrentProcess().Id}");
                Console.WriteLine($"   Working Directory: {Environment.CurrentDirectory}");
                Console.WriteLine();

                // Show debug status
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_GREEN | FOREGROUND_INTENSITY);
                Console.WriteLine("🔧 Debug Console Status: ACTIVE");
                Console.WriteLine("📊 Dumping Process: MONITORING");
                Console.WriteLine("🎮 Game Process: WAITING FOR LAUNCH");
                Console.WriteLine();

                // Show what will be dumped
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_BLUE | FOREGROUND_INTENSITY);
                Console.WriteLine("📦 Dumping Targets:");
                Console.WriteLine("   ✓ GameAssembly.dll (Protected IL2CPP Binary)");
                Console.WriteLine("   ✓ IL2CPP Metadata (Classes, Methods, Fields)");
                Console.WriteLine("   ✓ Game Text Resources (Localization Data)");
                Console.WriteLine("   ✓ Racing Data (Horse Stats, Skills, etc.)");
                Console.WriteLine("   ✓ Event Data (Story Events, Live Events)");
                Console.WriteLine();

                // Show console commands
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_RED | FOREGROUND_INTENSITY);
                Console.WriteLine("⚠️  IMPORTANT: This console will show real-time dumping progress.");
                Console.WriteLine("   The game will launch automatically after kernel bypass.");
                Console.WriteLine("   Do not close this console until dumping is complete.");
                Console.WriteLine();

                // Reset color
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE);

                _logger("   [+] Debug console initialized with system information");
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning showing debug info: {ex.Message}");
            }
        }

        public async Task LogDebugMessage(string message, DebugLevel level = DebugLevel.Info)
        {
            try
            {
                if (!_consoleCreated) return;

                var consoleOutput = GetStdHandle(-11);
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

                switch (level)
                {
                    case DebugLevel.Info:
                        SetConsoleTextAttribute(consoleOutput, FOREGROUND_GREEN | FOREGROUND_INTENSITY);
                        Console.WriteLine($"[{timestamp}] ℹ️  {message}");
                        break;
                    case DebugLevel.Warning:
                        SetConsoleTextAttribute(consoleOutput, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_INTENSITY);
                        Console.WriteLine($"[{timestamp}] ⚠️  {message}");
                        break;
                    case DebugLevel.Error:
                        SetConsoleTextAttribute(consoleOutput, FOREGROUND_RED | FOREGROUND_INTENSITY);
                        Console.WriteLine($"[{timestamp}] ❌ {message}");
                        break;
                    case DebugLevel.Success:
                        SetConsoleTextAttribute(consoleOutput, FOREGROUND_GREEN | FOREGROUND_INTENSITY);
                        Console.WriteLine($"[{timestamp}] ✅ {message}");
                        break;
                    case DebugLevel.Progress:
                        SetConsoleTextAttribute(consoleOutput, FOREGROUND_BLUE | FOREGROUND_INTENSITY);
                        Console.WriteLine($"[{timestamp}] 🔄 {message}");
                        break;
                }

                // Reset color
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE);

                // Also log to main application
                _logger($"Debug Console: {message}");
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning logging debug message: {ex.Message}");
            }
        }

        public async Task ShowDumpingProgress(string step, int progress = 0)
        {
            try
            {
                if (!_consoleCreated) return;

                var consoleOutput = GetStdHandle(-11);
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

                SetConsoleTextAttribute(consoleOutput, FOREGROUND_BLUE | FOREGROUND_INTENSITY);
                Console.WriteLine($"[{timestamp}] 🔄 DUMPING PROGRESS: {step}");

                if (progress > 0)
                {
                    var progressBar = CreateProgressBar(progress);
                    Console.WriteLine($"   Progress: {progressBar} {progress}%");
                }

                Console.WriteLine();

                // Reset color
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE);
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning showing progress: {ex.Message}");
            }
        }

        private string CreateProgressBar(int percentage)
        {
            const int barLength = 30;
            var filledLength = (int)(barLength * percentage / 100.0);
            var bar = new string('█', filledLength) + new string('░', barLength - filledLength);
            return $"[{bar}]";
        }

        public async Task ShowCompletionMessage()
        {
            try
            {
                if (!_consoleCreated) return;

                var consoleOutput = GetStdHandle(-11);
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

                SetConsoleTextAttribute(consoleOutput, FOREGROUND_GREEN | FOREGROUND_INTENSITY);
                Console.WriteLine();
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                        DUMPING COMPLETE!                    ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine($"[{timestamp}] ✅ All dumping operations completed successfully!");
                Console.WriteLine("   You can now close this console window.");
                Console.WriteLine("   The game will be terminated automatically.");
                Console.WriteLine();

                // Reset color
                SetConsoleTextAttribute(consoleOutput, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE);

                _logger("   [+] Debug console completion message displayed");
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning showing completion: {ex.Message}");
            }
        }

        public enum DebugLevel
        {
            Info,
            Warning,
            Error,
            Success,
            Progress
        }
    }
} 