using Microsoft.EntityFrameworkCore;

namespace SolidGround;

static class RetentionCleanup
{
    public static async Task DeleteExpiredInputs(AppDbContext db, Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational())
            return;

        var cutoff = DateTime.UtcNow.AddDays(-tenant.RetentionDays);

        var deletedInputs = await db.Inputs
            .Where(i => i.CreationTime < cutoff && !i.Tags.Any(t => t.PreventAutoDelete))
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedInputs == 0)
            return;

        await db.Executions
            .Where(e => !e.Outputs.Any())
            .ExecuteDeleteAsync(cancellationToken);
    }
}
