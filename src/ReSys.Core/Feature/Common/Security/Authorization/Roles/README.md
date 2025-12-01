# Authorization Roles

This directory centralizes the definition of default roles used within the application's authorization system. These roles provide a structured way to categorize users and assign them specific levels of access and responsibilities, both for internal system management and for storefront interactions.

---

## Modules

### 1. `DefaultRole.cs`

This file defines a static class containing constant strings for all predefined roles in the application.

-   **Purpose**: To provide a single, consistent source of truth for role names, preventing magic strings and ensuring type safety when assigning or checking roles throughout the codebase.
-   **Key Features**:
    -   **System Roles**: Defines roles for internal administrative and operational functions, such as:
        -   `Admin`: Full administrative access.
        -   `StoreManager`: Manages store operations.
        -   `Merchandiser`: Manages product catalog and merchandising.
        -   `InventoryManager`: Manages stock and inventory.
        -   `OrderManager`: Manages customer orders.
        -   `CustomerService`: Handles customer inquiries and support.
        -   `MarketingManager`: Manages marketing campaigns.
        -   `ContentManager`: Manages website content.
        -   `WarehouseStaff`: Manages warehouse operations.
        -   `SalesAssociate`: Handles sales-related tasks.
    -   **Storefront Roles**: Defines roles for public-facing interactions:
        -   `Anonymous`: Represents unauthenticated users.
        -   `Customer`: Represents authenticated customers.
    -   **Role Collections**: Provides `AllRoles`, `SystemRoles`, and `StorefrontRoles` arrays for easy enumeration and management of role groups.

---

## Purpose

The components within this directory are designed to:

-   **Standardize Role Definitions**: Ensure consistent naming and usage of roles across the application.
-   **Facilitate RBAC Implementation**: Serve as the foundation for implementing Role-Based Access Control, allowing for clear assignment of permissions based on roles.
-   **Improve Code Readability**: Make authorization logic more understandable by using descriptive role constants.
-   **Enhance Maintainability**: Centralize role definitions, simplifying updates and additions to the application's role structure.
-   **Support User Management**: Provide the necessary building blocks for user and role management features within the administration panel.
