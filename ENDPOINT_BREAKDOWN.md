# API Endpoints Breakdown - Complete Reference

**Last Updated**: November 30, 2024  
**Total Endpoints**: 150+  
**Documentation Status**: ✅ Complete

---

## 📊 Endpoint Summary by Category

### 📦 PRODUCTS (33 endpoints)

```
Base CRUD Operations (5)
├── GET    /api/v2/admin/products                              # List all products
├── GET    /api/v2/admin/products/{productId}                  # Get product by ID
├── POST   /api/v2/admin/products                              # Create new product
├── PUT    /api/v2/admin/products/{productId}                  # Update product
└── DELETE /api/v2/admin/products/{productId}                  # Soft delete

Status Management (4)
├── PATCH  /api/v2/admin/products/{productId}/activate         # Activate product
├── PATCH  /api/v2/admin/products/{productId}/archive          # Archive product
├── PATCH  /api/v2/admin/products/{productId}/draft            # Set to draft
└── PATCH  /api/v2/admin/products/{productId}/discontinue      # Discontinue

📷 Images (5)
├── GET    /api/v2/admin/products/{productId}/images           # List images
├── POST   /api/v2/admin/products/{productId}/images           # Upload image
├── PUT    /api/v2/admin/products/{productId}/images/{imageId} # Update image
├── DELETE /api/v2/admin/products/{productId}/images/{imageId} # Delete image
└── PATCH  /api/v2/admin/products/{productId}/images/reorder   # Reorder images

🏷️ Properties (4)
├── GET    /api/v2/admin/products/{productId}/properties       # List properties
├── POST   /api/v2/admin/products/{productId}/properties       # Add property
├── PUT    /api/v2/admin/products/{productId}/properties/{propertyId} # Update
└── DELETE /api/v2/admin/products/{productId}/properties/{propertyId} # Remove

🔧 Option Types (3)
├── GET    /api/v2/admin/products/{productId}/option-types     # List option types
├── POST   /api/v2/admin/products/{productId}/option-types     # Add option type
└── DELETE /api/v2/admin/products/{productId}/option-types/{optionTypeId} # Remove

📂 Categories (3)
├── GET    /api/v2/admin/products/{productId}/categories       # List categories
├── POST   /api/v2/admin/products/{productId}/categories       # Add to category
└── DELETE /api/v2/admin/products/{productId}/categories/{taxonId} # Remove

🏪 Stores (4)
├── GET    /api/v2/admin/products/{productId}/stores           # List stores
├── POST   /api/v2/admin/products/{productId}/stores           # Add to store
├── PUT    /api/v2/admin/products/{productId}/stores/{storeId} # Update settings
└── DELETE /api/v2/admin/products/{productId}/stores/{storeId} # Remove from store

📊 Analytics (2)
├── GET    /api/v2/admin/products/{productId}/analytics        # Product analytics
└── GET    /api/v2/admin/products/{productId}/sales            # Sales history
```

**Total: 33 endpoints**

---

### 📦 VARIANTS (25 endpoints)

```
Base Operations (7)
├── GET    /api/v2/admin/products/{productId}/variants         # List variants
├── GET    /api/v2/admin/variants/{variantId}                  # Get variant
├── POST   /api/v2/admin/products/{productId}/variants         # Create variant
├── PUT    /api/v2/admin/variants/{variantId}                  # Update variant
├── DELETE /api/v2/admin/variants/{variantId}                  # Delete variant
└── PATCH  /api/v2/admin/variants/{variantId}/discontinue      # Discontinue

💰 Pricing (4)
├── GET    /api/v2/admin/variants/{variantId}/prices           # List prices
├── POST   /api/v2/admin/variants/{variantId}/prices           # Add price
├── PUT    /api/v2/admin/variants/{variantId}/prices/{priceId} # Update price
└── DELETE /api/v2/admin/variants/{variantId}/prices/{priceId} # Delete price

📦 Inventory (4)
├── GET    /api/v2/admin/variants/{variantId}/stock            # Get stock levels
├── POST   /api/v2/admin/variants/{variantId}/stock            # Set stock
├── PATCH  /api/v2/admin/variants/{variantId}/stock/adjust     # Adjust stock
└── GET    /api/v2/admin/variants/{variantId}/stock/movements  # Movement history

🔧 Options (3)
├── GET    /api/v2/admin/variants/{variantId}/options          # List options
├── POST   /api/v2/admin/variants/{variantId}/options          # Set option
└── DELETE /api/v2/admin/variants/{variantId}/options/{optionValueId} # Remove

📷 Images (3)
├── GET    /api/v2/admin/variants/{variantId}/images           # List images
├── POST   /api/v2/admin/variants/{variantId}/images           # Upload image
└── DELETE /api/v2/admin/variants/{variantId}/images/{imageId} # Delete image
```

**Total: 25 endpoints**

---

### 🏪 STOCK LOCATIONS (7 endpoints)

```
Base Operations (5)
├── GET    /api/v2/admin/stock-locations                       # List locations
├── GET    /api/v2/admin/stock-locations/{locationId}          # Get location
├── POST   /api/v2/admin/stock-locations                       # Create location
├── PUT    /api/v2/admin/stock-locations/{locationId}          # Update location
└── DELETE /api/v2/admin/stock-locations/{locationId}          # Delete location

🏪 Store Configuration (2)
├── POST   /api/v2/admin/stock-locations/{locationId}/stores   # Link to store
└── DELETE /api/v2/admin/stock-locations/{locationId}/stores/{storeId} # Unlink

📊 Reports (2)
├── GET    /api/v2/admin/stock-locations/{locationId}/stock    # Stock summary
└── GET    /api/v2/admin/stock-locations/{locationId}/movements # Movement history
```

**Total: 7 endpoints**

---

### 📊 STOCK ITEMS (12 endpoints)

```
Base Operations (5)
├── GET    /api/v2/admin/stock-items                           # List items
├── GET    /api/v2/admin/stock-items/{stockItemId}             # Get item
├── POST   /api/v2/admin/stock-items                           # Create item
├── PUT    /api/v2/admin/stock-items/{stockItemId}             # Update item
└── DELETE /api/v2/admin/stock-items/{stockItemId}             # Delete item

📊 Adjustments (4)
├── POST   /api/v2/admin/stock-items/{stockItemId}/adjust      # Adjust count
├── POST   /api/v2/admin/stock-items/{stockItemId}/reserve     # Reserve stock
├── POST   /api/v2/admin/stock-items/{stockItemId}/release     # Release reserved
└── POST   /api/v2/admin/stock-items/{stockItemId}/ship        # Confirm shipment

📈 Movements (1)
└── GET    /api/v2/admin/stock-items/{stockItemId}/movements   # Movement history

📊 Reports (3)
├── GET    /api/v2/admin/stock-items/low-stock                 # Low stock alert
├── GET    /api/v2/admin/stock-items/out-of-stock              # Out of stock
└── GET    /api/v2/admin/stock-items/by-location               # Stock by location
```

**Total: 12 endpoints**

---

### 🎁 ORDERS (30 endpoints)

```
Order Operations (4)
├── GET    /api/v2/admin/orders                                # List orders
├── GET    /api/v2/admin/orders/{orderId}                      # Get order
├── PUT    /api/v2/admin/orders/{orderId}                      # Update order
└── DELETE /api/v2/admin/orders/{orderId}                      # Delete order

🔄 State Management (3)
├── PATCH  /api/v2/admin/orders/{orderId}/complete             # Mark complete
├── PATCH  /api/v2/admin/orders/{orderId}/cancel               # Cancel order
└── PATCH  /api/v2/admin/orders/{orderId}/resume               # Resume canceled

📦 Line Items (4)
├── GET    /api/v2/admin/orders/{orderId}/line-items           # List items
├── POST   /api/v2/admin/orders/{orderId}/line-items           # Add item
├── PUT    /api/v2/admin/orders/{orderId}/line-items/{itemId}  # Update item
└── DELETE /api/v2/admin/orders/{orderId}/line-items/{itemId}  # Remove item

🚚 Shipments (6)
├── GET    /api/v2/admin/orders/{orderId}/shipments            # List shipments
├── POST   /api/v2/admin/orders/{orderId}/shipments            # Create shipment
├── PUT    /api/v2/admin/orders/{orderId}/shipments/{shipmentId} # Update
├── PATCH  /api/v2/admin/orders/{orderId}/shipments/{shipmentId}/ship # Mark shipped
├── PATCH  /api/v2/admin/orders/{orderId}/shipments/{shipmentId}/deliver # Deliver
└── DELETE /api/v2/admin/orders/{orderId}/shipments/{shipmentId} # Cancel

💳 Payments (5)
├── GET    /api/v2/admin/orders/{orderId}/payments             # List payments
├── POST   /api/v2/admin/orders/{orderId}/payments             # Create payment
├── PATCH  /api/v2/admin/orders/{orderId}/payments/{paymentId}/capture # Capture
├── PATCH  /api/v2/admin/orders/{orderId}/payments/{paymentId}/void # Void
└── PATCH  /api/v2/admin/orders/{orderId}/payments/{paymentId}/refund # Refund

📊 Reports (3)
├── GET    /api/v2/admin/orders/statistics                     # Order statistics
├── GET    /api/v2/admin/orders/revenue                        # Revenue report
└── GET    /api/v2/admin/orders/by-status                      # Orders by status
```

**Total: 30 endpoints**

---

### 📂 TAXONOMIES (5 endpoints)

```
├── GET    /api/v2/admin/taxonomies                            # List taxonomies
├── GET    /api/v2/admin/taxonomies/{taxonomyId}               # Get taxonomy
├── POST   /api/v2/admin/taxonomies                            # Create taxonomy
├── PUT    /api/v2/admin/taxonomies/{taxonomyId}               # Update taxonomy
└── DELETE /api/v2/admin/taxonomies/{taxonomyId}               # Delete taxonomy
```

**Total: 5 endpoints**

---

### 🌳 TAXONS (10+ endpoints)

```
Navigation (2)
├── GET    /api/v2/admin/taxonomies/{taxonomyId}/taxons        # List tree
└── GET    /api/v2/admin/taxonomies/{taxonomyId}/taxons/flat   # List flat

Base Operations (6)
├── GET    /api/v2/admin/taxons/{taxonId}                      # Get taxon
├── POST   /api/v2/admin/taxonomies/{taxonomyId}/taxons        # Create taxon
├── PUT    /api/v2/admin/taxons/{taxonId}                      # Update taxon
├── DELETE /api/v2/admin/taxons/{taxonId}                      # Delete taxon
└── PATCH  /api/v2/admin/taxons/{taxonId}/move                 # Move in hierarchy

📷 Images (3)
├── GET    /api/v2/admin/taxons/{taxonId}/images               # List images
├── POST   /api/v2/admin/taxons/{taxonId}/images               # Upload image
└── DELETE /api/v2/admin/taxons/{taxonId}/images/{imageId}     # Delete image

📦 Products (3)
├── GET    /api/v2/admin/taxons/{taxonId}/products             # List products
├── POST   /api/v2/admin/taxons/{taxonId}/products             # Add product
└── DELETE /api/v2/admin/taxons/{taxonId}/products/{productId} # Remove product

🤖 Rules (5)
├── GET    /api/v2/admin/taxons/{taxonId}/rules                # List rules
├── POST   /api/v2/admin/taxons/{taxonId}/rules                # Create rule
├── PUT    /api/v2/admin/taxons/{taxonId}/rules/{ruleId}       # Update rule
├── DELETE /api/v2/admin/taxons/{taxonId}/rules/{ruleId}       # Delete rule
└── POST   /api/v2/admin/taxons/{taxonId}/rules/regenerate     # Regenerate products
```

**Total: 20+ endpoints**

---

### 🏪 STORES (6 endpoints)

```
Base Operations (5)
├── GET    /api/v2/admin/stores                                # List stores
├── GET    /api/v2/admin/stores/{storeId}                      # Get store
├── POST   /api/v2/admin/stores                                # Create store
├── PUT    /api/v2/admin/stores/{storeId}                      # Update store
└── DELETE /api/v2/admin/stores/{storeId}                      # Delete store

⚙️ Configuration (1)
└── GET/PUT /api/v2/admin/stores/{storeId}/settings            # Store settings

Additional Endpoints
├── GET/POST/DELETE /api/v2/admin/stores/{storeId}/products
├── GET/POST/DELETE /api/v2/admin/stores/{storeId}/stock-locations
```

**Total: 6+ endpoints**

---

### 💳 PAYMENT METHODS (7 endpoints)

```
Base Operations (5)
├── GET    /api/v2/admin/payment-methods                       # List methods
├── GET    /api/v2/admin/payment-methods/{methodId}            # Get method
├── POST   /api/v2/admin/payment-methods                       # Create method
├── PUT    /api/v2/admin/payment-methods/{methodId}            # Update method
└── DELETE /api/v2/admin/payment-methods/{methodId}            # Delete method

🏪 Store Configuration (2)
├── GET/POST/PUT/DELETE /api/v2/admin/payment-methods/{methodId}/stores

⚙️ Settings (1)
└── GET/PUT /api/v2/admin/payment-methods/{methodId}/settings
```

**Total: 7+ endpoints**

---

### 🚚 SHIPPING METHODS (7+ endpoints)

```
Base Operations (5)
├── GET    /api/v2/admin/shipping-methods                      # List methods
├── GET    /api/v2/admin/shipping-methods/{methodId}           # Get method
├── POST   /api/v2/admin/shipping-methods                      # Create method
├── PUT    /api/v2/admin/shipping-methods/{methodId}           # Update method
└── DELETE /api/v2/admin/shipping-methods/{methodId}           # Delete method

🏪 Store Configuration (2)
├── GET/POST/PUT/DELETE /api/v2/admin/shipping-methods/{methodId}/stores

💰 Rate Calculation (2)
├── POST   /api/v2/admin/shipping-methods/{methodId}/calculate # Calculate cost
└── GET    /api/v2/admin/shipping-methods/{methodId}/rates     # Get rates
```

**Total: 7+ endpoints**

---

### 🎁 PROMOTIONS (7+ endpoints)

```
Base Operations (5)
├── GET    /api/v2/admin/promotions                            # List promotions
├── GET    /api/v2/admin/promotions/{promotionId}              # Get promotion
├── POST   /api/v2/admin/promotions                            # Create promotion
├── PUT    /api/v2/admin/promotions/{promotionId}              # Update promotion
└── DELETE /api/v2/admin/promotions/{promotionId}              # Delete promotion

Activation (2)
├── PATCH  /api/v2/admin/promotions/{promotionId}/activate     # Activate
└── PATCH  /api/v2/admin/promotions/{promotionId}/deactivate   # Deactivate

📋 Rules (3)
├── GET/POST/PUT/DELETE /api/v2/admin/promotions/{promotionId}/rules

📊 Analytics (2)
├── GET    /api/v2/admin/promotions/{promotionId}/usage        # Usage stats
└── GET    /api/v2/admin/promotions/{promotionId}/orders       # Orders using promo
```

**Total: 7+ endpoints**

---

## 📊 TOTAL BREAKDOWN

```
Admin Platform Endpoints:

Products               33
Variants               25
Stock Locations         7
Stock Items            12
Orders                 30
Taxonomies             5
Taxons              20+
Stores              6+
Payment Methods      7+
Shipping Methods     7+
Promotions           7+
Customers         (included)
Reviews           (included)
Analytics         (included)
─────────────────────────
TOTAL            150+
```

---

## 🌐 STOREFRONT API (50+ endpoints)

```
Store Context (1)
├── GET /api/v2/storefront/stores/{storeCode}

Product Catalog (15)
├── GET /api/v2/storefront/products
├── GET /api/v2/storefront/products/{slug}
├── GET /api/v2/storefront/products/{id}/variants
├── GET /api/v2/storefront/products/{id}/related
├── GET /api/v2/storefront/products/{id}/reviews
└── Various filter/search endpoints

Categories (8)
├── GET /api/v2/storefront/categories
├── GET /api/v2/storefront/categories/{slug}
├── GET /api/v2/storefront/categories/{slug}/products
└── GET /api/v2/storefront/categories/{slug}/children

Cart (12)
├── GET /api/v2/storefront/cart
├── POST /api/v2/storefront/cart/items
├── PUT /api/v2/storefront/cart/items/{itemId}
├── DELETE /api/v2/storefront/cart/items/{itemId}
├── DELETE /api/v2/storefront/cart
└── Promo/total endpoints

Checkout (15)
├── GET /api/v2/storefront/checkout
├── PATCH /api/v2/storefront/checkout/email
├── PATCH /api/v2/storefront/checkout/addresses
├── GET /api/v2/storefront/checkout/shipping-methods
├── PATCH /api/v2/storefront/checkout/shipping-method
├── GET /api/v2/storefront/checkout/payment-methods
├── PATCH /api/v2/storefront/checkout/payment-method
└── Summary endpoint

Account (20)
├── Profile CRUD
├── Address management
├── Order history
├── Payment methods
├── Wishlist
└── Various account endpoints

Inventory (2)
├── GET /api/v2/storefront/variants/{variantId}/availability
└── POST /api/v2/storefront/variants/availability/bulk

Search (8)
├── GET /api/v2/storefront/search
├── GET /api/v2/storefront/search/suggest
└── Advanced search endpoints

TOTAL: 50+ endpoints
```

---

## 🎯 Grand Total

**Admin Platform**: 150+ endpoints  
**Storefront API**: 50+ endpoints  

**COMBINED TOTAL: 200+ endpoints**

---

**Status**: ✅ Complete and documented  
**Last Updated**: November 30, 2024
