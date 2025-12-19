using ReSys.Core.Domain.Orders.History;

namespace ReSys.Core.Domain.Orders.Payments;

/// <summary>
/// Configures the database mapping for the <see cref="OrderHistory"/> entity.
/// </summary>
public sealed class OrderHistoryConfiguration : IEntityTypeConfiguration<OrderHistory>
{
    public void Configure(EntityTypeBuilder<OrderHistory> builder)
    {
        #region Table
        builder.ToTable(name: "order_history_logs");
        #endregion

        #region Primary Key
        builder.HasKey(h => h.Id);
        #endregion

        #region Properties
        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.OrderId)
            .IsRequired();

        builder.Property(h => h.Description)
            .HasMaxLength(OrderHistory.Constraints.DescriptionMaxLength)
            .IsRequired();

        builder.Property(h => h.FromState)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired(false);
        
        builder.Property(h => h.ToState)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(h => h.TriggeredBy)
            .HasMaxLength(OrderHistory.Constraints.TriggeredByMaxLength)
            .IsRequired(false);

        builder.Property(h => h.Context)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.HasOne(h => h.Order)
            .WithMany(o => o.Histories)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion

        #region Indexes
        // Index for efficiently querying the history of a specific order
        builder.HasIndex(h => h.OrderId);

        // Index to quickly find events by state
        builder.HasIndex(h => h.ToState);
        #endregion
    }
}
