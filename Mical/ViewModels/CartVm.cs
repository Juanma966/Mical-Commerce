namespace Mical.ViewModels;

/// <summary>Ítem que envía el cliente (desde LocalStorage). Solo id y cantidad; nunca precio.</summary>
public class CartItemInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>Línea del carrito ya validada y con precio resuelto en el servidor.</summary>
public class CartLineVm
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }

    /// <summary>Precio unitario efectivo (oferta si aplica), autoritativo del servidor.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Cantidad efectiva (recortada al stock disponible).</summary>
    public int Quantity { get; set; }

    /// <summary>Cantidad que pidió el cliente (para avisar si se ajustó).</summary>
    public int RequestedQuantity { get; set; }

    public int AvailableStock { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>false si el producto ya no existe, está inactivo o su categoría está inactiva.</summary>
    public bool Available { get; set; }
}

/// <summary>Carrito re-hidratado: líneas validadas + total calculado en el servidor.</summary>
public class CartVm
{
    public List<CartLineVm> Lines { get; set; } = new();
    public decimal Total { get; set; }
    public int ItemCount { get; set; }

    /// <summary>true si alguna línea se ajustó, quedó sin stock o dejó de estar disponible.</summary>
    public bool HasIssues { get; set; }
}
