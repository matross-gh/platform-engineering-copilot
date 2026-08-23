using Microsoft.EntityFrameworkCore;
using Platform.Engineering.Copilot.Chat.App.Models;
using System.Text.Json;

namespace Platform.Engineering.Copilot.Chat.App.Data;

/// <summary>
/// Database context for the enhanced chat application
/// </summary>
public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ChatMessage> Messages { get; set; }
    public DbSet<ConversationContext> Contexts { get; set; }
    public DbSet<MessageAttachment> Attachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ------------------------------------------------------------------
        // Cosmos DB modeling notes: one container per entity, partition key is
        // each entity's own Id. Cosmos indexes all properties by default, so
        // HasIndex() calls are removed. HasMany()/WithOne()/HasForeignKey()/
        // OnDelete(Cascade) are SQL-only relational constructs and are removed;
        // ChatService now fetches related entities (Messages, Context,
        // Attachments) manually via separate queries filtered by the parent id
        // and performs manual cascade-delete of child documents.
        // ------------------------------------------------------------------

        // Configure Conversation entity
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToContainer("Conversations").HasPartitionKey(e => e.Id);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(450); // Match foreign key references
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );
        });

        // Configure ChatMessage entity
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToContainer("Messages").HasPartitionKey(e => e.Id);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ConversationId).HasMaxLength(450).IsRequired(); // Match Conversation.Id length
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );
            entity.Property(e => e.Tools)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                );
            entity.Property(e => e.ToolResult)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => v == null ? null : JsonSerializer.Deserialize<ToolExecutionResult>(v, (JsonSerializerOptions?)null)
                );
        });

        // Configure ConversationContext entity
        modelBuilder.Entity<ConversationContext>(entity =>
        {
            entity.ToContainer("Contexts").HasPartitionKey(e => e.Id);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConversationId).HasMaxLength(450).IsRequired(); // Match Conversation.Id length
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Data)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                );
        });

        // Configure MessageAttachment entity
        modelBuilder.Entity<MessageAttachment>(entity =>
        {
            entity.ToContainer("Attachments").HasPartitionKey(e => e.Id);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageId).HasMaxLength(450).IsRequired(); // Match ChatMessage.Id length
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );
        });
    }
}