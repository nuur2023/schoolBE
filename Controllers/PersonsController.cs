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
public class PersonsController : ControllerBase
{
    private readonly IPersons _repository;

    public PersonsController(IPersons repository)
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
    public async Task<ActionResult<List<Person>>> GetAll()
    {
        var persons = await _repository.GetAllAsync();
        return Ok(persons);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Person>> GetById(int id)
    {
        var person = await _repository.GetByIdAsync(id);
        if (person == null) return NotFound();
        return Ok(person);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Person>> Create([FromBody] Person person)
    {
        if (person == null) return BadRequest("Person data is required");
        person.CreatorId = GetCurrentUserId();
        person.CreatedAt = DateTime.UtcNow;
        person.UpdatedAt = DateTime.UtcNow;
        var created = await _repository.AddAsync(person);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Person>> Update(int id, [FromBody] Person person)
    {
        if (person == null) return BadRequest("Person data is required");
        if (id != person.Id) return BadRequest("ID mismatch");
        
        var existingPerson = await _repository.GetByIdAsync(id);
        if (existingPerson == null) return NotFound();
        
        person.UpdatedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateAsync(person);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var person = await _repository.GetByIdAsync(id);
        if (person == null) return NotFound();
        await _repository.DeleteAsync(person);
        return NoContent();
    }
}











