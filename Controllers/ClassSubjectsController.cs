using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassSubjectsController : ControllerBase
{
    private readonly IClassSubjects _repository;

    public ClassSubjectsController(IClassSubjects repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClassSubject>>> GetAll()
    {
        var classSubjects = await _repository.GetAllAsync();
        return Ok(classSubjects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClassSubject>> GetById(int id)
    {
        var classSubject = await _repository.GetByIdAsync(id);
        if (classSubject == null) return NotFound();
        return Ok(classSubject);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<ClassSubject>> Create(ClassSubject classSubject)                                                                             
    {
        var created = await _repository.AddAsync(classSubject);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);                                                                              
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<ClassSubject>>> CreateBulk(List<ClassSubject> classSubjects)
    {
        var createdClassSubjects = new List<ClassSubject>();
        foreach (var classSubject in classSubjects)
        {
            var created = await _repository.AddAsync(classSubject);
            createdClassSubjects.Add(created);
        }
        return Ok(createdClassSubjects);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClassSubject>> Update(int id, ClassSubject classSubject)
    {
        if (id != classSubject.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(classSubject);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var classSubject = await _repository.GetByIdAsync(id);
        if (classSubject == null) return NotFound();
        await _repository.DeleteAsync(classSubject);
        return NoContent();
    }
}

