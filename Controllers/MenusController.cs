using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class MenusController : ControllerBase
{
    private readonly IMenus _repository;
    private readonly ISubmenus _submenusRepository;
    private readonly IPermissions _permissions;
    private readonly SchoolContext _context;

    public MenusController(
        IMenus repository,
        ISubmenus submenusRepository,
        IPermissions permissions,
        SchoolContext context)
    {
        _repository = repository;
        _submenusRepository = submenusRepository;
        _permissions = permissions;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Menu>>> GetAll()
    {
        var menus = await _repository.GetAllAsync();
        return Ok(menus);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Menu>> GetById(int id)
    {
        var menu = await _repository.GetByIdAsync(id);
        if (menu == null) return NotFound();
        return Ok(menu);
    }

    /// <summary>
    /// Seeds default menus and submenus (matches app sidebar). Skips entries that already exist by name.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<object>> GenerateDefaults()
    {
        try
        {
        var menus = await _repository.GetAllAsync();
        var submenus = await _submenusRepository.GetAllAsync();

        var definitions = new (string Name, string Route, (string SubName, string SubRoute)[] Subs)[]
        {
            // Matches frontend Sidebar.tsx layout/paths.
            ("Dashboard", "/dashboard", Array.Empty<(string, string)>()),
            ("Setup", "/menu-management", new[]
            {
                ("Schools", "/schools"),
                ("School Users", "/schools/users"),
                ("Users", "/users"),
                ("Menus", "/menu-management"),
                ("Submenus", "/menu-management/submenus"),
                ("Roles", "/menu-management/roles"),
                ("Permissions", "/menu-management/permissions"),
                ("Stages", "/academic/stages"),
            }),
            ("Academic", "/academic", new[]
            {
                ("Levels", "/academic/levels"),
                ("Classes", "/academic/classes"),
                ("Subjects", "/academic/subjects"),
                ("Level Subjects", "/academic/level-subjects"),
                ("Class Subjects", "/academic/class-subjects"),
                ("Teachers", "/academic/teachers"),
            }),
            ("Exams", "/exams", new[]
            {
                ("Exams", "/exams"),
                ("Exam Schedule", "/exams/schedule"),
                ("Exam Subjects", "/exams/subjects"),
                ("Create Participants", "/exams/participants/create"),
                ("Participants", "/exams/participants"),
                ("Exam Rooms", "/exams/rooms"),
            }),
            ("Enrollment", "/student-enrollment", new[]
            {
                ("Enrollment", "/student-enrollment"),
                ("Students", "/student-enrollment/students"),
            }),
            ("Exit Students", "/exit-students", new[]
            {
                ("Mark as Exited", "/exit-students/mark"),
                ("Exited Students", "/exit-students/list"),
            }),
            ("Graduated", "/graduated-students", new[]
            {
                ("Add Graduated", "/graduated-students/mark"),
                ("View Graduated", "/graduated-students/list"),
            }),
            ("Transfer", "/transfer-students", new[] { ("Transfer Students", "/transfer-students") }),
            ("Result Entry", "/result-entry", new[]
            {
                ("Add Exam Results", "/result-entry/add"),
                ("Exam Results", "/result-entry"),
                ("Published Exam Results", "/result-entry/published"),
                ("Exam History", "/result-entry/history"),
            }),
            ("Attendances", "/attendances", new[]
            {
                ("Take Attendance", "/attendances/take"),
                ("View Attendance", "/attendances"),
            }),
            ("Payments", "/payments", new[]
            {
                ("Process Payment", "/payments"),
                ("Fee Categories", "/payments/categories"),
                ("Payment Methods", "/payments/methods"),
                ("Charges", "/payments/charges"),
            }),
            ("Financial Reports", "/financial-reports", new[]
            {
                ("Outstanding Students", "/financial-reports/outstanding-students"),
                ("Student Finance Report", "/financial-reports/student-finance"),
            }),
        };

        var menusCreated = 0;
        var submenusCreated = 0;

        foreach (var def in definitions)
        {
            var menu = menus.FirstOrDefault(m =>
                string.Equals(m.Name, def.Name, StringComparison.OrdinalIgnoreCase));
            if (menu == null)
            {
                menu = await _repository.AddAsync(new Menu { Name = def.Name, Route = def.Route });
                menus.Add(menu);
                menusCreated++;
            }
            else if (!string.Equals(menu.Route, def.Route, StringComparison.OrdinalIgnoreCase))
            {
                // Keep menu route in sync with the default seed.
                menu.Route = def.Route;
                await _repository.UpdateAsync(menu);
            }

            foreach (var (subName, subRoute) in def.Subs)
            {
                var existingSub = submenus.FirstOrDefault(s =>
                    s.MenuId == menu.Id &&
                    string.Equals(s.Name, subName, StringComparison.OrdinalIgnoreCase));

                if (existingSub == null)
                {
                    var createdSub = await _submenusRepository.AddAsync(new Submenu
                    {
                        MenuId = menu.Id,
                        Name = subName,
                        Route = subRoute,
                    });
                    submenus.Add(createdSub);
                    submenusCreated++;
                }
                else if (!string.Equals(existingSub.Route, subRoute, StringComparison.OrdinalIgnoreCase))
                {
                    // Keep submenu route in sync with the default seed.
                    existingSub.Route = subRoute;
                    await _submenusRepository.UpdateAsync(existingSub);
                }
            }
        }

            // Remove retired Accounting menu + submenus (management UI removed; data kept for charges/reports).
            var accountingMenu = menus.FirstOrDefault(m =>
                string.Equals(m.Name, "Accounting", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Route, "/accounting", StringComparison.OrdinalIgnoreCase));
            if (accountingMenu != null)
            {
                var accountingSubmenus = submenus
                    .Where(sm => sm.MenuId == accountingMenu.Id)
                    .ToList();

                foreach (var sm in accountingSubmenus)
                {
                    var obsoletePerms = _context.Permissions
                        .Where(p => p.SubmenuId == sm.Id);
                    _context.Permissions.RemoveRange(obsoletePerms);
                }

                var menuPerms = _context.Permissions.Where(p => p.MenuId == accountingMenu.Id);
                _context.Permissions.RemoveRange(menuPerms);
                await _context.SaveChangesAsync();

                foreach (var sm in accountingSubmenus)
                {
                    await _submenusRepository.DeleteAsync(sm);
                }

                await _repository.DeleteAsync(accountingMenu);
            }

            // Seed default stages for every school (Primary + Secondary).
            var schools = await _context.Schools.AsNoTracking().ToListAsync();
            foreach (var school in schools)
            {
                await EnsureStage(_context, school.Id, school.CreatorId, "Primary");
                await EnsureStage(_context, school.Id, school.CreatorId, "Secondary");
            }

        return Ok(new { menusCreated, submenusCreated, message = "Default menus generated or updated." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Generate menus failed", error = ex.Message });
        }
    }

    private static async Task EnsureStage(SchoolContext context, int schoolId, int creatorId, string stageName)
    {
        var exists = await context.Set<Stage>()
            .AnyAsync(s => s.SchoolId == schoolId && s.Name == stageName);
        if (exists) return;

        context.Set<Stage>().Add(new Stage
        {
            Name = stageName,
            SchoolId = schoolId,
            CreatorId = creatorId,
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Menu>> Create(Menu menu)
    {
        var created = await _repository.AddAsync(menu);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Menu>> Update(int id, Menu menu)
    {
        if (id != menu.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(menu);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var menu = await _repository.GetByIdAsync(id);
        if (menu == null) return NotFound();
        await _permissions.DeleteForMenuAsync(id);
        await _repository.DeleteAsync(menu);
        return NoContent();
    }
}

