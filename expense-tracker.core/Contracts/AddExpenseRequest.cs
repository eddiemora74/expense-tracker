using expense_tracker.core.Enumerations;

namespace expense_tracker.core.Contracts;

public class AddExpenseRequest
{
    public string Merchant { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public ExpenseCategory Category { get; set; }
    public string? OtherExpenseCategory { get; set; }
}