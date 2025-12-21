using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly IPersons _repository;

    public PersonsController(IPersons repository)
    {
        _repository = repository;
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
    public async Task<ActionResult<Person>> Create(Person person)
    {
        var created = await _repository.AddAsync(person);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Person>> Update(int id, Person person)
    {
        if (id != person.Id) return BadRequest();
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

    [HttpPut("{id}/activate")]
    public async Task<ActionResult<Person>> Activate(int id)
    {
        var person = await _repository.ActivateAsync(id);
        return Ok(person);
    }
}

