using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Newtype.Server.Data;
using Newtype.Server.Hubs;
using Newtype.Server.Models;
using Newtype.Shared.Models;
using System.Runtime.InteropServices;

namespace Newtype.Server.Services;

public class ExecutionService
{
    private readonly IHubContext<CompilerHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public ExecutionService(IHubContext<CompilerHub> hubContext, IServiceScopeFactory scopeFactory) 
    {
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
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

            compileProcess.OutputDataReceived += (s, e) => {
                if (e.Data != null) _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", e.Data).GetAwaiter().GetResult();
            };
            compileProcess.ErrorDataReceived += (s, e) => {
                if (e.Data != null) _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", $"Compiler Error: {e.Data}").GetAwaiter().GetResult();
            };

            compileProcess.Start();
            compileProcess.BeginOutputReadLine();
            compileProcess.BeginErrorReadLine();
            await compileProcess.WaitForExitAsync();
            
            compileSuccess = (compileProcess.ExitCode == 0);

            if (!compileSuccess)
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

            runProcess.OutputDataReceived += (s, e) => {
                if (e.Data != null) _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", e.Data).GetAwaiter().GetResult();
            };
            runProcess.ErrorDataReceived += (s, e) => {
                if (e.Data != null) _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", $"Error: {e.Data}").GetAwaiter().GetResult();
            };

            runProcess.Start();
            runProcess.BeginOutputReadLine();
            runProcess.BeginErrorReadLine();
            await runProcess.WaitForExitAsync();
            
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveTerminalOutput", $"--- Process Exited with code {runProcess.ExitCode} ---");
        }
        finally
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var log = new SubmissionLog
                {
                    ConnectionId = connectionId,
                    FileName = request.FileName,
                    SourceCode = request.SourceCode,
                    IsSuccess = compileSuccess
                };

                dbContext.SubmissionLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
