using Carter;
using expense_tracker.api.Database;
using expense_tracker.api.Utilities;
using expense_tracker.core.Contracts;
using expense_tracker.core.Enumerations;
using expense_tracker.core.Primitives;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace expense_tracker.api.Features.Expense;

public static class AddExpense
{
    public class Command : IRequest<Result<core.Entities.Expense>>
    {
        public string Merchant { get; }
        public string Description { get; }
        public decimal Amount { get; }
        public ExpenseCategory ExpenseCategory { get; }
        public string? OtherCategoryName { get; }
        public string UserEmail { get; }
        
        private Command(string merchant, string description, decimal amount, ExpenseCategory expenseCategory, 
            string userEmail, string? otherCategoryName) => 
            (Merchant, Description, Amount, ExpenseCategory, UserEmail, OtherCategoryName)
                = (merchant, description, amount, expenseCategory, userEmail, otherCategoryName);
        public static Command Create(string merchant, string description, decimal amount, 
            ExpenseCategory expenseCategory, string userEmail, string? otherCategoryName) =>
                new(merchant, description, amount, expenseCategory, userEmail, otherCategoryName);
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(m => m.Merchant)
                .NotEmpty().WithMessage("Merchant cannot be empty")
                .MaximumLength(100).WithMessage("Merchant cannot be longer than 100 characters");
            RuleFor(m => m.Description)
                .NotEmpty().WithMessage("Description cannot be empty")
                .MaximumLength(255).WithMessage("Description cannot be more than 255 characters");
            RuleFor(m => m.Amount)
                .GreaterThanOrEqualTo(0).WithMessage("Amount must be greater than 0")
                .NotNull().WithMessage("Amount cannot be null");
            RuleFor(m => m.ExpenseCategory)
                .NotEmpty().WithMessage("ExpenseCategory cannot be empty")
                .NotNull().WithMessage("ExpenseCategory cannot be null");
            RuleFor(m => m.OtherCategoryName)
                .NotNull().When(m => m.ExpenseCategory == ExpenseCategory.Others)
                .WithMessage("OtherCategoryName must be added when using 'Other' expense category.");
        }
    }

    internal sealed class Handler(AppDbContext context, IValidator<Command> validator)
        : IRequestHandler<Command, Result<core.Entities.Expense>>
    {
        public async Task<Result<core.Entities.Expense>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<core.Entities.Expense>(
                    new Error("AddExpense.ValidationError", validationResult.ToString()));
            }

            try
            {
                var user = await context.GetCurrentUserByEmail(request.UserEmail, cancellationToken);
                if (user == null)
                {
                    return Result.Failure<core.Entities.Expense>(
                        new Error("AddExpense.UserNotFound", request.UserEmail));
                }
                
                var dt = DateTime.UtcNow;
                var newExpense = new core.Entities.Expense
                {
                    Amount = request.Amount,
                    Category = request.ExpenseCategory,
                    Description = request.Description,
                    DateCreated = dt,
                    DateModified = dt,
                    Id = Guid.NewGuid(),
                    Merchant = request.Merchant,
                    OtherCategoryName = request.OtherCategoryName,
                    UserId = user.Id
                };
                await context.Expenses.AddAsync(newExpense, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                
                return Result.Success(newExpense);
            }
            catch (Exception e)
            {
                return Result.Failure<core.Entities.Expense>(
                    new Error("AddExpense.Exception", e.Message));
            }
        }
    }
}

public class AddExpenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/expenses", async (AddExpenseRequest request, ISender sender, HttpContext httpContext) =>
        {
            var userEmail = httpContext.GetUserEmail();
            if (userEmail is null) return Results.Unauthorized();
            var command = AddExpense.Command.Create(request.Merchant, request.Description, request.Amount,
                request.Category, userEmail, request.OtherExpenseCategory);
            var response = await sender.Send(command);
            return !response.IsSuccess ? Results.BadRequest(response.Error) : Results.Ok(response.Value);
        }).RequireAuthorization();
    }
}