using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using expense_tracker.core.Primitives;

namespace expense_tracker.core.Entities;

public class UserRefreshToken : Entity
{
    public string Token { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateExpires { get; set; }
    public DateTime? DateRevoked { get; set; }
    public Guid UserId { get; set; }
    
    [JsonIgnore] public User User { get; set; } = null!;
}