using FluentAssertions;

using ReSys.Core.Common.Models.Pagination;

namespace Core.UnitTests.Common.Models.Paging;

public sealed class PagingParamsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultValues_AreNull()
    {
        var @params = new PagingParams();

        @params.PageSize.Should().BeNull();
        @params.PageIndex.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithPageSize_SetsPageSize()
    {
        var @params = new PagingParams(PageSize: 10);

        @params.PageSize.Should().Be(10);
        @params.PageIndex.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithPageIndex_SetsPageIndex()
    {
        var @params = new PagingParams(PageIndex: 5);

        @params.PageSize.Should().BeNull();
        @params.PageIndex.Should().Be(5);
    }

    [Fact]
    public void Constructor_WithBothValues_SetsBothValues()
    {
        var @params = new PagingParams(PageSize: 10, PageIndex: 5);

        @params.PageSize.Should().Be(10);
        @params.PageIndex.Should().Be(5);
    }

    [Fact]
    public void Constructor_WithNegativeValues_AllowsNegativeValues()
    {
        var @params = new PagingParams(PageSize: -1, PageIndex: -2);

        @params.PageSize.Should().Be(-1);
        @params.PageIndex.Should().Be(-2);
    }

    #endregion

    #region EffectivePageNumber Tests

    [Fact]
    public void EffectivePageNumber_NullPageIndex_ReturnsOne()
    {
        var @params = new PagingParams();

        @params.EffectivePageNumber().Should().Be(1);
    }

    [Fact]
    public void EffectivePageNumber_ZeroPageIndex_ReturnsOne()
    {
        var @params = new PagingParams(PageIndex: 0);

        @params.EffectivePageNumber().Should().Be(1);
    }

    [Fact]
    public void EffectivePageNumber_PositivePageIndex_ReturnsPageIndexPlusOne()
    {
        var @params = new PagingParams(PageIndex: 5);

        @params.EffectivePageNumber().Should().Be(6);
    }

    [Fact]
    public void EffectivePageNumber_NegativePageIndex_ReturnsOne()
    {
        var @params = new PagingParams(PageIndex: -5);

        @params.EffectivePageNumber().Should().Be(1);
    }

    #endregion

    #region EffectivePageIndex Tests

    [Fact]
    public void EffectivePageIndex_NullPageIndex_ReturnsZero()
    {
        var @params = new PagingParams();

        @params.EffectivePageIndex().Should().Be(0);
    }

    [Fact]
    public void EffectivePageIndex_ZeroPageIndex_ReturnsZero()
    {
        var @params = new PagingParams(PageIndex: 0);

        @params.EffectivePageIndex().Should().Be(0);
    }

    [Fact]
    public void EffectivePageIndex_PositivePageIndex_ReturnsPageIndex()
    {
        var @params = new PagingParams(PageIndex: 5);

        @params.EffectivePageIndex().Should().Be(5);
    }

    [Fact]
    public void EffectivePageIndex_NegativePageIndex_ReturnsZero()
    {
        var @params = new PagingParams(PageIndex: -5);

        @params.EffectivePageIndex().Should().Be(0);
    }

    #endregion

    #region HasPagingValues Tests

    [Fact]
    public void HasPagingValues_NullParams_ReturnsFalse()
    {
        PagingParams? @params = null;
        @params.HasPagingValues().Should().BeFalse();
    }

    [Fact]
    public void HasPagingValues_EmptyParams_ReturnsFalse()
    {
        var @params = new PagingParams();
        @params.HasPagingValues().Should().BeFalse();
    }

    [Fact]
    public void HasPagingValues_WithPageSize_ReturnsTrue()
    {
        var @params = new PagingParams(PageSize: 10);
        @params.HasPagingValues().Should().BeTrue();
    }

    [Fact]
    public void HasPagingValues_WithPageIndex_ReturnsTrue()
    {
        var @params = new PagingParams(PageIndex: 0);
        @params.HasPagingValues().Should().BeTrue();
    }

    [Fact]
    public void HasPagingValues_WithBothValues_ReturnsTrue()
    {
        var @params = new PagingParams(PageSize: 10, PageIndex: 5);
        @params.HasPagingValues().Should().BeTrue();
    }

    [Fact]
    public void HasPagingValues_WithNegativePageSize_ReturnsTrue()
    {
        var @params = new PagingParams(PageSize: -1);
        @params.HasPagingValues().Should().BeTrue();
    }

    [Fact]
    public void HasPagingValues_WithNegativePageIndex_ReturnsTrue()
    {
        var @params = new PagingParams(PageIndex: -1);
        @params.HasPagingValues().Should().BeTrue();
    }

    #endregion
}