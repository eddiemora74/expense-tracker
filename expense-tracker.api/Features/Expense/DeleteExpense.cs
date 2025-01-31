using Carter;
using expense_tracker.api.Database;
using expense_tracker.api.Utilities;
using expense_tracker.core.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace expense_tracker.api.Features.Expense;

public static class DeleteExpense
{
    public class Command : IRequest<Result>
    {
        public Guid Id { get; }
        public string UserEmail { get; }
        private Command(Guid id, string userEmail) => (Id, UserEmail) = (id, userEmail);
        public static Command Create(Guid id, string userEmail) => new Command(id, userEmail);
    }

    internal sealed class Handler(AppDbContext context)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await context.GetCurrentUserByEmail(request.UserEmail, cancellationToken);
                if (user == null)
                {
                    return Result.Failure(
                        new Error("DeleteExpense.UserNotFound", request.UserEmail));
                }
                
                var expenseToDelete = await context.Expenses
                    .Where(e => e.UserId == user.Id && e.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (expenseToDelete is null)
                {
                    return Result.Failure(
                        new Error("DeleteExpense.ExpenseNotFound", request.UserEmail));
                }
                
                context.Expenses.Remove(expenseToDelete);
                await context.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Failure(new Error("DeleteExpense.Exception", e.Message));
            }
        }
    }
}

public class DeleteExpenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/expenses/{id}", async (Guid id, ISender sender, HttpContext httpContext) =>
        {
            var userEmail = httpContext.GetUserEmail();
            if (userEmail is null) return Results.Unauthorized();
            var command = DeleteExpense.Command.Create(id, userEmail);
            var response = await sender.Send(command);
            return !response.IsSuccess ? Results.BadRequest(response.Error) : Results.NoContent();
        });
    }
}