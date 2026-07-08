using Mical.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Mical.Services.Implementations;

public class FileStorageService : IFileStorageService
{
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
    private const string UploadsRoot = "uploads";
    private const string ProductsFolder = "products";
    // Toda imagen se guarda como WebP; se reduce si excede este ancho.
    private const int MaxWidth = 1600;
    private const int WebpQuality = 80;

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public Task<FileSaveResult> SaveProductImageAsync(IFormFile file) =>
        SaveImageAsync(file, ProductsFolder);

    public void DeleteProductImage(string? relativePath) => DeleteImage(relativePath);

    public async Task<FileSaveResult> SaveImageAsync(IFormFile file, string subfolder)
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

        // Normaliza la subcarpeta (evita rutas fuera de uploads/).
        subfolder = string.IsNullOrWhiteSpace(subfolder) ? "misc" : Path.GetFileName(subfolder);

        var folderAbsolute = Path.Combine(_env.WebRootPath, UploadsRoot, subfolder);
        Directory.CreateDirectory(folderAbsolute);

        // Nombre regenerado con GUID: evita colisiones y nombres maliciosos.
        // Toda imagen se persiste como WebP (mejor compresión).
        var fileName = $"{Guid.NewGuid():N}.webp";
        var absolutePath = Path.Combine(folderAbsolute, fileName);

        try
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input);

            // Reduce solo si supera el ancho máximo (nunca agranda).
            if (image.Width > MaxWidth)
                image.Mutate(x => x.Resize(MaxWidth, 0));

            await image.SaveAsWebpAsync(absolutePath, new WebpEncoder { Quality = WebpQuality });
        }
        catch (Exception ex) when (ex is not IOException)
        {
            // Si el contenido no es una imagen decodificable pese al content-type.
            _logger.LogWarning(ex, "No se pudo procesar la imagen subida.");
            return FileSaveResult.Fail("No pudimos procesar la imagen. Probá con otro archivo.");
        }

        var relativePath = $"{UploadsRoot}/{subfolder}/{fileName}";
        _logger.LogInformation("Imagen guardada como WebP: {Path}", relativePath);
        return FileSaveResult.Success(relativePath);
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        // Solo se permite borrar dentro de uploads/ (defensa ante rutas raras).
        if (!relativePath.Replace('\\', '/').StartsWith(UploadsRoot + "/", StringComparison.OrdinalIgnoreCase))
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
