# Common Storage Use Cases

This directory defines the abstractions and models for interacting with a generic file storage system. It provides a standardized interface for common storage operations, along with models for file information and a comprehensive set of error definitions. This modular design allows for easy integration with various storage providers (e.g., local file system, cloud storage) without affecting the core application logic.

---

## Modules

### 1. Models (`Models/`)

This subdirectory contains data models and error definitions related to file storage operations.

-   **`Storage.Errors.cs`**:
    -   **Functionality**: Defines a static class `StorageErrors` that provides a comprehensive set of standardized `ErrorOr.Error` objects. These errors cover various scenarios such as file not found, upload failures (e.g., empty file, too large, invalid type), deletion failures, read failures, and general validation issues (e.g., invalid URL). This ensures consistent error reporting across all storage-related operations.
-   **`Storage.FileInfo.cs`**:
    -   **Functionality**: Defines the `StorageFileInfo` class, a simple data transfer object (DTO) used to encapsulate metadata about a stored file. This includes properties like `Path`, `Size`, `LastModifiedUtc`, and `Url`, providing essential information about files retrieved from storage.

### 2. Services (`Services/`)

This subdirectory defines the interface for the generic storage service.

-   **`Storage.Service.cs`**:
    -   **Functionality**: Defines the `IStorageService` interface, which outlines the contract for any concrete storage implementation. This interface provides methods for:
        -   `UploadFileAsync`: Uploads a file to storage, returning its URL or path.
        -   `DeleteFileAsync`: Deletes a file from storage using its URL.
        -   `GetFileAsync`: Retrieves a file as a stream from storage.
        -   `ExistsAsync`: Checks if a file exists in storage.
        -   `ListFilesAsync`: Lists files within a specified folder, with an option for recursive listing.
    -   **Purpose**: This interface ensures that the application's business logic remains decoupled from the specifics of the underlying storage technology, promoting flexibility and testability.

---

## Purpose

The components within this directory are designed to:

-   **Abstract Storage Logic**: Provide a clean abstraction layer over file storage operations, making it easy to switch between different storage providers.
-   **Standardize Error Handling**: Ensure consistent and detailed error reporting for all storage-related issues.
-   **Encapsulate File Metadata**: Offer a structured way to represent information about stored files.
-   **Promote Modularity**: Allow for independent development and testing of storage implementations.
