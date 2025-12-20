namespace schoolBE.Models;

public class User : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

