using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
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
public class AutoChargeSettingsController : ControllerBase
{
    private readonly SchoolContext _context;
    private readonly IStudentEnrollments _studentRepository;
    private readonly IFeeCategories _feeCategoriesRepository;
    private readonly IStudentChargePostingService _chargePostingService;

    public AutoChargeSettingsController(
        SchoolContext context,
        IStudentEnrollments studentRepository,
        IFeeCategories feeCategoriesRepository,
        IStudentChargePostingService chargePostingService)
    {
        _context = context;
        _studentRepository = studentRepository;
        _feeCategoriesRepository = feeCategoriesRepository;
        _chargePostingService = chargePostingService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            throw new UnauthorizedAccessException("User ID not found in token");
        return userId;
    }

    private async Task<int?> GetCurrentUserSchoolIdAsync()
    {
        var userId = GetCurrentUserId();
        var schoolUser = await _context.Set<SchoolUser>()
            .Where(su => su.UsersId == userId && su.IsActive)
            .FirstOrDefaultAsync();
        return schoolUser?.SchoolId;
    }

    private async Task<ActionResult?> EnsureSchoolAccessAsync(int schoolId)
    {
        var tokenSchoolId = await GetCurrentUserSchoolIdAsync();
        if (tokenSchoolId == null)
            return null;
        if (tokenSchoolId.Value != schoolId)
            return Forbid();
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<List<AutoChargeSetting>>> GetAll([FromQuery] int? schoolId = null)
    {
        try
        {
            var tokenSchoolId = await GetCurrentUserSchoolIdAsync();
            var query = _context.Set<AutoChargeSetting>().AsQueryable();

            if (tokenSchoolId != null)
            {
                query = query.Where(s => s.SchoolId == tokenSchoolId.Value);
            }
            else if (schoolId.HasValue)
            {
                query = query.Where(s => s.SchoolId == schoolId.Value);
            }

            var list = await query.OrderByDescending(s => s.Id).ToListAsync();
            return Ok(list);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to load auto-charge settings", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AutoChargeSetting>> GetById(int id)
    {
        var entity = await _context.Set<AutoChargeSetting>().FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return NotFound();

        var denied = await EnsureSchoolAccessAsync(entity.SchoolId);
        if (denied != null) return denied;

        return Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<AutoChargeSetting>> Create([FromBody] AutoChargeSettingWriteDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { message = "Body required" });

            var denied = await EnsureSchoolAccessAsync(dto.SchoolId);
            if (denied != null) return denied;

            var category = await _feeCategoriesRepository.GetByIdAsync(dto.FeeCategoryId);
            if (category == null)
                return BadRequest(new { message = "Fee category not found" });

            var stage = await _context.Set<Stage>().FirstOrDefaultAsync(st => st.Id == dto.StageId);
            if (stage == null || stage.SchoolId != dto.SchoolId)
                return BadRequest(new { message = "Stage not found for this school" });

            var entity = new AutoChargeSetting
            {
                IsAllowedAutoCharge = dto.IsAllowedAutoCharge,
                MonthsMode = string.IsNullOrWhiteSpace(dto.MonthsMode) ? "all" : dto.MonthsMode.Trim(),
                SpecificMonths = dto.SpecificMonths,
                DayOfMonth = Math.Clamp(dto.DayOfMonth, 1, 31),
                ChargeTimeLocal = string.IsNullOrWhiteSpace(dto.ChargeTimeLocal) ? null : dto.ChargeTimeLocal.Trim(),
                Amount = dto.Amount,
                StageId = dto.StageId,
                FeeCategoryId = dto.FeeCategoryId,
                IsActive = dto.IsActive,
                SchoolId = dto.SchoolId,
                CreatorId = GetCurrentUserId(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<AutoChargeSetting>().Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to create auto-charge setting", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AutoChargeSetting>> Update(int id, [FromBody] AutoChargeSettingWriteDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { message = "Body required" });

            var entity = await _context.Set<AutoChargeSetting>().FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return NotFound();

            var denied = await EnsureSchoolAccessAsync(entity.SchoolId);
            if (denied != null) return denied;

            if (dto.SchoolId != entity.SchoolId)
                return BadRequest(new { message = "Cannot change school for an existing rule" });

            var category = await _feeCategoriesRepository.GetByIdAsync(dto.FeeCategoryId);
            if (category == null)
                return BadRequest(new { message = "Fee category not found" });

            var stage = await _context.Set<Stage>().FirstOrDefaultAsync(st => st.Id == dto.StageId);
            if (stage == null || stage.SchoolId != dto.SchoolId)
                return BadRequest(new { message = "Stage not found for this school" });

            entity.IsAllowedAutoCharge = dto.IsAllowedAutoCharge;
            entity.MonthsMode = string.IsNullOrWhiteSpace(dto.MonthsMode) ? "all" : dto.MonthsMode.Trim();
            entity.SpecificMonths = dto.SpecificMonths;
            entity.DayOfMonth = Math.Clamp(dto.DayOfMonth, 1, 31);
            entity.ChargeTimeLocal = string.IsNullOrWhiteSpace(dto.ChargeTimeLocal) ? null : dto.ChargeTimeLocal.Trim();
            entity.Amount = dto.Amount;
            entity.StageId = dto.StageId;
            entity.FeeCategoryId = dto.FeeCategoryId;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(entity);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update auto-charge setting", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var entity = await _context.Set<AutoChargeSetting>().FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return NotFound();

        var denied = await EnsureSchoolAccessAsync(entity.SchoolId);
        if (denied != null) return denied;

        _context.Set<AutoChargeSetting>().Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Run this rule immediately (still respects student filters; uses same posting as manual charges).</summary>
    [HttpPost("{id:int}/execute-now")]
    public async Task<ActionResult> ExecuteNow(int id)
    {
        try
        {
            var setting = await _context.Set<AutoChargeSetting>().FirstOrDefaultAsync(s => s.Id == id);
            if (setting == null) return NotFound();

            var denied = await EnsureSchoolAccessAsync(setting.SchoolId);
            if (denied != null) return denied;

            if (!setting.IsActive || !setting.IsAllowedAutoCharge)
                return BadRequest(new { message = "Rule must be active and allow auto-charge to execute." });

            var category = await _feeCategoriesRepository.GetByIdAsync(setting.FeeCategoryId);
            if (category == null)
                return BadRequest(new { message = "Fee category not found" });

            var students = await _studentRepository.GetByStageLevelClassAsync(setting.StageId, null, null, null);
            students = students.Where(s => s.SchoolId == setting.SchoolId).ToList();

            var request = new ChargeStudentsRequest
            {
                ChargeType = "stage",
                SchoolId = setting.SchoolId,
                StageId = setting.StageId,
                FeeCategories = new List<FeeCategoryCharge>
                {
                    new() { FeeCategoryId = setting.FeeCategoryId, Amount = setting.Amount }
                },
                Description = $"Manual auto-charge run (rule #{setting.Id})"
            };

            var outcome = await _chargePostingService.PostChargesAsync(request, students, GetCurrentUserId());
            if (outcome.IsFailure)
                return StatusCode(outcome.StatusCode ?? 500, outcome.ErrorBody);

            return Ok(new
            {
                message = $"Charged {outcome.ChargedCount} student(s)",
                outcome.ChargedCount,
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
            return StatusCode(500, new { message = "Execute failed", error = ex.Message });
        }
    }
}
