using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TICinema.Identity.Domain.Entities;

namespace TICinema.Identity.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PendingContactChange> PendingContactChanges { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().ToTable("accounts");

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Token).IsRequired();

                
                entity.HasOne(t => t.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(t => t.UserId);
            });
            
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "2", Name = "User", NormalizedName = "USER" }
            );
            
            builder.Entity<PendingContactChange>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => new { e.UserId, e.Type }).IsUnique();
                
                entity.HasOne(d => d.User)
                    .WithMany(p => p.PendingContactChanges)
                    .HasForeignKey(d => d.UserId) 
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
