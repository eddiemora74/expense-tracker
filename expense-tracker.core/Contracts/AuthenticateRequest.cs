namespace expense_tracker.core.Contracts;

public class AuthenticateRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}