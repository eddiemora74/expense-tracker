using System.Text.Json.Serialization;
using expense_tracker.core.Enumerations;
using expense_tracker.core.Primitives;

namespace expense_tracker.core.Entities;

public class Expense : Entity
{
    public string Merchant { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public ExpenseCategory Category { get; set; }
    public string? OtherCategoryName { get; set; }
    public Guid UserId { get; set; }
    
    [JsonIgnore]
    public User User { get; set; } = null!;
}