using expense_tracker.core.Enumerations;

namespace expense_tracker.core.Contracts;

public class UpdateExpenseRequest
{
    public string? Merchant { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public ExpenseCategory? Category { get; set; }
    public string? OtherCategoryName { get; set; }   
}