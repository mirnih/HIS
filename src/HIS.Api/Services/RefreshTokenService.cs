using HIS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HIS.Api.Services;

public class RefreshTokenService
{
    private readonly HisDbContext _db;

    public RefreshTokenService(HisDbContext db)
    {
        _db = db;
    }

    public async Task<int> RevokeAllAsync(Guid accountId)
    {
        var tokens = await _db.RefreshTokens
            .Where(x =>
                x.AccountId == accountId &&
                x.RevokedAt == null &&
                x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var revokedAt = DateTime.UtcNow;

        foreach (var token in tokens)
        {
            token.RevokedAt = revokedAt;
        }

        return tokens.Count;
    }
}
