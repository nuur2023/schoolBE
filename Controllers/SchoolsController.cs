using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class SchoolsController : ControllerBase
{
    private readonly ISchools _repository;
    private readonly SchoolContext _context;
    private readonly IWebHostEnvironment _environment;

    public SchoolsController(ISchools repository, SchoolContext context, IWebHostEnvironment environment)
    {
        _repository = repository;
        _context = context;
        _environment = environment;
    }

    public class SchoolUpsertRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public int? Days { get; set; }
        public int CreatorId { get; set; }
        public bool IsActive { get; set; }
        public List<string>? PhoneNumbers { get; set; }
    }

    private int TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim)) return 0;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>Turns a data-URL logo into a stored file path under wwwroot Images/logo; returns null on failure.</summary>
    private async Task<string?> TryPersistLogoFromDataUrlAsync(string? logo)
    {
        if (string.IsNullOrWhiteSpace(logo) || !logo.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            return logo;

        try
        {
            var commaIndex = logo.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (commaIndex <= -1)
                return null;

            var header = logo.Substring(0, commaIndex);
            var base64Data = logo[(commaIndex + "base64,".Length)..];

            var extension = ".png";
            if (header.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                header.Contains("image/jpg", StringComparison.OrdinalIgnoreCase))
            {
                extension = ".jpg";
            }
            else if (header.Contains("image/png", StringComparison.OrdinalIgnoreCase))
            {
                extension = ".png";
            }
            else
            {
                // Only jpg/png supported (matches frontend)
                return null;
            }

            var bytes = Convert.FromBase64String(base64Data);

            var imagesRoot = Path.Combine(_environment.ContentRootPath, "Images", "logo");
            if (!Directory.Exists(imagesRoot))
                Directory.CreateDirectory(imagesRoot);

            var fileName = $"school-logo-{Guid.NewGuid()}{extension}";
            var savePath = Path.Combine(imagesRoot, fileName);
            await System.IO.File.WriteAllBytesAsync(savePath, bytes);

            // Stored relative to Images/ → public URL is /Images/logo/{fileName}
            return Path.Combine("logo", fileName).Replace("\\", "/");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Normalize inbound logo values (strip /Images/ prefix; keep relative logo/... path).</summary>
    private static string? NormalizeStoredLogoPath(string? logo)
    {
        if (string.IsNullOrWhiteSpace(logo)) return logo;
        var v = logo.Trim();
        if (v.StartsWith("data:image", StringComparison.OrdinalIgnoreCase)) return v;
        if (v.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(v, UriKind.Absolute, out var uri))
                v = uri.AbsolutePath;
        }
        v = v.TrimStart('/');
        if (v.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
            v = v["Images/".Length..];
        return v;
    }

    [HttpGet]
    public async Task<ActionResult<List<School>>> GetAll()
    {
        var schools = await _repository.GetAllAsync();
        return Ok(schools);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var school = await _repository.GetByIdAsync(id);
        if (school == null) return NotFound();
        var phoneNumbers = await _context.Contacts
            .Where(c => c.SchoolId == id && c.PersonId == null && c.StudentId == null && c.UsersId == null)
            .OrderBy(c => c.Id)
            .Select(c => c.Phone)
            .ToListAsync();
        return Ok(new
        {
            school.Id,
            school.Name,
            school.Address,
            school.Logo,
            school.Days,
            school.CreatorId,
            school.IsActive,
            school.CreatedAt,
            school.UpdatedAt,
            phoneNumbers
        });
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        var ids = result.data.Select(s => s.Id).ToList();
        Dictionary<int, List<string>> phonesBySchool = new();
        if (ids.Count > 0)
        {
            var rows = await _context.Contacts
                .AsNoTracking()
                .Where(c => ids.Contains(c.SchoolId) && c.PersonId == null && c.StudentId == null && c.UsersId == null)
                .OrderBy(c => c.Id)
                .Select(c => new { c.SchoolId, c.Phone })
                .ToListAsync();

            phonesBySchool = rows
                .GroupBy(x => x.SchoolId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Phone).ToList());
        }

        var data = result.data.Select(s => new
        {
            s.Id,
            s.Name,
            s.Address,
            s.Logo,
            s.Days,
            s.CreatorId,
            s.IsActive,
            s.CreatedAt,
            s.UpdatedAt,
            phoneNumbers = phonesBySchool.GetValueOrDefault(s.Id) ?? []
        }).ToList();

        return Ok(new { data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<School>> Create([FromBody] SchoolUpsertRequest request)
    {
        var creatorId = request.CreatorId != 0 ? request.CreatorId : TryGetCurrentUserId();

        var logoOut = request.Logo;
        if (!string.IsNullOrWhiteSpace(logoOut) && logoOut.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            logoOut = await TryPersistLogoFromDataUrlAsync(logoOut);
        else
            logoOut = NormalizeStoredLogoPath(logoOut);

        var school = new School
        {
            Name = request.Name,
            Address = request.Address,
            Logo = logoOut,
            Days = request.Days,
            CreatorId = creatorId,
            IsActive = request.IsActive
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var created = await _repository.AddAsync(school);

            // Seed default stages for this newly created school.
            // Primary + Secondary are required by student enrollment flow.
            var primaryExists = await _context.Set<Stage>()
                .AnyAsync(s => s.SchoolId == created.Id && s.Name == "Primary");
            if (!primaryExists)
            {
                _context.Set<Stage>().Add(new Stage
                {
                    Name = "Primary",
                    SchoolId = created.Id,
                    CreatorId = created.CreatorId,
                    IsActive = true
                });
            }

            var secondaryExists = await _context.Set<Stage>()
                .AnyAsync(s => s.SchoolId == created.Id && s.Name == "Secondary");
            if (!secondaryExists)
            {
                _context.Set<Stage>().Add(new Stage
                {
                    Name = "Secondary",
                    SchoolId = created.Id,
                    CreatorId = created.CreatorId,
                    IsActive = true
                });
            }

            // Persist stage seeds before committing.
            await _context.SaveChangesAsync();

            var normalizedPhones = NormalizePhones(request.PhoneNumbers);
            if (normalizedPhones.Count > 0)
            {
                var contacts = normalizedPhones.Select(phone => new Contact
                {
                    SchoolId = created.Id,
                    Phone = phone,
                    CreatorId = created.CreatorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                await _context.Contacts.AddRangeAsync(contacts);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<School>> Update(int id, [FromBody] SchoolUpsertRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { message = "School not found" });

        var creatorId = 0;
        if (existing.CreatorId != 0)
            creatorId = existing.CreatorId;
        else if (request.CreatorId != 0)
            creatorId = request.CreatorId;
        else
            creatorId = TryGetCurrentUserId();

        var bodyId = request.Id ?? id;
        if (id != bodyId)
            return BadRequest(new { message = "Route id and body id must match." });

        // null/omit = keep existing logo; "" = clear; path or data URL = replace
        string? logoOut;
        if (request.Logo == null)
            logoOut = existing.Logo;
        else if (string.IsNullOrWhiteSpace(request.Logo))
            logoOut = null;
        else if (request.Logo.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            var persisted = await TryPersistLogoFromDataUrlAsync(request.Logo);
            logoOut = persisted ?? existing.Logo;
        }
        else
            logoOut = NormalizeStoredLogoPath(request.Logo);

        var school = new School
        {
            Id = bodyId,
            Name = request.Name,
            Address = request.Address,
            Logo = logoOut,
            Days = request.Days,
            CreatorId = creatorId,
            IsActive = request.IsActive
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var updated = await _repository.UpdateAsync(school);

            if (request.PhoneNumbers != null)
            {
                var normalizedPhones = NormalizePhones(request.PhoneNumbers);
                var existingSchoolContacts = await _context.Contacts
                    .Where(c => c.SchoolId == id && c.PersonId == null && c.StudentId == null && c.UsersId == null)
                    .ToListAsync();

                if (existingSchoolContacts.Count > 0)
                {
                    _context.Contacts.RemoveRange(existingSchoolContacts);
                }

                if (normalizedPhones.Count > 0)
                {
                    var contacts = normalizedPhones.Select(phone => new Contact
                    {
                        SchoolId = id,
                        Phone = phone,
                        CreatorId = updated.CreatorId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await _context.Contacts.AddRangeAsync(contacts);
                }

                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return Ok(updated);
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
        var school = await _repository.GetByIdAsync(id);
        if (school == null) return NotFound();
        await _repository.DeleteAsync(school);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<School>> Activate(int id)
    {
        var school = await _repository.ActivateAsync(id);
        return Ok(school);
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
}

