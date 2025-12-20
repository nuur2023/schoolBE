namespace schoolBE.Models;

public class Contact : BaseModel
{
    public int SchoolId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public int CreatorId { get; set; }
    public int? PersonId { get; set; }
    public int? StudentId { get; set; }
    public int? UsersId { get; set; }
}

