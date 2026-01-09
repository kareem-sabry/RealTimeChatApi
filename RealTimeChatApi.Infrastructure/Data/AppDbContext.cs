using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Core.Interfaces;

namespace RealTimeChatApi.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User,IdentityRole<Guid>, Guid>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options,IHttpContextAccessor httpContextAccessor,IDateTimeProvider dateTimeProvider) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _dateTimeProvider = dateTimeProvider;
    }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureUserEntity(modelBuilder);
        ConfigureConversationEntity(modelBuilder);
        ConfigureConversationParticipantEntity(modelBuilder);
        ConfigureMessageEntity(modelBuilder);
    }
    private static void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.RefreshToken).HasMaxLength(500);
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }

    private static void ConfigureConversationEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.CreatedAtUtc);
        });
    }

    private static void ConfigureConversationParticipantEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(cp => new { cp.ConversationId, cp.UserId });

            entity.HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cp => cp.User)
                .WithMany(u => u.ConversationParticipants)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cp => cp.UserId);
            entity.HasIndex(cp => cp.ConversationId);
        });
    }

    private static void ConfigureMessageEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.ConversationId);
            entity.HasIndex(m => m.SenderId);
            entity.HasIndex(m => m.SentAtUtc);
        });
    }

    private void ApplyAuditInformation()
    {
        var currentUserEmail = _httpContextAccessor?.HttpContext?.User?.Identity?.Name 
                               ?? ApplicationConstants.SystemUser;
        var currentTime = _dateTimeProvider.UtcNow;

        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditable auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.CreatedAtUtc = currentTime;
                    auditableEntity.UpdatedAtUtc = currentTime;
                    auditableEntity.CreatedBy = currentUserEmail;
                    auditableEntity.UpdatedBy = currentUserEmail;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableEntity.UpdatedAtUtc = currentTime;
                    auditableEntity.UpdatedBy = currentUserEmail;
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                }
            }
            else if (entry.Entity is ITimestampedEntity timestampedEntity && entry.State == EntityState.Added)
            {
                timestampedEntity.CreatedAtUtc = currentTime;
            }
            else if (entry.Entity is User user)
            {
                if (entry.State == EntityState.Added)
                {
                    user.CreatedAtUtc = currentTime;
                    user.UpdatedAtUtc = currentTime;
                }
                else if (entry.State == EntityState.Modified)
                {
                    user.UpdatedAtUtc = currentTime;
                    entry.Property(nameof(user.CreatedAtUtc)).IsModified = false;
                }
            }
        }
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }
}