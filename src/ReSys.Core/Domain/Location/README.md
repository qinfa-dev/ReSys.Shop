# Location Bounded Context

This document describes the `Location` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain provides foundational geographical data, specifically managing countries and their administrative divisions (states/provinces). It serves as a crucial reference for other domains that require location-based information, such as user addresses, stock locations, shipping configurations, and tax calculations, ensuring consistent and validated geographical data across the application.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Location` bounded context.

-   **Country**: A sovereign state or nation, identified by its unique name, ISO 2-letter code (`Iso`), and ISO 3-letter code (`Iso3`). Represented by the `Country` aggregate.
-   **State**: A primary administrative division within a `Country` (e.g., a state, province, region, or territory). Represented by the `State` entity.
-   **ISO (International Organization for Standardization)**: Standardized two-letter and three-letter codes used globally to uniquely identify countries (e.g., "US" and "USA" for the United States).
-   **Abbr (Abbreviation)**: A short form or code for a `State`'s name (e.g., "CA" for California).
-   **Address**: A structured set of information describing a physical location, typically including street lines, city, postal code, country, and state. Represented by the `IAddress` interface and its associated `AddressConstraints`.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Country`**: This is the Aggregate Root. It represents a country and is responsible for managing its associated `State`s. It ensures the integrity of country data and its relationships with other entities that depend on it.
    -   **Entities**: `State` (owned by `Country`). Represents an administrative division within the country.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Iso`, and `Iso3` are intrinsic attributes of the `Country` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `UserAddress` (from `Core.Domain.Identity.UserAddresses`): Referenced by `Country` and `State` (via foreign keys), but managed by its own aggregate.
-   `StockLocation` (from `Core.Domain.Inventories.Locations`): Referenced by `Country` (via foreign key), but managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`IAddress`**: An interface that defines a common contract for address-related properties. This promotes consistency in how address information is structured and used across different domains (e.g., `Identity.UserAddresses`, `Inventories.Locations`).
-   **`AddressConstraints`**: A static class providing constants for maximum lengths of various address fields (e.g., `Address1MaxLength`, `CityMaxLength`), ensuring data consistency and preventing overflow.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All business logic related to `Country` and `State` management is encapsulated within their respective aggregate and entity.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Location` bounded context.

-   `Country` names, ISO 2-letter codes (`Iso`), and ISO 3-letter codes (`Iso3`) have maximum length constraints to ensure data integrity.
-   `Country` ISO codes (`Iso`, `Iso3`) are automatically converted to uppercase upon creation and update for standardization.
-   A `Country` cannot be deleted if it has associated `State`s, `UserAddress`es, or `StockLocation`s. This prevents data inconsistencies and orphaned records. (Enforced by `Country.Delete()`)
-   A `State` cannot be deleted if it has associated `UserAddress`es. (Enforced by `State.Delete()`)
-   `State` names and abbreviations (`Abbr`) are trimmed, and abbreviations are converted to uppercase for consistency.
-   Address fields (e.g., `Address1`, `City`, `Zipcode`) adhere to predefined maximum length constraints as defined in `AddressConstraints`.

---

## 🤝 Relationships & Dependencies

-   **`Country` to `State`**: One-to-many composition. `Country` is the aggregate root for its `State`s, meaning `State`s cannot exist independently of a `Country`.
-   **`Country` to `UserAddress`**: A one-to-many relationship, where `UserAddress`es (from `Identity.UserAddresses`) reference a `Country` via `CountryId`.
-   **`Country` to `StockLocation`**: A one-to-many relationship, where `StockLocation`s (from `Inventories.Locations`) reference a `Country` via `CountryId`.
-   **`State` to `UserAddress`**: A one-to-many relationship, where `UserAddress`es reference a `State` via `StateId`.
-   **Shared Kernel**: `Country` inherits from `Aggregate` and `State` inherits from `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`). Both leverage `SharedKernel.Validations` for common input validation. The `IAddress` interface is also defined here, promoting a consistent address structure across the application.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Country**: Add a new country to the system, providing its name, ISO 2-letter code, and ISO 3-letter code.
-   **Update Country Details**: Modify the name or ISO codes of an existing country.
-   **Delete Country**: Remove a country from the system, provided there are no dependent `State`s, `UserAddress`es, or `StockLocation`s.
-   **Create State**: Add a new administrative division (state/province) within a specific `Country`, providing its name and abbreviation.
-   **Update State Details**: Modify the name or abbreviation of an existing state.
-   **Delete State**: Remove a state from the system, provided there are no dependent `UserAddress`es.
-   **Standardize Address Information**: Provide a common interface (`IAddress`) and constraints (`AddressConstraints`) for structuring address data, facilitating interoperability across domains.
-   **Publish Domain Events**: Emit domain events for `Country` creation, update, and deletion, enabling a decoupled architecture where other parts of the system can react asynchronously.

---

## 📝 Considerations / Notes

-   This domain is primarily a reference data domain, providing static or semi-static geographical information.
-   The `Country` aggregate is designed to protect its `State`s and prevent deletion if other entities depend on it, ensuring data integrity.
-   The `IAddress` interface is a good example of how shared kernel concepts can be used to define common structures without tightly coupling domains.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit and easier to manage.
-   While `State` is an entity owned by `Country`, it also tracks its own auditing information (`AuditableEntity`).