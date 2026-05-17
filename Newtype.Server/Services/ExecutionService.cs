using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Newtype.Server.Data;
using Newtype.Server.Hubs;
using Newtype.Server.Models;
using Newtype.Shared.Models;
using System.Runtime.InteropServices;

namespace Newtype.Server.Services;

public class ExecutionService
{
    private readonly IHubContext<CompilerHub> _hubContext;
    private readonly AppDbContext _dbContext;

    public ExecutionService(IHubContext<CompilerHub> hubContext, AppContext dbContext) 
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
    }

    public async Task RunCompilerAsync(string connectionId, CompileRequest request)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NewtypeBuilds", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        bool compileSuccess = false;

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
            bool compileSucces = (compileProcess.ExitCode == 0);

            if (!compileProcess)
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
            var log = new SubmissionLog
            {
                ConnectionId = connectionId,
                FileName = request.FileName,
                SourceCode = request.SourceCode,
                IsSuccess = compileSuccess
            };

            _dbContext.SubmissionLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
