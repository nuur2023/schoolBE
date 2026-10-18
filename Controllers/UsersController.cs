using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Utils;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class UsersController : ControllerBase
{
    private readonly IUsers _repository;
    private readonly SchoolContext _context;

    public UsersController(IUsers repository, SchoolContext context)
    {
        _repository = repository;
        _context = context;
    }

    public class UserUpsertRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>Plain password (required on create; optional on update)</summary>
        public string? Password { get; set; }
        public bool IsActive { get; set; }
        public List<string>? PhoneNumbers { get; set; }
    }

    private int TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim)) return 0;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private static List<string> NormalizePhones(IEnumerable<string>? phones)
    {
        if (phones == null) return [];

        return phones
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<string>> GetUserPhoneNumbersAsync(int userId)
    {
        return await _context.Contacts
            .Where(c => c.UsersId == userId && c.PersonId == null && c.StudentId == null)
            .OrderBy(c => c.Id)
            .Select(c => c.Phone)
            .ToListAsync();
    }

    private object ToUserResponse(User user, List<string> phoneNumbers)
    {
        return new
        {
            user.Id,
            user.Name,
            user.Email,
            hashedPassword = string.Empty,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            phoneNumbers
        };
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        var users = await _repository.GetAllAsync();
        foreach (var user in users)
        {
            user.HashedPassword = string.Empty;
        }
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NotFound();
        user.HashedPassword = string.Empty;
        var phones = await GetUserPhoneNumbersAsync(id);
        return Ok(ToUserResponse(user, phones));
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        foreach (var user in result.data)
        {
            user.HashedPassword = string.Empty;
        }
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] UserUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Password is required" });

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            HashedPassword = Helper.HashPassword(request.Password),
            IsActive = request.IsActive
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var created = await _repository.AddAsync(user);
            var normalizedPhones = NormalizePhones(request.PhoneNumbers);
            if (normalizedPhones.Count > 0)
            {
                var creatorId = TryGetCurrentUserId();
                if (creatorId == 0) creatorId = created.Id;

                var contacts = normalizedPhones.Select(phone => new Contact
                {
                    SchoolId = 0,
                    Phone = phone,
                    CreatorId = creatorId,
                    UsersId = created.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await _context.Contacts.AddRangeAsync(contacts);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            created.HashedPassword = string.Empty;
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToUserResponse(created, normalizedPhones));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<object>> Update(int id, [FromBody] UserUpsertRequest request)
    {
        if (request.Id.HasValue && request.Id.Value != id) return BadRequest();

        var existingUser = await _repository.GetByIdAsync(id);
        if (existingUser == null) return NotFound();

        var user = new User
        {
            Id = id,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            IsActive = request.IsActive,
            CreatedAt = existingUser.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.HashedPassword = Helper.HashPassword(request.Password);
        }
        else
        {
            user.HashedPassword = existingUser.HashedPassword;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var updated = await _repository.UpdateAsync(user);

            if (request.PhoneNumbers != null)
            {
                var normalizedPhones = NormalizePhones(request.PhoneNumbers);
                var existingContacts = await _context.Contacts
                    .Where(c => c.UsersId == id && c.PersonId == null && c.StudentId == null)
                    .ToListAsync();

                if (existingContacts.Count > 0)
                    _context.Contacts.RemoveRange(existingContacts);

                if (normalizedPhones.Count > 0)
                {
                    var creatorId = TryGetCurrentUserId();
                    if (creatorId == 0) creatorId = id;

                    var contacts = normalizedPhones.Select(phone => new Contact
                    {
                        SchoolId = 0,
                        Phone = phone,
                        CreatorId = creatorId,
                        UsersId = id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await _context.Contacts.AddRangeAsync(contacts);
                }

                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            updated.HashedPassword = string.Empty;
            var phonesOut = request.PhoneNumbers != null
                ? NormalizePhones(request.PhoneNumbers)
                : await GetUserPhoneNumbersAsync(id);
            return Ok(ToUserResponse(updated, phonesOut));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NotFound();

        var userContacts = await _context.Contacts
            .Where(c => c.UsersId == id && c.PersonId == null && c.StudentId == null)
            .ToListAsync();
        if (userContacts.Count > 0)
        {
            _context.Contacts.RemoveRange(userContacts);
            await _context.SaveChangesAsync();
        }

        await _repository.DeleteAsync(user);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<User>> Activate(int id)
    {
        var user = await _repository.ActivateAsync(id);
        return Ok(user);
    }
}
