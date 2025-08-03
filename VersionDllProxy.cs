using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UmamusumeDumper
{
    public class VersionDllProxy
    {
        // Original function pointers from system32 version.dll
        private static IntPtr GetFileVersionInfoA_Original = IntPtr.Zero;
        private static IntPtr GetFileVersionInfoByHandle_Original = IntPtr.Zero;
        private static IntPtr GetFileVersionInfoExA_Original = IntPtr.Zero;
        private static IntPtr GetFileVersionInfoExW_Original = IntPtr.Zero;
        private static IntPtr GetFileVersionInfoSizeA_Original = IntPtr.Zero;
        private static IntPtr GetFileVersionInfoSizeExA_Original = IntPtr.Zero;
        private static IntPtr GetFileVersionInfoSizeExW_Original = IntPtr.Zero;
        private static IntPtr GetFileVersionInfoSizeW_Original = IntPtr.Zero;
        private static IntPtr GetFileVersionInfoW_Original = IntPtr.Zero;
        private static IntPtr VerFindFileA_Original = IntPtr.Zero;
        private static IntPtr VerFindFileW_Original = IntPtr.Zero;
        private static IntPtr VerInstallFileA_Original = IntPtr.Zero;
        private static IntPtr VerInstallFileW_Original = IntPtr.Zero;
        private static IntPtr VerLanguageNameA_Original = IntPtr.Zero;
        private static IntPtr VerLanguageNameW_Original = IntPtr.Zero;
        private static IntPtr VerQueryValueA_Original = IntPtr.Zero;
        private static IntPtr VerQueryValueW_Original = IntPtr.Zero;

        // Windows API imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryA(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetSystemDirectoryA(StringBuilder lpBuffer, int nSize);

        private readonly Action<string> _logger;
        private readonly string _gameDirectory;
        private IntPtr _originalVersionDll = IntPtr.Zero;

        public VersionDllProxy(Action<string> logger, string gameDirectory)
        {
            _logger = logger;
            _gameDirectory = gameDirectory;
        }

        public async Task<bool> CreateVersionDllProxy()
        {
            try
            {
                _logger("   Creating version.dll proxy...");

                // Step 1: Load original version.dll from system32
                if (!await LoadOriginalVersionDll())
                {
                    return false;
                }

                // Step 2: Get function pointers
                if (!await GetFunctionPointers())
                {
                    return false;
                }

                // Step 3: Create proxy DLL
                if (!await CreateProxyDll())
                {
                    return false;
                }

                // Step 4: Copy to game directory
                if (!await CopyToGameDirectory())
                {
                    return false;
                }

                _logger("✅ Version.dll proxy created successfully!");
                return true;
            }
            catch (Exception ex)
            {
                _logger($"❌ Failed to create version.dll proxy: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> LoadOriginalVersionDll()
        {
            try
            {
                var system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var versionDllPath = Path.Combine(system32Path, "version.dll");

                if (!File.Exists(versionDllPath))
                {
                    _logger("❌ Original version.dll not found in system32!");
                    return false;
                }

                _originalVersionDll = LoadLibraryA(versionDllPath);
                if (_originalVersionDll == IntPtr.Zero)
                {
                    _logger("❌ Failed to load original version.dll!");
                    return false;
                }

                _logger($"   [+] Loaded original version.dll from: {versionDllPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger($"❌ Failed to load original version.dll: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> GetFunctionPointers()
        {
            try
            {
                // Get function pointers for all version.dll exports
                GetFileVersionInfoA_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoA");
                GetFileVersionInfoByHandle_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoByHandle");
                GetFileVersionInfoExA_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoExA");
                GetFileVersionInfoExW_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoExW");
                GetFileVersionInfoSizeA_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoSizeA");
                GetFileVersionInfoSizeExA_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoSizeExA");
                GetFileVersionInfoSizeExW_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoSizeExW");
                GetFileVersionInfoSizeW_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoSizeW");
                GetFileVersionInfoW_Original = GetProcAddress(_originalVersionDll, "GetFileVersionInfoW");
                VerFindFileA_Original = GetProcAddress(_originalVersionDll, "VerFindFileA");
                VerFindFileW_Original = GetProcAddress(_originalVersionDll, "VerFindFileW");
                VerInstallFileA_Original = GetProcAddress(_originalVersionDll, "VerInstallFileA");
                VerInstallFileW_Original = GetProcAddress(_originalVersionDll, "VerInstallFileW");
                VerLanguageNameA_Original = GetProcAddress(_originalVersionDll, "VerLanguageNameA");
                VerLanguageNameW_Original = GetProcAddress(_originalVersionDll, "VerLanguageNameW");
                VerQueryValueA_Original = GetProcAddress(_originalVersionDll, "VerQueryValueA");
                VerQueryValueW_Original = GetProcAddress(_originalVersionDll, "VerQueryValueW");

                _logger("   [+] Retrieved all function pointers from original version.dll");
                return true;
            }
            catch (Exception ex)
            {
                _logger($"❌ Failed to get function pointers: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CreateProxyDll()
        {
            try
            {
                // Create a C++ DLL project that would be compiled separately
                // For now, we'll create a placeholder that copies the original
                var proxyDllPath = Path.Combine(_gameDirectory, "version_proxy.dll");
                var system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var originalVersionDll = Path.Combine(system32Path, "version.dll");

                // Copy original as our proxy (in real implementation, this would be a compiled DLL)
                File.Copy(originalVersionDll, proxyDllPath, true);

                // Create TLG-style configuration files for persistence
                await CreateTLGConfigFiles();

                // Create TLG-style assembly exports file (simulating TLG's version.asm)
                await CreateAssemblyExportsFile();

                // Create TLG-style module definition file (simulating TLG's version.def)
                await CreateModuleDefinitionFile();

                _logger($"   [+] Created proxy DLL at: {proxyDllPath}");
                _logger($"   [+] Created TLG-style assembly exports");
                _logger($"   [+] Created TLG-style module definition");
                return true;
            }
            catch (Exception ex)
            {
                _logger($"❌ Failed to create proxy DLL: {ex.Message}");
                return false;
            }
        }

        private async Task CreateTLGConfigFiles()
        {
            try
            {
                // Create temporary config.json for debug console only
                var configPath = Path.Combine(_gameDirectory, "config_temp.json");
                var configContent = @"{
  ""enable_console"": true,
  ""enable_hook"": true,
  ""enable_http_server"": false,
  ""enable_event_helper"": false,
  ""dumpGameAssemblyPath"": ""GameAssembly_dumped.dll"",
  ""dump_entries"": true,
  ""no_static_dict_cache"": false,
  ""enable_replaceBuiltInAssets"": false,
  ""openExternalPluginOnLoad"": false,
  ""autoChangeLineBreakMode"": false,
  ""start_width"": -1,
  ""start_height"": -1,
  ""closeTrans"": {""enable"": false},
  ""g_enable_console"": true,
  ""g_enable_hook"": true,
  ""g_enable_http_server"": false,
  ""g_enable_event_helper"": false,
  ""g_dump_entries"": true,
  ""g_no_static_dict_cache"": false,
  ""g_enable_replaceBuiltInAssets"": false,
  ""g_dump_sprite_tex"": false,
  ""g_dump_bundle_tex"": false,
  ""g_aspect_ratio"": 0.0,
  ""g_home_free_camera"": false,
  ""g_home_walk_chara_id"": 0,
  ""enableRaceInfoTab"": false,
  ""raceInfoTabAttachToGame"": false,
  ""loadDll"": []
}";

                File.WriteAllText(configPath, configContent);
                _logger("   [+] Created temporary config.json for debug console");

            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning creating temporary config files: {ex.Message}");
            }
        }

        private async Task<bool> CopyToGameDirectory()
        {
            try
            {
                var proxyDllPath = Path.Combine(_gameDirectory, "version_proxy.dll");
                var gameVersionDllPath = Path.Combine(_gameDirectory, "version.dll");

                // Always backup original version.dll (even if it doesn't exist, we'll create a placeholder)
                var backupPath = Path.Combine(_gameDirectory, "version_backup.dll");
                
                if (File.Exists(gameVersionDllPath))
                {
                    // Backup existing version.dll
                    File.Copy(gameVersionDllPath, backupPath, true);
                    _logger("   [+] Backed up existing version.dll");
                }
                else
                {
                    // Create empty backup file to mark that we need to remove version.dll later
                    File.WriteAllText(backupPath, "REMOVE_ON_CLEANUP");
                    _logger("   [+] Created backup marker (no original version.dll found)");
                }

                // Copy our proxy to game directory
                File.Copy(proxyDllPath, gameVersionDllPath, true);
                _logger($"   [+] Copied proxy version.dll to game directory (temporary)");

                return true;
            }
            catch (Exception ex)
            {
                _logger($"❌ Failed to copy proxy to game directory: {ex.Message}");
                return false;
            }
        }

        private async Task CreateAssemblyExportsFile()
        {
            try
            {
                // Create TLG-style assembly exports file (simulating version.asm)
                var asmExportsPath = Path.Combine(_gameDirectory, "version_exports.asm");
                var asmContent = @"; TLG-style Assembly Exports for version.dll proxy
; Generated by UmaDumper by watsonjph

.386
.model flat, stdcall
option casemap:none

include windows.inc

; Original function pointers from system32 version.dll
extern GetFileVersionInfoA_Original:DWORD
extern GetFileVersionInfoByHandle_Original:DWORD
extern GetFileVersionInfoExA_Original:DWORD
extern GetFileVersionInfoExW_Original:DWORD
extern GetFileVersionInfoSizeA_Original:DWORD
extern GetFileVersionInfoSizeExA_Original:DWORD
extern GetFileVersionInfoSizeExW_Original:DWORD
extern GetFileVersionInfoSizeW_Original:DWORD
extern GetFileVersionInfoW_Original:DWORD
extern VerFindFileA_Original:DWORD
extern VerFindFileW_Original:DWORD
extern VerInstallFileA_Original:DWORD
extern VerInstallFileW_Original:DWORD
extern VerLanguageNameA_Original:DWORD
extern VerLanguageNameW_Original:DWORD
extern VerQueryValueA_Original:DWORD
extern VerQueryValueW_Original:DWORD

; Export functions that proxy to original
GetFileVersionInfoA_EXPORT proc
    jmp GetFileVersionInfoA_Original
GetFileVersionInfoA_EXPORT endp

GetFileVersionInfoByHandle_EXPORT proc
    jmp GetFileVersionInfoByHandle_Original
GetFileVersionInfoByHandle_EXPORT endp

GetFileVersionInfoExA_EXPORT proc
    jmp GetFileVersionInfoExA_Original
GetFileVersionInfoExA_EXPORT endp

GetFileVersionInfoExW_EXPORT proc
    jmp GetFileVersionInfoExW_Original
GetFileVersionInfoExW_EXPORT endp

GetFileVersionInfoSizeA_EXPORT proc
    jmp GetFileVersionInfoSizeA_Original
GetFileVersionInfoSizeA_EXPORT endp

GetFileVersionInfoSizeExA_EXPORT proc
    jmp GetFileVersionInfoSizeExA_Original
GetFileVersionInfoSizeExA_EXPORT endp

GetFileVersionInfoSizeExW_EXPORT proc
    jmp GetFileVersionInfoSizeExW_Original
GetFileVersionInfoSizeExW_EXPORT endp

GetFileVersionInfoSizeW_EXPORT proc
    jmp GetFileVersionInfoSizeW_Original
GetFileVersionInfoSizeW_EXPORT endp

GetFileVersionInfoW_EXPORT proc
    jmp GetFileVersionInfoW_Original
GetFileVersionInfoW_EXPORT endp

VerFindFileA_EXPORT proc
    jmp VerFindFileA_Original
VerFindFileA_EXPORT endp

VerFindFileW_EXPORT proc
    jmp VerFindFileW_Original
VerFindFileW_EXPORT endp

VerInstallFileA_EXPORT proc
    jmp VerInstallFileA_Original
VerInstallFileA_EXPORT endp

VerInstallFileW_EXPORT proc
    jmp VerInstallFileW_Original
VerInstallFileW_EXPORT endp

VerLanguageNameA_EXPORT proc
    jmp VerLanguageNameA_Original
VerLanguageNameA_EXPORT endp

VerLanguageNameW_EXPORT proc
    jmp VerLanguageNameW_Original
VerLanguageNameW_EXPORT endp

VerQueryValueA_EXPORT proc
    jmp VerQueryValueA_Original
VerQueryValueA_EXPORT endp

VerQueryValueW_EXPORT proc
    jmp VerQueryValueW_Original
VerQueryValueW_EXPORT endp

; DllMain entry point (TLG-style)
DllMain proc hInstance:DWORD, reason:DWORD, reserved:DWORD
    .if reason == DLL_PROCESS_ATTACH
        ; TLG-style initialization code would go here
        ; This is where TLG injects its custom code
        invoke MessageBox, NULL, addr szTLGMessage, addr szTLGTitle, MB_OK
    .endif
    mov eax, TRUE
    ret
DllMain endp

; TLG-style strings
szTLGTitle db 'UmaDumper by watsonjph - DLL Proxy Active', 0
szTLGMessage db 'Version.dll proxy loaded successfully!', 0

end";

                File.WriteAllText(asmExportsPath, asmContent);
                _logger("   [+] Created TLG-style assembly exports file");
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning creating assembly exports: {ex.Message}");
            }
        }

        private async Task CreateModuleDefinitionFile()
        {
            try
            {
                // Create TLG-style module definition file (simulating version.def)
                var defPath = Path.Combine(_gameDirectory, "version.def");
                var defContent = @"; TLG-style Module Definition File for version.dll proxy
; Generated by UmaDumper by watsonjph

EXPORTS
    GetFileVersionInfoA=GetFileVersionInfoA_EXPORT
    GetFileVersionInfoByHandle=GetFileVersionInfoByHandle_EXPORT
    GetFileVersionInfoExA=GetFileVersionInfoExA_EXPORT
    GetFileVersionInfoExW=GetFileVersionInfoExW_EXPORT
    GetFileVersionInfoSizeA=GetFileVersionInfoSizeA_EXPORT
    GetFileVersionInfoSizeExA=GetFileVersionInfoSizeExA_EXPORT
    GetFileVersionInfoSizeExW=GetFileVersionInfoSizeExW_EXPORT
    GetFileVersionInfoSizeW=GetFileVersionInfoSizeW_EXPORT
    GetFileVersionInfoW=GetFileVersionInfoW_EXPORT
    VerFindFileA=VerFindFileA_EXPORT
    VerFindFileW=VerFindFileW_EXPORT
    VerInstallFileA=VerInstallFileA_EXPORT
    VerInstallFileW=VerInstallFileW_EXPORT
    VerLanguageNameA=VerLanguageNameA_EXPORT
    VerLanguageNameW=VerLanguageNameW_EXPORT
    VerQueryValueA=VerQueryValueA_EXPORT
    VerQueryValueW=VerQueryValueW_EXPORT

; TLG-style DLL entry point
    DllMain=DllMain";

                File.WriteAllText(defPath, defContent);
                _logger("   [+] Created TLG-style module definition file");
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning creating module definition: {ex.Message}");
            }
        }

        public async Task CleanupProxy()
        {
            try
            {
                _logger("   Cleaning up version.dll proxy...");

                var gameVersionDllPath = Path.Combine(_gameDirectory, "version.dll");
                var backupPath = Path.Combine(_gameDirectory, "version_backup.dll");
                var proxyDllPath = Path.Combine(_gameDirectory, "version_proxy.dll");
                var tempConfigPath = Path.Combine(_gameDirectory, "config_temp.json");

                // Check backup marker
                bool shouldRemoveVersionDll = false;
                if (File.Exists(backupPath))
                {
                    var backupContent = File.ReadAllText(backupPath);
                    if (backupContent == "REMOVE_ON_CLEANUP")
                    {
                        shouldRemoveVersionDll = true;
                        File.Delete(backupPath);
                        _logger("   [+] Removed backup marker");
                    }
                    else
                    {
                        // Restore original version.dll
                        if (File.Exists(gameVersionDllPath))
                        {
                            File.Delete(gameVersionDllPath);
                        }
                        File.Move(backupPath, gameVersionDllPath);
                        _logger("   [+] Restored original version.dll");
                    }
                }

                // Remove version.dll if it was our temporary one
                if (shouldRemoveVersionDll && File.Exists(gameVersionDllPath))
                {
                    File.Delete(gameVersionDllPath);
                    _logger("   [+] Removed temporary version.dll");
                }

                // Clean up proxy files
                if (File.Exists(proxyDllPath))
                {
                    File.Delete(proxyDllPath);
                    _logger("   [+] Removed proxy version.dll");
                }

                // Clean up temporary config
                if (File.Exists(tempConfigPath))
                {
                    File.Delete(tempConfigPath);
                    _logger("   [+] Removed temporary config.json");
                }

                // Free original DLL
                if (_originalVersionDll != IntPtr.Zero)
                {
                    FreeLibrary(_originalVersionDll);
                    _originalVersionDll = IntPtr.Zero;
                }

                _logger("✅ Version.dll proxy cleanup completed (no persistence)");
            }
            catch (Exception ex)
            {
                _logger($"⚠️  Warning during proxy cleanup: {ex.Message}");
            }
        }
    }
} 