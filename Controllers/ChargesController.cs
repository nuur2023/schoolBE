using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Models.DTOs;
using schoolBE.Services;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class ChargesController : ControllerBase
{
    private readonly IStudentEnrollments _studentRepository;
    private readonly IFeeCategories _feeCategoriesRepository;
    private readonly IStudentChargePostingService _chargePostingService;

    public ChargesController(
        IStudentEnrollments studentRepository,
        IFeeCategories feeCategoriesRepository,
        IStudentChargePostingService chargePostingService)
    {
        _studentRepository = studentRepository;
        _feeCategoriesRepository = feeCategoriesRepository;
        _chargePostingService = chargePostingService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpPost("charge-students")]
    [Authorize]
    public async Task<ActionResult> ChargeStudents([FromBody] ChargeStudentsRequest request)
    {
        try
        {
            if (request == null || request.FeeCategories == null || request.FeeCategories.Count == 0)
            {
                return BadRequest(new { message = "Fee categories and amounts are required" });
            }

            foreach (var feeCategory in request.FeeCategories)
            {
                var category = await _feeCategoriesRepository.GetByIdAsync(feeCategory.FeeCategoryId);
                if (category == null)
                {
                    return BadRequest(new { message = $"Fee category with ID {feeCategory.FeeCategoryId} not found" });
                }
            }

            List<StudentEnrollment> students = new();

            switch (request.ChargeType.ToLower())
            {
                case "stage":
                    if (!request.StageId.HasValue)
                        return BadRequest(new { message = "StageId is required for stage charge type" });
                    students = await _studentRepository.GetByStageLevelClassAsync(request.StageId, null, null, null);
                    break;

                case "level":
                    if (!request.LevelId.HasValue)
                        return BadRequest(new { message = "LevelId is required for level charge type" });
                    students = await _studentRepository.GetByStageLevelClassAsync(null, request.LevelId, null, null);
                    break;

                case "class":
                    if (!request.ClassId.HasValue)
                        return BadRequest(new { message = "ClassId is required for class charge type" });
                    students = await _studentRepository.GetByStageLevelClassAsync(null, null, request.ClassId, null);
                    break;

                case "individual":
                    if (!request.StudentId.HasValue)
                        return BadRequest(new { message = "StudentId is required for individual charge type" });
                    var student = await _studentRepository.GetByIdAsync(request.StudentId.Value);
                    if (student == null)
                        return NotFound(new { message = "Student not found" });
                    if (student.SchoolId != request.SchoolId)
                        return BadRequest(new { message = "Student does not belong to the selected school." });
                    students = new List<StudentEnrollment> { student };
                    break;

                default:
                    return BadRequest(new { message = "Invalid charge type. Must be 'stage', 'level', 'class', or 'individual'" });
            }

            students = students.Where(s => s.SchoolId == request.SchoolId).ToList();

            if (students.Count == 0)
            {
                return BadRequest(new { message = "No students found for the specified criteria" });
            }

            var creatorId = GetCurrentUserId();
            var outcome = await _chargePostingService.PostChargesAsync(request, students, creatorId);

            if (outcome.IsFailure)
                return StatusCode(outcome.StatusCode ?? 500, outcome.ErrorBody);

            return Ok(new
            {
                message = $"Successfully charged {outcome.ChargedCount} student(s)",
                chargedCount = outcome.ChargedCount,
                totalStudents = outcome.TotalStudents,
                errors = outcome.RowErrors
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while charging students", error = ex.Message });
        }
    }
}
