using CodeExecution.UseCases.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeExecution.Controllers;

[ApiController]
[Route("api/v1/code-execution")]
public sealed class TestRunController(ISender sender) : ControllerBase
{
    [HttpPost("test-run")]
    public async Task<IActionResult> RunOnTests([FromBody] TestRunRequest request, CancellationToken cancellationToken)
    {
        if (request.TestCases.Count == 0)
        {
            return BadRequest(new { code = "EMPTY_TEST_CASES", description = "Нужно передать хотя бы один тест-кейс" });
        }

        var command = new RunCodeOnTestsCommand(
            request.Code,
            request.Language,
            request.TestCases.Select(tc => new RunCodeOnTestsCaseDto(tc.Input, tc.ExpectedOutput, tc.Order)).ToList(),
            request.MaxTimeSeconds,
            request.MaxMemoryMb);

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record TestRunRequest(
    string Code,
    string Language,
    IReadOnlyList<TestRunCaseRequest> TestCases,
    int? MaxTimeSeconds = null,
    int? MaxMemoryMb = null);

public sealed record TestRunCaseRequest(
    int Order,
    string Input,
    string ExpectedOutput);
