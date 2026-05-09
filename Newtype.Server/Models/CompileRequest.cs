public record CompileRequest
{
    public string Content { get; set; }
    public string FileName { get; set; }
    public string Language { get; set; }
}
