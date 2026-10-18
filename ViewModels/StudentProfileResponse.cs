namespace schoolBE.ViewModels;

public class StudentProfileResponse
{
    public StudentProfileDto Student { get; set; } = new();
    public List<ContactDto> Contacts { get; set; } = new();
}

public class StudentProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GenId { get; set; } = string.Empty;
    public DateTime DOB { get; set; }
    public string POB { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Mother { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string CurrentAcademicYear { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsActiveInApp { get; set; }
    public string? School { get; set; }
    public string? Stage { get; set; }
    public string? Level { get; set; }
    public string? Class { get; set; }
}

public class ContactDto
{
    public int Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public int SchoolId { get; set; }
}

