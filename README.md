# Umamusume IL2CPP Dumper Wrapper

A C# Windows Forms application that automates the process of dumping IL2CPP resources from Umamusume Pretty Derby using the Trainers-Legend-G (TLG) framework.

## Features

- **Custom Kernel Bypass**: Implements its own kernel-level bypass without external dependencies
- **Automatic Process Management**: Handles the entire dump process from start to finish
- **Antivirus Detection**: Checks if antivirus is enabled and warns users
- **Virtualization Detection**: Detects if running in a virtualized environment
- **Real-time Logging**: Shows detailed progress and status information
- **Automatic Cleanup**: Properly terminates all processes when done

## Requirements

- Windows 10/11
- .NET 6.0 Runtime
- Umamusume Pretty Derby (DMM/Japanese version)
- Administrator privileges (for kernel operations)

## Installation

1. **Build the Application**:
   ```bash
   dotnet build -c Release
   ```

2. **Copy to Game Directory**:
   - Copy the built executable to your Umamusume game directory
   - Or run it from any location and specify the game directory

## Usage

### Step 1: Prepare Your Environment

1. **Disable Antivirus Temporarily**:
   - The application will detect if antivirus is enabled
   - Kernel bypass requires antivirus to be disabled to work properly
   - You can re-enable antivirus after the dump is complete

2. **Run as Administrator**:
   - The application requires administrator privileges for kernel operations
   - Right-click the application and select "Run as administrator"

### Step 2: Run the Dumper

1. **Launch the Application**:
   ```
   UmamusumeDumper.exe
   ```

2. **Configure Settings**:
   - **Game Directory**: Path to your Umamusume installation
   - **Dump Location**: Where to save the dumped files
   - Default paths are pre-filled for convenience

3. **Click "Start Dump"**:
   - The application will automatically:
     - Check antivirus status
     - Validate paths
     - Initialize custom kernel bypass
     - Launch the game
     - Wait for dump completion
     - Clean up processes and files

### Step 3: Monitor Progress

The application provides real-time logging showing:
- ✅ Success messages
- ❌ Error messages  
- ⚠️ Warning messages
- Detailed progress for each step

## Process Flow

1. **Antivirus Check**: Detects enabled antivirus software
2. **Virtualization Check**: Detects virtualized environment
3. **Path Validation**: Verifies game directory and required files
4. **Custom Kernel Bypass**: Initializes kernel-level bypass operations
5. **Game Launch**: Automatically launches Umamusume
6. **Dump Wait**: Waits for dump process to complete
7. **Cleanup**: Terminates processes and cleans up files

## Output

The dumped files will be saved to your specified dump location, including:
- IL2CPP metadata
- Game assembly information
- Class structures
- Method signatures
- Game-specific data

## Troubleshooting

### Common Issues

1. **"Antivirus is enabled"**:
   - Temporarily disable your antivirus
   - Add the game directory to antivirus exclusions

2. **"Kernel bypass failed"**:
   - Ensure you're running as administrator
   - Check if virtualization is properly detected
   - Verify antivirus is disabled

3. **"Game exited unexpectedly"**:
   - Check if the game requires additional setup
   - Verify game files are not corrupted

4. **"Dump failed"**:
   - Check the log for specific error messages
   - Ensure you have sufficient disk space
   - Try running as administrator

### Advanced Options

- **Custom TLG Configuration**: Modify TLG settings if needed
- **Extended Wait Times**: Adjust timing for slower systems
- **Debug Mode**: Enable detailed logging for troubleshooting

## Security Notes

- This application uses kernel-level drivers (via TLG)
- Only run on systems you trust
- The antivirus warning is important - heed it
- Use in a controlled environment when possible

## Technical Details

### Architecture
- **C# Windows Forms**: User interface
- **Process Management**: Handles TLG Starter and game processes
- **WMI Integration**: Antivirus and virtualization detection
- **Async Operations**: Non-blocking UI during operations

### Dependencies
- `System.Management`: For system information queries
- `System.Diagnostics.Process`: For process management
- Windows Forms: For the user interface

## License

This project is provided as-is for educational and research purposes. Use responsibly and in accordance with applicable laws and terms of service.

## Contributing

Feel free to submit issues and enhancement requests. This is a research tool for understanding game mechanics and should be used responsibly. 