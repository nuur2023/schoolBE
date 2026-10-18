using System.Text.Json.Serialization;

namespace schoolBE.ViewModels;

public class StudentLoginRequest
{
    [JsonPropertyName("genId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string GenId { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Password { get; set; } = string.Empty;
}

