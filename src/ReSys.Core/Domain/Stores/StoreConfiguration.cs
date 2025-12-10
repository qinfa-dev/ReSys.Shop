using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;
using ReSys.Core.Domain.Location;

namespace ReSys.Core.Domain.Stores;

/// <summary>
/// Configures the database mapping for the <see cref="Store"/> entity.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Configuration Strategy:</strong>
/// <list type="bullet">
/// <item><description>Table: Stored in 'Stores' table (defined in Schema.Stores)</description></item>
/// <item><description>Primary Key: Store.Id (Guid, value-generated never)</description></item>
/// <item><description>Uniqueness: Name, Code, Url all have unique constraints at DB level</description></item>
/// <item><description>Query Filter: Soft-deleted stores automatically excluded from queries</description></item>
/// <item><description>Cascade Delete: StoreProducts, StoreShippingMethods, StorePaymentMethods cascade on delete</description></item>
/// <item><description>Restrict Delete: Orders restricted (do not auto-delete with store)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Soft Deletion Pattern:</strong>
/// All queries automatically exclude soft-deleted stores via HasQueryFilter.
/// Use .IgnoreQueryFilters() to include deleted stores in a specific query.
/// DeletedAt timestamp recorded for audit trail and recovery.
/// </para>
/// <para>
/// <strong>Key Concerns Applied:</strong>
/// <list type="bullet">
/// <item><description>IHasMetadata - Public/private metadata support</description></item>
/// <item><description>IHasUniqueName - Unique name constraint enforcement</description></item>
/// <item><description>IHasSeoMetadata - Meta title/description/keywords</description></item>
/// <item><description>IHasAuditable - CreatedAt/UpdatedAt audit fields</description></item>
/// <item><description>ISoftDeletable - DeletedAt soft deletion support</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    /// <summary>
    /// Configures the entity of type <see cref="Store"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Configuration Steps:</strong>
    /// <list type="ordered">
    /// <item><description>Table mapping to 'Stores' table</description></item>
    /// <item><description>Primary key definition (Id)</description></item>
    /// <item><description>Unique indexes for Name, Code, Url (queryable)</description></item>
    /// <item><description>Alternate key for Code (database constraint)</description></item>
    /// <item><description>Property configurations with constraints and comments</description></item>
    /// <item><description>Common concern configurations (Metadata, SEO, Auditable)</description></item>
    /// <item><description>Soft delete query filter (automatic exclusion of deleted)</description></item>
    /// <item><description>Relationship configurations with cascade/restrict delete behavior</description></item>
    /// <item><description>Computed property ignores</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Query Filter Behavior:</strong>
    /// By default, all Store queries automatically exclude soft-deleted stores:
    /// <code>
    /// // Automatically excludes deleted stores
    /// var stores = await dbContext.Stores.ToListAsync();
    ///
    /// // Include deleted stores explicitly
    /// var allStores = await dbContext.Stores.IgnoreQueryFilters().ToListAsync();
    ///
    /// // Delete a store (soft delete, not hard delete)
    /// var result = store.Delete();
    /// await dbContext.SaveChangesAsync();
    /// // Now automatically excluded from future queries
    /// </code>
    /// </para>
    /// </remarks>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        #region Table
        // Set the table name for the Storefront entity.
        builder.ToTable(name: Schema.Stores);
        #endregion

        #region Primary Key
        // Configure the primary key for the Storefront entity.
        builder.HasKey(keyExpression: s => s.Id);
        #endregion

        #region Indexes & Constraints
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: s => s.Name).IsUnique();
        builder.HasIndex(indexExpression: s => s.Code).IsUnique();
        builder.HasIndex(indexExpression: s => s.Url).IsUnique();
        builder.HasIndex(indexExpression: s => s.Default);
        builder.HasIndex(indexExpression: s => s.DeletedAt);
        
        // Configure alternate key for Code uniqueness at database level
        // This ensures no two stores can have the same code, even at the database level
        builder.HasAlternateKey(keyExpression: s => s.Code)
            .HasName("AK_Store_Code");
        #endregion

        #region Properties
        // Configure properties for the Storefront entity.
        builder.Property(propertyExpression: s => s.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the store. Value generated never.");

        builder.Property(propertyExpression: s => s.Name)
            .IsRequired()
            .HasMaxLength(maxLength: Store.Constraints.NameMaxLength)
            .HasComment(comment: "Name: The display name of the store (e.g., 'MyShop', 'Fashion Storefront'). Required and must be unique.");

        builder.Property(propertyExpression: s => s.Presentation)
            .IsRequired()
            .HasMaxLength(maxLength: Store.Constraints.NameMaxLength)
            .HasComment(comment: "Presentation: Alternative presentation name for the store used in UI/branding contexts. Required.");

        builder.Property(propertyExpression: s => s.Code)
            .IsRequired()
            .HasMaxLength(maxLength: Store.Constraints.CodeMaxLength)
            .HasComment(comment: "Code: Unique store identifier used as subdomain or slug (e.g., 'myshop', 'fashion-store'). Required, uppercase normalized.");

        builder.Property(propertyExpression: s => s.Url)
            .IsRequired()
            .HasMaxLength(maxLength: Store.Constraints.UrlMaxLength)
            .HasComment(comment: "Url: The store's base URL (e.g., 'shop.example.com', 'example.com/shop'). Required and must be unique.");

        builder.Property(propertyExpression: s => s.DefaultCurrency)
            .IsRequired()
            .HasMaxLength(maxLength: 3)
            .HasComment(comment: "DefaultCurrency: The default currency code for transactions (e.g., 'USD', 'EUR', 'GBP', 'VND'). Required, defaults to 'USD'.");

        builder.Property(propertyExpression: s => s.DefaultLocale)
            .IsRequired()
            .HasMaxLength(maxLength: 10)
            .HasComment(comment: "DefaultLocale: The default language/locale for the store (e.g., 'en', 'en-US', 'fr'). Required, defaults to 'en'.");

        builder.Property(propertyExpression: s => s.Timezone)
            .IsRequired()
            .HasMaxLength(maxLength: 50)
            .HasComment(comment: "Timezone: The store's timezone for scheduling and time-based operations (e.g., 'UTC', 'America/New_York'). Required, defaults to 'UTC'.");

        builder.Property(propertyExpression: s => s.Default)
            .IsRequired()
            .HasComment(comment: "Default: Indicates if this is the default store for the system. Only one store should be marked as default.");

        builder.Property(propertyExpression: s => s.MailFromAddress)
            .HasMaxLength(maxLength: Store.Constraints.EmailMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "MailFromAddress: The email address used as the 'From' address for transactional emails sent by the store.");

        builder.Property(propertyExpression: s => s.CustomerSupportEmail)
            .HasMaxLength(maxLength: Store.Constraints.EmailMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "CustomerSupportEmail: The email address for customer support inquiries and contact information.");

        builder.Property(propertyExpression: s => s.MetaTitle)
            .HasMaxLength(maxLength: Store.Constraints.NameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "MetaTitle: SEO meta title tag for the store homepage, displayed in search results.");

        builder.Property(propertyExpression: s => s.MetaDescription)
            .HasMaxLength(maxLength: 500)
            .IsRequired(required: false)
            .HasComment(comment: "MetaDescription: SEO meta description tag for the store homepage, displayed in search engine previews.");

        builder.Property(propertyExpression: s => s.MetaKeywords)
            .HasMaxLength(maxLength: 500)
            .IsRequired(required: false)
            .HasComment(comment: "MetaKeywords: SEO meta keywords for the store (comma-separated list of relevant search terms).");

        builder.Property(propertyExpression: s => s.SeoTitle)
            .HasMaxLength(maxLength: Store.Constraints.NameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "SeoTitle: Alternative SEO title for improved search engine optimization.");

        builder.Property(propertyExpression: s => s.Available)
            .IsRequired()
            .HasComment(comment: "Available: Indicates if the store is currently available/active for customer access. Defaults to true.");

        builder.Property(propertyExpression: s => s.GuestCheckoutAllowed)
            .IsRequired()
            .HasComment(comment: "GuestCheckoutAllowed: Indicates whether customers can proceed to checkout without creating an account. Defaults to true.");

        builder.Property(propertyExpression: s => s.PasswordProtected)
            .IsRequired()
            .HasComment(comment: "PasswordProtected: Indicates if the storefront is protected by a password. Useful for private/beta stores.");

        builder.Property(propertyExpression: s => s.StorefrontPassword)
            .HasMaxLength(maxLength: 255)
            .IsRequired(required: false)
            .HasComment(comment: "StorefrontPassword: The password required to access a password-protected store (typically hashed/encrypted).");

        // Social media
        builder.Property(propertyExpression: s => s.Facebook)
            .HasMaxLength(maxLength: 500)
            .IsRequired(required: false)
            .HasComment(comment: "Facebook: Storefront's Facebook profile URL or handle for social media integration.");

        builder.Property(propertyExpression: s => s.Instagram)
            .HasMaxLength(maxLength: 500)
            .IsRequired(required: false)
            .HasComment(comment: "Instagram: Storefront's Instagram profile URL or handle for social media integration.");

        builder.Property(propertyExpression: s => s.Twitter)
            .HasMaxLength(maxLength: 500)
            .IsRequired(required: false)
            .HasComment(comment: "Twitter: Storefront's Twitter/X profile URL or handle for social media integration.");

        // IAddress properties
        builder.Property(propertyExpression: s => s.Address1)
            .HasMaxLength(maxLength: AddressConstraints.Address1MaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Address1: Primary address line for the store's physical location (street address).");

        builder.Property(propertyExpression: s => s.Address2)
            .HasMaxLength(maxLength: AddressConstraints.Address2MaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Address2: Secondary address line for the store's physical location (apartment, suite, etc.).");

        builder.Property(propertyExpression: s => s.City)
            .HasMaxLength(maxLength: AddressConstraints.CityMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "City: The city or municipality where the store is located.");

        builder.Property(propertyExpression: s => s.Zipcode)
            .HasMaxLength(maxLength: AddressConstraints.ZipcodeMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Zipcode: The postal code or ZIP code for the store's physical address.");

        builder.Property(propertyExpression: s => s.Phone)
            .HasMaxLength(maxLength: AddressConstraints.PhoneMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Phone: The primary phone number for the store's physical location.");

        builder.Property(propertyExpression: s => s.Company)
            .HasMaxLength(maxLength: AddressConstraints.CompanyMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Company: The legal company name associated with the store.");

        // Soft delete
        builder.Property(propertyExpression: s => s.DeletedAt)
            .IsRequired(required: false)
            .HasComment(comment: "DeletedAt: The timestamp when the store was soft deleted (null if not deleted). Enables recovery and audit trails.");

        // Ignore the Products property as it's a computed property and not a direct database mapping.
        builder.Ignore(propertyExpression: s => s.Products);

        // Apply common configurations using extension methods.
        builder.ConfigureMetadata();
        builder.ConfigureParameterizableName(); // For IHasUniqueName
        builder.ConfigureSeoMetadata();
        builder.ConfigureAuditable(); // For CreatedAt, UpdatedAt
        
        // Configure soft delete query filter - automatically exclude deleted stores
        builder.HasQueryFilter(s => !s.IsDeleted);
        #endregion

        #region Relationships
        // Configure relationships for the Storefront entity.
        builder.HasMany(navigationExpression: s => s.StoreProducts)
            .WithOne(navigationExpression: sp => sp.Store)
            .HasForeignKey(foreignKeyExpression: sp => sp.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: s => s.Orders)
            .WithOne(navigationExpression: o => o.Store)
            .HasForeignKey(foreignKeyExpression: o => o.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Orders should not be deleted if store is deleted

        builder.HasMany(navigationExpression: s => s.StoreShippingMethods)
            .WithOne(navigationExpression: ssm => ssm.Store)
            .HasForeignKey(foreignKeyExpression: ssm => ssm.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: s => s.StorePaymentMethods)
            .WithOne(navigationExpression: spm => spm.Store)
            .HasForeignKey(foreignKeyExpression: spm => spm.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: s => s.Taxonomies)
            .WithOne(navigationExpression: c => c.Store)
            .HasForeignKey(foreignKeyExpression: c => c.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: s => s.Country)
            .WithMany(navigationExpression: m => m.Stores)
            .HasForeignKey(foreignKeyExpression: s => s.CountryId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict);

        builder.HasOne(navigationExpression: s => s.State)
            .WithMany(navigationExpression: m => m.Stores)
            .HasForeignKey(foreignKeyExpression: s => s.StateId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict);
        #endregion

        #region Ignored Properties
        builder.Ignore(propertyExpression: oa => oa.Products);
        builder.Ignore(propertyExpression: oa => oa.AvailablePaymentMethods);
        builder.Ignore(propertyExpression: oa => oa.AvailableShippingMethods);
        #endregion
    }
}
