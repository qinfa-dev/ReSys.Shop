# API Implementation Guide - ReSys.Shop

**Purpose**: Quick reference for implementing the API specification  
**Audience**: Backend developers (.NET)  
**Date**: November 30, 2024

---

## 📁 Project Structure for API Implementation

```
src/ReSys.API/
├── Endpoints/                          # FastEndpoints or Controllers
│   ├── Storefront/
│   │   ├── Store/
│   │   │   └── GetStoreInfoEndpoint.cs
│   │   ├── Catalog/
│   │   │   ├── ListProductsEndpoint.cs
│   │   │   ├── GetProductBySlugEndpoint.cs
│   │   │   ├── ListProductVariantsEndpoint.cs
│   │   │   ├── ListProductReviewsEndpoint.cs
│   │   │   ├── SubmitProductReviewEndpoint.cs
│   │   │   └── GetRelatedProductsEndpoint.cs
│   │   ├── Search/
│   │   │   ├── SearchProductsEndpoint.cs
│   │   │   └── ImageSimilaritySearchEndpoint.cs
│   │   ├── Taxonomy/
│   │   │   ├── ListTaxonomiesEndpoint.cs
│   │   │   └── GetCategoryEndpoint.cs
│   │   ├── Cart/
│   │   │   ├── GetCurrentCartEndpoint.cs
│   │   │   ├── AddCartItemEndpoint.cs
│   │   │   ├── UpdateCartItemEndpoint.cs
│   │   │   ├── RemoveCartItemEndpoint.cs
│   │   │   ├── ApplyCouponEndpoint.cs
│   │   │   ├── RemoveCouponEndpoint.cs
│   │   │   └── MergeCartsEndpoint.cs
│   │   ├── Checkout/
│   │   │   ├── InitiateCheckoutEndpoint.cs
│   │   │   ├── SetShippingAddressEndpoint.cs
│   │   │   ├── SetBillingAddressEndpoint.cs
│   │   │   ├── GetShippingMethodsEndpoint.cs
│   │   │   ├── SelectShippingMethodEndpoint.cs
│   │   │   ├── GetPaymentMethodsEndpoint.cs
│   │   │   ├── SubmitPaymentEndpoint.cs
│   │   │   ├── ReviewOrderEndpoint.cs
│   │   │   └── CompleteOrderEndpoint.cs
│   │   ├── Account/
│   │   │   ├── RegisterEndpoint.cs
│   │   │   ├── LoginEndpoint.cs
│   │   │   ├── RefreshTokenEndpoint.cs
│   │   │   ├── GetProfileEndpoint.cs
│   │   │   ├── UpdateProfileEndpoint.cs
│   │   │   ├── ChangePasswordEndpoint.cs
│   │   │   ├── ListAddressesEndpoint.cs
│   │   │   ├── CreateAddressEndpoint.cs
│   │   │   ├── UpdateAddressEndpoint.cs
│   │   │   ├── DeleteAddressEndpoint.cs
│   │   │   ├── ListOrdersEndpoint.cs
│   │   │   ├── GetOrderDetailsEndpoint.cs
│   │   │   └── CancelOrderEndpoint.cs
│   │   └── Inventory/
│   │       ├── CheckVariantAvailabilityEndpoint.cs
│   │       └── BulkCheckAvailabilityEndpoint.cs
│   │
│   └── Admin/
│       ├── Stores/
│       │   ├── ListStoresEndpoint.cs
│       │   ├── CreateStoreEndpoint.cs
│       │   ├── UpdateStoreEndpoint.cs
│       │   └── ... (store management)
│       ├── Products/
│       │   ├── ListProductsEndpoint.cs
│       │   ├── CreateProductEndpoint.cs
│       │   ├── UpdateProductEndpoint.cs
│       │   ├── ActivateProductEndpoint.cs
│       │   ├── ArchiveProductEndpoint.cs
│       │   ├── ListVariantsEndpoint.cs
│       │   ├── CreateVariantEndpoint.cs
│       │   ├── UploadImageEndpoint.cs
│       │   ├── SetPriceEndpoint.cs
│       │   └── ... (product management)
│       ├── Inventory/
│       │   ├── ... (stock management)
│       ├── Orders/
│       │   ├── ... (order operations)
│       ├── Promotions/
│       │   ├── ... (promotion management)
│       ├── Customers/
│       │   ├── ... (customer management)
│       ├── Reviews/
│       │   ├── ... (review moderation)
│       └── Analytics/
│           └── ... (reporting)
│
├── Dtos/                               # Response DTOs
│   ├── StorefrontDtos.cs
│   ├── AdminDtos.cs
│   └── CommonDtos.cs
│
├── Mappers/                            # Mapster configurations
│   ├── StorefrontMappingConfig.cs
│   └── AdminMappingConfig.cs
│
├── Middleware/                         # Custom middleware
│   ├── ErrorHandlingMiddleware.cs       # JSON:API error formatting
│   ├── RateLimitingMiddleware.cs
│   ├── AuthenticationMiddleware.cs
│   └── CorrelationIdMiddleware.cs
│
├── Services/                           # API-specific services
│   ├── TokenService.cs                 # JWT token generation
│   ├── CartService.cs                  # Cart/session management
│   ├── ImageUploadService.cs           # Image processing
│   └── VectorSearchService.cs          # pgvector integration
│
├── Filters/                            # Action filters
│   ├── AuthorizationFilter.cs
│   ├── ValidationFilter.cs
│   └── RateLimitFilter.cs
│
├── WebhookHandlers/                    # Webhook event handlers
│   ├── OrderWebhookHandler.cs
│   ├── ProductWebhookHandler.cs
│   └── PromotionWebhookHandler.cs
│
├── Constants/
│   ├── ApiRoutes.cs
│   ├── ApiErrors.cs
│   └── PermissionNames.cs
│
└── Program.cs                          # Main configuration
```

---

## 🚀 Implementation Roadmap

### Phase 1: Foundation (Week 1-2)
- [ ] JSON:API response serialization (NuGet: `JsonApiDotNetCore`)
- [ ] Error handling middleware
- [ ] Authentication/JWT setup
- [ ] Rate limiting middleware
- [ ] API documentation (OpenAPI/Swagger)

### Phase 2: Storefront Browsing (Week 3-4)
- [ ] Store info endpoint
- [ ] Product listing with filters
- [ ] Product detail by slug
- [ ] Variant details
- [ ] Category/taxonomy browsing
- [ ] Search endpoints

### Phase 3: Cart & Checkout (Week 5-6)
- [ ] Cart operations (add, update, remove)
- [ ] Coupon application
- [ ] Checkout flow (address → delivery → payment → complete)
- [ ] Address management

### Phase 4: Account (Week 7)
- [ ] Registration/Login
- [ ] Token refresh
- [ ] Profile management
- [ ] Order history

### Phase 5: Admin Features (Week 8-10)
- [ ] Product management
- [ ] Inventory operations
- [ ] Order management
- [ ] Promotion management
- [ ] Customer management

### Phase 6: Advanced Features (Week 11-12)
- [ ] Visual similarity search
- [ ] Webhooks
- [ ] Analytics endpoints
- [ ] Review moderation

---

## 💻 Code Example: Implementing an Endpoint

### 1. Create the Query/Command (in Feature layer)

```csharp
// src/ReSys.Core/Feature/Catalog/Products/ListProductsQuery.cs
namespace ReSys.Core.Feature.Catalog.Products;

public sealed record ListProductsQuery(
    int Page = 1,
    int PerPage = 25,
    string? FilterName = null,
    string? FilterSkus = null,
    string? FilterPrice = null,
    List<string>? FilterTaxons = null,
    string? FilterInStock = null,
    string? Sort = null
) : IRequest<ErrorOr<PaginatedResult<ProductResponse>>>;

public sealed class ListProductsValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PerPage).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
    }
}

public sealed class ListProductsHandler(IApplicationDbContext dbContext) 
    : IRequestHandler<ListProductsQuery, ErrorOr<PaginatedResult<ProductResponse>>>
{
    public async Task<ErrorOr<PaginatedResult<ProductResponse>>> Handle(
        ListProductsQuery query, 
        CancellationToken ct)
    {
        var queryable = dbContext.Products.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.FilterName))
            queryable = queryable.Where(p => EF.Functions.Like(p.Name, $"%{query.FilterName}%"));

        if (query.FilterTaxons?.Any() == true)
            queryable = queryable.Where(p => p.Taxons.Any(t => query.FilterTaxons.Contains(t.Slug)));

        if (query.FilterInStock == "true")
            queryable = queryable.Where(p => p.MasterVariant.StockItems.Any(si => si.QuantityOnHand > 0));

        // Apply sorting
        queryable = query.Sort?.ToLower() switch
        {
            "price" => queryable.OrderBy(p => p.MasterVariant.Prices.FirstOrDefault().Amount),
            "-price" => queryable.OrderByDescending(p => p.MasterVariant.Prices.FirstOrDefault().Amount),
            "name" => queryable.OrderBy(p => p.Name),
            "-name" => queryable.OrderByDescending(p => p.Name),
            _ => queryable.OrderByDescending(p => p.CreatedAt)
        };

        // Paginate
        var totalCount = await queryable.CountAsync(ct);
        var products = await queryable
            .Skip((query.Page - 1) * query.PerPage)
            .Take(query.PerPage)
            .ToListAsync(ct);

        var result = new PaginatedResult<ProductResponse>
        {
            Data = products.Adapt<List<ProductResponse>>(),
            TotalCount = totalCount,
            CurrentPage = query.Page,
            PerPage = query.PerPage
        };

        return result;
    }
}
```

### 2. Create Mapper Configuration

```csharp
// src/ReSys.Core/Feature/Catalog/Products/ProductMappingConfig.cs
namespace ReSys.Core.Feature.Catalog.Products;

public sealed class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Product, ProductResponse>()
            .Map(dest => dest.Purchasable, src => src.MasterVariant.Prices.Any())
            .Map(dest => dest.InStock, src => src.MasterVariant.StockItems.Any(si => si.QuantityOnHand > 0))
            .Map(dest => dest.Price, src => src.MasterVariant.Prices.FirstOrDefault().Amount)
            .Map(dest => dest.DisplayPrice, src => 
                src.MasterVariant.Prices.FirstOrDefault().Amount.ToString("C"))
            .Map(dest => dest.CompareAtPrice, src => 
                src.MasterVariant.Prices.FirstOrDefault().CompareAtAmount);
    }
}
```

### 3. Create Endpoint (FastEndpoints style)

```csharp
// src/ReSys.API/Endpoints/Storefront/Catalog/ListProductsEndpoint.cs
namespace ReSys.API.Endpoints.Storefront.Catalog;

public sealed class ListProductsEndpoint : Endpoint<ListProductsRequest, StorefrontListProductsResponse>
{
    public ListProductsEndpoint()
    {
        Get("/api/v2/storefront/products");
        AllowAnonymous();
        Description(b => b
            .WithName("List Products")
            .WithSummary("Get paginated product list with filtering"));
    }

    public override async Task HandleAsync(ListProductsRequest req, CancellationToken ct)
    {
        var query = new ListProductsQuery(
            Page: req.Page ?? 1,
            PerPage: req.PerPage ?? 25,
            FilterName: req.Q,
            FilterSkus: req.FilterSkus,
            FilterPrice: req.FilterPrice,
            FilterTaxons: req.FilterTaxons,
            FilterInStock: req.FilterInStock,
            Sort: req.Sort
        );

        var result = await Mediator.Send(query, ct);

        if (result.IsError)
            ThrowError(r => r.StatusCode = StatusCodes.Status400BadRequest);

        var response = new StorefrontListProductsResponse
        {
            Data = result.Value.Data,
            Meta = new()
            {
                Count = result.Value.Data.Count,
                TotalCount = result.Value.TotalCount,
                TotalPages = (result.Value.TotalCount + result.Value.PerPage - 1) / result.Value.PerPage
            },
            Links = new()
            {
                Self = $"/api/v2/storefront/products?page={req.Page}",
                First = $"/api/v2/storefront/products?page=1",
                Prev = req.Page > 1 ? $"/api/v2/storefront/products?page={req.Page - 1}" : null,
                Next = result.Value.TotalCount > (req.Page ?? 1) * (req.PerPage ?? 25) 
                    ? $"/api/v2/storefront/products?page={(req.Page ?? 1) + 1}" 
                    : null
            }
        };

        await SendJsonApiResponseAsync(response, StatusCode: StatusCodes.Status200OK);
    }
}

public sealed class ListProductsRequest
{
    public int? Page { get; set; }
    public int? PerPage { get; set; }
    public string? Q { get; set; }
    public string? FilterSkus { get; set; }
    public string? FilterPrice { get; set; }
    public List<string>? FilterTaxons { get; set; }
    public string? FilterInStock { get; set; }
    public string? Sort { get; set; }
}
```

### 4. Create Response DTOs

```csharp
// src/ReSys.API/Dtos/StorefrontDtos.cs
namespace ReSys.API.Dtos;

public sealed class ProductResponse
{
    public string Id { get; set; }
    public string Type => "product";
    
    public ProductAttributes Attributes { get; set; }
    public ProductRelationships Relationships { get; set; }
}

public sealed class ProductAttributes
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; }
    public string Sku { get; set; }
    public bool Purchasable { get; set; }
    public bool InStock { get; set; }
    public decimal Price { get; set; }
    public string DisplayPrice { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string Status { get; set; }
    public string Currency { get; set; }
    public string MetaTitle { get; set; }
    public string MetaDescription { get; set; }
    public string MetaKeywords { get; set; }
    public DateTime AvailableOn { get; set; }
}

public sealed class ProductRelationships
{
    public RelationshipData Variants { get; set; }
    public RelationshipData OptionTypes { get; set; }
    public RelationshipData ProductProperties { get; set; }
    public RelationshipData Taxons { get; set; }
    public RelationshipData Images { get; set; }
}

public sealed class StorefrontListProductsResponse
{
    [JsonPropertyName("data")]
    public List<ProductResponse> Data { get; set; }
    
    [JsonPropertyName("meta")]
    public ResponseMeta Meta { get; set; }
    
    [JsonPropertyName("links")]
    public ResponseLinks Links { get; set; }
}

public sealed class ResponseMeta
{
    public int Count { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public Dictionary<string, object> Filters { get; set; }
}

public sealed class ResponseLinks
{
    public string Self { get; set; }
    public string First { get; set; }
    public string Prev { get; set; }
    public string Next { get; set; }
    public string Last { get; set; }
}
```

---

## 🔐 Authentication Setup

### JWT Token Service

```csharp
// src/ReSys.API/Services/TokenService.cs
namespace ReSys.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}

public sealed class TokenService(IConfiguration config) : ITokenService
{
    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("scope", "storefront admin")
        };

        foreach (var role in roles)
            claims.Add(new(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"])),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, 
            out SecurityToken securityToken);
        
        if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}
```

---

## 📝 Error Handling Middleware

```csharp
// src/ReSys.API/Middleware/ErrorHandlingMiddleware.cs
namespace ReSys.API.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/vnd.api+json";

        var response = exception switch
        {
            ValidationException ve => new ErrorResponse
            {
                Errors = ve.Errors.Select(e => new ApiError
                {
                    Status = StatusCodes.Status422UnprocessableEntity.ToString(),
                    Code = "validation_error",
                    Title = "Validation Failed",
                    Detail = e.Message
                }).ToList()
            },
            _ => new ErrorResponse
            {
                Errors = new List<ApiError>
                {
                    new()
                    {
                        Status = StatusCodes.Status500InternalServerError.ToString(),
                        Code = "internal_error",
                        Title = "Internal Server Error",
                        Detail = exception.Message
                    }
                }
            }
        };

        context.Response.StatusCode = int.Parse(response.Errors[0].Status);
        return context.Response.WriteAsJsonAsync(response);
    }
}

public sealed class ErrorResponse
{
    [JsonPropertyName("errors")]
    public List<ApiError> Errors { get; set; }
}

public sealed class ApiError
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("code")]
    public string Code { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("detail")]
    public string Detail { get; set; }
    
    [JsonPropertyName("source")]
    public ErrorSource Source { get; set; }
}

public sealed class ErrorSource
{
    [JsonPropertyName("pointer")]
    public string Pointer { get; set; }
}
```

---

## 🔧 Program.cs Setup

```csharp
// src/ReSys.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services
    .AddCore()
    .AddInfrastructure(builder.Configuration)
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
```

---

## 📊 Testing Checklist

- [ ] Unit tests for QueryHandlers
- [ ] Unit tests for Validators
- [ ] Integration tests for Endpoints
- [ ] Auth/Permission tests
- [ ] Filter/Pagination tests
- [ ] Error handling tests
- [ ] Rate limiting tests
- [ ] Load testing (k6, JMeter)

---

## 🚢 Deployment Considerations

- [ ] Environment-specific configuration (JWT keys, rate limits)
- [ ] Database migrations
- [ ] Cache strategies (Redis for cart, sessions)
- [ ] CDN setup (images, static assets)
- [ ] Monitoring (Application Insights, ELK)
- [ ] Health check endpoints
- [ ] API versioning strategy

---

**Ready to Start**: Yes ✅  
**Questions?**: See `.github/copilot-instructions.md` for architecture questions
