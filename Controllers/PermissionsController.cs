using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.ViewModels;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissions _repository;

    public PermissionsController(IPermissions repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Permission>>> GetAll()
    {
        var permissions = await _repository.GetAllAsync();
        return Ok(permissions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Permission>> GetById(int id)
    {
        var permission = await _repository.GetByIdAsync(id);
        if (permission == null) return NotFound();
        return Ok(permission);
    }

    [HttpGet("user/{userId}/school/{schoolId}")]
    public async Task<ActionResult<List<PermissionVM>>> GetUserPermissions(int userId, int schoolId)
    {
        var permissions = await _repository.GetUserPermissionsAsync(userId, schoolId);
        return Ok(permissions);
    }

    [HttpPost("user/{userId}/school/{schoolId}")]
    public async Task<ActionResult> SaveUserPermissions(int userId, int schoolId, [FromBody] List<PermissionVM> permissions)
    {
        await _repository.SaveUserPermissionsAsync(userId, schoolId, permissions);
        return Ok();
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Permission>> Create(Permission permission)
    {
        var created = await _repository.AddAsync(permission);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Permission>> Update(int id, Permission permission)
    {
        if (id != permission.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(permission);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var permission = await _repository.GetByIdAsync(id);
        if (permission == null) return NotFound();
        await _repository.DeleteAsync(permission);
        return NoContent();
    }
}

