using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Security.Principal;

namespace UmamusumeDumper
{
    public class TlgStarterManager
    {
        private readonly Action<string> _logger;
        private readonly string _gameDirectory;
        private readonly string _resourcesPath;

        public TlgStarterManager(Action<string> logger, string gameDirectory)
        {
            _logger = logger;
            _gameDirectory = gameDirectory;
            _resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        }

        public async Task<bool> RunTlgStarter()
        {
            try
            {
                _logger("🔧 Starting tlg_starter.exe...");

                // Check administrator privileges
                if (!IsRunningAsAdministrator())
                {
                    _logger("❌ Application must be run as Administrator!");
                    _logger("   Please right-click on UmaDumper.exe and select 'Run as Administrator'");
                    return false;
                }

                // Check if tlg_starter.exe exists in Resources
                var tlgStarterPath = Path.Combine(_resourcesPath, "tlg_starter.exe");
                if (!File.Exists(tlgStarterPath))
                {
                    _logger("❌ tlg_starter.exe not found in Resources folder!");
                    _logger("   Please ensure tlg_starter.exe is placed in the Resources folder");
                    return false;
                }

                // Check for existing tlg_starter.exe processes
                var existingProcesses = Process.GetProcessesByName("tlg_starter");
                bool processAlreadyRunning = existingProcesses.Length > 0;
                
                // Define the game tlg_starter path for use in both branches
                var gameTlgStarterPath = Path.Combine(_gameDirectory, "tlg_starter.exe");
                
                if (processAlreadyRunning)
                {
                    _logger($"ℹ️  Found {existingProcesses.Length} existing tlg_starter.exe process(es), proceeding normally...");
                    // Don't kill existing processes, just proceed normally
                }
                else
                {
                    // Kill any existing tlg_starter.exe processes
                    await KillExistingTlgStarterProcesses();

                    // Wait a bit more for file handles to be released
                    await Task.Delay(2000);

                    // Copy tlg_starter.exe to game directory
                    try
                    {
                        // Try to remove existing file with multiple attempts
                        for (int attempt = 1; attempt <= 3; attempt++)
                        {
                            try
                            {
                                if (File.Exists(gameTlgStarterPath))
                                {
                                    File.Delete(gameTlgStarterPath);
                                    _logger($"✅ Removed existing tlg_starter.exe from game directory (attempt {attempt})");
                                }
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (attempt == 3)
                                {
                                    _logger($"❌ Failed to remove existing tlg_starter.exe after 3 attempts: {ex.Message}");
                                    return false;
                                }
                                _logger($"⚠️  Attempt {attempt} failed to remove existing tlg_starter.exe, retrying...");
                                await Task.Delay(1000);
                            }
                        }

                        // Copy the file with multiple attempts
                        for (int attempt = 1; attempt <= 3; attempt++)
                        {
                            try
                            {
                                File.Copy(tlgStarterPath, gameTlgStarterPath, true);
                                _logger($"✅ Copied tlg_starter.exe to game directory (attempt {attempt})");
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (attempt == 3)
                                {
                                    _logger($"❌ Failed to copy tlg_starter.exe after 3 attempts: {ex.Message}");
                                    _logger("   This may be due to insufficient permissions or file being in use");
                                    return false;
                                }
                                _logger($"⚠️  Attempt {attempt} failed to copy tlg_starter.exe, retrying...");
                                await Task.Delay(1000);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger($"❌ Failed to copy tlg_starter.exe: {ex.Message}");
                        _logger("   This may be due to insufficient permissions or file being in use");
                        return false;
                    }
                }

                // Copy version.dll to game directory
                var versionDllPath = Path.Combine(_resourcesPath, "version.dll");
                if (File.Exists(versionDllPath))
                {
                    var gameVersionDllPath = Path.Combine(_gameDirectory, "version.dll");
                    try
                    {
                        // Try to remove existing file with multiple attempts
                        for (int attempt = 1; attempt <= 3; attempt++)
                        {
                            try
                            {
                                if (File.Exists(gameVersionDllPath))
                                {
                                    File.Delete(gameVersionDllPath);
                                    _logger($"✅ Removed existing version.dll from game directory (attempt {attempt})");
                                }
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (attempt == 3)
                                {
                                    _logger($"⚠️  Warning: Failed to remove existing version.dll after 3 attempts: {ex.Message}");
                                    break;
                                }
                                await Task.Delay(1000);
                            }
                        }

                        // Copy the file with multiple attempts
                        for (int attempt = 1; attempt <= 3; attempt++)
                        {
                            try
                            {
                                File.Copy(versionDllPath, gameVersionDllPath, true);
                                _logger($"✅ Copied version.dll to game directory (attempt {attempt})");
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (attempt == 3)
                                {
                                    _logger($"⚠️  Warning: Failed to copy version.dll after 3 attempts: {ex.Message}");
                                    break;
                                }
                                await Task.Delay(1000);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger($"⚠️  Warning: Failed to copy version.dll: {ex.Message}");
                    }
                }

                // Set environment variable
                Environment.SetEnvironmentVariable("TLG_PATH", _gameDirectory);

                if (processAlreadyRunning)
                {
                    _logger("✅ tlg_starter.exe is already running, skipping process launch");
                    return true;
                }
                else
                {
                    // Run tlg_starter.exe silently and capture output
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = gameTlgStarterPath,
                        WorkingDirectory = _gameDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = startInfo };
                    
                    // Capture output
                    var outputBuffer = new System.Text.StringBuilder();
                    var readyMessage = "Now you can start umamusume.";
                    var isReady = false;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            var message = $"[UmaKernel] {e.Data}";
                            _logger(message);
                            outputBuffer.AppendLine(e.Data);
                            
                            // Check if the ready message appears
                            if (e.Data.Contains(readyMessage))
                            {
                                isReady = true;
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            _logger($"[UmaKernel-ERR] {e.Data}");
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Wait for tlg_starter to indicate readiness or exit
                    var timeout = TimeSpan.FromMinutes(2); // 2 minute timeout
                    var startTime = DateTime.Now;
                    
                    while (!process.HasExited && !isReady)
                    {
                        await Task.Delay(100);
                        
                        // Check for timeout
                        if (DateTime.Now - startTime > timeout)
                        {
                            _logger("⚠️  Timeout waiting for tlg_starter.exe to indicate readiness");
                            try
                            {
                                if (!process.HasExited)
                                {
                                    process.Kill();
                                    await process.WaitForExitAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger($"⚠️  Warning: Could not terminate timed out process: {ex.Message}");
                            }
                            return false;
                        }
                    }

                    // If we detected the ready message, wait a bit more for any additional output
                    if (isReady)
                    {
                        _logger("✅ tlg_starter.exe indicates ready to launch game");
                        await Task.Delay(1000); // Wait a bit more for any final output
                        return true;
                    }

                    // If process exited, check exit code
                    if (process.ExitCode == 0)
                    {
                        _logger("✅ tlg_starter.exe completed successfully");
                        return true;
                    }
                    else
                    {
                        _logger($"❌ tlg_starter.exe failed with exit code: {process.ExitCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger($"❌ Failed to run tlg_starter.exe: {ex.Message}");
                return false;
            }
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private async Task KillExistingTlgStarterProcesses()
        {
            try
            {
                var processes = Process.GetProcessesByName("tlg_starter");
                if (processes.Length > 0)
                {
                    _logger($"ℹ️  Found {processes.Length} existing tlg_starter.exe process(es), proceeding normally...");
                    // Don't kill existing processes, just proceed normally
                }
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning: Could not check for existing tlg_starter.exe processes: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            try
            {
                // Kill any remaining tlg_starter.exe processes
                var processes = Process.GetProcessesByName("tlg_starter");
                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            _logger($"✅ Terminated remaining tlg_starter.exe process (PID: {process.Id})");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger($"⚠️  Warning: Could not terminate tlg_starter.exe process (PID: {process.Id}): {ex.Message}");
                    }
                }

                // Remove tlg_starter.exe from game directory
                var gameTlgStarterPath = Path.Combine(_gameDirectory, "tlg_starter.exe");
                if (File.Exists(gameTlgStarterPath))
                {
                    try
                    {
                        File.Delete(gameTlgStarterPath);
                        _logger("✅ Removed tlg_starter.exe from game directory");
                    }
                    catch (Exception ex)
                    {
                        // Don't log if we can't delete tlg_starter.exe
                    }
                }

                // Remove version.dll from game directory
                var gameVersionDllPath = Path.Combine(_gameDirectory, "version.dll");
                if (File.Exists(gameVersionDllPath))
                {
                    try
                    {
                        File.Delete(gameVersionDllPath);
                        _logger("✅ Removed version.dll from game directory");
                    }
                    catch (Exception ex)
                    {
                        _logger($"⚠️  Warning: Could not remove version.dll: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning during cleanup: {ex.Message}");
            }
        }
    }
} 