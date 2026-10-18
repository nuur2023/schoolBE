using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Models.DTOs;

namespace schoolBE.Services;

public class AutoChargeRunner
{
    private readonly SchoolContext _context;
    private readonly IStudentEnrollments _studentRepository;
    private readonly IFeeCategories _feeCategoriesRepository;
    private readonly IStudentChargePostingService _chargePostingService;
    private readonly ILogger<AutoChargeRunner> _logger;

    public AutoChargeRunner(
        SchoolContext context,
        IStudentEnrollments studentRepository,
        IFeeCategories feeCategoriesRepository,
        IStudentChargePostingService chargePostingService,
        ILogger<AutoChargeRunner> logger)
    {
        _context = context;
        _studentRepository = studentRepository;
        _feeCategoriesRepository = feeCategoriesRepository;
        _chargePostingService = chargePostingService;
        _logger = logger;
    }

    public async Task RunDueAsync(CancellationToken cancellationToken = default)
    {
        var nowLocal = DateTime.Now;
        var settings = await _context.Set<AutoChargeSetting>()
            .Where(s => s.IsActive && s.IsAllowedAutoCharge)
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            if (!AutoChargeScheduleHelper.ShouldRun(setting, nowLocal))
                continue;

            var category = await _feeCategoriesRepository.GetByIdAsync(setting.FeeCategoryId);
            if (category == null)
            {
                _logger.LogWarning("AutoCharge setting {Id}: fee category {FeeCategoryId} missing", setting.Id,
                    setting.FeeCategoryId);
                continue;
            }

            var students = await _studentRepository.GetByStageLevelClassAsync(setting.StageId, null, null, null);
            students = students.Where(s => s.SchoolId == setting.SchoolId).ToList();

            var request = new ChargeStudentsRequest
            {
                ChargeType = "stage",
                SchoolId = setting.SchoolId,
                StageId = setting.StageId,
                FeeCategories = new List<FeeCategoryCharge>
                {
                    new() { FeeCategoryId = setting.FeeCategoryId, Amount = setting.Amount }
                },
                Description = $"Automatic charge (auto-charge rule #{setting.Id})"
            };

            var outcome = await _chargePostingService.PostChargesAsync(request, students, setting.CreatorId,
                cancellationToken);

            if (outcome.IsFailure)
            {
                _logger.LogWarning("AutoCharge setting {Id} failed: {Body}", setting.Id, outcome.ErrorBody);
                continue;
            }

            if (outcome.ChargedCount > 0)
            {
                setting.LastRunOn = DateTime.SpecifyKind(nowLocal.Date, DateTimeKind.Unspecified);
                setting.UpdatedAt = DateTime.UtcNow;
                _context.Set<AutoChargeSetting>().Update(setting);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
