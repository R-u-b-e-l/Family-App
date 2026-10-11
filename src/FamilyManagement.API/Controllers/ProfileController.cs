using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class ProfileController(IUserService userService, ICurrentUser currentUser, IWebHostEnvironment env)
    : ApiControllerBase
{
    private const long MaxPictureBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDto>>> Get(CancellationToken ct) =>
        Ok(Success(await userService.GetProfileAsync(ct)));

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(Success(await userService.UpdateProfileAsync(request, ct), "Profile updated"));

    /// <summary>Uploads the signed-in user's profile picture (jpeg/png/webp, max 5 MB).</summary>
    [HttpPost("picture")]
    [RequestSizeLimit(MaxPictureBytes + 1024)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UploadPicture(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("Choose an image file."));
        if (file.Length > MaxPictureBytes)
            return BadRequest(ApiResponse.Fail("The image must be 5 MB or smaller."));
        if (!AllowedTypes.TryGetValue(file.ContentType, out var extension))
            return BadRequest(ApiResponse.Fail("Only JPEG, PNG or WebP images are allowed."));

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", currentUser.FamilyId.ToString("N"));
        Directory.CreateDirectory(folder);

        var fileName = $"{currentUser.UserId:N}_{DateTime.UtcNow.Ticks}{extension}";
        var fullPath = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, ct);

        var relativePath = $"/uploads/{currentUser.FamilyId:N}/{fileName}";
        var (user, oldPath) = await userService.SetProfilePicturePathAsync(relativePath, ct);

        // Best-effort cleanup of the previous picture.
        if (!string.IsNullOrEmpty(oldPath))
        {
            try
            {
                var old = Path.Combine(webRoot, oldPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(old))
                    System.IO.File.Delete(old);
            }
            catch { /* ignore */ }
        }

        return Ok(Success(user, "Profile picture updated"));
    }
}
