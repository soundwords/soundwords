using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SoundWords.Auth;

internal sealed class AuthDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<AuthDbContext> options = new();
        options.UseNpgsql("Host=localhost;Database=soundwords_design;Username=postgres;Password=postgres");
        return new AuthDbContext(options.Options);
    }
}
