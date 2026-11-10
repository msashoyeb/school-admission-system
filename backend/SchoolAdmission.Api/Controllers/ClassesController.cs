using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public sealed class ClassesController(IAdmissionService admissionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var created = await admissionService.CreateClassAsync(request, cancellationToken);
        return Created($"/classes/{created.Id}", created);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var classes = await admissionService.GetClassesAsync(cancellationToken);
        return Ok(classes);
    }

    [HttpGet("{classId}")]
    public async Task<IActionResult> GetById(string classId, CancellationToken cancellationToken)
    {
        var item = await admissionService.GetClassByIdAsync(classId, cancellationToken);
        return Ok(item);
    }

    [HttpPost("{classId}/holds")]
    public async Task<IActionResult> CreateHold(
        string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        var hold = await admissionService.CreateHoldAsync(classId, studentId, cancellationToken);
        return Created($"/holds/{hold.HoldId}", hold);
    }
}
