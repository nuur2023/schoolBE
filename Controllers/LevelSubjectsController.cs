using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class LevelSubjectsController : ControllerBase
{
    private readonly ILevelSubjects _repository;

    public LevelSubjectsController(ILevelSubjects repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<LevelSubject>>> GetAll()
    {
        var levelSubjects = await _repository.GetAllAsync();
        return Ok(levelSubjects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LevelSubject>> GetById(int id)
    {
        var levelSubject = await _repository.GetByIdAsync(id);
        if (levelSubject == null) return NotFound();
        return Ok(levelSubject);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<LevelSubject>> Create(LevelSubject levelSubject)                                                                             
    {
        var created = await _repository.AddAsync(levelSubject);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);                                                                              
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<LevelSubject>>> CreateBulk(List<LevelSubject> levelSubjects)
    {
        var createdLevelSubjects = new List<LevelSubject>();
        foreach (var levelSubject in levelSubjects)
        {
            var created = await _repository.AddAsync(levelSubject);
            createdLevelSubjects.Add(created);
        }
        return Ok(createdLevelSubjects);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<LevelSubject>> Update(int id, LevelSubject levelSubject)
    {
        if (id != levelSubject.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(levelSubject);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var levelSubject = await _repository.GetByIdAsync(id);
        if (levelSubject == null) return NotFound();
        await _repository.DeleteAsync(levelSubject);
        return NoContent();
    }

    [HttpGet("by-level/{levelId}")]
    public async Task<ActionResult<List<Subject>>> GetSubjectsByLevel(int levelId)
    {
        var subjects = await _repository.GetSubjectsByLevelIdAsync(levelId);
        return Ok(subjects);
    }
}

