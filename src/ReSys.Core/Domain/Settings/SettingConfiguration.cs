namespace ReSys.Core.Domain.Settings;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable(Schema.Settings);

        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.Key)
            .IsUnique();

        builder.Property(c => c.Key)
            .HasMaxLength(Setting.Constraints.KeyMaxLength)
            .IsRequired();

        builder.Property(c => c.Value)
            .HasMaxLength(Setting.Constraints.ValueMaxLength)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(Setting.Constraints.DescriptionMaxLength)
            .IsRequired();
            
        builder.Property(c => c.DefaultValue)
            .HasMaxLength(Setting.Constraints.DefaultValueMaxLength)
            .IsRequired();

        builder.Property(c => c.ValueType)
            .IsRequired();
    }
}
