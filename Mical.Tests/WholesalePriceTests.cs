using Mical.Areas.Admin.Models;
using Mical.Validators;

namespace Mical.Tests;

/// <summary>
/// The wholesale price is mandatory when loading a product, and it is shown to
/// customers, so an empty or absurd value must never reach the catalog.
/// </summary>
public class WholesalePriceValidationTests
{
    private static ProductFormVm ValidForm(decimal? wholesalePrice) => new()
    {
        Name = "Taza personalizada",
        CategoryId = 1,
        Price = 1000m,
        Stock = 5,
        MinStock = 0,
        WholesalePrice = wholesalePrice
    };

    private static readonly ProductFormVmValidator Validator = new();

    [Fact]
    public void A_product_without_a_wholesale_price_is_rejected()
    {
        var result = Validator.Validate(ValidForm(null));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors, e => e.PropertyName == nameof(ProductFormVm.WholesalePrice));
        Assert.Equal("El precio mayorista es obligatorio.", error.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999.99)]
    public void A_wholesale_price_of_zero_or_less_is_rejected(decimal wholesalePrice)
    {
        var result = Validator.Validate(ValidForm(wholesalePrice));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProductFormVm.WholesalePrice));
    }

    /// <summary>
    /// The error must land on the WholesalePrice key, not on "Value" — otherwise the
    /// view's asp-validation-for never renders it and the field fails silently.
    /// This is the same trap that bit SalePrice in phase 3.
    /// </summary>
    [Fact]
    public void The_error_is_reported_under_the_field_the_view_binds_to()
    {
        var result = Validator.Validate(ValidForm(0));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProductFormVm.WholesalePrice));
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Value");
    }

    [Fact]
    public void A_positive_wholesale_price_passes()
    {
        var result = Validator.Validate(ValidForm(650m));

        Assert.True(result.IsValid, string.Join(" | ", result.Errors.Select(e => e.ErrorMessage)));
    }

    /// <summary>
    /// No relationship with the retail price is enforced: a wholesale unit price can
    /// be lower, and a "price per batch" can legitimately be higher. Pinned so the
    /// absence of that rule is a decision, not an oversight.
    /// </summary>
    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void The_wholesale_price_is_not_constrained_against_the_retail_price(decimal wholesalePrice)
    {
        var result = Validator.Validate(ValidForm(wholesalePrice));

        Assert.True(result.IsValid, string.Join(" | ", result.Errors.Select(e => e.ErrorMessage)));
    }
}
