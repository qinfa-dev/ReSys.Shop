using FluentAssertions;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Orders;

using ErrorOr;

using ReSys.Core.Domain.PaymentMethods;
using ReSys.Core.Domain.ShippingMethods; // Add this using for ErrorOr.Deleted
using ReSys.Core.Domain.Stores;

using static ReSys.Core.Domain.Stores.Store;

namespace Core.UnitTests.Domain.Stores;

public class StoreTests
{
    // Helper method to create a valid Store instance for tests
    private static Store CreateValidStore(string name = "Test Store", string code = "TEST",
        string url = "test.example.com")
    {
        var result = Store.Create(
            name: name,
            code: code,
            url: url,
            currency: "USD",
            mailFromAddress: "orders@test.com",
            customerSupportEmail: "support@test.com");
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper method to create a valid Product instance for tests
    private static Product CreateValidProduct(string name = "Test Product", string slug = "test-product")
    {
        var result = Product.Create(name: name, slug: slug);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper method to create a valid StockLocation instance for tests
    private static StockLocation CreateValidStockLocation(string name = "Test Location", string code = "TLOC")
    {
        var result = StockLocation.Create(name, code);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper method to create a valid ShippingMethod instance for tests
    private static ShippingMethod CreateValidShippingMethod(string name = "Test Shipping", ShippingMethod.ShippingType type = ShippingMethod.ShippingType.Standard, decimal baseCost = 5.0m)
    {
        var result = ShippingMethod.Create(name: name, presentation: name, type: type, baseCost: baseCost);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper method to create a valid PaymentMethod instance for tests
    private static PaymentMethod CreateValidPaymentMethod(string name = "Test Payment", string presentation = "Test Payment", PaymentMethod.PaymentType type = PaymentMethod.PaymentType.CreditCard)
    {
        var result = PaymentMethod.Create(name: name, presentation: presentation, type: type);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper method to create a valid Order instance for tests
    private static Order CreateValidOrder(Guid storeId, string currency = "USD", string? userId = null)
    {
        var result = Order.Create(storeId, currency, userId);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    [Fact]
    public void Store_Create_ShouldReturnStore_WhenValidParameters()
    {
        // Arrange
        var name = "Valid Store Name";
        var code = "VSN";
        var url = "https://valid.store.com";
        var currency = "EUR";
        var mailFromAddress = "email@store.com";
        var customerSupportEmail = "support@store.com";
        var timezone = "America/New_York";

        // Act
        var result = Store.Create(
            name: name,
            code: code,
            url: url,
            currency: currency,
            mailFromAddress: mailFromAddress,
            customerSupportEmail: customerSupportEmail,
            timezone: timezone);

        // Assert
        result.IsError.Should().BeFalse();
        var store = result.Value;
        store.Should().NotBeNull();
        store.Name.Should().Be(name.ToLowerInvariant().Replace(" ", "-"));
        store.Presentation.Should().Be(name); // Default presentation to name if not provided
        store.Code.Should().Be(code.ToUpperInvariant());
        store.Url.Should().Be(url.ToLowerInvariant());
        store.DefaultCurrency.Should().Be(currency.ToUpperInvariant());
        store.MailFromAddress.Should().Be(mailFromAddress);
        store.CustomerSupportEmail.Should().Be(customerSupportEmail);
        store.Timezone.Should().Be(timezone);
        store.Available.Should().BeTrue();
        store.GuestCheckoutAllowed.Should().BeTrue();
        store.PasswordProtected.Should().BeFalse();
        store.IsDeleted.Should().BeFalse();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreCreated);
        store.DomainEvents.First().As<Events.StoreCreated>().StoreId.Should().Be(store.Id);
    }

    [Fact]
    public void Store_Create_ShouldUseDefaultValues_WhenOptionalParametersAreNull()
    {
        // Arrange
        var name = "Default Store";
        var code = "DEFAULT_STORE"; // Auto-generated from name                                                    
        var url = "default-store"; // Auto-generated from name                                                     
                                                                                                                       
            // Act                                                                                                     
        var result = Store.Create(name: name);                                                                     
                                                                                                                       
            // Assert                                                                                                  
        result.IsError.Should().BeFalse();
        var store = result.Value;
        store.Should().NotBeNull();
        store.Name.Should().Be(name.ToLowerInvariant().Replace(" ", "-"));
        store.Code.Should().Be(code.ToUpperInvariant());
        store.Url.Should().Be(url);
        store.DefaultCurrency.Should().Be(Constraints.DefaultCurrency);
        store.DefaultLocale.Should().Be(Constraints.DefaultLocale);
        store.Timezone.Should().Be(Constraints.DefaultTimezone);
        store.MailFromAddress.Should().BeNull();
        store.CustomerSupportEmail.Should().BeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreCreated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Store_Create_ShouldReturnNameRequiredError_WhenNameIsNullOrEmpty(string? invalidName)
    {
        // Act
        var result = Store.Create(name: invalidName!, code: "CODE", url: "url.com");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.NameRequired);
    }

    [Fact]
    public void Store_Create_ShouldReturnNameTooLongError_WhenNameExceedsMaxLength()
    {
        // Arrange
        var longName = new string('A', Constraints.NameMaxLength + 1);

        // Act
        var result = Store.Create(name: longName, code: "CODE", url: "url.com");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.NameTooLong);
    }

    [Fact]
    public void Store_Create_ShouldReturnPresentationTooLongError_WhenPresentationExceedsMaxLength()
    {
        // Arrange
        var longPresentation = new string('A', Constraints.PresentationMaxLength + 1);

        // Act
        var result = Store.Create(name: "Name", presentation: longPresentation, code: "CODE", url: "url.com");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.PresentationTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Store_Create_ShouldReturnCodeRequiredError_WhenCodeIsNullOrEmpty(string? invalidCode)
    {
        // Act
        var result = Store.Create(name: "Name", code: invalidCode, url: "url.com");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.CodeRequired);
    }

    [Fact]
    public void Store_Create_ShouldReturnCodeTooLongError_WhenCodeExceedsMaxLength()
    {
        // Arrange
        var longCode = new string('A', Constraints.CodeMaxLength + 1);

        // Act
        var result = Store.Create(name: "Name", code: longCode, url: "url.com");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.CodeTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Store_Create_ShouldReturnUrlRequiredError_WhenUrlIsNullOrEmpty(string? invalidUrl)
    {
        // Act
        var result = Store.Create(name: "Name", code: "CODE", url: invalidUrl);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.UrlRequired);
    }

    [Fact]
    public void Store_Create_ShouldReturnUrlTooLongError_WhenUrlExceedsMaxLength()
    {
        // Arrange
        var longUrl =
            "https://" + new string('A', Constraints.UrlMaxLength - 8 + 1) + ".com"; // -8 for https:// and .com

        // Act
        var result = Store.Create(name: "Name", code: "CODE", url: longUrl);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.UrlTooLong);
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("GBPX")]
    [InlineData("EUDR")] // New invalid currency
    public void Store_Create_ShouldReturnInvalidCurrencyError_WhenCurrencyIsInvalid(string invalidCurrency)
    {
        // Act
        var result = Store.Create(name: "Name", code: "CODE", url: "url.com", currency: invalidCurrency);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidCurrency);
    }

    [Theory]
    [InlineData("invalid-timezone")]
    [InlineData("Europe/Invalid")]
    public void Store_Create_ShouldReturnInvalidTimezoneError_WhenTimezoneIsInvalid(string invalidTimezone)
    {
        // Act
        var result = Store.Create(name: "Name", code: "CODE", url: "url.com", timezone: invalidTimezone);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidTimezone);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@.com")]
    public void Store_Create_ShouldReturnInvalidMailFromAddressError_WhenMailFromAddressIsInvalid(
        string invalidEmail)
    {
        // Act
        var result = Store.Create(name: "Name", code: "CODE", url: "url.com", mailFromAddress: invalidEmail);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidMailFromAddress);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@.com")]
    public void Store_Create_ShouldReturnInvalidCustomerSupportEmailError_WhenCustomerSupportEmailIsInvalid(
        string invalidEmail)
    {
        // Act
        var result = Store.Create(name: "Name", code: "CODE", url: "url.com", customerSupportEmail: invalidEmail);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidCustomerSupportEmail);
    }

    [Fact]
    public void Store_Create_ShouldAutoGenerateCodeAndUrl_WhenNotProvided()
    {
        // Arrange
        var name = "My Awesome Store";

        // Act
        var result = Store.Create(name: name);

        // Assert
        result.IsError.Should().BeFalse();
        var store = result.Value;
        store.Code.Should().Be("MY_AWESOME_STORE");
        store.Url.Should().Be("my-awesome-store");
    }

    [Fact]
    public void Store_Update_ShouldUpdateNameAndPresentation_WhenValidNamesProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var newName = "New Store Name";
        var newPresentation = "New Store Presentation";

        // Act
        var result = store.Update(name: newName, presentation: newPresentation);

        // Assert
        result.IsError.Should().BeFalse();
        store.Name.Should().Be(newName.ToLowerInvariant().Replace(" ", "-"));
        store.Presentation.Should().Be(newPresentation);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreUpdated);
    }

    [Fact]
    public void Store_Update_ShouldUpdateUrl_WhenValidUrlProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var newUrl = "https://new.store.com";

        // Act
        var result = store.Update(url: newUrl);

        // Assert
        result.IsError.Should().BeFalse();
        store.Url.Should().Be(newUrl.ToLowerInvariant());
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreUpdated);
    }

    [Fact]
    public void Store_Update_ShouldReturnNameTooLongError_WhenNewNameExceedsMaxLength()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var longName = new string('B', Constraints.NameMaxLength + 1);

        // Act
        var result = store.Update(name: longName);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.NameTooLong);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_Update_ShouldReturnPresentationTooLongError_WhenNewPresentationExceedsMaxLength()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var longPresentation = new string('B', Constraints.PresentationMaxLength + 1);

        // Act
        var result = store.Update(presentation: longPresentation);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.PresentationTooLong);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_Update_ShouldUpdateMailFromAddressAndCustomerSupportEmail_WhenValidEmailsProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var newMailFrom = "newmail@test.com";
        var newSupportEmail = "newsupport@test.com";

        // Act
        var result = store.Update(mailFromAddress: newMailFrom, customerSupportEmail: newSupportEmail);

        // Assert
        result.IsError.Should().BeFalse();
        store.MailFromAddress.Should().Be(newMailFrom);
        store.CustomerSupportEmail.Should().Be(newSupportEmail);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreUpdated);
    }

    [Fact]
    public void Store_Update_ShouldUpdateSeoMetadata_WhenProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var newMetaTitle = "New Meta Title";
        var newMetaDescription = "New Meta Description";
        var newMetaKeywords = "keyword1, keyword2";
        var newSeoTitle = "New Seo Title";

        // Act
        var result = store.Update(
            metaTitle: newMetaTitle,
            metaDescription: newMetaDescription,
            metaKeywords: newMetaKeywords,
            seoTitle: newSeoTitle);

        // Assert
        result.IsError.Should().BeFalse();
        store.MetaTitle.Should().Be(newMetaTitle);
        store.MetaDescription.Should().Be(newMetaDescription);
        store.MetaKeywords.Should().Be(newMetaKeywords);
        store.SeoTitle.Should().Be(newSeoTitle);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreUpdated);
    }

    [Fact]
    public void Store_Update_ShouldUpdateAvailabilityAndGuestCheckout_WhenProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();

        // Act
        var result = store.Update(available: false, guestCheckoutAllowed: false);

        // Assert
        result.IsError.Should().BeFalse();
        store.Available.Should().BeFalse();
        store.GuestCheckoutAllowed.Should().BeFalse();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreUpdated);
    }

    [Fact]
    public void Store_Update_ShouldUpdateDefaultLocaleAndCurrency_WhenValidValuesProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var newLocale = "fr";
        var newCurrency = "GBP";

        // Act
        var result = store.Update(defaultLocale: newLocale, defaultCurrency: newCurrency);

        // Assert
        result.IsError.Should().BeFalse();
        store.DefaultLocale.Should().Be(newLocale);
        store.DefaultCurrency.Should().Be(newCurrency);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreUpdated);
    }

    [Fact]
    public void Store_Update_ShouldReturnInvalidCurrencyError_WhenInvalidCurrencyProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();

        // Act
        var result = store.Update(defaultCurrency: "XYZ");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidCurrency);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_Update_ShouldNotUpdate_WhenNoChangesAreMade()
    {
        // Arrange
        var store = CreateValidStore();
        var initialUpdatedAt = store.UpdatedAt;
        store.ClearDomainEvents();

        // Act
        var result = store.Update(name: store.Name, url: store.Url); // No actual changes

        // Assert
        result.IsError.Should().BeFalse();
        store.UpdatedAt.Should().Be(initialUpdatedAt);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_SetAddress_ShouldUpdateAddressProperties_WhenProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var countryId = Guid.NewGuid();
        var stateId = Guid.NewGuid();

        // Act
        var result = store.SetAddress(
            address1: "123 Main St",
            address2: "Apt 101",
            city: "Test City",
            zipcode: "12345",
            phone: "555-1234",
            company: "Test Co",
            countryId: countryId,
            stateId: stateId);

        // Assert
        result.IsError.Should().BeFalse();
        store.Address1.Should().Be("123 Main St");
        store.Address2.Should().Be("Apt 101");
        store.City.Should().Be("Test City");
        store.Zipcode.Should().Be("12345");
        store.Phone.Should().Be("555-1234");
        store.Company.Should().Be("Test Co");
        store.CountryId.Should().Be(countryId);
        store.StateId.Should().Be(stateId);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreAddressUpdated);
    }

    [Fact]
    public void Store_SetAddress_ShouldNotUpdate_WhenNoChangesAreMade()
    {
        // Arrange
        var store = CreateValidStore();
        var initialUpdatedAt = store.UpdatedAt;
        store.ClearDomainEvents();

        // Act
        var result = store.SetAddress(address1: store.Address1, city: store.City); // No actual changes

        // Assert
        result.IsError.Should().BeFalse();
        store.UpdatedAt.Should().Be(initialUpdatedAt);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_SetSocialLinks_ShouldUpdateSocialLinks_WhenProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();

        // Act
        var result = store.SetSocialLinks(
            facebook: "https://facebook.com/test",
            instagram: "@teststore",
            twitter: "https://twitter.com/teststore");

        // Assert
        result.IsError.Should().BeFalse();
        store.Facebook.Should().Be("https://facebook.com/test");
        store.Instagram.Should().Be("@teststore");
        store.Twitter.Should().Be("https://twitter.com/teststore");
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreSocialLinksUpdated);
    }

    [Fact]
    public void Store_MakeDefault_ShouldSetDefaultToTrueAndRaiseEvent_WhenNotDefault()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        store.Default = false; // Ensure it's not default initially

        // Act
        var result = store.MakeDefault();

        // Assert
        result.IsError.Should().BeFalse();
        store.Default.Should().BeTrue();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreMadeDefault);
    }

    [Fact]
    public void Store_MakeDefault_ShouldNotChangeStateOrRaiseEvent_WhenAlreadyDefault()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        store.Default = true; // Ensure it's default initially
        var initialUpdatedAt = store.UpdatedAt;

        // Act
        var result = store.MakeDefault();

        // Assert
        result.IsError.Should().BeFalse();
        store.Default.Should().BeTrue();
        store.UpdatedAt.Should().Be(initialUpdatedAt);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_ProtectWithPassword_ShouldSetPasswordProtectedAndRaiseEvent_WhenValidPasswordProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var hashedPassword = "hashedPassword123";

        // Act
        var result = store.ProtectWithPassword(hashedPassword);

        // Assert
        result.IsError.Should().BeFalse();
        store.PasswordProtected.Should().BeTrue();
        store.StorefrontPassword.Should().Be(hashedPassword);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StorePasswordProtectionEnabled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Store_ProtectWithPassword_ShouldReturnInvalidPasswordError_WhenPasswordIsNullOrEmpty(string? invalidPassword)
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();

        // Act
        var result = store.ProtectWithPassword(invalidPassword!);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidPassword);
        store.PasswordProtected.Should().BeFalse();
        store.StorefrontPassword.Should().BeNull();
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_RemovePasswordProtection_ShouldRemovePasswordProtectionAndRaiseEvent_WhenProtected()
    {
        // Arrange
        var store = CreateValidStore();
        store.ProtectWithPassword("somehash"); // Protect it first
        store.ClearDomainEvents();

        // Act
        var result = store.RemovePasswordProtection();

        // Assert
        result.IsError.Should().BeFalse();
        store.PasswordProtected.Should().BeFalse();
        store.StorefrontPassword.Should().BeNull();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StorePasswordProtectionRemoved);
    }

    [Fact]
    public void Store_AddProduct_ShouldAddProductAndRaiseEvent_WhenValidProductProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var product = CreateValidProduct();

        // Act
        var result = store.AddProduct(product);

        // Assert
        result.IsError.Should().BeFalse();
        store.StoreProducts.Should().ContainSingle(sp => sp.ProductId == product.Id);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.ProductAddedToStore);
    }

    [Fact]
    public void Store_AddProduct_ShouldReturnInvalidProductError_WhenNullProductProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();

        // Act
        var result = store.AddProduct(null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidProduct);
        store.StoreProducts.Should().BeEmpty();
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_AddProduct_ShouldReturnProductAlreadyInStoreError_WhenProductAlreadyExists()
    {
        // Arrange
        var store = CreateValidStore();
        var product = CreateValidProduct();
        store.AddProduct(product); // Add once
        store.ClearDomainEvents();

        // Act
        var result = store.AddProduct(product); // Add again

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.ProductAlreadyInStore);
        store.StoreProducts.Should().ContainSingle(sp => sp.ProductId == product.Id);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_RemoveProduct_ShouldRemoveProductAndRaiseEvent_WhenProductExists()
    {
        // Arrange
        var store = CreateValidStore();
        var product = CreateValidProduct();
        store.AddProduct(product);
        store.ClearDomainEvents();

        // Act
        var result = store.RemoveProduct(product.Id);

        // Assert
        result.IsError.Should().BeFalse();
        store.StoreProducts.Should().BeEmpty();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.ProductRemovedFromStore);
    }

    [Fact]
    public void Store_RemoveProduct_ShouldReturnProductNotInStoreError_WhenProductDoesNotExist()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var nonExistentProductId = Guid.NewGuid();

        // Act
        var result = store.RemoveProduct(nonExistentProductId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.ProductNotInStore);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_UpdateProductSettings_ShouldUpdateSettingsAndRaiseEvent_WhenProductExists()
    {
        // Arrange
        var store = CreateValidStore();
        var product = CreateValidProduct();
        store.AddProduct(product, visible: true, featured: false);
        store.ClearDomainEvents();
        var storeProduct = store.StoreProducts.First();

        // Act
        var result = store.UpdateProductSettings(product.Id, visible: false, featured: true);

        // Assert
        result.IsError.Should().BeFalse();
        storeProduct.Visible.Should().BeFalse();
        storeProduct.Featured.Should().BeTrue();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.ProductSettingsUpdated);
    }

    [Fact]
    public void Store_UpdateProductSettings_ShouldNotUpdateOrRaiseEvent_WhenNoChanges()
    {
        // Arrange
        var store = CreateValidStore();
        var product = CreateValidProduct();
        store.AddProduct(product, visible: true, featured: false);
        store.ClearDomainEvents();

        // Act
        var result = store.UpdateProductSettings(product.Id, visible: true, featured: false);

        // Assert
        result.IsError.Should().BeFalse();
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_AddShippingMethod_ShouldAddMethodAndRaiseEvent_WhenValidMethodProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var shippingMethod = CreateValidShippingMethod();

        // Act
        var result = store.AddShippingMethod(shippingMethod, available: true, storeBaseCost: 10.00m);

        // Assert
        result.IsError.Should().BeFalse();
        store.StoreShippingMethods.Should().ContainSingle(sm => sm.ShippingMethodId == shippingMethod.Id);
        store.StoreShippingMethods.First().Available.Should().BeTrue();
        store.StoreShippingMethods.First().StoreBaseCost.Should().Be(10.00m);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.ShippingMethodAddedToStore);
    }

    [Fact]
    public void Store_AddShippingMethod_ShouldReturnInvalidShippingMethodError_WhenNullMethodProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();

        // Act
        var result = store.AddShippingMethod(null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidShippingMethod);
        store.StoreShippingMethods.Should().BeEmpty();
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_AddShippingMethod_ShouldReturnShippingMethodAlreadyAddedError_WhenMethodAlreadyExists()
    {
        // Arrange
        var store = CreateValidStore();
        var shippingMethod = CreateValidShippingMethod();
        store.AddShippingMethod(shippingMethod); // Add once
        store.ClearDomainEvents();

        // Act
        var result = store.AddShippingMethod(shippingMethod); // Add again

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.ShippingMethodAlreadyAdded);
        store.StoreShippingMethods.Should().ContainSingle(sm => sm.ShippingMethodId == shippingMethod.Id);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_RemoveShippingMethod_ShouldRemoveMethodAndRaiseEvent_WhenMethodExists()
    {
        // Arrange
        var store = CreateValidStore();
        var shippingMethod = CreateValidShippingMethod();
        store.AddShippingMethod(shippingMethod);
        store.ClearDomainEvents();

        // Act
        var result = store.RemoveShippingMethod(shippingMethod.Id);

        // Assert
        result.IsError.Should().BeFalse();
        store.StoreShippingMethods.Should().BeEmpty();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.ShippingMethodRemovedFromStore);
    }

    [Fact]
    public void Store_RemoveShippingMethod_ShouldReturnShippingMethodNotFoundError_WhenMethodDoesNotExist()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var nonExistentMethodId = Guid.NewGuid();

        // Act
        var result = store.RemoveShippingMethod(nonExistentMethodId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.ShippingMethodNotFound);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_UpdateShippingMethodSettings_ShouldUpdateSettingsAndRaiseEvent_WhenMethodExists()
    {
        // Arrange
        var store = CreateValidStore();
        var shippingMethod = CreateValidShippingMethod();
        store.AddShippingMethod(shippingMethod, available: true, storeBaseCost: 10.00m);
        store.ClearDomainEvents();
        var storeShippingMethod = store.StoreShippingMethods.First();

        // Act
        var result = store.UpdateShippingMethodSettings(shippingMethod.Id, available: false, storeBaseCost: 15.00m);

        // Assert
        result.IsError.Should().BeFalse();
        storeShippingMethod.Available.Should().BeFalse();
        storeShippingMethod.StoreBaseCost.Should().Be(15.00m);
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.ShippingMethodSettingsUpdated);
    }

    [Fact]
    public void Store_UpdateShippingMethodSettings_ShouldNotUpdateOrRaiseEvent_WhenNoChanges()
    {
        // Arrange
        var store = CreateValidStore();
        var shippingMethod = CreateValidShippingMethod();
        store.AddShippingMethod(shippingMethod, available: true, storeBaseCost: 10.00m);
        store.ClearDomainEvents();

        // Act
        var result = store.UpdateShippingMethodSettings(shippingMethod.Id, available: true, storeBaseCost: 10.00m);

        // Assert
        result.IsError.Should().BeFalse();
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_AddPaymentMethod_ShouldAddMethodAndRaiseEvent_WhenValidMethodProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var paymentMethod = CreateValidPaymentMethod();

        // Act
        var result = store.AddPaymentMethod(paymentMethod, available: true);

        // Assert
        result.IsError.Should().BeFalse();
        store.StorePaymentMethods.Should().ContainSingle(pm => pm.PaymentMethodId == paymentMethod.Id);
        store.StorePaymentMethods.First().Available.Should().BeTrue();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.PaymentMethodAddedToStore);
    }

    [Fact]
    public void Store_AddPaymentMethod_ShouldReturnInvalidPaymentMethodError_WhenNullMethodProvided()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();

        // Act
        var result = store.AddPaymentMethod(null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.InvalidPaymentMethod);
        store.StorePaymentMethods.Should().BeEmpty();
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_AddPaymentMethod_ShouldReturnPaymentMethodAlreadyAddedError_WhenMethodAlreadyExists()
    {
        // Arrange
        var store = CreateValidStore();
        var paymentMethod = CreateValidPaymentMethod();
        store.AddPaymentMethod(paymentMethod); // Add once
        store.ClearDomainEvents();

        // Act
        var result = store.AddPaymentMethod(paymentMethod); // Add again

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.PaymentMethodAlreadyAdded);
        store.StorePaymentMethods.Should().ContainSingle(pm => pm.PaymentMethodId == paymentMethod.Id);
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_RemovePaymentMethod_ShouldRemoveMethodAndRaiseEvent_WhenMethodExists()
    {
        // Arrange
        var store = CreateValidStore();
        var paymentMethod = CreateValidPaymentMethod();
        store.AddPaymentMethod(paymentMethod);
        store.ClearDomainEvents();

        // Act
        var result = store.RemovePaymentMethod(paymentMethod.Id);

        // Assert
        result.IsError.Should().BeFalse();
        store.StorePaymentMethods.Should().BeEmpty();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.PaymentMethodRemovedFromStore);
    }

    [Fact]
    public void Store_Delete_ShouldSetIsDeletedToTrueAndRaiseEvent_WhenNotDefaultAndNoActiveOrders()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();

        // Act
        var result = store.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        store.IsDeleted.Should().BeTrue();
        store.DeletedAt.Should().NotBeNull();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreDeleted);
    }

    [Fact]
    public void Store_Delete_ShouldReturnCannotDeleteDefaultStoreError_WhenIsDefaultAndForceIsFalse()
    {
        // Arrange
        var store = CreateValidStore();
        store.Default = true;
        store.ClearDomainEvents();

        // Act
        var result = store.Delete(force: false);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.CannotDeleteDefaultStore);
        store.IsDeleted.Should().BeFalse();
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_Delete_ShouldDeleteDefaultStore_WhenForceIsTrue()
    {
        // Arrange
        var store = CreateValidStore();
        store.Default = true;
        store.ClearDomainEvents();

        // Act
        var result = store.Delete(force: true);

        // Assert
        result.IsError.Should().BeFalse();
        store.IsDeleted.Should().BeTrue();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreDeleted);
    }

    [Fact]
    public void Store_Delete_ShouldReturnHasActiveOrdersError_WhenHasActiveOrdersAndForceIsFalse()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var userId = Guid.NewGuid();
        store.Orders.Add(CreateValidOrder(store.Id, "USD", userId.ToString())); // Add an active order

        // Act
        var result = store.Delete(force: false);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(Errors.HasActiveOrders);
        store.IsDeleted.Should().BeFalse();
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_Delete_ShouldDeleteStoreWithActiveOrders_WhenForceIsTrue()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        var userId = Guid.NewGuid();
        store.Orders.Add(CreateValidOrder(store.Id, "USD", userId.ToString())); // Add an active order

        // Act
        var result = store.Delete(force: true);

        // Assert
        result.IsError.Should().BeFalse();
        store.IsDeleted.Should().BeTrue();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreDeleted);
    }

    [Fact]
    public void Store_Delete_ShouldReturnDeleted_WhenAlreadyDeleted()
    {
        // Arrange
        var store = CreateValidStore();
        store.Delete(); // Delete it first
        store.ClearDomainEvents();

        // Act
        var result = store.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        store.IsDeleted.Should().BeTrue(); // Still deleted
        result.Value.Should().Be(Result.Deleted); // Corrected assertion
        store.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Store_Restore_ShouldSetIsDeletedToFalseAndRaiseEvent_WhenDeleted()
    {
        // Arrange
        var store = CreateValidStore();
        store.Delete(); // Delete it first
        store.ClearDomainEvents();

        // Act
        var result = store.Restore();

        // Assert
        result.IsError.Should().BeFalse();
        store.IsDeleted.Should().BeFalse();
        store.DeletedAt.Should().BeNull();
        store.UpdatedAt.Should().NotBeNull();
        store.DomainEvents.Should().ContainSingle(e => e is Events.StoreRestored);
    }

    [Fact]
    public void Store_Restore_ShouldNotChangeStateOrRaiseEvent_WhenNotDeleted()
    {
        // Arrange
        var store = CreateValidStore();
        store.ClearDomainEvents();
        store.IsDeleted = false; // Ensure not deleted initially
        var initialUpdatedAt = store.UpdatedAt;

        // Act
        var result = store.Restore();

        // Assert
        result.IsError.Should().BeFalse();
        store.IsDeleted.Should().BeFalse();
        store.UpdatedAt.Should().Be(initialUpdatedAt);
        store.DomainEvents.Should().BeEmpty();
    }
}
