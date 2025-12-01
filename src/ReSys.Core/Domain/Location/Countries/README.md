# Location.Countries Bounded Context

This document describes the `Location.Countries` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain is dedicated to managing the definition and lifecycle of countries. It provides a foundational dataset of sovereign states, identified by their names and ISO codes, and serves as a crucial reference for other domains requiring geographical context for addresses, stock locations, and store configurations.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Location.Countries` bounded context.

-   **Country**: A sovereign state or nation, representing a primary geographical and political entity. Represented by the `Country` aggregate.
-   **Name**: The full, human-readable name of the country.
-   **ISO**: The two-letter ISO 3166-1 alpha-2 code for the country (e.g., "US", "CA", "GB").
-   **ISO3**: The three-letter ISO 3166-1 alpha-3 code for the country (e.g., "USA", "CAN", "GBR").
-   **State**: An administrative division within a country (e.g., a state, province, region). (Referenced from `Location.States` Bounded Context).
-   **User Address**: A physical address associated with a user. (Referenced from `Identity.UserAddresses` Bounded Context).
-   **Stock Location**: A physical location for storing inventory. (Referenced from `Inventories.Locations` Bounded Context).
-   **Store**: A sales channel or storefront. (Referenced from `Stores` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Country`**: This is the Aggregate Root. It represents a country and is responsible for managing its own properties, ensuring data integrity, and handling its lifecycle. It acts as the container for its associated `State` entities.
    -   **Entities**: `State` (owned by `Country`). Represents an administrative division within the country.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Iso`, and `Iso3` are intrinsic attributes of the `Country` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   None, as `State` is owned by `Country`.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All business logic related to country management is encapsulated within the `Country` aggregate.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Location.Countries` bounded context.

-   `Name`, `Iso`, and `Iso3` are required fields for a `Country`.
-   `Iso` must be exactly two characters and `Iso3` exactly three characters.
-   `Iso` and `Iso3` values are automatically converted to uppercase for standardization.
-   A `Country` cannot be deleted if it has associated `State`s, `UserAddress`es, `StockLocation`s, or `Store`s, ensuring data integrity and preventing orphaned records.
-   `Country` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`Country` to `State`**: One-to-many composition. `Country` is the aggregate root for its `State`s (from `Location.States`).
-   **`Country` to `UserAddress`**: One-to-many relationship. `UserAddress`es (from `Identity.UserAddresses`) reference a `Country`.
-   **`Country` to `StockLocation`**: One-to-many relationship. `StockLocation`s (from `Inventories.Locations`) reference a `Country`.
-   **`Country` to `Store`**: One-to-many relationship. `Store`s (from `Stores`) reference a `Country` for their physical address.
-   **Shared Kernel**: `Country` inherits from `Aggregate` (from `SharedKernel.Domain.Entities`), providing common base properties. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Country**: Add a new country to the system, providing its name, ISO 2-letter code, and ISO 3-letter code.
-   **Update Country Details**: Modify the name or ISO codes of an existing country.
-   **Delete Country**: Remove a country from the system, subject to checks for existing dependencies (states, addresses, stock locations, stores).
-   **Publish Domain Events**: Emit events for country creation, update, and deletion, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   This domain serves as a reference data bounded context, providing stable geographical information to other parts of the system.
-   The `Country` aggregate ensures that `State`s cannot exist without a parent country.
-   The strict validation of ISO codes and dependency checks on deletion are crucial for maintaining data quality and integrity.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
