# Authorization Permissions

This directory serves as the central repository for defining all granular access permissions within the application. It employs a hierarchical structure of static classes to organize permissions by feature area (e.g., Admin, Store, Testing) and sub-feature, making them discoverable, manageable, and consistently named. Each permission is represented by an `AccessPermission` object, which encapsulates its name, display name, and description.

---

## Structure and Organization

Permissions are organized into a nested hierarchy of static classes under the `Feature` namespace, reflecting the application's modular design. This structure allows for logical grouping and easy retrieval of related permissions.

-   **`Feature.cs`**: The top-level static class that aggregates all permissions defined across the application. It provides a single entry point (`AllPermissions`) to access every permission.
-   **`Feature.Admin.cs`**: Groups all permissions related to the administrative panel.
    -   **`Feature.Admin.AuditLog.cs`**: Defines permissions for viewing, listing, and exporting audit logs.
    -   **`Feature.Admin.Catalog.cs`**: Aggregates all permissions for the product catalog management within the admin panel.
        -   `Feature.Admin.Catalog.OptionType.cs`: Permissions for managing product option types.
        -   `Feature.Admin.Catalog.OptionValue.cs`: Permissions for managing product option values.
        -   `Feature.Admin.Catalog.Property.cs`: Permissions for managing product properties.
        -   `Feature.Admin.Catalog.Taxon.cs`: Permissions for managing product taxons (categories).
        -   `Feature.Admin.Catalog.Taxonomy.cs`: Permissions for managing product taxonomies.
    -   **`Feature.Admin.Permissions.cs`**: Defines permissions for managing access control permissions themselves (e.g., assigning permissions to roles).
    -   **`Feature.Admin.Role.cs`**: Defines permissions for managing user roles (CRUD operations, assignment).
    -   **`Feature.Admin.User.cs`**: Defines permissions for managing user accounts (CRUD operations).
-   **`Feature.Store.cs`**: Groups all permissions related to the storefront application.
    -   `Feature.Store.Product.cs`: Permissions for viewing, searching, and browsing products.
    -   `Feature.Store.Order.cs`: Permissions for creating, viewing, updating, canceling, and tracking orders.
    -   `Feature.Store.Cart.cs`: Permissions for managing the shopping cart (add, remove, update, view, clear).
    -   `Feature.Store.Wishlist.cs`: Permissions for managing wishlists (add, remove, view, share).
    -   `Feature.Store.Review.cs` : Permissions for managing product reviews (create, update, delete, view).
    -   `Feature.Store.Profile.cs` : Permissions for managing user profiles (view, update, delete).
-   **`Feature.Testing.cs`**: Groups permissions specifically for testing-related features.
    -   `Feature.Testing.Todo.cs`: Permissions for managing todo items and lists, likely used for development or testing purposes.

---

## `AccessPermission` Entity

Each permission is an instance of `Domain.Identity.Permissions.AccessPermission`, which is created using a static factory method.

-   **`AccessPermission.Create(name, displayName, description)`**:
    -   `name`: A unique, machine-readable string identifier for the permission (e.g., "admin.user.create"). This follows a `{area}.{resource}.{action}` convention.
    -   `displayName`: A human-readable name for the permission, suitable for display in a UI (e.g., "Create User").
    -   `description`: A brief explanation of what the permission allows.

---

## Purpose

The permissions defined in this directory serve several key purposes:

-   **Granular Access Control**: Enable fine-grained control over what actions users can perform within the application.
-   **Centralized Definition**: Provide a single, organized location for all permission definitions, making them easy to find, understand, and manage.
-   **Consistency**: Ensure consistent naming conventions and descriptions for permissions across the entire application.
-   **Type Safety**: By using static properties to access `AccessPermission` objects, the codebase benefits from compile-time checking and avoids magic strings.
-   **UI Integration**: The `displayName` and `description` properties facilitate the creation of user-friendly interfaces for role and permission management.
-   **Extensibility**: The partial class structure allows for easy addition of new feature permissions without modifying existing files.
