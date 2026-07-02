using Microsoft.EntityFrameworkCore;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Data
{
    public class ChurchDbContext : DbContext
    {
        public ChurchDbContext(DbContextOptions<ChurchDbContext> options) : base(options)
        {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<PrayerRequest> PrayerRequests { get; set; }
        public DbSet<ChurchEvent> ChurchEvents { get; set; }
        public DbSet<Sermon> Sermons { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EventRSVP> EventRSVPs { get; set; }
        public DbSet<EventVolunteer> EventVolunteers { get; set; }
        public DbSet<SermonNoteEntity> SermonNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Member
            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(m => m.Email);
                entity.Property(m => m.Email).HasMaxLength(150);
                entity.Property(m => m.Name).IsRequired().HasMaxLength(100);
                entity.Property(m => m.Password).IsRequired().HasMaxLength(100);
                entity.Property(m => m.Phone).HasMaxLength(30);
                entity.Property(m => m.Address).HasMaxLength(250);
                entity.Property(m => m.FamilyMembers).HasMaxLength(150);
                entity.Property(m => m.HowHeard).HasMaxLength(100);
                
                // Convert List<string> to string for database compatibility
                entity.Property(m => m.Interests)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );
            });

            // 2. Post
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.Category);
                entity.HasIndex(p => p.PublishedDate);

                entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Content).IsRequired().HasColumnType("text");
                entity.Property(p => p.ImageUrl).HasMaxLength(500);
                entity.Property(p => p.Category).HasMaxLength(50);
                entity.Property(p => p.Author).HasMaxLength(100);

                entity.Property(p => p.Likes)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );

                entity.HasMany(p => p.Comments)
                    .WithOne()
                    .HasForeignKey("PostId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 3. Comment
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.Timestamp);
                entity.HasIndex("PostId");
                entity.HasIndex("PrayerRequestId");

                entity.Property(c => c.AuthorName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Content).IsRequired().HasColumnType("text");
            });

            // 4. GalleryImage
            modelBuilder.Entity<GalleryImage>(entity =>
            {
                entity.HasKey(g => g.Id);
                entity.HasIndex(g => g.Album);
                entity.HasIndex(g => g.IsApproved);
                entity.HasIndex(g => g.UploadedDate);

                entity.Property(g => g.ImageUrl).IsRequired().HasMaxLength(500);
                entity.Property(g => g.Caption).HasMaxLength(250);
                entity.Property(g => g.Album).HasMaxLength(50);
                entity.Property(g => g.UploadedBy).HasMaxLength(150);
            });

            // 5. PrayerRequest
            modelBuilder.Entity<PrayerRequest>(entity =>
            {
                entity.HasKey(pr => pr.Id);
                entity.HasIndex(pr => pr.CreatedDate);

                entity.Property(pr => pr.Title).IsRequired().HasMaxLength(200);
                entity.Property(pr => pr.Details).IsRequired().HasColumnType("text");
                entity.Property(pr => pr.AuthorName).HasMaxLength(100);
                entity.Property(pr => pr.MobileNumber).HasMaxLength(30);

                entity.Property(pr => pr.PrayingUsers)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );

                entity.HasMany(pr => pr.Encouragements)
                    .WithOne()
                    .HasForeignKey("PrayerRequestId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 6. ChurchEvent
            modelBuilder.Entity<ChurchEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DateText);

                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.DateText).HasMaxLength(100);
                
                // Exclude the dictionaries from direct EF Core mapping (we pop/persist them manually or via entity tables)
                entity.Ignore(e => e.RSVPs);
                entity.Ignore(e => e.Volunteers);
            });

            // 7. EventRSVP
            modelBuilder.Entity<EventRSVP>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasIndex(r => r.EventId);
                entity.HasIndex(r => r.Email);
                entity.Property(r => r.Email).IsRequired().HasMaxLength(150);
                entity.Property(r => r.Status).IsRequired().HasMaxLength(30);
            });

            // 8. EventVolunteer
            modelBuilder.Entity<EventVolunteer>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.HasIndex(v => v.EventId);
                entity.HasIndex(v => v.Email);
                entity.Property(v => v.Email).IsRequired().HasMaxLength(150);
                entity.Property(v => v.Role).IsRequired().HasMaxLength(100);
            });

            // 9. Sermon
            modelBuilder.Entity<Sermon>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.Speaker);

                entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
                entity.Property(s => s.Speaker).HasMaxLength(100);
                entity.Property(s => s.DateText).HasMaxLength(50);
                entity.Property(s => s.AudioUrl).HasMaxLength(500);
                entity.Property(s => s.DurationText).HasMaxLength(30);
            });

            // 10. SermonNoteEntity
            modelBuilder.Entity<SermonNoteEntity>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.HasIndex(n => n.SermonId);
                entity.HasIndex(n => n.UserEmail);
                entity.Property(n => n.UserEmail).IsRequired().HasMaxLength(150);
                entity.Property(n => n.NoteText).IsRequired().HasColumnType("text");
            });

            // 11. Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.HasIndex(n => n.Timestamp);
                entity.Property(n => n.Title).IsRequired().HasMaxLength(150);
                entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
            });
        }
    }

    public class EventRSVP
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Yes, No, Maybe
    }

    public class EventVolunteer
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class SermonNoteEntity
    {
        public int Id { get; set; }
        public int SermonId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime UpdatedDate { get; set; }
    }
}
