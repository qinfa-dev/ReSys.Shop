# Missing API Endpoints - Implementation Guide

**Status**: 🔴 **NOT YET DOCUMENTED IN API_SPECIFICATION.md**  
**Total New Endpoints**: 38  
**Implementation Priority**: Critical + Important  

---

## SECTION A: PRODUCT REVIEWS (6 endpoints)

### 1. List Product Reviews (Storefront)

```
GET /api/v2/storefront/products/{productId}/reviews
```

**Headers**:
```
Authorization: Bearer {token} (optional)
Accept: application/vnd.api+json
```

**Query Parameters**:
```
filter[status]=approved              # Approved | Pending | Rejected
filter[rating]=5                     # 1-5
filter[verified]=true                # Verified purchases only
sort=-createdAt                      # Sort by date
page[number]=1
page[size]=20
include=user,variant
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "reviews",
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "attributes": {
        "rating": 5,
        "title": "Excellent Quality!",
        "body": "Amazing product, shipped quickly...",
        "verified": true,
        "helpful_count": 24,
        "status": "approved",
        "created_at": "2024-01-15T10:30:00Z",
        "updated_at": "2024-01-15T10:30:00Z"
      },
      "relationships": {
        "product": {
          "data": { "type": "products", "id": "{productId}" }
        },
        "user": {
          "data": { "type": "users", "id": "{userId}" }
        },
        "variant": {
          "data": { "type": "variants", "id": "{variantId}" }
        }
      }
    }
  ],
  "meta": {
    "pagination": {
      "total": 145,
      "count": 20,
      "per_page": 20,
      "current_page": 1,
      "total_pages": 8
    }
  }
}
```

**Validation**:
- `productId` must be valid GUID
- `filter[rating]` must be 1-5
- `filter[status]` must be in (approved, pending, rejected)

**Permissions**: Public (authenticated optional)

---

### 2. Create Product Review (Storefront)

```
POST /api/v2/storefront/products/{productId}/reviews
```

**Headers**:
```
Authorization: Bearer {token}  (REQUIRED)
Content-Type: application/vnd.api+json
Accept: application/vnd.api+json
```

**Request Body**:
```json
{
  "data": {
    "type": "reviews",
    "attributes": {
      "rating": 5,
      "title": "Great product!",
      "body": "This product exceeded my expectations...",
      "variant_id": "550e8400-e29b-41d4-a716-446655440001"
    }
  }
}
```

**Response** (201):
```json
{
  "data": {
    "type": "reviews",
    "id": "550e8400-e29b-41d4-a716-446655440002",
    "attributes": {
      "rating": 5,
      "title": "Great product!",
      "body": "This product exceeded my expectations...",
      "verified": true,
      "status": "pending",
      "helpful_count": 0,
      "created_at": "2024-01-20T14:22:00Z",
      "updated_at": "2024-01-20T14:22:00Z"
    }
  }
}
```

**Validation**:
- `rating` required, must be 1-5 (integer)
- `title` required, max 200 characters
- `body` required, max 5000 characters
- `variant_id` optional, must reference valid variant of product
- User must have purchased product (verified=true if order exists)
- User can only have 1 review per variant

**Permissions**: `reviews.create` (authenticated users)

**Domain Events**:
- `ReviewCreated(reviewId, productId, userId, rating)`
- If `status=approved`: `ReviewApproved(reviewId)`

---

### 3. Get Product Review Details

```
GET /api/v2/storefront/reviews/{reviewId}
```

**Response** (200):
```json
{
  "data": {
    "type": "reviews",
    "id": "{reviewId}",
    "attributes": {
      "rating": 5,
      "title": "Great product!",
      "body": "Full review text...",
      "verified": true,
      "helpful_count": 15,
      "status": "approved",
      "created_at": "2024-01-20T14:22:00Z",
      "updated_at": "2024-01-20T14:22:00Z"
    },
    "relationships": {
      "product": { "data": { "type": "products", "id": "{productId}" } },
      "user": { "data": { "type": "users", "id": "{userId}" } }
    }
  }
}
```

**Permissions**: Public (show only approved reviews to guests)

---

### 4. Update Review (Storefront - Own Reviews Only)

```
PUT /api/v2/storefront/reviews/{reviewId}
```

**Request Body**:
```json
{
  "data": {
    "type": "reviews",
    "id": "{reviewId}",
    "attributes": {
      "rating": 4,
      "title": "Updated review",
      "body": "Updated review text..."
    }
  }
}
```

**Response** (200): Updated review

**Validation**:
- User must be review author (or admin)
- Cannot update if status=rejected
- Update resets status to `pending` for moderation

**Permissions**: `reviews.update` (review author or admin)

---

### 5. Delete Review (Storefront - Own Reviews)

```
DELETE /api/v2/storefront/reviews/{reviewId}
```

**Response** (204): No content

**Permissions**: `reviews.delete` (review author or admin)

**Domain Events**:
- `ReviewDeleted(reviewId, productId)`

---

### 6. List Pending Reviews for Moderation (Admin Only)

```
GET /api/v2/admin/reviews/moderation
```

**Query Parameters**:
```
filter[status]=pending              # pending | flagged | reported
sort=-created_at
page[number]=1
page[size]=50
include=product,user,variant
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "reviews",
      "id": "{reviewId}",
      "attributes": {
        "rating": 2,
        "title": "Bad quality",
        "body": "Poor quality product, breaking down...",
        "verified": true,
        "status": "pending",
        "flagged_reason": null,
        "flag_count": 0,
        "created_at": "2024-01-15T10:30:00Z"
      }
    }
  ],
  "meta": {
    "pagination": { "total": 42, "count": 50 }
  }
}
```

**Endpoints for Moderation Actions**:
```
PATCH /api/v2/admin/reviews/{reviewId}/approve           # Approve review
PATCH /api/v2/admin/reviews/{reviewId}/reject            # Reject review
PATCH /api/v2/admin/reviews/{reviewId}/flag              # Flag for review
```

---

## SECTION B: STOCK TRANSFERS (4 endpoints)

### 1. Create Stock Transfer

```
POST /api/v2/admin/stock-transfers
```

**Request Body**:
```json
{
  "data": {
    "type": "stock_transfers",
    "attributes": {
      "from_stock_location_id": "550e8400-e29b-41d4-a716-446655440001",
      "to_stock_location_id": "550e8400-e29b-41d4-a716-446655440002",
      "variant_id": "550e8400-e29b-41d4-a716-446655440003",
      "quantity": 50,
      "reason": "Rebalancing inventory between warehouses",
      "receive_by": "2024-02-15T23:59:59Z"
    }
  }
}
```

**Response** (201):
```json
{
  "data": {
    "type": "stock_transfers",
    "id": "550e8400-e29b-41d4-a716-446655440004",
    "attributes": {
      "quantity": 50,
      "from_location_name": "Main Warehouse",
      "to_location_name": "Secondary Warehouse",
      "variant_sku": "SKU-12345",
      "status": "pending",
      "reason": "Rebalancing inventory between warehouses",
      "receive_by": "2024-02-15T23:59:59Z",
      "created_at": "2024-01-20T14:22:00Z"
    },
    "relationships": {
      "from_location": { "data": { "type": "stock_locations", "id": "{fromLocationId}" } },
      "to_location": { "data": { "type": "stock_locations", "id": "{toLocationId}" } },
      "variant": { "data": { "type": "variants", "id": "{variantId}" } }
    }
  }
}
```

**Validation**:
- Both locations must exist and be different
- Variant must exist
- Quantity must be > 0 and <= on-hand quantity at source location
- receive_by must be future date

**Permissions**: `inventory.transfer`

**Domain Events**:
- `StockTransferCreated(transferId, fromLocationId, toLocationId, variantId, quantity)`
- If `status=pending`: StockMovement records created for audit trail

---

### 2. Get Stock Transfer

```
GET /api/v2/admin/stock-transfers/{transferId}
```

**Response** (200): Stock transfer details

**Permissions**: `inventory.view`

---

### 3. List Stock Transfers

```
GET /api/v2/admin/stock-transfers
```

**Query Parameters**:
```
filter[status]=pending              # pending | confirmed | completed | canceled
filter[variant_id]={variantId}
filter[from_location_id]={locationId}
filter[to_location_id]={locationId}
sort=-created_at
page[number]=1
page[size]=20
include=variant,from_location,to_location
```

**Response** (200): List of transfers with pagination

**Permissions**: `inventory.view`

---

### 4. Confirm Stock Transfer (Receiving)

```
PATCH /api/v2/admin/stock-transfers/{transferId}/confirm
```

**Request Body**:
```json
{
  "data": {
    "type": "stock_transfers",
    "attributes": {
      "received_quantity": 50,
      "received_at": "2024-02-10T10:30:00Z",
      "notes": "All items received and verified"
    }
  }
}
```

**Response** (200):
```json
{
  "data": {
    "type": "stock_transfers",
    "id": "{transferId}",
    "attributes": {
      "status": "completed",
      "quantity": 50,
      "received_quantity": 50,
      "received_at": "2024-02-10T10:30:00Z"
    }
  }
}
```

**Validation**:
- Transfer status must be "pending" or "confirmed"
- received_quantity must <= transfer quantity
- received_at must be <= now

**Permissions**: `inventory.transfer`

**Domain Events**:
- `StockTransferConfirmed(transferId, receivedQuantity)`
- StockMovement created: source location `-quantity`, dest location `+quantity`

---

## SECTION C: PROMOTION RULES (4 endpoints)

### 1. List Promotion Rules

```
GET /api/v2/admin/promotions/{promotionId}/rules
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "promotion_rules",
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "attributes": {
        "rule_type": "cart_total",              # cart_total | taxon | user | date_range
        "operator": "minimum",                   # minimum | maximum | equals
        "value": "100.00",                      # depends on rule type
        "description": "Minimum cart total $100",
        "active": true,
        "created_at": "2024-01-20T14:22:00Z"
      }
    },
    {
      "type": "promotion_rules",
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "attributes": {
        "rule_type": "taxon",
        "taxon_ids": ["550e8400-e29b-41d4-a716-446655440003"],
        "description": "Only for Electronics category",
        "active": true
      }
    }
  ]
}
```

**Permissions**: `promotions.view`

---

### 2. Create Promotion Rule

```
POST /api/v2/admin/promotions/{promotionId}/rules
```

**Request Body** (Cart Total Rule):
```json
{
  "data": {
    "type": "promotion_rules",
    "attributes": {
      "rule_type": "cart_total",
      "operator": "minimum",
      "value": "100.00",
      "description": "Minimum cart total $100"
    }
  }
}
```

**Request Body** (Taxon Rule):
```json
{
  "data": {
    "type": "promotion_rules",
    "attributes": {
      "rule_type": "taxon",
      "taxon_ids": ["550e8400-e29b-41d4-a716-446655440003", "550e8400-e29b-41d4-a716-446655440004"],
      "description": "Apply only to Electronics or Clothing"
    }
  }
}
```

**Request Body** (User Rule):
```json
{
  "data": {
    "type": "promotion_rules",
    "attributes": {
      "rule_type": "user",
      "user_ids": ["user1", "user2"],
      "description": "VIP customers only"
    }
  }
}
```

**Request Body** (Date Range Rule):
```json
{
  "data": {
    "type": "promotion_rules",
    "attributes": {
      "rule_type": "date_range",
      "starts_at": "2024-02-01T00:00:00Z",
      "ends_at": "2024-02-14T23:59:59Z",
      "description": "Valentine's Day promotion"
    }
  }
}
```

**Response** (201): Created rule

**Validation**:
- Promotion must exist
- rule_type must be one of: cart_total, taxon, user, date_range
- For cart_total: value must be > 0
- For taxon: taxon_ids must reference existing taxons
- For user: user_ids must reference existing users
- For date_range: starts_at < ends_at, both required

**Permissions**: `promotions.manage`

**Domain Events**:
- `PromotionRuleAdded(promotionId, ruleId, ruleType)`

---

### 3. Update Promotion Rule

```
PUT /api/v2/admin/promotions/{promotionId}/rules/{ruleId}
```

**Request Body**: Same as create

**Response** (200): Updated rule

**Permissions**: `promotions.manage`

**Domain Events**:
- `PromotionRuleUpdated(promotionId, ruleId)`

---

### 4. Delete Promotion Rule

```
DELETE /api/v2/admin/promotions/{promotionId}/rules/{ruleId}
```

**Response** (204): No content

**Permissions**: `promotions.manage`

**Domain Events**:
- `PromotionRuleRemoved(promotionId, ruleId)`

---

## SECTION D: PROMOTION ACTIONS (4 endpoints)

### 1. List Promotion Actions

```
GET /api/v2/admin/promotions/{promotionId}/actions
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "promotion_actions",
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "attributes": {
        "action_type": "percentage_discount",  # percentage_discount | fixed_amount | free_shipping | buy_x_get_y
        "value": "20",                         # percentage for percentage_discount
        "description": "20% off entire order",
        "calculator": "percentage",
        "created_at": "2024-01-20T14:22:00Z"
      }
    }
  ]
}
```

**Permissions**: `promotions.view`

---

### 2. Create Promotion Action

```
POST /api/v2/admin/promotions/{promotionId}/actions
```

**Request Body** (Percentage Discount):
```json
{
  "data": {
    "type": "promotion_actions",
    "attributes": {
      "action_type": "percentage_discount",
      "percentage": 25,
      "description": "25% off"
    }
  }
}
```

**Request Body** (Fixed Amount):
```json
{
  "data": {
    "type": "promotion_actions",
    "attributes": {
      "action_type": "fixed_amount",
      "amount": "50.00",
      "currency": "USD",
      "description": "$50 off"
    }
  }
}
```

**Request Body** (Free Shipping):
```json
{
  "data": {
    "type": "promotion_actions",
    "attributes": {
      "action_type": "free_shipping",
      "description": "Free shipping on this order"
    }
  }
}
```

**Request Body** (Buy X Get Y):
```json
{
  "data": {
    "type": "promotion_actions",
    "attributes": {
      "action_type": "buy_x_get_y",
      "buy_quantity": 2,
      "get_quantity": 1,
      "variant_id": "550e8400-e29b-41d4-a716-446655440002",
      "description": "Buy 2 get 1 free"
    }
  }
}
```

**Response** (201): Created action

**Validation**:
- Promotion must exist and not have an action (one action per promotion)
- action_type must be valid
- For percentage: value 0-100
- For fixed_amount: amount > 0, currency must be 3 chars
- For buy_x_get_y: quantities > 0, variant must exist

**Permissions**: `promotions.manage`

**Domain Events**:
- `PromotionActionAdded(promotionId, actionId, actionType)`

---

### 3. Update Promotion Action

```
PUT /api/v2/admin/promotions/{promotionId}/actions/{actionId}
```

**Response** (200): Updated action

**Permissions**: `promotions.manage`

---

### 4. Delete Promotion Action

```
DELETE /api/v2/admin/promotions/{promotionId}/actions/{actionId}
```

**Response** (204): No content

**Permissions**: `promotions.manage`

---

## SECTION E: USER ADDRESSES (6 endpoints)

### 1. List User Addresses

```
GET /api/v2/storefront/addresses
```

**Headers**:
```
Authorization: Bearer {token}  (REQUIRED)
```

**Query Parameters**:
```
filter[type]=shipping              # shipping | billing | both
sort=+position,-created_at
page[number]=1
page[size]=20
include=country,state
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "user_addresses",
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "attributes": {
        "first_name": "John",
        "last_name": "Doe",
        "address1": "123 Main St",
        "address2": "Apt 4B",
        "city": "New York",
        "zipcode": "10001",
        "phone": "+1-555-0000",
        "is_default": true,
        "is_billing_default": false,
        "country_name": "United States",
        "state_name": "New York",
        "created_at": "2024-01-15T10:30:00Z"
      },
      "relationships": {
        "country": { "data": { "type": "countries", "id": "{countryId}" } },
        "state": { "data": { "type": "states", "id": "{stateId}" } }
      }
    }
  ],
  "meta": {
    "pagination": { "total": 3, "count": 3 }
  }
}
```

**Permissions**: `addresses.view` (authenticated user - own addresses only)

---

### 2. Create User Address

```
POST /api/v2/storefront/addresses
```

**Headers**:
```
Authorization: Bearer {token}  (REQUIRED)
Content-Type: application/vnd.api+json
```

**Request Body**:
```json
{
  "data": {
    "type": "user_addresses",
    "attributes": {
      "first_name": "Jane",
      "last_name": "Smith",
      "address1": "456 Oak Ave",
      "address2": "Suite 100",
      "city": "Los Angeles",
      "zipcode": "90001",
      "phone": "+1-555-1111",
      "country_id": "550e8400-e29b-41d4-a716-446655440001",
      "state_id": "550e8400-e29b-41d4-a716-446655440002",
      "set_as_default": false,
      "set_as_billing_default": false
    }
  }
}
```

**Response** (201): Created address

**Validation**:
- first_name, last_name required, max 100
- address1 required, max 255
- address2 optional, max 255
- city required, max 100
- zipcode required, max 20
- phone required, E.164 format
- country_id must reference valid country
- state_id optional (required if country has states)

**Permissions**: `addresses.create` (authenticated user)

**Domain Events**:
- `UserAddressCreated(addressId, userId)`
- If `set_as_default=true`: `UserDefaultAddressSet(addressId, userId)`

---

### 3. Get User Address

```
GET /api/v2/storefront/addresses/{addressId}
```

**Response** (200): Single address

**Permissions**: `addresses.view` (authenticated user - own address only)

---

### 4. Update User Address

```
PUT /api/v2/storefront/addresses/{addressId}
```

**Request Body**: Same structure as create

**Response** (200): Updated address

**Permissions**: `addresses.update` (authenticated user - own address only)

**Domain Events**:
- `UserAddressUpdated(addressId, userId)`

---

### 5. Delete User Address

```
DELETE /api/v2/storefront/addresses/{addressId}
```

**Response** (204): No content

**Permissions**: `addresses.delete` (authenticated user - own address only)

**Domain Events**:
- `UserAddressDeleted(addressId, userId)`

---

### 6. Set Default Address

```
PATCH /api/v2/storefront/addresses/{addressId}/set-default
```

**Request Body**:
```json
{
  "data": {
    "type": "user_addresses",
    "attributes": {
      "is_default": true,
      "type": "shipping"  # shipping | billing
    }
  }
}
```

**Response** (200): Updated address

**Validation**:
- Address must belong to authenticated user
- type must be "shipping" or "billing"

**Permissions**: `addresses.update` (authenticated user)

**Domain Events**:
- `UserDefaultShippingAddressSet(addressId, userId)` if type=shipping
- `UserDefaultBillingAddressSet(addressId, userId)` if type=billing

---

## SECTION F: AUTHENTICATION & TOKEN MANAGEMENT (3 endpoints)

### 1. Refresh JWT Token

```
POST /api/v2/auth/refresh
```

**Headers**:
```
Content-Type: application/json
```

**Request Body**:
```json
{
  "refresh_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response** (200):
```json
{
  "data": {
    "type": "tokens",
    "attributes": {
      "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
      "refresh_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
      "token_type": "Bearer",
      "expires_in": 3600
    }
  }
}
```

**Validation**:
- refresh_token must be valid and not revoked
- refresh_token must not be expired

**Permissions**: Public (no auth header needed)

**Domain Events**:
- `RefreshTokenUsed(userId, tokenId)`

---

### 2. Logout (Revoke Refresh Token)

```
POST /api/v2/auth/logout
```

**Headers**:
```
Authorization: Bearer {token}  (REQUIRED)
Content-Type: application/json
```

**Request Body**:
```json
{
  "refresh_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response** (200):
```json
{
  "data": {
    "type": "auth",
    "attributes": {
      "message": "Successfully logged out"
    }
  }
}
```

**Permissions**: `auth.logout` (authenticated user)

**Domain Events**:
- `RefreshTokenRevoked(userId, tokenId)`

---

### 3. Revoke Refresh Token (Admin)

```
PATCH /api/v2/admin/users/{userId}/tokens/{tokenId}/revoke
```

**Headers**:
```
Authorization: Bearer {token}  (REQUIRED)
```

**Request Body**:
```json
{
  "data": {
    "type": "tokens",
    "attributes": {
      "reason": "User requested password reset"
    }
  }
}
```

**Response** (200): Token revoked

**Permissions**: `auth.admin` or `users.manage`

**Domain Events**:
- `RefreshTokenRevokedByAdmin(userId, tokenId, admin, reason)`

---

## SECTION G: AUDIT LOGS (3 endpoints)

### 1. List Audit Logs

```
GET /api/v2/admin/audit-logs
```

**Query Parameters**:
```
filter[entity_type]=Product            # Product | Order | User | etc
filter[action]=created                 # created | updated | deleted
filter[user_id]={userId}
filter[created_after]=2024-01-01T00:00:00Z
filter[created_before]=2024-02-01T00:00:00Z
sort=-created_at
page[number]=1
page[size]=50
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "audit_logs",
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "attributes": {
        "entity_type": "Product",
        "entity_id": "550e8400-e29b-41d4-a716-446655440002",
        "action": "created",
        "user_id": "admin-001",
        "user_name": "John Admin",
        "old_values": null,
        "new_values": {
          "name": "New Product",
          "slug": "new-product",
          "status": "draft"
        },
        "ip_address": "192.168.1.1",
        "user_agent": "Mozilla/5.0...",
        "created_at": "2024-01-20T14:22:00Z"
      }
    }
  ],
  "meta": {
    "pagination": { "total": 5000, "count": 50 }
  }
}
```

**Permissions**: `audit.view` (admin only)

---

### 2. Get Audit Log Entry

```
GET /api/v2/admin/audit-logs/{logId}
```

**Response** (200): Detailed audit entry

**Permissions**: `audit.view` (admin only)

---

### 3. Export Audit Logs

```
GET /api/v2/admin/audit-logs/export
```

**Query Parameters**:
```
format=csv                        # csv | json | xlsx
from=2024-01-01T00:00:00Z
to=2024-02-01T00:00:00Z
filter[entity_type]=Order
```

**Response** (200):
```
Content-Type: text/csv
Content-Disposition: attachment; filename="audit-logs-2024-01-20.csv"

entity_type,entity_id,action,user_id,user_name,created_at,old_values,new_values
Product,550e8400-e29b-41d4-a716-446655440002,created,admin-001,John Admin,2024-01-20T14:22:00Z,"{}","{""name"":""New Product""}"
...
```

**Permissions**: `audit.export` (admin only)

---

## SECTION H: PERMISSIONS & ROLES (4 endpoints)

### 1. List All Available Permissions

```
GET /api/v2/admin/permissions
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "permissions",
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "attributes": {
        "code": "products.view",
        "name": "View Products",
        "description": "View product catalog",
        "category": "catalog",
        "level": "basic"
      }
    },
    {
      "type": "permissions",
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "attributes": {
        "code": "products.create",
        "name": "Create Products",
        "description": "Create new products",
        "category": "catalog",
        "level": "admin"
      }
    }
  ],
  "meta": {
    "total": 120,
    "grouped_by_category": {
      "catalog": 30,
      "orders": 20,
      "inventory": 15,
      "admin": 55
    }
  }
}
```

**Permissions**: `permissions.view` (admin only)

---

### 2. List Role Permissions

```
GET /api/v2/admin/roles/{roleId}/permissions
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "role_permissions",
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "attributes": {
        "permission_code": "products.view",
        "permission_name": "View Products",
        "assigned_at": "2024-01-15T10:30:00Z"
      }
    }
  ],
  "meta": { "total": 25 }
}
```

**Permissions**: `roles.view` (admin only)

---

### 3. Assign Permission to Role

```
POST /api/v2/admin/roles/{roleId}/permissions
```

**Request Body**:
```json
{
  "data": {
    "type": "role_permissions",
    "attributes": {
      "permission_code": "orders.refund"
    }
  }
}
```

**Response** (201): Permission assigned

**Validation**:
- Role must exist
- Permission code must be valid
- Permission must not already be assigned

**Permissions**: `roles.manage` (admin only)

**Domain Events**:
- `PermissionAssignedToRole(roleId, permissionCode)`

---

### 4. Revoke Permission from Role

```
DELETE /api/v2/admin/roles/{roleId}/permissions/{permissionCode}
```

**Response** (204): Permission revoked

**Permissions**: `roles.manage` (admin only)

**Domain Events**:
- `PermissionRevokedFromRole(roleId, permissionCode)`

---

## SECTION I: LOCATION DATA - COUNTRIES & STATES (4 endpoints)

### 1. List Countries

```
GET /api/v2/shared/countries
```

**Query Parameters**:
```
filter[iso2]=US                   # Search by ISO code
sort=+name,-population
page[number]=1
page[size]=100
include=states
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "countries",
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "attributes": {
        "name": "United States",
        "iso2": "US",
        "iso3": "USA",
        "currency": "USD",
        "timezone": "America/New_York"
      }
    }
  ],
  "meta": { "pagination": { "total": 195, "count": 100 } }
}
```

**Permissions**: Public (no auth required)

---

### 2. Get Country Details

```
GET /api/v2/shared/countries/{countryId}
```

**Query Parameters**:
```
include=states
```

**Response** (200):
```json
{
  "data": {
    "type": "countries",
    "id": "{countryId}",
    "attributes": {
      "name": "United States",
      "iso2": "US",
      "iso3": "USA",
      "currency": "USD",
      "timezone": "America/New_York",
      "has_states": true
    },
    "relationships": {
      "states": {
        "data": [
          { "type": "states", "id": "{stateId1}" },
          { "type": "states", "id": "{stateId2}" }
        ]
      }
    }
  },
  "included": [
    {
      "type": "states",
      "id": "{stateId1}",
      "attributes": {
        "name": "California",
        "code": "CA"
      }
    }
  ]
}
```

**Permissions**: Public

---

### 3. List States by Country

```
GET /api/v2/shared/countries/{countryId}/states
```

**Query Parameters**:
```
filter[code]=CA
sort=+name
page[number]=1
page[size]=100
```

**Response** (200):
```json
{
  "data": [
    {
      "type": "states",
      "id": "{stateId}",
      "attributes": {
        "name": "California",
        "code": "CA",
        "country_id": "{countryId}"
      }
    }
  ],
  "meta": { "pagination": { "total": 50, "count": 50 } }
}
```

**Permissions**: Public

---

### 4. Get State Details

```
GET /api/v2/shared/countries/{countryId}/states/{stateId}
```

**Response** (200):
```json
{
  "data": {
    "type": "states",
    "id": "{stateId}",
    "attributes": {
      "name": "California",
      "code": "CA",
      "country_id": "{countryId}",
      "country_name": "United States"
    },
    "relationships": {
      "country": { "data": { "type": "countries", "id": "{countryId}" } }
    }
  }
}
```

**Permissions**: Public

---

## IMPLEMENTATION SUMMARY

**Total New Endpoints: 38**

| Section | Count | Priority |
|---------|-------|----------|
| A. Product Reviews | 6 | 🔴 Critical |
| B. Stock Transfers | 4 | 🔴 Critical |
| C. Promotion Rules | 4 | 🟡 Important |
| D. Promotion Actions | 4 | 🟡 Important |
| E. User Addresses | 6 | 🟡 Important |
| F. Auth/Tokens | 3 | 🟡 Important |
| G. Audit Logs | 3 | 🟡 Important |
| H. Permissions | 4 | 🟡 Important |
| I. Locations | 4 | 🟡 Important |
| **TOTAL** | **38** | |

---

**Status**: Ready for integration into API_SPECIFICATION.md  
**Next Step**: Create CQRS handlers and controllers for each endpoint

---

## QUICK REFERENCE FOR DEVELOPERS

### For Each New Endpoint, Create:

1. **Domain Model Updates** (if needed) - May already exist
2. **CQRS Command/Query** - `CreateReviewCommand`, `ListProductReviewsQuery`
3. **Validator** - Input validation using FluentValidation
4. **Handler** - `IRequestHandler<CreateReviewCommand, ErrorOr<ReviewResponse>>`
5. **Mapster Config** - Response DTO mapping
6. **Controller/Endpoint** - HTTP routing
7. **Unit Tests** - Domain model behavior tests
8. **Integration Tests** - API endpoint tests

### Example Command Structure:

```csharp
public sealed record CreateReviewCommand(
    Guid ProductId,
    int Rating,
    string Title,
    string Body,
    Guid? VariantId = null
) : IRequest<ErrorOr<ReviewResponse>>;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public sealed class CreateReviewHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser
) : IRequestHandler<CreateReviewCommand, ErrorOr<ReviewResponse>>
{
    public async Task<ErrorOr<ReviewResponse>> Handle(
        CreateReviewCommand command,
        CancellationToken ct)
    {
        var product = await dbContext.Products.FindAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return Error.NotFound();
        
        var review = Review.Create(
            productId: command.ProductId,
            rating: command.Rating,
            title: command.Title,
            body: command.Body,
            userId: currentUser.Id,
            variantId: command.VariantId
        );

        dbContext.Reviews.Add(review);
        await dbContext.SaveChangesAsync(ct);
        
        return review.Adapt<ReviewResponse>();
    }
}
```

---

**Document Generated**: November 30, 2025
