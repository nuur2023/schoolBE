using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendance _repository;

    public AttendanceController(IAttendance repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Attendance>>> GetAll()
    {
        var attendances = await _repository.GetAllAsync();
        return Ok(attendances);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Attendance>> GetById(int id)
    {
        var attendance = await _repository.GetByIdAsync(id);
        if (attendance == null) return NotFound();
        return Ok(attendance);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Attendance>> Create(Attendance attendance)
    {
        var created = await _repository.AddAsync(attendance);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Attendance>> Update(int id, Attendance attendance)
    {
        if (id != attendance.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(attendance);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var attendance = await _repository.GetByIdAsync(id);
        if (attendance == null) return NotFound();
        await _repository.DeleteAsync(attendance);
        return NoContent();
    }
}

