using Microsoft.AspNetCore.Mvc;
using Newtype.Shared.Models;
namespace Newtype.Server.Services;

[ApiController]
[Route("api/[controller]")]
public class CompileController : ControllerBase
{
    private readonly ExecutionService _execution;
    public CompileController(ExecutionService execution) => _execution = execution;

    [HttpPost("run")]
    public IActionResult Run([FromBody] CompileRequest request, [FromQuery] string connectionId)
    {
        _ = _execution.RunCompilerAsync(connectionId, request);
        return Accepted();
    }
}
