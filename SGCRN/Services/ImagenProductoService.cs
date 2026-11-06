using System.ComponentModel.DataAnnotations;
using SkiaSharp;

namespace SGCRN.Services;

public sealed class ImagenProductoService(IWebHostEnvironment environment)
{
    private const string Prefix = "/uploads/productos/";
    public static bool EsLocal(string? url) => url is not null &&
        System.Text.RegularExpressions.Regex.IsMatch(url, @"^/uploads/productos/[a-f0-9]{32}\.png$");

    public async Task<string> GuardarAsync(IFormFile archivo)
    {
        if (archivo.Length is <= 0 or > 2097152)
            throw new ValidationException("Selecciona una imagen de hasta 2 MB.");
        await using var stream = archivo.OpenReadStream();
        using var codec = SKCodec.Create(stream);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new ValidationException("El archivo no es una imagen JPG, PNG o WebP válida.");
        var info = codec.Info;
        if (info.Width > 6000 || info.Height > 6000 || (long)info.Width * info.Height > 16000000)
            throw new ValidationException("La imagen debe tener hasta 6000 px por lado y 16 megapíxeles.");
        using var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new ValidationException("La imagen está dañada. Selecciona otro archivo.");

        // Normalize camera orientation, cap dimensions and re-encode pixels without source metadata.
        var swapped = (int)codec.EncodedOrigin >= 5;
        var width = swapped ? info.Height : info.Width;
        var height = swapped ? info.Width : info.Height;
        var scale = Math.Min(1f, 1600f / Math.Max(width, height));
        using var output = new SKBitmap(Math.Max(1, (int)(width * scale)), Math.Max(1, (int)(height * scale)));
        using (var canvas = new SKCanvas(output))
        {
            canvas.Clear(SKColors.Transparent);
            var w = info.Width;
            var h = info.Height;
            (float a, float b, float c, float d, float e, float f) = (int)codec.EncodedOrigin switch
            {
                2 => (-1, 0, 0, 1, w, 0), 3 => (-1, 0, 0, -1, w, h),
                4 => (1, 0, 0, -1, 0, h), 5 => (0, 1, 1, 0, 0, 0),
                6 => (0, 1, -1, 0, h, 0), 7 => (0, -1, -1, 0, h, w),
                8 => (0, -1, 1, 0, 0, w), _ => (1, 0, 0, 1, 0, 0)
            };
            canvas.SetMatrix(new SKMatrix { ScaleX = a * scale, SkewX = c * scale, TransX = e * scale,
                SkewY = b * scale, ScaleY = d * scale, TransY = f * scale, Persp2 = 1 });
            canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        }
        using var image = SKImage.FromBitmap(output);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var name = Guid.NewGuid().ToString("N") + ".png";
        var directory = Path.Combine(environment.WebRootPath, "uploads", "productos");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        try { await File.WriteAllBytesAsync(path, encoded.ToArray()); }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
        return Prefix + name;
    }

    // Solo se elimina una carga nueva cuando falla su registro, nunca imágenes de otros productos.
    public void Descartar(string? url)
    {
        if (EsLocal(url)) File.Delete(Path.Combine(environment.WebRootPath, "uploads", "productos", Path.GetFileName(url!)));
    }
}
