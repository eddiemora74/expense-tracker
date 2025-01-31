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

public static class UpdateExpense
{
    public class Command : IRequest<Result<core.Entities.Expense>>
    {
        public Guid Id { get; }
        public string UserEmail { get; }
        public string? Merchant { get; }
        public string? Description { get; }
        public decimal? Amount { get; }
        public ExpenseCategory? Category { get; }
        public string? OtherCategoryName { get; }
        
        private Command(Guid id, string userEmail, string? merchant, string? description, decimal? amount, 
            ExpenseCategory? category, string? otherExpenseCategory) =>
            (Id, UserEmail, Merchant, Description, Amount, Category, Category, OtherCategoryName) =
            (id, userEmail, merchant, description, amount, Category, category, otherExpenseCategory);
        
        public static Command Create(Guid id, string userEmail, string? merchant, string? description, decimal? amount,
            ExpenseCategory? category, string? otherExpenseCategory) =>
                new(id, userEmail, merchant, description, amount, category, otherExpenseCategory);
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required");
            RuleFor(x => x.UserEmail)
                .NotEmpty().WithMessage("Email cannot be empty.")
                .EmailAddress().WithMessage("Invalid email address.");
            RuleFor(u => u.Merchant)
                .NotEqual(string.Empty).WithMessage("Merchant cannot be an empty string.");
            RuleFor(u => u.Description)
                .NotEqual(string.Empty).WithMessage("Description cannot be an empty string.");
            RuleFor(u => u.Amount)
                .GreaterThanOrEqualTo(0).WithMessage("Amount cannot be a negative number.");
            RuleFor(u => u.OtherCategoryName)
                .NotEmpty().When(u => u.Category == ExpenseCategory.Others)
                .WithMessage("OtherExpenseCategory cannot be empty or null when category is 'Others'.");
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
                    new Error("UpdateExpense.ValidationError", validationResult.ToString()));
            }
            
            try
            {
                var user = await context.GetCurrentUserByEmail(request.UserEmail, cancellationToken);
                if (user == null)
                {
                    return Result.Failure<core.Entities.Expense>(
                        new Error("UpdateExpense.UserNotFound", request.UserEmail));
                }

                var expenseToUpdate = await context.Expenses
                    .Where(e => e.UserId == user.Id && e.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (expenseToUpdate is null)
                {
                    return Result.Failure<core.Entities.Expense>(
                        new Error("UpdateExpense.ExpenseNotFound", request.UserEmail));
                }
                
                expenseToUpdate.Merchant = request.Merchant ?? expenseToUpdate.Merchant;
                expenseToUpdate.Description = request.Description ?? expenseToUpdate.Description;
                expenseToUpdate.Amount = request.Amount ?? expenseToUpdate.Amount;
                expenseToUpdate.Category = request.Category ?? expenseToUpdate.Category;
                expenseToUpdate.OtherCategoryName = request.Category.Equals(ExpenseCategory.Others) ? 
                    request.OtherCategoryName : null;
                expenseToUpdate.DateModified = DateTime.UtcNow;
                
                await context.Expenses
                    .Where(e => e.Id == request.Id)
                    .ExecuteUpdateAsync(tu =>
                            tu.SetProperty(v => v.Merchant, expenseToUpdate.Merchant)
                                .SetProperty(v => v.Description, expenseToUpdate.Description)
                                .SetProperty(v => v.Amount, expenseToUpdate.Amount)
                                .SetProperty(v => v.Category, expenseToUpdate.Category)
                                .SetProperty(v => v.OtherCategoryName, expenseToUpdate.OtherCategoryName)
                                .SetProperty(v => v.DateModified, expenseToUpdate.DateModified),
                        cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                return Result.Success(expenseToUpdate);
            }
            catch (Exception e)
            {
                return Result.Failure<core.Entities.Expense>(
                    new Error("UpdateExpense.Exception", e.Message));
            }
        }
    }
}

public class UpdateExpenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/expenses/{id}",
            async (Guid id, UpdateExpenseRequest request, ISender sender, HttpContext httpContext) =>
            {
                var userEmail = httpContext.GetUserEmail();
                if (userEmail is null) return Results.Unauthorized();
                var command = UpdateExpense.Command.Create(id, userEmail, request.Merchant, request.Description,
                    request.Amount, request.Category, request.OtherCategoryName);
                var response = await sender.Send(command);
                return !response.IsSuccess ? Results.BadRequest(response.Error) : Results.Ok(response.Value);
            }).RequireAuthorization();
    }
}