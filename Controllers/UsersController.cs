using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Utils;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsers _repository;

    public UsersController(IUsers repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        var users = await _repository.GetAllAsync();
        // Don't return hashed passwords
        foreach (var user in users)
        {
            user.HashedPassword = string.Empty;
        }
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NotFound();
        // Don't return the hashed password
        user.HashedPassword = string.Empty;
        return Ok(user);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        // Don't return hashed passwords
        foreach (var user in result.data)
        {
            user.HashedPassword = string.Empty;
        }
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<User>> Create(User user)
    {
        // Hash the password before saving
        if (!string.IsNullOrEmpty(user.HashedPassword))
        {
            user.HashedPassword = Helper.HashPassword(user.HashedPassword);
        }
        var created = await _repository.AddAsync(user);
        // Don't return the hashed password
        created.HashedPassword = string.Empty;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<User>> Update(int id, User user)
    {
        if (id != user.Id) return BadRequest();
        
        // Get existing user to preserve password if not provided
        var existingUser = await _repository.GetByIdAsync(id);
        if (existingUser == null) return NotFound();
        
        // If password is provided and not empty, hash it. Otherwise, keep existing password
        // Note: Frontend sends plain password in HashedPassword field, or empty string if not changing
        if (!string.IsNullOrEmpty(user.HashedPassword))
        {
            // Hash the new password
            user.HashedPassword = Helper.HashPassword(user.HashedPassword);
        }
        else
        {
            // Keep existing password if not provided
            user.HashedPassword = existingUser.HashedPassword;
        }
        
        var updated = await _repository.UpdateAsync(user);
        // Don't return the hashed password
        updated.HashedPassword = string.Empty;
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NotFound();
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

