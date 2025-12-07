using FluentAssertions;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Orders.Payments;

using System;
using System.Collections.Generic;
using System.Linq;

using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.PaymentMethods;

using Xunit;

namespace Core.UnitTests.Domain.PaymentMethods;

public class PaymentMethodTests
{
    // Helper method to create a valid PaymentMethod instance
    private PaymentMethod CreateValidPaymentMethod(
        string name = "Test Card",
        string presentation = "Test Credit Card",
        PaymentMethod.PaymentType type = PaymentMethod.PaymentType.CreditCard,
        string? description = null,
        bool active = true,
        bool autoCapture = false,
        int position = 0,
        ReSys.Core.Common.Domain.Concerns.DisplayOn displayOn = ReSys.Core.Common.Domain.Concerns.DisplayOn.Both,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        var result = PaymentMethod.Create(
            name,
            presentation,
            type,
            description,
            active,
            autoCapture,
            position,
            displayOn,
            publicMetadata,
            privateMetadata);
        result.IsError.Should().BeFalse($"Failed to create valid payment method: {result.FirstError.Code} - {result.FirstError.Description}");
        return result.Value;
    }

    // Helper method to create a dummy Payment for testing 'InUse' scenarios
    private Payment CreateDummyPayment(Guid paymentMethodId)
    {
        var paymentResult = Payment.Create(
            orderId: Guid.NewGuid(),
            amountCents: 1000,
            currency: "USD",
            paymentMethodType: "CreditCard",
            paymentMethodId: paymentMethodId);
        paymentResult.IsError.Should().BeFalse();
        return paymentResult.Value;
    }

    #region Create Test Cases

    [Fact]
    public void Create_ShouldReturnPaymentMethod_WhenValidInputs()
    {
        // Arrange
        var name = "Visa/MasterCard";
        var presentation = "Pay with Credit Card";
        var type = PaymentMethod.PaymentType.CreditCard;

        // Act
        var result = PaymentMethod.Create(name, presentation, type);

        // Assert
        result.IsError.Should().BeFalse();
        var paymentMethod = result.Value;
        paymentMethod.Should().NotBeNull();
        paymentMethod.Name.Should().Be(name.ToSlug());
        paymentMethod.Presentation.Should().Be(presentation);
        paymentMethod.Type.Should().Be(type);
        paymentMethod.Active.Should().BeTrue();
        paymentMethod.AutoCapture.Should().BeFalse();
        paymentMethod.Position.Should().Be(0);
        paymentMethod.DisplayOn.Should().Be(ReSys.Core.Common.Domain.Concerns.DisplayOn.Both);
        paymentMethod.CreatedAt.Should().NotBe(default(DateTimeOffset));
        paymentMethod.PublicMetadata.Should().BeEmpty();
        paymentMethod.PrivateMetadata.Should().BeEmpty();
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Created);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues_WhenOptionalParametersAreOmitted()
    {
        // Arrange
        var name = "PayPal";
        var presentation = "PayPal Checkout";
        var type = PaymentMethod.PaymentType.PayPal;

        // Act
        var result = PaymentMethod.Create(name, presentation, type);

        // Assert
        result.IsError.Should().BeFalse();
        var paymentMethod = result.Value;
        paymentMethod.Active.Should().BeTrue();
        paymentMethod.AutoCapture.Should().BeFalse();
        paymentMethod.Position.Should().Be(0);
        paymentMethod.DisplayOn.Should().Be(ReSys.Core.Common.Domain.Concerns.DisplayOn.Both);
        paymentMethod.PublicMetadata.Should().BeEmpty();
        paymentMethod.PrivateMetadata.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldReturnError_WhenNameIsRequiredAndEmpty()
    {
        // Arrange
        var name = "";
        var presentation = "Presentation";
        var type = PaymentMethod.PaymentType.CreditCard;

        // Act
        var result = PaymentMethod.Create(name, presentation, type);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PaymentMethod.Errors.NameRequired.Code);
    }

    [Fact]
    public void Create_ShouldNormalizeNameAndPresentation()
    {
        // Arrange
        var name = "  Padded Name  ";
        var presentation = "  Padded Presentation  ";
        var type = PaymentMethod.PaymentType.CreditCard;

        // Act
        var result = PaymentMethod.Create(name, presentation, type);

        // Assert
        result.IsError.Should().BeFalse();
        var paymentMethod = result.Value;
        paymentMethod.Name.Should().Be(name.Trim().ToSlug());
        paymentMethod.Presentation.Should().Be(presentation.Trim());
    }

    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Arrange
        var name = "New Method";
        var presentation = "New Method Pres";
        var type = PaymentMethod.PaymentType.BankTransfer;

        // Act
        var result = PaymentMethod.Create(name, presentation, type);

        // Assert
        result.IsError.Should().BeFalse();
        var paymentMethod = result.Value;
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Created);
        var createdEvent = paymentMethod.DomainEvents.OfType<PaymentMethod.Events.Created>().Single();
        createdEvent.PaymentMethodId.Should().Be(paymentMethod.Id);
        createdEvent.Name.Should().Be(name.ToSlug());
        createdEvent.Type.Should().Be(type);
    }

    #endregion

    #region Update Test Cases

    [Fact]
    public void Update_ShouldUpdateNameAndPresentation_WhenValid()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();
        var newName = "Updated Name";
        var newPresentation = "Updated Presentation";

        // Act
        var result = paymentMethod.Update(name: newName, presentation: newPresentation);

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.Name.Should().Be(newName.ToSlug());
        paymentMethod.Presentation.Should().Be(newPresentation);
        paymentMethod.UpdatedAt.Should().NotBeNull();
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Updated);
    }

    [Fact]
    public void Update_ShouldUpdateActiveStatus()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(active: true);

        // Act
        var result = paymentMethod.Update(active: false);

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.Active.Should().BeFalse();
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Updated);
    }

    [Fact]
    public void Update_ShouldUpdateAutoCapture()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(autoCapture: false);

        // Act
        var result = paymentMethod.Update(autoCapture: true);

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.AutoCapture.Should().BeTrue();
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Updated);
    }

    [Fact]
    public void Update_ShouldUpdatePosition()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(position: 0);

        // Act
        var result = paymentMethod.Update(position: 5);

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.Position.Should().Be(5);
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Updated);
    }

    [Fact]
    public void Update_ShouldUpdateDisplayOn()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(displayOn: ReSys.Core.Common.Domain.Concerns.DisplayOn.Both);

        // Act
        var result = paymentMethod.Update(displayOn: ReSys.Core.Common.Domain.Concerns.DisplayOn.Storefront);

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.DisplayOn.Should().Be(ReSys.Core.Common.Domain.Concerns.DisplayOn.Storefront);
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Updated);
    }

    [Fact]
    public void Update_ShouldUpdateMetadata_WhenChanged()
    {
        // Arrange
        var initialPublicMetadata = new Dictionary<string, object?> { { "key1", "value1" } };
        var initialPrivateMetadata = new Dictionary<string, object?> { { "secret1", "data1" } };
        var paymentMethod = CreateValidPaymentMethod(publicMetadata: initialPublicMetadata, privateMetadata: initialPrivateMetadata);

        var updatedPublicMetadata = new Dictionary<string, object?> { { "key1", "new_value" }, { "key2", "value2" } };
        var updatedPrivateMetadata = new Dictionary<string, object?> { { "secret1", "new_data" } };

        // Act
        var result = paymentMethod.Update(publicMetadata: updatedPublicMetadata, privateMetadata: updatedPrivateMetadata);

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.PublicMetadata.Should().Contain("key1", "new_value");
        paymentMethod.PublicMetadata.Should().Contain("key2", "value2");
        paymentMethod.PrivateMetadata.Should().Contain("secret1", "new_data");
        paymentMethod.UpdatedAt.Should().NotBeNull();
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Updated);
    }

    [Fact]
    public void Update_ShouldNotUpdateIfNoChanges()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();
        paymentMethod.ClearDomainEvents(); // Clear initial created event
        var initialUpdatedAt = paymentMethod.UpdatedAt;

        // Act
        var result = paymentMethod.Update(name: paymentMethod.Name, presentation: paymentMethod.Presentation, active: paymentMethod.Active);

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.UpdatedAt.Should().Be(initialUpdatedAt); // UpdatedAt should not change
        paymentMethod.DomainEvents.Should().BeEmpty(); // No updated event
    }

    [Fact]
    public void Update_ShouldPublishUpdatedEvent()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();
        paymentMethod.ClearDomainEvents(); // Clear initial created event
        var newName = "New Name";

        // Act
        var result = paymentMethod.Update(name: newName);

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Updated);
    }

    #endregion

    #region Delete & Restore Test Cases

    [Fact]
    public void Delete_ShouldSoftDelete_WhenNoAssociatedPayments()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();

        // Act
        var result = paymentMethod.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.IsDeleted.Should().BeTrue();
        paymentMethod.Active.Should().BeFalse();
        paymentMethod.DeletedAt.Should().NotBeNull();
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Deleted);
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenAssociatedPaymentsExist()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();
        var dummyPayment = CreateDummyPayment(paymentMethod.Id);
        paymentMethod.Payments.Add(dummyPayment);

        // Act
        var result = paymentMethod.Delete();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PaymentMethod.Errors.InUse.Code);
        paymentMethod.IsDeleted.Should().BeFalse(); // Should not be deleted
        paymentMethod.Active.Should().BeTrue();    // Should remain active
    }

    [Fact]
    public void Delete_ShouldPublishDeletedEvent()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();
        paymentMethod.ClearDomainEvents(); // Clear initial created event

        // Act
        paymentMethod.Delete();

        // Assert
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Deleted);
    }

    [Fact]
    public void Restore_ShouldRestoreDeletedMethod()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();
        paymentMethod.Delete(); // Soft delete it first
        paymentMethod.ClearDomainEvents(); // Clear deleted event

        // Act
        var result = paymentMethod.Restore();

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.IsDeleted.Should().BeFalse();
        paymentMethod.Active.Should().BeTrue();
        paymentMethod.DeletedAt.Should().BeNull();
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Restored);
    }

    [Fact]
    public void Restore_ShouldBeIdempotent_WhenNotDeleted()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(); // Not deleted
        paymentMethod.ClearDomainEvents(); // Clear initial created event

        // Act
        var result = paymentMethod.Restore();

        // Assert
        result.IsError.Should().BeFalse();
        paymentMethod.IsDeleted.Should().BeFalse(); // Still not deleted
        paymentMethod.Active.Should().BeTrue();     // Still active
        paymentMethod.DomainEvents.Should().BeEmpty(); // No new event published
    }

    [Fact]
    public void Restore_ShouldPublishRestoredEvent()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();
        paymentMethod.Delete(); // Soft delete it first
        paymentMethod.ClearDomainEvents(); // Clear deleted event

        // Act
        paymentMethod.Restore();

        // Assert
        paymentMethod.DomainEvents.Should().ContainSingle(e => e is PaymentMethod.Events.Restored);
    }

    #endregion

    #region Computed Properties Test Cases

    [Theory]
    [InlineData(PaymentMethod.PaymentType.CreditCard, true)]
    [InlineData(PaymentMethod.PaymentType.DebitCard, true)]
    [InlineData(PaymentMethod.PaymentType.PayPal, false)]
    [InlineData(PaymentMethod.PaymentType.BankTransfer, false)]
    public void IsCardPayment_ShouldReturnCorrectValue(PaymentMethod.PaymentType type, bool expected)
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(type: type);

        // Act & Assert
        paymentMethod.IsCardPayment.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void RequiresManualCapture_ShouldReturnCorrectValue(bool autoCapture, bool expected)
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(autoCapture: autoCapture);

        // Act & Assert
        paymentMethod.RequiresManualCapture.Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentMethod.PaymentType.StoreCredit, false)]
    [InlineData(PaymentMethod.PaymentType.GiftCard, false)]
    [InlineData(PaymentMethod.PaymentType.CreditCard, true)]
    [InlineData(PaymentMethod.PaymentType.PayPal, true)]
    public void SourceRequired_ShouldReturnCorrectValue(PaymentMethod.PaymentType type, bool expected)
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(type: type);

        // Act & Assert
        paymentMethod.SourceRequired.Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentMethod.PaymentType.CreditCard, true)]
    [InlineData(PaymentMethod.PaymentType.Stripe, true)]
    [InlineData(PaymentMethod.PaymentType.ApplePay, true)]
    [InlineData(PaymentMethod.PaymentType.GooglePay, true)]
    [InlineData(PaymentMethod.PaymentType.PayPal, false)]
    [InlineData(PaymentMethod.PaymentType.CashOnDelivery, false)]
    public void SupportsSavedCards_ShouldReturnCorrectValue(PaymentMethod.PaymentType type, bool expected)
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(type: type);

        // Act & Assert
        paymentMethod.SupportsSavedCards.Should().Be(expected);
    }

    [Theory]
    [InlineData(PaymentMethod.PaymentType.CreditCard, "creditcard")]
    [InlineData(PaymentMethod.PaymentType.PayPal, "paypal")]
    [InlineData(PaymentMethod.PaymentType.CashOnDelivery, "cashondelivery")]
    public void MethodCode_ShouldReturnLowercaseStringOfPaymentType(PaymentMethod.PaymentType type, string expected)
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(type: type);

        // Act & Assert
        paymentMethod.MethodCode.Should().Be(expected);
    }

    [Fact]
    public void IsDeleted_ShouldReturnTrueWhenDeletedAtHasValue()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod();
        paymentMethod.Delete(); // Soft delete it

        // Act & Assert
        paymentMethod.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void IsDeleted_ShouldReturnFalseWhenDeletedAtIsNull()
    {
        // Arrange
        var paymentMethod = CreateValidPaymentMethod(); // Not deleted

        // Act & Assert
        paymentMethod.IsDeleted.Should().BeFalse();
    }

    #endregion
}
