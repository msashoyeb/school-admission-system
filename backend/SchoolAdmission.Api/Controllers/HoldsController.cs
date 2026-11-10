using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Interfaces;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public sealed class HoldsController(IAdmissionService admissionService) : ControllerBase
{
    [HttpGet("{holdId}")]
    public async Task<IActionResult> GetById(string holdId, CancellationToken cancellationToken)
    {
        var hold = await admissionService.GetHoldByIdAsync(holdId, cancellationToken);
        return Ok(hold);
    }

    [HttpPost("{holdId}/confirm")]
    public async Task<IActionResult> Confirm(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        var confirmed = await admissionService.ConfirmHoldAsync(holdId, studentId, cancellationToken);
        return Ok(confirmed);
    }

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> Cancel(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        await admissionService.CancelHoldAsync(holdId, studentId, cancellationToken);
        return NoContent();
    }
}
