using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Newtype.Server.Hubs;
using Newtype.Shared.Models;
using System.Runtime.InteropServices;

namespace Newtype.Server.Services;

public class ExecutionService
{
    private readonly IHubContext<CompilerHub> _hubContext;

    public ExecutionService(IHubContext<CompilerHub> hubContext) => _hubContext = hubContext;

    public async Task RunCompilerAsync(string connectionId, CompileRequest request)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NewtypeBuilds", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var filePath = Path.Combine(tempDir, request.FileName);
            await File.WriteAllTextAsync(filePath, request.SourceCode);

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var exeName = isWindows ? "out.exe" : "out";
            var exePath = Path.Combine(tempDir, exeName);

            using var compileProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gcc",
                    Arguments = $"\"{filePath}\" -o \"{exePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                }
            };

            compileProcess.OutputDataReceived += async (s, e) => {
                if (e.Data != null) await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", e.Data);
            };
            compileProcess.ErrorDataReceived += async (s, e) => {
                if (e.Data != null) await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", $"Compiler Error: {e.Data}");
            };

            compileProcess.Start();
            compileProcess.BeginOutputReadLine();
            compileProcess.BeginErrorReadLine();
            await compileProcess.WaitForExitAsync();

            if (compileProcess.ExitCode != 0)
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", "--- Compilation Failed ---");
                return;
            }

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", "--- Compilation Succeeded. Running... ---");

            using var runProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                }
            };

            runProcess.OutputDataReceived += async (s, e) => {
                if (e.Data != null) await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", e.Data);
            };
            runProcess.ErrorDataReceived += async (s, e) => {
                if (e.Data != null) await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", $"Error: {e.Data}");
            };

            runProcess.Start();
            runProcess.BeginOutputReadLine();
            runProcess.BeginErrorReadLine();
            await runProcess.WaitForExitAsync();
            
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", $"--- Process Exited with code {runProcess.ExitCode} ---");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
