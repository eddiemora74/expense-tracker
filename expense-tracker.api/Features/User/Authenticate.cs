using Carter;
using expense_tracker.api.Database;
using expense_tracker.api.Services;
using expense_tracker.core.Contracts;
using expense_tracker.core.Entities;
using expense_tracker.core.Primitives;
using expense_tracker.core.Utilities;
using FluentValidation;
using MediatR;

namespace expense_tracker.api.Features.User;

public static class Authenticate
{
    public class Command : IRequest<Result<AuthenticateResponse>>
    {
        public string Email { get; }
        public string Password { get; }
        
        private Command(string email, string password) 
            => (Email, Password) = (email, password);
        
        public static Command Create(string email, string password) => new Command(email, password);
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Username must be a valid email");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }

    internal class Handler(AppDbContext context, JwtService jwtService, IValidator<Command> validator)
        : IRequestHandler<Command, Result<AuthenticateResponse>>
    {
        public async Task<Result<AuthenticateResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<AuthenticateResponse>(
                    new Error("Authenticate.ValidationError", validationResult.ToString()));
            }

            try
            {
                var user = context.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                {
                    return Result.Failure<AuthenticateResponse>(
                        new Error("User.BadRequest", "Login request failed"));
                }

                if (!PasswordUtilities.VerifyPassword(request.Password, user.PasswordSalt, user.Password))
                {
                    return Result.Failure<AuthenticateResponse>(
                        new Error("User.BadRequest", "Login request failed"));
                }
                
                var dt = DateTime.UtcNow;
                var newRefreshToken = new UserRefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = PasswordUtilities.GenerateSalt(36),
                    DateCreated = dt,
                    DateExpires = dt.AddDays(30),
                    UserId = user.Id,
                };
                await context.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                var authResponse = new AuthenticateResponse
                {
                    Expires = dt.AddMinutes(20),
                    RefreshToken = newRefreshToken.Token,
                    UserId = user.Id,
                    AccessToken = jwtService.GenerateToken(user.Email, TimeSpan.FromMinutes(20))
                };
                return Result.Success(authResponse);
            }
            catch (Exception e)
            {
                return Result.Failure<AuthenticateResponse>(
                    new Error("Authenticate.Exception", e.Message));
            }
        }
    }
}

public class AuthenticateEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/authenticate", async (AuthenticateRequest request, ISender sender) =>
        {
            var command = Authenticate.Command.Create(request.Email, request.Password);
            var response = await sender.Send(command);
            if (!response.IsSuccess) return Results.BadRequest(response.Error);
            return Results.Ok(response.Value);
        });
    }
}