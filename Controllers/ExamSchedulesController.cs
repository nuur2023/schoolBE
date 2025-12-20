using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamSchedulesController : ControllerBase
{
    private readonly IExamSchedule _repository;

    public ExamSchedulesController(IExamSchedule repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExamSchedule>>> GetAll()
    {
        var schedules = await _repository.GetAllAsync();
        return Ok(schedules);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamSchedule>> GetById(int id)
    {
        var schedule = await _repository.GetByIdAsync(id);
        if (schedule == null) return NotFound();
        return Ok(schedule);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<ExamSchedule>> Create(ExamSchedule schedule)
    {
        var created = await _repository.AddAsync(schedule);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamSchedule>> Update(int id, ExamSchedule schedule)
    {
        if (id != schedule.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(schedule);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var schedule = await _repository.GetByIdAsync(id);
        if (schedule == null) return NotFound();
        await _repository.DeleteAsync(schedule);
        return NoContent();
    }
}

