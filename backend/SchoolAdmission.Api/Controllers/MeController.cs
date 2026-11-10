using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Interfaces;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public sealed class MeController(IAdmissionService admissionService) : ControllerBase
{
    [HttpGet("holds")]
    public async Task<IActionResult> GetHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        var holds = await admissionService.GetStudentHoldsAsync(studentId, cancellationToken);
        return Ok(holds);
    }
}
