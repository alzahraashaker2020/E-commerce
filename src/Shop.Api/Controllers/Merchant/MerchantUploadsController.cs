using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.Response_VM;
using SkiaSharp;

namespace Shop.Api.Controllers.Merchant;

/// <summary>
/// Merchant image upload endpoint. Accepts a single image file (max 5 MB), stores
/// it under <c>wwwroot/uploads</c> with a generated unique name, and returns the
/// relative path (<c>/uploads/&lt;file&gt;</c>) that the static-files middleware
/// serves. JWT-protected under the <c>api/merchant/uploads</c> route.
/// </summary>
[ApiController]
[Authorize(Roles = "Merchant")]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ImageProcessing")]
[Route("api/merchant/uploads")]
public class MerchantUploadsController : ControllerBase
{
    /// <summary>Maximum accepted upload size: 5 MB.</summary>
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Creates a new <see cref="UploadsController"/>.
    /// </summary>
    /// <param name="env">Host environment, used to resolve the content root.</param>
    private readonly Shop.Api.Tenancy.StoreScope _store;
    public MerchantUploadsController(IWebHostEnvironment env, Shop.Api.Tenancy.StoreScope store)
    {
        _env = env;
        _store = store;
    }

    /// <summary>
    /// Accepts a single image file and stores it under <c>wwwroot/uploads</c>.
    /// Rejects non-image content types and files larger than 5 MB with a 400.
    /// </summary>
    /// <param name="file">The uploaded image file (multipart/form-data field <c>file</c>).</param>
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<string>>> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<string>.Fail("No file was uploaded.", 400));
        }

        if (file.Length > MaxBytes)
        {
            return BadRequest(ApiResponse<string>.Fail("The file exceeds the 5 MB maximum.", 400));
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string contentType = file.ContentType?.ToLowerInvariant() ?? "";
        var expected = extension switch { ".png" => ("image/png", SKEncodedImageFormat.Png),
            ".jpg" or ".jpeg" => ("image/jpeg", SKEncodedImageFormat.Jpeg),
            ".webp" => ("image/webp", SKEncodedImageFormat.Webp), _ => ("", (SKEncodedImageFormat)(-1)) };
        if (expected.Item1 == "" || contentType != expected.Item1)
            return BadRequest(ApiResponse<string>.Fail("Upload a PNG, JPEG or WebP image with its matching file extension and content type.", 400));
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        if (buffer.Length > MaxBytes) return BadRequest(ApiResponse<string>.Fail("The file exceeds the 5 MB maximum.", 400));
        buffer.Position = 0;
        using var codec = SKCodec.Create(buffer);
        if (codec is null || codec.EncodedFormat != expected.Item2 || codec.FrameCount > 1 ||
            codec.Info.Width < 1 || codec.Info.Height < 1 || codec.Info.Width > 8192 || codec.Info.Height > 8192 ||
            (long)codec.Info.Width * codec.Info.Height > 16_000_000)
            return BadRequest(ApiResponse<string>.Fail("Invalid image, animated image, or image larger than 16 megapixels / 8192 pixels per side.", 400));
        using var bitmap = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            return BadRequest(ApiResponse<string>.Fail("The image is incomplete or corrupt.", 400));
        using var oriented = Orient(bitmap, codec.EncodedOrigin);
        using var image = SKImage.FromBitmap(oriented);
        using var clean = image.Encode(SKEncodedImageFormat.Webp, 90);
        if (clean is null || clean.Size > 10 * 1024 * 1024)
            return BadRequest(ApiResponse<string>.Fail("The image cannot be safely processed within the allowed size.", 400));

        // Store under <content-root>/wwwroot/uploads, creating the folder on first use.
        string uploadsRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "stores", _store.StoreId);
        Directory.CreateDirectory(uploadsRoot);

        // Generate a unique, extension-preserving filename to avoid collisions.
        string fileName = Guid.NewGuid().ToString("N") + ".webp";
        string fullPath = Path.Combine(uploadsRoot, fileName);

        await using (FileStream stream = System.IO.File.Create(fullPath))
        {
            clean.SaveTo(stream);
        }

        // Return the public, static-served relative path.
        string relativePath = "/uploads/stores/" + _store.StoreId + "/" + fileName;
        return Ok(ApiResponse<string>.Ok(relativePath));
    }

    private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
    {
        int w = source.Width, h = source.Height;
        bool swap = (int)origin >= 5;
        var output = new SKBitmap(swap ? h : w, swap ? w : h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix { ScaleX=-1,ScaleY=1,TransX=w,Persp2=1 },
            SKEncodedOrigin.BottomRight => new SKMatrix { ScaleX=-1,ScaleY=-1,TransX=w,TransY=h,Persp2=1 },
            SKEncodedOrigin.BottomLeft => new SKMatrix { ScaleX=1,ScaleY=-1,TransY=h,Persp2=1 },
            SKEncodedOrigin.LeftTop => new SKMatrix { SkewX=1,SkewY=1,Persp2=1 },
            SKEncodedOrigin.RightTop => new SKMatrix { SkewX=-1,SkewY=1,TransX=h,Persp2=1 },
            SKEncodedOrigin.RightBottom => new SKMatrix { SkewX=-1,SkewY=-1,TransX=h,TransY=w,Persp2=1 },
            SKEncodedOrigin.LeftBottom => new SKMatrix { SkewX=1,SkewY=-1,TransY=w,Persp2=1 },
            _ => SKMatrix.Identity
        };
        canvas.SetMatrix(matrix); canvas.DrawBitmap(source,0,0); canvas.Flush();
        return output;
    }
}
