using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing; // Added for Icon
using System.Linq; // Added for Where

namespace UmamusumeDumper
{
    public partial class MainForm : Form
    {
        private Process? gameProcess;
        private bool isDumping = false;

        // UI Controls
        private TextBox txtGameDirectory = null!;
        private TextBox txtDumpLocation = null!;
        private Button btnBrowseGame = null!;
        private Button btnBrowseDump = null!;
        private Button btnStartDump = null!;
        private RichTextBox txtLog = null!;
        private ProgressBar progressBar = null!;
        private Label lblStatus = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeComponent()
        {
            this.Text = "UmaDumper V1.1";
            this.Size = new Size(600, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            
            // Set the form icon
            try
            {
                var iconPath = Path.Combine(Application.StartupPath, "Resources", "icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch
            {
                // Ignore icon loading errors
            }
        }

        private void InitializeUI()
        {
            // Game Directory Section
            var lblGameDir = new Label
            {
                Text = "Game Directory:",
                Location = new Point(20, 20),
                Size = new Size(100, 20)
            };

            txtGameDirectory = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(400, 23),
                Text = GetDefaultGameDirectory()
            };

            btnBrowseGame = new Button
            {
                Text = "Browse",
                Location = new Point(430, 44),
                Size = new Size(80, 25)
            };
            btnBrowseGame.Click += BtnBrowseGame_Click;

            // Dump Location Section
            var lblDumpDir = new Label
            {
                Text = "Dump Location:",
                Location = new Point(20, 80),
                Size = new Size(100, 20)
            };

            txtDumpLocation = new TextBox
            {
                Location = new Point(20, 105),
                Size = new Size(400, 23),
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "UmamusumeDump")
            };

            btnBrowseDump = new Button
            {
                Text = "Browse",
                Location = new Point(430, 104),
                Size = new Size(80, 25)
            };
            btnBrowseDump.Click += BtnBrowseDump_Click;

            // Start Button
            btnStartDump = new Button
            {
                Text = "Start Dump",
                Location = new Point(20, 150),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen
            };
            btnStartDump.Click += BtnStartDump_Click;

            // Log Display
            txtLog = new RichTextBox
            {
                Location = new Point(20, 200),
                Size = new Size(540, 200),
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9)
            };

            // Progress Bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 400),
                Size = new Size(540, 20),
                Visible = false
            };

            // Status Label
            lblStatus = new Label
            {
                Location = new Point(20, 430),
                Size = new Size(540, 20),
                Text = "Ready to start dump process",
                ForeColor = Color.Black
            };

            // Credit Label
            var lblCredit = new Label
            {
                Location = new Point(20, 460),
                Size = new Size(540, 20),
                Text = "Made with ❤️ by watsonjph",
                ForeColor = Color.Gray,
                Font = new Font("Arial", 8),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Add controls to form
            this.Controls.AddRange(new Control[]
            {
                lblGameDir, txtGameDirectory, btnBrowseGame,
                lblDumpDir, txtDumpLocation, btnBrowseDump,
                btnStartDump, txtLog, progressBar, lblStatus, lblCredit
            });
        }

        private void BtnBrowseGame_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select Umamusume Game Directory",
                ShowNewFolderButton = false
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtGameDirectory.Text = folderDialog.SelectedPath;
            }
        }

        private void BtnBrowseDump_Click(object? sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select Dump Output Directory",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtDumpLocation.Text = folderDialog.SelectedPath;
            }
        }

        private async void BtnStartDump_Click(object? sender, EventArgs e)
        {
            if (isDumping) return;
            await StartDumpProcess();
        }

        private async Task StartDumpProcess()
        {
            isDumping = true;
            btnStartDump.Enabled = false;
            progressBar.Visible = true;
            txtLog.Clear();

            try
            {
                LogMessage("Starting Umamusume IL2CPP Dumper...");
                LogMessage("=====================================");

                // Step 0: Check administrator privileges
                UpdateProgress(10, "Step 0: Checking administrator privileges...");
                if (!IsRunningAsAdministrator())
                {
                    LogMessage("❌ Application must be run as Administrator!");
                    LogMessage("   Please right-click on UmaDumper.exe and select 'Run as Administrator'");
                    LogMessage("   This is required to access game files and manage processes.");
                    return;
                }
                LogMessage("✅ Administrator privileges confirmed.");

                // Step 1: Validate paths
                UpdateProgress(20, "Step 1: Validating paths...");
                if (!ValidatePaths())
                {
                    return;
                }
                LogMessage("✅ Paths validated.");

                // Step 2: Copy version.dll to game directory
                UpdateProgress(30, "Step 2: Copying version.dll to game directory...");
                if (!await CopyVersionDll())
                {
                    LogMessage("❌ Failed to copy version.dll! Cannot proceed.");
                    return;
                }
                LogMessage("✅ version.dll copied successfully.");

                // Step 3: Launch game
                UpdateProgress(50, "Step 3: Launching UmamusumePrettyDerby.exe...");
                if (!await LaunchGame())
                {
                    LogMessage("❌ Game launch failed! Cannot proceed with dumping.");
                    LogMessage("   Please check that:");
                    LogMessage("   1. The game directory path is correct");
                    LogMessage("   2. UmamusumePrettyDerby.exe exists in the game directory");
                    LogMessage("   3. You have permission to run the game");
                    return;
                }
                LogMessage("✅ Game launched successfully.");

                // Step 4: Wait for dump completion
                UpdateProgress(70, "Step 4: Waiting for dump completion...");
                await WaitForDumpCompletion();

                // Step 5: Cleanup
                UpdateProgress(90, "Step 5: Cleaning up...");
                await CleanupGameDirectory();

                // Final status update based on whether files were found in dump location
                var finalDumpFiles = Directory.GetFiles(txtDumpLocation.Text, "*", SearchOption.AllDirectories);
                if (finalDumpFiles.Length > 0)
                {
                    UpdateProgress(100, $"✅ Dump process completed successfully! Found {finalDumpFiles.Length} dump files.");
                    lblStatus.Text = "Dump completed successfully!";
                    lblStatus.ForeColor = Color.Green;
                    MessageBox.Show($"Dump process completed successfully!\nFound {finalDumpFiles.Length} dump files in the specified location.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    UpdateProgress(100, "❌ Dump process completed but no dump files were found.");
                    lblStatus.Text = "Dump failed - no files created!";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show("Dump process completed but no dump files were found.\nThe game may not have launched properly or the version.dll may not have executed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error during dump process: {ex.Message}");
                lblStatus.Text = "Dump failed!";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show($"Error during dump process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isDumping = false;
                btnStartDump.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private async Task<bool> CopyVersionDll()
        {
            try
            {
                var resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                var versionDllPath = Path.Combine(resourcesPath, "version.dll");
                
                if (!File.Exists(versionDllPath))
                {
                    LogMessage("❌ version.dll not found in Resources folder!");
                    LogMessage("   Please ensure version.dll is placed in the Resources folder");
                    return false;
                }

                var gameVersionDllPath = Path.Combine(txtGameDirectory.Text, "version.dll");
                
                // Try to remove existing file with multiple attempts
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        if (File.Exists(gameVersionDllPath))
                        {
                            File.Delete(gameVersionDllPath);
                            LogMessage($"✅ Removed existing version.dll from game directory (attempt {attempt})");
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (attempt == 3)
                        {
                            LogMessage($"⚠️  Warning: Failed to remove existing version.dll after 3 attempts: {ex.Message}");
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
                        LogMessage($"✅ Copied version.dll to game directory (attempt {attempt})");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        if (attempt == 3)
                        {
                            LogMessage($"❌ Failed to copy version.dll after 3 attempts: {ex.Message}");
                            LogMessage("   This may be due to insufficient permissions or file being in use");
                            return false;
                        }
                        LogMessage($"⚠️  Attempt {attempt} failed to copy version.dll, retrying...");
                        await Task.Delay(1000);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Failed to copy version.dll: {ex.Message}");
                return false;
            }
        }

        private string GetDefaultGameDirectory()
        {
            var steamPath = @"C:\Program Files (x86)\Steam\steamapps\common\UmamusumePrettyDerby";
            if (Directory.Exists(steamPath))
            {
                return steamPath;
            }

            var steamLibraryPath = @"C:\Program Files\Steam\steamapps\common\UmamusumePrettyDerby";
            if (Directory.Exists(steamLibraryPath))
            {
                return steamLibraryPath;
            }

            return @"C:\Program Files (x86)\Steam\steamapps\common\UmamusumePrettyDerby";
        }

        private bool ValidatePaths()
        {
            // Validate game directory
            if (string.IsNullOrEmpty(txtGameDirectory.Text) || !Directory.Exists(txtGameDirectory.Text))
            {
                LogMessage("❌ Invalid game directory!");
                return false;
            }

            // Check for game executable
            var gameExePath = Path.Combine(txtGameDirectory.Text, "UmamusumePrettyDerby.exe");
            if (!File.Exists(gameExePath))
            {
                LogMessage("❌ UmamusumePrettyDerby.exe not found in game directory!");
                return false;
            }

            // Validate dump location
            if (string.IsNullOrEmpty(txtDumpLocation.Text))
            {
                LogMessage("❌ Invalid dump location!");
                return false;
            }

            // Create dump directory if it doesn't exist
            try
            {
                Directory.CreateDirectory(txtDumpLocation.Text);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Cannot create dump directory: {ex.Message}");
                return false;
            }

            LogMessage("✅ Path validation passed.");
            return true;
        }

        private async Task<bool> LaunchGame()
        {
            try
            {
                var gameExePath = Path.Combine(txtGameDirectory.Text, "UmamusumePrettyDerby.exe");
                
                // Check if the game executable exists
                if (!File.Exists(gameExePath))
                {
                    LogMessage($"❌ Game executable not found: {gameExePath}");
                    return false;
                }
                
                LogMessage($"Starting game: {gameExePath}");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = gameExePath,
                    WorkingDirectory = txtGameDirectory.Text,
                    UseShellExecute = true
                };

                gameProcess = Process.Start(startInfo);
                if (gameProcess == null)
                {
                    LogMessage("❌ Failed to start game process!");
                    return false;
                }

                // Wait a moment to see if the process starts successfully
                await Task.Delay(2000);
                
                // Check if the process is still running
                if (gameProcess.HasExited)
                {
                    LogMessage($"❌ Game process exited immediately with code: {gameProcess.ExitCode}");
                    return false;
                }

                LogMessage($"✅ Game process started successfully (PID: {gameProcess.Id})");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Failed to launch game: {ex.Message}");
                return false;
            }
        }

        private async Task WaitForDumpCompletion()
        {
            try
            {
                LogMessage("Waiting for game to complete dumping...");
                
                // Check if game process is valid
                if (gameProcess == null)
                {
                    LogMessage("❌ Game process is null - game may not have started properly!");
                    return;
                }
                
                // Wait for game process to exit or for dump files to appear
                while (!gameProcess.HasExited)
                {
                    await Task.Delay(1000);
                    
                    // Check multiple possible dump locations
                    var waitPossibleDumpDirs = new[]
                    {
                        Path.Combine(txtGameDirectory.Text, "dump_output"),
                        Path.Combine(txtGameDirectory.Text, "basic_dump"),
                        Path.Combine(txtGameDirectory.Text, "dump"),
                        txtGameDirectory.Text // Check root game directory for dump files
                    };
                    
                    bool foundDumpFiles = false;
                    string waitFoundDumpDir = "";
                    
                    foreach (var dumpDir in waitPossibleDumpDirs)
                    {
                        if (Directory.Exists(dumpDir))
                        {
                            var files = Directory.GetFiles(dumpDir, "*", SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                LogMessage($"✅ Found {files.Length} dump files in {dumpDir}!");
                                foundDumpFiles = true;
                                waitFoundDumpDir = dumpDir;
                                
                                // Check for various dump file patterns
                                var possibleDumpFiles = new[]
                                {
                                    "classes_dump.txt", "methods_dump.txt", "dump_summary.txt", 
                                    "il2cpp_detailed_dump.txt", "detailed_summary.txt",
                                    "basic_dump.txt", "dump.txt", "summary.txt",
                                    "classes.txt", "methods.txt", "il2cpp.txt"
                                };
                                
                                var foundFiles = possibleDumpFiles.Where(f => File.Exists(Path.Combine(dumpDir, f))).ToArray();
                                
                                if (foundFiles.Length >= 1) // At least 1 dump file
                                {
                                    LogMessage($"✅ Found {foundFiles.Length} dump files: {string.Join(", ", foundFiles)}");
                                    
                                    // Check for completion flag or GameAssembly_dumped.dll
                                    var flagFile = Path.Combine(dumpDir, "dump_complete.flag");
                                    var gameAssemblyDumped = Path.Combine(txtGameDirectory.Text, "GameAssembly_dumped.dll");
                                    
                                    if (File.Exists(flagFile) || File.Exists(gameAssemblyDumped))
                                    {
                                        LogMessage("✅ Dump completion detected!");
                                        break;
                                    }
                                    else
                                    {
                                        LogMessage("⚠️  Dump files found but completion not detected yet...");
                                    }
                                }
                                break;
                            }
                        }
                    }
                    
                    if (foundDumpFiles)
                    {
                        break;
                    }
                }

                // If game is still running, wait a bit more for version.dll to kill it
                if (!gameProcess.HasExited)
                {
                    LogMessage("Waiting for version.dll to terminate game...");
                    await Task.Delay(5000);
                    
                    if (!gameProcess.HasExited)
                    {
                        LogMessage("⚠️  Game process still running, terminating manually...");
                        try
                        {
                            gameProcess.Kill();
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"⚠️  Warning: Could not terminate game process: {ex.Message}");
                        }
                    }
                }

                LogMessage("✅ Game process completed.");
                
                // Find and copy dump files from various possible locations
                var copyPossibleDumpDirs = new[]
                {
                    Path.Combine(txtGameDirectory.Text, "dump_output"),
                    Path.Combine(txtGameDirectory.Text, "basic_dump"),
                    Path.Combine(txtGameDirectory.Text, "dump"),
                    txtGameDirectory.Text // Check root game directory for dump files
                };
                
                string sourceDumpDir = "";
                bool foundDumpDir = false;
                
                foreach (var dumpDir in copyPossibleDumpDirs)
                {
                    if (Directory.Exists(dumpDir))
                    {
                        var files = Directory.GetFiles(dumpDir, "*", SearchOption.AllDirectories);
                        if (files.Length > 0)
                        {
                            sourceDumpDir = dumpDir;
                            foundDumpDir = true;
                            LogMessage($"📁 Found dump directory: {dumpDir} with {files.Length} files");
                            break;
                        }
                    }
                }
                
                if (foundDumpDir)
                {
                    try
                    {
                        LogMessage("Copying dump files to specified location...");
                        
                        // Create target directory if it doesn't exist
                        if (!string.IsNullOrEmpty(txtDumpLocation.Text) && !Directory.Exists(txtDumpLocation.Text))
                        {
                            Directory.CreateDirectory(txtDumpLocation.Text!);
                        }
                        
                        // Copy all files from dump directory
                        foreach (var file in Directory.GetFiles(sourceDumpDir, "*", SearchOption.AllDirectories))
                        {
                            var relativePath = Path.GetRelativePath(sourceDumpDir, file);
                            var targetPath = Path.Combine(txtDumpLocation.Text, relativePath);
                            var targetDir = Path.GetDirectoryName(targetPath);
                            
                            if (!Directory.Exists(targetDir))
                            {
                                Directory.CreateDirectory(targetDir);
                            }
                            
                            File.Copy(file, targetPath, true);
                        }
                        
                        // Copy dumped GameAssembly.dll if it exists
                        var gameAssemblyPath = Path.Combine(txtGameDirectory.Text, "GameAssembly_dumped.dll");
                        if (File.Exists(gameAssemblyPath))
                        {
                            var targetGameAssemblyPath = Path.Combine(txtDumpLocation.Text, "GameAssembly_dumped.dll");
                            File.Copy(gameAssemblyPath, targetGameAssemblyPath, true);
                            LogMessage("✅ Copied GameAssembly_dumped.dll to dump location");
                        }
                        
                        LogMessage($"✅ Dump files copied to: {txtDumpLocation.Text}");
                        
                        // Show summary of copied files
                        var copiedFiles = Directory.GetFiles(txtDumpLocation.Text, "*", SearchOption.AllDirectories);
                        LogMessage($"📁 Total files copied: {copiedFiles.Length}");
                        
                        // List the main dump files
                        var mainDumpFiles = new[] { 
                            "classes_dump.txt", "methods_dump.txt", "dump_summary.txt", 
                            "il2cpp_detailed_dump.txt", "detailed_summary.txt", 
                            "basic_dump.txt", "dump.txt", "summary.txt",
                            "classes.txt", "methods.txt", "il2cpp.txt",
                            "GameAssembly_dumped.dll" 
                        };
                        foreach (var fileName in mainDumpFiles)
                        {
                            var filePath = Path.Combine(txtDumpLocation.Text, fileName);
                            if (File.Exists(filePath))
                            {
                                var fileInfo = new FileInfo(filePath);
                                LogMessage($"📄 {fileName} ({fileInfo.Length / 1024} KB)");
                            }
                        }
                        
                        // Check if dump was successful by verifying files were copied to the dump location
                        LogMessage("🔍 Verifying dump success...");
                        var verificationFiles = Directory.GetFiles(txtDumpLocation.Text, "*", SearchOption.AllDirectories);
                        if (verificationFiles.Length > 0)
                        {
                            LogMessage($"✅ Dump verification successful! Found {verificationFiles.Length} files in dump location.");
                            
                            // Show completion message
                            LogMessage("🎉 IL2CPP Dump completed successfully!");
                            LogMessage("📂 You can find the dump files in the specified dump location.");
                            
                            // Delete dump folder from game directory after successful verification
                            LogMessage($"🧹 Cleaning up dump folder from game directory: {sourceDumpDir}");
                            try
                            {
                                Directory.Delete(sourceDumpDir, true);
                                LogMessage($"✅ Successfully deleted dump folder from game directory: {Path.GetFileName(sourceDumpDir)}");
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"⚠️  Warning: Could not delete dump folder from game directory: {ex.Message}");
                            }
                            
                            // Clean up other temporary files in game directory
                            await CleanupGameDirectory();
                        }
                        else
                        {
                            LogMessage("❌ Dump verification failed - no files were copied to dump location!");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"⚠️  Warning: Could not copy dump files: {ex.Message}");
                    }
                }
                else
                {
                    LogMessage("⚠️  Warning: No dump files found in any expected dump directories!");
                    LogMessage("   Checked locations:");
                    foreach (var dumpDir in copyPossibleDumpDirs)
                    {
                        LogMessage($"   - {dumpDir}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️  Warning during dump completion wait: {ex.Message}");
            }
        }

        private async Task CleanupGameDirectory()
        {
            try
            {
                LogMessage("🧹 Cleaning up temporary files in game directory...");
                
                // Remove version.dll from game directory
                var versionDllPath = Path.Combine(txtGameDirectory.Text, "version.dll");
                if (File.Exists(versionDllPath))
                {
                    try
                    {
                        File.Delete(versionDllPath);
                        LogMessage("✅ Removed version.dll from game directory");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"⚠️  Warning: Could not remove version.dll: {ex.Message}");
                    }
                }
                
                // Remove GameAssembly_dumped.dll from game directory
                var gameAssemblyDumpedPath = Path.Combine(txtGameDirectory.Text, "GameAssembly_dumped.dll");
                if (File.Exists(gameAssemblyDumpedPath))
                {
                    try
                    {
                        File.Delete(gameAssemblyDumpedPath);
                        LogMessage("✅ Removed GameAssembly_dumped.dll from game directory");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"⚠️  Warning: Could not remove GameAssembly_dumped.dll: {ex.Message}");
                    }
                }
                
                LogMessage("✅ Cleanup completed");
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️  Warning during cleanup: {ex.Message}");
            }
        }

        private void UpdateProgress(int percentage, string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => UpdateProgress(percentage, message)));
                return;
            }

            progressBar.Value = percentage;
            LogMessage(message);
        }

        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] {message}\n");
            txtLog.ScrollToCaret();
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isDumping)
            {
                var result = MessageBox.Show("Dump process is still running. Do you want to cancel?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Cleanup
            if (gameProcess != null && !gameProcess.HasExited)
            {
                try
                {
                    gameProcess.Kill();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            base.OnFormClosing(e);
        }
    }
} 