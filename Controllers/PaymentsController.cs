using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.ViewModels;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class PaymentsController : ControllerBase
{
    private readonly IPayment _repository;
    private readonly SchoolContext _context;
    private readonly IApp _appRepository;
    private readonly ISchoolUsers _schoolUsersRepository;

    public PaymentsController(IPayment repository, SchoolContext context, IApp appRepository, ISchoolUsers schoolUsersRepository)
    {
        _repository = repository;
        _context = context;
        _appRepository = appRepository;
        _schoolUsersRepository = schoolUsersRepository;
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

    /// <summary>Builds PAY-yyyyMMdd-0001 style references, sequencing within the payment day.</summary>
    private async Task<string> GeneratePaidReferenceNoAsync(DateTime createdAt)
    {
        var datePart = createdAt.ToString("yyyyMMdd");
        var prefix = $"PAY-{datePart}-";

        var lastRef = await _context.Set<Payment>()
            .AsNoTracking()
            .Where(p => p.Type == "paid" && p.ReferenceNo.StartsWith(prefix))
            .OrderByDescending(p => p.ReferenceNo)
            .Select(p => p.ReferenceNo)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (!string.IsNullOrEmpty(lastRef))
        {
            var suffix = lastRef.Length > prefix.Length ? lastRef[prefix.Length..] : string.Empty;
            if (int.TryParse(suffix, out var parsed) && parsed >= 0)
                nextSeq = parsed + 1;
        }

        return $"{prefix}{nextSeq:0000}";
    }

    private async Task<int?> GetCurrentUserSchoolIdAsync()
    {
        var userId = GetCurrentUserId();
        // Get schoolId from SchoolUser table where UsersId matches the current user
        var schoolUser = await _context.Set<SchoolUser>()
            .Where(su => su.UsersId == userId && su.IsActive)
            .FirstOrDefaultAsync();
        
        // If no SchoolUser found, user is likely a superadmin (no schoolId in token)
        if (schoolUser == null)
        {
            return null;
        }
        return schoolUser.SchoolId;
    }

    [HttpGet]
    public async Task<ActionResult<List<Payment>>> GetAll()
    {
        var payments = await _repository.GetAllAsync();
        return Ok(payments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetById(int id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null) return NotFound();
        return Ok(payment);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> Create(Payment payment)
    {
        var created = await _repository.AddAsync(payment);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Payment>> Update(int id, Payment payment)
    {
        if (id != payment.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(payment);
        return Ok(updated);
    }

    /// <summary>Update a charge row amount (type must be charged).</summary>
    [HttpPut("charges/{id}")]
    [Authorize]
    public async Task<ActionResult<Payment>> UpdateCharge(int id, [FromBody] UpdateChargeAmountRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than 0." });

        var payment = await _repository.GetByIdAsync(id);
        if (payment == null) return NotFound(new { message = "Charge not found." });
        if (!string.Equals(payment.Type, "charged", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only charge records can be updated here." });

        payment.Charged = request.Amount;
        payment.UpdatedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateAsync(payment);
        return Ok(updated);
    }

    /// <summary>Update a paid payment amount (type must be paid).</summary>
    [HttpPut("paid/{id}")]
    [Authorize]
    public async Task<ActionResult<Payment>> UpdatePaidPayment(int id, [FromBody] UpdateChargeAmountRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than 0." });

        var payment = await _repository.GetByIdAsync(id);
        if (payment == null) return NotFound(new { message = "Payment not found." });
        if (!string.Equals(payment.Type, "paid", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only payment records can be updated here." });

        payment.Paid = request.Amount;
        payment.UpdatedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateAsync(payment);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null) return NotFound();
        await _repository.DeleteAsync(payment);
        return NoContent();
    }

    /// <summary>Delete a charge row only (not payments).</summary>
    [HttpDelete("charges/{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteCharge(int id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null) return NotFound(new { message = "Charge not found." });
        if (!string.Equals(payment.Type, "charged", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only charge records can be deleted here." });

        await _repository.DeleteAsync(payment);
        return NoContent();
    }

    /// <summary>Delete a paid payment row only (not charges).</summary>
    [HttpDelete("paid/{id}")]
    [Authorize]
    public async Task<ActionResult> DeletePaidPayment(int id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null) return NotFound(new { message = "Payment not found." });
        if (!string.Equals(payment.Type, "paid", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only payment records can be deleted here." });

        await _repository.DeleteAsync(payment);
        return NoContent();
    }

    [HttpGet("student/{studentId}/details")]
    [Authorize]
    public async Task<ActionResult<StudentPaymentDetailsResponse>> GetStudentPaymentDetails(int studentId)
    {
        try
        {
            var student = await _appRepository.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var balance = await _appRepository.GetStudentBalanceAsync(studentId);

            var response = new StudentPaymentDetailsResponse
            {
                Id = student.Id,
                Name = student.Name,
                GenId = student.GenId,
                Level = student.Level?.Name,
                Class = student.Class?.Name,
                UnpaidBalance = balance
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching student payment details", error = ex.Message });
        }
    }

    [HttpGet("student/{studentId}/history")]
    [Authorize]
    public async Task<ActionResult> GetStudentPaymentHistory(int studentId)
    {
        try
        {
            var student = await _appRepository.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Fetch directly from Payments table to match what admins see in phpMyAdmin.
            var payments = await _context.Set<Payment>()
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var sortedHistory = payments.Select(p => new
            {
                id = p.Id,
                referenceNo = p.ReferenceNo,
                description = p.Type == "charged" ? "Charge" : "Payment",
                date = p.CreatedAt,
                debit = p.Type == "charged" ? p.Charged : 0m,
                credit = p.Type == "paid" ? p.Paid : 0m,
                charged = p.Charged,
                paid = p.Paid,
                type = p.Type == "charged" ? "charge" : "payment",
                schoolId = p.SchoolId
            }).ToList();

            return Ok(new
            {
                student = new
                {
                    id = student.Id,
                    name = student.Name,
                    genId = student.GenId,
                    level = student.Level?.Name,
                    @class = student.Class?.Name,
                    schoolId = student.SchoolId,
                    schoolName = student.School?.Name ?? string.Empty,
                    schoolLogo = student.School?.Logo,
                    schoolAddress = student.School?.Address ?? string.Empty
                },
                history = sortedHistory
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching student payment history", error = ex.Message });
        }
    }

    /// <summary>
    /// Payment-table based history (one row per charge/payment record).
    /// This is useful for the admin UI \"Charge & Payment History\" table.
    /// </summary>
    [HttpGet("student/{studentId}/payments-history")]
    [Authorize]
    public async Task<ActionResult> GetStudentPaymentsHistory(int studentId)
    {
        try
        {
            var student = await _context.Set<StudentEnrollment>()
                .Include(s => s.Level)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null) return NotFound(new { message = "Student not found" });

            var school = await _context.Set<School>()
                .FirstOrDefaultAsync(s => s.Id == student.SchoolId);

            var payments = await _context.Set<Payment>()
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var history = payments.Select(p => new
            {
                id = p.Id,
                referenceNo = p.ReferenceNo,
                description = p.Type,
                date = p.CreatedAt,
                charged = (decimal)p.Charged,
                paid = (decimal)p.Paid,
                type = p.Type == "charged" ? "charge" : "payment",
                schoolId = p.SchoolId,
                studentId = p.StudentId
            }).ToList();

            return Ok(new
            {
                student = new
                {
                    id = student.Id,
                    name = student.Name,
                    genId = student.GenId,
                    level = student.Level?.Name,
                    @class = student.Class?.Name,
                    schoolId = student.SchoolId,
                    schoolName = school?.Name ?? string.Empty,
                    schoolLogo = school?.Logo,
                    schoolAddress = school?.Address ?? string.Empty
                },
                history
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching payment history", error = ex.Message });
        }
    }

    [HttpPost("process")]
    [Authorize]
    public async Task<ActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        try
        {
            if (request == null || request.StudentId <= 0 || request.AmountPaid <= 0)
            {
                return BadRequest(new { message = "Student ID and amount paid are required and must be greater than 0" });
            }

            var creatorId = GetCurrentUserId();

            // Get student
            var student = await _appRepository.GetStudentByIdAsync(request.StudentId);
            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            var currentBalance = await _appRepository.GetStudentBalanceAsync(request.StudentId);

            if (request.AmountPaid > currentBalance)
            {
                return BadRequest(new { message = $"Payment amount ({request.AmountPaid}) cannot exceed unpaid balance ({currentBalance})" });
            }

            var userSchoolIdFromToken = await GetCurrentUserSchoolIdAsync();
            if (userSchoolIdFromToken == null)
            {
                if (!request.SchoolId.HasValue || request.SchoolId.Value <= 0)
                {
                    return BadRequest(new { message = "School ID is required for superadmin users" });
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var payment = new Payment
            {
                ReferenceNo = string.Empty,
                Type = "paid",
                Charged = 0,
                Paid = request.AmountPaid,
                SchoolId = student.SchoolId,
                StudentId = student.Id,
                PaymentMethod = request.PaymentMethod?.Trim() ?? string.Empty,
                CreatorId = creatorId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Set<Payment>().AddAsync(payment);
            await _context.SaveChangesAsync();
            payment.ReferenceNo = await GeneratePaidReferenceNoAsync(payment.CreatedAt);
            payment.UpdatedAt = DateTime.UtcNow;
            _context.Set<Payment>().Update(payment);
            await _context.SaveChangesAsync();

            var updatedBalance = await _appRepository.GetStudentBalanceAsync(request.StudentId);

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Payment processed successfully",
                paymentId = payment.Id,
                referenceNo = payment.ReferenceNo,
                previousBalance = currentBalance,
                amountPaid = request.AmountPaid,
                newBalance = updatedBalance
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing payment", error = ex.Message });
        }
    }
}

public class ProcessPaymentRequest
{
    public int StudentId { get; set; }
    public decimal AmountPaid { get; set; }
    public string? Description { get; set; }
    public string? PaymentMethod { get; set; }
    public int? SchoolId { get; set; } // Required for superadmin, optional for admin (uses token schoolId)
}

public class UpdateChargeAmountRequest
{
    public decimal Amount { get; set; }
}

