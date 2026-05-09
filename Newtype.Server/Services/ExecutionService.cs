public interface IExecutionService
{
    Task ExecuteAsync(string connectionId, CompileRequest request);
}

public class LocalExecutionService : IExecutionService
{
    private readonly IHubContext<CompilerHub> _hubContext;

    public LocalExecutionService(IHubContext<CompilerHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task ExecuteAsync(string connectionId, CompileRequest request)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), request.FileName);
        await File.WriteAllTextAsync(tempPath, request.SourceCode);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "gcc", 
                Arguments = $"{tempPath} -o out.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += async (s, e) => 
        {
            if (e.Data != null)
                await _hubContext.Clients.Client(connectionId).
                    SendAsync("ReceiveOutput", e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync();
    }
}
