using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace ReSys.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "eshopdb");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the audit log. Value generated never."),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "EntityId: The ID of the entity that was audited."),
                    entity_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "EntityName: The name of the entity that was audited."),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Action: The action performed on the entity (e.g., 'Create', 'Update', 'Delete')."),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp: The date and time when the audit log entry was created."),
                    user_id = table.Column<string>(type: "text", nullable: true, comment: "UserId: The ID of the user who performed the action."),
                    user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "UserName: The username of the user who performed the action."),
                    user_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UserEmail: The email of the user who performed the action."),
                    old_values = table.Column<string>(type: "jsonb", nullable: true, comment: "OldValues: JSONB field containing the old values of the entity's properties before the action."),
                    new_values = table.Column<string>(type: "jsonb", nullable: true, comment: "NewValues: JSONB field containing the new values of the entity's properties after the action."),
                    changed_properties = table.Column<string>(type: "jsonb", nullable: true, comment: "ChangedProperties: JSONB field containing the names of properties that were changed."),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true, comment: "IpAddress: The IP address from which the action was performed."),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "UserAgent: The user agent string of the client that performed the action."),
                    request_id = table.Column<string>(type: "text", nullable: true, comment: "RequestId: The ID of the request that triggered the action."),
                    reason = table.Column<string>(type: "text", nullable: true, comment: "Reason: The reason for the action, if provided."),
                    additional_data = table.Column<string>(type: "jsonb", nullable: true, comment: "AdditionalData: JSONB field for any additional data related to the audit log entry."),
                    severity = table.Column<string>(type: "text", nullable: false, comment: "Severity: The severity level of the audit log entry (e.g., 'Information', 'Warning', 'Error')."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the country."),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name: The full name of the country. Required."),
                    iso = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, comment: "Iso: ISO 3166-1 alpha-2 code."),
                    iso3 = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, comment: "Iso3: ISO 3166-1 alpha-3 code."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "option_types",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the option type. Value generated never."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Name: Normalized parameterizable name for internal use."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: Human-readable version of the parameterizable name."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    filterable = table.Column<bool>(type: "boolean", nullable: false),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_option_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the payment method. Value generated never."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Name: Normalized parameterizable name for internal use."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: Human-readable version of the parameterizable name."),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Description: A detailed description of the payment method."),
                    type = table.Column<string>(type: "text", nullable: false, comment: "Type: The type of payment method (e.g., 'CreditCard', 'PayPal')."),
                    active = table.Column<bool>(type: "boolean", nullable: false, comment: "Active: Indicates if the payment method is active."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    auto_capture = table.Column<bool>(type: "boolean", nullable: false, comment: "AutoCapture: Indicates if payments made with this method should be automatically captured."),
                    display_on = table.Column<string>(type: "text", nullable: false, comment: "DisplayOn: Specifies where the payment method should be displayed (e.g., 'Frontend', 'Backend')."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "DeletedAt: The timestamp when the payment method was soft deleted (null if not deleted)."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the access permission. Value generated never."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Name: The unique name of the permission (e.g., 'admin.users.create')."),
                    area = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Area: The area or domain of the permission (e.g., 'admin')."),
                    resource = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Resource: The resource the permission applies to (e.g., 'users')."),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Action: The action allowed by the permission (e.g., 'create')."),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "DisplayName: A user-friendly display name for the permission."),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Description: A detailed description of what the permission allows."),
                    value = table.Column<string>(type: "text", nullable: true),
                    PermissionCategory = table.Column<int>(type: "integer", nullable: true, comment: "Category: The category of the permission."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the product. Value generated never."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name: The name of the product."),
                    presentation = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true, comment: "Description: Full description of the product."),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Slug: SEO-friendly URL slug for the product."),
                    available_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "AvailableOn: Date when the product becomes available."),
                    make_active_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "MakeActiveAt: Date when the product should become active."),
                    discontinue_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "DiscontinueOn: Date when the product is discontinued."),
                    status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Status: The current status of the product (e.g., Draft, Active, Archived)."),
                    is_digital = table.Column<bool>(type: "boolean", nullable: false, comment: "IsDigital: Indicates if the product is digital."),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "MetaTitle: SEO title for the product."),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "MetaDescription: SEO description for the product."),
                    meta_keywords = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "MetaKeywords: SEO keywords for the product."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    marked_for_regenerate_taxon_products = table.Column<bool>(type: "boolean", nullable: false, comment: "MarkedForRegenerateTaxonProducts: Flag to indicate if automatic taxon products need regeneration."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "DeletedAt: Date when the product was soft-deleted."),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "DeletedBy: User who soft-deleted the product."),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, comment: "IsDeleted: Flag indicating if the product is soft-deleted."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the promotion."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name: The unique name of the entity."),
                    promotion_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Code: Optional coupon code for the promotion."),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Description: Optional detailed description of the promotion."),
                    minimum_order_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "MinimumOrderAmount: Minimum order amount required for the promotion to apply."),
                    maximum_discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true, comment: "MaximumDiscountAmount: Maximum discount amount that can be applied by the promotion."),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "StartsAt: Optional start date/time when the promotion becomes active."),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "ExpiresAt: Optional expiration date/time when the promotion ends."),
                    usage_limit = table.Column<int>(type: "integer", nullable: true, comment: "UsageLimit: Optional maximum number of times the promotion can be used."),
                    usage_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "UsageCount: Number of times the promotion has been used."),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Active: Indicates if the promotion is manually activated/deactivated."),
                    requires_coupon_code = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "RequiresCouponCode: Indicates if a coupon code must be entered to use this promotion."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "property_types",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the property. Value generated never."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name: The unique name of the entity."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: Human-readable version of the parameterizable name."),
                    filter_param = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "URL-friendly filter parameter generated from a source property (e.g., Name)."),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Kind: The data type or input type of the property (e.g., 'ShortText', 'Number', 'Boolean')."),
                    filterable = table.Column<bool>(type: "boolean", nullable: false, comment: "Filterable: Indicates if this property can be used for filtering products."),
                    display_on = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "DisplayOn: Controls the visibility of the entity. Options: None, FrontEnd, BackEnd, Both."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_system_role = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    default_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    value_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_methods",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the shipping method. Value generated never."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Name: Normalized parameterizable name for internal use."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: Human-readable version of the parameterizable name."),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Description: A detailed description of the shipping method."),
                    type = table.Column<string>(type: "text", nullable: false, comment: "Type: The type of shipping method (Standard, Express, Overnight, Pickup, FreeShipping)."),
                    active = table.Column<bool>(type: "boolean", nullable: false, comment: "Active: Indicates if the shipping method is active and available for selection."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    base_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false, comment: "BaseCost: The base cost of the shipping method in specified currency."),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, comment: "Currency: ISO 4217 currency code (USD, EUR, GBP, etc.)."),
                    estimated_days_min = table.Column<int>(type: "integer", nullable: true, comment: "EstimatedDaysMin: Minimum estimated delivery days (e.g., 5 for 5-7 days delivery)."),
                    estimated_days_max = table.Column<int>(type: "integer", nullable: true, comment: "EstimatedDaysMax: Maximum estimated delivery days (e.g., 7 for 5-7 days delivery)."),
                    max_weight = table.Column<decimal>(type: "numeric(18,2)", nullable: true, comment: "MaxWeight: Maximum weight eligible for base cost (exceed = 1.5x surcharge)."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    display_on = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "FirstName: The user's first name."),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "LastName: The user's last name."),
                    date_of_birth = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "DateOfBirth: The user's date of birth."),
                    profile_image_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, comment: "ProfileImagePath: The path to the user's profile image."),
                    last_sign_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "LastSignInAt: The timestamp of the user's last sign-in."),
                    last_sign_in_ip = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true, comment: "LastSignInIp: The IP address from which the user last signed in."),
                    current_sign_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "CurrentSignInAt: The timestamp of the user's current sign-in."),
                    current_sign_in_ip = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true, comment: "CurrentSignInIp: The IP address from which the user is currently signed in."),
                    sign_in_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "SignInCount: The total number of times the user has signed in."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "UserName: The user's chosen username."),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "NormalizedUserName: The normalized username for efficient lookups."),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Email: The user's email address."),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "NormalizedEmail: The normalized email for efficient lookups."),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true, comment: "PhoneNumber: The user's phone number."),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "states",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the state."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name: The full name of the state. Required."),
                    abbr = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true, comment: "Abbr: The abbreviation for the state (e.g., 'CA'). Optional."),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "CountryId: Foreign key to the Country entity."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_states_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "eshopdb",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "option_values",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the option value. Value generated never."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Name: Normalized parameterizable name for internal use."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: Human-readable version of the parameterizable name."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    option_type_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "OptionTypeId: Foreign key to the associated OptionType."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_option_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_option_values_option_types_option_type_id",
                        column: x => x.option_type_id,
                        principalSchema: "eshopdb",
                        principalTable: "option_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_option_types",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the product option type. Value generated never."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ProductId: Foreign key to the associated Product."),
                    option_type_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "OptionTypeId: Foreign key to the associated OptionType."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_option_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_option_types_option_types_option_type_id",
                        column: x => x.option_type_id,
                        principalSchema: "eshopdb",
                        principalTable: "option_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_option_types_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "eshopdb",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variants",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the product variant. Value generated never."),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ProductId: Foreign key to the associated Product."),
                    is_master = table.Column<bool>(type: "boolean", nullable: false, comment: "IsMaster: Indicates if this is the master variant."),
                    sku = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Sku: Stock Keeping Unit for the variant."),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Barcode: Unique code printed on product label for internal or store scanning."),
                    weight = table.Column<decimal>(type: "numeric(18,4)", nullable: true, comment: "Weight: The weight of the variant."),
                    height = table.Column<decimal>(type: "numeric(18,4)", nullable: true, comment: "Height: The height of the variant."),
                    width = table.Column<decimal>(type: "numeric(18,4)", nullable: true, comment: "Width: The width of the variant."),
                    depth = table.Column<decimal>(type: "numeric(18,4)", nullable: true, comment: "Depth: The depth of the variant."),
                    dimensions_unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "DimensionsUnit: The unit of measurement for dimensions (e.g., mm, cm)."),
                    weight_unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "WeightUnit: The unit of measurement for weight (e.g., g, kg)."),
                    track_inventory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "TrackInventory: Indicates if inventory should be tracked for this variant."),
                    cost_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true, comment: "CostPrice: The cost price of the variant."),
                    cost_currency = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "DeletedAt: Timestamp when the entity was soft-deleted."),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "DeletedBy: User who soft-deleted the entity."),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "IsDeleted: Indicates if the entity is soft-deleted."),
                    discontinue_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "DiscontinueOn: Date when the variant is discontinued."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true, comment: "RowVersion: Concurrency token for optimistic locking."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_variants_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "eshopdb",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_actions",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the promotion usage action."),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "PromotionId: Foreign key to the associated Promotion."),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Type: The type of promotion action (e.g., OrderDiscount, ItemDiscount)."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_actions_promotion_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "eshopdb",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_rules",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the promotion rule. Value generated never."),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "PromotionId: Foreign key to the associated Promotion."),
                    type = table.Column<string>(type: "text", nullable: false, comment: "Type: The type of the promotion rule (e.g., 'UserLoggedIn', 'ProductInCart')."),
                    value = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false, comment: "Value: The value associated with the rule (e.g., a product ID, a minimum quantity)."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_rules_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "eshopdb",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_usages",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "PromotionId: Identifier of the promotion this audit entry belongs to."),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Action: Name of the action performed (Created, Updated, Activated, etc.)."),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Description: Detailed explanation of the audit event."),
                    user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true, comment: "UserId: Identifier of the user who performed the action, if available."),
                    user_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UserEmail: Email of the user who performed the action, if available."),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true, comment: "IpAddress: IP address from which the action originated."),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "UserAgent: Client user-agent string associated with the action."),
                    changes_before = table.Column<string>(type: "jsonb", nullable: true, comment: "ChangesBefore: Dictionary snapshot of entity state before the action."),
                    changes_after = table.Column<string>(type: "jsonb", nullable: true, comment: "ChangesAfter: Dictionary snapshot of entity state after the action."),
                    metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Metadata: Additional contextual metadata for the audit entry."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_usages_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "eshopdb",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_property_types",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the product property. Value generated never."),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ProductId: Foreign key to the associated Product."),
                    property_type_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "PropertyId: Foreign key to the associated Property."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    property_type_value = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false, comment: "Value: The value of the property for this product."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_property_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_property_types_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "eshopdb",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_property_types_property_type_property_type_id",
                        column: x => x.property_type_id,
                        principalSchema: "eshopdb",
                        principalTable: "property_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "AssignedAt: The date and time when the entity was assigned. Nullable if not yet assigned."),
                    assigned_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "AssignedBy: The username of the person who assigned this entity. Nullable if unknown."),
                    assigned_to = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "AssignedTo: The username of the assignee. Nullable if not yet assigned."),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "eshopdb",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_sources",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the payment source. Value generated never."),
                    user_id = table.Column<string>(type: "text", nullable: false, comment: "UserId: Foreign key to the associated ApplicationUser."),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "PaymentMethodId: Foreign key to the associated PaymentMethod."),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Type: The type of payment source (e.g., 'CreditCard', 'PayPal')."),
                    last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true, comment: "Last4: The last four digits of the credit card number, if applicable."),
                    brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Brand: The brand of the credit card (e.g., 'Visa', 'MasterCard')."),
                    expiration_month = table.Column<int>(type: "integer", nullable: true, comment: "ExpirationMonth: The expiration month of the credit card."),
                    expiration_year = table.Column<int>(type: "integer", nullable: true, comment: "ExpirationYear: The expiration year of the credit card."),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, comment: "IsDefault: Indicates if this is the user's default payment source."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_sources", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_sources_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalSchema: "eshopdb",
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_sources_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the refresh token. Value generated never."),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "TokenHash: The hashed version of the refresh token."),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "ExpiresAt: The expiration date and time of the refresh token."),
                    created_by_ip = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, comment: "CreatedByIp: The IP address from which the token was created."),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "RevokedAt: The date and time when the refresh token was revoked."),
                    revoked_by_ip = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true, comment: "RevokedByIp: The IP address from which the token was revoked, if applicable."),
                    revoked_reason = table.Column<string>(type: "text", nullable: true),
                    token_family = table.Column<string>(type: "text", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "AssignedAt: The date and time when the entity was assigned. Nullable if not yet assigned."),
                    assigned_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "AssignedBy: The username of the person who assigned this entity. Nullable if unknown."),
                    assigned_to = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "AssignedTo: The username of the assignee. Nullable if not yet assigned."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the review. Value generated never."),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ProductId: Foreign key to the associated Product."),
                    user_id = table.Column<string>(type: "text", nullable: false, comment: "UserId: Foreign key to the associated ApplicationUser."),
                    rating = table.Column<int>(type: "integer", nullable: false, comment: "Rating: The star rating given by the user (e.g., 1-5)."),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Title: A short title for the review."),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Comment: The detailed review text provided by the user."),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Status: The current moderation status of the review (e.g., Pending, Approved, Rejected)."),
                    moderated_by = table.Column<string>(type: "text", nullable: true),
                    moderated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    moderation_notes = table.Column<string>(type: "text", nullable: true),
                    helpful_count = table.Column<int>(type: "integer", nullable: false),
                    not_helpful_count = table.Column<int>(type: "integer", nullable: false),
                    is_verified_purchase = table.Column<bool>(type: "boolean", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviews_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reviews_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "eshopdb",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "AssignedAt: The date and time when the entity was assigned. Nullable if not yet assigned."),
                    assigned_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "AssignedBy: The username of the person who assigned this entity. Nullable if unknown."),
                    assigned_to = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "AssignedTo: The username of the assignee. Nullable if not yet assigned."),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "eshopdb",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "eshopdb",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false, comment: "UserId: Foreign key to the associated ApplicationUser."),
                    role_id = table.Column<string>(type: "text", nullable: false, comment: "RoleId: Foreign key to the associated ApplicationRole."),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "AssignedAt: The date and time when the entity was assigned. Nullable if not yet assigned."),
                    assigned_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "AssignedBy: The username of the person who assigned this entity. Nullable if unknown."),
                    assigned_to = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "AssignedTo: The username of the assignee. Nullable if not yet assigned.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "eshopdb",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "eshopdb",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false, comment: "UserId: Foreign key to the associated ApplicationUser."),
                    login_provider = table.Column<string>(type: "text", nullable: false, comment: "LoginProvider: The login provider for the user (e.g., 'Google', 'Facebook')."),
                    name = table.Column<string>(type: "text", nullable: false, comment: "Name: The name of the user token."),
                    value = table.Column<string>(type: "text", nullable: true, comment: "Value: The value of the user token.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the user address. Value generated never."),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "FirstName: The first name of the recipient. Required."),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "LastName: The last name of the recipient. Required."),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Label: A user-defined label for the address (e.g., 'Home', 'Work')."),
                    quick_checkout = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Type: The type of address (e.g., Shipping, Billing). Stored as a string."),
                    address1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Address1: The primary line of the street address."),
                    address2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Address2: The secondary line of the street address (e.g., apartment, suite)."),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "City: The city of the address."),
                    zipcode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, comment: "Zipcode: The postal code of the address."),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Phone: The phone number associated with the address."),
                    company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Company: The company name associated with the address."),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_addresses_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_addresses_country_country_id",
                        column: x => x.country_id,
                        principalSchema: "eshopdb",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_customer_addresses_state_state_id",
                        column: x => x.state_id,
                        principalSchema: "eshopdb",
                        principalTable: "states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stock_locations",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the stock location."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name: The internal system name for the stock location (e.g., 'main-warehouse', 'nyc-store')."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: The human-readable display name for the stock location (e.g., 'Main Warehouse', 'NYC Retail Store')."),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Active: Indicates if the stock location is active. Required, defaults to true."),
                    @default = table.Column<bool>(name: "default", type: "boolean", nullable: false, defaultValue: false, comment: "Default: Indicates if this is the default stock location. Required, defaults to false."),
                    address1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Address1: The primary line of the street address."),
                    address2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Address2: The secondary line of the street address."),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "City: The city of the address."),
                    zipcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Zipcode: The postal code of the address."),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Phone: The phone number associated with the address."),
                    company = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Company: The company name associated with the stock location address."),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Email: The email address associated with the stock location."),
                    type = table.Column<int>(type: "integer", nullable: false, comment: "Type: Enum indicating location type (Warehouse, RetailStore, Both). Stored as string for flexibility."),
                    ship_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "ShipEnabled: Whether this location can ship orders. Defaults to true."),
                    pickup_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "PickupEnabled: Whether this location supports store pickup. Defaults to false."),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true, comment: "Latitude: Geographic latitude coordinate (-90 to 90) for distance calculations."),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true, comment: "Longitude: Geographic longitude coordinate (-180 to 180) for distance calculations."),
                    operating_hours = table.Column<string>(type: "jsonb", nullable: true, comment: "OperatingHours: JSON dictionary of operating hours by day of week (e.g., {\"Monday\": \"09:00-17:00\"})."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_locations_country_country_id",
                        column: x => x.country_id,
                        principalSchema: "eshopdb",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_stock_locations_state_state_id",
                        column: x => x.state_id,
                        principalSchema: "eshopdb",
                        principalTable: "states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stores",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the store. Value generated never."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Name: Normalized parameterizable name for internal use."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: Human-readable version of the parameterizable name."),
                    code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Code: Unique store identifier used as subdomain or slug (e.g., 'myshop', 'fashion-store'). Required, uppercase normalized."),
                    url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Url: The store's base URL (e.g., 'shop.example.com', 'example.com/shop'). Required and must be unique."),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, comment: "DefaultCurrency: The default currency code for transactions (e.g., 'USD', 'EUR', 'GBP', 'VND'). Required, defaults to 'USD'."),
                    default_locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, comment: "DefaultLocale: The default language/locale for the store (e.g., 'en', 'en-US', 'fr'). Required, defaults to 'en'."),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Timezone: The store's timezone for scheduling and time-based operations (e.g., 'UTC', 'America/New_York'). Required, defaults to 'UTC'."),
                    @default = table.Column<bool>(name: "default", type: "boolean", nullable: false, comment: "Default: Indicates if this is the default store for the system. Only one store should be marked as default."),
                    mail_from_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "MailFromAddress: The email address used as the 'From' address for transactional emails sent by the store."),
                    customer_support_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "CustomerSupportEmail: The email address for customer support inquiries and contact information."),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "MetaTitle: Optional SEO title for the entity."),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "MetaDescription: Optional SEO description for the entity."),
                    meta_keywords = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "MetaKeywords: Optional SEO keywords (comma-separated)."),
                    seo_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "SeoTitle: Alternative SEO title for improved search engine optimization."),
                    available = table.Column<bool>(type: "boolean", nullable: false, comment: "Available: Indicates if the store is currently available/active for customer access. Defaults to true."),
                    guest_checkout_allowed = table.Column<bool>(type: "boolean", nullable: false, comment: "GuestCheckoutAllowed: Indicates whether customers can proceed to checkout without creating an account. Defaults to true."),
                    password_protected = table.Column<bool>(type: "boolean", nullable: false, comment: "PasswordProtected: Indicates if the storefront is protected by a password. Useful for private/beta stores."),
                    storefront_password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "StorefrontPassword: The password required to access a password-protected store (typically hashed/encrypted)."),
                    facebook = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Facebook: Storefront's Facebook profile URL or handle for social media integration."),
                    instagram = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Instagram: Storefront's Instagram profile URL or handle for social media integration."),
                    twitter = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Twitter: Storefront's Twitter/X profile URL or handle for social media integration."),
                    address1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Address1: Primary address line for the store's physical location (street address)."),
                    address2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Address2: Secondary address line for the store's physical location (apartment, suite, etc.)."),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "City: The city or municipality where the store is located."),
                    zipcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Zipcode: The postal code or ZIP code for the store's physical address."),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Phone: The primary phone number for the store's physical location."),
                    company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Company: The legal company name associated with the store."),
                    country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "DeletedAt: The timestamp when the store was soft deleted (null if not deleted). Enables recovery and audit trails."),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stores", x => x.id);
                    table.UniqueConstraint("AK_Store_Code", x => x.code);
                    table.ForeignKey(
                        name: "fk_stores_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "eshopdb",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stores_states_state_id",
                        column: x => x.state_id,
                        principalSchema: "eshopdb",
                        principalTable: "states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prices",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the price. Value generated never."),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "VariantId: Foreign key to the associated Product Variant."),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true, comment: "Amount: The current price of the product variant."),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, comment: "Currency: The currency of the price (e.g., 'USD', 'EUR')."),
                    compare_at_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true, comment: "CompareAtAmount: The original price for comparison, indicating a sale or discount."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_prices_variant_variant_id",
                        column: x => x.variant_id,
                        principalSchema: "eshopdb",
                        principalTable: "variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the product image."),
                    content_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "ContentType: MIME type of the image."),
                    width = table.Column<int>(type: "integer", nullable: true, comment: "Width: Image width in pixels."),
                    height = table.Column<int>(type: "integer", nullable: true, comment: "Height: Image height in pixels."),
                    dimensions_unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, comment: "DimensionsUnit: Unit of measurement."),
                    embedding_open_clip = table.Column<Vector>(type: "vector(1024)", nullable: true, comment: "EmbeddingOpenCLIP: 1024-dimensional OpenCLIP vector for visual similarity search."),
                    embedding_open_clip_model = table.Column<string>(type: "text", nullable: true),
                    embedding_open_clip_generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    embedding_dino_v2 = table.Column<Vector>(type: "vector(1024)", nullable: true, comment: "EmbeddingDinoV2: Embedding vector generated by DINOv2 model for visual similarity search."),
                    embedding_dino_v2model = table.Column<string>(type: "text", nullable: true),
                    embedding_dino_v2generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    embedding_blip2 = table.Column<Vector>(type: "vector(768)", nullable: true, comment: "EmbeddingBLIP2: Embedding vector generated by BLIP-2 model for visual similarity search."),
                    embedding_blip2model = table.Column<string>(type: "text", nullable: true),
                    embedding_blip2generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "ProductId: Foreign key to Product."),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "VariantId: Foreign key to Variant."),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Type: Image type (Default, Thumbnail, Gallery)."),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false, comment: "Url: The URL of the image asset."),
                    alt = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Alt: Alternative text for the image."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_images_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "eshopdb",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_images_variant_variant_id",
                        column: x => x.variant_id,
                        principalSchema: "eshopdb",
                        principalTable: "variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variant_option_values",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the option value variant. Value generated never."),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "VariantId: Foreign key to the associated Product Variant."),
                    option_value_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "OptionValueId: Foreign key to the associated OptionValue."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variant_option_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_variant_option_values_option_values_option_value_id",
                        column: x => x.option_value_id,
                        principalSchema: "eshopdb",
                        principalTable: "option_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_variant_option_values_variants_variant_id",
                        column: x => x.variant_id,
                        principalSchema: "eshopdb",
                        principalTable: "variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_rule_users",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the promotion rule user. Value generated never."),
                    promotion_rule_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "PromotionRuleId: Foreign key to the associated PromotionRule."),
                    user_id = table.Column<string>(type: "text", nullable: false, comment: "UserId: Foreign key to the associated ApplicationUser."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_rule_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_rule_users_promotion_rules_promotion_rule_id",
                        column: x => x.promotion_rule_id,
                        principalSchema: "eshopdb",
                        principalTable: "promotion_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_promotion_rule_users_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_items",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the stock item."),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Foreign key to the associated Variant."),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Foreign key to the associated StockLocation."),
                    sku = table.Column<string>(type: "text", nullable: false),
                    quantity_on_hand = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "QuantityOnHand: The current quantity of the variant in stock."),
                    quantity_reserved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "QuantityReserved: The quantity of the variant reserved for orders."),
                    backorderable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Backorderable: Indicates if the variant can be backordered."),
                    max_backorder_quantity = table.Column<int>(type: "integer", nullable: false),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true, comment: "RowVersion: Used for optimistic concurrency control.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_items_stock_locations_stock_location_id",
                        column: x => x.stock_location_id,
                        principalSchema: "eshopdb",
                        principalTable: "stock_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stock_items_variants_variant_id",
                        column: x => x.variant_id,
                        principalSchema: "eshopdb",
                        principalTable: "variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfers",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the stock transfer."),
                    source_location_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Foreign key to the source location (null for supplier receipts)."),
                    destination_location_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Foreign key to the destination location (required)."),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Number: Auto-generated transfer number for reference."),
                    reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Reference: Optional reference code (e.g., purchase order number)."),
                    state = table.Column<int>(type: "integer", nullable: false, comment: "The current state of the stock transfer (e.g., Pending, Finalized)."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_transfers", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_transfers_stock_locations_destination_location_id",
                        column: x => x.destination_location_id,
                        principalSchema: "eshopdb",
                        principalTable: "stock_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_transfers_stock_locations_source_location_id",
                        column: x => x.source_location_id,
                        principalSchema: "eshopdb",
                        principalTable: "stock_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the order. Value generated never."),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "StoreId: Foreign key to the associated Storefront."),
                    user_id = table.Column<string>(type: "text", nullable: true, comment: "UserId: Foreign key to the associated ApplicationUser."),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "PromotionId: Foreign key to the associated Promotion."),
                    shipping_method_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "ShippingMethodId: Foreign key to the associated ShippingMethod."),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Number: Unique order number."),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "State: Current state of the order (e.g., Cart, Complete)."),
                    item_total_cents = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m, comment: "ItemTotalCents: Total amount of all line items in cents."),
                    shipment_total_cents = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m, comment: "ShipmentTotalCents: Total amount for shipping in cents."),
                    total_cents = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m, comment: "TotalCents: Grand total of the order in cents."),
                    adjustment_total_cents = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m, comment: "AdjustmentTotalCents: Total amount for adjustments (e.g., discounts) in cents."),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD", comment: "Currency: The currency of the order (e.g., USD, EUR)."),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Email: Customer's email address."),
                    Name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "SpecialInstructions: Any special instructions for the order."),
                    promo_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "PromoCode: Promotional code applied to the order."),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "CompletedAt: Timestamp when the order was completed."),
                    canceled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "CanceledAt: Timestamp when the order was canceled."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    ship_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bill_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true, comment: "RowVersion: Used for optimistic concurrency control.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_customer_addresses_bill_address_id",
                        column: x => x.bill_address_id,
                        principalSchema: "eshopdb",
                        principalTable: "customer_addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_customer_addresses_ship_address_id",
                        column: x => x.ship_address_id,
                        principalSchema: "eshopdb",
                        principalTable: "customer_addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_promotion_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "eshopdb",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_orders_store_store_id",
                        column: x => x.store_id,
                        principalSchema: "eshopdb",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "eshopdb",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "store_payment_methods",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the store payment method. Value generated never."),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "StoreId: Foreign key to the associated Storefront."),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "PaymentMethodId: Foreign key to the associated PaymentMethod."),
                    available = table.Column<bool>(type: "boolean", nullable: false, comment: "Available: Indicates if the payment method is available for this store."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_payment_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_payment_methods_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalSchema: "eshopdb",
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_store_payment_methods_store_store_id",
                        column: x => x.store_id,
                        principalSchema: "eshopdb",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_products",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the store product link. Value generated never."),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "StoreId: Foreign key to the associated Storefront."),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ProductId: Foreign key to the associated Product."),
                    visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Visible: Indicates if the product is visible in the store."),
                    featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Featured: Indicates if the product is featured in the store."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_products_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "eshopdb",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_store_products_store_store_id",
                        column: x => x.store_id,
                        principalSchema: "eshopdb",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_shipping_methods",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the store shipping method. Value generated never."),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "StoreId: Foreign key to the associated Storefront."),
                    shipping_method_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ShippingMethodId: Foreign key to the associated ShippingMethod."),
                    available = table.Column<bool>(type: "boolean", nullable: false, comment: "Available: Indicates if the shipping method is available for this store."),
                    store_base_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true, comment: "StoreBaseCost: The base cost of the shipping method for this specific store."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_shipping_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_shipping_methods_shipping_methods_shipping_method_id",
                        column: x => x.shipping_method_id,
                        principalSchema: "eshopdb",
                        principalTable: "shipping_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_store_shipping_methods_store_store_id",
                        column: x => x.store_id,
                        principalSchema: "eshopdb",
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "taxonomies",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the taxonomy. Value generated never."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name: The unique name of the entity."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: Human-readable version of the parameterizable name."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxonomies", x => x.id);
                    table.ForeignKey(
                        name: "fk_taxonomies_store_store_id",
                        column: x => x.store_id,
                        principalSchema: "eshopdb",
                        principalTable: "stores",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "line_items",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the line item. Value generated never."),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "OrderId: Foreign key to the associated Order."),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "VariantId: Foreign key to the associated Product Variant."),
                    quantity = table.Column<int>(type: "integer", nullable: false, comment: "Quantity: Number of units of the product variant."),
                    price_cents = table.Column<long>(type: "bigint", nullable: false, comment: "PriceCents: Price of a single unit in cents at the time of order."),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, comment: "Currency: The currency of the line item."),
                    captured_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "CapturedName: Name of the product variant at the time of order."),
                    captured_sku = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "CapturedSku: SKU of the product variant at the time of order."),
                    is_promotional = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "IsPromotional: Indicates if this line item was part of a promotion."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_line_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_line_items_order_order_id",
                        column: x => x.order_id,
                        principalSchema: "eshopdb",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_line_items_variants_variant_id",
                        column: x => x.variant_id,
                        principalSchema: "eshopdb",
                        principalTable: "variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_adjustments",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the order adjustment. Value generated never."),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "OrderId: Foreign key to the associated Order."),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    eligible = table.Column<bool>(type: "boolean", nullable: false),
                    mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "PromotionId: Foreign key to the associated Promotion."),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false, comment: "AmountCents: The adjustment amount in cents."),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false, comment: "Description: Description of the adjustment."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_adjustments_order_order_id",
                        column: x => x.order_id,
                        principalSchema: "eshopdb",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_adjustments_promotion_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "eshopdb",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the payment. Value generated never."),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "OrderId: Foreign key to the associated Order."),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "PaymentMethodId: Foreign key to the associated PaymentMethod."),
                    amount_cents = table.Column<decimal>(type: "numeric", nullable: false, comment: "AmountCents: The payment amount in cents."),
                    currency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "USD", comment: "Currency: The currency of the payment."),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "State: Current state of the payment (e.g., Pending, Completed)."),
                    payment_method_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "PaymentMethodType: The type of payment method used."),
                    reference_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "ReferenceTransactionId: The transaction ID from the payment gateway."),
                    gateway_auth_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "GatewayAuthCode: Authorization code from payment gateway."),
                    gateway_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "GatewayErrorCode: Error code from payment gateway."),
                    authorized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "AuthorizedAt: Timestamp when the payment was authorized."),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "CapturedAt: Timestamp when the payment was captured."),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "VoidedAt: Timestamp when the payment was voided."),
                    failure_reason = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true, comment: "FailureReason: Reason for payment failure."),
                    idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "IdempotencyKey: Key for idempotent payment processing."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true, comment: "RowVersion: Used for optimistic concurrency control.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "eshopdb",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payments_payment_method_payment_method_id",
                        column: x => x.payment_method_id,
                        principalSchema: "eshopdb",
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tracking_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    allocated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    picking_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    picked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    packed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ready_to_ship_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    shipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    package_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    shipping_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipments", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipments_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "eshopdb",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shipments_shipping_method_shipping_method_id",
                        column: x => x.shipping_method_id,
                        principalSchema: "eshopdb",
                        principalTable: "shipping_methods",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_shipments_stock_locations_stock_location_id",
                        column: x => x.stock_location_id,
                        principalSchema: "eshopdb",
                        principalTable: "stock_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "taxa",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the taxon. Value generated never."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Name: The unique name of the entity."),
                    presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Presentation: Human-readable version of the parameterizable name."),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Description: A detailed description of the taxon."),
                    permalink = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Permalink: The unique, URL-friendly identifier for the taxon."),
                    pretty_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "PrettyName: A user-friendly name for the taxon."),
                    hide_from_nav = table.Column<bool>(type: "boolean", nullable: false, comment: "HideFromNav: Indicates if the taxon should be hidden from navigation menus."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    lft = table.Column<int>(type: "integer", nullable: false, comment: "Lft: Left value for the nested set model, used for hierarchical ordering."),
                    rgt = table.Column<int>(type: "integer", nullable: false, comment: "Rgt: Right value for the nested set model, used for hierarchical ordering."),
                    depth = table.Column<int>(type: "integer", nullable: false, comment: "Depth: The depth of the taxon in the hierarchy."),
                    automatic = table.Column<bool>(type: "boolean", nullable: false, comment: "Automatic: Indicates if the taxon's product associations are managed automatically by rules."),
                    rules_match_policy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "RulesMatchPolicy: Defines how multiple rules are applied to determine product association (e.g., 'All', 'Any')."),
                    sort_order = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "SortOrder: The order in which the taxon should appear in lists."),
                    marked_for_regenerate_taxon_products = table.Column<bool>(type: "boolean", nullable: false, comment: "MarkedForRegenerateTaxonProducts: Indicates if the taxon's product associations need to be re-evaluated."),
                    meta_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "MetaTitle: Optional SEO title for the entity."),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "MetaDescription: Optional SEO description for the entity."),
                    meta_keywords = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "MetaKeywords: Optional SEO keywords (comma-separated)."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON."),
                    taxonomy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 0L, comment: "Version: Optimistic concurrency token, incremented on updates."),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxa", x => x.id);
                    table.ForeignKey(
                        name: "fk_taxa_taxa_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "eshopdb",
                        principalTable: "taxa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_taxa_taxonomy_taxonomy_id",
                        column: x => x.taxonomy_id,
                        principalSchema: "eshopdb",
                        principalTable: "taxonomies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "line_item_adjustments",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the line item adjustment. Value generated never."),
                    line_item_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "LineItemId: Foreign key to the associated LineItem."),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "PromotionId: Foreign key to the associated Promotion."),
                    amount_cents = table.Column<long>(type: "bigint", nullable: false, comment: "AmountCents: The adjustment amount in cents."),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false, comment: "Description: Description of the adjustment."),
                    eligible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_line_item_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_line_item_adjustments_line_item_line_item_id",
                        column: x => x.line_item_id,
                        principalSchema: "eshopdb",
                        principalTable: "line_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_line_item_adjustments_promotion_promotion_id",
                        column: x => x.promotion_id,
                        principalSchema: "eshopdb",
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inventory_units",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    pending = table.Column<bool>(type: "boolean", nullable: false),
                    state_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    original_return_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_units_line_items_line_item_id",
                        column: x => x.line_item_id,
                        principalSchema: "eshopdb",
                        principalTable: "line_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_units_shipment_shipment_id",
                        column: x => x.shipment_id,
                        principalSchema: "eshopdb",
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_units_variants_variant_id",
                        column: x => x.variant_id,
                        principalSchema: "eshopdb",
                        principalTable: "variants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    originator = table.Column<string>(type: "text", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    originator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stock_transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_shipments_originator_id",
                        column: x => x.originator_id,
                        principalSchema: "eshopdb",
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_stock_movements_stock_item_stock_item_id",
                        column: x => x.stock_item_id,
                        principalSchema: "eshopdb",
                        principalTable: "stock_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stock_movements_stock_transfer_stock_transfer_id",
                        column: x => x.stock_transfer_id,
                        principalSchema: "eshopdb",
                        principalTable: "stock_transfers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "classification",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the classification. Value generated never."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: The display order of the classification within a taxon's product list."),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ProductId: Foreign key to the associated Product."),
                    taxon_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "TaxonId: Foreign key to the associated Taxon."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classification", x => x.id);
                    table.ForeignKey(
                        name: "fk_classification_product_product_id",
                        column: x => x.product_id,
                        principalSchema: "eshopdb",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_classification_taxon_taxon_id",
                        column: x => x.taxon_id,
                        principalSchema: "eshopdb",
                        principalTable: "taxa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_rule_taxons",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the promotion rule taxon. Value generated never."),
                    promotion_rule_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "PromotionRuleId: Foreign key to the associated PromotionRule."),
                    taxon_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "TaxonId: Foreign key to the associated Taxon."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_rule_taxons", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_rule_taxons_promotion_rules_promotion_rule_id",
                        column: x => x.promotion_rule_id,
                        principalSchema: "eshopdb",
                        principalTable: "promotion_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_promotion_rule_taxons_taxa_taxon_id",
                        column: x => x.taxon_id,
                        principalSchema: "eshopdb",
                        principalTable: "taxa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "taxon_images",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the taxon image. Value generated never."),
                    taxon_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "TaxonId: Foreign key to the associated Taxon."),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Type: The type of the image (e.g., 'default', 'square')."),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, comment: "Url: The URL of the image asset."),
                    alt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Alt: Alternative text for the image."),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Position: Sortable ordering of the entity, minimum value is 1."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record."),
                    public_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Public key-value metadata for the entity, stored as JSON."),
                    private_metadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Private key-value metadata for the entity, stored as JSON.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxon_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_taxon_images_taxon_taxon_id",
                        column: x => x.taxon_id,
                        principalSchema: "eshopdb",
                        principalTable: "taxa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "taxon_rules",
                schema: "eshopdb",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id: Unique identifier for the taxon rule. Value generated never."),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Type: The type of the rule (e.g., 'product_name', 'product_property')."),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Value: The value to match against for the rule."),
                    match_policy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "MatchPolicy: The policy for matching (e.g., 'is_equal_to', 'contains')."),
                    property_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "PropertyName: The name of the product property if the rule type is 'product_property'."),
                    taxon_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "TaxonId: Foreign key to the associated Taxon."),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "CreatedAt: Timestamp of when the record was created."),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "CreatedBy: User who initially created this record."),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "UpdatedAt: Timestamp of when the record was last updated."),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "UpdatedBy: User who last updated this record.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxon_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_taxon_rules_taxon_taxon_id",
                        column: x => x.taxon_id,
                        principalSchema: "eshopdb",
                        principalTable: "taxa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_by",
                schema: "eshopdb",
                table: "audit_logs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_classification_created_by",
                schema: "eshopdb",
                table: "classification",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_classification_position",
                schema: "eshopdb",
                table: "classification",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_classification_product_id",
                schema: "eshopdb",
                table: "classification",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_classification_product_id_taxon_id",
                schema: "eshopdb",
                table: "classification",
                columns: new[] { "product_id", "taxon_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_classification_taxon_id",
                schema: "eshopdb",
                table: "classification",
                column: "taxon_id");

            migrationBuilder.CreateIndex(
                name: "ix_countries_created_by",
                schema: "eshopdb",
                table: "countries",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_country_id",
                schema: "eshopdb",
                table: "customer_addresses",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_created_by",
                schema: "eshopdb",
                table: "customer_addresses",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_state_id",
                schema: "eshopdb",
                table: "customer_addresses",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_user_id",
                schema: "eshopdb",
                table: "customer_addresses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_units_line_item_id",
                schema: "eshopdb",
                table: "inventory_units",
                column: "line_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_units_shipment_id",
                schema: "eshopdb",
                table: "inventory_units",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_units_state",
                schema: "eshopdb",
                table: "inventory_units",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_units_variant_id",
                schema: "eshopdb",
                table: "inventory_units",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_units_variant_id_state",
                schema: "eshopdb",
                table: "inventory_units",
                columns: new[] { "variant_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_line_item_adjustments_created_by",
                schema: "eshopdb",
                table: "line_item_adjustments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_line_item_adjustments_line_item_id",
                schema: "eshopdb",
                table: "line_item_adjustments",
                column: "line_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_line_item_adjustments_promotion_id",
                schema: "eshopdb",
                table: "line_item_adjustments",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "ix_line_items_created_by",
                schema: "eshopdb",
                table: "line_items",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_line_items_order_id",
                schema: "eshopdb",
                table: "line_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_line_items_variant_id",
                schema: "eshopdb",
                table: "line_items",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_option_types_created_by",
                schema: "eshopdb",
                table: "option_types",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_option_types_name",
                schema: "eshopdb",
                table: "option_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_option_types_position",
                schema: "eshopdb",
                table: "option_types",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_option_values_created_by",
                schema: "eshopdb",
                table: "option_values",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_option_values_option_type_id_name",
                schema: "eshopdb",
                table: "option_values",
                columns: new[] { "option_type_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_option_values_position",
                schema: "eshopdb",
                table: "option_values",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_order_adjustments_created_by",
                schema: "eshopdb",
                table: "order_adjustments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_order_adjustments_order_id",
                schema: "eshopdb",
                table: "order_adjustments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_adjustments_promotion_id",
                schema: "eshopdb",
                table: "order_adjustments",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_bill_address_id",
                schema: "eshopdb",
                table: "orders",
                column: "bill_address_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_completed_at",
                schema: "eshopdb",
                table: "orders",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "ix_orders_created_by",
                schema: "eshopdb",
                table: "orders",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_orders_number",
                schema: "eshopdb",
                table: "orders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_promotion_id",
                schema: "eshopdb",
                table: "orders",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_ship_address_id",
                schema: "eshopdb",
                table: "orders",
                column: "ship_address_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_shipping_method_id",
                schema: "eshopdb",
                table: "orders",
                column: "shipping_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_state",
                schema: "eshopdb",
                table: "orders",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_orders_store_id",
                schema: "eshopdb",
                table: "orders",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_user_id",
                schema: "eshopdb",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_created_by",
                schema: "eshopdb",
                table: "payment_methods",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_name",
                schema: "eshopdb",
                table: "payment_methods",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_position",
                schema: "eshopdb",
                table: "payment_methods",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_payment_sources_created_by",
                schema: "eshopdb",
                table: "payment_sources",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_payment_sources_is_default",
                schema: "eshopdb",
                table: "payment_sources",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "ix_payment_sources_payment_method_id",
                schema: "eshopdb",
                table: "payment_sources",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_sources_user_id",
                schema: "eshopdb",
                table: "payment_sources",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_created_by",
                schema: "eshopdb",
                table: "payments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_payments_idempotency_key",
                schema: "eshopdb",
                table: "payments",
                column: "idempotency_key");

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_id",
                schema: "eshopdb",
                table: "payments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_payment_method_id",
                schema: "eshopdb",
                table: "payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_reference_transaction_id",
                schema: "eshopdb",
                table: "payments",
                column: "reference_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_state",
                schema: "eshopdb",
                table: "payments",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_area_resource_action",
                schema: "eshopdb",
                table: "permissions",
                columns: new[] { "area", "resource", "action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_created_by",
                schema: "eshopdb",
                table: "permissions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_name",
                schema: "eshopdb",
                table: "permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prices_created_by",
                schema: "eshopdb",
                table: "prices",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_prices_currency",
                schema: "eshopdb",
                table: "prices",
                column: "currency");

            migrationBuilder.CreateIndex(
                name: "ix_prices_variant_id",
                schema: "eshopdb",
                table: "prices",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_created_by",
                schema: "eshopdb",
                table: "product_images",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_embedding_blip2_hnsw",
                schema: "eshopdb",
                table: "product_images",
                column: "embedding_blip2")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_embedding_dinov2_hnsw",
                schema: "eshopdb",
                table: "product_images",
                column: "embedding_dino_v2")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_embedding_openclip_hnsw",
                schema: "eshopdb",
                table: "product_images",
                column: "embedding_open_clip")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_position",
                schema: "eshopdb",
                table: "product_images",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id",
                schema: "eshopdb",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_variant_id",
                schema: "eshopdb",
                table: "product_images",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_option_types_created_by",
                schema: "eshopdb",
                table: "product_option_types",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_option_types_option_type_id",
                schema: "eshopdb",
                table: "product_option_types",
                column: "option_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_option_types_position",
                schema: "eshopdb",
                table: "product_option_types",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_product_option_types_product_id",
                schema: "eshopdb",
                table: "product_option_types",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_option_types_product_id_option_type_id",
                schema: "eshopdb",
                table: "product_option_types",
                columns: new[] { "product_id", "option_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_property_types_created_by",
                schema: "eshopdb",
                table: "product_property_types",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_product_property_types_position",
                schema: "eshopdb",
                table: "product_property_types",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_product_property_types_product_id",
                schema: "eshopdb",
                table: "product_property_types",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_property_types_product_id_property_type_id",
                schema: "eshopdb",
                table: "product_property_types",
                columns: new[] { "product_id", "property_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_property_types_property_type_id",
                schema: "eshopdb",
                table: "product_property_types",
                column: "property_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_created_by",
                schema: "eshopdb",
                table: "products",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_products_name",
                schema: "eshopdb",
                table: "products",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_slug",
                schema: "eshopdb",
                table: "products",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_actions_created_by",
                schema: "eshopdb",
                table: "promotion_actions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_actions_promotion_id",
                schema: "eshopdb",
                table: "promotion_actions",
                column: "promotion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_rule_taxons_created_by",
                schema: "eshopdb",
                table: "promotion_rule_taxons",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_rule_taxons_promotion_rule_id",
                schema: "eshopdb",
                table: "promotion_rule_taxons",
                column: "promotion_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_rule_taxons_taxon_id",
                schema: "eshopdb",
                table: "promotion_rule_taxons",
                column: "taxon_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_rule_users_created_by",
                schema: "eshopdb",
                table: "promotion_rule_users",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_rule_users_promotion_rule_id",
                schema: "eshopdb",
                table: "promotion_rule_users",
                column: "promotion_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_rule_users_user_id",
                schema: "eshopdb",
                table: "promotion_rule_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_rules_created_by",
                schema: "eshopdb",
                table: "promotion_rules",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_rules_promotion_id",
                schema: "eshopdb",
                table: "promotion_rules",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_usages_action",
                schema: "eshopdb",
                table: "promotion_usages",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_usages_created_at",
                schema: "eshopdb",
                table: "promotion_usages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_usages_promotion_id",
                schema: "eshopdb",
                table: "promotion_usages",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "ix_promotion_usages_promotion_id_created_at",
                schema: "eshopdb",
                table: "promotion_usages",
                columns: new[] { "promotion_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_promotions_active",
                schema: "eshopdb",
                table: "promotions",
                column: "active");

            migrationBuilder.CreateIndex(
                name: "ix_promotions_created_by",
                schema: "eshopdb",
                table: "promotions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_promotions_name",
                schema: "eshopdb",
                table: "promotions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotions_promotion_code",
                schema: "eshopdb",
                table: "promotions",
                column: "promotion_code");

            migrationBuilder.CreateIndex(
                name: "ix_promotions_starts_at_expires_at_active",
                schema: "eshopdb",
                table: "promotions",
                columns: new[] { "starts_at", "expires_at", "active" });

            migrationBuilder.CreateIndex(
                name: "ix_property_types_created_by",
                schema: "eshopdb",
                table: "property_types",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_property_types_name",
                schema: "eshopdb",
                table: "property_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_property_types_position",
                schema: "eshopdb",
                table: "property_types",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_assigned_to",
                schema: "eshopdb",
                table: "refresh_tokens",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_created_by",
                schema: "eshopdb",
                table: "refresh_tokens",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "eshopdb",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "eshopdb",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_created_by",
                schema: "eshopdb",
                table: "reviews",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_product_id",
                schema: "eshopdb",
                table: "reviews",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_status",
                schema: "eshopdb",
                table: "reviews",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_user_id",
                schema: "eshopdb",
                table: "reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_assigned_to",
                schema: "eshopdb",
                table: "role_claims",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                schema: "eshopdb",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_created_by",
                schema: "eshopdb",
                table: "roles",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_roles_is_default",
                schema: "eshopdb",
                table: "roles",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "ix_roles_is_system_role",
                schema: "eshopdb",
                table: "roles",
                column: "is_system_role");

            migrationBuilder.CreateIndex(
                name: "ix_roles_priority",
                schema: "eshopdb",
                table: "roles",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "eshopdb",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_settings_key",
                schema: "eshopdb",
                table: "settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipments_created_by",
                schema: "eshopdb",
                table: "shipments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_number",
                schema: "eshopdb",
                table: "shipments",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipments_order_id",
                schema: "eshopdb",
                table: "shipments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_shipping_method_id",
                schema: "eshopdb",
                table: "shipments",
                column: "shipping_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_state",
                schema: "eshopdb",
                table: "shipments",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_stock_location_id",
                schema: "eshopdb",
                table: "shipments",
                column: "stock_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipping_methods_created_by",
                schema: "eshopdb",
                table: "shipping_methods",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_shipping_methods_name",
                schema: "eshopdb",
                table: "shipping_methods",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipping_methods_position",
                schema: "eshopdb",
                table: "shipping_methods",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_states_country_id",
                schema: "eshopdb",
                table: "states",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_states_created_by",
                schema: "eshopdb",
                table: "states",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_created_by",
                schema: "eshopdb",
                table: "stock_items",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_quantity_reserved_quantity_on_hand",
                schema: "eshopdb",
                table: "stock_items",
                columns: new[] { "quantity_reserved", "quantity_on_hand" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_stock_location_id",
                schema: "eshopdb",
                table: "stock_items",
                column: "stock_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_stock_location_id_variant_id",
                schema: "eshopdb",
                table: "stock_items",
                columns: new[] { "stock_location_id", "variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_variant_id",
                schema: "eshopdb",
                table: "stock_items",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_locations_active",
                schema: "eshopdb",
                table: "stock_locations",
                column: "active");

            migrationBuilder.CreateIndex(
                name: "ix_stock_locations_country_id",
                schema: "eshopdb",
                table: "stock_locations",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_locations_created_by",
                schema: "eshopdb",
                table: "stock_locations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_stock_locations_default",
                schema: "eshopdb",
                table: "stock_locations",
                column: "default");

            migrationBuilder.CreateIndex(
                name: "ix_stock_locations_name",
                schema: "eshopdb",
                table: "stock_locations",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_locations_state_id",
                schema: "eshopdb",
                table: "stock_locations",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocation_Coordinates",
                schema: "eshopdb",
                table: "stock_locations",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLocation_PickupEnabled_IsDeleted",
                schema: "eshopdb",
                table: "stock_locations",
                columns: new[] { "pickup_enabled", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLocation_ShipEnabled_IsDeleted",
                schema: "eshopdb",
                table: "stock_locations",
                columns: new[] { "ship_enabled", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_action",
                schema: "eshopdb",
                table: "stock_movements",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_created_by",
                schema: "eshopdb",
                table: "stock_movements",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_originator",
                schema: "eshopdb",
                table: "stock_movements",
                column: "originator");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_originator_id",
                schema: "eshopdb",
                table: "stock_movements",
                column: "originator_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_stock_item_id",
                schema: "eshopdb",
                table: "stock_movements",
                column: "stock_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_stock_transfer_id",
                schema: "eshopdb",
                table: "stock_movements",
                column: "stock_transfer_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_created_at",
                schema: "eshopdb",
                table: "stock_transfers",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_created_by",
                schema: "eshopdb",
                table: "stock_transfers",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_destination_location_id",
                schema: "eshopdb",
                table: "stock_transfers",
                column: "destination_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_number",
                schema: "eshopdb",
                table: "stock_transfers",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_reference",
                schema: "eshopdb",
                table: "stock_transfers",
                column: "reference");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_source_location_id",
                schema: "eshopdb",
                table: "stock_transfers",
                column: "source_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_payment_methods_created_by",
                schema: "eshopdb",
                table: "store_payment_methods",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_store_payment_methods_payment_method_id",
                schema: "eshopdb",
                table: "store_payment_methods",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_payment_methods_store_id",
                schema: "eshopdb",
                table: "store_payment_methods",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_payment_methods_store_id_payment_method_id",
                schema: "eshopdb",
                table: "store_payment_methods",
                columns: new[] { "store_id", "payment_method_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_store_products_created_by",
                schema: "eshopdb",
                table: "store_products",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_store_products_featured",
                schema: "eshopdb",
                table: "store_products",
                column: "featured");

            migrationBuilder.CreateIndex(
                name: "ix_store_products_position",
                schema: "eshopdb",
                table: "store_products",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_store_products_product_id",
                schema: "eshopdb",
                table: "store_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_products_store_id",
                schema: "eshopdb",
                table: "store_products",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_products_visible",
                schema: "eshopdb",
                table: "store_products",
                column: "visible");

            migrationBuilder.CreateIndex(
                name: "ix_store_shipping_methods_created_by",
                schema: "eshopdb",
                table: "store_shipping_methods",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_store_shipping_methods_shipping_method_id",
                schema: "eshopdb",
                table: "store_shipping_methods",
                column: "shipping_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_shipping_methods_store_id",
                schema: "eshopdb",
                table: "store_shipping_methods",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_shipping_methods_store_id_shipping_method_id",
                schema: "eshopdb",
                table: "store_shipping_methods",
                columns: new[] { "store_id", "shipping_method_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stores_code",
                schema: "eshopdb",
                table: "stores",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stores_country_id",
                schema: "eshopdb",
                table: "stores",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_stores_created_by",
                schema: "eshopdb",
                table: "stores",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_stores_default",
                schema: "eshopdb",
                table: "stores",
                column: "default");

            migrationBuilder.CreateIndex(
                name: "ix_stores_deleted_at",
                schema: "eshopdb",
                table: "stores",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_stores_name",
                schema: "eshopdb",
                table: "stores",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stores_state_id",
                schema: "eshopdb",
                table: "stores",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "ix_stores_url",
                schema: "eshopdb",
                table: "stores",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_taxa_created_by",
                schema: "eshopdb",
                table: "taxa",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_taxa_depth",
                schema: "eshopdb",
                table: "taxa",
                column: "depth");

            migrationBuilder.CreateIndex(
                name: "ix_taxa_lft",
                schema: "eshopdb",
                table: "taxa",
                column: "lft");

            migrationBuilder.CreateIndex(
                name: "ix_taxa_name",
                schema: "eshopdb",
                table: "taxa",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_taxa_parent_id",
                schema: "eshopdb",
                table: "taxa",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_taxa_permalink",
                schema: "eshopdb",
                table: "taxa",
                column: "permalink",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_taxa_position",
                schema: "eshopdb",
                table: "taxa",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_taxa_rgt",
                schema: "eshopdb",
                table: "taxa",
                column: "rgt");

            migrationBuilder.CreateIndex(
                name: "ix_taxa_taxonomy_id",
                schema: "eshopdb",
                table: "taxa",
                column: "taxonomy_id");

            migrationBuilder.CreateIndex(
                name: "ix_taxon_images_created_by",
                schema: "eshopdb",
                table: "taxon_images",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_taxon_images_position",
                schema: "eshopdb",
                table: "taxon_images",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_taxon_images_taxon_id",
                schema: "eshopdb",
                table: "taxon_images",
                column: "taxon_id");

            migrationBuilder.CreateIndex(
                name: "ix_taxon_images_type",
                schema: "eshopdb",
                table: "taxon_images",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_taxon_rules_created_by",
                schema: "eshopdb",
                table: "taxon_rules",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_taxon_rules_taxon_id_type",
                schema: "eshopdb",
                table: "taxon_rules",
                columns: new[] { "taxon_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_taxonomies_created_by",
                schema: "eshopdb",
                table: "taxonomies",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_taxonomies_name",
                schema: "eshopdb",
                table: "taxonomies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_taxonomies_position",
                schema: "eshopdb",
                table: "taxonomies",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_taxonomies_store_id",
                schema: "eshopdb",
                table: "taxonomies",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_assigned_to",
                schema: "eshopdb",
                table: "user_claims",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                schema: "eshopdb",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                schema: "eshopdb",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_assigned_to",
                schema: "eshopdb",
                table: "user_roles",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "eshopdb",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "eshopdb",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_by",
                schema: "eshopdb",
                table: "users",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_users_phone_number",
                schema: "eshopdb",
                table: "users",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "eshopdb",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variant_option_values_created_by",
                schema: "eshopdb",
                table: "variant_option_values",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_variant_option_values_option_value_id",
                schema: "eshopdb",
                table: "variant_option_values",
                column: "option_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_variant_option_values_variant_id",
                schema: "eshopdb",
                table: "variant_option_values",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_variant_option_values_variant_id_option_value_id",
                schema: "eshopdb",
                table: "variant_option_values",
                columns: new[] { "variant_id", "option_value_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variants_created_by",
                schema: "eshopdb",
                table: "variants",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_variants_is_deleted",
                schema: "eshopdb",
                table: "variants",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_variants_position",
                schema: "eshopdb",
                table: "variants",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ix_variants_product_id",
                schema: "eshopdb",
                table: "variants",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "classification",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "inventory_units",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "line_item_adjustments",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "order_adjustments",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "payment_sources",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "prices",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "product_images",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "product_option_types",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "product_property_types",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "promotion_actions",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "promotion_rule_taxons",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "promotion_rule_users",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "promotion_usages",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "reviews",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "role_claims",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "store_payment_methods",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "store_products",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "store_shipping_methods",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "taxon_images",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "taxon_rules",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "variant_option_values",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "line_items",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "property_types",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "promotion_rules",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "shipments",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "stock_items",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "stock_transfers",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "taxa",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "option_values",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "shipping_methods",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "variants",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "stock_locations",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "taxonomies",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "option_types",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "customer_addresses",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "promotions",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "products",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "stores",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "users",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "states",
                schema: "eshopdb");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "eshopdb");
        }
    }
}
