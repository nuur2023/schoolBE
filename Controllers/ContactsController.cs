using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class ContactsController : ControllerBase
{
    private readonly IContacts _repository;

    public ContactsController(IContacts repository)
    {
        _repository = repository;
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

    [HttpGet]
    public async Task<ActionResult<List<Contact>>> GetAll()
    {
        var contacts = await _repository.GetAllAsync();
        return Ok(contacts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetById(int id)
    {
        var contact = await _repository.GetByIdAsync(id);
        if (contact == null) return NotFound();
        return Ok(contact);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Contact>> Create([FromBody] Contact contact)
    {
        if (contact == null) return BadRequest("Contact data is required");
        contact.CreatorId = GetCurrentUserId();
        contact.CreatedAt = DateTime.UtcNow;
        contact.UpdatedAt = DateTime.UtcNow;
        var created = await _repository.AddAsync(contact);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<Contact>>> CreateBulk([FromBody] List<Contact> contacts)
    {
        if (contacts == null || contacts.Count == 0) return BadRequest("Contacts data is required");
        
        var createdContacts = new List<Contact>();
        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;
        
        // Get all existing contacts for duplicate checking
        var allExistingContacts = await _repository.GetAllAsync();
        
        foreach (var contact in contacts)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(contact.Phone))
            {
                continue; // Skip empty phone numbers
            }
            
            if (contact.SchoolId <= 0)
            {
                continue; // Skip invalid school IDs
            }
            
            // Check if this phone number already exists for the same person/student/user and school
            var duplicate = allExistingContacts.FirstOrDefault(c => 
                c.SchoolId == contact.SchoolId && 
                c.Phone.Trim() == contact.Phone.Trim() &&
                ((contact.PersonId.HasValue && c.PersonId.HasValue && c.PersonId == contact.PersonId) ||
                 (contact.StudentId.HasValue && c.StudentId.HasValue && c.StudentId == contact.StudentId) ||
                 (contact.UsersId.HasValue && c.UsersId.HasValue && c.UsersId == contact.UsersId)));
            
            if (duplicate == null)
            {
                // No duplicate found - create new contact
                contact.CreatorId = currentUserId;
                contact.CreatedAt = now;
                contact.UpdatedAt = now;
                contact.Phone = contact.Phone.Trim(); // Normalize phone number
                
                try
                {
                    var created = await _repository.AddAsync(contact);
                    createdContacts.Add(created);
                    // Add to existing contacts list to check against in the same batch
                    allExistingContacts.Add(created);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other contacts
                    // You might want to add proper logging here
                    Console.WriteLine($"Error creating contact for phone {contact.Phone}: {ex.Message}");
                }
            }
        }
        
        return Ok(createdContacts);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Contact>> Update(int id, [FromBody] Contact contact)
    {
        if (contact == null) return BadRequest("Contact data is required");
        if (id != contact.Id) return BadRequest();
        contact.UpdatedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateAsync(contact);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var contact = await _repository.GetByIdAsync(id);
        if (contact == null) return NotFound();
        await _repository.DeleteAsync(contact);
        return NoContent();
    }

    [HttpGet("person/{personId}")]
    public async Task<ActionResult<List<Contact>>> GetByPersonId(int personId)
    {
        var contacts = await _repository.GetAllAsync();
        var personContacts = contacts.Where(c => c.PersonId == personId).ToList();
        return Ok(personContacts);
    }

    [HttpDelete("person/{personId}")]
    public async Task<ActionResult> DeleteByPersonId(int personId)
    {
        var contacts = await _repository.GetAllAsync();
        var personContacts = contacts.Where(c => c.PersonId == personId).ToList();
        foreach (var contact in personContacts)
        {
            await _repository.DeleteAsync(contact);
        }
        return NoContent();
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<Contact>>> GetByStudentId(int studentId)
    {
        var contacts = await _repository.GetAllAsync();
        var studentContacts = contacts.Where(c => c.StudentId == studentId).ToList();
        return Ok(studentContacts);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Contact>>> GetByUserId(int userId)
    {
        var contacts = await _repository.GetAllAsync();
        var userContacts = contacts
            .Where(c => c.UsersId == userId && c.PersonId == null && c.StudentId == null)
            .ToList();
        return Ok(userContacts);
    }

    [HttpDelete("student/{studentId}")]
    public async Task<ActionResult> DeleteByStudentId(int studentId)
    {
        var contacts = await _repository.GetAllAsync();
        var studentContacts = contacts.Where(c => c.StudentId == studentId).ToList();
        foreach (var contact in studentContacts)
        {
            await _repository.DeleteAsync(contact);
        }
        return NoContent();
    }
}