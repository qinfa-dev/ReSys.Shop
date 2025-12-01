# Identity.UserAddresses Bounded Context

This document describes the `Identity.UserAddresses` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the addresses associated with a user, providing a robust mechanism for storing, retrieving, and managing multiple shipping and billing addresses. It ensures the integrity and consistency of address data, facilitates user preferences such as default addresses and quick checkout options, and supports geographical data integration through country and state associations.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.UserAddresses` bounded context.

-   **User Address**: A physical address (e.g., street, city, postal code) explicitly associated with a specific user, used for purposes such as shipping, billing, or contact. Represented by the `UserAddress` aggregate.
-   **Address Type**: An enumeration (`AddressType`) indicating the primary purpose or classification of the address (e.g., `Shipping`, `Billing`).
-   **Label**: A user-defined, human-readable identifier or nickname for an address (e.g., "Home", "Work", "My Office"), allowing users to easily distinguish between their saved addresses.
-   **Quick Checkout**: A boolean flag indicating if this address is designated as a preferred or default address for expedited checkout processes, streamlining the user experience.
-   **Is Default**: A boolean flag indicating if this is the user's primary or default address for its specific `AddressType`. A user might have a default shipping address and a default billing address.
-   **First Name / Last Name**: The name of the individual or recipient associated with the address.
-   **Address1 / Address2**: The primary and secondary lines for the street address.
-   **City**: The city component of the address.
-   **Zipcode**: The postal code or ZIP code of the address.
-   **Phone**: A contact phone number associated with the address.
-   **Company**: The company name associated with the address, if applicable.
-   **Country**: The country where the address is located. (Referenced from `Location` Bounded Context).
-   **State**: The state, province, or region where the address is located. (Referenced from `Location` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`UserAddress`**: This is the Aggregate Root. It represents a single address belonging to a user and is responsible for managing its own properties, ensuring data integrity, and controlling its lifecycle (creation, update, deletion).
    -   **Entities**: None directly owned by `UserAddress`.
    -   **Value Objects**:
        -   **`AddressType`**: An enumeration that categorizes user addresses as either `Shipping` or `Billing`, allowing for distinct handling and preferences.
        -   Other properties like `FirstName`, `LastName`, `Label`, `QuickCheckout`, `IsDefault`, `Address1`, `Address2`, `City`, `Zipcode`, `Phone`, and `Company` are intrinsic attributes of the `UserAddress` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `ApplicationUser` (from `Core.Domain.Identity.Users`): Referenced by `UserAddress` to establish ownership, but `ApplicationUser` is managed by its own aggregate.
-   `Country` (from `Core.Domain.Location`): Referenced by `UserAddress` to specify the country of the address, but `Country` is managed by its own aggregate.
-   `State` (from `Core.Domain.Location`): Referenced by `UserAddress` to specify the state/province of the address, but `State` is managed by its own aggregate.

### Value Objects (standalone, if any)

-   None explicitly defined as standalone value objects beyond `AddressType` which is part of the `UserAddress` aggregate.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All business logic related to the `UserAddress` is encapsulated within the aggregate itself.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.UserAddresses` bounded context.

-   A `UserAddress` must always be associated with a valid `UserId` and `CountryId`, ensuring it belongs to a user and has a geographical context.
-   Essential address components such as `FirstName`, `LastName`, `Address1`, `City`, and `Zipcode` are required for a complete address.
-   The `Label` property has a maximum length constraint (`UserAddressConstraints.LabelMaxLength`) to ensure consistency and prevent excessive data.
-   `UserAddress` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.
-   When a `UserAddress` is marked as `IsDefault`, the system (likely at the application service level) should ensure that only one address of a given `AddressType` is default for a user.

---

## 🤝 Relationships & Dependencies

-   **`UserAddress` to `ApplicationUser`**: A many-to-one relationship. Each `UserAddress` instance belongs to a single `ApplicationUser` (from the `Identity.Users` bounded context).
-   **`UserAddress` to `Country`**: A many-to-one relationship. Each `UserAddress` is associated with a `Country` (from the `Location` bounded context).
-   **`UserAddress` to `State`**: A many-to-one relationship. Each `UserAddress` can optionally be associated with a `State` (from the `Location` bounded context).
-   **Shared Kernel**: The `UserAddress` aggregate inherits from `Aggregate<Guid>` and implements the `IAddress` interface (from `SharedKernel.Domain.Entities`), leveraging common patterns for address representation. It also uses `CommonInput.Constraints` (from `SharedKernel.Validations`) for consistent validation logic.

---

## 🚀 Key Use Cases / Behaviors

-   **Create User Address**: Instantiate a new `UserAddress` for a given user, providing all necessary address details, its `AddressType`, and initial preferences like `IsDefault` and `QuickCheckout`.
-   **Update User Address Details**: Modify any of the address components (e.g., `FirstName`, `Address1`, `City`, `CountryId`), `Label`, `QuickCheckout`, `IsDefault`, or `AddressType` of an existing `UserAddress`.
-   **Delete User Address**: Remove an `UserAddress` from the system.
-   **Publish Domain Events**: Emit domain events (`UserAddressCreated`, `UserAddressUpdated`, `UserAddressDeleted`) to signal significant state changes in the address's lifecycle, enabling a decoupled architecture where other parts of the system can react asynchronously (e.g., updating user profiles, order processing).

---

## 📝 Considerations / Notes

-   While `UserAddress` is an aggregate root, the management of a user's *collection* of addresses (e.g., ensuring only one default shipping address) is typically handled by an application service or the `ApplicationUser` aggregate itself, which would orchestrate operations on `UserAddress` instances.
-   The `IAddress` interface from `SharedKernel` promotes consistency in address representation across different domains that might need address information (e.g., `Orders`, `Stores`).
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit and easier to manage.
-   The domain relies on external `Country` and `State` entities, implying a dependency on the `Location` bounded context for geographical data.