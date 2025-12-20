namespace schoolBE.Models;

public class SchoolUser : BaseModel
{
    public int SchoolId { get; set; }
    public int UsersId { get; set; }
    public int CreatorId { get; set; }
    public bool IsActive { get; set; }
    
    public School? School { get; set; }
    public User? User { get; set; }
}

