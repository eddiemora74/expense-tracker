using System.Runtime.Serialization;
using expense_tracker.core.Primitives;

namespace expense_tracker.core.Entities;

public class User : Entity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public bool IsActive { get; set; }

    [IgnoreDataMember] public IList<UserRefreshToken> RefreshTokens { get; set; } = [];
    [IgnoreDataMember] public IList<Expense> Expenses { get; set; } = [];
}