using Microsoft.EntityFrameworkCore;
using TMH.Shared.Enums;
using TMH.Shared.Models;

namespace TMH.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<WorkSchedule> WorkSchedules { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── USERS ──────────────────────────────────────────
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.Username).IsUnique();
                e.HasIndex(u => u.Email).IsUnique();
                e.Ignore(u => u.FullName);
                e.Property(u => u.Role).HasConversion<int>();
                e.Property(u => u.HoTenDem).HasMaxLength(100).IsRequired();
                e.Property(u => u.Ten).HasMaxLength(50).IsRequired();
                e.Property(u => u.Username).HasMaxLength(50).IsRequired();
                e.Property(u => u.Email).HasMaxLength(150).IsRequired();
                e.Property(u => u.PasswordHash).IsRequired();
            });

            // ── PATIENTS ───────────────────────────────────────
            modelBuilder.Entity<Patient>(e =>
            {
                e.ToTable("Patients");
                e.HasKey(p => p.Id);
                e.HasIndex(p => p.RecordCode).IsUnique();
                e.Property(p => p.FullName).HasMaxLength(150).IsRequired();
                e.Property(p => p.Gender).HasMaxLength(10).IsRequired();
                e.Property(p => p.RecordCode).HasMaxLength(20).IsRequired();
                e.Property(p => p.MedicalHistory).HasMaxLength(1000);
                e.HasOne(p => p.User)
                 .WithMany()
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── DOCTORS ────────────────────────────────────────
            modelBuilder.Entity<Doctor>(e =>
            {
                e.ToTable("Doctors");
                e.HasKey(d => d.Id);
                e.HasIndex(d => d.UserId).IsUnique();
                e.Property(d => d.FullName).HasMaxLength(150).IsRequired();
                e.Property(d => d.Specialty).HasMaxLength(100).IsRequired();
                e.Property(d => d.Degree).HasMaxLength(50);
                e.Property(d => d.Description).HasMaxLength(500);
                e.HasOne(d => d.User)
                 .WithMany()
                 .HasForeignKey(d => d.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── WORK_SCHEDULES ─────────────────────────────────
            modelBuilder.Entity<WorkSchedule>(e =>
            {
                e.ToTable("WorkSchedules");
                e.HasKey(w => w.Id);
                e.Property(w => w.MaxPatients).HasDefaultValue(10);
                e.Property(w => w.CurrentPatients).HasDefaultValue(0);
                e.HasOne(w => w.Doctor)
                 .WithMany(d => d.WorkSchedules)
                 .HasForeignKey(w => w.DoctorId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── APPOINTMENTS ───────────────────────────────────
            modelBuilder.Entity<Appointment>(e =>
            {
                e.ToTable("Appointments");
                e.HasKey(a => a.Id);
                e.HasIndex(a => a.BookingCode).IsUnique();
                e.Property(a => a.BookingCode).HasMaxLength(20).IsRequired();
                e.Property(a => a.Status).HasConversion<int>();
                e.Property(a => a.Note).HasMaxLength(500);
                e.Property(a => a.Diagnosis).HasMaxLength(1000);
                e.HasOne(a => a.Patient)
                 .WithMany(p => p.Appointments)
                 .HasForeignKey(a => a.PatientId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Doctor)
                 .WithMany(d => d.Appointments)
                 .HasForeignKey(a => a.DoctorId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Schedule)
                 .WithMany(w => w.Appointments)
                 .HasForeignKey(a => a.ScheduleId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── NOTIFICATIONS ──────────────────────────────────
            modelBuilder.Entity<Notification>(e =>
            {
                e.ToTable("Notifications");
                e.HasKey(n => n.Id);
                e.Property(n => n.Title).HasMaxLength(150).IsRequired();
                e.Property(n => n.Content).IsRequired();
                e.Property(n => n.Type).HasConversion<int>();
                e.HasOne(n => n.User)
                 .WithMany()
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(n => n.Appointment)
                 .WithMany(a => a.Notifications)
                 .HasForeignKey(n => n.AppointmentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ══════════════════════════════════════════════════
            // SEED DATA — mật khẩu chung: TMH@123456
            // ══════════════════════════════════════════════════
            var pw = "$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi";
            var d0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, HoTenDem = "Nguyễn Văn", Ten = "Admin", Username = "admin", Email = "admin@pktatmuihong.vn", Phone = "0901000001", PasswordHash = pw, Role = UserRole.Admin, IsActive = true, IsEmailVerified = true, CreatedAt = d0 },
                new User { Id = 2, HoTenDem = "TS.BS. Nguyễn Thanh", Ten = "Vân", Username = "bsvan", Email = "bsvan@pktatmuihong.vn", Phone = "0901000002", PasswordHash = pw, Role = UserRole.Doctor, IsActive = true, IsEmailVerified = true, CreatedAt = d0 },
                new User { Id = 3, HoTenDem = "PGS.BS. Trần Minh", Ten = "Linh", Username = "bslinh", Email = "bslinh@pktatmuihong.vn", Phone = "0901000003", PasswordHash = pw, Role = UserRole.Doctor, IsActive = true, IsEmailVerified = true, CreatedAt = d0 },
                new User { Id = 4, HoTenDem = "TS.BS. Phạm Thị", Ten = "Hương", Username = "bshuong", Email = "bshuong@pktatmuihong.vn", Phone = "0901000004", PasswordHash = pw, Role = UserRole.Doctor, IsActive = true, IsEmailVerified = true, CreatedAt = d0 },
                new User { Id = 5, HoTenDem = "Trần Thị", Ten = "Mai", Username = "letanmai", Email = "letanmai@pktatmuihong.vn", Phone = "0901000005", PasswordHash = pw, Role = UserRole.Staff, IsActive = true, IsEmailVerified = true, CreatedAt = d0 },
                new User { Id = 6, HoTenDem = "Lê Văn", Ten = "An", Username = "benhnhan01", Email = "levan.an@gmail.com", Phone = "0901000006", PasswordHash = pw, Role = UserRole.Patient, IsActive = true, IsEmailVerified = false, CreatedAt = d0 },
                new User { Id = 7, HoTenDem = "Nguyễn Thị", Ten = "Bình", Username = "benhnhan02", Email = "nguyen.binh@gmail.com", Phone = "0901000007", PasswordHash = pw, Role = UserRole.Patient, IsActive = true, IsEmailVerified = true, CreatedAt = d0 },
                new User { Id = 8, HoTenDem = "Phạm Văn", Ten = "Cường", Username = "benhnhan03", Email = "pham.cuong@gmail.com", Phone = "0901000008", PasswordHash = pw, Role = UserRole.Patient, IsActive = true, IsEmailVerified = true, CreatedAt = d0 },
                new User { Id = 9, HoTenDem = "Hoàng Thị", Ten = "Dung", Username = "benhnhan04", Email = "hoang.dung@gmail.com", Phone = "0901000009", PasswordHash = pw, Role = UserRole.Patient, IsActive = true, IsEmailVerified = false, CreatedAt = d0 }
            );

            modelBuilder.Entity<Patient>().HasData(
                new Patient { Id = 1, UserId = 6, FullName = "Lê Văn An", DateOfBirth = new DateTime(1990, 5, 15), Gender = "Nam", RecordCode = "BN-2026-0001", MedicalHistory = "Viêm mũi dị ứng mãn tính", CreatedAt = d0 },
                new Patient { Id = 2, UserId = 7, FullName = "Nguyễn Thị Bình", DateOfBirth = new DateTime(1985, 8, 22), Gender = "Nữ", RecordCode = "BN-2026-0002", MedicalHistory = "Viêm amidan tái phát", CreatedAt = d0 },
                new Patient { Id = 3, UserId = 8, FullName = "Phạm Văn Cường", DateOfBirth = new DateTime(2000, 3, 10), Gender = "Nam", RecordCode = "BN-2026-0003", MedicalHistory = "Không có tiền sử bệnh", CreatedAt = d0 },
                new Patient { Id = 4, UserId = 9, FullName = "Hoàng Thị Dung", DateOfBirth = new DateTime(1978, 11, 30), Gender = "Nữ", RecordCode = "BN-2026-0004", MedicalHistory = "Polyp mũi, đã phẫu thuật năm 2020", CreatedAt = d0 },
                new Patient { Id = 5, UserId = 7, FullName = "Nguyễn Minh Khôi", DateOfBirth = new DateTime(2018, 6, 5), Gender = "Nam", RecordCode = "BN-2026-0005", MedicalHistory = "Trẻ em, viêm tai giữa tái phát nhiều lần", CreatedAt = d0 }
            );

            modelBuilder.Entity<Doctor>().HasData(
                new Doctor { Id = 1, UserId = 2, FullName = "TS.BS. Nguyễn Thanh Vân", Specialty = "Tai Mũi Họng — Phẫu thuật nội soi xoang", Degree = "TS", Description = "22 năm kinh nghiệm, BV TMH TW, tốt nghiệp ĐH Tokyo", IsAvailable = true },
                new Doctor { Id = 2, UserId = 3, FullName = "PGS.BS. Trần Minh Linh", Specialty = "Tai Mũi Họng — Thính học & cấy ốc tai", Degree = "PGS", Description = "18 năm kinh nghiệm, chuyên thính học, ĐH Melbourne", IsAvailable = true },
                new Doctor { Id = 3, UserId = 4, FullName = "TS.BS. Phạm Thị Hương", Specialty = "TMH Nhi — Viêm tai giữa trẻ em", Degree = "TS", Description = "14 năm kinh nghiệm tại BV Nhi Đồng 2, ĐH Seoul", IsAvailable = true }
            );

            modelBuilder.Entity<WorkSchedule>().HasData(
                new WorkSchedule { Id = 1, DoctorId = 1, WorkDate = new DateTime(2026, 3, 18), StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(11, 0, 0), MaxPatients = 10, CurrentPatients = 3 },
                new WorkSchedule { Id = 2, DoctorId = 1, WorkDate = new DateTime(2026, 3, 18), StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(17, 0, 0), MaxPatients = 10, CurrentPatients = 1 },
                new WorkSchedule { Id = 3, DoctorId = 2, WorkDate = new DateTime(2026, 3, 18), StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(11, 0, 0), MaxPatients = 8, CurrentPatients = 2 },
                new WorkSchedule { Id = 4, DoctorId = 3, WorkDate = new DateTime(2026, 3, 18), StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(11, 0, 0), MaxPatients = 10, CurrentPatients = 1 },
                new WorkSchedule { Id = 5, DoctorId = 2, WorkDate = new DateTime(2026, 3, 19), StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(11, 0, 0), MaxPatients = 8, CurrentPatients = 0 }
            );

            modelBuilder.Entity<Appointment>().HasData(
                new Appointment { Id = 1, PatientId = 1, DoctorId = 1, ScheduleId = 1, BookingCode = "APT-2026-00001", BookedAt = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc), Status = AppointmentStatus.HoanThanh, Note = "Khám viêm xoang mãn tính", Diagnosis = "Viêm xoang hàm hai bên mãn tính" },
                new Appointment { Id = 2, PatientId = 2, DoctorId = 1, ScheduleId = 1, BookingCode = "APT-2026-00002", BookedAt = new DateTime(2026, 3, 16, 10, 0, 0, DateTimeKind.Utc), Status = AppointmentStatus.DaXacNhan, Note = "Tái khám sau điều trị amidan", Diagnosis = null },
                new Appointment { Id = 3, PatientId = 3, DoctorId = 2, ScheduleId = 3, BookingCode = "APT-2026-00003", BookedAt = new DateTime(2026, 3, 17, 8, 0, 0, DateTimeKind.Utc), Status = AppointmentStatus.DaDen, Note = "Nghe kém tai phải", Diagnosis = null },
                new Appointment { Id = 4, PatientId = 5, DoctorId = 3, ScheduleId = 4, BookingCode = "APT-2026-00004", BookedAt = new DateTime(2026, 3, 17, 14, 0, 0, DateTimeKind.Utc), Status = AppointmentStatus.ChoXacNhan, Note = "Trẻ 7 tuổi, viêm tai giữa tái phát", Diagnosis = null },
                new Appointment { Id = 5, PatientId = 4, DoctorId = 1, ScheduleId = 2, BookingCode = "APT-2026-00005", BookedAt = new DateTime(2026, 3, 18, 7, 30, 0, DateTimeKind.Utc), Status = AppointmentStatus.DaHuy, Note = "Bệnh nhân huỷ do bận việc", Diagnosis = null }
            );

            modelBuilder.Entity<Notification>().HasData(
                new Notification { Id = 1, UserId = 6, AppointmentId = 1, Title = "Đặt lịch thành công", Content = "Lịch khám ngày 18/03 lúc 07:00 với BS. Nguyễn Thanh Vân đã xác nhận. Mã: APT-2026-00001", Type = NotificationType.XacNhanLich, SentAt = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc), IsRead = true },
                new Notification { Id = 2, UserId = 6, AppointmentId = 1, Title = "Nhắc lịch khám ngày mai", Content = "Bạn có lịch khám 07:00 ngày 18/03 với BS. Nguyễn Thanh Vân. Đến trước 15 phút.", Type = NotificationType.NhacLich, SentAt = new DateTime(2026, 3, 17, 8, 0, 0, DateTimeKind.Utc), IsRead = true },
                new Notification { Id = 3, UserId = 7, AppointmentId = 2, Title = "Xác nhận lịch tái khám", Content = "Lịch tái khám ngày 18/03 lúc 07:00 đã được lễ tân xác nhận. Mã: APT-2026-00002", Type = NotificationType.XacNhanLich, SentAt = new DateTime(2026, 3, 16, 10, 5, 0, DateTimeKind.Utc), IsRead = false },
                new Notification { Id = 4, UserId = 7, AppointmentId = 4, Title = "Đặt lịch cho bé thành công", Content = "Lịch khám bé Minh Khôi ngày 18/03 lúc 07:00 với BS. Phạm Thị Hương đang chờ xác nhận.", Type = NotificationType.XacNhanLich, SentAt = new DateTime(2026, 3, 17, 14, 5, 0, DateTimeKind.Utc), IsRead = false },
                new Notification { Id = 5, UserId = 9, AppointmentId = 5, Title = "Lịch khám đã bị huỷ", Content = "Lịch khám ngày 18/03 lúc 13:00 với BS. Nguyễn Thanh Vân đã được huỷ theo yêu cầu.", Type = NotificationType.HuyLich, SentAt = new DateTime(2026, 3, 18, 7, 0, 0, DateTimeKind.Utc), IsRead = false }
            );
        }
    }
}