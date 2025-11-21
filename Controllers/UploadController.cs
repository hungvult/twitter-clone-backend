using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwitterClone.Api.Models.DTOs;

namespace TwitterClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] AllowedImageExtensions = { ".apng", ".avif", ".gif", ".jpg", ".jpeg", ".png", ".svg", ".webp" };
    private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
    private const long MaxImageSize = 20 * 1024 * 1024; // 20MB
    private const long MaxVideoSize = 50 * 1024 * 1024; // 50MB

    public UploadController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    [HttpPost("images")]
    public async Task<ActionResult<object>> UploadImages([FromForm] IFormFileCollection files)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (files == null || files.Count == 0)
                return BadRequest(new ErrorResponse
                {
                    Code = "NO_FILES",
                    Message = "No files provided"
                });

            if (files.Count > 4)
                return BadRequest(new ErrorResponse
                {
                    Code = "TOO_MANY_FILES",
                    Message = "Maximum 4 files allowed"
                });

            var uploadedImages = new List<ImageData>();
            var uploadPath = _configuration["Storage:LocalPath"] ?? "./uploads";
            var userUploadPath = Path.Combine(uploadPath, "images", userId);

            // Create directory if it doesn't exist
            Directory.CreateDirectory(userUploadPath);

            foreach (var file in files)
            {
                // Validate file
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var isImage = AllowedImageExtensions.Contains(extension);
                var isVideo = AllowedVideoExtensions.Contains(extension);

                if (!isImage && !isVideo)
                    return BadRequest(new ErrorResponse
                    {
                        Code = "INVALID_FILE_TYPE",
                        Message = $"File type {extension} is not allowed"
                    });

                if (isImage && file.Length > MaxImageSize)
                    return BadRequest(new ErrorResponse
                    {
                        Code = "FILE_TOO_LARGE",
                        Message = "Image file size cannot exceed 20MB"
                    });

                if (isVideo && file.Length > MaxVideoSize)
                    return BadRequest(new ErrorResponse
                    {
                        Code = "FILE_TOO_LARGE",
                        Message = "Video file size cannot exceed 50MB"
                    });

                // Generate unique filename
                var imageId = Guid.NewGuid().ToString();
                var fileName = $"{imageId}{extension}";
                var filePath = Path.Combine(userUploadPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create image data
                var imageData = new ImageData
                {
                    Id = imageId,
                    Src = $"/uploads/images/{userId}/{fileName}",
                    Alt = file.FileName,
                    Type = file.ContentType
                };

                uploadedImages.Add(imageData);
            }

            return Ok(new { images = uploadedImages });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "UPLOAD_ERROR",
                Message = "Failed to upload files",
                Details = ex.Message
            });
        }
    }
}
