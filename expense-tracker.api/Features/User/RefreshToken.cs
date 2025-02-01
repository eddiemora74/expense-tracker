using Carter;
using expense_tracker.api.Database;
using expense_tracker.api.Services;
using expense_tracker.core.Contracts;
using expense_tracker.core.Entities;
using expense_tracker.core.Primitives;
using expense_tracker.core.Utilities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace expense_tracker.api.Features.User;

public static class RefreshToken
{
    public class Command : IRequest<Result<RefreshTokenResponse>>
    {
        public string RefreshToken { get; }
        private Command(string refreshToken) => RefreshToken = refreshToken;
        public static Command Create(string refreshToken) => new(refreshToken);
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required");
        }
    }

    internal sealed class Handler(AppDbContext context, JwtService jwtService, IValidator<Command> validator)
        : IRequestHandler<Command, Result<RefreshTokenResponse>>
    {
        public async Task<Result<RefreshTokenResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<RefreshTokenResponse>(
                    new Error("RefreshToken.ValidationError", validationResult.ToString()));
            }

            try
            {
                var dt = DateTime.UtcNow;
                var refreshToken = await context.RefreshTokens
                    .FirstOrDefaultAsync(t =>
                        t.Token.Equals(request.RefreshToken)
                        && t.DateExpires > dt
                        && !t.DateRevoked.HasValue, cancellationToken);

                if (refreshToken == null)
                {
                    return Result.Failure<RefreshTokenResponse>(
                        new Error("RefreshToken.BadRequest", "Refresh token is invalid."));
                }

                var user = await context.Users.FirstOrDefaultAsync(u =>
                    u.Id == refreshToken.UserId, cancellationToken);
                if (user == null)
                {
                    return Result.Failure<RefreshTokenResponse>(
                        new Error("RefreshToken.BadRequest", "Refresh token is invalid."));
                }
                var newRefreshToken = new UserRefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = PasswordUtilities.GenerateSalt(36),
                    DateCreated = dt,
                    DateExpires = dt.AddDays(30),
                    UserId = refreshToken.UserId,
                };
                
                await context.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
                await context.RefreshTokens
                    .Where(t => t.Id.Equals(refreshToken.Id))
                    .ExecuteUpdateAsync(c => 
                        c.SetProperty(p => p.DateRevoked, dt), cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                var refreshTokenResponse = new RefreshTokenResponse
                {
                    AccessToken = jwtService.GenerateToken(user.Email, TimeSpan.FromMinutes(20)),
                    RefreshToken = newRefreshToken.Token,
                };
                return Result.Success(refreshTokenResponse);
            }
            catch (Exception e)
            {
                return Result.Failure<RefreshTokenResponse>(
                    new Error("RefreshToken.Exception", e.Message));
            }
        }
    }
}

public class RefreshTokenEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/authenticate/refresh", async (RefreshTokenRequest request, ISender sender) =>
        {
            var command = RefreshToken.Command.Create(request.RefreshToken);
            var response = await sender.Send(command);
            if (!response.IsSuccess) return Results.BadRequest(response.Error);
            return Results.Ok(response.Value);
        });
    }
}