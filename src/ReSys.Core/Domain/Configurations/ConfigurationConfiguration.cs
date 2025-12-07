using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReSys.Core.Domain.Configurations;

public class ConfigurationConfiguration : IEntityTypeConfiguration<Configuration>
{
    public void Configure(EntityTypeBuilder<Configuration> builder)
    {
        builder.ToTable("Configurations");

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.Key)
            .IsUnique();

        builder.Property(c => c.Key)
            .HasMaxLength(Configuration.Constraints.KeyMaxLength)
            .IsRequired();

        builder.Property(c => c.Value)
            .HasMaxLength(Configuration.Constraints.ValueMaxLength)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(Configuration.Constraints.DescriptionMaxLength)
            .IsRequired();
            
        builder.Property(c => c.DefaultValue)
            .HasMaxLength(Configuration.Constraints.DefaultValueMaxLength)
            .IsRequired();

        builder.Property(c => c.ValueType)
            .IsRequired();
    }
}
