namespace Mical.Entities;

/// <summary>
/// Estados del pedido. Máquina de estados:
/// Pendiente → Pagado → Preparando → Enviado → Entregado, con Cancelado desde
/// cualquier estado (repone stock solo si NO estaba Entregado). Ver Fase 6.3.
/// </summary>
public enum OrderStatus
{
    Pendiente = 1,
    Pagado = 2,
    Preparando = 3,
    Enviado = 4,
    Entregado = 5,
    Cancelado = 6
}
