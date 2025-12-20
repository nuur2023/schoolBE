namespace schoolBE.ViewModels;

public class PermissionVM
{
    public int Id { get; set; }
    public int? MenuId { get; set; }
    public int? SubmenuId { get; set; }
    public int UsersId { get; set; }
    public int SchoolId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanAccessMenu { get; set; }
    public bool CanAccessSubmenu { get; set; }
}

