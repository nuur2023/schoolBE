using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Data.Repositories;

public class ExamParticipantsRepository(SchoolContext context) : IExamParticipants
{
    private readonly SchoolContext _context = context;
    private readonly DbSet<ExamParticipants> _dbSet = context.Set<ExamParticipants>();

    public async Task<ExamParticipants> AddAsync(ExamParticipants entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamParticipants> UpdateAsync(ExamParticipants entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamParticipants> DeleteAsync(ExamParticipants entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<ExamParticipants?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.Exam)
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.ExamSubjects)
                    .ThenInclude(exs => exs.LevelSubject)
                        .ThenInclude(ls => ls!.Level)
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.ExamSubjects)
                    .ThenInclude(exs => exs.LevelSubject)
                        .ThenInclude(ls => ls!.Subject)
            .Include(ep => ep.Student)
            .Include(ep => ep.Class)
            .Include(ep => ep.Level)
            .Include(ep => ep.Room)
            .Include(ep => ep.Subject)
            .FirstOrDefaultAsync(ep => ep.Id == id);
    }

    public async Task<List<ExamParticipants>> GetAllAsync()
    {
        return await _dbSet
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.Exam)
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.ExamSubjects)
                    .ThenInclude(exs => exs.LevelSubject)
                        .ThenInclude(ls => ls!.Level)
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.ExamSubjects)
                    .ThenInclude(exs => exs.LevelSubject)
                        .ThenInclude(ls => ls!.Subject)
            .Include(ep => ep.Student)
            .Include(ep => ep.Class)
            .Include(ep => ep.Level)
            .Include(ep => ep.Room)
            .Include(ep => ep.Subject)
            .OrderByDescending(ep => ep.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<ExamParticipants> data, int totalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _dbSet
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.Exam)
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.ExamSubjects)
                    .ThenInclude(exs => exs.LevelSubject)
                        .ThenInclude(ls => ls!.Level)
            .Include(ep => ep.ExamSchedule)
                .ThenInclude(es => es!.ExamSubjects)
                    .ThenInclude(exs => exs.LevelSubject)
                        .ThenInclude(ls => ls!.Subject)
            .Include(ep => ep.Student)
            .Include(ep => ep.Class)
            .Include(ep => ep.Level)
            .Include(ep => ep.Room)
            .Include(ep => ep.Subject)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(ep =>
                (ep.Student != null && ep.Student.Name.Contains(search)) ||
                (ep.Student != null && ep.Student.GenId.Contains(search)) ||
                (ep.ExamSchedule != null && ep.ExamSchedule.Exam != null && ep.ExamSchedule.Exam.Title.Contains(search)) ||
                (ep.ExamSchedule != null && ep.ExamSchedule.ExamSubjects.Any(exs => exs.LevelSubject != null && exs.LevelSubject.Subject != null && exs.LevelSubject.Subject.Name.Contains(search))) ||
                (ep.Level != null && ep.Level.Name.Contains(search)) ||
                (ep.Class != null && ep.Class.Name.Contains(search)) ||
                (ep.Room != null && ep.Room.Name.Contains(search)) ||
                (ep.Subject != null && ep.Subject.Name.Contains(search)) ||
                (ep.SeatNumber != null && ep.SeatNumber.Contains(search)) ||
                ep.Status.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderByDescending(ep => ep.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<List<ExamParticipants>> GetByExamScheduleIdAsync(int examScheduleId)
    {
        return await _dbSet
            .Include(ep => ep.Student)
            .Where(ep => ep.ExamScheduleId == examScheduleId)
            .ToListAsync();
    }

    public async Task<List<int>> GetParticipatedStudentIdsAsync(int examScheduleId)
    {
        return await _dbSet
            .Where(ep => ep.ExamScheduleId == examScheduleId && 
                        (ep.Status == "Scheduled" || (ep.Status == "ReceivedMarks" && ep.ReceivedMarks == false)))
            .Select(ep => ep.StudentId)
            .ToListAsync();
    }
}
