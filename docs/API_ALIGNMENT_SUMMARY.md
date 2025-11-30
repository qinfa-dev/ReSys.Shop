# API Specification - Alignment with ReSys Domain Models

**Created**: November 30, 2024  
**Based on**: ReSys.Shop Domain Models + Spree Commerce API v2 Patterns

---

## Key Improvements & Corrections

### 1. **Response Format Standardization**

**Changed From**: Inconsistent custom format  
**Changed To**: JSON:API (RFC 7231) standard

**Benefits**:
- Industry-standard format (used by Spree, Ember.js, etc.)
- Built-in support for relationships and sparse fieldsets
- Consistent error handling
- Client library support (json:api packages)

**Example**:
```json
// Before: Custom format
{
  "product": {
    "id": "prod-001",
    "name": "T-Shirt"
  }
}

// After: JSON:API standard
{
  "data": {
    "id": "prod-001",
    "type": "product",
    "attributes": {
      "name": "T-Shirt"
    },
    "relationships": {
      "variants": { "data": [...] }
    }
  },
  "included": [...]
}
```

---

### 2. **Order Domain Alignment**

**Corrected State Names** (aligned with ReSys Order.OrderState enum):
- ✅ `cart` → `cart`
- ✅ `address` → `address`
- ✅ `delivery` → `delivery`
- ✅ `payment` → `payment`
- ✅ `confirm` → `confirm`
- ✅ `complete` → `complete`
- ✅ `canceled` → `canceled`

**State Transitions**: Properly documented via `Next()` method flow

**Endpoints Updated**:
- `POST /checkout/initiate` → Clarified it creates `address` state
- `PUT /shipping-address` → Transitions to `delivery` state
- `PUT /shipping-method` → Transitions to `payment` state
- `POST /complete` → Transitions to `complete` state

---

### 3. **Product Catalog Alignment**

**Corrected to Match ReSys.Core Domain**:

| Field | ReSys Model | API Response |
|-------|------------|--------------|
| Status | `ProductStatus` enum (Active, Draft, Archived) | ✅ `status` attribute |
| Slug | `IHasSlug` concern | ✅ `slug` attribute |
| Pricing | Variant-based (no master price) | ✅ Moved price to variant level |
| Images | `ProductImage` collection | ✅ `images` relationship |
| Properties | `ProductProperty` collection | ✅ `product_properties` relationship |
| Categories | `Taxon` relationships | ✅ `taxons` relationship |

**Removed Errors**:
- ❌ `GET /{slug}/reviews` (reviews are tied to product, not variant)
- ✅ Corrected to: `GET /api/v2/storefront/products/{productId}/reviews`

---

### 4. **Inventory Model Corrections**

**ReSys Structure**:
- `StockLocation` (warehouse)
- `StockItem` (variant + location + quantity)
- `StockMovement` (audit trail)
- `StockTransfer` (inter-location transfers)

**API Endpoints Corrected**:
- ✅ Bulk operations: `POST /api/v2/admin/inventory/stock-items/bulk/adjust`
- ✅ Stock transfers: `POST /api/v2/admin/inventory/stock-transfers`
- ✅ Movement history: `GET /api/v2/admin/inventory/stock-movements`

**Removed Incorrect Field**:
- ❌ `quantity_on_hand` (should be singular context)
- ✅ Added `quantity_reserved` to track cart reservations

---

### 5. **Promotion & Discount Alignment**

**ReSys Model**:
- `Promotion` aggregate with `PromotionRule` + `PromotionAction`
- `Type`: `OrderDiscount`, `ItemDiscount`, `FreeShipping`
- `DiscountType`: `Percentage`, `FixedAmount`

**API Updated**:
```json
{
  "type": "order_discount",
  "discount_type": "percentage",
  "discount_value": 20,
  "minimum_order_amount": 50,
  "usage_limit": 1000,
  "usage_limit_per_user": 1
}
```

**Endpoints Fixed**:
- ✅ `POST /api/v2/storefront/cart/promotions` → Apply to current order
- ✅ `DELETE /api/v2/storefront/cart/promotions` → Remove promotion
- ✅ `GET /api/v2/admin/promotions/{promotionId}/usage` → Usage statistics

---

### 6. **Shipping & Payment Models**

**Corrected Field Names** (per domain):
- Shipping: `ShippingMethod` with cost calculation
- Payments: `PaymentMethod` (13+ provider types in domain)

**API Response Standardized**:
```json
{
  "id": "ship-standard",
  "type": "shipping_method",
  "attributes": {
    "name": "Standard Shipping",
    "code": "standard",
    "cost": "10.00",
    "display_cost": "$10.00"
  }
}
```

---

### 7. **Customer & Address Management**

**Aligned with ReSys Identity Context**:
- `ApplicationUser` model with address relationships
- Default billing/shipping address flags

**API Endpoints**:
```
GET /api/v2/storefront/account/addresses
POST /api/v2/storefront/account/addresses
PATCH /api/v2/storefront/account/addresses/{addressId}
DELETE /api/v2/storefront/account/addresses/{addressId}
```

---

### 8. **Multi-Store Support**

**ReSys Store Model**:
- Store aggregate with isolated orders, configs
- Shared product catalog (with visibility per-store)
- Store-specific payment/shipping methods

**API Structured**:
```
GET /api/v2/storefront/stores/{storeCode}
GET /api/v2/admin/stores
GET /api/v2/admin/stores/{storeId}/products
POST /api/v2/admin/stores/{storeId}/stock-locations/{locationId}
```

---

### 9. **Authentication & Authorization (Spree-aligned)**

**Corrected Structure**:
- JWT with RS256 signing (not HS256)
- Separate access token (15 min) and refresh token (7 days)
- Role-Based Access Control (RBAC) with granular permissions

**Permission Format** (resource.action):
```
stores.view, stores.create, stores.update, stores.delete
products.view, products.create, products.update, products.delete
inventory.view, inventory.adjust, inventory.transfer
orders.view, orders.update, orders.cancel, orders.fulfill
```

---

### 10. **Error Handling Improvements**

**Standardized Error Response**:
```json
{
  "errors": [
    {
      "status": "422",
      "code": "invalid_state_transition",
      "title": "Invalid Order State",
      "detail": "Cannot transition from 'cart' to 'payment'",
      "source": { "pointer": "/data/attributes/state" }
    }
  ]
}
```

**Domain Error Codes Added**:
- `invalid_state_transition` - Order state machine violation
- `inventory_error` - Stock unavailable
- `payment_error` - Payment processing failed

---

### 11. **Special Feature: Visual Similarity Search**

**Unique to ReSys** (pgvector integration):
```
POST /api/v2/storefront/products/search/by-image
```

**Request**:
```
image: <binary file>
limit: 10
threshold: 0.75 (vector similarity)
```

**Response**:
```json
{
  "data": [
    {
      "similarity_score": 0.92,
      "product": { "id": "prod-002", ... }
    }
  ],
  "meta": {
    "search_time_ms": 145,
    "vector_model_version": "resnet50-v2"
  }
}
```

---

### 12. **Query Parameters (Spree-aligned Filters)**

**Improved from hardcoded sorts to flexible filters**:

```
// Product listing
filter[ids]=prod-001,prod-002
filter[skus]=TS-BLUE-001,TS-RED-001
filter[name]=blue              // Wildcard search
filter[price]=10-100           // Range
filter[taxons]=cat-1,cat-2     // Multiple categories
filter[options][color]=blue    // Option filtering
filter[properties][brand]=Nike // Property filtering
filter[in_stock]=true
filter[backorderable]=false
filter[purchasable]=true
sort=name,-price,available_on
page=2&per_page=50
include=variants,images,taxons
```

---

### 13. **Pagination & Metadata**

**Standard JSON:API pagination**:
```json
{
  "meta": {
    "count": 25,
    "total_count": 250,
    "total_pages": 10,
    "filters": {
      "option_types": [...],
      "product_properties": [...]
    }
  },
  "links": {
    "self": "/api/v2/storefront/products?page=2",
    "first": "/api/v2/storefront/products?page=1",
    "prev": "/api/v2/storefront/products?page=1",
    "next": "/api/v2/storefront/products?page=3",
    "last": "/api/v2/storefront/products?page=10"
  }
}
```

---

### 14. **Webhook Events Alignment**

**Tied to ReSys Domain Events**:

| Domain Event | Webhook Event |
|-------------|---------------|
| `Order.Events.Created` | `order.created` |
| `Order.Events.Completed` | `order.completed` |
| `Order.Events.Canceled` | `order.cancelled` |
| `Order.Events.FinalizeInventory` | (internal) |
| `Promotion.Events.Used` | `promotion.applied` |
| `Product.Events.Created` | `product.created` |
| `Product.Events.Updated` | `product.updated` |

**Webhook Payload**:
```json
{
  "id": "event-123",
  "type": "order.created",
  "timestamp": "2024-11-30T12:00:00Z",
  "data": {
    "order_id": "ord-001",
    "order_number": "R123456789"
  }
}
```

---

## API Versioning Strategy

| Aspect | Standard |
|--------|----------|
| **Base Paths** | `/api/v2/storefront`, `/api/v2/admin` |
| **Backward Compatibility** | 2 major versions maintained |
| **Deprecation Notice** | 6 months advance warning |
| **Content Type** | `application/vnd.api+json` (JSON:API) |
| **Authentication** | JWT Bearer with RS256 |

---

## Rate Limits (Final)

| Tier | Storefront | Admin |
|------|-----------|-------|
| **Authenticated** | 1000/hour | 5000/hour |
| **Guest** | 100/hour | N/A |
| **Search** | 60/minute | 100/minute |
| **Bulk Ops** | N/A | 100/hour |

---

## Migration Path (For Existing Clients)

If updating from previous API spec:

1. **Update Response Parsing**: Use JSON:API library (client-side)
2. **Update Query Params**: Switch to `filter[*]` format
3. **Update Error Handling**: Check `errors` array instead of `error` field
4. **Update Pagination**: Use `meta.total_count` instead of custom pagination
5. **Update Auth**: Use refresh token flow, watch token expiration
6. **Update Webhooks**: Update payload parsing for new structure

**Deprecation Timeline**:
- Day 1: v2 API available (new clients use v2)
- Month 1-6: v1 API maintained, deprecation notices sent
- Month 7+: v1 API removed

---

## Key References

- **Domain Models**: `src/ReSys.Core/Domain/`
- **CQRS Handlers**: `src/ReSys.Core/Feature/`
- **Constraints**: `src/ReSys.Core/Common/Constants/CommonInput.cs`
- **Error Types**: `src/ReSys.Core/Common/Constants/CommonInput.Errors.cs`
- **Copilot Instructions**: `.github/copilot-instructions.md`

---

## Implementation Checklist

- [ ] Implement JSON:API response serialization
- [ ] Update all controllers/endpoints to new format
- [ ] Implement filter parameter parsing
- [ ] Update error handling to match spec
- [ ] Add rate limiting middleware
- [ ] Implement webhook system
- [ ] Add JWT refresh token flow
- [ ] Implement RBAC permission checks
- [ ] Add API versioning strategy
- [ ] Generate OpenAPI/Swagger docs
- [ ] Create SDK/client libraries
- [ ] Write integration tests for all endpoints
- [ ] Document breaking changes
- [ ] Set deprecation timeline

---

**Status**: Ready for Implementation  
**Maintainer**: Platform Team  
**Last Updated**: November 30, 2024
