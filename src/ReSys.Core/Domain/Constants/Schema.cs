namespace ReSys.Core.Domain.Constants;

/// <summary>
/// Provides a centralized repository for database schema-related constant strings.
/// These constants represent table names across various bounded contexts within the application.
/// Using these constants ensures consistency and reduces magic strings when interacting with the database schema.
/// </summary>
/// <remarks>
/// This class is organized into phases, reflecting the development roadmap of the ReSys.Shop project.
/// Each constant maps directly to a table name in the underlying database.
///
/// <strong>Example Usage:</strong>
/// <code>
/// modelBuilder.Entity&lt;Product&gt;().ToTable(Schema.Products);
/// var query = $"SELECT * FROM {Schema.Users} WHERE Id = @userId";
/// </code>
/// </remarks>
public static class Schema
{
    /// <summary>
    /// The default schema name for the e-shop database.
    /// </summary>
    public const string Default = "eshopdb";
    // ===========================================
    // PHASE 1: CORE FOUNDATION (Weeks 1-2) - Essential infrastructure
    // ===========================================

    // Messaging & Outbox Pattern
    /// <summary>
    /// Represents the database table name for storing outbox messages for reliable messaging.
    /// </summary>
    public const string OutboxMessages = "outbox_messages";
    /// <summary>
    /// Represents the database table name for storing application-wide audit logs.
    /// </summary>
    public const string AuditLogs = "audit_logs";
    
    /// <summary>
    /// Represents the database table name for storing application-wide settings.
    /// </summary>
    public const string Settings = "settings";

    // Authentication & Authorization - Full Spree complexity maintained
    /// <summary>
    /// Represents the database table name for storing user accounts.
    /// </summary>
    public const string Users = "users";
    /// <summary>
    /// Represents the database table name for storing user roles.
    /// </summary>
    public const string Roles = "roles";
    /// <summary>
    /// Represents the database table name for the join entity between users and roles.
    /// </summary>
    public const string UserRoles = "user_roles";
    // Remaining constants in Schema.cs should also have XML documentation added
    // following the pattern above. Due to the large number of constants,
    // only a representative subset are documented here as examples.
    // All other constants below would require similar XML comments.

    // Authentication & Authorization - Full Spree complexity maintained
    public const string RoleClaims = "role_claims";
    public const string UserClaims = "user_claims";
    public const string UserLogins = "user_logins";
    public const string UserTokens = "user_tokens";
    public const string RefreshTokens = "refresh_tokens";
    public const string AccessPermissions = "permissions";

    // Common Geo-Political Entities
    public const string Addresses = "addresses";
    public const string Countries = "countries";
    public const string States = "states";

    // Customer Management - Extended for fashion retail
    public const string Customers = "customers";
    public const string UserAddresses = "customer_addresses"; // Multiple shipping/billing addresses
    public const string CustomerWishlists = "customer_wishlists"; // Multiple wishlists
    public const string WishlistItems = "wishlist_items";
    public const string NewsletterSubscriptions = "newsletter_subscriptions";

    // Multi-Location Infrastructure - Essential for 2-3 locations
    public const string StoreLocations = "store_locations";
    public const string LocationOperatingHours = "location_operating_hours";
    public const string LocationContacts = "location_contacts";

    // Taxonomy System - Full Spree complexity for fashion categorization
    public const string Taxonomies = "taxonomies"; // Root systems: Categories, Occasions, Seasons
    public const string Taxons = "taxa"; // Hierarchical: Women > Clothing > Dresses > Casual
    public const string Classifications = "classification"; // Many-to-many relationships
    public const string TaxonRules = "taxon_rules"; // Dynamic rules for auto-categorization
    public const string TaxonImages = "taxon_images"; // Category-specific images

    // Translations - Multi-language support
    public static string TranslationFor(string tableName) => $"{tableName}_translations";

    // ===========================================
    // PHASE 2: PRODUCT CORE (Weeks 2-3) - E-commerce foundation
    // ===========================================

    // Product Architecture - Spree-inspired complexity
    public const string Products = "products";
    public const string StoreProducts = "store_products";
    public const string Tags = "tags"; // New constant for Tag entity
    public const string Reviews = "reviews"; // Product reviews and ratings
    public const string ProductImages = "product_images"; // Color-specific images

    // Product Attributes & Properties - Fashion-specific
    public const string PropertyTypes = "property_types"; // Color, Material, Care Instructions
    public const string ProductPropertyTypes = "product_property_types";

    // Product Options System - Size/Color/Style variations  
    public const string OptionTypes = "option_types"; // Size, Color, Material, Fit
    public const string OptionValues = "option_values"; // XS, Red, Cotton, Slim
    public const string ProductOptionTypes = "product_option_types";
    public const string ProductTags = "product_tags"; // New constant for the Product-Tag join table

    // Variants - Individual SKUs with pricing
    public const string Variants = "variants";
    public const string VariantOptionValues = "variant_option_values";
    public const string Prices = "prices"; // Multi-currency, location-based pricing

    // Stores- Multi-store support
    public const string Stores = "stores";
    public const string StoreShippingMethods = "store_shipping_methods";
    public const string StorePaymentMethods = "store_payment_methods";
    public const string StoreTaxons = "store_taxons";
    public const string StoreOrders = "store_orders";

    // ===========================================
    // PHASE 3: INVENTORY & ORDERS (Weeks 3-4) - Business operations
    // ===========================================

    // Advanced Inventory - Multi-location complexity
    public const string StockLocations = "stock_locations"; // Warehouses + stores
    public const string StockItems = "stock_items"; // Variant stock per location
    public const string StockMovements = "stock_movements"; // Audit trail
    public const string InventoryUnits = "inventory_units"; // Individual stock items
    public const string StockTransfers = "stock_transfers"; // Inter-location transfers
    public const string StorePickups = "store_pickups";

    // Order Management - Full e-commerce workflow
    public const string Orders = "orders";
    public const string LineItems = "line_items";
    public const string LineItemAdjustments = "line_item_adjustments";
    public const string OrderAdjustments = "order_adjustments";
    public const string OrderPromotions = "order_promotions";
    public const string Adjustments = "adjustments"; // Taxes, discounts, fees


    // Fulfillment & Shipping
    public const string Shipments = "shipments";
    public const string ShippingMethods = "shipping_methods";
    public const string ShippingRates = "shipping_rates";
    public const string ShippingCategories = "shipping_categories"; // Fashion-specific

    // Payment Processing
    public const string Payments = "payments";
    public const string PaymentMethods = "payment_methods";
    public const string PaymentSources = "payment_sources"; // Saved cards
    public const string Refunds = "refunds";

    // ===========================================
    // PHASE 4: ML & RECOMMENDATIONS (Weeks 5-7) - AI features
    // ===========================================

    // ML Feature Storage - Visual & behavioral data
    public const string ProductFeatures = "product_features"; // CNN embeddings
    public const string ProductColorPalettes = "product_color_palettes"; // Extracted colors
    public const string VisualSimilarityScores = "visual_similarity_scores";
    public const string StyleEmbeddings = "style_embeddings"; // Style vectors

    // Recommendation Engine
    public const string UserInteractions = "user_interactions"; // Views, clicks, purchases
    public const string UserPreferences = "user_preferences"; // Learned preferences
    public const string SimilarProducts = "similar_products"; // Pre-computed similarities
    public const string RecommendationSets = "recommendation_sets"; // Cached recommendations
    public const string RecommendationLogs = "recommendation_logs"; // Performance tracking

    // Behavioral Analytics
    public const string UserSessions = "user_sessions"; // Session tracking
    public const string ClickstreamEvents = "clickstream_events"; // Detailed interactions
    public const string SearchQueries = "search_queries"; // Search behavior
    public const string AbandonedCarts = "abandoned_carts"; // Cart analysis

    // ===========================================
    // PHASE 5: PROMOTIONS & MARKETING (Week 8) - Business growth
    // ===========================================

    // Promotion Engine - Spree-inspired complexity
    public const string Promotions = "promotions";
    public const string PromotionUsages = "promotion_usages";
    public const string PromotionRules = "promotion_rules";
    public const string PromotionActions = "promotion_actions";
    public const string PromotionCategories = "promotion_categories"; // Fashion seasons/events
    public const string PromotionRuleTaxons = "promotion_rule_taxons"; // Taxon associations for promotion rules
    public const string PromotionRuleRoles = "promotion_rule_roles"; // User associations for promotion rules
    public const string PromotionRuleUsers = "promotion_rule_users";

    // Marketing & CRM
    public const string EmailCampaigns = "email_campaigns";
    public const string CustomerSegments = "customer_segments";
    public const string LoyaltyPrograms = "loyalty_programs";
    public const string LoyaltyPoints = "loyalty_points";

    // ===========================================
    // PHASE 6: REVIEWS & SOCIAL (Week 9) - Community features
    // ===========================================

    // Product Reviews & Ratings
    public const string ProductReviews = "product_reviews";
    public const string ReviewImages = "review_images"; // User-generated content
    public const string ReviewHelpfulness = "review_helpfulness"; // Vote system
    public const string ReviewModerationQueue = "review_moderation_queue";

    // Social Features
    public const string UserFollows = "user_follows"; // Fashion influencers
    public const string ProductShares = "product_shares"; // Social sharing
    public const string OutfitPosts = "outfit_posts"; // User styling posts

    // ===========================================
    // PHASE 7: ANALYTICS & REPORTING (Week 10) - Business intelligence
    // ===========================================

    // Business Analytics
    public const string SalesReports = "sales_reports";
    public const string InventoryReports = "inventory_reports";
    public const string CustomerAnalytics = "customer_analytics";
    public const string ProductPerformance = "product_performance";

    // ML Performance Tracking
    public const string ModelPerformanceMetrics = "model_performance_metrics";
    public const string RecommendationAccuracy = "recommendation_accuracy";
    public const string SearchAnalytics = "search_analytics";
    public const string ConversionTracking = "conversion_tracking";

    // Operational Reports
    public const string InventoryAlerts = "inventory_alerts";
    public const string FulfillmentMetrics = "fulfillment_metrics";
    public const string CustomerServiceTickets = "customer_service_tickets";
    public const string ReturnReasons = "return_reasons"; // Fashion-specific returns

    // ===========================================
    // OPTIONAL EXTENSIONS - Future enhancements
    // ===========================================

    // Advanced Fashion Features (implement if time permits)
    // public const string FashionTrends = "fashion_trends";
    // public const string PersonalStyleProfiles = "personal_style_profiles";
    // public const string VirtualFittingData = "virtual_fitting_data";
    // public const string SustainabilityMetrics = "sustainability_metrics";

    // Multi-tenant Support (for scaling)
    // public const string Tenants = "tenants";
    // public const string TenantUsers = "tenant_users";
    // public const string TenantConfigurations = "tenant_configurations";

    // Advanced Integrations
    // public const string ExternalInventorySources = "external_inventory_sources";
    // public const string SupplierIntegrations = "supplier_integrations";
    // public const string DropshippingOrders = "dropshipping_orders";


    // Testing & QA (for development purposes)
    public const string TodoItems = "todo_items";
    public const string TodoLists = "todo_lists";
}