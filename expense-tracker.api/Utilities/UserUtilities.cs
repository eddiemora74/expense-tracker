using expense_tracker.api.Database;
using expense_tracker.core.Entities;
using expense_tracker.core.Primitives;
using Microsoft.EntityFrameworkCore;

namespace expense_tracker.api.Utilities;

public static class UserUtilities
{
    public static async Task<User?> GetCurrentUserByEmail(this AppDbContext context, string email, CancellationToken cancellationToken)
    {
        return await context.Users.FirstOrDefaultAsync(u 
            => u.Email == email, cancellationToken);
    }
}