using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class SchoolUsersController : ControllerBase
{
    private readonly ISchoolUsers _repository;
    private readonly ISchools _schools;
    private readonly IUsers _users;

    public SchoolUsersController(ISchoolUsers repository, ISchools schools, IUsers users)
    {
        _repository = repository;
        _schools = schools;
        _users = users;
    }

    private int TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim)) return 0;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>Users linked to a school + creator if not in list. Owner flagged for default selection.</summary>
    [HttpGet("for-school/{schoolId:int}")]
    public async Task<ActionResult<List<object>>> GetUsersForSchool(int schoolId)
    {
        var school = await _schools.GetByIdAsync(schoolId);
        if (school == null) return NotFound(new { message = "School not found" });

        var rows = await _repository.GetBySchoolIdWithUsersAsync(schoolId);
        var list = new List<(int usersId, string name, string email, bool isOwner)>();

        // Exactly ONE owner must be returned.
        // Owner logic:
        // - Prefer the SchoolUsers row whose CreatorId == School.CreatorId (superadmin who created the school).
        // - If none exists, fallback to the most recently created row with CreatorId == 0.
        // - Otherwise, no owner is flagged.
        var ownerCandidate = rows
            .Where(su => su.User != null && su.CreatorId == school.CreatorId)
            .OrderByDescending(su => su.CreatedAt)
            .FirstOrDefault();

        if (ownerCandidate == null)
        {
            ownerCandidate = rows
                .Where(su => su.User != null && su.CreatorId == 0)
                .OrderByDescending(su => su.CreatedAt)
                .FirstOrDefault();
        }

        foreach (var su in rows)
        {
            if (su.User == null) continue;
            var isOwner = ownerCandidate != null && su.UsersId == ownerCandidate.UsersId;
            list.Add((su.UsersId, su.User.Name, su.User.Email, isOwner));
        }

        list.Sort((a, b) =>
        {
            var o = b.isOwner.CompareTo(a.isOwner);
            return o != 0
                ? o
                : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        });

        return Ok(list.Select(x => new
        {
            usersId = x.usersId,
            name = x.name,
            email = x.email,
            isOwner = x.isOwner
        }).ToList());
    }

    [HttpGet]
    public async Task<ActionResult<List<SchoolUser>>> GetAll()
    {
        var schoolUsers = await _repository.GetAllAsync();
        return Ok(schoolUsers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SchoolUser>> GetById(int id)
    {
        var schoolUser = await _repository.GetByIdAsync(id);
        if (schoolUser == null) return NotFound();
        return Ok(schoolUser);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<SchoolUser>> Create(SchoolUser schoolUser)
    {
        if (schoolUser.CreatorId == 0)
        {
            schoolUser.CreatorId = TryGetCurrentUserId();
        }
        var created = await _repository.AddAsync(schoolUser);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SchoolUser>> Update(int id, SchoolUser schoolUser)
    {
        if (id != schoolUser.Id) return BadRequest();

        // FE does not send CreatorId; preserve it so it never becomes 0.
        if (schoolUser.CreatorId == 0)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing != null)
            {
                schoolUser.CreatorId = existing.CreatorId;
            }
        }

        var updated = await _repository.UpdateAsync(schoolUser);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var schoolUser = await _repository.GetByIdAsync(id);
        if (schoolUser == null) return NotFound();
        await _repository.DeleteAsync(schoolUser);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<SchoolUser>> Activate(int id)
    {
        var schoolUser = await _repository.ActivateAsync(id);
        return Ok(schoolUser);
    }
}

