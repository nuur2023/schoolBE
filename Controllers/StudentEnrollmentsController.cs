using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentEnrollmentsController : ControllerBase
{
    private readonly IStudentEnrollments _repository;

    public StudentEnrollmentsController(IStudentEnrollments repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentEnrollment>>> GetAll()
    {
        var students = await _repository.GetAllAsync();
        return Ok(students);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentEnrollment>> GetById(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null) return NotFound();
        return Ok(student);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<StudentEnrollment>> Create(StudentEnrollment student)
    {
        var created = await _repository.AddAsync(student);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<StudentEnrollment>> Update(int id, StudentEnrollment student)
    {
        if (id != student.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(student);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null) return NotFound();
        await _repository.DeleteAsync(student);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<StudentEnrollment>> Activate(int id)
    {
        var student = await _repository.ActivateAsync(id);
        return Ok(student);
    }
}

