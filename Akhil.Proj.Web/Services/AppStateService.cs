using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Akhil.Proj.Web.Data;

namespace Akhil.Proj.Web.Services
{
    public class AppStateService
    {
        // Active session simulation
        public Member? CurrentUser { get; private set; } = null;
        public string? ActiveRole => CurrentUser?.Email == "admin@example.com" ? "Admin" : (CurrentUser != null ? "User" : null);

        // Core data lists
        public List<Member> Members { get; } = new();
        public List<Post> Posts { get; } = new();
        public List<GalleryImage> GalleryImages { get; } = new();
        public List<PrayerRequest> Prayers { get; } = new();
        public List<ChurchEvent> Events { get; } = new();
        public List<Sermon> Sermons { get; } = new();
        public List<Notification> Notifications { get; } = new();
        public Dictionary<int, string> UserNotes { get; } = new(); // SermonId -> Note text
        public List<ChatMessage> ChatMessages { get; } = new();

        public bool IsLightMode { get; private set; } = false;

        public void ToggleTheme()
        {
            IsLightMode = !IsLightMode;
            NotifyStateChanged();
        }

        // Action events for UI updates (push notifications, dynamic re-rendering)
        public event Action? OnChange;
        public event Action<Notification>? OnNotificationReceived;
        public event Action<string>? OnReactionReceived;

        private readonly IServiceScopeFactory _scopeFactory;

        public AppStateService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            SeedData();
        }

        public void NotifyStateChanged() => OnChange?.Invoke();

        public void LogIn(string email, string password)
        {
            var existing = Members.FirstOrDefault(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (password != "magic" && !string.IsNullOrEmpty(existing.Password) && existing.Password != password)
                {
                    throw new Exception("Invalid email or password.");
                }
                CurrentUser = existing;
            }
            else
            {
                // Create automatically for demo if not exists, except admin
                if (email.Equals("admin@example.com", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentUser = new Member
                    {
                        Name = "Church Admin",
                        Email = "admin@example.com",
                        Password = "admin",
                        Phone = "555-0199",
                        Address = "Church Office, Main St",
                        FamilyMembers = "None",
                        HowHeard = "Staff",
                        Interests = new List<string> { "Missions", "Choir" },
                        JoinedDate = DateTime.Now.AddYears(-2)
                    };
                    
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                        if (!context.Members.Any(m => m.Email == "admin@example.com"))
                        {
                            context.Members.Add(CurrentUser);
                            context.SaveChanges();
                        }
                    }
                    
                    Members.Add(CurrentUser);
                }
                else
                {
                    CurrentUser = new Member
                    {
                        Name = email.Split('@')[0].ToUpper(),
                        Email = email,
                        Password = password,
                        Phone = "555-0100",
                        Address = "Demo Town",
                        FamilyMembers = "1",
                        HowHeard = "Search Engine",
                        Interests = new List<string> { "Youth" },
                        JoinedDate = DateTime.Now
                    };
                    
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                        if (!context.Members.Any(m => m.Email == email))
                        {
                            context.Members.Add(CurrentUser);
                            context.SaveChanges();
                        }
                    }
                    
                    Members.Add(CurrentUser);
                }
            }

            // Load UserNotes from DB
            UserNotes.Clear();
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var notes = context.SermonNotes
                    .Where(n => n.UserEmail.ToLower() == email.ToLower())
                    .ToList();
                foreach (var note in notes)
                {
                    UserNotes[note.SermonId] = note.NoteText;
                }
            }

            NotifyStateChanged();
        }

        public void LogOut()
        {
            CurrentUser = null;
            UserNotes.Clear();
            NotifyStateChanged();
        }

        public void Register(Member member)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                
                if (context.Members.Any(m => m.Email.ToLower() == member.Email.ToLower()))
                {
                    throw new Exception("A member with this email already exists.");
                }
                
                member.JoinedDate = DateTime.Now;
                context.Members.Add(member);
                context.SaveChanges();
                
                Members.Add(member);
            }
            CurrentUser = member;
            NotifyStateChanged();
        }

        public void UpdateProfileInterests(List<string> interests)
        {
            if (CurrentUser != null)
            {
                CurrentUser.Interests = interests;
                
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                    var dbMember = context.Members.FirstOrDefault(m => m.Email == CurrentUser.Email);
                    if (dbMember != null)
                    {
                        dbMember.Interests = interests;
                        context.SaveChanges();
                    }
                }
                
                var memberInList = Members.FirstOrDefault(m => m.Email == CurrentUser.Email);
                if (memberInList != null)
                {
                    memberInList.Interests = interests;
                }
                NotifyStateChanged();
            }
        }

        public void UpdateMemberProfile(string email, string name, string phone, string address, string familyMembers, string howHeard, List<string> interests, string password = "")
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var dbMember = context.Members.FirstOrDefault(m => m.Email.ToLower() == email.ToLower());
                if (dbMember != null)
                {
                    dbMember.Name = name;
                    dbMember.Phone = phone;
                    dbMember.Address = address;
                    dbMember.FamilyMembers = familyMembers;
                    dbMember.HowHeard = howHeard;
                    dbMember.Interests = interests;
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        dbMember.Password = password;
                    }
                    context.SaveChanges();
                }
            }

            var memMember = Members.FirstOrDefault(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (memMember != null)
            {
                memMember.Name = name;
                memMember.Phone = phone;
                memMember.Address = address;
                memMember.FamilyMembers = familyMembers;
                memMember.HowHeard = howHeard;
                memMember.Interests = interests;
                if (!string.IsNullOrWhiteSpace(password))
                {
                    memMember.Password = password;
                }
            }

            if (CurrentUser != null && CurrentUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                CurrentUser.Name = name;
                CurrentUser.Phone = phone;
                CurrentUser.Address = address;
                CurrentUser.FamilyMembers = familyMembers;
                CurrentUser.HowHeard = howHeard;
                CurrentUser.Interests = interests;
                if (!string.IsNullOrWhiteSpace(password))
                {
                    CurrentUser.Password = password;
                }
            }

            NotifyStateChanged();
        }

        public void ResetUserPassword(string email, string newPassword)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var dbMember = context.Members.FirstOrDefault(m => m.Email.ToLower() == email.ToLower());
                if (dbMember == null)
                {
                    throw new Exception("Account not found with this email address.");
                }
                dbMember.Password = newPassword;
                context.SaveChanges();
            }

            var memMember = Members.FirstOrDefault(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (memMember != null)
            {
                memMember.Password = newPassword;
            }

            if (CurrentUser != null && CurrentUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                CurrentUser.Password = newPassword;
            }

            NotifyStateChanged();
        }

        public void DeleteMemberAccount(string email)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var dbMember = context.Members.FirstOrDefault(m => m.Email.ToLower() == email.ToLower());
                if (dbMember != null)
                {
                    // Clean up related event participations
                    var rsvps = context.EventRSVPs.Where(r => r.Email.ToLower() == email.ToLower());
                    context.EventRSVPs.RemoveRange(rsvps);
                    
                    var volunteers = context.EventVolunteers.Where(v => v.Email.ToLower() == email.ToLower());
                    context.EventVolunteers.RemoveRange(volunteers);
                    
                    var notes = context.SermonNotes.Where(n => n.UserEmail.ToLower() == email.ToLower());
                    context.SermonNotes.RemoveRange(notes);

                    context.Members.Remove(dbMember);
                    context.SaveChanges();
                }
            }

            var memMember = Members.FirstOrDefault(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (memMember != null)
            {
                Members.Remove(memMember);
            }

            if (CurrentUser != null && CurrentUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                CurrentUser = null;
                UserNotes.Clear();
            }

            NotifyStateChanged();
        }

        // Post Operations
        public void CreatePost(string title, string content, string imageUrl, string category, string author)
        {
            var newPost = new Post
            {
                Title = title,
                Content = content,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? $"https://picsum.photos/800/400?random={Posts.Count + 1}" : imageUrl,
                Category = category,
                Author = author,
                PublishedDate = DateTime.Now
            };

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                context.Posts.Add(newPost);
                context.SaveChanges();
                Posts.Insert(0, newPost);
            }
            NotifyStateChanged();
        }

        public void UpdatePost(int id, string title, string content, string imageUrl, string category, string author)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var post = context.Posts.FirstOrDefault(p => p.Id == id);
                if (post != null)
                {
                    post.Title = title;
                    post.Content = content;
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        post.ImageUrl = imageUrl;
                    }
                    post.Category = category;
                    post.Author = author;
                    context.SaveChanges();

                    var memPost = Posts.FirstOrDefault(p => p.Id == id);
                    if (memPost != null)
                    {
                        memPost.Title = title;
                        memPost.Content = content;
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                        {
                            memPost.ImageUrl = imageUrl;
                        }
                        memPost.Category = category;
                        memPost.Author = author;
                    }
                }
            }
            NotifyStateChanged();
        }

        public void DeletePost(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var post = context.Posts.Include(p => p.Comments).FirstOrDefault(p => p.Id == id);
                if (post != null)
                {
                    context.Posts.Remove(post);
                    context.SaveChanges();
                }
            }
            var memPost = Posts.FirstOrDefault(p => p.Id == id);
            if (memPost != null)
            {
                Posts.Remove(memPost);
            }
            NotifyStateChanged();
        }


        public void ToggleLikePost(int postId, string userEmail)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var post = context.Posts.FirstOrDefault(p => p.Id == postId);
                if (post != null)
                {
                    if (post.Likes.Contains(userEmail))
                    {
                        post.Likes.Remove(userEmail);
                    }
                    else
                    {
                        post.Likes.Add(userEmail);
                    }
                    context.SaveChanges();

                    var memPost = Posts.FirstOrDefault(p => p.Id == postId);
                    if (memPost != null)
                    {
                        memPost.Likes = post.Likes;
                    }
                }
            }
            NotifyStateChanged();
        }

        public void AddCommentToPost(int postId, string authorName, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var comment = new Comment
            {
                AuthorName = authorName,
                Content = content,
                Timestamp = DateTime.Now
            };

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var post = context.Posts.Include(p => p.Comments).FirstOrDefault(p => p.Id == postId);
                if (post != null)
                {
                    post.Comments.Add(comment);
                    context.SaveChanges();

                    var memPost = Posts.FirstOrDefault(p => p.Id == postId);
                    if (memPost != null)
                    {
                        memPost.Comments.Add(comment);
                    }
                }
            }
            NotifyStateChanged();
        }

        // Gallery Operations
        public void UploadGalleryImage(string caption, string album, string imageUrl, string uploadedBy, bool autoApprove = false)
        {
            var newImg = new GalleryImage
            {
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? $"https://picsum.photos/600/400?random={GalleryImages.Count + 10}" : imageUrl,
                Caption = caption,
                Album = album,
                UploadedBy = uploadedBy,
                IsApproved = autoApprove || uploadedBy == "admin@example.com",
                UploadedDate = DateTime.Now
            };

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                context.GalleryImages.Add(newImg);
                context.SaveChanges();
                GalleryImages.Add(newImg);
            }
            NotifyStateChanged();
        }

        public void ApproveGalleryImage(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var img = context.GalleryImages.FirstOrDefault(g => g.Id == id);
                if (img != null)
                {
                    img.IsApproved = true;
                    context.SaveChanges();

                    var memImg = GalleryImages.FirstOrDefault(g => g.Id == id);
                    if (memImg != null)
                    {
                        memImg.IsApproved = true;
                    }
                }
            }
            NotifyStateChanged();
        }

        public void RejectGalleryImage(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var img = context.GalleryImages.FirstOrDefault(g => g.Id == id);
                if (img != null)
                {
                    context.GalleryImages.Remove(img);
                    context.SaveChanges();

                    var memImg = GalleryImages.FirstOrDefault(g => g.Id == id);
                    if (memImg != null)
                    {
                        GalleryImages.Remove(memImg);
                    }
                }
            }
            NotifyStateChanged();
        }

        public void AddPrayerRequest(string title, string details, bool isAnonymous, string authorName, string mobileNumber)
        {
            var newPrayer = new PrayerRequest
            {
                Title = title,
                Details = details,
                IsAnonymous = isAnonymous,
                AuthorName = authorName,
                MobileNumber = mobileNumber,
                CreatedDate = DateTime.Now
            };

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                context.PrayerRequests.Add(newPrayer);
                context.SaveChanges();
                Prayers.Insert(0, newPrayer);
            }
            NotifyStateChanged();
        }

        public void DeletePrayerRequest(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var prayer = context.PrayerRequests.FirstOrDefault(p => p.Id == id);
                if (prayer != null)
                {
                    context.PrayerRequests.Remove(prayer);
                    context.SaveChanges();
                }
            }
            var memPrayer = Prayers.FirstOrDefault(p => p.Id == id);
            if (memPrayer != null)
            {
                Prayers.Remove(memPrayer);
            }
            NotifyStateChanged();
        }

        public void UpdatePrayerRequest(int id, string title, string details, bool isAnonymous, string authorName, string mobileNumber)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var prayer = context.PrayerRequests.FirstOrDefault(p => p.Id == id);
                if (prayer != null)
                {
                    prayer.Title = title;
                    prayer.Details = details;
                    prayer.IsAnonymous = isAnonymous;
                    prayer.AuthorName = authorName;
                    prayer.MobileNumber = mobileNumber;
                    context.SaveChanges();
                }
            }
            var memPrayer = Prayers.FirstOrDefault(p => p.Id == id);
            if (memPrayer != null)
            {
                memPrayer.Title = title;
                memPrayer.Details = details;
                memPrayer.IsAnonymous = isAnonymous;
                memPrayer.AuthorName = authorName;
                memPrayer.MobileNumber = mobileNumber;
            }
            NotifyStateChanged();
        }

        public void TogglePraying(int prayerId, string userEmail)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var prayer = context.PrayerRequests.FirstOrDefault(p => p.Id == prayerId);
                if (prayer != null)
                {
                    if (prayer.PrayingUsers.Contains(userEmail))
                    {
                        prayer.PrayingUsers.Remove(userEmail);
                    }
                    else
                    {
                        prayer.PrayingUsers.Add(userEmail);
                    }
                    context.SaveChanges();

                    var memPrayer = Prayers.FirstOrDefault(p => p.Id == prayerId);
                    if (memPrayer != null)
                    {
                        memPrayer.PrayingUsers = prayer.PrayingUsers;
                    }
                }
            }
            NotifyStateChanged();
        }

        public void AddEncouragement(int prayerId, string authorName, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var comment = new Comment
            {
                AuthorName = authorName,
                Content = message,
                Timestamp = DateTime.Now
            };

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var prayer = context.PrayerRequests.Include(pr => pr.Encouragements).FirstOrDefault(p => p.Id == prayerId);
                if (prayer != null)
                {
                    prayer.Encouragements.Add(comment);
                    context.SaveChanges();

                    var memPrayer = Prayers.FirstOrDefault(p => p.Id == prayerId);
                    if (memPrayer != null)
                    {
                        memPrayer.Encouragements.Add(comment);
                    }
                }
            }
            NotifyStateChanged();
        }

        // Event RSVP & Volunteer Operations
        public void ToggleRSVP(int eventId, string email, string status)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var existing = context.EventRSVPs.FirstOrDefault(r => r.EventId == eventId && r.Email.ToLower() == email.ToLower());
                if (existing != null)
                {
                    existing.Status = status;
                }
                else
                {
                    context.EventRSVPs.Add(new EventRSVP { EventId = eventId, Email = email, Status = status });
                }
                context.SaveChanges();
                
                var ev = Events.FirstOrDefault(e => e.Id == eventId);
                if (ev != null)
                {
                    ev.RSVPs[email] = status;
                }
            }
            NotifyStateChanged();
        }

        public void SignUpAsVolunteer(int eventId, string email, string role)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var existing = context.EventVolunteers.FirstOrDefault(v => v.EventId == eventId && v.Email.ToLower() == email.ToLower());
                if (existing != null)
                {
                    existing.Role = role;
                }
                else
                {
                    context.EventVolunteers.Add(new EventVolunteer { EventId = eventId, Email = email, Role = role });
                }
                context.SaveChanges();
                
                var ev = Events.FirstOrDefault(e => e.Id == eventId);
                if (ev != null)
                {
                    ev.Volunteers[email] = role;
                }
            }
            NotifyStateChanged();
        }

        public void RemoveVolunteer(int eventId, string email)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var existing = context.EventVolunteers.FirstOrDefault(v => v.EventId == eventId && v.Email.ToLower() == email.ToLower());
                if (existing != null)
                {
                    context.EventVolunteers.Remove(existing);
                    context.SaveChanges();
                }
                
                var ev = Events.FirstOrDefault(e => e.Id == eventId);
                if (ev != null && ev.Volunteers.ContainsKey(email))
                {
                    ev.Volunteers.Remove(email);
                }
            }
            NotifyStateChanged();
        }

        public void AddChurchEvent(string title, string description, string dateText)
        {
            var newEvent = new ChurchEvent
            {
                Title = title,
                Description = description,
                DateText = dateText
            };

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                context.ChurchEvents.Add(newEvent);
                context.SaveChanges();
                Events.Add(newEvent);
            }
            NotifyStateChanged();
        }

        public void UpdateChurchEvent(int id, string title, string description, string dateText)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                var ev = context.ChurchEvents.FirstOrDefault(e => e.Id == id);
                if (ev != null)
                {
                    ev.Title = title;
                    ev.Description = description;
                    ev.DateText = dateText;
                    context.SaveChanges();

                    var memEv = Events.FirstOrDefault(e => e.Id == id);
                    if (memEv != null)
                    {
                        memEv.Title = title;
                        memEv.Description = description;
                        memEv.DateText = dateText;
                    }
                }
            }
            NotifyStateChanged();
        }

        public void DeleteChurchEvent(int id)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                
                // 1. Remove RSVPs
                var rsvps = context.EventRSVPs.Where(r => r.EventId == id).ToList();
                context.EventRSVPs.RemoveRange(rsvps);
                
                // 2. Remove Volunteers
                var volunteers = context.EventVolunteers.Where(v => v.EventId == id).ToList();
                context.EventVolunteers.RemoveRange(volunteers);
                
                // 3. Remove Event
                var ev = context.ChurchEvents.FirstOrDefault(e => e.Id == id);
                if (ev != null)
                {
                    context.ChurchEvents.Remove(ev);
                }
                
                context.SaveChanges();
            }
            
            var memEv = Events.FirstOrDefault(e => e.Id == id);
            if (memEv != null)
            {
                Events.Remove(memEv);
            }
            NotifyStateChanged();
        }

        // Sermon Notes
        public void SaveSermonNote(int sermonId, string text)
        {
            if (CurrentUser != null)
            {
                UserNotes[sermonId] = text;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                    var existing = context.SermonNotes.FirstOrDefault(n => n.SermonId == sermonId && n.UserEmail.ToLower() == CurrentUser.Email.ToLower());
                    if (existing != null)
                    {
                        existing.NoteText = text;
                        existing.UpdatedDate = DateTime.Now;
                    }
                    else
                    {
                        context.SermonNotes.Add(new SermonNoteEntity
                        {
                            SermonId = sermonId,
                            UserEmail = CurrentUser.Email,
                            NoteText = text,
                            UpdatedDate = DateTime.Now
                        });
                    }
                    context.SaveChanges();
                }
                NotifyStateChanged();
            }
        }

        public void DeleteSermonNote(int sermonId)
        {
            if (CurrentUser != null)
            {
                if (UserNotes.ContainsKey(sermonId))
                {
                    UserNotes.Remove(sermonId);
                }
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                    var existing = context.SermonNotes.FirstOrDefault(n => n.SermonId == sermonId && n.UserEmail.ToLower() == CurrentUser.Email.ToLower());
                    if (existing != null)
                    {
                        context.SermonNotes.Remove(existing);
                        context.SaveChanges();
                    }
                }
                NotifyStateChanged();
            }
        }

        // Broadcast notifications
        public void BroadcastNotification(string title, string message)
        {
            var notif = new Notification
            {
                Title = title,
                Message = message,
                Timestamp = DateTime.Now
            };

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();
                context.Notifications.Add(notif);
                context.SaveChanges();
                Notifications.Insert(0, notif);
            }

            OnNotificationReceived?.Invoke(notif);
            NotifyStateChanged();
        }

        private void SeedData()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ChurchDbContext>();

                // Automatically creates database and tables if they do not exist
                context.Database.EnsureCreated();

                try
                {
                    // Try to add the Password column if it doesn't exist in MySQL
                    context.Database.ExecuteSqlRaw("ALTER TABLE `Members` ADD COLUMN `Password` VARCHAR(100) NOT NULL DEFAULT 'password';");
                }
                catch (Exception)
                {
                    // Suppress if column already exists
                }

                try
                {
                    // Try to add the MobileNumber column if it doesn't exist in MySQL
                    context.Database.ExecuteSqlRaw("ALTER TABLE `PrayerRequests` ADD COLUMN `MobileNumber` VARCHAR(30) NULL;");
                }
                catch (Exception)
                {
                    // Suppress if column already exists
                }

                // 1. Seed Members
                if (!context.Members.Any())
                {
                    context.Members.Add(new Member { Name = "Pastor David Miller", Email = "david@christmiracle.org", Password = "password", Phone = "555-0122", Address = "12 Oak Ave, Miracle City", FamilyMembers = "Wife (Sarah), 2 Children", HowHeard = "Founder", Interests = new List<string> { "Choir", "Missions" }, JoinedDate = DateTime.Now.AddYears(-5) });
                    context.Members.Add(new Member { Name = "Sarah Connor", Email = "sarah@example.com", Password = "password", Phone = "555-0143", Address = "45 Elm St, Miracle City", FamilyMembers = "Son (John)", HowHeard = "Friend", Interests = new List<string> { "Youth", "Kids Ministry" }, JoinedDate = DateTime.Now.AddYears(-1) });
                    context.Members.Add(new Member { Name = "James Carter", Email = "james@example.com", Password = "password", Phone = "555-0187", Address = "88 Pine Rd, Miracle City", FamilyMembers = "None", HowHeard = "Social Media", Interests = new List<string> { "Sound Tech", "Greeters" }, JoinedDate = DateTime.Now.AddMonths(-6) });
                    context.Members.Add(new Member { Name = "The Henderson Family", Email = "hendersons@example.com", Password = "password", Phone = "555-0155", Address = "102 Maple Dr, Miracle City", FamilyMembers = "Husband, Wife, 3 Kids", HowHeard = "Flyer", Interests = new List<string> { "Missions", "Kids Ministry" }, JoinedDate = DateTime.Now.AddMonths(-3) });
                    context.SaveChanges();
                }
                Members.Clear();
                Members.AddRange(context.Members.ToList());

                // 2. Seed Posts & Comments
                if (!context.Posts.Any())
                {
                    var post1 = new Post
                    {
                        Title = "Building a Community of Hope",
                        Content = "Our community food pantry and fellowship program starts next month. Over the last year, we've seen a growing need in our surrounding neighborhoods. In response, our missions team has partnered with local food banks to open a weekly pantry. We are looking for volunteer drivers, packers, and greeters. Come join us in sharing hope and a hot meal!",
                        ImageUrl = "https://picsum.photos/800/400?random=101",
                        Category = "Announcement",
                        Author = "Pastor David Miller",
                        PublishedDate = DateTime.Now.AddDays(-2),
                        Likes = new List<string> { "sarah@example.com", "james@example.com" }
                    };
                    post1.Comments.Add(new Comment { AuthorName = "Sarah Connor", Content = "This is wonderful! John and I would love to volunteer on Saturday mornings.", Timestamp = DateTime.Now.AddDays(-1) });
                    post1.Comments.Add(new Comment { AuthorName = "James Carter", Content = "I can help set up the tables and organize the storage space.", Timestamp = DateTime.Now.AddHours(-12) });

                    var post2 = new Post
                    {
                        Title = "Reflections on Sunday's Message: Walk in Grace",
                        Content = "Ephesians 2:8 reminds us, 'For by grace you have been saved through faith.' Last Sunday, we unpacked the deep freedom that comes from knowing we don't have to earn God's love. It is a gift already paid for. How are you extending that same unconditional grace to those in your workplace, school, or home this week? Remember to take a deep breath and trust that you are held in His hands.",
                        ImageUrl = "https://picsum.photos/800/400?random=102",
                        Category = "Sermon",
                        Author = "Pastor David Miller",
                        PublishedDate = DateTime.Now.AddDays(-5),
                        Likes = new List<string> { "david@christmiracle.org" }
                    };

                    var post3 = new Post
                    {
                        Title = "Youth Summer Camp: Fire & Worship Testimony",
                        Content = "Last week, our teenagers spent 5 days in the wilderness at Camp Firefly. We witnessed 8 students commit their lives to Christ during our Wednesday night campfire worship service. The sheer joy and spiritual awakening was tangible. Thank you to the entire congregation for praying for us and sponsoring students who otherwise couldn't afford to attend. God is moving in our youth!",
                        ImageUrl = "https://picsum.photos/800/400?random=103",
                        Category = "Testimony",
                        Author = "Sarah Connor",
                        PublishedDate = DateTime.Now.AddDays(-9),
                        Likes = new List<string> { "david@christmiracle.org", "hendersons@example.com", "james@example.com" }
                    };
                    post3.Comments.Add(new Comment { AuthorName = "Pastor David Miller", Content = "Praise God! We are so blessed by our youth leaders.", Timestamp = DateTime.Now.AddDays(-8) });

                    context.Posts.AddRange(post1, post2, post3);
                    context.SaveChanges();
                }
                Posts.Clear();
                Posts.AddRange(context.Posts.Include(p => p.Comments).OrderByDescending(p => p.PublishedDate).ToList());

                // 3. Seed Gallery
                if (!context.GalleryImages.Any())
                {
                    context.GalleryImages.Add(new GalleryImage { ImageUrl = "https://images.unsplash.com/photo-1544427928-c49cd7f40173?auto=format&fit=crop&q=80&w=600", Caption = "Sunday Easter Baptism Celebration", Album = "Baptisms", UploadedBy = "david@christmiracle.org", IsApproved = true, UploadedDate = DateTime.Now.AddDays(-20) });
                    context.GalleryImages.Add(new GalleryImage { ImageUrl = "https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?auto=format&fit=crop&q=80&w=600", Caption = "Campfire Worship - Youth Camp 2026", Album = "Youth Camp", UploadedBy = "sarah@example.com", IsApproved = true, UploadedDate = DateTime.Now.AddDays(-8) });
                    context.GalleryImages.Add(new GalleryImage { ImageUrl = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&q=80&w=600", Caption = "Choir Rehearsal for Pentecost Sunday", Album = "Choir", UploadedBy = "david@christmiracle.org", IsApproved = true, UploadedDate = DateTime.Now.AddDays(-15) });
                    context.GalleryImages.Add(new GalleryImage { ImageUrl = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?auto=format&fit=crop&q=80&w=600", Caption = "Packing boxes at the food drive", Album = "Outreach", UploadedBy = "james@example.com", IsApproved = true, UploadedDate = DateTime.Now.AddDays(-1) });
                    context.GalleryImages.Add(new GalleryImage { ImageUrl = "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?auto=format&fit=crop&q=80&w=600", Caption = "Beautiful altar flowers from Sunday", Album = "Outreach", UploadedBy = "hendersons@example.com", IsApproved = false, UploadedDate = DateTime.Now });
                    context.SaveChanges();
                }
                GalleryImages.Clear();
                GalleryImages.AddRange(context.GalleryImages.ToList());

                // 4. Seed Prayers & Encouragements
                if (!context.PrayerRequests.Any())
                {
                    var prayer1 = new PrayerRequest
                    {
                        Title = "Healing for John's upcoming surgery",
                        Details = "My son John has minor surgery on his wrist scheduled for this Friday. Please pray for peace of mind for both of us and for a swift, complication-free recovery.",
                        IsAnonymous = false,
                        AuthorName = "Sarah Connor",
                        CreatedDate = DateTime.Now.AddDays(-1),
                        PrayingUsers = new List<string> { "david@christmiracle.org", "james@example.com" }
                    };
                    prayer1.Encouragements.Add(new Comment { AuthorName = "Pastor David Miller", Content = "We are praying for you and John, Sarah. God is our Healer.", Timestamp = DateTime.Now.AddHours(-18) });

                    var prayer2 = new PrayerRequest
                    {
                        Title = "Guidance in a difficult career transition",
                        Details = "I was recently laid off and am searching for a new engineering role. Praying for financial stability and wisdom to know which directions to pursue.",
                        IsAnonymous = true,
                        AuthorName = "Anonymous",
                        CreatedDate = DateTime.Now.AddDays(-3),
                        PrayingUsers = new List<string> { "david@christmiracle.org", "sarah@example.com", "hendersons@example.com" }
                    };
                    prayer2.Encouragements.Add(new Comment { AuthorName = "James Carter", Content = "Keep your head up! Standing in faith with you.", Timestamp = DateTime.Now.AddDays(-2) });

                    var prayer3 = new PrayerRequest
                    {
                        Title = "Restoration of family relationship",
                        Details = "Please pray for a breakthrough in communication between my daughter and me. We haven't spoken in months and my heart is heavy.",
                        IsAnonymous = true,
                        AuthorName = "Anonymous",
                        CreatedDate = DateTime.Now.AddDays(-6),
                        PrayingUsers = new List<string> { "sarah@example.com" }
                    };

                    context.PrayerRequests.AddRange(prayer1, prayer2, prayer3);
                    context.SaveChanges();
                }
                Prayers.Clear();
                Prayers.AddRange(context.PrayerRequests.Include(pr => pr.Encouragements).OrderByDescending(pr => pr.CreatedDate).ToList());

                // 5. Seed Events, RSVPs, Volunteers
                if (!context.ChurchEvents.Any())
                {
                    var event1 = new ChurchEvent
                    {
                        Title = "Teen Worship & Game Night",
                        Description = "A fun night filled with competitive group games, live student worship, pizza, and a brief devotional message. Open to grades 6-12.",
                        DateText = "Friday, June 20 at 7:00 PM"
                    };
                    var event2 = new ChurchEvent
                    {
                        Title = "Saturday Community Outreach",
                        Description = "Packing and distributing essential grocery boxes to senior citizens and low-income families in our local county. Families welcome!",
                        DateText = "Saturday, June 21 at 9:00 AM"
                    };
                    var event3 = new ChurchEvent
                    {
                        Title = "Sunday Worship & Communion",
                        Description = "Join us in-person or online for a message from Pastor David, corporate worship led by our choir, and Communion.",
                        DateText = "Sunday, June 22 at 9:00 AM & 11:00 AM"
                    };
                    context.ChurchEvents.AddRange(event1, event2, event3);
                    context.SaveChanges(); // Generates IDs

                    // Seed RSVPs and Volunteers
                    context.EventRSVPs.Add(new EventRSVP { EventId = event1.Id, Email = "sarah@example.com", Status = "Yes" });
                    context.EventRSVPs.Add(new EventRSVP { EventId = event1.Id, Email = "hendersons@example.com", Status = "Yes" });
                    context.EventVolunteers.Add(new EventVolunteer { EventId = event1.Id, Email = "sarah@example.com", Role = "Kids Helper" });

                    context.EventRSVPs.Add(new EventRSVP { EventId = event2.Id, Email = "david@christmiracle.org", Status = "Yes" });
                    context.EventRSVPs.Add(new EventRSVP { EventId = event2.Id, Email = "james@example.com", Status = "Yes" });
                    context.EventRSVPs.Add(new EventRSVP { EventId = event2.Id, Email = "sarah@example.com", Status = "Maybe" });
                    context.EventVolunteers.Add(new EventVolunteer { EventId = event2.Id, Email = "james@example.com", Role = "Audio Visual" });
                    context.EventVolunteers.Add(new EventVolunteer { EventId = event2.Id, Email = "david@christmiracle.org", Role = "Greeter" });

                    context.EventRSVPs.Add(new EventRSVP { EventId = event3.Id, Email = "david@christmiracle.org", Status = "Yes" });
                    context.EventRSVPs.Add(new EventRSVP { EventId = event3.Id, Email = "james@example.com", Status = "Yes" });
                    context.EventRSVPs.Add(new EventRSVP { EventId = event3.Id, Email = "sarah@example.com", Status = "Yes" });
                    context.EventRSVPs.Add(new EventRSVP { EventId = event3.Id, Email = "hendersons@example.com", Status = "Yes" });
                    context.EventVolunteers.Add(new EventVolunteer { EventId = event3.Id, Email = "james@example.com", Role = "Greeter" });
                    context.SaveChanges();
                }
                Events.Clear();
                var dbEvents = context.ChurchEvents.ToList();
                var dbRSVPs = context.EventRSVPs.ToList();
                var dbVolunteers = context.EventVolunteers.ToList();
                foreach (var ev in dbEvents)
                {
                    ev.RSVPs = dbRSVPs.Where(r => r.EventId == ev.Id).ToDictionary(r => r.Email, r => r.Status);
                    ev.Volunteers = dbVolunteers.Where(v => v.EventId == ev.Id).ToDictionary(v => v.Email, v => v.Role);
                    Events.Add(ev);
                }

                // 6. Seed Sermons
                if (!context.Sermons.Any())
                {
                    context.Sermons.Add(new Sermon { Title = "Walk in Grace", Speaker = "Pastor David Miller", DateText = "June 14, 2026", AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3", DurationText = "12:34" });
                    context.Sermons.Add(new Sermon { Title = "The Shield of Faith", Speaker = "Pastor David Miller", DateText = "June 07, 2026", AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-2.mp3", DurationText = "15:45" });
                    context.Sermons.Add(new Sermon { Title = "Living Generously", Speaker = "Pastor David Miller", DateText = "May 31, 2026", AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-3.mp3", DurationText = "14:12" });
                    context.Sermons.Add(new Sermon { Title = "Strength in the Storm", Speaker = "Pastor David Miller", DateText = "May 24, 2026", AudioUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-4.mp3", DurationText = "11:28" });
                    context.SaveChanges();
                }
                Sermons.Clear();
                Sermons.AddRange(context.Sermons.ToList());

                // 7. Notifications
                Notifications.Clear();
                Notifications.AddRange(context.Notifications.OrderByDescending(n => n.Timestamp).ToList());
            }
        }

        public void SendChatMessage(string authorName, string messageText, string userRole)
        {
            if (string.IsNullOrWhiteSpace(messageText)) return;

            var msg = new ChatMessage
            {
                AuthorName = authorName,
                MessageText = messageText,
                Timestamp = DateTime.Now,
                UserRole = userRole
            };

            if (ChatMessages.Count >= 50)
            {
                ChatMessages.RemoveAt(0);
            }

            ChatMessages.Add(msg);
            NotifyStateChanged();
        }

        public void BroadcastReaction(string reactionType)
        {
            OnReactionReceived?.Invoke(reactionType);
        }
    }

    // Model Definitions
    public class Member
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string FamilyMembers { get; set; } = string.Empty;
        public string HowHeard { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new();
        public DateTime JoinedDate { get; set; }
    }

    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Announcement, Sermon, Testimony
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public List<string> Likes { get; set; } = new(); // User Emails
        public List<Comment> Comments { get; set; } = new();
    }

    public class Comment
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class GalleryImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty; // Baptisms, Youth Camp, Choir, Outreach
        public string UploadedBy { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    public class PrayerRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public List<string> PrayingUsers { get; set; } = new(); // User Emails
        public List<Comment> Encouragements { get; set; } = new();
        public DateTime CreatedDate { get; set; }
    }

    public class ChurchEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public Dictionary<string, string> RSVPs { get; set; } = new(); // Email -> "Yes", "No", "Maybe"
        public Dictionary<string, string> Volunteers { get; set; } = new(); // Email -> Role
    }

    public class Sermon
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Speaker { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public string DurationText { get; set; } = string.Empty;
    }

    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ChatMessage
    {
        public string AuthorName { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string UserRole { get; set; } = string.Empty;
    }
}
