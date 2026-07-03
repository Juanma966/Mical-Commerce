namespace Mical.Services.Interfaces;

/// <summary>Resultado de guardar un archivo subido.</summary>
public class FileSaveResult
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }

    /// <summary>Ruta relativa a wwwroot (ej. <c>uploads/products/abc.jpg</c>).</summary>
    public string? RelativePath { get; private init; }

    public static FileSaveResult Success(string relativePath) =>
        new() { Succeeded = true, RelativePath = relativePath };

    public static FileSaveResult Fail(string error) =>
        new() { Succeeded = false, Error = error };
}

/// <summary>
/// Guarda y elimina imágenes de producto en <c>wwwroot/uploads/products</c>,
/// validando extensión, tipo y tamaño, y regenerando el nombre con un GUID.
/// </summary>
public interface IFileStorageService
{
    Task<FileSaveResult> SaveProductImageAsync(IFormFile file);

    /// <summary>Elimina la imagen indicada por su ruta relativa (si existe).</summary>
    void DeleteProductImage(string? relativePath);
}
