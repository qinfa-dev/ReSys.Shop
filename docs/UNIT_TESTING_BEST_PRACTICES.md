# Unit Testing Best Practices for ReSys.Shop

This document outlines best practices for writing unit tests within the `ReSys.Shop` project, drawing heavily from industry standards and Microsoft's recommendations for .NET development. Adhering to these guidelines ensures tests are effective, maintainable, and reliable.

## Characteristics of a Good Unit Test

Good unit tests should be:
*   **Fast**: Tests should run quickly to provide rapid feedback.
*   **Isolated**: Tests should be independent of each other; the order of execution should not matter.
*   **Repeatable**: Running the same test multiple times should yield the same result.
*   **Self-checking**: Tests should automatically determine if they passed or failed, without manual inspection.
*   **Timely**: Tests should be written concurrently with the code they test (e.g., Test-Driven Development).

## Key Recommendations

1.  **Avoid Infrastructure Dependencies**:
    *   Unit tests should focus on business logic and avoid direct interaction with databases, file systems, network services, or other external infrastructure.
    *   Use fakes, mocks, or stubs to isolate the code under test from its dependencies.

2.  **Clear Naming Standards**:
    *   Use a consistent naming convention that clearly describes what the test is doing. A common pattern is `MethodName_Scenario_ExpectedBehavior`.
    *   Example: `Product_Create_ShouldReturnProduct_WhenValidParameters` or `Product_Create_ShouldReturnNameRequiredError_WhenNameIsNullOrEmpty`.

3.  **Arrange, Act, Assert (AAA) Pattern**:
    *   Organize the code in your test methods into these three distinct sections:
        *   **Arrange**: Set up the test objects and data.
        *   **Act**: Execute the method or code under test.
        *   **Assert**: Verify that the expected outcome occurred.

4.  **Write Minimally Passing Tests**:
    *   Write just enough code in the test to verify a single behavior. Avoid testing multiple concerns in one unit test.

5.  **Avoid Magic Strings and Complex Logic**:
    *   Do not embed arbitrary string literals or complex conditional logic directly in tests. Use constants or helper methods for clarity.
    *   Keep test logic simple and easy to understand.

6.  **Helper Methods for Setup, Not `Setup`/`Teardown`**:
    *   For xUnit, prefer using helper methods to create common test data or objects, rather than relying solely on class constructors or `Dispose` for setup/teardown across all tests in a fixture. This improves test isolation.

7.  **Validate Private Methods through Public Callers**:
    *   Generally, avoid directly testing private methods. Their behavior should be validated indirectly through the public methods that call them. If a private method is complex enough to warrant direct testing, it might be a sign it should be a separate, testable public class or method.

8.  **Understand Fakes, Mocks, and Stubs**:
    *   **Fake**: An object that replaces a real object for testing purposes, but typically has a simpler implementation (e.g., in-memory database).
    *   **Stub**: An object that provides canned answers to calls made during the test, usually not responding to anything outside what's programmed for the test.
    *   **Mock**: An object that records calls made to it and allows you to verify that specific calls were made (e.g., verifying a method was called with certain arguments).
    *   Use appropriate types of test doubles based on whether you need to control behavior (stub) or verify interaction (mock).

9.  **Handle Statics and Time with "Seams"**:
    *   Dependencies on static methods (e.g., `DateTime.Now`) or tightly coupled code can make testing difficult. Introduce "seams" (e.g., an interface for a clock service) to allow test control over such dependencies.

## Applying These Standards in ReSys.Shop

When writing unit tests for `ReSys.Core` domain models, commands, queries, and other business logic, ensure these best practices are followed. This includes:
*   Using `xUnit` for test structure.
*   Employing `FluentAssertions` for clear and readable assertions.
*   Focusing tests on the behavior of `ErrorOr<T>` results, both success and error paths.
*   Asserting domain events raised by aggregate roots.
*   Organizing tests logically by feature or domain area.

---
**Source**: [Best practices for writing unit tests - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
