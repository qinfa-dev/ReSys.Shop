using FluentAssertions;

using ReSys.Core.Domain.Catalog.Taxonomies.Images;

namespace Core.UnitTests.Domain.Catalog.Taxonomies.Images;

public class TaxonImageTests
{
    // Helper method to create a valid TaxonImage instance (defined here for internal tests)
    private static TaxonImage CreateValidTaxonImage(Guid taxonId, string type = "default", string url = "http://example.com/image.jpg")
    {
        var result = TaxonImage.Create(taxonId: taxonId, type: type, url: url);
        result.IsError.Should().BeFalse();
        return result.Value;
    }
    [Fact]
    public void TaxonImage_Create_ShouldReturnTaxonImage_WhenValidParameters()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var type = "default";
        var url = "http://example.com/image.jpg";
        var alt = "Default Image";
        var position = 1;
        var size = 1024;
        var width = 100;
        var height = 200;

        // Act
        var result = TaxonImage.Create(taxonId: taxonId, type: type, url: url, alt: alt, position: position, size: size, width: width, height: height);

        // Assert
        result.IsError.Should().BeFalse();
        var image = result.Value;
        image.Should().NotBeNull();
        image.TaxonId.Should().Be(expected: taxonId);
        image.Type.Should().Be(expected: type);
        image.Url.Should().Be(expected: url);
        image.Alt.Should().Be(expected: alt);
        image.Position.Should().Be(expected: position);
        image.PublicMetadata.Should().ContainKey(expected: "size").WhoseValue.Should().Be(expected: size.ToString());
        image.PublicMetadata.Should().ContainKey(expected: "width").WhoseValue.Should().Be(expected: width.ToString());
        image.PublicMetadata.Should().ContainKey(expected: "height").WhoseValue.Should().Be(expected: height.ToString());
    }

    [Fact]
    public void TaxonImage_Update_ShouldUpdateProperties()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var image = CreateValidTaxonImage(taxonId: taxonId, type: "default", url: "http://example.com/old.jpg");

        var newType = "square";
        var newUrl = "http://example.com/new.jpg";
        var newAlt = "New Alt Text";
        var newSize = 2048;
        var newWidth = 200;
        var newHeight = 400;

        // Act
        var result = image.Update(type: newType, url: newUrl, alt: newAlt, size: newSize, width: newWidth, height: newHeight);

        // Assert
        result.IsError.Should().BeFalse();
        image.Type.Should().Be(expected: newType);
        image.Url.Should().Be(expected: newUrl);
        image.Alt.Should().Be(expected: newAlt);
        image.PublicMetadata.Should().ContainKey(expected: "size").WhoseValue.Should().Be(expected: newSize.ToString());
        image.PublicMetadata.Should().ContainKey(expected: "width").WhoseValue.Should().Be(expected: newWidth.ToString());
        image.PublicMetadata.Should().ContainKey(expected: "height").WhoseValue.Should().Be(expected: newHeight.ToString());
        image.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void TaxonImage_Update_ShouldNotChangeIfNoNewValues()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var image = CreateValidTaxonImage(taxonId: taxonId, type: "default", url: "http://example.com/old.jpg");

        var initialUpdatedAt = image.UpdatedAt; // Should be null initially for new object

        // Act
        var result = image.Update(type: "default", url: "http://example.com/old.jpg"); // Same values

        // Assert
        result.IsError.Should().BeFalse();
        image.UpdatedAt.Should().Be(expected: initialUpdatedAt); // UpdatedAt should not change
    }
}
