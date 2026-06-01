using DocuMindAI.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMindAI.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(document => document.Name).HasMaxLength(255).IsRequired();
            entity.Property(document => document.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(document => document.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(document => document.FilePath).HasMaxLength(1000).IsRequired();

            entity.HasOne(document => document.User)
                .WithMany(user => user.Documents)
                .HasForeignKey(document => document.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkIndex }).IsUnique();
            entity.HasIndex(chunk => chunk.VectorPointId).IsUnique();
            entity.Property(chunk => chunk.Text).IsRequired();

            entity.HasOne(chunk => chunk.Document)
                .WithMany(document => document.Chunks)
                .HasForeignKey(chunk => chunk.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.Property(session => session.Title).HasMaxLength(255).IsRequired();

            entity.HasOne(session => session.User)
                .WithMany(user => user.ChatSessions)
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(message => message.Role).HasMaxLength(30).IsRequired();
            entity.Property(message => message.Content).IsRequired();

            entity.HasOne(message => message.ChatSession)
                .WithMany(session => session.Messages)
                .HasForeignKey(message => message.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
