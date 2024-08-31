using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using TLC.LooseRoles.Database.Domain;

namespace TLC.LooseRoles.Database
{
    public interface IDbContext
    {
        // DbSet<Event> Events { get; set; }
        // DbSet<Guild> Guilds { get; set; }
        DbSet<EmoteRole> Users { get; set; }

        void Migrate();
        void SaveChanges();
        Task SaveChangesAsync();
    }
}