using Mical.Models;
using Mical.ViewModels;

namespace Mical.Services.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Crea el pedido en una transacción: re-valida stock/precio en el servidor,
    /// descuenta stock con concurrencia optimista (anti-sobreventa) y devuelve el Id.
    /// </summary>
    Task<OperationResult<int>> CheckoutAsync(string userId, CheckoutVm model);

    /// <summary>Detalle de un pedido, solo si pertenece al usuario indicado.</summary>
    Task<OrderDetailVm?> GetForUserAsync(int orderId, string userId);
}
