using System.Security.Claims;
using Carter;
using expense_tracker.api.Database;
using expense_tracker.core.Enumerations;
using expense_tracker.core.Primitives;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace expense_tracker.api.Features.Expense;

public static class GetExpenses
{
    public class Query : IRequest<Result<List<core.Entities.Expense>>>
    {
        public string UserEmail { get; }
        public ExpenseFilter Filter { get; }
        public DateTime? StartDate { get; }
        public DateTime? EndDate { get; }
        
        private Query(string userEmail, ExpenseFilter filter, DateTime? startDate, DateTime? endDate)
            => (UserEmail, Filter, StartDate, EndDate) = (userEmail, filter, startDate, endDate);

        public static Query Create(string userEmail, ExpenseFilter filter, DateTime? startDate, DateTime? endDate)
            => new Query(userEmail, filter, startDate, endDate);
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(u => u.UserEmail)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email");
            RuleFor(x => x.Filter).NotEmpty().WithMessage("You must provide a filter.");
            RuleFor(x => x.StartDate)
                .NotNull()
                .When(x => x.Filter == ExpenseFilter.Custom)
                .WithMessage("StartDate is required when Filter is Custom.");
            RuleFor(x => x.EndDate)
                .NotNull()
                .When(x => x.Filter == ExpenseFilter.Custom)
                .WithMessage("EndDate is required when Filter is Custom.")
                .When(x => x.StartDate > x.EndDate)
                .WithMessage("StartDate cannot be greater than EndDate.");
        }
    }

    internal sealed class Handler(AppDbContext context, IValidator<Query> validator)
        : IRequestHandler<Query, Result<List<core.Entities.Expense>>>
    {
        public async Task<Result<List<core.Entities.Expense>>> Handle(Query request,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<List<core.Entities.Expense>>(
                    new Error("GetExpenses.ValidationError", validationResult.ToString()));
            }

            try
            {
                var user = await context.Users.FirstOrDefaultAsync(u 
                    => u.Email == request.UserEmail, cancellationToken);
                
                if (user == null)
                {
                    return Result.Failure<List<core.Entities.Expense>>(
                        new Error("GetExpenses.UserNotFound", request.UserEmail));
                }
                
                var dt = DateTime.UtcNow;
                var expenseQuery = context.Expenses
                    .Where(e => e.UserId.Equals(user.Id));
                expenseQuery = request.Filter switch
                {
                    ExpenseFilter.Custom => expenseQuery
                        .Where(e => e.DateCreated >= request.StartDate
                                    && e.DateCreated <= request.EndDate),
                    ExpenseFilter.PastWeek => expenseQuery
                        .Where(e => e.DateCreated >= dt.AddDays(-7)),
                    ExpenseFilter.PastMonth => expenseQuery
                        .Where(e => e.DateCreated >= dt.AddMonths(-1)),
                    ExpenseFilter.LastThreeMonths => expenseQuery
                        .Where(e => e.DateCreated >= dt.AddMonths(-1)),
                    _ => expenseQuery
                };

                var expenseList = expenseQuery
                    .OrderByDescending(e => e.DateModified)
                    .ToList();
                
                return Result.Success(expenseList);
            }
            catch (Exception e)
            {
                return Result.Failure<List<core.Entities.Expense>>(
                    new Error("GetExpenses.Exception", e.Message));
            }
        }
    }
}

public class GetExpensesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/expenses",
            async (ExpenseFilter filter, DateTime? startDate, DateTime? endDate, HttpContext httpContext, ISender sender) 
                =>
            {
                var user = httpContext.User;
                if (!(user.Identity?.IsAuthenticated ?? false)) return Results.Unauthorized();
                var userEmail = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userEmail is null) return Results.Unauthorized();
                var command = GetExpenses.Query.Create(userEmail, filter, startDate, endDate);
                var response = await sender.Send(command, httpContext.RequestAborted);
                if (!response.IsSuccess) return Results.BadRequest(response.Error);
                return Results.Ok(response.Value);
            }).RequireAuthorization();
    }
}