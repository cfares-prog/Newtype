using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Newtype.Server.Hubs;
using Newtype.Shared.Models;

namespace Newtype.Server.Services;

public class ExecutionService
{
    private readonly IHubContext<CompilerHub> _hubContext;

    public ExecutionService(IHubContext<CompilerHub> hubContext) => _hubContext = hubContext;

    public async Task RunCompilerAsync(string connectionId, CompileRequest request)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NewtypeBuilds", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        var filePath = Path.Combine(tempDir, request.FileName);
        await File.WriteAllTextAsync(filePath, request.SourceCode);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "gcc",
                Arguments = $"{filePath} -o {tempDir}/out",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = tempDir
            }
        };

        process.OutputDataReceived += async (s, e) => {
            if (e.Data != null) await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", e.Data);
        };
        process.ErrorDataReceived += async (s, e) => {
            if (e.Data != null) await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", $"Error: {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        
        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", "--- Process Exited ---");
    }
}
