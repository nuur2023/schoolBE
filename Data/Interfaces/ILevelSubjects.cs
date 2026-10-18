using schoolBE.Models;

namespace schoolBE.Data.Interfaces;

public interface ILevelSubjects : IBaseSchoolInterface<LevelSubject>
{
    Task<LevelSubject?> GetByIdAsync(int id);
    Task<List<LevelSubject>> GetAllAsync();
    Task<(List<LevelSubject> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<List<Subject>> GetSubjectsByLevelIdAsync(int levelId);
}


