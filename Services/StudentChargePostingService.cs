using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Helpers;
using schoolBE.Models;
using schoolBE.Models.DTOs;

namespace schoolBE.Services;

public class ChargePostingOutcome
{
    public bool IsFailure { get; init; }
    public int? StatusCode { get; init; }
    public object? ErrorBody { get; init; }
    public int ChargedCount { get; init; }
    public int TotalStudents { get; init; }
    public List<string>? RowErrors { get; init; }

    public static ChargePostingOutcome Fail(int statusCode, object body) =>
        new() { IsFailure = true, StatusCode = statusCode, ErrorBody = body };

    public static ChargePostingOutcome Ok(int chargedCount, int totalStudents, List<string>? rowErrors) =>
        new()
        {
            IsFailure = false,
            ChargedCount = chargedCount,
            TotalStudents = totalStudents,
            RowErrors = rowErrors
        };
}

public interface IStudentChargePostingService
{
    Task<ChargePostingOutcome> PostChargesAsync(
        ChargeStudentsRequest request,
        IReadOnlyList<StudentEnrollment> students,
        int creatorId,
        CancellationToken cancellationToken = default);
}

public class StudentChargePostingService : IStudentChargePostingService
{
    private readonly SchoolContext _context;

    public StudentChargePostingService(SchoolContext context)
    {
        _context = context;
    }

    public async Task<ChargePostingOutcome> PostChargesAsync(
        ChargeStudentsRequest request,
        IReadOnlyList<StudentEnrollment> students,
        int creatorId,
        CancellationToken cancellationToken = default)
    {
        if (request.FeeCategories == null || request.FeeCategories.Count == 0)
        {
            return ChargePostingOutcome.Fail(400, new { message = "Fee categories and amounts are required" });
        }

        if (students.Count == 0)
        {
            return ChargePostingOutcome.Fail(400, new { message = "No students to charge" });
        }

        var chargedCount = 0;
        var errors = new List<string>();
        var feeList = request.FeeCategories;

        foreach (var student in students)
        {
            try
            {
                var allocations = ChargeAllocationHelper.AllocateAfterDiscount(student.Discount, feeList);
                var totalCharge = allocations.Sum();
                if (totalCharge <= 0m)
                {
                    continue;
                }

                var newPayments = new List<Payment>();
                for (var i = 0; i < feeList.Count; i++)
                {
                    var charged = allocations[i];
                    if (charged <= 0m)
                        continue;

                    var feeCategory = feeList[i];
                    var payment = new Payment
                    {
                        ReferenceNo = string.Empty,
                        Type = "charged",
                        Charged = charged,
                        Paid = 0,
                        SchoolId = request.SchoolId,
                        StudentId = student.Id,
                        PaymentMethod = string.Empty,
                        FeeCategoriesId = feeCategory.FeeCategoryId,
                        CreatorId = creatorId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.Set<Payment>().AddAsync(payment, cancellationToken);
                    newPayments.Add(payment);
                }

                if (newPayments.Count == 0)
                    continue;

                await _context.SaveChangesAsync(cancellationToken);
                foreach (var p in newPayments)
                {
                    p.ReferenceNo = $"CHG-{p.Id:0000}";
                    p.UpdatedAt = DateTime.UtcNow;
                    _context.Set<Payment>().Update(p);
                }

                await _context.SaveChangesAsync(cancellationToken);
                chargedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Error charging student {student.Name} (ID: {student.Id}): {ex.Message}");
            }
        }

        if (chargedCount == 0)
        {
            return ChargePostingOutcome.Fail(500,
                new { message = "Failed to charge any students", errors });
        }

        return ChargePostingOutcome.Ok(chargedCount, students.Count, errors.Count > 0 ? errors : null);
    }
}
