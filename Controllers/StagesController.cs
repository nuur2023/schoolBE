using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class StagesController : ControllerBase
{
    private readonly IStages _repository;

    public StagesController(IStages repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Stage>>> GetAll()
    {
        var stages = await _repository.GetAllAsync();
        return Ok(stages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Stage>> GetById(int id)
    {
        var stage = await _repository.GetByIdAsync(id);
        if (stage == null) return NotFound();
        return Ok(stage);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Stage>> Create(Stage stage)
    {
        var created = await _repository.AddAsync(stage);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<Stage>>> CreateBulk(List<Stage> stages)
    {
        var createdStages = new List<Stage>();
        foreach (var stage in stages)
        {
            var created = await _repository.AddAsync(stage);
            createdStages.Add(created);
        }
        return Ok(createdStages);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Stage>> Update(int id, Stage stage)
    {
        if (id != stage.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(stage);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var stage = await _repository.GetByIdAsync(id);
        if (stage == null) return NotFound();
        await _repository.DeleteAsync(stage);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<Stage>> Activate(int id)
    {
        var stage = await _repository.ActivateAsync(id);
        return Ok(stage);
    }
}

