namespace schoolBE.Constants;

/// <summary>
/// API endpoint constants for the mobile app
/// </summary>
public static class ApiEndpoints
{
    // Base Url
    public const string BaseUrl = "http://10.96.73.20:5028/api";

    // Login Endpoint
    public const string Login = $"{BaseUrl}/App/login";

    // Dashboard Endpoint
    public const string Dashboard = $"{BaseUrl}/App/dashboard";

    // Student Card Endpoint
    public const string StudentCard = $"{BaseUrl}/App/student-card";
}
