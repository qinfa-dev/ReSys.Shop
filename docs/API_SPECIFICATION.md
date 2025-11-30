# ReSys.Shop API Specification

**Version**: 2.1 (Updated Nov 30, 2025 - 38 NEW Endpoints Added)
**Based on**: Spree Commerce API v2 patterns + ReSys Domain Models  
**Response Format**: JSON:API (https://jsonapi.org/)  
**Authentication**: JWT Bearer Token (RS256)

**⭐ NEW in v2.1**:
- Product Reviews (6 endpoints)
- User Addresses (6 endpoints)
- Stock Transfers (4 endpoints)
- Promotion Rules (4 endpoints)
- Promotion Actions (4 endpoints)
- Authentication & Tokens (3 endpoints)
- Audit Logs (3 endpoints)
- Access Permissions (4 endpoints)
- Location Data (4 endpoints)
- **Total: 38 new endpoints added for 100% domain coverage**

---

## 📋 Table of Contents

### Overview & Setup
1. [Overview](#overview)
2. [Authentication & Authorization](#authentication--authorization)
3. [Response Format](#response-format)
4. [Error Handling](#error-handling)
5. [Rate Limiting](#rate-limiting)
6. [Webhooks](#webhooks)

### Storefront API
7. [Store Context](#store-context)
8. [Products Catalog](#catalog-context---products)
9. [Categories/Taxonomies](#catalog-context---taxonomies)
10. [Shopping Cart](#cart--order-context)
11. [Checkout](#checkout-context)
12. [Account Management](#account-context)
13. [User Addresses](#user-addresses-management-storefront) (6 endpoints) ⭐ NEW
14. [Product Reviews](#product-reviews-management) (6 endpoints) ⭐ NEW
15. [Inventory/Availability](#inventory-context-read-only)

### Admin Platform API
16. [Product Management](#product-catalog-management)
    - List, Create, Update, Delete Products (5 endpoints)
    - Activate, Archive, Draft, Discontinue (4 endpoints)
17. [Product Images](#product-images-management) (5 endpoints)
18. [Product Properties](#product-properties-management) (4 endpoints)
19. [Product Option Types](#product-option-types-management) (3 endpoints)
20. [Product Categories](#product-categories-management) (3 endpoints)
21. [Product Store Visibility](#product-store-visibility) (4 endpoints)
22. [Product Analytics](#product-analytics) (2 endpoints)
23. [Variants Management](#variants-management) (7 endpoints)
24. [Variant Pricing](#variant-pricing) (3 endpoints)
25. [Variant Inventory](#variant-inventory) (4 endpoints)
26. [Variant Options](#variant-options) (3 endpoints)
27. [Variant Images](#variant-images) (3 endpoints)
28. [Stock Locations](#stock-locations-management) (7 endpoints)
29. [Stock Items](#stock-items-management) (5 endpoints)
30. [Stock Item Operations](#stock-item-operations) (4 endpoints)
31. [Stock Item Reports](#stock-item-reports) (3 endpoints)
32. [Stock Transfers](#stock-transfers-management) (4 endpoints) ⭐ NEW
33. [Order Management](#order-management)
    - List, Get, Update, Delete Orders (4 endpoints)
    - State Management (3 endpoints)
    - Line Items (4 endpoints)
    - Shipments (6 endpoints)
    - Payments (5 endpoints)
    - Analytics (3 endpoints)
34. [Promotions Management](#promotion-management) (7 endpoints)
35. [Promotion Rules](#promotion-rules-management) (4 endpoints) ⭐ NEW
36. [Promotion Actions](#promotion-actions-management) (4 endpoints) ⭐ NEW
37. [Taxonomies](#taxonomies-management) (5 endpoints)
38. [Taxons](#taxons-management) (10+ endpoints)
39. [Stores](#stores-management) (6 endpoints)
40. [Payment Methods](#payment-methods-management) (7 endpoints)
41. [Shipping Methods](#shipping-methods-management) (7+ endpoints)

### Infrastructure & System
42. [Authentication & Token Management](#authentication--token-management) (3 endpoints) ⭐ NEW
43. [Audit Logs](#audit-logs-management) (3 endpoints) ⭐ NEW
44. [Access Permissions](#access-permissions-management) (4 endpoints) ⭐ NEW
45. [Location Data - Countries & States](#location-data---countries--states) (4 endpoints) ⭐ NEW

### Additional Sections
46. [Customers & Users](#customer-management)
47. [Analytics & Reporting](#analytics--reporting)

**Total Endpoints**: 188+ (150 documented + 38 new)

---

## Overview

ReSys.Shop provides two distinct APIs:

- **Storefront API** (`/api/v2/storefront`) - Customer-facing operations (browsing, cart, checkout)
- **Admin Platform API** (`/api/v2/admin`) - Admin operations (150+ endpoints for complete management)

Both follow JSON:API specification with resources, relationships, and sparse fieldsets.

---

# STOREFRONT API

**Base URL**: `/api/v2/storefront`  
**Authentication**: Optional (JWT for authenticated users, guest checkout via session/token)

---

## Store Context

### Get Store Information

```
GET /api/v2/storefront/stores/{storeCode}
```

**Query Parameters**:
- `include` - Related resources (e.g., `shipping_methods,payment_methods`)

**Response** (200):
```json
{
  "data": {
    "id": "default-store",
    "type": "store",
    "attributes": {
      "code": "default-store",
      "name": "Fashion Store",
      "description": "Premium fashion retailer",
      "currency": "USD",
      "timezone": "UTC",
      "locale": "en_US",
      "features_enabled": {
        "guest_checkout": true,
        "wishlists": true,
        "reviews": true,
        "recommendations": true
      },
      "operating_hours": {
        "monday_friday": "09:00-18:00",
        "saturday": "10:00-16:00",
        "sunday": "closed"
      },
      "contact_email": "support@fashionstore.com",
      "contact_phone": "+1-800-FASHION",
      "return_policy_url": "/policies/returns",
      "shipping_policy_url": "/policies/shipping"
    },
    "relationships": {
      "default_country": { "data": { "id": "US", "type": "country" } }
    }
  }
}
```

---

## Catalog Context - Products

### List Products (Browsing)

```
GET /api/v2/storefront/products
```

**Query Parameters** (Spree-aligned):
- `filter[ids]` - Comma-separated product IDs
- `filter[skus]` - Comma-separated SKUs
- `filter[name]` - Wildcard product name search
- `filter[price]` - Price range filter (e.g., `10-100`)
- `filter[taxons]` - Category IDs (comma-separated)
- `filter[options][{option_name}]` - Option filtering (e.g., `filter[options][color]=red`)
- `filter[properties][{property_name}]` - Property filtering (e.g., `filter[properties][brand]=Nike`)
- `filter[in_stock]` - Boolean, purchasable products only
- `filter[backorderable]` - Boolean, include backorderable items
- `filter[purchasable]` - Boolean, in stock or backorderable
- `sort` - Field and direction (e.g., `name`, `-price`, `available_on`)
- `page` - Page number (default: 1)
- `per_page` - Items per page (default: 25, max: 100)
- `include` - Related data (e.g., `variants,images,option_types,taxons`)

**Response** (200):
```json
{
  "data": [
    {
      "id": "prod-001",
      "type": "product",
      "attributes": {
        "name": "Classic Blue T-Shirt",
        "description": "Premium cotton t-shirt, perfect for everyday wear",
        "slug": "classic-blue-tshirt",
        "sku": "TS-BLUE-001",
        "purchasable": true,
        "in_stock": true,
        "backorderable": false,
        "available": true,
        "status": "active",
        "price": "29.99",
        "display_price": "$29.99",
        "compare_at_price": "49.99",
        "display_compare_at_price": "$49.99",
        "currency": "USD",
        "available_on": "2024-01-01T00:00:00Z",
        "meta_description": "Shop our premium blue t-shirt",
        "meta_keywords": "t-shirt, blue, cotton",
        "meta_title": "Blue T-Shirt | Fashion Store",
        "created_at": "2024-01-01T00:00:00Z",
        "updated_at": "2024-11-30T00:00:00Z"
      },
      "relationships": {
        "variants": { "data": [{ "id": "var-001", "type": "variant" }] },
        "option_types": { "data": [{ "id": "opt-color", "type": "option_type" }] },
        "product_properties": { "data": [{ "id": "prop-001", "type": "product_property" }] },
        "taxons": { "data": [{ "id": "cat-shirts", "type": "taxon" }] },
        "images": { "data": [{ "id": "img-001", "type": "product_image" }] }
      }
    }
  ],
  "meta": {
    "count": 25,
    "total_count": 250,
    "total_pages": 10,
    "filters": {
      "option_types": [
        {
          "id": "opt-color",
          "name": "color",
          "presentation": "Color",
          "option_values": [
            { "id": "ov-red", "name": "red", "presentation": "Red", "position": 1 },
            { "id": "ov-blue", "name": "blue", "presentation": "Blue", "position": 2 }
          ]
        }
      ],
      "product_properties": [
        {
          "id": "prop-brand",
          "name": "brand",
          "presentation": "Brand",
          "values": [
            { "value": "Nike", "filter_param": "nike" },
            { "value": "Adidas", "filter_param": "adidas" }
          ]
        }
      ]
    }
  },
  "links": {
    "self": "/api/v2/storefront/products",
    "first": "/api/v2/storefront/products?page=1",
    "prev": "/api/v2/storefront/products?page=1",
    "next": "/api/v2/storefront/products?page=2",
    "last": "/api/v2/storefront/products?page=10"
  }
}
```

### Get Product by Slug

```
GET /api/v2/storefront/products/{slug}
```

**Query Parameters**:
- `include` - Related data

**Response** (200): Single product object (same structure as list)

**Side Effects**: Increments view count (tracked in domain event)

---

### List Product Variants

```
GET /api/v2/storefront/products/{productId}/variants
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "var-001",
      "type": "variant",
      "attributes": {
        "sku": "TS-BLUE-SM",
        "name": "Classic Blue T-Shirt - Small",
        "position": 1,
        "price": "29.99",
        "display_price": "$29.99",
        "compare_at_price": "49.99",
        "currency": "USD",
        "purchasable": true,
        "in_stock": true,
        "stock_quantity": 150,
        "backorderable": false,
        "weight": "0.25",
        "height": "10",
        "width": "8",
        "depth": "2"
      },
      "relationships": {
        "product": { "data": { "id": "prod-001", "type": "product" } },
        "images": { "data": [{ "id": "img-001", "type": "product_image" }] },
        "option_values": { "data": [{ "id": "ov-sm", "type": "option_value" }] }
      }
    }
  ]
}
```

---

### Get Product Reviews

```
GET /api/v2/storefront/products/{productId}/reviews
```

**Query Parameters**:
- `filter[status]` - `approved`, `pending`, `rejected` (default: `approved`)
- `sort` - `helpful`, `recent`, `rating` (default: `helpful`)
- `page`, `per_page`

**Response** (200):
```json
{
  "data": [
    {
      "id": "rev-001",
      "type": "review",
      "attributes": {
        "title": "Great quality!",
        "comment": "Really satisfied with this purchase",
        "rating": 5,
        "author": "John D.",
        "helpful_count": 42,
        "status": "approved",
        "created_at": "2024-11-15T10:00:00Z"
      },
      "relationships": {
        "product": { "data": { "id": "prod-001", "type": "product" } },
        "customer": { "data": { "id": "cust-001", "type": "customer" } }
      }
    }
  ],
  "meta": {
    "average_rating": 4.5,
    "total_reviews": 128,
    "rating_distribution": { "5": 95, "4": 25, "3": 5, "2": 2, "1": 1 }
  }
}
```

---

### Submit Product Review

```
POST /api/v2/storefront/products/{productId}/reviews
```

**Authentication**: Required (JWT token)

**Request Body**:
```json
{
  "data": {
    "type": "review",
    "attributes": {
      "title": "Love it!",
      "comment": "Perfect fit and quality",
      "rating": 5
    }
  }
}
```

**Response** (201): Created review (pending approval)

---

### Get Related Products

```
GET /api/v2/storefront/products/{productId}/related
```

**Query Parameters**:
- `relation_type` - `upsell`, `cross_sell`, `similar` (default: all)

**Response** (200): Array of related products

---

### Visual Similarity Search (Unique ReSys Feature)

```
POST /api/v2/storefront/products/search/by-image
```

**Authentication**: Optional

**Request Body** (multipart/form-data):
```
image: <binary file>
limit: 10 (default)
threshold: 0.75 (vector similarity threshold, 0-1)
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "prod-002",
      "type": "product",
      "attributes": {
        "name": "Similar Blue T-Shirt",
        "similarity_score": 0.92
      }
    }
  ],
  "meta": {
    "search_time_ms": 145,
    "vector_model_version": "resnet50-v2"
  }
}
```

---

## Catalog Context - Categories (Taxonomies)

### List Category Tree

```
GET /api/v2/storefront/taxonomies/{taxonomyId}
```

**Response** (200):
```json
{
  "data": {
    "id": "tax-apparel",
    "type": "taxonomy",
    "attributes": {
      "name": "Apparel",
      "description": "Clothing and accessories"
    },
    "relationships": {
      "root": { "data": { "id": "taxon-root", "type": "taxon" } }
    }
  },
  "included": [
    {
      "id": "taxon-root",
      "type": "taxon",
      "attributes": {
        "name": "Apparel",
        "slug": "apparel",
        "description": null,
        "position": 0
      },
      "relationships": {
        "children": {
          "data": [
            { "id": "taxon-mens", "type": "taxon" },
            { "id": "taxon-womens", "type": "taxon" }
          ]
        }
      }
    },
    {
      "id": "taxon-mens",
      "type": "taxon",
      "attributes": {
        "name": "Men",
        "slug": "men",
        "position": 1
      },
      "relationships": {
        "parent": { "data": { "id": "taxon-root", "type": "taxon" } },
        "children": {
          "data": [
            { "id": "taxon-shirts", "type": "taxon" },
            { "id": "taxon-pants", "type": "taxon" }
          ]
        }
      }
    }
  ]
}
```

### Get Category with Products

```
GET /api/v2/storefront/taxonomies/{taxonomyId}/taxons/{taxonSlug}
```

**Response** (200): Taxon with paginated product list

---

## Cart & Order Context

### Get Current Cart

```
GET /api/v2/storefront/carts/current
```

**Authentication**: Optional (guest carts use session token)

**Headers**: 
- `X-Spree-Cart-Token` - For guest carts (provided in login response)

**Response** (200):
```json
{
  "data": {
    "id": "cart-guest-123",
    "type": "order",
    "attributes": {
      "number": null,
      "state": "cart",
      "email": "guest@example.com",
      "currency": "USD",
      "item_count": 2,
      "item_total": "59.98",
      "adjustment_total": "-10.00",
      "promo_total": "-10.00",
      "tax_total": "4.80",
      "shipment_total": "10.00",
      "total": "64.78",
      "display_item_total": "$59.98",
      "display_adjustment_total": "-$10.00",
      "display_tax_total": "$4.80",
      "display_shipment_total": "$10.00",
      "display_total": "$64.78",
      "completed_at": null,
      "created_at": "2024-11-30T12:00:00Z",
      "updated_at": "2024-11-30T12:15:00Z"
    },
    "relationships": {
      "line_items": { "data": [{ "id": "li-001", "type": "line_item" }] },
      "promotions": { "data": [{ "id": "promo-001", "type": "promotion" }] },
      "user": { "data": null }
    }
  },
  "included": [
    {
      "id": "li-001",
      "type": "line_item",
      "attributes": {
        "quantity": 2,
        "price": "29.99",
        "total": "59.98",
        "display_price": "$29.99",
        "display_total": "$59.98"
      },
      "relationships": {
        "variant": { "data": { "id": "var-001", "type": "variant" } }
      }
    }
  ]
}
```

---

### Add Item to Cart

```
POST /api/v2/storefront/carts/current/line_items
```

**Request Body**:
```json
{
  "data": {
    "type": "line_item",
    "attributes": {
      "variant_id": "var-001",
      "quantity": 2
    }
  }
}
```

**Response** (201): Updated cart

**Side Effects**:
- Increments "add to cart" count (domain event)
- Reserves inventory (holds stock)

---

### Update Cart Item Quantity

```
PATCH /api/v2/storefront/carts/current/line_items/{lineItemId}
```

**Request Body**:
```json
{
  "data": {
    "type": "line_item",
    "attributes": {
      "quantity": 5
    }
  }
}
```

**Response** (200): Updated cart

---

### Remove Item from Cart

```
DELETE /api/v2/storefront/carts/current/line_items/{lineItemId}
```

**Response** (200): Updated cart (item removed)

**Side Effects**: Releases inventory reservation

---

### Apply Coupon/Promotion Code

```
POST /api/v2/storefront/carts/current/promotions
```

**Request Body**:
```json
{
  "data": {
    "type": "promotion",
    "attributes": {
      "code": "SUMMER20"
    }
  }
}
```

**Response** (201): Updated cart with discount applied

**Errors**:
- 404: Promotion not found
- 422: Promotion expired, code limit exceeded, or user not eligible

---

### Remove Promotion

```
DELETE /api/v2/storefront/carts/current/promotions/{promotionId}
```

**Response** (200): Updated cart

---

### Merge Guest Cart with User Cart (Post-Login)

```
POST /api/v2/storefront/carts/merge
```

**Authentication**: Required (JWT token)

**Request Body**:
```json
{
  "data": {
    "type": "cart",
    "attributes": {
      "guest_token": "guest-cart-token-123"
    }
  }
}
```

**Response** (200): Merged cart

**Logic**:
- Guest items + User items combined
- Duplicate variants increase quantity
- Promotions re-validated

---

## Checkout Context

### Initiate Checkout

```
POST /api/v2/storefront/checkout/initiate
```

**Authentication**: Optional (creates guest/user account)

**Request Body**:
```json
{
  "data": {
    "type": "order",
    "attributes": {
      "email": "customer@example.com",
      "password": "SecurePassword123",
      "guest_checkout": true
    }
  }
}
```

**Response** (201): Order in `address` state

```json
{
  "data": {
    "id": "ord-001",
    "type": "order",
    "attributes": {
      "number": "R123456789",
      "state": "address",
      "email": "customer@example.com"
    }
  }
}
```

---

### Set Shipping Address

```
PUT /api/v2/storefront/orders/{orderId}/addresses/shipping
```

**Request Body** (Option A: Existing address):
```json
{
  "data": {
    "type": "address",
    "attributes": {
      "address_id": "addr-001"
    }
  }
}
```

**Request Body** (Option B: New address):
```json
{
  "data": {
    "type": "address",
    "attributes": {
      "first_name": "John",
      "last_name": "Doe",
      "address_1": "123 Main St",
      "address_2": "Apt 4B",
      "city": "New York",
      "state": "NY",
      "zipcode": "10001",
      "country_id": "US",
      "phone": "+1-555-0123"
    }
  }
}
```

**Response** (200): Order updated, transitions to `delivery` state

---

### Set Billing Address

```
PUT /api/v2/storefront/orders/{orderId}/addresses/billing
```

**Request Body**:
```json
{
  "data": {
    "type": "address",
    "attributes": {
      "same_as_shipping": true
    }
  }
}
```

**Response** (200): Order updated

---

### Get Available Shipping Methods

```
GET /api/v2/storefront/orders/{orderId}/shipping_methods
```

**Query Parameters**:
- `filter[delivery_type]` - `standard`, `express`, `overnight`, etc.

**Response** (200):
```json
{
  "data": [
    {
      "id": "ship-standard",
      "type": "shipping_method",
      "attributes": {
        "name": "Standard Shipping",
        "description": "Delivery in 5-7 business days",
        "code": "standard",
        "cost": "10.00",
        "display_cost": "$10.00",
        "selected": false
      }
    },
    {
      "id": "ship-express",
      "type": "shipping_method",
      "attributes": {
        "name": "Express Shipping",
        "description": "Delivery in 2-3 business days",
        "code": "express",
        "cost": "25.00",
        "display_cost": "$25.00",
        "selected": false
      }
    }
  ]
}
```

---

### Select Shipping Method

```
PUT /api/v2/storefront/orders/{orderId}/shipping_method
```

**Request Body**:
```json
{
  "data": {
    "type": "shipping_method",
    "attributes": {
      "shipping_method_id": "ship-express"
    }
  }
}
```

**Response** (200): Order updated with shipping cost, transitions to `payment` state

---

### Get Available Payment Methods

```
GET /api/v2/storefront/orders/{orderId}/payment_methods
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "pm-card",
      "type": "payment_method",
      "attributes": {
        "type": "credit_card",
        "name": "Credit Card",
        "description": "Visa, Mastercard, Amex"
      }
    },
    {
      "id": "pm-paypal",
      "type": "payment_method",
      "attributes": {
        "type": "paypal",
        "name": "PayPal",
        "description": "Pay securely with PayPal"
      }
    }
  ]
}
```

---

### Submit Payment

```
POST /api/v2/storefront/orders/{orderId}/payment
```

**Request Body** (Credit Card):
```json
{
  "data": {
    "type": "payment",
    "attributes": {
      "payment_method_id": "pm-card",
      "source_attributes": {
        "number": "4111111111111111",
        "month": "12",
        "year": "2025",
        "verification_value": "123",
        "name": "John Doe"
      }
    }
  }
}
```

**Response** (201): Order updated with payment authorization, transitions to `confirm` state

---

### Review & Complete Order

```
GET /api/v2/storefront/orders/{orderId}
```

**Purpose**: Final review before completion

**Response** (200): Complete order details with all totals

---

### Finalize Order

```
POST /api/v2/storefront/orders/{orderId}/complete
```

**Request Body**: (empty)
```json
{}
```

**Response** (201): Order transitions to `complete` state

**Side Effects**:
- Domain events published:
  - `Order.Events.Completed`
  - `Order.Events.FinalizeInventory` (reduces stock)
  - `Promotion.Events.Used` (if promotion applied)
- Confirmation email sent asynchronously
- Inventory locked/finalized

---

## Account Context

### Register Customer Account

```
POST /api/v2/storefront/account/register
```

**Request Body**:
```json
{
  "data": {
    "type": "user",
    "attributes": {
      "email": "customer@example.com",
      "password": "SecurePassword123",
      "password_confirmation": "SecurePassword123",
      "first_name": "John",
      "last_name": "Doe"
    }
  }
}
```

**Response** (201):
```json
{
  "data": {
    "id": "user-001",
    "type": "user",
    "attributes": {
      "email": "customer@example.com",
      "first_name": "John",
      "last_name": "Doe"
    },
    "relationships": {
      "addresses": { "data": [] }
    }
  },
  "meta": {
    "token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
    "token_type": "Bearer",
    "expires_in": 900,
    "refresh_token": "refresh_token_value"
  }
}
```

---

### Login

```
POST /api/v2/storefront/account/login
```

**Request Body**:
```json
{
  "data": {
    "type": "user",
    "attributes": {
      "email": "customer@example.com",
      "password": "SecurePassword123"
    }
  }
}
```

**Response** (200): User + JWT tokens (same as register)

---

### Refresh Access Token

```
POST /api/v2/storefront/account/refresh-token
```

**Request Body**:
```json
{
  "data": {
    "type": "refresh_token",
    "attributes": {
      "refresh_token": "refresh_token_value"
    }
  }
}
```

**Response** (200): New access token

---

### Get User Profile

```
GET /api/v2/storefront/account/profile
```

**Authentication**: Required

**Response** (200):
```json
{
  "data": {
    "id": "user-001",
    "type": "user",
    "attributes": {
      "email": "customer@example.com",
      "first_name": "John",
      "last_name": "Doe",
      "phone": "+1-555-0123",
      "created_at": "2024-01-01T00:00:00Z"
    },
    "relationships": {
      "addresses": { "data": [{ "id": "addr-001", "type": "address" }] },
      "orders": { "data": [{ "id": "ord-001", "type": "order" }] }
    }
  }
}
```

---

### Update User Profile

```
PATCH /api/v2/storefront/account/profile
```

**Request Body**:
```json
{
  "data": {
    "type": "user",
    "attributes": {
      "first_name": "Jane",
      "phone": "+1-555-9876"
    }
  }
}
```

**Response** (200): Updated user

---

### Change Password

```
PUT /api/v2/storefront/account/password
```

**Request Body**:
```json
{
  "data": {
    "type": "user",
    "attributes": {
      "current_password": "OldPassword123",
      "password": "NewPassword123",
      "password_confirmation": "NewPassword123"
    }
  }
}
```

**Response** (200): Success message

---

### List User Addresses

```
GET /api/v2/storefront/account/addresses
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "addr-001",
      "type": "address",
      "attributes": {
        "first_name": "John",
        "last_name": "Doe",
        "address_1": "123 Main St",
        "city": "New York",
        "state": "NY",
        "zipcode": "10001",
        "country": "US",
        "is_default_billing": true,
        "is_default_shipping": true
      }
    }
  ]
}
```

---

### Create Address

```
POST /api/v2/storefront/account/addresses
```

**Request Body**:
```json
{
  "data": {
    "type": "address",
    "attributes": {
      "first_name": "Jane",
      "last_name": "Doe",
      "address_1": "456 Oak Ave",
      "city": "Los Angeles",
      "state": "CA",
      "zipcode": "90001",
      "country_id": "US",
      "phone": "+1-555-4567",
      "is_default_billing": false,
      "is_default_shipping": false
    }
  }
}
```

**Response** (201): Created address

---

### Update Address

```
PATCH /api/v2/storefront/account/addresses/{addressId}
```

**Response** (200): Updated address

---

### Delete Address

```
DELETE /api/v2/storefront/account/addresses/{addressId}
```

**Response** (204): No content

---

### List Order History

```
GET /api/v2/storefront/account/orders
```

**Query Parameters**:
- `filter[status]` - `complete`, `canceled` (default: all)
- `sort` - `-completed_at`, `number`, etc.
- `page`, `per_page`

**Response** (200): Paginated order list

---

### Get Order Details

```
GET /api/v2/storefront/account/orders/{orderNumber}
```

**Response** (200): Complete order with line items, shipments, payments

---

### Request Order Cancellation

```
POST /api/v2/storefront/account/orders/{orderNumber}/cancel
```

**Request Body**:
```json
{
  "data": {
    "type": "order",
    "attributes": {
      "cancellation_reason": "Changed my mind"
    }
  }
}
```

**Response** (200): Order canceled (only if not yet shipped)

**Side Effects**:
- Inventory released
- Payment refunded (if captured)
- Domain event: `Order.Events.Canceled`

---

## Inventory Context (Read-Only)

### Check Variant Availability

```
GET /api/v2/storefront/variants/{variantId}/availability
```

**Response** (200):
```json
{
  "data": {
    "id": "var-001",
    "type": "variant_availability",
    "attributes": {
      "in_stock": true,
      "backorderable": false,
      "quantity_available": 150,
      "quantity_on_hand": 150,
      "quantity_reserved": 5
    }
  }
}
```

---

### Bulk Check Availability

```
POST /api/v2/storefront/variants/availability/bulk
```

**Request Body**:
```json
{
  "data": {
    "type": "bulk_availability",
    "attributes": {
      "variant_ids": ["var-001", "var-002", "var-003"]
    }
  }
}
```

**Response** (200): Array of availability for each variant

---

# ADMIN PLATFORM API

**Base URL**: `/api/v2/admin`  
**Authentication**: Required (JWT Bearer Token)  
**Authorization**: Role-Based Access Control (RBAC)

---

## Store Management

### List Stores

```
GET /api/v2/admin/stores
```

**Permissions**: `stores.view`

**Response** (200): Store list with configuration

---

### Create Store

```
POST /api/v2/admin/stores
```

**Permissions**: `stores.create`

**Request Body**:
```json
{
  "data": {
    "type": "store",
    "attributes": {
      "code": "store-eu",
      "name": "Fashion Store EU",
      "url": "https://eu.fashionstore.com",
      "currency": "EUR",
      "timezone": "Europe/Paris",
      "locale": "fr_FR",
      "default": false
    }
  }
}
```

**Response** (201): Created store

---

### Update Store Configuration

```
PUT /api/v2/admin/stores/{storeId}
```

**Permissions**: `stores.update`

**Request Body**:
```json
{
  "data": {
    "type": "store",
    "attributes": {
      "name": "Updated Name",
      "settings": {
        "guest_checkout_enabled": true,
        "reviews_enabled": true
      }
    }
  }
}
```

**Response** (200): Updated store

---

### Manage Store Products

```
GET /api/v2/admin/stores/{storeId}/products
```

**Permissions**: `stores.view`, `products.view`

**Response**: Products linked to this store with visibility settings

---

```
POST /api/v2/admin/stores/{storeId}/products/{productId}
```

**Permissions**: `stores.update`

**Request Body**:
```json
{
  "data": {
    "type": "store_product",
    "attributes": {
      "visible": true,
      "featured": false,
      "position": 1
    }
  }
}
```

---

### Manage Stock Locations

```
GET /api/v2/admin/stores/{storeId}/stock-locations
```

**Response**: Stock locations linked to store with fulfillment priority

---

```
POST /api/v2/admin/stores/{storeId}/stock-locations/{locationId}
```

**Permissions**: `stores.update`

**Request Body**:
```json
{
  "data": {
    "type": "store_stock_location",
    "attributes": {
      "priority": 1,
      "can_fulfill_orders": true
    }
  }
}
```

---

## Product Catalog Management

### List Products (Admin View)

```
GET /api/v2/admin/products
```

**Permissions**: `products.view`

**Query Parameters** (Spree-aligned):
- `filter[ids]` - Comma-separated product IDs
- `filter[status]` - `draft`, `active`, `archived`
- `filter[store_id]` - Filter by store (multi-store isolation)
- `filter[search]` - Search by name/description
- `filter[created_at_min]` - Created after date
- `filter[created_at_max]` - Created before date
- `filter[updated_at_min]` - Updated after date
- `filter[updated_at_max]` - Updated before date
- `sort` - `-created_at`, `name`, `-updated_at`
- `page`, `per_page` - Pagination
- `include` - Related resources (`variants`, `images`, `option_types`, `properties`, `taxons`)

**Response** (200):
```json
{
  "data": [
    {
      "id": "prod-123",
      "type": "product",
      "attributes": {
        "name": "Classic Blue T-Shirt",
        "presentation": "Classic Blue T-Shirt",
        "slug": "classic-blue-t-shirt",
        "description": "A comfortable cotton t-shirt",
        "status": "active",
        "available_on": "2024-01-15T10:00:00Z",
        "make_active_at": null,
        "discontinue_on": null,
        "is_digital": false,
        "meta_title": "Classic Blue T-Shirt | ReSys Shop",
        "meta_description": "Buy high-quality classic blue t-shirt",
        "meta_keywords": "t-shirt, blue, cotton",
        "purchasable": true,
        "in_stock": true,
        "backorderable": false,
        "total_on_hand": 150,
        "created_at": "2024-01-10T08:00:00Z",
        "updated_at": "2024-01-15T12:30:00Z",
        "public_metadata": {
          "featured": true,
          "collection": "summer-2024"
        }
      },
      "relationships": {
        "variants": { "data": [{ "id": "var-001", "type": "variant" }] },
        "images": { "data": [{ "id": "img-001", "type": "product_image" }] },
        "option_types": { "data": [{ "id": "opt-color", "type": "option_type" }] },
        "properties": { "data": [{ "id": "prop-brand", "type": "product_property" }] },
        "taxons": { "data": [{ "id": "tax-mens", "type": "taxon" }] }
      }
    }
  ],
  "meta": {
    "count": 10,
    "total_count": 450,
    "page": 1,
    "per_page": 10,
    "total_pages": 45
  },
  "links": {
    "self": "/api/v2/admin/products?page=1",
    "first": "/api/v2/admin/products?page=1",
    "prev": null,
    "next": "/api/v2/admin/products?page=2",
    "last": "/api/v2/admin/products?page=45"
  }
}
```

---

### Get Product by ID

```
GET /api/v2/admin/products/{productId}
```

**Permissions**: `products.view`

**Query Parameters**:
- `include` - Relationships to include

**Response** (200): Product with relationships

---

### Create Product

```
POST /api/v2/admin/products
```

**Permissions**: `products.create`

**Request Body**:
```json
{
  "data": {
    "type": "product",
    "attributes": {
      "name": "New Product",
      "presentation": "New Product Display Name",
      "description": "Product description",
      "slug": "new-product",
      "is_digital": false,
      "meta_title": "New Product | Store",
      "meta_description": "Buy new product",
      "meta_keywords": "new, product",
      "available_on": "2024-12-01T00:00:00Z",
      "make_active_at": null,
      "discontinue_on": null,
      "public_metadata": {
        "featured": false
      },
      "private_metadata": {
        "internal_sku": "PROD-NEW-001"
      }
    }
  }
}
```

**Response** (201): Created product with auto-generated master variant

**Side Effects**:
- Master variant created automatically
- Domain event: `Product.Events.Created` published

---

### Update Product

```
PUT /api/v2/admin/products/{productId}
```

**Permissions**: `products.update`

**Request Body** (all attributes optional):
```json
{
  "data": {
    "type": "product",
    "attributes": {
      "name": "Updated Product Name",
      "presentation": "Updated Display Name",
      "description": "Updated description",
      "slug": "updated-slug",
      "meta_title": "Updated Title",
      "meta_description": "Updated description",
      "meta_keywords": "updated, keywords",
      "is_digital": false,
      "public_metadata": {
        "featured": true,
        "sale_price": "29.99"
      }
    }
  }
}
```

**Response** (200): Updated product

**Side Effects**:
- Domain event: `Product.Events.ProductUpdated` published
- Related taxons touched for search index

**Validation Rules**:
- Name: required, max 200 chars
- Slug: max 100 chars, lowercase alphanumeric with hyphens
- Description: max 500 chars
- Slug must be unique per store context
- If presentation not provided, uses name value

---

### Delete Product

```
DELETE /api/v2/admin/products/{productId}
```

**Permissions**: `products.delete`

**Response** (204): No content

**Side Effects**:
- Soft-delete (marked as deleted, remains in DB)
- Domain event: `Product.Events.ProductDeleted` published

**Validation Rules**:
- Cannot delete if has completed orders
- Cannot delete if has published variants in active stores

---

### Activate Product

```
PATCH /api/v2/admin/products/{productId}/activate
```

**Permissions**: `products.update`

**Request Body** (optional):
```json
{
  "data": {
    "type": "product",
    "attributes": {}
  }
}
```

**Response** (200): Product with status = active

**Side Effects**:
- Domain event: `Product.Events.ProductActivated` published

---

### Archive Product

```
PATCH /api/v2/admin/products/{productId}/archive
```

**Permissions**: `products.update`

**Response** (200): Product with status = archived

**Side Effects**:
- Domain event: `Product.Events.ProductArchived` published

---

### Draft Product

```
PATCH /api/v2/admin/products/{productId}/draft
```

**Permissions**: `products.update`

**Response** (200): Product with status = draft

**Side Effects**:
- Domain event: `Product.Events.ProductDrafted` published

---

### Discontinue Product

```
PATCH /api/v2/admin/products/{productId}/discontinue
```

**Permissions**: `products.update`

**Request Body** (optional):
```json
{
  "data": {
    "type": "product",
    "attributes": {
      "discontinue_on": "2024-12-31T23:59:59Z"
    }
  }
}
```

**Response** (200): Product with discontinue_on set and status = archived

**Side Effects**:
- Domain event: `Product.Events.ProductDiscontinued` published

---

## Product Images Management

### List Product Images

```
GET /api/v2/admin/products/{productId}/images
```

**Permissions**: `products.view`

**Query Parameters**:
- `sort` - `-position`, `created_at`
- `page`, `per_page` - Pagination

**Response** (200):
```json
{
  "data": [
    {
      "id": "img-001",
      "type": "product_image",
      "attributes": {
        "url": "https://cdn.example.com/products/tshirt-blue.jpg",
        "alt": "Blue T-Shirt Front View",
        "type": "primary",
        "position": 1,
        "width": 1200,
        "height": 1200,
        "dimensions_unit": "px",
        "content_type": "image/jpeg",
        "created_at": "2024-01-15T10:00:00Z"
      }
    }
  ]
}
```

---

### Upload Product Image

```
POST /api/v2/admin/products/{productId}/images
```

**Permissions**: `products.update`

**Request** (multipart/form-data):
```
image: <binary file>
alt: "Blue T-Shirt Front View"
type: "primary"
position: 1
width: 1200
height: 1200
dimensions_unit: "px"
```

**Response** (201): Created image

**Side Effects**:
- Async vector embedding generation for visual similarity search
- Domain event: `Product.Events.ProductImageAdded` published

**Validation Rules**:
- Image required (binary)
- Alt text: required, max 255 chars
- Type: primary, secondary, or custom
- Position: auto-assigned if not provided
- Supported formats: JPEG, PNG, WebP

---

### Update Product Image

```
PUT /api/v2/admin/products/{productId}/images/{imageId}
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "product_image",
    "attributes": {
      "alt": "Updated Alt Text",
      "type": "primary",
      "position": 1
    }
  }
}
```

**Response** (200): Updated image

**Side Effects**:
- Domain event: `Product.Events.ProductAssetUpdated` published

---

### Delete Product Image

```
DELETE /api/v2/admin/products/{productId}/images/{imageId}
```

**Permissions**: `products.update`

**Response** (204): No content

**Side Effects**:
- Domain event: `Product.Events.ProductImageRemoved` published

---

### Reorder Product Images

```
PATCH /api/v2/admin/products/{productId}/images/reorder
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "product_images",
    "attributes": {
      "positions": {
        "img-001": 1,
        "img-002": 2,
        "img-003": 3
      }
    }
  }
}
```

**Response** (200): All images with new positions

---

## Product Properties Management

### List Product Properties

```
GET /api/v2/admin/products/{productId}/properties
```

**Permissions**: `products.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "prod-prop-001",
      "type": "product_property",
      "attributes": {
        "property_id": "prop-brand",
        "property_name": "Brand",
        "value": "Nike"
      },
      "relationships": {
        "property": { "data": { "id": "prop-brand", "type": "property" } }
      }
    }
  ]
}
```

---

### Add/Update Property

```
POST /api/v2/admin/products/{productId}/properties
PUT /api/v2/admin/products/{productId}/properties/{propertyId}
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "product_property",
    "attributes": {
      "property_id": "prop-brand",
      "value": "Nike"
    }
  }
}
```

**Response** (201/200): Created/updated property

**Side Effects**:
- Domain event: `Product.Events.ProductPropertyAdded` published

---

### Remove Property

```
DELETE /api/v2/admin/products/{productId}/properties/{propertyId}
```

**Permissions**: `products.update`

**Response** (204): No content

**Side Effects**:
- Domain event: `Product.Events.ProductPropertyRemoved` published

---

## Product Option Types Management

### List Option Types

```
GET /api/v2/admin/products/{productId}/option-types
```

**Permissions**: `products.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "pot-001",
      "type": "product_option_type",
      "attributes": {
        "name": "Color",
        "presentation": "Color",
        "position": 1
      },
      "relationships": {
        "option_type": { "data": { "id": "opt-color", "type": "option_type" } }
      }
    }
  ]
}
```

---

### Add Option Type to Product

```
POST /api/v2/admin/products/{productId}/option-types
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "product_option_type",
    "attributes": {
      "option_type_id": "opt-color",
      "position": 1
    }
  }
}
```

**Response** (201): Added option type

**Side Effects**:
- Domain event: `Product.Events.ProductOptionTypeAdded` published

---

### Remove Option Type

```
DELETE /api/v2/admin/products/{productId}/option-types/{optionTypeId}
```

**Permissions**: `products.update`

**Response** (204): No content

**Side Effects**:
- Domain event: `Product.Events.ProductOptionTypeRemoved` published

---

## Product Categories Management

### List Product Categories

```
GET /api/v2/admin/products/{productId}/categories
```

**Permissions**: `products.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "tax-mens",
      "type": "taxon",
      "attributes": {
        "name": "Men's Clothing",
        "slug": "mens-clothing",
        "description": "Men's clothing collection"
      }
    }
  ]
}
```

---

### Add Product to Category

```
POST /api/v2/admin/products/{productId}/categories
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "classification",
    "attributes": {
      "taxon_id": "tax-mens"
    }
  }
}
```

**Response** (201): Added to category

**Side Effects**:
- Domain event: `Product.Events.ProductCategoryAdded` published

---

### Remove Product from Category

```
DELETE /api/v2/admin/products/{productId}/categories/{taxonId}
```

**Permissions**: `products.update`

**Response** (204): No content

**Side Effects**:
- Domain event: `Product.Events.ProductCategoryRemoved` published

---

## Product Store Visibility

### List Stores with Product

```
GET /api/v2/admin/products/{productId}/stores
```

**Permissions**: `products.view`

**Response** (200): Stores where product is available with visibility settings

---

### Add Product to Store

```
POST /api/v2/admin/products/{productId}/stores
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "store_product",
    "attributes": {
      "store_id": "store-us",
      "visible": true,
      "featured": false,
      "position": 1
    }
  }
}
```

**Response** (201): Added to store

**Side Effects**:
- Domain event: `Product.Events.ProductAddedToStore` published

---

### Update Store Settings

```
PUT /api/v2/admin/products/{productId}/stores/{storeId}
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "store_product",
    "attributes": {
      "visible": true,
      "featured": true,
      "position": 2
    }
  }
}
```

**Response** (200): Updated settings

**Side Effects**:
- Domain event: `Product.Events.ProductStoreSettingsUpdated` published

---

### Remove Product from Store

```
DELETE /api/v2/admin/products/{productId}/stores/{storeId}
```

**Permissions**: `products.update`

**Response** (204): No content

**Side Effects**:
- Domain event: `Product.Events.ProductRemovedFromStore` published

---

## Product Analytics

### Get Product Analytics

```
GET /api/v2/admin/products/{productId}/analytics
```

**Permissions**: `products.view`, `analytics.view`

**Response** (200):
```json
{
  "data": {
    "id": "prod-123",
    "type": "product_analytics",
    "attributes": {
      "views": 1250,
      "add_to_cart_count": 85,
      "conversions": 22,
      "conversion_rate": "25.88%",
      "avg_time_on_page": 145,
      "bounce_rate": "12%",
      "revenue": "1210.50",
      "units_sold": 22
    }
  }
}
```

---

### Get Sales History

```
GET /api/v2/admin/products/{productId}/sales
```

**Permissions**: `products.view`, `analytics.view`

**Query Parameters**:
- `filter[date_from]` - Start date
- `filter[date_to]` - End date
- `filter[store_id]` - Filter by store
- `sort` - `-date`, `units_sold`

**Response** (200): Sales data grouped by day/week/month

---

## Variants Management

### List Product Variants

```
GET /api/v2/admin/products/{productId}/variants
```

**Permissions**: `products.view`

**Query Parameters**:
- `filter[sku]` - Search by SKU
- `filter[barcode]` - Search by barcode
- `sort` - `position`, `-created_at`
- `page`, `per_page`
- `include` - `prices`, `stock_items`, `option_values`, `images`

**Response** (200):
```json
{
  "data": [
    {
      "id": "var-001",
      "type": "variant",
      "attributes": {
        "sku": "TSHIRT-BLUE-LG",
        "barcode": "5901234123456",
        "position": 1,
        "track_inventory": true,
        "purchasable": true,
        "in_stock": true,
        "backorderable": false,
        "is_master": false,
        "discontinue_on": null,
        "created_at": "2024-01-15T10:00:00Z"
      },
      "relationships": {
        "prices": { "data": [{ "id": "price-001", "type": "price" }] },
        "stock_items": { "data": [{ "id": "stock-001", "type": "stock_item" }] },
        "option_values": { "data": [{ "id": "opt-val-blue", "type": "option_value" }] },
        "images": { "data": [{ "id": "var-img-001", "type": "variant_image" }] }
      }
    }
  ]
}
```

---

### Get Variant by ID

```
GET /api/v2/admin/variants/{variantId}
```

**Permissions**: `products.view`

**Query Parameters**:
- `include` - Related resources

**Response** (200): Single variant with relationships

---

### Create Variant

```
POST /api/v2/admin/products/{productId}/variants
```

**Permissions**: `products.create`

**Request Body**:
```json
{
  "data": {
    "type": "variant",
    "attributes": {
      "sku": "TSHIRT-BLUE-LG",
      "barcode": "5901234123456",
      "position": 2,
      "track_inventory": true,
      "option_values": [
        {
          "option_type_id": "opt-color",
          "value": "Blue"
        },
        {
          "option_type_id": "opt-size",
          "value": "Large"
        }
      ]
    }
  }
}
```

**Response** (201): Created variant

**Side Effects**:
- Domain event: `Product.Events.VariantAdded` published

**Validation Rules**:
- SKU: required, max 255 chars, unique per product
- Barcode: optional, max 255 chars
- Option values: must match product's option types
- Position: auto-assigned if not provided

---

### Update Variant

```
PUT /api/v2/admin/variants/{variantId}
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "variant",
    "attributes": {
      "sku": "TSHIRT-BLUE-LG-UPDATED",
      "barcode": "5901234123456",
      "track_inventory": true,
      "position": 2
    }
  }
}
```

**Response** (200): Updated variant

**Side Effects**:
- Domain event: `Product.Events.VariantUpdated` published

---

### Delete Variant

```
DELETE /api/v2/admin/variants/{variantId}
```

**Permissions**: `products.delete`

**Response** (204): No content

**Side Effects**:
- Domain event: `Product.Events.VariantRemoved` published
- Inventory movements created for reserved quantities

**Validation Rules**:
- Cannot delete master variant
- Cannot delete if has completed order line items

---

### Discontinue Variant

```
PATCH /api/v2/admin/variants/{variantId}/discontinue
```

**Permissions**: `products.update`

**Request Body** (optional):
```json
{
  "data": {
    "type": "variant",
    "attributes": {
      "discontinue_on": "2024-12-31T23:59:59Z"
    }
  }
}
```

**Response** (200): Discontinued variant

**Side Effects**:
- Domain event: `Product.Events.VariantUpdated` published

---

## Variant Pricing

### List Variant Prices

```
GET /api/v2/admin/variants/{variantId}/prices
```

**Permissions**: `products.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "price-001",
      "type": "price",
      "attributes": {
        "amount": "49.99",
        "compare_at_amount": "79.99",
        "currency": "USD",
        "created_at": "2024-01-15T10:00:00Z"
      }
    }
  ]
}
```

---

### Add Price (Currency)

```
POST /api/v2/admin/variants/{variantId}/prices
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "price",
    "attributes": {
      "amount": "49.99",
      "compare_at_amount": "79.99",
      "currency": "USD"
    }
  }
}
```

**Response** (201): Created price

**Validation Rules**:
- Amount: required, decimal, > 0
- Compare at amount: optional, >= amount (sale price)
- Currency: required, ISO 4217 code (USD, EUR, GBP, etc.)
- Only one price per currency per variant

---

### Update Price

```
PUT /api/v2/admin/variants/{variantId}/prices/{priceId}
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "price",
    "attributes": {
      "amount": "44.99",
      "compare_at_amount": "74.99"
    }
  }
}
```

**Response** (200): Updated price

**Side Effects**:
- Domain event: `Product.Events.PriceDiscountChanged` published

---

### Delete Price

```
DELETE /api/v2/admin/variants/{variantId}/prices/{priceId}
```

**Permissions**: `products.update`

**Response** (204): No content

---

## Variant Inventory

### Get Stock Levels

```
GET /api/v2/admin/variants/{variantId}/stock
```

**Permissions**: `inventory.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "stock-001",
      "type": "stock_item",
      "attributes": {
        "quantity_on_hand": 100,
        "quantity_reserved": 5,
        "quantity_available": 95,
        "stock_location_name": "Warehouse A",
        "track_inventory": true
      },
      "relationships": {
        "stock_location": { "data": { "id": "loc-001", "type": "stock_location" } }
      }
    }
  ]
}
```

---

### Set Stock

```
POST /api/v2/admin/variants/{variantId}/stock
```

**Permissions**: `inventory.adjust`

**Request Body**:
```json
{
  "data": {
    "type": "stock",
    "attributes": {
      "stock_location_id": "loc-001",
      "quantity_on_hand": 150,
      "quantity_reserved": 10
    }
  }
}
```

**Response** (201): Stock item created/updated

---

### Adjust Stock

```
PATCH /api/v2/admin/variants/{variantId}/stock/adjust
```

**Permissions**: `inventory.adjust`

**Request Body**:
```json
{
  "data": {
    "type": "stock_adjustment",
    "attributes": {
      "stock_location_id": "loc-001",
      "quantity": -5,
      "reason": "Damaged goods"
    }
  }
}
```

**Response** (200): Updated stock

**Side Effects**:
- Stock movement recorded with originator (Adjustment, Shipment, etc.)
- Domain event published if low/out of stock

---

### Get Stock Movements

```
GET /api/v2/admin/variants/{variantId}/stock/movements
```

**Permissions**: `inventory.view`

**Query Parameters**:
- `filter[date_from]` - Start date
- `filter[date_to]` - End date
- `filter[stock_location_id]` - Filter by location
- `sort` - `-created_at`, `quantity`
- `page`, `per_page`

**Response** (200):
```json
{
  "data": [
    {
      "id": "move-001",
      "type": "stock_movement",
      "attributes": {
        "quantity": -5,
        "originator": "Shipment",
        "reason": "Order fulfillment",
        "created_at": "2024-01-20T14:30:00Z"
      },
      "relationships": {
        "stock_location": { "data": { "id": "loc-001", "type": "stock_location" } }
      }
    }
  ]
}
```

---

## Variant Options

### List Variant Options

```
GET /api/v2/admin/variants/{variantId}/options
```

**Permissions**: `products.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "opt-val-blue",
      "type": "option_value",
      "attributes": {
        "option_type_name": "Color",
        "value": "Blue",
        "position": 1
      }
    }
  ]
}
```

---

### Set/Update Option Value

```
POST /api/v2/admin/variants/{variantId}/options
PUT /api/v2/admin/variants/{variantId}/options/{optionValueId}
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "option_value",
    "attributes": {
      "option_type_id": "opt-color",
      "value": "Blue"
    }
  }
}
```

**Response** (201/200): Created/updated option

---

### Remove Option Value

```
DELETE /api/v2/admin/variants/{variantId}/options/{optionValueId}
```

**Permissions**: `products.update`

**Response** (204): No content

---

## Variant Images

### List Variant Images

```
GET /api/v2/admin/variants/{variantId}/images
```

**Permissions**: `products.view`

**Response** (200): Variant-specific images

---

### Upload Variant Image

```
POST /api/v2/admin/variants/{variantId}/images
```

**Permissions**: `products.update`

**Request** (multipart/form-data):
```
image: <binary file>
alt: "Variant image"
position: 1
```

**Response** (201): Created variant image

**Side Effects**:
- Async vector embedding generation

---

### Delete Variant Image

```
DELETE /api/v2/admin/variants/{variantId}/images/{imageId}
```

**Permissions**: `products.update`

**Response** (204): No content

---

### Manage Option Types

```
POST /api/v2/admin/products/{productId}/option-types


---

### Bulk Product Operations

```
POST /api/v2/admin/products/bulk/activate
POST /api/v2/admin/products/bulk/archive
POST /api/v2/admin/products/bulk/update
```

**Permissions**: `products.update`

**Request Body**:
```json
{
  "data": {
    "type": "bulk_action",
    "attributes": {
      "product_ids": ["prod-001", "prod-002"],
      "action": "activate",
      "updates": { "featured": true }
    }
  }
}
```

---

## Stock Locations Management

### List Stock Locations

```
GET /api/v2/admin/stock-locations
```

**Permissions**: `inventory.view`

**Query Parameters**:
- `filter[active]` - Boolean, active locations only
- `filter[store_id]` - Filter by store
- `sort` - `name`, `-created_at`
- `page`, `per_page`
- `include` - `stores`, `stock_items`

**Response** (200):
```json
{
  "data": [
    {
      "id": "loc-001",
      "type": "stock_location",
      "attributes": {
        "name": "New York Warehouse",
        "code": "ny-main",
        "address_line_1": "123 Warehouse Ln",
        "city": "New York",
        "state": "NY",
        "postal_code": "10001",
        "country": "US",
        "active": true,
        "default": true,
        "can_fulfill_orders": true,
        "created_at": "2024-01-01T00:00:00Z"
      },
      "relationships": {
        "stores": { "data": [{ "id": "store-us", "type": "store" }] },
        "stock_items": { "data": [{ "id": "stock-001", "type": "stock_item" }] }
      }
    }
  ]
}
```

---

### Get Stock Location

```
GET /api/v2/admin/stock-locations/{locationId}
```

**Permissions**: `inventory.view`

**Response** (200): Single location with relationships

---

### Create Stock Location

```
POST /api/v2/admin/stock-locations
```

**Permissions**: `inventory.create`

**Request Body**:
```json
{
  "data": {
    "type": "stock_location",
    "attributes": {
      "name": "Los Angeles Warehouse",
      "code": "la-west",
      "address_line_1": "456 Distribution Center Dr",
      "address_line_2": "Suite 200",
      "city": "Los Angeles",
      "state": "CA",
      "postal_code": "90001",
      "country": "US",
      "phone": "+1-555-7890",
      "active": true,
      "default": false,
      "can_fulfill_orders": true
    }
  }
}
```

**Response** (201): Created location

**Validation Rules**:
- Name: required, max 255 chars
- Code: required, unique, max 100 chars, alphanumeric with hyphens
- Address fields: required
- Active: boolean, default true
- Default: only one per system
- Can fulfill orders: boolean, default true

---

### Update Stock Location

```
PUT /api/v2/admin/stock-locations/{locationId}
```

**Permissions**: `inventory.update`

**Request Body** (all attributes optional):
```json
{
  "data": {
    "type": "stock_location",
    "attributes": {
      "name": "Updated Name",
      "active": true,
      "can_fulfill_orders": false,
      "default": false
    }
  }
}
```

**Response** (200): Updated location

---

### Delete Stock Location

```
DELETE /api/v2/admin/stock-locations/{locationId}
```

**Permissions**: `inventory.delete`

**Response** (204): No content

**Validation Rules**:
- Cannot delete if has stock items
- Cannot delete if is default location
- Cannot delete if linked to orders

---

### Link Location to Store

```
POST /api/v2/admin/stock-locations/{locationId}/stores
```

**Permissions**: `inventory.update`

**Request Body**:
```json
{
  "data": {
    "type": "store_stock_location",
    "attributes": {
      "store_id": "store-us",
      "priority": 1,
      "can_fulfill_orders": true
    }
  }
}
```

**Response** (201): Linked to store

---

### Unlink Location from Store

```
DELETE /api/v2/admin/stock-locations/{locationId}/stores/{storeId}
```

**Permissions**: `inventory.update`

**Response** (204): No content

---

### Get Location Stock Summary

```
GET /api/v2/admin/stock-locations/{locationId}/stock
```

**Permissions**: `inventory.view`

**Response** (200):
```json
{
  "data": {
    "id": "loc-001",
    "type": "stock_location_summary",
    "attributes": {
      "total_on_hand": 5000,
      "total_reserved": 150,
      "total_available": 4850,
      "low_stock_count": 12,
      "out_of_stock_count": 3
    }
  }
}
```

---

### Get Location Movement History

```
GET /api/v2/admin/stock-locations/{locationId}/movements
```

**Permissions**: `inventory.view`

**Query Parameters**:
- `filter[date_from]` - Start date
- `filter[date_to]` - End date
- `filter[originator]` - Filter by movement type (Shipment, Adjustment, Transfer, etc.)
- `sort` - `-created_at`, `quantity`
- `page`, `per_page`

**Response** (200): Movement history

---

## Stock Items Management

### List Stock Items

```
GET /api/v2/admin/stock-items
```

**Permissions**: `inventory.view`

**Query Parameters**:
- `filter[variant_id]` - Filter by variant
- `filter[location_id]` - Filter by location
- `filter[low_stock]` - Boolean, low stock only
- `filter[out_of_stock]` - Boolean, out of stock only
- `filter[sku]` - Search by SKU
- `sort` - `sku`, `-quantity_on_hand`, `-created_at`
- `page`, `per_page`
- `include` - `variant`, `stock_location`

**Response** (200):
```json
{
  "data": [
    {
      "id": "stock-001",
      "type": "stock_item",
      "attributes": {
        "quantity_on_hand": 150,
        "quantity_reserved": 10,
        "quantity_available": 140,
        "quantity_backordered": 0,
        "track_inventory": true,
        "backorderable": false,
        "low_stock_threshold": 20,
        "is_low_stock": false,
        "created_at": "2024-01-15T10:00:00Z"
      },
      "relationships": {
        "variant": { "data": { "id": "var-001", "type": "variant" } },
        "stock_location": { "data": { "id": "loc-001", "type": "stock_location" } }
      }
    }
  ]
}
```

---

### Get Stock Item

```
GET /api/v2/admin/stock-items/{stockItemId}
```

**Permissions**: `inventory.view`

**Query Parameters**:
- `include` - Related resources

**Response** (200): Single stock item

---

### Create Stock Item

```
POST /api/v2/admin/stock-items
```

**Permissions**: `inventory.create`

**Request Body**:
```json
{
  "data": {
    "type": "stock_item",
    "attributes": {
      "variant_id": "var-001",
      "stock_location_id": "loc-001",
      "quantity_on_hand": 100,
      "quantity_reserved": 0,
      "track_inventory": true,
      "backorderable": false,
      "low_stock_threshold": 20
    }
  }
}
```

**Response** (201): Created stock item

**Validation Rules**:
- Variant: required
- Stock Location: required
- Quantity on hand: non-negative integer
- Reserved: non-negative integer
- Only one stock item per variant per location

---

### Update Stock Item

```
PUT /api/v2/admin/stock-items/{stockItemId}
```

**Permissions**: `inventory.update`

**Request Body**:
```json
{
  "data": {
    "type": "stock_item",
    "attributes": {
      "track_inventory": true,
      "backorderable": false,
      "low_stock_threshold": 20
    }
  }
}
```

**Response** (200): Updated stock item

---

### Delete Stock Item

```
DELETE /api/v2/admin/stock-items/{stockItemId}
```

**Permissions**: `inventory.delete`

**Response** (204): No content

**Validation Rules**:
- Cannot delete if quantity_on_hand > 0
- Cannot delete if has reserved quantity
- Cannot delete if has open transfers

---

## Stock Item Operations

### Adjust Stock Quantity

```
POST /api/v2/admin/stock-items/{stockItemId}/adjust
```

**Permissions**: `inventory.adjust`

**Request Body**:
```json
{
  "data": {
    "type": "stock_adjustment",
    "attributes": {
      "quantity": -5,
      "reason": "Damaged goods",
      "originator": "Adjustment"
    }
  }
}
```

**Response** (200): Updated stock item with movement

**Side Effects**:
- Stock movement created with originator and reason
- Domain event published if crossing thresholds (low stock, out of stock)

**Validation Rules**:
- Quantity: can be positive or negative
- Resulting quantity_on_hand cannot be negative (unless backorderable)
- Reason: required, max 500 chars

---

### Reserve Stock

```
POST /api/v2/admin/stock-items/{stockItemId}/reserve
```

**Permissions**: `inventory.adjust`

**Request Body**:
```json
{
  "data": {
    "type": "stock_reservation",
    "attributes": {
      "quantity": 10,
      "order_id": "order-123"
    }
  }
}
```

**Response** (200): Updated stock with reserved quantity

**Side Effects**:
- Quantity available decreases by reserved amount
- Linked to order for fulfillment tracking

**Validation Rules**:
- Quantity: > 0
- Cannot reserve more than available
- Order: required, must exist

---

### Release Reserved Stock

```
POST /api/v2/admin/stock-items/{stockItemId}/release
```

**Permissions**: `inventory.adjust`

**Request Body**:
```json
{
  "data": {
    "type": "stock_release",
    "attributes": {
      "quantity": 10,
      "reason": "Order canceled",
      "order_id": "order-123"
    }
  }
}
```

**Response** (200): Updated stock with released quantity

**Side Effects**:
- Quantity reserved decreases
- Quantity available increases

---

### Confirm Shipment (Deduct from Stock)

```
POST /api/v2/admin/stock-items/{stockItemId}/ship
```

**Permissions**: `inventory.adjust`

**Request Body**:
```json
{
  "data": {
    "type": "shipment_confirm",
    "attributes": {
      "quantity": 5,
      "shipment_id": "ship-123"
    }
  }
}
```

**Response** (200): Updated stock with shipment confirmed

**Side Effects**:
- Quantity on hand reduced
- Quantity reserved reduced by same amount
- Stock movement with originator "Shipment"

---

## Stock Item Reports

### Get Movement History

```
GET /api/v2/admin/stock-items/{stockItemId}/movements
```

**Permissions**: `inventory.view`

**Query Parameters**:
- `filter[date_from]` - Start date
- `filter[date_to]` - End date
- `filter[originator]` - Movement type filter
- `sort` - `-created_at`, `quantity`
- `page`, `per_page`

**Response** (200):
```json
{
  "data": [
    {
      "id": "move-001",
      "type": "stock_movement",
      "attributes": {
        "quantity": -5,
        "quantity_on_hand_after": 145,
        "originator": "Shipment",
        "reason": "Order #12345 fulfillment",
        "reference": "ship-123",
        "created_at": "2024-01-20T14:30:00Z"
      }
    }
  ]
}
```

---

### Create Movement Record

```
POST /api/v2/admin/stock-items/{stockItemId}/movements
```

**Permissions**: `inventory.adjust`

**Request Body**:
```json
{
  "data": {
    "type": "stock_movement",
    "attributes": {
      "quantity": -10,
      "originator": "Adjustment",
      "reason": "Physical inventory count correction",
      "reference": "COUNT-2024-01"
    }
  }
}
```

**Response** (201): Movement recorded

---

### Low Stock Alert

```
GET /api/v2/admin/stock-items/low-stock
```

**Permissions**: `inventory.view`

**Query Parameters**:
- `filter[location_id]` - Filter by location
- `filter[store_id]` - Filter by store
- `page`, `per_page`

**Response** (200): Items below low stock threshold

---

### Out of Stock Alert

```
GET /api/v2/admin/stock-items/out-of-stock
```

**Permissions**: `inventory.view`

**Query Parameters**:
- `filter[location_id]` - Filter by location
- `filter[store_id]` - Filter by store
- `page`, `per_page`

**Response** (200): Items with zero quantity and not backorderable

---

### Stock by Location Report

```
GET /api/v2/admin/stock-items/by-location
```

**Permissions**: `inventory.view`, `analytics.view`

**Query Parameters**:
- `filter[variant_id]` - Filter by variant
- `filter[store_id]` - Filter by store

**Response** (200):
```json
{
  "data": [
    {
      "id": "var-001",
      "type": "variant_stock_report",
      "attributes": {
        "sku": "TSHIRT-BLUE-LG",
        "product_name": "Blue T-Shirt",
        "total_on_hand": 350,
        "total_reserved": 25,
        "total_available": 325,
        "locations": [
          {
            "location_name": "NY Warehouse",
            "on_hand": 150,
            "reserved": 10,
            "available": 140
          },
          {
            "location_name": "LA Warehouse",
            "on_hand": 200,
            "reserved": 15,
            "available": 185
          }
        ]
      }
    }
  ]
}
```

---

## Inventory Management

## Order Management

### List Orders

```
GET /api/v2/admin/orders
```

**Permissions**: `orders.view`

**Query Parameters**:
- `filter[store_id]` - Filter by store
- `filter[state]` - `cart`, `address`, `delivery`, `payment`, `confirm`, `complete`, `canceled`
- `filter[number]` - Order number search
- `filter[email]` - Customer email filter
- `filter[created_at_min]` - Created after date
- `filter[created_at_max]` - Created before date
- `filter[completed_at_min]` - Completed after date
- `filter[completed_at_max]` - Completed before date
- `sort` - `-number`, `-created_at`, `total`
- `page`, `per_page`
- `include` - `line_items`, `shipments`, `payments`, `addresses`

**Response** (200):
```json
{
  "data": [
    {
      "id": "ord-001",
      "type": "order",
      "attributes": {
        "number": "100001",
        "state": "complete",
        "email": "customer@example.com",
        "currency": "USD",
        "total_cents": 4999,
        "subtotal_cents": 3999,
        "tax_total_cents": 400,
        "shipping_total_cents": 600,
        "promo_total_cents": -1000,
        "created_at": "2024-01-15T10:00:00Z",
        "completed_at": "2024-01-20T14:30:00Z"
      },
      "relationships": {
        "line_items": { "data": [{ "id": "li-001", "type": "line_item" }] },
        "shipments": { "data": [{ "id": "ship-001", "type": "shipment" }] },
        "payments": { "data": [{ "id": "pay-001", "type": "payment" }] }
      }
    }
  ]
}
```

---

### Get Order by ID

```
GET /api/v2/admin/orders/{orderId}
```

**Permissions**: `orders.view`

**Query Parameters**:
- `include` - Relationships to include

**Response** (200): Complete order with all relationships

---

### Update Order

```
PUT /api/v2/admin/orders/{orderId}
```

**Permissions**: `orders.update`

**Request Body** (optional):
```json
{
  "data": {
    "type": "order",
    "attributes": {
      "email": "newemail@example.com",
      "notes": "VIP customer"
    }
  }
}
```

**Response** (200): Updated order

---

### Delete Order

```
DELETE /api/v2/admin/orders/{orderId}
```

**Permissions**: `orders.delete`

**Response** (204): No content

**Validation Rules**:
- Cannot delete completed orders
- Cannot delete if has shipments
- Hard delete (removes order permanently)

---

## Order State Management

### Mark Order Complete

```
PATCH /api/v2/admin/orders/{orderId}/complete
```

**Permissions**: `orders.update`

**Request Body** (optional):
```json
{
  "data": {
    "type": "order",
    "attributes": {}
  }
}
```

**Response** (200): Order with state = complete

**Side Effects**:
- Inventory finalized for all shipments
- Domain event: `Order.Events.Completed` published

**Validation Rules**:
- Must be in confirm state
- All line items must be accounted for

---

### Cancel Order

```
PATCH /api/v2/admin/orders/{orderId}/cancel
```

**Permissions**: `orders.cancel`

**Request Body**:
```json
{
  "data": {
    "type": "order",
    "attributes": {
      "cancellation_reason": "Customer request"
    }
  }
}
```

**Response** (200): Order with state = canceled

**Side Effects**:
- Inventory released from all stock locations
- Payment refunded if captured
- Domain event: `Order.Events.Canceled` published

**Validation Rules**:
- Cannot cancel if already completed
- Cannot cancel if already shipped (partial refund required)

---

### Resume Canceled Order

```
PATCH /api/v2/admin/orders/{orderId}/resume
```

**Permissions**: `orders.update`

**Response** (200): Order restored to previous state

**Validation Rules**:
- Order must be in canceled state
- Inventory must be available again

---

## Order Line Items

### List Line Items

```
GET /api/v2/admin/orders/{orderId}/line-items
```

**Permissions**: `orders.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "li-001",
      "type": "line_item",
      "attributes": {
        "variant_sku": "TSHIRT-BLUE-LG",
        "product_name": "Blue T-Shirt",
        "variant_presentation": "Size: Large, Color: Blue",
        "quantity": 2,
        "unit_price_cents": 1999,
        "total_price_cents": 3998,
        "tax_total_cents": 320
      },
      "relationships": {
        "variant": { "data": { "id": "var-001", "type": "variant" } }
      }
    }
  ]
}
```

---

### Add Line Item

```
POST /api/v2/admin/orders/{orderId}/line-items
```

**Permissions**: `orders.update`

**Request Body**:
```json
{
  "data": {
    "type": "line_item",
    "attributes": {
      "variant_id": "var-001",
      "quantity": 2
    }
  }
}
```

**Response** (201): Added line item

**Side Effects**:
- Inventory reserved
- Order totals recalculated

---

### Update Line Item

```
PUT /api/v2/admin/orders/{orderId}/line-items/{lineItemId}
```

**Permissions**: `orders.update`

**Request Body**:
```json
{
  "data": {
    "type": "line_item",
    "attributes": {
      "quantity": 3
    }
  }
}
```

**Response** (200): Updated line item

**Side Effects**:
- Inventory adjusted
- Totals recalculated

---

### Remove Line Item

```
DELETE /api/v2/admin/orders/{orderId}/line-items/{lineItemId}
```

**Permissions**: `orders.update`

**Response** (204): No content

**Side Effects**:
- Inventory released
- Totals recalculated

---

## Order Shipments

### List Shipments

```
GET /api/v2/admin/orders/{orderId}/shipments
```

**Permissions**: `orders.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "ship-001",
      "type": "shipment",
      "attributes": {
        "number": "SHIP-100001-1",
        "state": "shipped",
        "stock_location_name": "NY Warehouse",
        "tracking_number": "1Z999AA10123456784",
        "carrier": "UPS",
        "shipped_at": "2024-01-18T09:00:00Z",
        "delivered_at": null
      },
      "relationships": {
        "line_items": { "data": [{ "id": "li-001", "type": "line_item" }] }
      }
    }
  ]
}
```

---

### Create Shipment

```
POST /api/v2/admin/orders/{orderId}/shipments
```

**Permissions**: `orders.fulfill`

**Request Body**:
```json
{
  "data": {
    "type": "shipment",
    "attributes": {
      "stock_location_id": "loc-001",
      "line_items": [
        {
          "line_item_id": "li-001",
          "quantity": 2
        }
      ]
    }
  }
}
```

**Response** (201): Created shipment

**Side Effects**:
- Shipment number generated
- Inventory reserved for fulfillment

**Validation Rules**:
- Stock location must exist
- Line items must belong to order
- Quantity cannot exceed line item quantity

---

### Update Shipment

```
PUT /api/v2/admin/orders/{orderId}/shipments/{shipmentId}
```

**Permissions**: `orders.fulfill`

**Request Body**:
```json
{
  "data": {
    "type": "shipment",
    "attributes": {
      "tracking_number": "1Z999AA10123456784",
      "carrier": "UPS"
    }
  }
}
```

**Response** (200): Updated shipment

---

### Mark Shipped

```
PATCH /api/v2/admin/orders/{orderId}/shipments/{shipmentId}/ship
```

**Permissions**: `orders.fulfill`

**Request Body**:
```json
{
  "data": {
    "type": "shipment",
    "attributes": {
      "tracking_number": "1Z999AA10123456784",
      "carrier": "UPS"
    }
  }
}
```

**Response** (200): Shipment state = shipped

**Side Effects**:
- Inventory deducted from stock location
- Customer notified with tracking info
- Stock movements created

---

### Mark Delivered

```
PATCH /api/v2/admin/orders/{orderId}/shipments/{shipmentId}/deliver
```

**Permissions**: `orders.fulfill`

**Request Body** (optional):
```json
{
  "data": {
    "type": "shipment",
    "attributes": {
      "delivered_at": "2024-01-20T14:30:00Z"
    }
  }
}
```

**Response** (200): Shipment state = delivered

**Side Effects**:
- Customer notified
- Return window starts

---

### Cancel Shipment

```
DELETE /api/v2/admin/orders/{orderId}/shipments/{shipmentId}
```

**Permissions**: `orders.fulfill`

**Response** (204): No content

**Side Effects**:
- Inventory released
- Stock movements reversed

---

## Order Payments

### List Payments

```
GET /api/v2/admin/orders/{orderId}/payments
```

**Permissions**: `orders.view`

**Response** (200):
```json
{
  "data": [
    {
      "id": "pay-001",
      "type": "payment",
      "attributes": {
        "amount_cents": 4999,
        "payment_method_type": "credit_card",
        "payment_state": "captured",
        "response_code": "APPROVED",
        "created_at": "2024-01-15T10:00:00Z"
      }
    }
  ]
}
```

---

### Create Payment

```
POST /api/v2/admin/orders/{orderId}/payments
```

**Permissions**: `orders.payment`

**Request Body**:
```json
{
  "data": {
    "type": "payment",
    "attributes": {
      "amount_cents": 4999,
      "payment_method_id": "pm-001"
    }
  }
}
```

**Response** (201): Created payment

---

### Capture Payment

```
PATCH /api/v2/admin/orders/{orderId}/payments/{paymentId}/capture
```

**Permissions**: `orders.payment`

**Request Body** (optional):
```json
{
  "data": {
    "type": "payment",
    "attributes": {
      "amount_cents": 4999
    }
  }
}
```

**Response** (200): Payment captured

**Side Effects**:
- Payment state = captured
- Funds deducted from customer account

---

### Void Payment

```
PATCH /api/v2/admin/orders/{orderId}/payments/{paymentId}/void
```

**Permissions**: `orders.payment`

**Response** (200): Payment voided

**Side Effects**:
- Payment state = void
- Authorization canceled

---

### Refund Payment

```
PATCH /api/v2/admin/orders/{orderId}/payments/{paymentId}/refund
```

**Permissions**: `orders.refund`

**Request Body**:
```json
{
  "data": {
    "type": "refund",
    "attributes": {
      "amount_cents": 4999,
      "reason": "Customer request"
    }
  }
}
```

**Response** (200): Refund processed

**Side Effects**:
- Payment state = refunded
- Funds returned to customer

---

## Order Analytics

### Get Order Statistics

```
GET /api/v2/admin/orders/statistics
```

**Permissions**: `orders.view`, `analytics.view`

**Response** (200):
```json
{
  "data": {
    "type": "order_statistics",
    "attributes": {
      "total_orders": 5420,
      "orders_today": 45,
      "pending_shipments": 123,
      "average_order_value": "125.50",
      "repeat_customer_rate": "35%"
    }
  }
}
```

---

### Get Revenue Report

```
GET /api/v2/admin/orders/revenue
```

**Permissions**: `orders.view`, `analytics.view`

**Query Parameters**:
- `filter[date_from]` - Start date
- `filter[date_to]` - End date
- `filter[store_id]` - Filter by store
- `group_by` - `day`, `week`, `month`

**Response** (200): Revenue breakdown by period

---

### Get Orders by Status

```
GET /api/v2/admin/orders/by-status
```

**Permissions**: `orders.view`, `analytics.view`

**Response** (200): Orders grouped by state with counts

---

## Promotion Management

### List Promotions

```
GET /api/v2/admin/promotions
```

**Permissions**: `promotions.view`

**Query Parameters**:
- `filter[status]` - `draft`, `active`, `paused`, `expired`
- `filter[type]` - Promotion type

---

### Create Promotion

```
POST /api/v2/admin/promotions
```

**Permissions**: `promotions.create`

**Request Body**:
```json
{
  "data": {
    "type": "promotion",
    "attributes": {
      "name": "Summer Sale 20% Off",
      "type": "order_discount",
      "discount_type": "percentage",
      "discount_value": 20,
      "code": "SUMMER20",
      "description": "20% off all items",
      "starts_at": "2024-06-01T00:00:00Z",
      "ends_at": "2024-08-31T23:59:59Z",
      "usage_limit": 1000,
      "usage_limit_per_user": 1,
      "requires_coupon_code": true,
      "minimum_order_amount": 50
    }
  }
}
```

**Response** (201): Created promotion

---

### Activate/Pause Promotion

```
POST /api/v2/admin/promotions/{promotionId}/activate
POST /api/v2/admin/promotions/{promotionId}/pause
```

**Permissions**: `promotions.update`

---

### Get Promotion Usage Statistics

```
GET /api/v2/admin/promotions/{promotionId}/usage
```

**Permissions**: `promotions.view`

**Response** (200):
```json
{
  "data": {
    "id": "promo-001",
    "type": "promotion_usage",
    "attributes": {
      "usage_count": 342,
      "orders_count": 320,
      "revenue_impact": "6400.00",
      "average_discount": "18.76",
      "last_used_at": "2024-11-30T18:30:00Z"
    }
  }
}
```

---

## Customer Management

### List Customers

```
GET /api/v2/admin/customers
```

**Permissions**: `customers.view`

**Query Parameters**:
- `filter[search]` - Email/name search
- `filter[registered_from]` - Date range
- `filter[order_count_min]` - Minimum orders
- `sort` - `-created_at`, `lifetime_value`

---

### Get Customer Details

```
GET /api/v2/admin/customers/{customerId}
```

**Response** (200):
```json
{
  "data": {
    "id": "cust-001",
    "type": "customer",
    "attributes": {
      "email": "customer@example.com",
      "first_name": "John",
      "last_name": "Doe",
      "phone": "+1-555-0123",
      "lifetime_value": "1250.00",
      "order_count": 5,
      "created_at": "2024-01-01T00:00:00Z",
      "last_order_date": "2024-11-15T00:00:00Z"
    },
    "relationships": {
      "orders": { "data": [{ "id": "ord-001", "type": "order" }] },
      "addresses": { "data": [{ "id": "addr-001", "type": "address" }] }
    }
  }
}
```

---

### Add Customer Note

```
POST /api/v2/admin/customers/{customerId}/notes
```

**Permissions**: `customers.update`

---

## Review Management

### List Reviews

```
GET /api/v2/admin/reviews
```

**Permissions**: `reviews.view`

**Query Parameters**:
- `filter[status]` - `pending`, `approved`, `rejected`
- `filter[product_id]`
- `filter[rating]` - Star rating

---

### Approve/Reject Review

```
POST /api/v2/admin/reviews/{reviewId}/approve
POST /api/v2/admin/reviews/{reviewId}/reject
```

**Permissions**: `reviews.moderate`

**Request Body** (for rejection):
```json
{
  "data": {
    "type": "review",
    "attributes": {
      "rejection_reason": "Inappropriate content"
    }
  }
}
```

---

## Analytics & Reporting

### Dashboard Overview

```
GET /api/v2/admin/analytics/dashboard
```

**Permissions**: `analytics.view`

**Response** (200):
```json
{
  "data": {
    "type": "dashboard",
    "attributes": {
      "sales_today": "2500.00",
      "orders_today": 15,
      "customers_today": 8,
      "inventory_alerts": 5,
      "low_stock_items": 12,
      "out_of_stock_items": 3
    }
  }
}
```

---

### Sales Analytics

```
GET /api/v2/admin/analytics/sales/overview
GET /api/v2/admin/analytics/sales/by-product
GET /api/v2/admin/analytics/sales/by-category
```

**Permissions**: `analytics.view`

**Query Parameters**:
- `period` - `today`, `week`, `month`, `year`
- `store_id`

---

### Inventory Analytics

```
GET /api/v2/admin/analytics/inventory/low-stock
GET /api/v2/admin/analytics/inventory/turnover
```

---

### Customer Analytics

```
GET /api/v2/admin/analytics/customers/acquisition
GET /api/v2/admin/analytics/customers/lifetime-value
```

---

## System Configuration

## Taxonomies Management

### List Taxonomies

```
GET /api/v2/admin/taxonomies
```

**Permissions**: `taxonomy.view`

**Query Parameters**:
- `sort` - `name`, `-created_at`
- `page`, `per_page`
- `include` - `taxons`

**Response** (200):
```json
{
  "data": [
    {
      "id": "tax-001",
      "type": "taxonomy",
      "attributes": {
        "name": "Categories",
        "code": "categories",
        "description": "Product categories taxonomy",
        "position": 1,
        "created_at": "2024-01-01T00:00:00Z"
      },
      "relationships": {
        "taxons": { "data": [{ "id": "taxon-1", "type": "taxon" }] }
      }
    }
  ]
}
```

---

### Get Taxonomy

```
GET /api/v2/admin/taxonomies/{taxonomyId}
```

**Permissions**: `taxonomy.view`

**Query Parameters**:
- `include` - `taxons`

**Response** (200): Single taxonomy with taxons tree

---

### Create Taxonomy

```
POST /api/v2/admin/taxonomies
```

**Permissions**: `taxonomy.create`

**Request Body**:
```json
{
  "data": {
    "type": "taxonomy",
    "attributes": {
      "name": "Product Types",
      "code": "product_types",
      "description": "Product type classification"
    }
  }
}
```

**Response** (201): Created taxonomy

**Validation Rules**:
- Name: required, max 255 chars
- Code: required, unique, max 100 chars, alphanumeric with underscores

---

### Update Taxonomy

```
PUT /api/v2/admin/taxonomies/{taxonomyId}
```

**Permissions**: `taxonomy.update`

**Request Body**:
```json
{
  "data": {
    "type": "taxonomy",
    "attributes": {
      "name": "Updated Name",
      "description": "Updated description"
    }
  }
}
```

**Response** (200): Updated taxonomy

---

### Delete Taxonomy

```
DELETE /api/v2/admin/taxonomies/{taxonomyId}
```

**Permissions**: `taxonomy.delete`

**Response** (204): No content

**Validation Rules**:
- Cannot delete if has taxons

---

## Taxons Management

### List Taxons (Tree)

```
GET /api/v2/admin/taxonomies/{taxonomyId}/taxons
```

**Permissions**: `taxonomy.view`

**Query Parameters**:
- `include` - Nested `children`, `products`

**Response** (200):
```json
{
  "data": [
    {
      "id": "taxon-1",
      "type": "taxon",
      "attributes": {
        "name": "Men's Clothing",
        "slug": "mens-clothing",
        "description": "Men's clothing collection",
        "position": 1,
        "lft": 1,
        "rgt": 8,
        "depth": 0,
        "permalink": "/categories/mens-clothing"
      },
      "relationships": {
        "children": { "data": [{ "id": "taxon-2", "type": "taxon" }] },
        "products": { "data": [{ "id": "prod-1", "type": "product" }] }
      }
    }
  ]
}
```

---

### List Taxons (Flat)

```
GET /api/v2/admin/taxonomies/{taxonomyId}/taxons/flat
```

**Permissions**: `taxonomy.view`

**Query Parameters**:
- `sort` - `name`, `position`
- `page`, `per_page`

**Response** (200): Flat list of all taxons

---

### Get Taxon

```
GET /api/v2/admin/taxons/{taxonId}
```

**Permissions**: `taxonomy.view`

**Response** (200): Single taxon with parent and children

---

### Create Taxon

```
POST /api/v2/admin/taxonomies/{taxonomyId}/taxons
```

**Permissions**: `taxonomy.create`

**Request Body**:
```json
{
  "data": {
    "type": "taxon",
    "attributes": {
      "name": "T-Shirts",
      "slug": "t-shirts",
      "description": "T-Shirt collection",
      "parent_id": "taxon-1",
      "position": 1
    }
  }
}
```

**Response** (201): Created taxon

**Side Effects**:
- Nested set model lft/rgt updated
- Permalink generated

---

### Update Taxon

```
PUT /api/v2/admin/taxons/{taxonId}
```

**Permissions**: `taxonomy.update`

**Request Body**:
```json
{
  "data": {
    "type": "taxon",
    "attributes": {
      "name": "Updated Name",
      "slug": "updated-slug",
      "description": "Updated description"
    }
  }
}
```

**Response** (200): Updated taxon

---

### Delete Taxon

```
DELETE /api/v2/admin/taxons/{taxonId}
```

**Permissions**: `taxonomy.delete`

**Response** (204): No content

**Validation Rules**:
- Cannot delete if has children
- Cannot delete if has products

---

### Move Taxon in Hierarchy

```
PATCH /api/v2/admin/taxons/{taxonId}/move
```

**Permissions**: `taxonomy.update`

**Request Body**:
```json
{
  "data": {
    "type": "taxon_move",
    "attributes": {
      "parent_id": "taxon-999",
      "position": 1
    }
  }
}
```

**Response** (200): Taxon with updated position

**Side Effects**:
- Nested set model reorganized
- Permalinks updated for all children
- Slugs regenerated

---

### Manage Taxon Images

```
GET /api/v2/admin/taxons/{taxonId}/images
POST /api/v2/admin/taxons/{taxonId}/images
DELETE /api/v2/admin/taxons/{taxonId}/images/{imageId}
```

**Permissions**: `taxonomy.update`

**Request** (POST, multipart/form-data):
```
image: <binary file>
alt: "Taxon image"
position: 1
```

---

### Manage Taxon Products

```
GET /api/v2/admin/taxons/{taxonId}/products
POST /api/v2/admin/taxons/{taxonId}/products
DELETE /api/v2/admin/taxons/{taxonId}/products/{productId}
```

**Permissions**: `taxonomy.update`, `products.update`

**Request Body** (POST):
```json
{
  "data": {
    "type": "product",
    "attributes": {
      "product_id": "prod-001",
      "position": 1
    }
  }
}
```

---

### Manage Automatic Taxons Rules

```
GET /api/v2/admin/taxons/{taxonId}/rules
POST /api/v2/admin/taxons/{taxonId}/rules
PUT /api/v2/admin/taxons/{taxonId}/rules/{ruleId}
DELETE /api/v2/admin/taxons/{taxonId}/rules/{ruleId}
```

**Permissions**: `taxonomy.update`

**Request Body** (POST/PUT):
```json
{
  "data": {
    "type": "taxon_rule",
    "attributes": {
      "name": "Size: Large",
      "type": "property_match",
      "conditions": {
        "property_id": "prop-size",
        "value": "Large"
      }
    }
  }
}
```

**Response**: Created/updated rule

---

### Regenerate Automatic Taxon Products

```
POST /api/v2/admin/taxons/{taxonId}/rules/regenerate
```

**Permissions**: `taxonomy.update`

**Request Body** (optional):
```json
{
  "data": {
    "type": "regenerate_request",
    "attributes": {
      "dry_run": false
    }
  }
}
```

**Response** (200):
```json
{
  "data": {
    "type": "regenerate_result",
    "attributes": {
      "added_count": 15,
      "removed_count": 3,
      "total_count": 250
    }
  }
}
```

---

## Stores Management

### List Stores

```
GET /api/v2/admin/stores
```

**Permissions**: `stores.view`

**Query Parameters**:
- `filter[active]` - Boolean
- `sort` - `name`, `-created_at`
- `page`, `per_page`
- `include` - `products`, `stock_locations`

**Response** (200):
```json
{
  "data": [
    {
      "id": "store-us",
      "type": "store",
      "attributes": {
        "name": "US Store",
        "code": "us",
        "url": "https://us.example.com",
        "currency": "USD",
        "timezone": "America/New_York",
        "locale": "en_US",
        "active": true,
        "default": true,
        "created_at": "2024-01-01T00:00:00Z"
      },
      "relationships": {
        "products": { "data": [{ "id": "prod-1", "type": "product" }] },
        "stock_locations": { "data": [{ "id": "loc-1", "type": "stock_location" }] }
      }
    }
  ]
}
```

---

### Get Store

```
GET /api/v2/admin/stores/{storeId}
```

**Permissions**: `stores.view`

**Response** (200): Single store with relationships

---

### Create Store

```
POST /api/v2/admin/stores
```

**Permissions**: `stores.create`

**Request Body**:
```json
{
  "data": {
    "type": "store",
    "attributes": {
      "name": "EU Store",
      "code": "eu",
      "url": "https://eu.example.com",
      "currency": "EUR",
      "timezone": "Europe/London",
      "locale": "en_GB",
      "active": true,
      "default": false
    }
  }
}
```

**Response** (201): Created store

**Validation Rules**:
- Name: required, max 255 chars
- Code: required, unique, alphanumeric with underscores/hyphens
- Currency: ISO 4217 code (USD, EUR, GBP, etc.)
- Timezone: Valid IANA timezone
- Locale: IETF language tag (en_US, fr_FR, etc.)

---

### Update Store

```
PUT /api/v2/admin/stores/{storeId}
```

**Permissions**: `stores.update`

**Request Body** (all attributes optional):
```json
{
  "data": {
    "type": "store",
    "attributes": {
      "name": "Updated Store Name",
      "url": "https://updated-url.example.com",
      "active": true
    }
  }
}
```

**Response** (200): Updated store

---

### Delete Store

```
DELETE /api/v2/admin/stores/{storeId}
```

**Permissions**: `stores.delete`

**Response** (204): No content

**Validation Rules**:
- Cannot delete if is default store
- Cannot delete if has orders

---

### Manage Store Products

```
GET /api/v2/admin/stores/{storeId}/products
POST /api/v2/admin/stores/{storeId}/products
DELETE /api/v2/admin/stores/{storeId}/products/{productId}
```

**Permissions**: `stores.view/update`, `products.view/update`

**Request Body** (POST):
```json
{
  "data": {
    "type": "store_product",
    "attributes": {
      "product_id": "prod-001",
      "visible": true,
      "featured": false,
      "position": 1
    }
  }
}
```

---

### Manage Store Stock Locations

```
GET /api/v2/admin/stores/{storeId}/stock-locations
POST /api/v2/admin/stores/{storeId}/stock-locations
DELETE /api/v2/admin/stores/{storeId}/stock-locations/{locationId}
```

**Permissions**: `stores.update`, `inventory.view/update`

**Request Body** (POST):
```json
{
  "data": {
    "type": "store_stock_location",
    "attributes": {
      "stock_location_id": "loc-001",
      "priority": 1,
      "can_fulfill_orders": true
    }
  }
}
```

---

### Get/Update Store Settings

```
GET /api/v2/admin/stores/{storeId}/settings
PUT /api/v2/admin/stores/{storeId}/settings
```

**Permissions**: `stores.view/update`

**Response** (200):
```json
{
  "data": {
    "id": "store-us-settings",
    "type": "store_settings",
    "attributes": {
      "guest_checkout_enabled": true,
      "reviews_enabled": true,
      "wishlists_enabled": true,
      "recommendations_enabled": true,
      "min_order_value_cents": 0,
      "return_window_days": 30,
      "track_inventory": true
    }
  }
}
```

---

## Payment Methods Management

### List Payment Methods

```
GET /api/v2/admin/payment-methods
```

**Permissions**: `payments.view`

**Query Parameters**:
- `filter[active]` - Boolean
- `sort` - `name`, `position`
- `page`, `per_page`
- `include` - `stores`

**Response** (200):
```json
{
  "data": [
    {
      "id": "pm-001",
      "type": "payment_method",
      "attributes": {
        "name": "Credit Card",
        "type": "credit_card",
        "code": "card",
        "description": "Visa, Mastercard, Amex",
        "position": 1,
        "active": true,
        "settings": {
          "gateway": "stripe",
          "merchant_id": "acct_xxx"
        }
      },
      "relationships": {
        "stores": { "data": [{ "id": "store-us", "type": "store" }] }
      }
    }
  ]
}
```

---

### Get Payment Method

```
GET /api/v2/admin/payment-methods/{paymentMethodId}
```

**Permissions**: `payments.view`

**Response** (200): Single payment method

---

### Create Payment Method

```
POST /api/v2/admin/payment-methods
```

**Permissions**: `payments.create`

**Request Body**:
```json
{
  "data": {
    "type": "payment_method",
    "attributes": {
      "name": "PayPal",
      "type": "paypal",
      "code": "paypal",
      "description": "PayPal Express Checkout",
      "position": 2,
      "active": true,
      "settings": {
        "gateway": "paypal",
        "client_id": "xxx",
        "client_secret": "xxx"
      }
    }
  }
}
```

**Response** (201): Created payment method

---

### Update Payment Method

```
PUT /api/v2/admin/payment-methods/{paymentMethodId}
```

**Permissions**: `payments.update`

**Request Body**:
```json
{
  "data": {
    "type": "payment_method",
    "attributes": {
      "name": "Updated Name",
      "active": true,
      "settings": { ... }
    }
  }
}
```

**Response** (200): Updated payment method

---

### Delete Payment Method

```
DELETE /api/v2/admin/payment-methods/{paymentMethodId}
```

**Permissions**: `payments.delete`

**Response** (204): No content

---

### Manage Payment Method Stores

```
GET /api/v2/admin/payment-methods/{paymentMethodId}/stores
POST /api/v2/admin/payment-methods/{paymentMethodId}/stores
PUT /api/v2/admin/payment-methods/{paymentMethodId}/stores/{storeId}
DELETE /api/v2/admin/payment-methods/{paymentMethodId}/stores/{storeId}
```

**Permissions**: `payments.update`, `stores.update`

**Request Body** (POST):
```json
{
  "data": {
    "type": "payment_method_store",
    "attributes": {
      "store_id": "store-us",
      "display_name": "Credit Card",
      "position": 1
    }
  }
}
```

---

### Get/Update Payment Method Settings

```
GET /api/v2/admin/payment-methods/{paymentMethodId}/settings
PUT /api/v2/admin/payment-methods/{paymentMethodId}/settings
```

**Permissions**: `payments.view/update`

---

## Shipping Methods Management

### List Shipping Methods

```
GET /api/v2/admin/shipping-methods
```

**Permissions**: `shipping.view`

**Query Parameters**:
- `filter[active]` - Boolean
- `sort` - `name`, `position`
- `page`, `per_page`
- `include` - `stores`

**Response** (200):
```json
{
  "data": [
    {
      "id": "ship-001",
      "type": "shipping_method",
      "attributes": {
        "name": "Standard Shipping",
        "code": "standard",
        "description": "5-7 business days",
        "position": 1,
        "active": true,
        "calculator_type": "flat_rate",
        "base_cost_cents": 1000
      },
      "relationships": {
        "stores": { "data": [{ "id": "store-us", "type": "store" }] }
      }
    }
  ]
}
```

---

### Get Shipping Method

```
GET /api/v2/admin/shipping-methods/{shippingMethodId}
```

**Permissions**: `shipping.view`

**Response** (200): Single shipping method

---

### Create Shipping Method

```
POST /api/v2/admin/shipping-methods
```

**Permissions**: `shipping.create`

**Request Body**:
```json
{
  "data": {
    "type": "shipping_method",
    "attributes": {
      "name": "Express Shipping",
      "code": "express",
      "description": "1-2 business days",
      "position": 2,
      "active": true,
      "calculator_type": "flat_rate",
      "base_cost_cents": 2500
    }
  }
}
```

**Response** (201): Created shipping method

---

### Update Shipping Method

```
PUT /api/v2/admin/shipping-methods/{shippingMethodId}
```

**Permissions**: `shipping.update`

**Request Body**:
```json
{
  "data": {
    "type": "shipping_method",
    "attributes": {
      "name": "Updated Name",
      "base_cost_cents": 2000,
      "active": true
    }
  }
}
```

**Response** (200): Updated shipping method

---

### Delete Shipping Method

```
DELETE /api/v2/admin/shipping-methods/{shippingMethodId}
```

**Permissions**: `shipping.delete`

**Response** (204): No content

---

### Manage Shipping Method Stores

```
GET /api/v2/admin/shipping-methods/{shippingMethodId}/stores
POST /api/v2/admin/shipping-methods/{shippingMethodId}/stores
PUT /api/v2/admin/shipping-methods/{shippingMethodId}/stores/{storeId}
DELETE /api/v2/admin/shipping-methods/{shippingMethodId}/stores/{storeId}
```

**Permissions**: `shipping.update`, `stores.update`

**Request Body** (POST):
```json
{
  "data": {
    "type": "shipping_method_store",
    "attributes": {
      "store_id": "store-us",
      "display_name": "Express",
      "position": 1
    }
  }
}
```

---

### Calculate Shipping Cost

```
POST /api/v2/admin/shipping-methods/{shippingMethodId}/calculate
```

**Permissions**: `shipping.view`

**Request Body**:
```json
{
  "data": {
    "type": "shipping_calculation",
    "attributes": {
      "line_items": [
        {
          "variant_id": "var-001",
          "quantity": 2,
          "price_cents": 1999
        }
      ],
      "destination_country": "US",
      "destination_state": "NY"
    }
  }
}
```

**Response** (200):
```json
{
  "data": {
    "type": "shipping_cost",
    "attributes": {
      "amount_cents": 1000,
      "currency": "USD",
      "description": "Standard Shipping (5-7 days)"
    }
  }
}
```

---

### Get Shipping Rates

```
GET /api/v2/admin/shipping-methods/{shippingMethodId}/rates
```

**Permissions**: `shipping.view`

**Response** (200): List of rate rules

---



# Authentication & Authorization

## JWT Token Structure

```
Header:
{
  "alg": "RS256",
  "typ": "JWT"
}

Payload:
{
  "sub": "user-id",
  "email": "user@example.com",
  "scope": ["storefront", "orders"],
  "roles": ["customer"],
  "permissions": ["orders.view", "orders.create"],
  "iat": 1234567890,
  "exp": 1234568790
}
```

## Token Validity

- **Access Token**: 15 minutes
- **Refresh Token**: 7 days
- **Signing Algorithm**: RS256 (public key provided in `/.well-known/jwks.json`)

## Refresh Token Flow

```
1. Access token expires
2. Client calls POST /api/v2/storefront/account/refresh-token with refresh_token
3. Server issues new access token
4. Client uses new token for subsequent requests
```

## Role-Based Access Control (RBAC)

**Admin Roles**:
- `super_admin` - All permissions
- `store_manager` - Store-specific operations
- `product_manager` - Product catalog
- `order_manager` - Order management
- `inventory_manager` - Stock operations
- `customer_service` - Customer support

**Permissions** (resource.action):
- `stores.view`, `stores.create`, `stores.update`, `stores.delete`
- `products.view`, `products.create`, `products.update`, `products.delete`
- `inventory.view`, `inventory.adjust`, `inventory.transfer`
- `orders.view`, `orders.update`, `orders.cancel`, `orders.refund`, `orders.fulfill`
- `promotions.view`, `promotions.create`, `promotions.update`
- `reviews.view`, `reviews.moderate`, `reviews.delete`
- `customers.view`, `customers.update`
- `analytics.view`
- `settings.view`, `settings.update`

---

# Response Format (JSON:API)

All responses follow JSON:API specification:

```json
{
  "data": {
    "id": "resource-id",
    "type": "resource-type",
    "attributes": {
      "property": "value"
    },
    "relationships": {
      "related": {
        "data": { "id": "related-id", "type": "related-type" }
      }
    }
  },
  "included": [
    {
      "id": "included-id",
      "type": "included-type",
      "attributes": {}
    }
  ],
  "meta": {
    "count": 25,
    "total_count": 250,
    "total_pages": 10
  },
  "links": {
    "self": "/api/v2/storefront/products",
    "first": "/api/v2/storefront/products?page=1",
    "next": "/api/v2/storefront/products?page=2",
    "last": "/api/v2/storefront/products?page=10"
  }
}
```

---

# Error Handling

## Standard Error Response

```json
{
  "errors": [
    {
      "status": "422",
      "code": "validation_error",
      "title": "Validation Failed",
      "detail": "Name cannot be blank",
      "source": {
        "pointer": "/data/attributes/name"
      }
    }
  ]
}
```

## Common Error Codes

| Code | HTTP | Description |
|------|------|-------------|
| `validation_error` | 422 | Input validation failed |
| `not_found` | 404 | Resource not found |
| `unauthorized` | 401 | Missing/invalid authentication |
| `forbidden` | 403 | Insufficient permissions |
| `conflict` | 409 | Resource state conflict (e.g., duplicate) |
| `too_many_requests` | 429 | Rate limit exceeded |
| `invalid_state_transition` | 422 | Order state machine violation |
| `inventory_error` | 422 | Stock unavailable |
| `payment_error` | 422 | Payment processing failed |

---

# Rate Limiting

## Limits per Client IP

**Storefront**:
- Authenticated: 1000 requests/hour
- Guest: 100 requests/hour
- Search: 60 requests/minute

**Admin**:
- Standard: 5000 requests/hour
- Bulk operations: 100 requests/hour

## Headers

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 998
X-RateLimit-Reset: 1640000000
```

---

# Webhooks

## Supported Events

- `order.created` - New order created
- `order.completed` - Order finalized
- `order.cancelled` - Order canceled
- `payment.captured` - Payment authorized
- `shipment.shipped` - Shipment dispatched
- `product.created` - New product added
- `product.updated` - Product modified
- `product.deleted` - Product removed
- `inventory.low_stock` - Stock below threshold
- `inventory.out_of_stock` - Stock exhausted
- `review.submitted` - Review awaiting approval
- `promotion.activated` - Promotion enabled
- `promotion.expired` - Promotion ended

## Webhook Payload Structure

```json
{
  "id": "event-123",
  "type": "order.created",
  "timestamp": "2024-11-30T12:00:00Z",
  "data": {
    "order_id": "ord-001",
    "order_number": "R123456789",
    "total": "99.99",
    "currency": "USD"
  }
}
```

## Webhook Registration

```
POST /api/v2/admin/webhooks
```

**Permissions**: `settings.update`

**Request Body**:
```json
{
  "data": {
    "type": "webhook",
    "attributes": {
      "url": "https://your-app.com/webhooks/orders",
      "events": ["order.created", "order.completed"],
      "secret": "webhook_signing_secret"
    }
  }
}
```

**Webhook Signing**:
```
X-Spree-Signature: sha256=hex(HMAC-SHA256(body, secret))
```

---

# NEW SECTIONS - 38 MISSING ENDPOINTS

## User Addresses Management (Storefront)

### List User Addresses

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
  "meta": { "pagination": { "total": 3, "count": 3 } }
}
```

**Permissions**: `addresses.view` (authenticated user - own addresses only)

---

### Create User Address

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
- city required, max 100
- zipcode required, max 20
- phone required, E.164 format
- country_id must reference valid country
- state_id optional (required if country has states)

**Permissions**: `addresses.create` (authenticated user)

**Domain Events**:
- `UserAddressCreated(addressId, userId)`

---

### Get User Address

```
GET /api/v2/storefront/addresses/{addressId}
```

**Response** (200): Single address details

**Permissions**: `addresses.view` (authenticated user - own address only)

---

### Update User Address

```
PUT /api/v2/storefront/addresses/{addressId}
```

**Request Body**: Same structure as create

**Response** (200): Updated address

**Permissions**: `addresses.update` (authenticated user - own address only)

**Domain Events**:
- `UserAddressUpdated(addressId, userId)`

---

### Delete User Address

```
DELETE /api/v2/storefront/addresses/{addressId}
```

**Response** (204): No content

**Permissions**: `addresses.delete` (authenticated user - own address only)

**Domain Events**:
- `UserAddressDeleted(addressId, userId)`

---

### Set Default Address

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

**Permissions**: `addresses.update` (authenticated user)

**Domain Events**:
- `UserDefaultShippingAddressSet(addressId, userId)` if type=shipping
- `UserDefaultBillingAddressSet(addressId, userId)` if type=billing

---

## Product Reviews Management

### List Product Reviews (Storefront)

```
GET /api/v2/storefront/products/{productId}/reviews
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
        "product": { "data": { "type": "products", "id": "{productId}" } },
        "user": { "data": { "type": "users", "id": "{userId}" } },
        "variant": { "data": { "type": "variants", "id": "{variantId}" } }
      }
    }
  ],
  "meta": { "pagination": { "total": 145, "count": 20 } }
}
```

**Permissions**: Public (authenticated optional)

---

### Create Product Review

```
POST /api/v2/storefront/products/{productId}/reviews
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

**Response** (201): Created review with status=pending

**Validation**:
- rating required, 1-5 (integer)
- title required, max 200 characters
- body required, max 5000 characters
- variant_id optional, must reference valid variant of product
- User must have purchased product

**Permissions**: `reviews.create` (authenticated users)

**Domain Events**:
- `ReviewCreated(reviewId, productId, userId, rating)`

---

### Get Product Review Details

```
GET /api/v2/storefront/reviews/{reviewId}
```

**Response** (200): Review details

**Permissions**: Public (show only approved to guests)

---

### Update Review

```
PUT /api/v2/storefront/reviews/{reviewId}
```

**Request Body**: Same as create

**Response** (200): Updated review

**Permissions**: `reviews.update` (review author or admin)

---

### Delete Review

```
DELETE /api/v2/storefront/reviews/{reviewId}
```

**Response** (204): No content

**Permissions**: `reviews.delete` (review author or admin)

**Domain Events**:
- `ReviewDeleted(reviewId, productId)`

---

### List Pending Reviews for Moderation (Admin Only)

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

**Response** (200): Reviews awaiting moderation

**Permissions**: `reviews.moderate` (admin only)

**Moderation Actions**:
```
PATCH /api/v2/admin/reviews/{reviewId}/approve           # Approve review
PATCH /api/v2/admin/reviews/{reviewId}/reject            # Reject review
PATCH /api/v2/admin/reviews/{reviewId}/flag              # Flag for review
```

---

## Stock Transfers Management

### Create Stock Transfer

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

**Response** (201): Created transfer with status=pending

**Validation**:
- Both locations must exist and be different
- Variant must exist
- Quantity > 0 and <= on-hand quantity at source
- receive_by must be future date

**Permissions**: `inventory.transfer`

**Domain Events**:
- `StockTransferCreated(transferId, fromLocationId, toLocationId, variantId, quantity)`

---

### Get Stock Transfer

```
GET /api/v2/admin/stock-transfers/{transferId}
```

**Response** (200): Transfer details

**Permissions**: `inventory.view`

---

### List Stock Transfers

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

### Confirm Stock Transfer (Receiving)

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

**Response** (200): Updated transfer with status=completed

**Permissions**: `inventory.transfer`

**Domain Events**:
- `StockTransferConfirmed(transferId, receivedQuantity)`

---

## Promotion Rules Management

### List Promotion Rules

```
GET /api/v2/admin/promotions/{promotionId}/rules
```

**Response** (200): Array of rules with types (cart_total, taxon, user, date_range)

**Permissions**: `promotions.view`

---

### Create Promotion Rule

```
POST /api/v2/admin/promotions/{promotionId}/rules
```

**Request Body** (examples for different rule types):
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

**Response** (201): Created rule

**Validation**:
- rule_type: cart_total | taxon | user | date_range
- For cart_total: value > 0
- For taxon: taxon_ids reference existing taxons
- For user: user_ids reference existing users
- For date_range: starts_at < ends_at

**Permissions**: `promotions.manage`

**Domain Events**:
- `PromotionRuleAdded(promotionId, ruleId, ruleType)`

---

### Update Promotion Rule

```
PUT /api/v2/admin/promotions/{promotionId}/rules/{ruleId}
```

**Request Body**: Same as create

**Response** (200): Updated rule

**Permissions**: `promotions.manage`

---

### Delete Promotion Rule

```
DELETE /api/v2/admin/promotions/{promotionId}/rules/{ruleId}
```

**Response** (204): No content

**Permissions**: `promotions.manage`

**Domain Events**:
- `PromotionRuleRemoved(promotionId, ruleId)`

---

## Promotion Actions Management

### List Promotion Actions

```
GET /api/v2/admin/promotions/{promotionId}/actions
```

**Response** (200): Actions array with types (percentage_discount, fixed_amount, free_shipping, buy_x_get_y)

**Permissions**: `promotions.view`

---

### Create Promotion Action

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

**Response** (201): Created action

**Validation**:
- action_type: percentage_discount | fixed_amount | free_shipping | buy_x_get_y
- For percentage: 0-100
- For fixed_amount: amount > 0
- For buy_x_get_y: quantities > 0, variant exists

**Permissions**: `promotions.manage`

**Domain Events**:
- `PromotionActionAdded(promotionId, actionId, actionType)`

---

### Update Promotion Action

```
PUT /api/v2/admin/promotions/{promotionId}/actions/{actionId}
```

**Response** (200): Updated action

**Permissions**: `promotions.manage`

---

### Delete Promotion Action

```
DELETE /api/v2/admin/promotions/{promotionId}/actions/{actionId}
```

**Response** (204): No content

**Permissions**: `promotions.manage`

---

## Authentication & Token Management

### Refresh JWT Token

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

### Logout (Revoke Refresh Token)

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

### Revoke Refresh Token (Admin)

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

## Audit Logs Management

### List Audit Logs

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

**Response** (200): Audit log entries with details

**Permissions**: `audit.view` (admin only)

---

### Get Audit Log Entry

```
GET /api/v2/admin/audit-logs/{logId}
```

**Response** (200): Detailed audit entry

**Permissions**: `audit.view` (admin only)

---

### Export Audit Logs

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

**Response** (200): CSV/JSON/XLSX file download

**Permissions**: `audit.export` (admin only)

---

## Access Permissions Management

### List All Available Permissions

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

### List Role Permissions

```
GET /api/v2/admin/roles/{roleId}/permissions
```

**Response** (200): Permissions assigned to role

**Permissions**: `roles.view` (admin only)

---

### Assign Permission to Role

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

### Revoke Permission from Role

```
DELETE /api/v2/admin/roles/{roleId}/permissions/{permissionCode}
```

**Response** (204): Permission revoked

**Permissions**: `roles.manage` (admin only)

**Domain Events**:
- `PermissionRevokedFromRole(roleId, permissionCode)`

---

## Location Data - Countries & States

### List Countries

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

### Get Country Details

```
GET /api/v2/shared/countries/{countryId}
```

**Query Parameters**:
```
include=states
```

**Response** (200): Country with optional included states

**Permissions**: Public

---

### List States by Country

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

**Response** (200): States within country with pagination

**Permissions**: Public

---

### Get State Details

```
GET /api/v2/shared/countries/{countryId}/states/{stateId}
```

**Response** (200): State details with country relationship

**Permissions**: Public

---

## API Versioning

- Current version: `v2`
- Backward compatibility: 2 major versions
- Deprecation notice: 6 months before removal
- Change log: `/api/v2/changelog`

---

## Additional Resources

- **API Documentation**: https://api.fashionstore.local/docs
- **OpenAPI/Swagger**: https://api.fashionstore.local/openapi.json
- **Status Page**: https://status.fashionstore.com
- **Support**: https://support.fashionstore.com

---

**Last Updated**: November 30, 2025  
**Status**: Complete - All 188+ Endpoints Documented  
**Maintainers**: Platform Team
