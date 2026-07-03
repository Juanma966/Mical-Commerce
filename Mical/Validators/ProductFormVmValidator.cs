using FluentValidation;
using Mical.Areas.Admin.Models;

namespace Mical.Validators;

/// <summary>
/// Reglas de negocio del formulario de producto. FluentValidation las ejecuta
/// automáticamente en el binding y las vuelca a ModelState.
/// </summary>
public class ProductFormVmValidator : AbstractValidator<ProductFormVm>
{
    public ProductFormVmValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("Máximo 150 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("La descripción es demasiado larga.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Elegí una categoría.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");

        When(x => x.SalePrice.HasValue, () =>
        {
            RuleFor(x => x.SalePrice!.Value)
                .GreaterThan(0).WithMessage("El precio de oferta debe ser mayor a 0.")
                .LessThan(x => x.Price).WithMessage("La oferta debe ser menor al precio de lista.")
                // Sin esto el error se registra bajo la clave "Value" y no lo ve asp-validation-for="SalePrice".
                .OverridePropertyName(nameof(ProductFormVm.SalePrice));
        });

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");

        RuleFor(x => x.MinStock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.");
    }
}
