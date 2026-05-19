namespace Newtype.Server.Models;

public class SubmissionLog
{
    public int Id { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public DateTime CompilationData { get; set; }
}
