namespace Newtype.Shared.Models;

public record CompileRequest(
        string SourceCode,
        string Language,
        string FileName
        );
