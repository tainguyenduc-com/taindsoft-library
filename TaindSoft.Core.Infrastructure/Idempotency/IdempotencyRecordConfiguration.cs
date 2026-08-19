using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaindSoft.Core.Infrastructure.Idempotency
{
    /// <summary>
    /// EF Core configuration for IdempotencyRecord
    /// </summary>
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecords");

            builder.HasKey(x => x.Id);

            // Key field - unique, max 200 chars
            builder.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(x => x.Key)
                .IsUnique()
                .HasDatabaseName("IX_IdempotencyRecords_Key");

            // Request hash for mutation detection
            builder.Property(x => x.RequestHash)
                .IsRequired()
                .HasMaxLength(64); // SHA256 hex = 64 chars

            // Response body - store as text
            builder.Property(x => x.ResponseBody)
                .IsRequired()
                .HasColumnType("text");

            // HTTP status code
            builder.Property(x => x.StatusCode)
                .IsRequired();

            // Timestamps
            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            // Index for expiration cleanup (future background job)
            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("IX_IdempotencyRecords_ExpiresAt");
        }
    }
}
