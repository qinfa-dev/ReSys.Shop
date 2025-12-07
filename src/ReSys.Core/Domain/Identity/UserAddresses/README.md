# Identity.UserAddresses Bounded Context

This document describes the `Identity.UserAddresses` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the addresses associated with a user, providing a robust mechanism for storing, retrieving, and managing multiple shipping and billing addresses. It ensures the integrity and consistency of address data, facilitates user preferences such as default addresses and quick checkout options, and supports geographical data integration through country and state associations.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.UserAddresses` bounded context.

-   **User Address**: A physical address (e.g., street, city, postal code) explicitly associated with a specific user, used for purposes such as shipping, billing, or contact. Represented by the <see cref="UserAddress"/> aggregate.
-   **Address Type**: An enumeration (<see cref="AddressType"/>) indicating the primary purpose or classification of the address (e.g., <c>Shipping</c>, <c>Billing</c>).
-   **Label**: A user-defined, human-readable identifier or nickname for an address (e.g., "Home", "Work", "My Office"), allowing users to easily distinguish between their saved addresses. Its maximum length is defined by <see cref="UserAddress.UserAddressConstraints.LabelMaxLength"/>.
-   **Quick Checkout**: A boolean flag indicating if this address is designated as a preferred or default address for expedited checkout processes, streamlining the user experience (<see cref="UserAddress.QuickCheckout"/>).
-   **Is Default**: A boolean flag indicating if this is the user's primary or default address for its specific <see cref="AddressType"/>. A user might have a default shipping address and a default billing address (<see cref="UserAddress.IsDefault"/>).
-   **First Name / Last Name**: The name of the individual or recipient associated with the address (<see cref="UserAddress.FirstName"/>, <see cref="UserAddress.LastName"/>).
-   **Address1 / Address2**: The primary (<see cref="UserAddress.Address1"/>) and secondary (<see cref="UserAddress.Address2"/>) lines for the street address.
-   **City**: The city component of the address (<see cref="UserAddress.City"/>).
-   **Zipcode**: The postal code or ZIP code of the address (<see cref="UserAddress.Zipcode"/>).
-   **Phone**: A contact phone number associated with the address (<see cref="UserAddress.Phone"/>).
-   **Company**: The company name associated with the address, if applicable (<see cref="UserAddress.Company"/>).
-   **Country**: The country where the address is located. (Referenced from `Location` Bounded Context).
-   **State**: The state, province, or region where the address is located. (Referenced from `Location` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`UserAddress`**: This is the Aggregate Root. It represents a single address belonging to a user and is responsible for managing its own properties, ensuring data integrity, and controlling its lifecycle (creation, update, deletion).
    -   **Entities**: None directly owned by `UserAddress`.
    -   **Value Objects**:
        -   **<see cref="AddressType"/>**: An enumeration that categorizes user addresses as either <c>Shipping</c> or <c>Billing</c>, allowing for distinct handling and preferences.
        -   Other properties like <c>FirstName</c>, <c>LastName</c>, <c>Label</c>, <c>QuickCheckout</c>, <c>IsDefault</c>, <c>Address1</c>, <c>Address2</c>, <c>City</c>, <c>Zipcode</c>, <c>Phone</c>, and <c>Company</c> are intrinsic attributes of the <see cref="UserAddress"/> aggregate.

### Entities (not part of an Aggregate Root, if any)

-   <see cref="ApplicationUser"/> (from `Core.Domain.Identity.Users`): Referenced by <see cref="UserAddress"/> to establish ownership, but <see cref="ApplicationUser"/> is managed by its own aggregate.
-   <see cref="Country"/> (from `Core.Domain.Location`): Referenced by <see cref="UserAddress"/> to specify the country of the address, but <see cref="Country"/> is managed by its own aggregate.
-   <see cref="State"/> (from `Core.Domain.Location`): Referenced by <see cref="UserAddress"/> to specify the state/province of the address, but <see cref="State"/> is managed by its own aggregate.

### Value Objects (standalone, if any)

-   None explicitly defined as standalone value objects beyond <see cref="AddressType"/> which is part of the <see cref="UserAddress"/> aggregate.

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All business logic related to the `UserAddress` is encapsulated within the aggregate itself.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.UserAddresses` bounded context.

-   A <see cref="UserAddress"/> must always be associated with a valid <c>UserId</c> and <c>CountryId</c>, ensuring it belongs to a user and has a geographical context.
-   Essential address components such as <c>FirstName</c>, <c>LastName</c>, <c>Address1</c>, <c>City</c>, and <c>Zipcode</c> are typically required for a complete address.
-   The <c>Label</c> property has a maximum length constraint (<see cref="UserAddress.UserAddressConstraints.LabelMaxLength"/>) to ensure consistency and prevent excessive data.
-   <see cref="UserAddress"/> instances track their creation and update timestamps (<c>CreatedAt</c>, <c>UpdatedAt</c>), adhering to auditing requirements.
-   When a <see cref="UserAddress"/> is marked as <c>IsDefault</c>, the system (likely at the application service level) should ensure that only one address of a given <see cref="AddressType"/> is default for a user, preventing multiple default addresses of the same type.

---

## 🤝 Relationships & Dependencies

-   **`UserAddress` to `ApplicationUser`**: A many-to-one relationship. Each `UserAddress` instance belongs to a single `ApplicationUser` (from the `Identity.Users` bounded context).
-   **`UserAddress` to `Country`**: A many-to-one relationship. Each `UserAddress` is associated with a `Country` (from the `Location` bounded context).
-   **`UserAddress` to `State`**: A many-to-one relationship. Each `UserAddress` can optionally be associated with a `State` (from the `Location` bounded context).
-   **Shared Kernel**: The `UserAddress` aggregate inherits from `Aggregate<Guid>` and implements the `IAddress` interface (from `SharedKernel.Domain.Entities`), leveraging common patterns for address representation. It also uses `CommonInput.Constraints` (from `SharedKernel.Validations`) for consistent validation logic.

---

## 🚀 Key Use Cases / Behaviors

-   **Create User Address**: Instantiate a new <see cref="UserAddress"/> for a given user using <see cref="UserAddress.Create(string, string, string, Guid, string, string, string, Guid?, string?, string?, string?, string?, bool, bool, AddressType)"/>, providing all necessary address details, its <see cref="AddressType"/>, and initial preferences like <c>IsDefault</c> and <c>QuickCheckout</c>.
-   **Update User Address Details**: Modify any of the address components (e.g., <c>FirstName</c>, <c>Address1</c>, <c>City</c>, <c>CountryId</c>), <c>Label</c>, <c>QuickCheckout</c>, <c>IsDefault</c>, or <c>AddressType</c> of an existing <see cref="UserAddress"/> using <see cref="UserAddress.Update(string?, string?, string?, bool?, bool?, AddressType?, string?, string?, string?, string?, Guid?, Guid?, string?, string?)"/>.
-   **Delete User Address**: Remove a <see cref="UserAddress"/> from the system using <see cref="UserAddress.Delete()"/>. This method adds a <see cref="UserAddress.Events.UserAddressDeleted"/> domain event.
-   **Publish Domain Events**: <see cref="UserAddress"/> emits domain events (<see cref="UserAddress.Events.UserAddressCreated"/>, <see cref="UserAddress.Events.UserAddressUpdated"/>, <see cref="UserAddress.Events.UserAddressDeleted"/>) to signal significant state changes in the address's lifecycle, enabling a decoupled architecture where other parts of the system can react asynchronously (e.g., updating user profiles, order processing).

---

## 📝 Considerations / Notes

-   While `UserAddress` is an aggregate root, the management of a user's *collection* of addresses (e.g., ensuring only one default shipping address) is typically handled by an application service or the `ApplicationUser` aggregate itself, which would orchestrate operations on `UserAddress` instances.
-   The `IAddress` interface from `SharedKernel` promotes consistency in address representation across different domains that might need address information (e.g., `Orders`, `Stores`).
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit and easier to manage.
-   The domain relies on external `Country` and `State` entities, implying a dependency on the `Location` bounded context for geographical data.