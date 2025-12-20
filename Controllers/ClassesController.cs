using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly IClasses _repository;

    public ClassesController(IClasses repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Class>>> GetAll()
    {
        var classes = await _repository.GetAllAsync();
        return Ok(classes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Class>> GetById(int id)
    {
        var classEntity = await _repository.GetByIdAsync(id);
        if (classEntity == null) return NotFound();
        return Ok(classEntity);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Class>> Create(Class classEntity)
    {
        var created = await _repository.AddAsync(classEntity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);                                                                              
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<Class>>> CreateBulk(List<Class> classes)
    {
        var createdClasses = new List<Class>();
        foreach (var classEntity in classes)
        {
            var created = await _repository.AddAsync(classEntity);
            createdClasses.Add(created);
        }
        return Ok(createdClasses);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Class>> Update(int id, Class classEntity)
    {
        if (id != classEntity.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(classEntity);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var classEntity = await _repository.GetByIdAsync(id);
        if (classEntity == null) return NotFound();
        await _repository.DeleteAsync(classEntity);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<Class>> Activate(int id)
    {
        var classEntity = await _repository.ActivateAsync(id);
        return Ok(classEntity);
    }
}

