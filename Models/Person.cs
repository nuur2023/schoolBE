namespace schoolBE.Models;

public class Person : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int RoleId { get; set; }
    public int? SchoolId { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
    
    public Role? Role { get; set; }
    public School? School { get; set; }
}

