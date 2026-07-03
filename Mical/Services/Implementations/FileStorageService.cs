using Mical.Services.Interfaces;

namespace Mical.Services.Implementations;

public class FileStorageService : IFileStorageService
{
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
    private const string ProductsFolder = "uploads/products";

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<FileSaveResult> SaveProductImageAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return FileSaveResult.Fail("El archivo está vacío.");

        if (file.Length > MaxBytes)
            return FileSaveResult.Fail("La imagen supera el tamaño máximo de 2 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return FileSaveResult.Fail("Formato no permitido. Usá JPG, PNG o WEBP.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return FileSaveResult.Fail("El contenido del archivo no es una imagen válida.");

        var folderAbsolute = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folderAbsolute);

        // Nombre regenerado con GUID: evita colisiones y nombres maliciosos.
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var absolutePath = Path.Combine(folderAbsolute, fileName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"{ProductsFolder}/{fileName}";
        _logger.LogInformation("Imagen de producto guardada: {Path}", relativePath);
        return FileSaveResult.Success(relativePath);
    }

    public void DeleteProductImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        // Solo se permite borrar dentro de uploads/products (defensa ante rutas raras).
        if (!relativePath.Replace('\\', '/').StartsWith(ProductsFolder, StringComparison.OrdinalIgnoreCase))
            return;

        var absolutePath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "No se pudo borrar la imagen {Path}", relativePath);
        }
    }
}
