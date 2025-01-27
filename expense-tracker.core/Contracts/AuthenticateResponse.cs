namespace expense_tracker.core.Contracts;

public class AuthenticateResponse
{
    public Guid UserId { get; set; }
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime Expires { get; set; }
}