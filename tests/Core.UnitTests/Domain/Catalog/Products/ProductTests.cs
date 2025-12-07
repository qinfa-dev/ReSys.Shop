using FluentAssertions;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Catalog.Products.Classifications;

using static ReSys.Core.Domain.Catalog.Products.Images.ProductImage;
using static ReSys.Core.Domain.Catalog.Products.Product;

namespace Core.UnitTests.Domain.Catalog.Products;

public class ProductTests
{
    // Helper method to create a valid Product instance for tests
    private static Product CreateValidProduct(string name = "Test Product", string slug = "test-product")
    {
        var result = Product.Create(name: name, slug: slug);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    [Fact]
    public void Product_Create_ShouldReturnProduct_WhenValidParameters()
    {
        // Arrange
        var name = "Test Product";
        var presentation = "Test Product";
        var description = "This is a test product.";
        var slug = "test-product";
        var isDigital = true;

        // Act
        var result = Product.Create(name: name, presentation: presentation, description: description, slug: slug, isDigital: isDigital);

        // Assert
        result.IsError.Should().BeFalse();
        var product = result.Value;
        product.Should().NotBeNull();
        product.Name.Should().Be(expected: "test-product");
        product.Presentation.Should().Be(expected: "Test Product"); // Default presentation to Humanized name
        product.Description.Should().Be(expected: "This is a test product.");
        product.Slug.Should().Be(expected: "test-product");
        product.IsDigital.Should().Be(expected: true);
        product.Status.Should().Be(expected: ProductStatus.Draft);
        product.Variants.Should().ContainSingle(predicate: v => v.IsMaster);
        product.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Created);
    }

    [Theory]
    [InlineData(data: "")]
    [InlineData(data: " ")]
    public void Product_Create_ShouldReturnNameRequiredError_WhenNameIsNullOrEmpty(string invalidName)
    {
        // Arrange
        var description = "This is a test product.";
        var slug = "test-product";

        // Act
        var result = Product.Create(name: invalidName, presentation: description, description: slug);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Product.Errors.NameRequired);
    }

    [Fact]
    public void Product_Update_ShouldUpdateNameAndRaiseEvent_WhenValidNameProvided()
    {
        // Arrange
        var initialName = "Initial Product Name";
        var initialSlug = "initial-product-name";
        var createResult = Product.Create(name: initialName, slug: initialSlug);
        createResult.IsError.Should().BeFalse();
        var product = createResult.Value;
        product.ClearDomainEvents(); // Clear initial creation events

        // Assert initial state
        product.Name.Should().Be(expected: "initial-product-name");
        product.Slug.Should().Be(expected: "initial-product-name");

        var newName = "Updated Product Name"; // Original cased name

        // Act
        var result = product.Update(name: newName);

        // Assert final state
        result.IsError.Should().BeFalse();
        product.Name.Should().Be(expected: "updated-product-name"); // Name should be slugified
        product.Presentation.Should().Be(expected: newName); // Presentation should retain original cased name if not provided separately and derived from name
        product.Slug.Should().Be(expected: "updated-product-name");
        product.UpdatedAt.Should().NotBeNull();
        product.DomainEvents.Should().ContainSingle(predicate: e => e is Events.ProductUpdated);
    }
    [Fact]
    public void Product_Activate_ShouldChangeStatusToActiveAndRaiseEvent_WhenDraft()
    {
        // Arrange
        var product = CreateValidProduct(); // Starts as Draft
        product.ClearDomainEvents();

        // Act
        var result = product.Activate();

        // Assert
        result.IsError.Should().BeFalse();
        product.Status.Should().Be(expected: ProductStatus.Active);
        product.UpdatedAt.Should().NotBeNull();
        product.DomainEvents.Should().ContainSingle(predicate: e => e is Events.ProductActivated);
    }

    [Fact]
    public void Product_Activate_ShouldNotChangeStatusOrRaiseEvent_WhenAlreadyActive()
    {
        // Arrange
        var product = CreateValidProduct();
        product.Activate(); // Make it active first
        product.ClearDomainEvents(); // Clear previous events

        // Act
        var result = product.Activate();

        // Assert
        result.IsError.Should().BeFalse();
        product.Status.Should().Be(expected: ProductStatus.Active);
        product.DomainEvents.Should().BeEmpty(); // No new events should be raised
    }

    [Fact]
    public void Product_RemoveImage_ShouldRemoveImageAndRaiseEvent_WhenImageExists()
    {
        // Arrange
        var product = CreateValidProduct();
        var image = ProductImage.Create(url: "http://example.com/image.jpg", productId: product.Id, alt: "Test Image", position: 1, type: nameof(ProductImageType.Default)).Value;
        product.AddImage(asset: image);
        product.ClearDomainEvents();

        // Act
        var result = product.RemoveImage(assetId: image.Id);

        // Assert
        result.IsError.Should().BeFalse();
        product.Images.Should().BeEmpty();
        product.UpdatedAt.Should().NotBeNull();
        product.DomainEvents.Should().ContainSingle(predicate: e => e is Events.ProductImageRemoved);
    }

    [Fact]
    public void Product_RemoveImage_ShouldReturnNotFound_WhenImageDoesNotExist()
    {
        // Arrange
        var product = CreateValidProduct();
        var nonExistentImageId = Guid.NewGuid();
        product.ClearDomainEvents();

        // Act
        var result = product.RemoveImage(assetId: nonExistentImageId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: ProductImage.Errors.NotFound(id: nonExistentImageId));
        product.DomainEvents.Should().BeEmpty();
        }
        [Fact]
        public void Product_Archive_ShouldChangeStatusToArchivedAndRaiseEvent_WhenActive()
        {
            // Arrange
            var product = CreateValidProduct();
            product.Activate(); // Make it active first
            product.ClearDomainEvents();
    
            // Act
            var result = product.Archive();
    
            // Assert
            result.IsError.Should().BeFalse();
            product.Status.Should().Be(expected: ProductStatus.Archived);
            product.UpdatedAt.Should().NotBeNull();
            product.DomainEvents.Should().ContainSingle(predicate: e => e is Events.ProductArchived);
        }
    
        [Fact]
        public void Product_Archive_ShouldNotChangeStatusOrRaiseEvent_WhenAlreadyArchived()
        {
            // Arrange
            var product = CreateValidProduct();
            product.Archive(); // Make it archived first
            product.ClearDomainEvents();
    
            // Act
            var result = product.Archive();
    
            // Assert
            result.IsError.Should().BeFalse();
                    product.Status.Should().Be(expected: ProductStatus.Archived);
                    product.DomainEvents.Should().BeEmpty();
                }
                [Fact]
                public void Product_Draft_ShouldChangeStatusToDraftAndRaiseEvent_WhenActive()
                {
                    // Arrange
                    var product = CreateValidProduct();
                    product.Activate(); // Make it active first
                    product.ClearDomainEvents();
            
                    // Act
                    var result = product.Draft();
            
                    // Assert
                    result.IsError.Should().BeFalse();
                    product.Status.Should().Be(expected: ProductStatus.Draft);
                    product.UpdatedAt.Should().NotBeNull();
                    product.DomainEvents.Should().ContainSingle(predicate: e => e is Events.ProductDrafted);
                }
            
                [Fact]
                public void Product_Draft_ShouldNotChangeStatusOrRaiseEvent_WhenAlreadyDraft()
                {
                    // Arrange
                    var product = CreateValidProduct(); // Already in draft
                    product.ClearDomainEvents();
            
                    // Act
                    var result = product.Draft();
            
                    // Assert
                    result.IsError.Should().BeFalse();
                            product.Status.Should().Be(expected: ProductStatus.Draft);
                            product.DomainEvents.Should().BeEmpty();
                        }
                    
                        [Fact]
                        public void Product_AddVariant_ShouldAddVariantAndRaiseEvent_WhenValidVariantProvided()
                        {
                            // Arrange
                            var product = CreateValidProduct();
                            var variant = ReSys.Core.Domain.Catalog.Products.Variants.Variant.Create(productId: product.Id, sku: "VAR-001").Value;
                            product.ClearDomainEvents();
                    
                            // Act
                            var result = product.AddVariant(variant: variant);
                    
                            // Assert
                            result.IsError.Should().BeFalse();
                            product.Variants.Should().Contain(expected: variant);
                            product.Variants.Should().HaveCount(expected: 2); // Master + new variant
                            product.UpdatedAt.Should().NotBeNull();
                            product.DomainEvents.Should().ContainSingle(predicate: e => e is Events.VariantAdded);
                        }
                    
                        [Fact]
                        public void Product_AddVariant_ShouldReturnError_WhenNullVariantProvided()
                        {
                            // Arrange
                            var product = CreateValidProduct();
                            product.ClearDomainEvents();
                    
                            // Act
                            var result = product.AddVariant(variant: null);
                    
                            // Assert
                            result.IsError.Should().BeTrue();
                                    result.FirstError.Code.Should().Be(expected: "Product.InvalidVariant");
                                    product.DomainEvents.Should().BeEmpty();
                                }
                                
                                [Fact]
                                public void Product_AddClassification_ShouldAddClassificationAndRaiseEvent_WhenValidClassificationProvided()
                                {
                                    // Arrange
                                    var product = CreateValidProduct();
                                    var taxonId = Guid.NewGuid();
                                    var classification = ReSys.Core.Domain.Catalog.Products.Classifications.Classification.Create(productId: product.Id, taxonId: taxonId).Value;
                                    product.ClearDomainEvents();
                            
                                    // Act
                                    var result = product.AddClassification(classification: classification);
                            
                                    // Assert
                                    result.IsError.Should().BeFalse();
                                    product.Classifications.Should().Contain(expected: classification);
                                    product.DomainEvents.Should().ContainSingle(predicate: e => e is Events.ProductCategoryAdded);
                                }
                            
                                [Fact]
                                public void Product_AddClassification_ShouldReturnError_WhenNullClassificationProvided()
                                {
                                    // Arrange
                                    var product = CreateValidProduct();
                                    product.ClearDomainEvents();
                            
                                    // Act
                                    var result = product.AddClassification(classification: null);
                            
                                    // Assert
                                    result.IsError.Should().BeTrue();
                                    result.FirstError.Code.Should().Be(expected: "Classifications.Validation.Null");
                                    product.DomainEvents.Should().BeEmpty();
                                }
                            
                                [Fact]
                                public void Product_AddClassification_ShouldReturnError_WhenDuplicateClassificationProvided()
                                {
                                    // Arrange
                                    var product = CreateValidProduct();
                                    var taxonId = Guid.NewGuid();
                                    var classification = ReSys.Core.Domain.Catalog.Products.Classifications.Classification.Create(productId: product.Id, taxonId: taxonId).Value;
                                    product.AddClassification(classification: classification); // Add once
                                    product.ClearDomainEvents();
                            
                                    // Act
                                    var result = product.AddClassification(classification: classification); // Add again
                            
                                    // Assert
                                    result.IsError.Should().BeTrue();
                                    result.FirstError.Code.Should().Be(expected: "Classification.AlreadyLinked");
                                    product.DomainEvents.Should().BeEmpty();
                                }
                            }
                            
