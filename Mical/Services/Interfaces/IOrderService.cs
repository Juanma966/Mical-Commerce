using Mical.Areas.Admin.Models;
using Mical.Entities;
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

    /// <summary>Historial "Mis pedidos" del usuario, más nuevos primero.</summary>
    Task<IReadOnlyList<OrderHistoryVm>> GetHistoryForUserAsync(string userId);

    // ----- Admin -----

    Task<IReadOnlyList<AdminOrderListItemVm>> GetAllForAdminAsync();

    Task<AdminOrderDetailVm?> GetForAdminAsync(int orderId);

    /// <summary>
    /// Cambia el estado según la máquina de estados. Al cancelar, repone stock
    /// solo si el estado previo NO era Entregado. Registra auditoría.
    /// </summary>
    Task<OperationResult> UpdateStatusAsync(int orderId, OrderStatus newStatus, string adminUserName);
}
