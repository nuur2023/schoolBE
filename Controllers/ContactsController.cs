using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IContacts _repository;

    public ContactsController(IContacts repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Contact>>> GetAll()
    {
        var contacts = await _repository.GetAllAsync();
        return Ok(contacts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetById(int id)
    {
        var contact = await _repository.GetByIdAsync(id);
        if (contact == null) return NotFound();
        return Ok(contact);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Contact>> Create(Contact contact)
    {
        var created = await _repository.AddAsync(contact);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<Contact>>> CreateBulk(List<Contact> contacts)
    {
        var createdContacts = new List<Contact>();
        var existingContacts = await _repository.GetAllAsync();
        
        foreach (var contact in contacts)
        {
            // Check if this phone number already exists for the same person and school
            var duplicate = existingContacts.FirstOrDefault(c => 
                c.PersonId == contact.PersonId && 
                c.SchoolId == contact.SchoolId && 
                c.Phone == contact.Phone);
            
            if (duplicate == null)
            {
                var created = await _repository.AddAsync(contact);
                createdContacts.Add(created);
                // Add to existing contacts list to check against in the same batch
                existingContacts.Add(created);
            }
        }
        return Ok(createdContacts);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Contact>> Update(int id, Contact contact)
    {
        if (id != contact.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(contact);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var contact = await _repository.GetByIdAsync(id);
        if (contact == null) return NotFound();
        await _repository.DeleteAsync(contact);
        return NoContent();
    }

    [HttpGet("person/{personId}")]
    public async Task<ActionResult<List<Contact>>> GetByPersonId(int personId)
    {
        var contacts = await _repository.GetAllAsync();
        var personContacts = contacts.Where(c => c.PersonId == personId).ToList();
        return Ok(personContacts);
    }

    [HttpDelete("person/{personId}")]
    public async Task<ActionResult> DeleteByPersonId(int personId)
    {
        var contacts = await _repository.GetAllAsync();
        var personContacts = contacts.Where(c => c.PersonId == personId).ToList();
        foreach (var contact in personContacts)
        {
            await _repository.DeleteAsync(contact);
        }
        return NoContent();
    }
}

