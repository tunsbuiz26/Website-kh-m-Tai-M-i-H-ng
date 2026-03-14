using Microsoft.EntityFrameworkCore;
using TMH.Shared.Models;

namespace TMH.API.Data
{
    /// <summary>
    /// DbContext là "cầu nối" giữa code C# và SQL Server.
    /// EF Core sẽ dùng class này để sinh migration và thực thi truy vấn.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                // Username và Email phải là duy nhất trong hệ thống
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();

                // Bỏ qua FullName vì là computed property, không cần cột DB
                entity.Ignore(u => u.FullName);

                // Lưu enum Role dưới dạng int (1-4) vào cột Role
                entity.Property(u => u.Role).HasConversion<int>();

                entity.Property(u => u.HoTenDem).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Ten).HasMaxLength(50).IsRequired();
                entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
                entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
            });
        }
    }
}
