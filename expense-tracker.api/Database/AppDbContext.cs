using expense_tracker.core.Entities;
using Microsoft.EntityFrameworkCore;

namespace expense_tracker.api.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>()
            .ToTable("users");
        modelBuilder.Entity<User>()
            .Property(u => u.Id).HasColumnName("id");
        modelBuilder.Entity<User>()
            .Property(u => u.FirstName).HasColumnName("first_name");
        modelBuilder.Entity<User>()
            .Property(u => u.LastName).HasColumnName("last_name");
        modelBuilder.Entity<User>()
            .Property(u => u.Email).HasColumnName("email");
        modelBuilder.Entity<User>()
            .Property(u => u.Password).HasColumnName("password");
        modelBuilder.Entity<User>()
            .Property(u => u.PasswordSalt).HasColumnName("password_salt");
        modelBuilder.Entity<User>()
            .Property(u => u.DateCreated).HasColumnName("date_created")
            .HasColumnType("timestamptz");
        modelBuilder.Entity<User>()
            .Property(u => u.DateModified).HasColumnName("date_modified")
            .HasColumnType("timestamptz");
        modelBuilder.Entity<User>()
            .Property(u => u.IsActive).HasColumnName("is_active");
        
        // User Refresh Tokens
        modelBuilder.Entity<UserRefreshToken>()
            .ToTable("user_refresh_tokens");
        modelBuilder.Entity<UserRefreshToken>()
            .Property(u => u.Id).HasColumnName("id");
        modelBuilder.Entity<UserRefreshToken>()
            .Property(u => u.Token).HasColumnName("token");
        modelBuilder.Entity<UserRefreshToken>()
            .Property(u => u.DateCreated).HasColumnName("date_created")
            .HasColumnType("timestamptz");
        modelBuilder.Entity<UserRefreshToken>()
            .Property(u => u.DateExpires).HasColumnName("date_expires")
            .HasColumnType("timestamptz");
        modelBuilder.Entity<UserRefreshToken>()
            .Property(u => u.DateRevoked).HasColumnName("date_revoked")
            .HasColumnType("timestamptz");
        modelBuilder.Entity<UserRefreshToken>()
            .Property(u => u.UserId).HasColumnName("user_id_fk");
        
        // Expenses
        modelBuilder.Entity<Expense>()
            .ToTable("expenses");
        modelBuilder.Entity<Expense>()
            .Property(u => u.Id).HasColumnName("id");
        modelBuilder.Entity<Expense>()
            .Property(u => u.Merchant).HasColumnName("merchant");
        modelBuilder.Entity<Expense>()
            .Property(u => u.Description).HasColumnName("description");
        modelBuilder.Entity<Expense>()
            .Property(u => u.Amount).HasColumnName("amount");
        modelBuilder.Entity<Expense>()
            .Property(u => u.Category).HasColumnName("category");
        modelBuilder.Entity<Expense>()
            .Property(u => u.OtherCategoryName).HasColumnName("other_category_name");
        modelBuilder.Entity<Expense>()
            .Property(u => u.DateCreated).HasColumnName("date_created")
            .HasColumnType("timestamptz");
        modelBuilder.Entity<Expense>()
            .Property(u => u.DateModified).HasColumnName("date_modified")
            .HasColumnType("timestamptz");
        modelBuilder.Entity<Expense>()
            .Property(u => u.UserId).HasColumnName("user_id_fk");
        
        // Relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .HasPrincipalKey(u => u.Id);
        modelBuilder.Entity<User>()
            .HasMany(u => u.Expenses)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .HasPrincipalKey(u => u.Id);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserRefreshToken> RefreshTokens { get; set; }
    public DbSet<Expense> Expenses { get; set; }
}