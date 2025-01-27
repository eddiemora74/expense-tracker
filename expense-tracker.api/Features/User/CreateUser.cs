using Carter;
using expense_tracker.api.Database;
using expense_tracker.core.Contracts;
using expense_tracker.core.Primitives;
using expense_tracker.core.Utilities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace expense_tracker.api.Features.User;

public static class CreateUser
{
    public class Command : IRequest<Result<core.Entities.User>>
    {
        public string FirstName { get; }
        public string LastName { get; }
        public string Email { get; }
        public string Password { get; }

        private Command(string firstName, string lastName, string email, string password)
            => (FirstName, LastName, Email, Password) = (firstName, lastName, email, password);
        
        public static Command Create(string firstName, string lastName, string email, string password) 
            => new(firstName, lastName, email, password);
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name cannot be empty");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name cannot be empty");
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email cannot be empty");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password cannot be empty")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .MaximumLength(36).WithMessage("Password must be between 8 and 36 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"\d").WithMessage("Password must contain at least one number.")
                .Matches(@"[\W_]").WithMessage("Password must contain at least one symbol.")
                .Matches(@"^\S+$").WithMessage("Password must not contain any whitespace.")
                .WithMessage("Password must meet complexity requirements.");
        }
    }

    internal sealed class Handler(AppDbContext context, IValidator<Command> validator)
        : IRequestHandler<Command, Result<core.Entities.User>>
    {
        public async Task<Result<core.Entities.User>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<core.Entities.User>(
                    new Error("CreateUser.ValidationError", validationResult.ToString()));
            }
            
            var userExists = await context.Users.AnyAsync(
                u 
                    => u.Email.Equals(request.Email)
                    && u.IsActive, cancellationToken);
            if (userExists)
            {
                return Result.Failure<core.Entities.User>(
                    new Error("CreateUser.BadRequest", $"Email {request.Email} already exists"));
            }
            
            var dt = DateTime.UtcNow;
            var salt = PasswordUtilities.GenerateSalt();
            var hashedPassword = PasswordUtilities.HashPassword(request.Password, salt);
            var newUser = new core.Entities.User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Password = hashedPassword,
                PasswordSalt = salt,
                DateCreated = dt,
                DateModified = dt,
                IsActive = true
            };

            try
            {
                await context.Users.AddAsync(newUser, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                return Result.Success(newUser);
            }
            catch (Exception e)
            {
                return Result.Failure<core.Entities.User>(
                    new Error("CreateUser.Exception", e.Message));
            }
        }
    }
}

public class CreateUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users", async (CreateUserRequest request, ISender sender) =>
        {
            var command =
                CreateUser.Command.Create(request.FirstName, request.LastName, request.Email, request.Password);
            var response = await sender.Send(command);
            if (!response.IsSuccess) return Results.BadRequest(response.Error);
            return Results.Ok(response.Value);
        });
    }
}