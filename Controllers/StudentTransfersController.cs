using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class StudentTransfersController : ControllerBase
{
    private readonly IStudentTransfer _transferRepository;
    private readonly IStudentEnrollments _enrollmentRepository;

    public StudentTransfersController(IStudentTransfer transferRepository, IStudentEnrollments enrollmentRepository)
    {
        _transferRepository = transferRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<StudentTransfer>>> GetAll()
    {
        try
        {
            var transfers = await _transferRepository.GetAllAsync();
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching student transfers", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<StudentTransfer>> GetById(int id)
    {
        try
        {
            var transfer = await _transferRepository.GetByIdAsync(id);
            if (transfer == null) return NotFound();
            return Ok(transfer);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while fetching student transfer with ID {id}", error = ex.Message });
        }
    }

    [HttpGet("paged")]
    [Authorize]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] int? schoolId = null, [FromQuery] int? studentId = null)
    {
        try
        {
            var result = await _transferRepository.GetPagedAsync(page, pageSize, search, schoolId, studentId);
            return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching paged student transfers", error = ex.Message });
        }
    }

    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<List<StudentTransfer>>> GetByStudentId(int studentId)
    {
        try
        {
            var transfers = await _transferRepository.GetByStudentIdAsync(studentId);
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while fetching transfers for student {studentId}", error = ex.Message });
        }
    }

    [HttpPost("transfer")]
    [Authorize]
    public async Task<ActionResult<StudentTransfer>> TransferStudents([FromBody] TransferRequest request)
    {
        try
        {
            if (request == null || request.StudentIds == null || request.StudentIds.Count == 0)
            {
                return BadRequest(new { message = "Transfer request with student IDs is required" });
            }

            if (request.ToStageId <= 0 || request.ToLevelId <= 0 || request.ToClassId <= 0)
            {
                return BadRequest(new { message = "Valid target stage, level, and class are required" });
            }

            var userId = GetCurrentUserId();
            var transfers = new List<StudentTransfer>();

            foreach (var studentId in request.StudentIds)
            {
                // Get the current student enrollment
                var student = await _enrollmentRepository.GetByIdAsync(studentId);
                if (student == null)
                {
                    continue; // Skip if student not found
                }

                // Determine transfer type
                string transferType;
                if (student.StageId != request.ToStageId)
                {
                    transferType = "StageToStage";
                }
                else if (student.LevelId != request.ToLevelId)
                {
                    transferType = "LevelToLevel";
                }
                else
                {
                    transferType = "ClassToClass";
                }

                // Create transfer record
                var transfer = new StudentTransfer
                {
                    StudentId = studentId,
                    SchoolId = student.SchoolId,
                    CreatorId = userId,
                    TransferDate = request.TransferDate ?? DateTime.UtcNow,
                    TransferType = transferType,
                    FromStageId = student.StageId,
                    FromLevelId = student.LevelId,
                    FromClassId = student.ClassId,
                    ToStageId = request.ToStageId,
                    ToLevelId = request.ToLevelId,
                    ToClassId = request.ToClassId,
                    Reason = request.Reason
                };

                // Update student enrollment with new stage, level, and class
                student.StageId = request.ToStageId;
                student.LevelId = request.ToLevelId;
                student.ClassId = request.ToClassId;
                student.UpdatedAt = DateTime.UtcNow;

                // Save transfer record first, then update enrollment
                await _transferRepository.AddAsync(transfer);
                await _enrollmentRepository.UpdateAsync(student);

                transfers.Add(transfer);
            }

            if (transfers.Count == 0)
            {
                return BadRequest(new { message = "No valid students found to transfer" });
            }

            // Return the first transfer with full details
            var result = await _transferRepository.GetByIdAsync(transfers[0].Id);
            return Ok(new { 
                message = $"Successfully transferred {transfers.Count} student(s)",
                transfer = result,
                count = transfers.Count
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while transferring students", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<StudentTransfer>> Update(int id, [FromBody] StudentTransfer transfer)
    {
        try
        {
            if (id != transfer.Id) return BadRequest();
            
            // Get existing transfer to preserve history
            var existingTransfer = await _transferRepository.GetByIdAsync(id);
            if (existingTransfer == null) return NotFound();

            // Validate target stage, level, and class
            if (transfer.ToStageId <= 0 || transfer.ToLevelId <= 0 || transfer.ToClassId <= 0)
            {
                return BadRequest(new { message = "Valid target stage, level, and class are required" });
            }

            // Get the student enrollment
            var student = await _enrollmentRepository.GetByIdAsync(existingTransfer.StudentId);
            if (student == null)
            {
                return NotFound(new { message = $"Student with ID {existingTransfer.StudentId} not found" });
            }

            // Recalculate transfer type based on the original "from" values and new "to" values
            string transferType;
            if (existingTransfer.FromStageId != transfer.ToStageId)
            {
                transferType = "StageToStage";
            }
            else if (existingTransfer.FromLevelId != transfer.ToLevelId)
            {
                transferType = "LevelToLevel";
            }
            else
            {
                transferType = "ClassToClass";
            }

            // Update the transfer record (for correcting mistakes)
            existingTransfer.ToStageId = transfer.ToStageId;
            existingTransfer.ToLevelId = transfer.ToLevelId;
            existingTransfer.ToClassId = transfer.ToClassId;
            existingTransfer.TransferType = transferType;
            existingTransfer.Reason = transfer.Reason;
            existingTransfer.UpdatedAt = DateTime.UtcNow;

            // Update the student's current enrollment to match the corrected transfer
            student.StageId = transfer.ToStageId;
            student.LevelId = transfer.ToLevelId;
            student.ClassId = transfer.ToClassId;
            student.UpdatedAt = DateTime.UtcNow;

            // Save both updates
            await _enrollmentRepository.UpdateAsync(student);
            var updated = await _transferRepository.UpdateAsync(existingTransfer);
            
            var result = await _transferRepository.GetByIdAsync(updated.Id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while updating student transfer with ID {id}", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var transfer = await _transferRepository.GetByIdAsync(id);
            if (transfer == null) return NotFound();
            await _transferRepository.DeleteAsync(transfer);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while deleting student transfer with ID {id}", error = ex.Message });
        }
    }
}

public class TransferRequest
{
    public List<int> StudentIds { get; set; } = new();
    public int ToStageId { get; set; }
    public int ToLevelId { get; set; }
    public int ToClassId { get; set; }
    public DateTime? TransferDate { get; set; }
    public string? Reason { get; set; }
}

