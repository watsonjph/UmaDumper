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
        private TlgStarterManager? tlgStarterManager;

        // UI Controls
        private TextBox txtGameDirectory;
        private TextBox txtDumpLocation;
        private Button btnBrowseGame;
        private Button btnBrowseDump;
        private Button btnStartDump;
        private RichTextBox txtLog;
        private ProgressBar progressBar;
        private Label lblStatus;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeComponent()
        {
            this.Text = "UmaDumper V1.0";
            this.Size = new Size(600, 550);
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
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };

            // Progress Bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 410),
                Size = new Size(540, 20),
                Visible = false
            };

            // Status Label
            lblStatus = new Label
            {
                Location = new Point(20, 440),
                Size = new Size(540, 20),
                Text = "Ready to start dump process",
                ForeColor = Color.Black
            };

            // Credit Label at the bottom
            var lblCredit = new Label
            {
                Location = new Point(20, 470),
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
            progressBar.Value = 0;
            progressBar.Maximum = 100;
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

                // Step 2: Run tlg_starter.exe
                UpdateProgress(30, "Step 2: Running tlg_starter.exe...");
                tlgStarterManager = new TlgStarterManager(LogMessage, txtGameDirectory.Text);
                if (!await tlgStarterManager.RunTlgStarter())
                {
                    LogMessage("❌ tlg_starter.exe failed! Cannot proceed.");
                    return;
                }
                LogMessage("✅ tlg_starter.exe completed successfully.");

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
                tlgStarterManager.Cleanup();

                // Check if dump files were actually created
                var dumpDir = Path.Combine(txtGameDirectory.Text, "dump_output");
                if (Directory.Exists(dumpDir))
                {
                    var files = Directory.GetFiles(dumpDir, "*", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        UpdateProgress(100, $"✅ Dump process completed successfully! Found {files.Length} dump files.");
                        lblStatus.Text = "Dump completed successfully!";
                        lblStatus.ForeColor = Color.Green;
                        MessageBox.Show($"Dump process completed successfully!\nFound {files.Length} dump files.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        UpdateProgress(100, "⚠️  Dump process completed but no dump files were found.");
                        lblStatus.Text = "Dump completed but no files found!";
                        lblStatus.ForeColor = Color.Orange;
                        MessageBox.Show("Dump process completed but no dump files were found.\nThe game may not have launched properly or the version.dll may not have executed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    UpdateProgress(100, "❌ Dump process completed but no dump directory was created.");
                    lblStatus.Text = "Dump failed - no files created!";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show("Dump process completed but no dump directory was created.\nThe game may not have launched properly or the version.dll may not have executed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    
                    // Check if dump files have appeared in the game directory
                    var dumpDir = Path.Combine(txtGameDirectory.Text, "dump_output");
                    if (Directory.Exists(dumpDir))
                    {
                        var files = Directory.GetFiles(dumpDir, "*", SearchOption.AllDirectories);
                        if (files.Length > 0)
                        {
                            LogMessage($"✅ Found {files.Length} dump files in {dumpDir}!");
                            
                            // Check for specific dump files that indicate completion
                            var expectedFiles = new[] { "classes_dump.txt", "methods_dump.txt", "dump_summary.txt", "il2cpp_detailed_dump.txt", "detailed_summary.txt" };
                            var foundFiles = expectedFiles.Where(f => File.Exists(Path.Combine(dumpDir, f))).ToArray();
                            
                            if (foundFiles.Length >= 3) // At least 3 of the main dump files
                            {
                                LogMessage($"✅ Found {foundFiles.Length}/{expectedFiles.Length} expected dump files!");
                                
                                // Check for completion flag
                                var flagFile = Path.Combine(dumpDir, "dump_complete.flag");
                                if (File.Exists(flagFile))
                                {
                                    LogMessage("✅ Dump completion flag detected!");
                                    break;
                                }
                                else
                                {
                                    LogMessage("⚠️  Dump files found but completion flag not detected yet...");
                                }
                            }
                        }
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
                
                // Copy dump files to the specified dump location if different
                var sourceDumpDir = Path.Combine(txtGameDirectory.Text, "dump_output");
                if (Directory.Exists(sourceDumpDir))
                {
                    try
                    {
                        LogMessage("Copying dump files to specified location...");
                        
                        // Create target directory if it doesn't exist
                        if (!Directory.Exists(txtDumpLocation.Text))
                        {
                            Directory.CreateDirectory(txtDumpLocation.Text);
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
                        var mainDumpFiles = new[] { "classes_dump.txt", "methods_dump.txt", "dump_summary.txt", "il2cpp_detailed_dump.txt", "detailed_summary.txt", "GameAssembly_dumped.dll" };
                        foreach (var fileName in mainDumpFiles)
                        {
                            var filePath = Path.Combine(txtDumpLocation.Text, fileName);
                            if (File.Exists(filePath))
                            {
                                var fileInfo = new FileInfo(filePath);
                                LogMessage($"📄 {fileName} ({fileInfo.Length / 1024} KB)");
                            }
                        }
                        
                        // Show completion message
                        LogMessage("🎉 IL2CPP Dump completed successfully!");
                        LogMessage("📂 You can find the dump files in the specified dump location.");
                        
                        // Clean up temporary files in game directory
                        await CleanupGameDirectory();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"⚠️  Warning: Could not copy dump files: {ex.Message}");
                    }
                }
                else
                {
                    LogMessage("⚠️  Warning: No dump files found in dump_output directory!");
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
                
                // Remove tlg_starter.exe from game directory
                var tlgStarterPath = Path.Combine(txtGameDirectory.Text, "tlg_starter.exe");
                if (File.Exists(tlgStarterPath))
                {
                    try
                    {
                        File.Delete(tlgStarterPath);
                        LogMessage("✅ Removed tlg_starter.exe from game directory");
                    }
                    catch (Exception ex)
                    {
                        // Don't log if we can't delete tlg_starter.exe
                    }
                }
                
                // Note: dump_output directory is left in game directory for user reference
                // It will be cleaned up by the game or user manually if needed
                
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

            tlgStarterManager?.Cleanup();

            base.OnFormClosing(e);
        }
    }
} 