using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Shop.BLL.Response_VM;

namespace Shop.Api.Controllers.Admin;

/// <summary>
/// Admin image upload endpoint. Accepts a single image file (max 5 MB), stores
/// it under <c>wwwroot/uploads</c> with a generated unique name, and returns the
/// relative path (<c>/uploads/&lt;file&gt;</c>) that the static-files middleware
/// serves. JWT-protected under the <c>api/admin/uploads</c> route.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/uploads")]
public class UploadsController : ControllerBase
{
    /// <summary>Maximum accepted upload size: 5 MB.</summary>
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Creates a new <see cref="UploadsController"/>.
    /// </summary>
    /// <param name="env">Host environment, used to resolve the content root.</param>
    public UploadsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Accepts a single image file and stores it under <c>wwwroot/uploads</c>.
    /// Rejects non-image content types and files larger than 5 MB with a 400.
    /// </summary>
    /// <param name="file">The uploaded image file (multipart/form-data field <c>file</c>).</param>
    [HttpPost]
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

        // Only images are allowed — reject anything whose content type is not image/*.
        if (string.IsNullOrWhiteSpace(file.ContentType)
            || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<string>.Fail("Only image files are allowed.", 400));
        }

        // Store under <content-root>/wwwroot/uploads, creating the folder on first use.
        string uploadsRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsRoot);

        // Generate a unique, extension-preserving filename to avoid collisions.
        string extension = Path.GetExtension(file.FileName);
        string fileName = Guid.NewGuid().ToString("N") + extension;
        string fullPath = Path.Combine(uploadsRoot, fileName);

        await using (FileStream stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        // Return the public, static-served relative path.
        string relativePath = "/uploads/" + fileName;
        return Ok(ApiResponse<string>.Ok(relativePath));
    }
}
